[CmdletBinding()]
param([string]$UnityEditorPath='',[switch]$Install,[string]$Serial='99RAY1BELH')
$ErrorActionPreference='Stop'
$DirectOptions=@{Editor=$UnityEditorPath;Install=[bool]$Install;Serial=$Serial}
. (Join-Path $PSScriptRoot 'preserve-characters-prepared-inputs.ps1')
function Get-CharactersDirectArguments([string]$Repository,[string]$Directory,[string]$Step){
    $Methods=@{'prepare-noah'='Mofumachi.Editor.NoahAuthoredBuild.Prepare';'prepare-five'='Mofumachi.Editor.CharacterAuthoredBuild.PrepareAll';build='Mofumachi.Editor.CharactersDirectPixelBuild.Build'}
    if(-not $Repository -or -not $Directory -or -not $Methods.ContainsKey($Step)){throw 'Direct route allows preparation and one APK build only.'}
    $DirectArguments=@('-batchmode','-quit','-projectPath',$Repository,'-buildTarget','Android','-executeMethod',$Methods[$Step],'-logFile',(Join-Path $Directory ($Step+'.log')))
    $Flag=switch($Step){'prepare-noah' {'-mofumachiNoahAuthoredDirectory'};'prepare-five' {'-mofumachiCharacterDirectory'};build {'-mofumachiCharactersDirectDirectory'}}
    return $DirectArguments+@($Flag,$Directory)
}
function Read-CharactersDirectBuild([string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    if($Revision -cnotmatch '^[a-f0-9]{40}$' -or $Fingerprint -cnotmatch '^[a-f0-9]{64}$' -or $Packed -cnotmatch '^[a-f0-9]{64}$'){throw 'Exact direct source and resource identity required.'}
    $Source=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'source-info.json') | ConvertFrom-Json
    $Info=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'characters-authored-build.json') | ConvertFrom-Json
    foreach($Record in @($Source,$Info)){
        if($Record.scope -cne 'characters-direct-pixel-no-tests' -or $Record.gitRevision -cne $Revision -or $Record.productFingerprint -cne $Fingerprint -or $Record.packed_resource_sha256 -cne $Packed){throw 'Direct APK/source identity differs.'}
        foreach($Flag in @('tests_run','full_qa_verified','pixel_verified')){if($Record.$Flag -isnot [bool] -or $Record.$Flag){throw 'Direct preview cannot claim tests or device acceptance.'}}
    }
    if($Source.build_generated -isnot [bool] -or -not $Source.build_generated -or $Source.source_unchanged -isnot [bool] -or -not $Source.source_unchanged -or $Source.error){throw 'Direct preview failed or unfinished; installation blocked.'}
    if($Info.mode -cne 'authored' -or $Info.package -cne 'com.mofumachi.merge.qa.townlife' -or $Info.apk -cne 'characters-authored.apk' -or $Info.sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Wrong direct APK mode/package/name.'}
    $Apk=Join-Path $Directory $Info.apk
    if(-not(Test-Path -LiteralPath $Apk -PathType Leaf) -or (Get-FileHash -LiteralPath $Apk -Algorithm SHA256).Hash -ine $Info.sha256){throw 'Direct APK bytes differ from the new build identity.'}
    return $Info
}
if($MyInvocation.InvocationName -eq '.'){return}
$DirectRepository=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$DirectGit=Find-SourceGit
$DirectEditor=if($DirectOptions.Editor){$DirectOptions.Editor}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $DirectEditor -PathType Leaf) -or -not(Test-Path -LiteralPath (Join-Path (Split-Path -Parent $DirectEditor) 'Data/PlaybackEngines/AndroidPlayer'))){throw 'Unity6000.6.4f1 Android support is required for APK compilation; no Unity tests will run.'}
if(Get-Process -Name Unity -ErrorAction SilentlyContinue){throw 'Close the Unity Editor before building this APK.'}
$DirectSdk=Join-Path (Split-Path -Parent $DirectEditor) 'Data/PlaybackEngines/AndroidPlayer/SDK'
$DirectAdb=Join-Path $DirectSdk 'platform-tools/adb.exe';$DirectAapt=Join-Path $DirectSdk 'build-tools/36.0.0/aapt2.exe'
if($DirectOptions.Install){
    foreach($Path in @($DirectAdb,$DirectAapt)){if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "Android tool missing: $Path"}}
    $DirectOptions.Serial=Select-AuthorizedDevice (Invoke-CheckedNative $DirectAdb @('devices')) $DirectOptions.Serial
    if((Invoke-CheckedNative $DirectAdb @('-s',$DirectOptions.Serial,'shell','getprop','ro.product.model')).Trim() -cne 'Pixel 3a'){throw 'Connect the selected Pixel 3a.'}
}
$DirectPreserved=Move-PreviousCharacterInputs $DirectRepository $DirectGit
Assert-CleanProductSource $DirectRepository $DirectGit;Assert-CharactersVisualLock $DirectRepository;Assert-NoahAuthoredMasterFixed $DirectRepository;Assert-NoahAuthoredFrozenInputs $DirectRepository
foreach($Relative in @('CharacterAuthored','NoahAuthoredTrial','QaBuildRevision.txt')){
    $Path=Join-Path $DirectRepository ('Assets/Mofumachi/Resources/Mofumachi/'+$Relative)
    if((Test-Path -LiteralPath $Path) -or (Test-Path -LiteralPath ($Path+'.meta'))){throw 'Existing generated inputs preserved; inspect their owning run.'}
}
$DirectRevision=Read-SourceRevision $DirectRepository;$DirectFingerprint=Get-CharacterAuthoredSourceFingerprint $DirectRepository
$DirectRun=Join-Path $DirectRepository ('Builds/Diagnostics/characters-direct-pixel-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory -Path $DirectRun | Out-Null
$DirectRecord=[ordered]@{scope='characters-direct-pixel-no-tests';gitRevision=$DirectRevision;productFingerprint=$DirectFingerprint;packed_resource_sha256='';preserved_inputs=$DirectPreserved;tests_run=$false;full_qa_verified=$false;pixel_verified=$false;build_generated=$false;source_unchanged=$false;error=''};$DirectFailure=$null
Write-Host "Direct Pixel APK run (no Unity tests): $DirectRun"
try{
    foreach($Step in @('prepare-noah','prepare-five')){Invoke-NoahAuthoredUnity $DirectEditor (Get-CharactersDirectArguments $DirectRepository $DirectRun $Step) $DirectRun}
    $DirectRecord.packed_resource_sha256=Get-PreparedCharactersDigest $DirectRepository
    @{gitRevision=$DirectRevision;productFingerprint=$DirectFingerprint;packed_resource_sha256=$DirectRecord.packed_resource_sha256} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $DirectRun 'direct-context.json') -Encoding UTF8
    Invoke-NoahAuthoredUnity $DirectEditor (Get-CharactersDirectArguments $DirectRepository $DirectRun 'build') $DirectRun
    if((Get-PreparedCharactersDigest $DirectRepository) -cne $DirectRecord.packed_resource_sha256){throw 'Prepared resources changed during the direct APK build.'}
    $DirectRecord.build_generated=$true
}catch{$DirectFailure=$_;$DirectRecord.error=$_.Exception.Message}
finally{
    try{Remove-OwnedCharacterResources $DirectRepository $DirectRun;Remove-OwnedNoahAuthoredResources $DirectRepository $DirectRun}catch{if(-not $DirectFailure){$DirectFailure=$_};$DirectRecord.error+=' Cleanup: '+$_.Exception.Message}
    try{
        if((Read-SourceRevision $DirectRepository) -cne $DirectRevision -or (Get-CharacterAuthoredSourceFingerprint $DirectRepository) -cne $DirectFingerprint){throw 'Product source changed during build.'}
        Assert-CharactersVisualLock $DirectRepository;Assert-NoahAuthoredMasterFixed $DirectRepository;Assert-NoahAuthoredFrozenInputs $DirectRepository;$DirectRecord.source_unchanged=$true
    }catch{if(-not $DirectFailure){$DirectFailure=$_};$DirectRecord.error+=' '+$_.Exception.Message}
    $DirectRecord | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $DirectRun 'source-info.json') -Encoding UTF8
    Write-Host "APK/logs preserved: $DirectRun"
}
if($DirectFailure){throw $DirectFailure}
$DirectInfo=Read-CharactersDirectBuild $DirectRun $DirectRevision $DirectFingerprint $DirectRecord.packed_resource_sha256
Write-Host "New authored APK: $(Join-Path $DirectRun $DirectInfo.apk)"
if($DirectOptions.Install){Invoke-PixelInstall (Join-Path $DirectRun $DirectInfo.apk) $DirectAdb $DirectOptions.Serial $DirectInfo.package $DirectAapt -ExpectedSha256 $DirectInfo.sha256}
Write-Host 'Direct preview ready. No Unity tests, metrics, uninstall or save reset. Check the updated town QA app on Pixel.'
