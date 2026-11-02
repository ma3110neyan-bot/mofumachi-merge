$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../prepare-merge-reward-preview.ps1')
function RejectPreview([scriptblock]$Action){$Caught=$false;try{& $Action | Out-Null}catch{$Caught=$true};if(-not $Caught){throw 'Invalid Merge preview evidence admitted.'}}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-merge-preview-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path (Join-Path $Root 'screenshots') -Force | Out-Null
try{
    $Rev='a'*40;$Fp='b'*64;$Packed='c'*64
    $Capture=@(Get-MergeRewardArguments 'repo' $Root $Rev $Fp $Packed 'capture')
    $FilterIndex=[Array]::IndexOf($Capture,'-testFilter')
    if($FilterIndex -lt 0 -or $Capture[$FilterIndex+1] -cne 'Mofumachi.Tests.MergeRewardCaptureTests.MergeRewardCandidateScreens' -or $Capture -notcontains '-mofumachiMergeReward'){
        throw 'Capture must precede Visual Gate without starting other Merge or full suites.'
    }
    $A=@(Get-MergeRewardArguments 'repo' $Root $Rev $Fp $Packed 'focus')
    if($A -contains '-quit' -or $A -contains '-executeMethod' -or $A -notcontains '-testFilter' -or $A -notcontains '-mofumachiMergeReward' -or ($A -join ' ') -match 'Head|NoahAuthoredPartsPreview|SixAuthored'){throw 'Focus escaped Merge-only tests.'}
    foreach($Platform in @('EditMode','PlayMode')){
        $A=@(Get-MergeRewardArguments 'repo' $Root $Rev $Fp $Packed $Platform)
        if($A -contains '-testFilter' -or $A -contains '-quit' -or $A -contains '-executeMethod'){throw 'Full candidate stage must use the unfiltered platform suite once.'}
        if($Platform -eq 'PlayMode' -and $A -notcontains (Join-Path $Root 'full-playmode-capture')){throw 'Full PlayMode capture requires separate disk output.'}
    }
    $Files=@('initial-720x1480-before-reconstructed.jpg','initial-720x1480-after.jpg','initial-720x1280-before-reconstructed.jpg','initial-720x1280-after.jpg','initial-640x1072-before-reconstructed.jpg','initial-640x1072-after.jpg','bread-request-720x1480-before-reconstructed.jpg','bread-request-720x1480-after.jpg','multi-requirement-720x1480-after.jpg','inventory-ready-720x1480-after.jpg','full-board-720x1280-after.jpg')
    # Synthetic files validate routing/hash rejection only; never image/native acceptance.
    $Images=@(foreach($Name in $Files){$File=Join-Path $Root ('screenshots/'+$Name);[IO.File]::WriteAllText($File,'synthetic');$Name -cmatch '^(.+)-([0-9]+)x([0-9]+)-(before-reconstructed|after).jpg$'|Out-Null;@{file=$Name;scenario=$Matches[1];width=[int]$Matches[2];height=[int]$Matches[3];before_reconstructed=$Name.EndsWith('-before-reconstructed.jpg');sha256=(Get-FileHash $File -Algorithm SHA256).Hash.ToLowerInvariant();state_sha256=('d'*64);state_unchanged=$true;writes_unchanged=$true}})
    $Path=Join-Path $Root 'screenshots/merge-reward-native.json'
    $Good=@{kind='merge-reward-native-candidate';source_revision=$Rev;product_fingerprint=$Fp;packed_resource_sha256=$Packed;native_unity=$true;formal_fixed=$false;adopted=$false;pixel_verified=$false;disk_verified=$true;output_directory=(Join-Path $Root 'screenshots');image_count=11;images=$Images}|ConvertTo-Json -Depth 8
    [IO.File]::WriteAllText($Path,$Good);$null=Assert-MergeRewardProof $Root $Rev $Fp $Packed
    foreach($Case in @('identity','missing','duplicate','native','pixel','state','writes','label','paired-state','bytes')){
        $Bad=$Good|ConvertFrom-Json
        switch($Case){
            identity {$Bad.source_revision=('f'*40)}
            missing {$Bad.images=$Bad.images[1..10]}
            duplicate {$Bad.images[1]=$Bad.images[0]}
            native {$Bad.native_unity=$false}
            pixel {$Bad.pixel_verified=$true}
            state {$Bad.images[0].state_unchanged=$false}
            writes {$Bad.images[0].writes_unchanged=$false}
            label {$Bad.images[0].before_reconstructed=$false}
            paired-state {$Bad.images[7].state_sha256=('e'*64)}
            bytes {$Bad.images[0].sha256=('e'*64)}
        }
        [IO.File]::WriteAllText($Path,($Bad|ConvertTo-Json -Depth 8));RejectPreview {Assert-MergeRewardProof $Root $Rev $Fp $Packed}
    }
    $ApkPath=Join-Path $Root 'characters-authored.apk';[IO.File]::WriteAllText($ApkPath,'synthetic-apk');$Path=Join-Path $Root 'characters-authored-build.json'
    $Good=@{scope='characters-direct-pixel-no-tests';mode='authored';package='com.mofumachi.merge.qa.townlife';apk='characters-authored.apk';gitRevision=$Rev;productFingerprint=$Fp;packed_resource_sha256=$Packed;sha256=(Get-FileHash $ApkPath -Algorithm SHA256).Hash.ToLowerInvariant();tests_run=$false;full_qa_verified=$false;pixel_verified=$false}|ConvertTo-Json
    [IO.File]::WriteAllText($Path,$Good);$null=Assert-MergeRewardApk $Root $Rev $Fp $Packed
    foreach($Case in @('revision','hash','package','qa-claim')){$Bad=$Good|ConvertFrom-Json;switch($Case){revision{$Bad.gitRevision=('e'*40)} hash{$Bad.sha256=('f'*64)} package{$Bad.package='other'} qa-claim{$Bad.full_qa_verified=$true}};[IO.File]::WriteAllText($Path,($Bad|ConvertTo-Json));RejectPreview {Assert-MergeRewardApk $Root $Rev $Fp $Packed}}
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Merge preview argument/identity/pair-state/APK gates passed with synthetic fixtures; not native/full/device results.'
