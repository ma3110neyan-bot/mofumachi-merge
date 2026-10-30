[CmdletBinding()]
param([string]$UnityEditorPath='',[switch]$Install,[string]$Serial='99RAY1BELH',[ValidateRange(0,600)][int]$MeasureSeconds=60)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'build-android.ps1')
function Get-NoahAuthoredArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint) {
    if(-not $Repository -or -not $Directory -or $Revision -notmatch '^[a-f0-9]{40}$' -or $Fingerprint -notmatch '^[a-f0-9]{64}$'){throw 'Exact Noah source identity required.'}
    return @('-batchmode','-projectPath',$Repository,'-buildTarget','Android','-runTests','-testPlatform','PlayMode','-testFilter','Mofumachi.Tests.ExperienceCaptureTests.NoahAuthoredPartsPreviewPreservesState','-testResults',(Join-Path $Directory 'playmode.xml'),'-logFile',(Join-Path $Directory 'playmode.log'),'-mofumachiScreenshots',(Join-Path $Directory 'screenshots'),'-mofumachiSourceRevision',$Revision,'-mofumachiProductFingerprint',$Fingerprint,'-mofumachiNoahAuthoredPreview')
}
function Remove-OwnedNoahAuthoredResources([string]$Repository,[string]$Directory) {
    $Folder=Join-Path $Repository 'Assets/Mofumachi/Resources/Mofumachi/NoahAuthoredTrial'
    if(-not(Test-Path -LiteralPath $Folder)){return}
    $Marker=Join-Path $Folder '.owner'
    if(-not(Test-Path -LiteralPath $Marker -PathType Leaf) -or -not [string]::Equals([IO.File]::ReadAllText($Marker),[IO.Path]::GetFullPath($Directory),[StringComparison]::OrdinalIgnoreCase)){throw 'Refuse to delete Noah resources not owned by this run.'}
    [IO.Directory]::Delete($Folder,$true)
    if(Test-Path -LiteralPath ($Folder+'.meta')){[IO.File]::Delete($Folder+'.meta')}
}
function Read-NoahAuthoredPair([string]$Directory) {
    $RecordPath=Join-Path $Directory 'source-info.json'
    if(-not(Test-Path -LiteralPath $RecordPath -PathType Leaf)){throw 'New preview build has not completed. Use its exact printed run directory, not a previous trial.'}
    $Source=Get-Content -Raw -Encoding UTF8 -LiteralPath $RecordPath | ConvertFrom-Json
    if($Source.native_test_passed -isnot [bool] -or -not $Source.native_test_passed -or $Source.source_unchanged -isnot [bool] -or -not $Source.source_unchanged -or $Source.experimental_apk_pair_generated -isnot [bool] -or -not $Source.experimental_apk_pair_generated -or $Source.adopted -isnot [bool] -or $Source.adopted -or $Source.error){throw 'Preview native/build/source checks incomplete; do not install.'}
    $Pair=@{}
    foreach($Mode in @('current','authored')){
        $Path=Join-Path $Directory "noah-$Mode-build.json"
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "New $Mode APK identity missing; build stopped. Do not substitute old APKs."}
        $Info=Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
        if($Info.scope -ne 'experimental-noah-authored-not-release' -or $Info.mode -ne $Mode -or $Info.package -ne 'com.mofumachi.merge.qa.townlife' -or $Info.apk -ne "noah-$Mode.apk" -or $Info.gitRevision -notmatch '^[a-f0-9]{40}$' -or $Info.productFingerprint -notmatch '^[a-f0-9]{64}$' -or $Info.input_sha256 -notmatch '^[a-f0-9]{64}$' -or $Info.packed_resource_sha256 -notmatch '^[a-f0-9]{64}$' -or $Info.sha256 -notmatch '^[a-f0-9]{64}$' -or $Info.gitRevision -ne $Source.gitRevision -or $Info.productFingerprint -ne $Source.productFingerprint){throw 'Wrong preview source/mode/package identity.'}
        if($Info.native_capture_method_verified -isnot [bool] -or -not $Info.native_capture_method_verified){throw 'Single native method not verified.'}
        foreach($Key in @('full_qa_verified','pixel_verified','adopted')){if($Info.$Key -isnot [bool] -or $Info.$Key){throw 'Experimental preview cannot claim full QA,Pixel or adoption.'}}
        $Apk=Join-Path $Directory $Info.apk
        if(-not(Test-Path -LiteralPath $Apk -PathType Leaf) -or (Get-FileHash -Algorithm SHA256 -LiteralPath $Apk).Hash -ne $Info.sha256){throw 'Preview APK bytes do not match their identity.'}
        $Pair[$Mode]=$Info
    }
    if($Pair.current.input_sha256 -ne $Pair.authored.input_sha256 -or $Pair.current.packed_resource_sha256 -ne $Pair.authored.packed_resource_sha256 -or $Pair.current.sha256 -eq $Pair.authored.sha256){throw 'Distinct same-art/source APK pair required.'}
    return $Pair
}
function Get-NoahAuthoredSourceFingerprint([string]$Repository) {
    $Temp='Assets/Mofumachi/Resources/Mofumachi/NoahAuthoredTrial';$Entries=@(foreach($Relative in (@(Source-InputPaths)+@('References/Characters/noah-authored-parts-2026-10-29','References/Characters/noah-directional-parts-2026-10-30'))){
        $Path=Join-Path $Repository $Relative;if(-not(Test-Path -LiteralPath $Path)){continue}
        $Files=if(Test-Path -LiteralPath $Path -PathType Leaf){Get-Item -LiteralPath $Path -Force}else{Get-ChildItem -LiteralPath $Path -File -Recurse -Force}
        foreach($File in $Files){$Name=$File.FullName.Substring($Repository.TrimEnd('/','\').Length+1).Replace('\','/');if($Name -eq ($Temp+'.meta') -or $Name.StartsWith($Temp+'/')){continue};$Name+':'+(Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash}
    })
    $Hasher=[Security.Cryptography.SHA256]::Create();try{return ([BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes((($Entries | Sort-Object) -join "`n"))))).Replace('-','').ToLowerInvariant()}finally{$Hasher.Dispose()}
}
function Assert-NoahAuthoredFrozenInputs([string]$Repository) {
    $Record=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Repository 'References/VisualLock/noah-authored-game-candidate-2026-10-30.json') | ConvertFrom-Json
    if($Record.adopted -isnot [bool] -or $Record.adopted -or $Record.six_character_rollout_authorized -isnot [bool] -or $Record.six_character_rollout_authorized -or $Record.default_off -isnot [bool] -or -not $Record.default_off -or @($Record.protected_files).Count -lt 100){throw 'Wrong/unprotected authored trial scope.'}
    foreach($File in $Record.protected_files){
        $Path=Join-Path $Repository $File.path
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "Protected input missing: $($File.path)"}
        $Bytes=if($File.text_normalized){[Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($Path).Replace("`r`n","`n"))}else{[IO.File]::ReadAllBytes($Path)}
        $Hasher=[Security.Cryptography.SHA256]::Create();try{$Hash=([BitConverter]::ToString($Hasher.ComputeHash($Bytes))).Replace('-','').ToLowerInvariant()}finally{$Hasher.Dispose()}
        if($Hash -ne $File.sha256){throw "Frozen input changed: $($File.path)"}
    }
}
function Invoke-NoahAuthoredUnity([string]$Editor,[string[]]$EditorArguments,[string]$Directory) {
    $Text=($EditorArguments | ForEach-Object {'"'+$_+'"'}) -join ' '
    $Process=Start-Process -FilePath $Editor -ArgumentList $Text -Wait -PassThru
    if($Process.ExitCode -ne 0){throw "Unity exit $($Process.ExitCode). Preserve this run: $Directory"}
}
function Write-NoahAuthoredArchives([string]$Directory) {
    $Files=@(Get-ChildItem -LiteralPath $Directory -Recurse -File | Where-Object {$_.Extension -ne '.apk'} | Sort-Object FullName)
    if(@($Files.Name | Select-Object -Unique).Count -ne $Files.Count){throw 'Evidence filenames collide; original directory preserved.'}
    $Batch=New-Object 'System.Collections.Generic.List[string]';$Bytes=0L;$Part=1
    foreach($File in $Files){
        if($File.Length -gt 24MB){throw 'Single diagnostic file exceeds upload budget; original preserved.'}
        if($Batch.Count -and $Bytes+$File.Length -gt 24MB){$Zip=$Directory+('-{0:D2}.zip' -f $Part);Compress-Archive -LiteralPath $Batch.ToArray() -DestinationPath $Zip;$Zip;$Part++;$Batch.Clear();$Bytes=0L}
        $Batch.Add($File.FullName);$Bytes+=$File.Length
    }
    if($Batch.Count){$Zip=$Directory+('-{0:D2}.zip' -f $Part);Compress-Archive -LiteralPath $Batch.ToArray() -DestinationPath $Zip;$Zip}
}
if($MyInvocation.InvocationName -eq '.'){return}
$Repository=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$Git=Find-SourceGit
Assert-CleanProductSource $Repository $Git
Assert-NoahAuthoredFrozenInputs $Repository
$Temp=Join-Path $Repository 'Assets/Mofumachi/Resources/Mofumachi/NoahAuthoredTrial'
if((Test-Path -LiteralPath $Temp) -or (Test-Path -LiteralPath ($Temp+'.meta'))){throw 'Existing trial resources must be reviewed/cleaned by their owning run; nothing was deleted.'}
if(Test-Path -LiteralPath (Join-Path $Repository 'Assets/Mofumachi/Resources/Mofumachi/QaBuildRevision.txt')){throw 'Preserve existing QA revision stamp; inspect its owning build before this trial.'}
$Editor=if($UnityEditorPath){$UnityEditorPath}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf) -or -not(Test-Path -LiteralPath (Join-Path (Split-Path -Parent $Editor) 'Data/PlaybackEngines/AndroidPlayer') -PathType Container)){throw 'Unity6000.6.4f1 with Android Build Support required. No build or install performed.'}
$Revision=Read-SourceRevision $Repository;$Fingerprint=Get-NoahAuthoredSourceFingerprint $Repository
$Run=Join-Path $Repository ('Builds/Diagnostics/noah-authored-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory -Path $Run | Out-Null
@{gitRevision=$Revision;productFingerprint=$Fingerprint} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'context.json') -Encoding UTF8
$Record=[ordered]@{scope='experimental-noah-authored-not-release';gitRevision=$Revision;productFingerprint=$Fingerprint;native_test_passed=$false;source_unchanged=$false;experimental_apk_pair_generated=$false;pixel_verified=$false;adopted=$false;error=''};$Failure=''
Write-Host "Noah authored run: $Run"
try{
    $Common=@('-batchmode','-quit','-projectPath',$Repository,'-buildTarget','Android','-mofumachiNoahAuthoredDirectory',$Run)
    Invoke-NoahAuthoredUnity $Editor ($Common+@('-executeMethod','Mofumachi.Editor.NoahAuthoredBuild.Prepare','-logFile',(Join-Path $Run 'prepare.log'))) $Run
    Invoke-NoahAuthoredUnity $Editor (Get-NoahAuthoredArguments $Repository $Run $Revision $Fingerprint) $Run
    Assert-TestRun (Join-Path $Run 'playmode.xml') 1 @('ExperienceCaptureTests')
    [xml]$Xml=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Run 'playmode.xml');$Cases=@($Xml.SelectNodes('//test-case'))
    if($Cases.Count -ne 1 -or $Cases[0].fullname -ne 'Mofumachi.Tests.ExperienceCaptureTests.NoahAuthoredPartsPreviewPreservesState'){throw 'Wrong native method.'}
    if((Read-SourceRevision $Repository) -ne $Revision -or (Get-NoahAuthoredSourceFingerprint $Repository) -ne $Fingerprint){throw 'Sources changed during native trial.'}
    $Record.native_test_passed=$true
    Invoke-NoahAuthoredUnity $Editor ($Common+@('-executeMethod','Mofumachi.Editor.NoahAuthoredBuild.BuildPair','-logFile',(Join-Path $Run 'build.log'))) $Run
    foreach($Mode in @('current','authored')){if(-not(Test-Path -LiteralPath (Join-Path $Run "noah-$Mode-build.json") -PathType Leaf)){throw "New $Mode build identity missing. Stop before install."}}
    $Record.experimental_apk_pair_generated=$true
}catch{$Failure=$_.Exception.Message;$Record.error=$Failure}
finally{
    try{Remove-OwnedNoahAuthoredResources $Repository $Run}catch{if(-not $Failure){$Failure=$_.Exception.Message};$Record.error+=' Cleanup: '+$_.Exception.Message}
    try{if((Read-SourceRevision $Repository) -ne $Revision -or (Get-NoahAuthoredSourceFingerprint $Repository) -ne $Fingerprint){throw 'Source inputs changed; installation blocked.'};$Record.source_unchanged=$true}catch{if(-not $Failure){$Failure=$_.Exception.Message};$Record.error+=' '+$_.Exception.Message}
    $Record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'source-info.json') -Encoding UTF8
    try{foreach($Zip in @(Write-NoahAuthoredArchives $Run)){Write-Host "Noah authored review ZIP: $Zip"}}catch{if(-not $Failure){$Failure=$_.Exception.Message};Write-Host "Archive failure; full originals remain at $Run : $($_.Exception.Message)"}
}
if($Failure){$Record.error=$Failure;$Record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'source-info.json') -Encoding UTF8;throw "Noah-only preview stopped. Share this run's ZIP/logs; no full QA, old APK or save reset. $Failure"}
$Pair=Read-NoahAuthoredPair $Run
Write-Host "Current APK: $(Join-Path $Run $Pair.current.apk)"
Write-Host "Authored APK: $(Join-Path $Run $Pair.authored.apk)"
Write-Host 'Experimental only: front independent rig and new-direction expression/blink remain unfinished. User adoption and Pixel performance unverified.'
if($Install){& (Join-Path $PSScriptRoot 'test-noah-authored-pixel.ps1') -BuildDirectory $Run -Mode authored -Serial $Serial -Seconds $MeasureSeconds}
