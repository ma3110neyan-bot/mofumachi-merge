[CmdletBinding()]
param([string]$BuildDirectory,[ValidateSet('current','rig')][string]$Mode='rig',[ValidateRange(30,600)][int]$Seconds=600,[string]$Serial='99RAY1BELH',[string]$Adb="$env:ProgramFiles\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe")
$ErrorActionPreference='Stop'
function Read-NoahTrialBuild([string]$Directory,[string]$RequestedMode) {
    if($RequestedMode -notin @('current','rig')){throw 'Choose current or rig.'}
    $Info=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory "noah-$RequestedMode-build.json") | ConvertFrom-Json
    if($Info.scope -ne 'experimental-noah-only-not-release' -or $Info.mode -ne $RequestedMode -or $Info.package -ne 'com.mofumachi.merge.qa.townlife' -or $Info.apk -ne "noah-$RequestedMode.apk" -or $Info.gitRevision -notmatch '^[a-f0-9]{40}$' -or $Info.productFingerprint -notmatch '^[a-f0-9]{64}$' -or $Info.sha256 -notmatch '^[a-f0-9]{64}$' -or $Info.native_capture_method_verified -isnot [bool] -or -not $Info.native_capture_method_verified){throw 'Wrong experimental source/mode/package identity.'}
    foreach($Key in @('full_qa_verified','pixel_verified','adopted')){if($Info.$Key -isnot [bool] -or $Info.$Key){throw 'Trial build cannot claim full QA,device or adoption.'}}
    $Apk=Join-Path $Directory $Info.apk
    if(-not(Test-Path -LiteralPath $Apk -PathType Leaf) -or (Get-FileHash -Algorithm SHA256 -LiteralPath $Apk).Hash -ne $Info.sha256){throw 'Trial APK bytes do not match its identity.'}
    return $Info
}
function Wait-NoahUnitySurface([string]$AdbPath,[string]$DeviceSerial,[int]$Attempts=30) {
    for($I=0;$I -lt $Attempts;$I++){
        $Layers=@((Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'shell','dumpsys','SurfaceFlinger','--list')) -split "`r?`n" | Where-Object {$_ -match 'SurfaceView' -and $_.Contains('com.mofumachi.merge.qa.townlife')})
        if($Layers.Count -eq 1){return $Layers[0].Trim()}
        if($I%5 -eq 0){Write-Host 'Waiting for town QA Unity surface. Keep the app unlocked and in the foreground.'};Start-Sleep -Seconds 1
    }
    throw 'No unique town QA Unity surface. No Android window FPS substitute was used.'
}
function Read-NoahTrialPair([string]$Directory) {
    $Current=Read-NoahTrialBuild $Directory 'current';$Rig=Read-NoahTrialBuild $Directory 'rig'
    if($Current.gitRevision -ne $Rig.gitRevision -or $Current.productFingerprint -ne $Rig.productFingerprint -or $Current.sha256 -eq $Rig.sha256){throw 'Current and rig APKs must be distinct builds of the same tested source.'}
    return @{current=$Current;rig=$Rig}
}
function Assert-NoahInstalledMode([string]$Raw,[string]$Expected,[long]$Since) {
    $Lines=@($Raw -split "`r?`n" | Where-Object {$_ -match '^\s*(\d+\.\d+)\s' -and [double]$Matches[1] -ge $Since -and $_ -match 'MOFUMACHI_NOAH_TRIAL_MODE (current|rig)\b'})
    if($Lines.Count -lt 1 -or $Lines[-1] -notmatch ('MOFUMACHI_NOAH_TRIAL_MODE '+[regex]::Escape($Expected)+'\b')){throw 'The running Noah renderer mode is missing or differs from the APK label. Stop before attributing measurements.'}
}
if($MyInvocation.InvocationName -eq '.'){return}
if(-not $BuildDirectory){throw 'Use the exact directory printed by capture-noah-final-rig.ps1 -BuildApks.'}
$BuildDirectory=(Resolve-Path -LiteralPath $BuildDirectory).Path;$Pair=Read-NoahTrialPair $BuildDirectory;$Info=$Pair[$Mode]
. (Join-Path $PSScriptRoot 'install-pixel3a.ps1') -Adb $Adb -Serial $Serial
$Serial=Select-AuthorizedDevice (Invoke-CheckedNative $Adb @('devices')) $Serial
$Model=(Invoke-CheckedNative $Adb @('-s',$Serial,'shell','getprop','ro.product.model')).Trim()
if($Model -ne 'Pixel 3a'){throw 'This trial requires the Pixel 3a, not an emulator or a different phone.'}
$BootTime=(Invoke-CheckedNative $Adb @('-s',$Serial,'shell','date','+%s')).Trim();if($BootTime -notmatch '^\d+$'){throw 'Device time unavailable for renderer-mode proof.'}
$Sdk=Split-Path (Split-Path $Adb -Parent) -Parent;$Aapt=Join-Path $Sdk 'build-tools/36.0.0/aapt2.exe'
Invoke-PixelInstall (Join-Path $BuildDirectory $Info.apk) $Adb $Serial $Info.package $Aapt -ExpectedSha256 $Info.sha256
$Layer=Wait-NoahUnitySurface $Adb $Serial
Write-Host 'On Pixel choose Noah as the character. Keep the same save, area, camera and route for both APKs. Walk left/right, toward/away, stop, tap Noah and greet Momo.'
$Ready=Read-Host 'When Noah and the town are visible, press Enter to begin measurement'
$Layer=Wait-NoahUnitySurface $Adb $Serial
$Output=Join-Path $BuildDirectory ('pixel-'+$Mode+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss'));New-Item -ItemType Directory -Path $Output | Out-Null
$ModeLog=Invoke-CheckedNative $Adb @('-s',$Serial,'logcat','-d','-v','epoch','-s','Unity');[IO.File]::WriteAllText((Join-Path $Output 'renderer-mode-log.txt'),$ModeLog)
Assert-NoahInstalledMode $ModeLog $Mode ([long]$BootTime)
$Capture=Join-Path $Output 'surface-capture';$Label=if($Mode -eq 'current'){'Before'}else{'After'}
$DeviceTime=(Invoke-CheckedNative $Adb @('-s',$Serial,'shell','date','+%s')).Trim();if($DeviceTime -notmatch '^\d+$'){throw 'Device time unavailable for crash correlation.'}
$MemoryStart=Invoke-CheckedNative $Adb @('-s',$Serial,'shell','dumpsys','meminfo',$Info.package);[IO.File]::WriteAllText((Join-Path $Output 'meminfo-start.txt'),$MemoryStart)
$PssStart=if($MemoryStart -match 'TOTAL PSS:\s*(\d+)'){[long]$Matches[1]}else{$null}
$PidStart=(Invoke-CheckedNative $Adb @('-s',$Serial,'shell','pidof',$Info.package)).Trim()
$ErrorText='';$PidEnd=$null;$CrashStatus='unknown';$Crashes=@();$VideoError=''
try {& (Join-Path $PSScriptRoot 'capture-pixel-visual-check.ps1') -Label $Label -Seconds $Seconds -Serial $Serial -Adb $Adb -Layer $Layer -OutputDirectory $Capture}
catch{$ErrorText=$_.Exception.Message;Write-Host "Measurement incomplete: $ErrorText"}
$DeviceVideo='/sdcard/mofu-noah-final-'+[guid]::NewGuid().ToString('N')+'.mp4'
try {
    $RecordReady=Read-Host 'The FPS interval is finished. Press Enter for a separate20-second clip, then walk/turn/stop/tap/greet Noah on Pixel'
    Invoke-CheckedNative $Adb @('-s',$Serial,'shell','screenrecord','--bit-rate','2000000','--time-limit','20',$DeviceVideo) | Out-Null
    Invoke-CheckedNative $Adb @('-s',$Serial,'pull',$DeviceVideo,(Join-Path $Output 'noah-actions.mp4')) | Out-Null
}catch{$VideoError=$_.Exception.Message}
finally{try{Invoke-CheckedNative $Adb @('-s',$Serial,'shell','rm',$DeviceVideo) | Out-Null}catch{Write-Warning 'Could not remove this capture temporary video from /sdcard.'}}
try {
    $Raw=Invoke-CheckedNative $Adb @('-s',$Serial,'logcat','-b','crash','-d','-v','epoch','-t','2000');[IO.File]::WriteAllText((Join-Path $Output 'crash-buffer.txt'),$Raw)
    $InWindow=@($Raw -split "`r?`n" | Where-Object {$_ -match '^\s*(\d+\.\d+)\s' -and [double]$Matches[1] -ge [double]$DeviceTime})
    [IO.File]::WriteAllLines((Join-Path $Output 'crash-window.txt'),[string[]]$InWindow);$Crashes=@($InWindow | Where-Object {$_ -match 'FATAL EXCEPTION|Fatal signal|>>> com\.mofumachi\.merge\.qa\.townlife <<<|Process: com\.mofumachi\.merge\.qa\.townlife'})
    $CrashStatus=if($Crashes.Count){'crash_buffer_events_require_review'}else{'no_crash_events_in_read_buffer'}
}catch{$CrashStatus='unavailable';[IO.File]::WriteAllText((Join-Path $Output 'crash-read-error.txt'),$_.Exception.Message)}
try{$PidEnd=(Invoke-CheckedNative $Adb @('-s',$Serial,'shell','pidof',$Info.package)).Trim()}catch{[IO.File]::WriteAllText((Join-Path $Output 'process-end-error.txt'),$_.Exception.Message)}
$Stutter=Read-Host 'Stutter while walking/turning? Enter none / observed / unknown'
$Tap=Read-Host 'Tap response on Noah and town? Enter responsive / delayed / unknown'
$Heat=Read-Host 'Heat compared with the current build? Enter similar / hotter / unknown'
$Report=[ordered]@{scope='experimental-noah-only-not-release';mode=$Mode;gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;expected_apk_sha256=$Info.sha256;serial=$Serial;model=$Model;package=$Info.package;device_start_epoch=$DeviceTime;total_pss_start_kb=$PssStart;pid_start=$PidStart;pid_end=$PidEnd;same_process=($null -ne $PidEnd -and $PidStart -eq $PidEnd);crash_buffer_status=$CrashStatus;crash_event_lines=$Crashes.Count;manual_stutter=$Stutter;manual_tap_response=$Tap;manual_heat=$Heat;tap_latency_ms=$null;tap_latency_note='Manual interaction report only; no instrumented input-to-frame latency measurement.';video_error=$VideoError;video_scope='Separate20-second device clip AFTER performance sampling; encoder is not running during FPS/thermal/PSS interval.';measurement_error=$ErrorText;full_qa_verified=$false;visual_accepted=$false;adopted=$false;six_character_rollout_authorized=$false}
$Metrics=Join-Path $Capture 'pixel-evidence.json'
if(Test-Path -LiteralPath $Metrics){$M=Get-Content -Raw -Encoding UTF8 -LiteralPath $Metrics | ConvertFrom-Json;if($M.installed_apk_sha256 -ne $Info.sha256){$Report.measurement_error='Installed APK changed during capture. Discard attribution.'};$Report.surface_metrics=$M}
$Report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $Output 'noah-pixel-result.json') -Encoding UTF8
$Zip=$Output+'.zip';Compress-Archive -Path (Join-Path $Output '*') -DestinationPath $Zip
Write-Host "Noah Pixel result ZIP: $Zip. Measured data and manual feedback are not adoption. Saves/app data were not cleared."
if($Report.measurement_error){throw $Report.measurement_error}
