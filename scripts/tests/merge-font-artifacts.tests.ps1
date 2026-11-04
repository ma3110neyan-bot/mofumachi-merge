$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../prepare-merge-reward-preview.ps1')
$Root=Join-Path ([IO.Path]::GetTempPath()) ('merge-font-fixture-'+[guid]::NewGuid().ToString('N'))
$Folder=Join-Path $Root 'screenshots/font-comparison';New-Item -ItemType Directory -Path $Folder -Force|Out-Null
$Rev='a'*40;$Fp='b'*64;$Packed='c'*64
function Fixture{
    $Images=@(foreach($Width in @(720,640)){foreach($Variant in @('A','B','C')){
        $Height=if($Width -eq 720){1480}else{1072};$Name=$Variant+'-'+$Width+'x'+$Height+'.jpg';$File=Join-Path $Folder $Name
        [IO.File]::WriteAllText($File,'synthetic font fixture '+$Name)
        @{file=$Name;width=$Width;height=$Height;variant=$Variant;font=$(if($Variant -ceq 'C'){'UIFontStory'}else{'UIFontAccent'});bold=($Variant -cne 'C');outline=$(if($Variant -ceq 'B'){.35}else{0});sha256=(Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant();state_sha256=('d'*64);layout_sha256=('e'*64)}
    }})
    $P=@{kind='merge-font-abc-native';source_revision=$Rev;product_fingerprint=$Fp;packed_resource_sha256=$Packed;native_unity=$true;disk_verified=$true;formal_fixed=$false;adopted=$false;pixel_verified=$false;output_directory=$Folder;image_count=6;images=$Images}
    $P|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $Folder 'merge-font-native.json') -Encoding UTF8
    [IO.File]::WriteAllText((Join-Path $Folder 'merge-font.html'),'<!doctype html>'+($Images.file -join ' '));return $P
}
try{
    $null=Fixture;$A=@(Assert-MergeFontArtifacts $Root $Rev $Fp $Packed);if($A.Count -ne 8){throw 'Six JPEGs plus JSON and HTML required.'}
    foreach($Case in @('jpeg','zero','json','html','sha','state','layout','dimensions','variant','output','accepted','count','link')){
        $P=Fixture
        switch($Case){
            jpeg {Remove-Item -LiteralPath (Join-Path $Folder $P.images[0].file)}
            zero {Get-ChildItem -LiteralPath $Folder -File|Remove-Item -Force}
            json {Remove-Item -LiteralPath (Join-Path $Folder 'merge-font-native.json')}
            html {Remove-Item -LiteralPath (Join-Path $Folder 'merge-font.html')}
            sha {$P.images[0].sha256=('f'*64)}
            state {$P.images[0].state_sha256=('f'*64)}
            layout {$P.images[0].layout_sha256=('f'*64)}
            dimensions {$P.images[0].height=1000}
            variant {$P.images[0].font='UIFontReading'}
            output {$P.output_directory=Join-Path $Root 'wrong'}
            accepted {$P.adopted=$true}
            count {$P.image_count=5}
            link {[IO.File]::WriteAllText((Join-Path $Folder 'merge-font.html'),'<!doctype html>missing links')}
        }
        if($Case -notin @('zero','json')){$P|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $Folder 'merge-font-native.json') -Encoding UTF8}
        $Rejected=$false;try{$null=Assert-MergeFontArtifacts $Root $Rev $Fp $Packed}catch{$Rejected=$true};if(-not $Rejected){throw "Invalid font proof accepted: $Case"}
    }
    $A=@(Get-MergeRewardArguments $Root $Root $Rev $Fp $Packed capture -FontComparison)
    if(($A -join ' ') -notmatch '-mofumachiMergeFontComparison' -or ($A -join ' ') -match '-executeMethod'){throw 'Font capture args wrong.'}
    $Rejected=$false;try{$null=Get-MergeRewardArguments $Root $Root $Rev $Fp $Packed PlayMode -FontComparison}catch{$Rejected=$true};if(-not $Rejected){throw 'Font-only mode reached full PlayMode.'}
}finally{if(Test-Path -LiteralPath $Root){Remove-Item -LiteralPath $Root -Recurse -Force}}
Write-Host 'Font comparison six-image/SHA/state/layout/HTML gates and capture-only arguments passed. Synthetic fixtures, not native renders.'
