[CmdletBinding()]
param([string]$UnityEditorPath='')
$ErrorActionPreference='Stop'
$HeadSyncEditor=$UnityEditorPath
. (Join-Path $PSScriptRoot 'preserve-characters-prepared-inputs.ps1')
function Get-HeadSyncArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed) {
    if(-not $Repository -or -not $Directory -or $Revision -notmatch '^[a-f0-9]{40}$' -or $Fingerprint -notmatch '^[a-f0-9]{64}$' -or $Packed -notmatch '^[a-f0-9]{64}$'){throw 'Exact head-sync source and packed identity required.'}
    return @('-batchmode','-projectPath',$Repository,'-buildTarget','Android','-runTests','-testPlatform','PlayMode','-testFilter','Mofumachi.Tests.ExperienceCaptureTests.SixCharactersKeepHeadAndBodySynchronized','-testResults',(Join-Path $Directory 'head-sync-playmode.xml'),'-logFile',(Join-Path $Directory 'head-sync-playmode.log'),'-mofumachiHeadSync','-mofumachiHeadSyncDirectory',$Directory,'-mofumachiSourceRevision',$Revision,'-mofumachiProductFingerprint',$Fingerprint,'-mofumachiPackedDigest',$Packed)
}
function Assert-HeadSyncProof($Proof,[string]$Revision,[string]$Fingerprint,[string]$Packed) {
    if($Revision -notmatch '^[a-f0-9]{40}$' -or $Fingerprint -notmatch '^[a-f0-9]{64}$' -or $Packed -notmatch '^[a-f0-9]{64}$'){throw 'Head-sync expected identity is invalid.'}
    if($Proof.kind -cne 'six-character-head-body-sync-only' -or $Proof.gitRevision -cne $Revision -or $Proof.productFingerprint -cne $Fingerprint -or $Proof.packed_resource_sha256 -cne $Packed){throw 'Head-sync proof/source identity mismatch.'}
    foreach($Flag in @('native_unity','save_unchanged','non_head_unchanged','head_art_size_angle_unchanged')){if($Proof.$Flag -isnot [bool] -or -not $Proof.$Flag){throw "Head-sync invariant failed: $Flag"}}
    foreach($Flag in @('full_qa_verified','pixel_verified','formal_fixed','adopted')){if($Proof.$Flag -isnot [bool] -or $Proof.$Flag){throw 'Focused head proof cannot claim unrelated acceptance.'}}
    $Ids=@('fio','lulu','miel','momo','noah','pom')
    if(@($Proof.characters).Count -ne 6 -or @($Proof.characters | Select-Object -Unique).Count -ne 6 -or @(Compare-Object $Ids @($Proof.characters)).Count -or $Proof.misalignments -ne 0 -or $Proof.unmet_characters -ne 0){throw 'All six identities with zero head misalignments are required.'}
    $States=@{'front-idle'='Front';'front-to-left-start'='Left';'left-walk'='Left';'left-to-right-walk'='Right';'right-walk'='Right';'right-to-back-walk'='Back';'back-walk'='Back';'back-to-front-stop-before'='Front';'front-stop-after'='Front'}
    if(@($Proof.samples).Count -ne 432 -or @($Proof.images).Count -ne 36){throw 'Full focused sample/image coverage missing.'}
    $Seen=@{}
    foreach($Sample in @($Proof.samples)){
        $Key=$Sample.character+':'+$Sample.state+':'+$Sample.tick
        if($Sample.character -cnotin $Ids -or -not $States.ContainsKey($Sample.state) -or $Sample.direction -cne $States[$Sample.state] -or $Sample.tick -lt 0 -or $Sample.tick -gt 7 -or $Seen.ContainsKey($Key)){throw 'Missing, duplicate or foreign head sample.'};$Seen[$Key]=$true
        if($Sample.front_single_mesh -isnot [bool] -or $Sample.front_single_mesh -ne ($Sample.direction -ceq 'Front') -or $Sample.blink -isnot [bool]){throw 'Front/split-material sample mismatch.'}
        if($Sample.direction -cne 'Front' -and $Sample.blink -ne ($Sample.character -cne 'noah' -and $Sample.tick -eq 7)){throw 'Closed-head material coverage missing.'}
        if([double]::IsNaN([double]$Sample.error) -or [double]::IsInfinity([double]$Sample.error) -or [double]$Sample.error -lt 0 -or [double]$Sample.error -gt .002){throw 'Rendered neck contact failed.'}
        if($Sample.direction -cne 'Front'){
            $ErrorX=[double]$Sample.head_x-[double]$Sample.body_x;$ErrorY=[double]$Sample.head_y-[double]$Sample.body_y
            $Distance=[Math]::Sqrt($ErrorX*$ErrorX+$ErrorY*$ErrorY)
            if([double]::IsNaN($Distance) -or [double]::IsInfinity($Distance) -or $Distance -gt .002 -or [Math]::Abs($Distance-[double]$Sample.error) -gt .0001){throw 'Head/body coordinate evidence disagrees.'}
        }
    }
    foreach($Id in $Ids){foreach($State in $States.Keys){for($Tick=0;$Tick -lt 8;$Tick++){if(-not $Seen.ContainsKey($Id+':'+$State+':'+$Tick)){throw 'Head transition coverage incomplete.'}}}}
}
function Assert-HeadSyncImages([string]$Directory,$Proof){
    $Expected=@();foreach($State in @('front-idle','front-to-left-start','left-walk','left-to-right-walk','right-walk','right-to-back-walk','back-walk','back-to-front-stop-before','front-stop-after')){foreach($Tick in @(0,7)){foreach($Kind in @('before','after')){$Expected+=($State+'-'+$Tick+'-'+$Kind+'.png')}}}
    if(@($Proof.images).Count -ne 36 -or @($Proof.images.file | Select-Object -Unique).Count -ne 36 -or @(Compare-Object $Expected @($Proof.images.file)).Count){throw 'Head comparison image set is incomplete.'}
    foreach($Image in @($Proof.images)){
        $Path=Join-Path $Directory $Image.file
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf) -or $Image.sha256 -notmatch '^[a-f0-9]{64}$' -or (Get-Item -LiteralPath $Path).Length -ne $Image.bytes -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ine $Image.sha256){throw 'Head image bytes do not match the native manifest.'}
        $Stream=[IO.File]::OpenRead($Path);try{$Header=New-Object byte[] 24;if($Stream.Read($Header,0,24) -ne 24){throw 'Head PNG is truncated.'}}finally{$Stream.Dispose()}
        if([BitConverter]::ToString($Header[0..7]) -cne '89-50-4E-47-0D-0A-1A-0A'){throw 'Head image is not PNG.'}
        [byte[]]$Width=$Header[16..19];[byte[]]$Height=$Header[20..23];[Array]::Reverse($Width);[Array]::Reverse($Height)
        if($Image.width -ne 720 -or $Image.height -ne 1280 -or [BitConverter]::ToInt32($Width,0) -ne $Image.width -or [BitConverter]::ToInt32($Height,0) -ne $Image.height){throw 'Head portrait dimensions are wrong.'}
    }
}
function Assert-HeadSyncResults([string]$Path){
    [xml]$Xml=Get-Content -Raw -Encoding UTF8 -LiteralPath $Path;$Cases=@($Xml.SelectNodes('//test-case'))
    if($Cases.Count -ne 1 -or $Cases[0].fullname -cne 'Mofumachi.Tests.ExperienceCaptureTests.SixCharactersKeepHeadAndBodySynchronized'){throw 'Exactly the head-sync capture method is required.'}
    Assert-TestRun $Path 1 @('ExperienceCaptureTests')
}
if($MyInvocation.InvocationName -eq '.'){return}
$Repository=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$Git=Find-SourceGit
$Preserved=Move-PreviousCharacterInputs $Repository $Git
Assert-CleanProductSource $Repository $Git;Assert-NoahAuthoredMasterFixed $Repository;Assert-NoahAuthoredFrozenInputs $Repository
$Editor=if($HeadSyncEditor){$HeadSyncEditor}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf) -or -not(Test-Path -LiteralPath (Join-Path (Split-Path -Parent $Editor) 'Data/PlaybackEngines/AndroidPlayer'))){throw 'Unity6000.6.4f1 with Android support required.'}
$Revision=Read-SourceRevision $Repository;$Fingerprint=Get-CharacterAuthoredSourceFingerprint $Repository
$Run=Join-Path $Repository ('Builds/Diagnostics/characters-head-sync-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory -Path $Run | Out-Null
$Record=[ordered]@{scope='head-body-sync-only';gitRevision=$Revision;productFingerprint=$Fingerprint;packed_resource_sha256='';preserved_inputs=$Preserved;head_native_verified=$false;full_qa_verified=$false;pixel_verified=$false;formal_fixed=$false;adopted=$false;source_unchanged=$false;error=''}
$Failure=$null;$FirstZip='';Write-Host "Exact head-sync run: $Run"
try{
    $Common=@('-batchmode','-quit','-projectPath',$Repository,'-buildTarget','Android')
    Invoke-NoahAuthoredUnity $Editor ($Common+@('-executeMethod','Mofumachi.Editor.NoahAuthoredBuild.Prepare','-mofumachiNoahAuthoredDirectory',$Run,'-logFile',(Join-Path $Run 'prepare-noah.log'))) $Run
    Invoke-NoahAuthoredUnity $Editor ($Common+@('-executeMethod','Mofumachi.Editor.CharacterAuthoredBuild.PrepareAll','-mofumachiCharacterDirectory',$Run,'-logFile',(Join-Path $Run 'prepare-five.log'))) $Run
    $Record.packed_resource_sha256=Get-PreparedCharactersDigest $Repository
    Invoke-NoahAuthoredUnity $Editor (Get-HeadSyncArguments $Repository $Run $Revision $Fingerprint $Record.packed_resource_sha256) $Run
    Assert-HeadSyncResults (Join-Path $Run 'head-sync-playmode.xml')
    $Proof=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Run 'screenshots/head-sync/head-sync-proof.json') | ConvertFrom-Json
    Assert-HeadSyncProof $Proof $Revision $Fingerprint $Record.packed_resource_sha256
    Assert-HeadSyncImages (Join-Path $Run 'screenshots/head-sync') $Proof
    if((Get-PreparedCharactersDigest $Repository) -cne $Record.packed_resource_sha256){throw 'Packed inputs changed during head capture.'};$Record.head_native_verified=$true
}catch{$Failure=$_;$Record.error=$_.Exception.Message}
finally{
    try{Remove-OwnedCharacterResources $Repository $Run;Remove-OwnedNoahAuthoredResources $Repository $Run}catch{if(-not $Failure){$Failure=$_};$Record.error+=' Cleanup: '+$_.Exception.Message}
    try{
        if((Read-SourceRevision $Repository) -cne $Revision -or (Get-CharacterAuthoredSourceFingerprint $Repository) -cne $Fingerprint){throw 'Source changed during head capture.'}
        Assert-NoahAuthoredMasterFixed $Repository;Assert-NoahAuthoredFrozenInputs $Repository;$Record.source_unchanged=$true
    }catch{if(-not $Failure){$Failure=$_};$Record.error+=' '+$_.Exception.Message}
    $Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Run 'source-info.json') -Encoding UTF8
    try{foreach($Zip in @(Write-CharactersReviewArchives $Run)){if(-not $FirstZip){$FirstZip=$Zip};Write-Host "Head-sync review ZIP: $Zip"}}catch{if(-not $Failure){$Failure=$_};Write-Host $_}
    Write-Host 'One head-only native method for six characters. No full suites, APK/install, passing/conversation change or Save reset.'
    if($FirstZip -and $env:OS -eq 'Windows_NT'){Start-Process -FilePath explorer.exe -ArgumentList ('/select,"'+$FirstZip+'"')}
}
if($Failure){throw $Failure}
Write-Host 'Head-only native gate passed. Review all six new images before formal visual freeze.'
