$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../prepare-merge-reward-preview.ps1')
function RejectCapture([scriptblock]$Action){$Caught=$false;try{& $Action | Out-Null}catch{$Caught=$true};if(-not $Caught){throw 'Incomplete capture artifacts were accepted.'}}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('merge-capture-contract-'+[guid]::NewGuid().ToString('N'));$Shots=Join-Path $Root 'screenshots';New-Item -ItemType Directory -Path $Shots -Force | Out-Null
$Rev='a'*40;$Fp='b'*64;$Packed='c'*64
$Names=@('initial-720x1480-before-reconstructed.jpg','initial-720x1480-after.jpg','initial-720x1280-before-reconstructed.jpg','initial-720x1280-after.jpg','initial-640x1072-before-reconstructed.jpg','initial-640x1072-after.jpg','bread-request-720x1480-before-reconstructed.jpg','bread-request-720x1480-after.jpg','multi-requirement-720x1480-after.jpg','inventory-ready-720x1480-after.jpg','full-board-720x1280-after.jpg')
function ResetCaptureFixture{
    $Images=@(foreach($Name in $Names){$File=Join-Path $Shots $Name;[IO.File]::WriteAllText($File,'synthetic contract bytes: '+$Name);$Name -cmatch '^(.+)-([0-9]+)x([0-9]+)-(before-reconstructed|after).jpg$'|Out-Null;@{file=$Name;scenario=$Matches[1];width=[int]$Matches[2];height=[int]$Matches[3];before_reconstructed=$Name.EndsWith('-before-reconstructed.jpg');sha256=(Get-FileHash $File -Algorithm SHA256).Hash.ToLowerInvariant();state_sha256=('d'*64);state_unchanged=$true;writes_unchanged=$true}})
    $P=@{kind='merge-reward-native-candidate';source_revision=$Rev;product_fingerprint=$Fp;packed_resource_sha256=$Packed;native_unity=$true;formal_fixed=$false;adopted=$false;pixel_verified=$false;output_directory=$Shots;image_count=11;disk_verified=$true;original_png_saved=$false;reference_sha256='';images=$Images}
    $P|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $Shots 'merge-reward-native.json') -Encoding UTF8
    return $P
}
try{
    $P=ResetCaptureFixture;$null=Assert-MergeRewardProof $Root $Rev $Fp $Packed
    $P.output_directory=Join-Path $Root 'wrong-folder';$P|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $Shots 'merge-reward-native.json') -Encoding UTF8
    RejectCapture {Assert-MergeRewardProof $Root $Rev $Fp $Packed}
    $null=ResetCaptureFixture
    $Record=[ordered]@{capture_verified=$true;source_unchanged=$true;error='';artifact_directory='';image_count=0;artifacts=@()}
    RejectCapture {Confirm-MergeRewardCapture $Record $Root $Rev $Fp $Packed}
    if($Record.capture_verified){throw 'HTML missing but capture_verified stayed true.'}
    Write-MergeRewardViewer $Root (Join-Path $Root 'missing-reference.png') (Assert-MergeRewardProof $Root $Rev $Fp $Packed)
    Confirm-MergeRewardCapture $Record $Root $Rev $Fp $Packed
    if(-not $Record.capture_verified -or $Record.image_count -ne 11 -or $Record.artifacts.Count -ne 13 -or $Record.artifact_directory -cne $Shots){throw 'Only complete disk evidence may be accepted.'}
    foreach($Case in @('one-jpeg','all-jpegs','json','html','empty-html','empty-jpeg','html-links','source-failed','source-error','zero-artifacts')){
        $null=ResetCaptureFixture
        Write-MergeRewardViewer $Root (Join-Path $Root 'missing-reference.png') (Assert-MergeRewardProof $Root $Rev $Fp $Packed)
        $Record.capture_verified=$true;$Record.source_unchanged=$true;$Record.error=''
        switch($Case){
            one-jpeg {Remove-Item -LiteralPath (Join-Path $Shots $Names[0])}
            all-jpegs {foreach($Name in $Names){Remove-Item -LiteralPath (Join-Path $Shots $Name)}}
            json {Remove-Item -LiteralPath (Join-Path $Shots 'merge-reward-native.json')}
            html {Remove-Item -LiteralPath (Join-Path $Shots 'merge-reward.html')}
            empty-html {[IO.File]::WriteAllText((Join-Path $Shots 'merge-reward.html'),'')}
            empty-jpeg {[IO.File]::WriteAllText((Join-Path $Shots $Names[0]),'')}
            html-links {[IO.File]::WriteAllText((Join-Path $Shots 'merge-reward.html'),'<html>no capture links</html>')}
            source-failed {$Record.source_unchanged=$false}
            source-error {$Record.error='cleanup failed'}
            zero-artifacts {Get-ChildItem -LiteralPath $Shots -File|Remove-Item -Force}
        }
        RejectCapture {Confirm-MergeRewardCapture $Record $Root $Rev $Fp $Packed}
        if($Record.capture_verified){throw "Failed $Case capture remained verified."}
    }
    # Check again after a successful validation: disappearance before final publication must fail.
    $null=ResetCaptureFixture;Write-MergeRewardViewer $Root (Join-Path $Root 'none.png') (Assert-MergeRewardProof $Root $Rev $Fp $Packed)
    $Record.error='';$Record.source_unchanged=$true;Confirm-MergeRewardCapture $Record $Root $Rev $Fp $Packed
    Remove-Item -LiteralPath (Join-Path $Shots 'merge-reward.html');RejectCapture {Confirm-MergeRewardCapture $Record $Root $Rev $Fp $Packed}
    if($Record.capture_verified){throw 'Post-cleanup disappearance was accepted.'}
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Capture disk contracts passed: missing/wrong directory, zero/ten images, JSON/HTML loss and failed cleanup reject success. Synthetic fixtures only.'
