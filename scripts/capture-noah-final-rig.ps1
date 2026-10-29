[CmdletBinding()]
param([string]$UnityEditorPath='',[switch]$BuildApks,[switch]$Pixel3a)
$ErrorActionPreference='Stop'
$FinalEditorPath=$UnityEditorPath
. (Join-Path $PSScriptRoot 'capture-noah-reprototype.ps1')
function Get-NoahFinalRigArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint) {
    return @((Get-NoahReprototypeArguments $Repository $Directory $Revision $Fingerprint))+@('-mofumachiNoahFinalRig')
}
function Assert-NoahFinalTrialInputs([string]$Repository) {
    $Record=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Repository 'References/VisualLock/noah-final-rig-candidate-2026-10-29.json') | ConvertFrom-Json
    if($Record.scope_id -ne 'town_noah_quality_2026_10_28' -or $Record.trial_only -isnot [bool] -or -not $Record.trial_only){throw 'Final trial must remain Noah-only.'}
    foreach($Key in @('adopted','six_character_rollout_authorized')){if($Record.$Key -isnot [bool] -or $Record.$Key){throw 'Final trial must remain unadopted with no rollout.'}}
    if(@($Record.trial_inputs).Count -ne 2){throw 'Final trial must bind its exact editor builder and meta.'}
    $Expected=@('Assets/Mofumachi/Editor/NoahFinalRigBuild.cs','Assets/Mofumachi/Editor/NoahFinalRigBuild.cs.meta')
    foreach($InputFile in $Record.trial_inputs){if($InputFile.path -notin $Expected -or $InputFile.sha256 -notmatch '^[a-f0-9]{64}$'){throw 'Unexpected trial input.'};$Expected=@($Expected | Where-Object {$_ -ne $InputFile.path});$Text=[IO.File]::ReadAllText((Join-Path $Repository $InputFile.path)).Replace("`r`n","`n");if((Get-VisualLockDigest ([Text.Encoding]::UTF8.GetBytes($Text))) -ne $InputFile.sha256){throw 'Trial builder changed after binding.'}}
    if($Expected.Count){throw 'Missing trial input binding.'}
}
function Invoke-NoahTrialPairActions([scriptblock]$CurrentAction,[scriptblock]$RigAction,[scriptblock]$RestoreAction) {
    $CaptureFailure=$null;$RestoreFailure=$null
    try{& $CurrentAction;& $RigAction}catch{$CaptureFailure=$_}
    try{& $RestoreAction}catch{$RestoreFailure=$_;Write-Host ('Current renderer restoration failed: '+$_.Exception.Message)}
    if($CaptureFailure){throw $CaptureFailure};if($RestoreFailure){throw $RestoreFailure}
}
if($MyInvocation.InvocationName -eq '.'){return}
if($Pixel3a -and -not $BuildApks){throw '-Pixel3a requires -BuildApks so the exact current/rig APK pair is available.'}
$Repository=Split-Path -Parent $PSScriptRoot;$Git=Find-SourceGit
Assert-CleanProductSource $Repository $Git
$Revision=Read-SourceRevision $Repository;$Fingerprint=Get-ProductFingerprint $Repository
Assert-ProductUnchanged $Repository $Revision $Fingerprint;Assert-NoahFinalTrialInputs $Repository
$Editor=if($FinalEditorPath){$FinalEditorPath}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf)){throw 'Unity6000.6.4f1 not found; use -UnityEditorPath.'}
$OutputDir=Join-Path $Repository ('Builds/Diagnostics/noah-final-rig-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+([guid]::NewGuid().ToString('N').Substring(0,8)))
New-Item -ItemType Directory -Path (Join-Path $OutputDir 'screenshots/noah-reprototype') -Force | Out-Null
$Record=[ordered]@{scope='single-native-Noah-final-rig-not-full-QA';gitRevision=$Revision;productFingerprint=$Fingerprint;native_test_passed=$false;source_unchanged=$false;qa_apk_generated=$false;experimental_apk_pair_generated=$false;pixel_verified=$false;adopted=$false;error=''}
$Failure=''
try {
    Invoke-Unity (Get-NoahFinalRigArguments $Repository $OutputDir $Revision $Fingerprint) -EditorPath $Editor
    Assert-TestRun (Join-Path $OutputDir 'playmode.xml') 1 @('ExperienceCaptureTests')
    [xml]$Xml=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $OutputDir 'playmode.xml');$Cases=@($Xml.SelectNodes('//test-case'))
    if($Cases.Count -ne 1 -or $Cases[0].fullname -ne 'Mofumachi.Tests.ExperienceCaptureTests.NoahReprototypeContinuousSequencePreservesArtAndSavedState'){throw 'Not the exact single Noah method.'}
    $Atlas=(Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $Repository 'Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png')).Hash.ToLowerInvariant()
    Assert-NoahReprototypeEvidence (Join-Path $OutputDir 'screenshots/noah-reprototype') $Revision $Fingerprint $Atlas -FinalRig
    Assert-ProductUnchanged $Repository $Revision $Fingerprint;$Record.native_test_passed=$true;$Record.source_unchanged=$true
    $Record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDir 'source-info.json') -Encoding UTF8
    if($BuildApks){
        Invoke-Unity @('-batchmode','-nographics','-quit','-projectPath',$Repository,'-buildTarget','Android','-executeMethod','Mofumachi.Editor.NoahFinalRigBuild.BuildPair','-mofumachiRevision',$Revision,'-mofumachiProductFingerprint',$Fingerprint,'-mofumachiNoahTrialDirectory',$OutputDir,'-logFile',(Join-Path $OutputDir 'trial-build.log')) -EditorPath $Editor
        foreach($Mode in @('current','rig')){if(-not(Test-Path -LiteralPath (Join-Path $OutputDir "noah-$Mode-build.json") -PathType Leaf)){throw 'Experimental APK identity missing.'}}
        $Record.experimental_apk_pair_generated=$true
    }
}catch{$Failure=$_.Exception.Message;$Record.error=$Failure}
try{Assert-ProductUnchanged $Repository $Revision $Fingerprint;$Record.source_unchanged=$true}catch{$Record.source_unchanged=$false;$Failure=$_.Exception.Message;$Record.error=$Failure}
$Record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDir 'source-info.json') -Encoding UTF8
foreach($Zip in @(Write-NoahReprototypeArchives $OutputDir -ExtraNames @('trial-build.log','noah-current-build.json','noah-rig-build.json'))){Write-Host "Noah final-trial review ZIP: $Zip"}
Write-Host 'Exactly one Noah method. No full suites, Pixel install, uninstall or save reset. Trial APKs are experimental, not release acceptance.'
if($Failure){throw "Final trial stopped. Share this run's ZIPs/log; do not rerun full QA. $Failure"}
Write-Host "Final-trial directory: $OutputDir"
if($BuildApks){Write-Host 'Two experimental APKs generated. Use capture-noah-final-pixel.ps1 with this exact directory, current first then rig.'}
Start-Process -FilePath (Join-Path $OutputDir 'screenshots/noah-reprototype/noah-reprototype.html')
if($Pixel3a){
    Invoke-NoahTrialPairActions {
        & (Join-Path $PSScriptRoot 'capture-noah-final-pixel.ps1') -BuildDirectory $OutputDir -Mode current
    } {
        & (Join-Path $PSScriptRoot 'capture-noah-final-pixel.ps1') -BuildDirectory $OutputDir -Mode rig
    } {
        # Return the device to the current renderer after this experiment.
        # install-r preserves the save; no trial adoption is implied.
        . (Join-Path $PSScriptRoot 'capture-noah-final-pixel.ps1')
        $CurrentBuild=Read-NoahTrialBuild $OutputDir 'current'
        $TrialAdb=Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe'
        & (Join-Path $PSScriptRoot 'install-pixel3a.ps1') -Apk (Join-Path $OutputDir $CurrentBuild.apk) -Adb $TrialAdb -Serial '99RAY1BELH' -PackageName $CurrentBuild.package -ExpectedSha256 $CurrentBuild.sha256
    }
}
