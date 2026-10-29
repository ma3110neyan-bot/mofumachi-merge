$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../capture-noah-final-rig.ps1')
function RejectFinal([scriptblock]$Action){$Caught=$false;try{& $Action}catch{$Caught=$true};if(-not $Caught){throw 'Invalid final trial accepted.'}}
$A=@(Get-NoahFinalRigArguments 'repo with spaces' 'out with spaces' ('a'*40) ('b'*64))
if($A[[Array]::IndexOf($A,'-testFilter')+1] -ne 'Mofumachi.Tests.ExperienceCaptureTests.NoahReprototypeContinuousSequencePreservesArtAndSavedState' -or @($A | Where-Object {$_ -eq '-mofumachiNoahFinalRig'}).Count -ne 1){throw 'Final pass must run exactly the one Noah method with explicit final mode.'}
if('-quit' -in $A -or 'EditMode' -in $A){throw 'Wrong capture job.'}
RejectFinal {Get-NoahFinalRigArguments 'repo' 'out' 'bad-source' ('b'*64)}
. (Join-Path $PSScriptRoot '../capture-noah-final-pixel.ps1')
$D=Join-Path ([IO.Path]::GetTempPath()) ('mofu-final-device-'+[guid]::NewGuid());New-Item -ItemType Directory -Path $D | Out-Null
try {
    $RecordDir=Join-Path $D 'References/VisualLock';New-Item -ItemType Directory -Path $RecordDir -Force | Out-Null
    $Inputs=@(foreach($Name in @('Assets/Mofumachi/Editor/NoahFinalRigBuild.cs','Assets/Mofumachi/Editor/NoahFinalRigBuild.cs.meta')){
        $Path=Join-Path $D $Name;New-Item -ItemType Directory -Path (Split-Path -Parent $Path) -Force | Out-Null;[IO.File]::WriteAllText($Path,'trial fixture')
        @{path=$Name;sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()}
    })
    $Record=@{scope_id='town_noah_quality_2026_10_28';trial_only=$true;adopted=$false;six_character_rollout_authorized=$false;trial_inputs=$Inputs};$RecordPath=Join-Path $RecordDir 'noah-final-rig-candidate-2026-10-29.json'
    $Record | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $RecordPath;Assert-NoahFinalTrialInputs $D
    $Changed=Join-Path $D $Inputs[0].path;[IO.File]::AppendAllText($Changed,' changed');RejectFinal {Assert-NoahFinalTrialInputs $D};[IO.File]::WriteAllText($Changed,'trial fixture')
    $Record.adopted=$true;$Record | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $RecordPath;RejectFinal {Assert-NoahFinalTrialInputs $D}
    [IO.File]::WriteAllText((Join-Path $D 'trial-build.log'),'raw build record')
    $Parts=@(Write-NoahReprototypeArchives $D -ExtraNames @('trial-build.log'));Add-Type -AssemblyName System.IO.Compression.FileSystem
    $Z=[IO.Compression.ZipFile]::OpenRead($Parts[0]);try{if($null -eq $Z.GetEntry('trial-build.log')){throw 'Build diagnostics omitted from final review ZIP.'}}finally{$Z.Dispose()}
    foreach($Part in $Parts){Remove-Item -LiteralPath $Part}
    RejectFinal {Write-NoahReprototypeArchives $D -ExtraNames @('../external.log')}
    RejectFinal {Write-NoahReprototypeArchives $D -ExtraNames @('trial.apk')}
    [IO.File]::WriteAllBytes((Join-Path $D 'noah-rig.apk'),[byte[]]@(1,2,3))
    $Build=[ordered]@{scope='experimental-noah-only-not-release';mode='rig';package='com.mofumachi.merge.qa.townlife';gitRevision=('a'*40);productFingerprint=('b'*64);sha256=(Get-FileHash -LiteralPath (Join-Path $D 'noah-rig.apk') -Algorithm SHA256).Hash.ToLowerInvariant();apk='noah-rig.apk';full_qa_verified=$false;pixel_verified=$false;adopted=$false;native_capture_method_verified=$true}
    $Meta=Join-Path $D 'noah-rig-build.json';$Build | ConvertTo-Json | Set-Content -LiteralPath $Meta
    $R=Read-NoahTrialBuild $D 'rig';if($R.sha256 -ne $Build.sha256){throw 'Expected APK identity lost.'}
    [IO.File]::WriteAllBytes((Join-Path $D 'noah-current.apk'),[byte[]]@(4,5,6));$Current=$Build | ConvertTo-Json | ConvertFrom-Json;$Current.mode='current';$Current.apk='noah-current.apk';$Current.sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $D 'noah-current.apk')).Hash.ToLowerInvariant();$Current | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $D 'noah-current-build.json')
    $null=Read-NoahTrialPair $D;$Current.gitRevision=('d'*40);$Current | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $D 'noah-current-build.json');RejectFinal {Read-NoahTrialPair $D}
    Assert-NoahInstalledMode '100.5 123 123 I Unity: MOFUMACHI_NOAH_TRIAL_MODE rig' 'rig' 100
    RejectFinal {Assert-NoahInstalledMode '99.5 123 123 I Unity: MOFUMACHI_NOAH_TRIAL_MODE rig' 'rig' 100}
    RejectFinal {Assert-NoahInstalledMode '100.5 123 123 I Unity: MOFUMACHI_NOAH_TRIAL_MODE current' 'rig' 100}
    RejectFinal {Assert-NoahInstalledMode '100.5 no renderer proof' 'rig' 100}
    foreach($Field in @('sha256','package','mode','apk','adopted','native_capture_method_verified','full_qa_verified','productFingerprint')){
        $Bad=$Build | ConvertTo-Json | ConvertFrom-Json
        if($Field -eq 'adopted' -or $Field -eq 'full_qa_verified'){$Bad.$Field=$true}elseif($Field -eq 'native_capture_method_verified'){$Bad.$Field=$false}else{$Bad.$Field='bad'}
        $Bad | ConvertTo-Json | Set-Content -LiteralPath $Meta;RejectFinal {Read-NoahTrialBuild $D 'rig'}
    }
    $Build | ConvertTo-Json | Set-Content -LiteralPath $Meta
    [IO.File]::WriteAllBytes((Join-Path $D 'noah-rig.apk'),[byte[]]@(1,2,4));RejectFinal {Read-NoahTrialBuild $D 'rig'}
    $script:Calls=0
    function Invoke-CheckedNative($Exe,$NativeArguments){$script:Calls++;if($script:Calls -eq 1){return 'launcher'};return 'SurfaceView[com.mofumachi.merge.qa.townlife/com.unity3d.player.UnityPlayerActivity]#0'}
    function Start-Sleep {param($Seconds)}
    $Layer=Wait-NoahUnitySurface 'adb' 'SERIAL' -Attempts 3
    if($script:Calls -ne 2 -or $Layer -notmatch 'SurfaceView'){throw 'App foreground readiness not awaited.'}
    function Invoke-CheckedNative($Exe,$NativeArguments){return 'launcher'}
    RejectFinal {Wait-NoahUnitySurface 'adb' 'SERIAL' -Attempts 2}
    Remove-Item Function:Invoke-CheckedNative,Function:Start-Sleep
    Write-Host 'Final Noah helper: exact method, APK identity/mode failures, delayed/missing Unity surface covered. No device contacted.'
}finally{Remove-Item -LiteralPath $D -Recurse -Force}
