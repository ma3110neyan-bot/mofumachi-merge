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
function Wait-NoahUnitySurface([string]$AdbPath,[string]$DeviceSerial,[int]$Attempts=30,[string]$EvidenceDirectory='') {
    for($I=0;$I -lt $Attempts;$I++){
        $Raw=Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'shell','dumpsys','SurfaceFlinger','--list')
        if($EvidenceDirectory){[IO.File]::WriteAllText((Join-Path $EvidenceDirectory ('surface-startup-{0:D2}.txt' -f $I)),$Raw)}
        $Layers=@($Raw -split "`r?`n" | Where-Object {$_ -match 'SurfaceView' -and $_.Contains('com.mofumachi.merge.qa.townlife')})
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
function Invoke-NoahSurfaceCapture([string]$Directory,[string]$DeviceSerial,[string]$AdbPath,[int]$SampleSeconds,[string]$Label,[string]$Layer) {
    & (Join-Path $PSScriptRoot 'capture-pixel-visual-check.ps1') -Label $Label -Seconds $SampleSeconds -Serial $DeviceSerial -Adb $AdbPath -Layer $Layer -OutputDirectory $Directory
}
function Invoke-NoahPixelTrial([string]$Directory,$Info,[string]$TrialMode,[string]$DeviceSerial,[string]$AdbPath,[int]$SampleSeconds) {
    # Create the nullable record BEFORE touching the app: launch failures are
    # evidence, too. Main has already verified the paired APK identities.
    $Output=Join-Path $Directory ('pixel-'+$TrialMode+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
    New-Item -ItemType Directory -Path $Output | Out-Null
    $Capture=Join-Path $Output 'surface-capture';$Failure=$null;$DeviceVerified=$false;$BootTime=$null
    $Report=[ordered]@{scope='experimental-noah-only-not-release';mode=$TrialMode;gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;expected_apk_sha256=$Info.sha256;serial=$DeviceSerial;model=$null;package=$Info.package;phase='startup';device_start_epoch=$null;total_pss_start_kb=$null;total_pss_after_session_kb=$null;pid_start=$null;pid_end=$null;same_process=$null;renderer_mode_verified=$false;surface_metrics=$null;crash_buffer_status='unknown';crash_event_lines=$null;manual_stutter='unknown';manual_tap_response='unknown';manual_heat='unknown';tap_latency_ms=$null;tap_latency_note='Manual interaction only, not instrumented input-to-frame latency.';video_error=$null;video_scope='Separate20-second clip AFTER performance interval; no encoder during FPS/thermal/PSS sampling.';measurement_error='';full_qa_verified=$false;visual_accepted=$false;adopted=$false;six_character_rollout_authorized=$false}
    try {
        $DeviceSerial=Select-AuthorizedDevice (Invoke-CheckedNative $AdbPath @('devices')) $DeviceSerial;$Report.serial=$DeviceSerial
        $Report.model=(Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'shell','getprop','ro.product.model')).Trim()
        if($Report.model -ne 'Pixel 3a'){throw 'This trial requires Pixel 3a.'};$DeviceVerified=$true
        $BootTime=(Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'shell','date','+%s')).Trim();if($BootTime -notmatch '^\d+$'){$BootTime=$null;throw 'Device time unavailable.'};$Report.device_start_epoch=$BootTime
        $Sdk=Split-Path (Split-Path $AdbPath -Parent) -Parent;$Aapt=Join-Path $Sdk 'build-tools/36.0.0/aapt2.exe';$Report.phase='install-and-launch'
        Invoke-PixelInstall (Join-Path $Directory $Info.apk) $AdbPath $DeviceSerial $Info.package $Aapt -ExpectedSha256 $Info.sha256
        $Report.phase='surface';$Layer=Wait-NoahUnitySurface $AdbPath $DeviceSerial -EvidenceDirectory $Output
        Write-Host 'Choose Noah in town. Use the same save, area and route in both APKs: walk,turn,stop,tap and greet Momo.'
        $Ready=Read-Host 'When Noah and town are visible, press Enter'
        $Layer=Wait-NoahUnitySurface $AdbPath $DeviceSerial -EvidenceDirectory $Output
        $Report.phase='renderer-mode';$ModeLog=Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'logcat','-d','-v','epoch','-s','Unity');[IO.File]::WriteAllText((Join-Path $Output 'renderer-mode-log.txt'),$ModeLog)
        Assert-NoahInstalledMode $ModeLog $TrialMode ([long]$BootTime);$Report.renderer_mode_verified=$true
        $Report.phase='starting-process';$Memory=Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'shell','dumpsys','meminfo',$Info.package);[IO.File]::WriteAllText((Join-Path $Output 'meminfo-start.txt'),$Memory)
        if($Memory -match 'TOTAL PSS:\s*(\d+)'){$Report.total_pss_start_kb=[long]$Matches[1]}
        $StartPid=(Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'shell','pidof',$Info.package)).Trim();if($StartPid -notmatch '^\d+$'){throw 'Starting app PID unavailable.'};$Report.pid_start=$StartPid
        $Report.phase='performance';$Label=if($TrialMode -eq 'current'){'Before'}else{'After'}
        Invoke-NoahSurfaceCapture $Capture $DeviceSerial $AdbPath $SampleSeconds $Label $Layer
        $Report.phase='video';$DeviceVideo='/sdcard/mofu-noah-final-'+[guid]::NewGuid().ToString('N')+'.mp4'
        try {
            $Ready=Read-Host 'FPS interval finished. Press Enter for a separate20-second clip, then walk/turn/stop/tap/greet Noah'
            Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'shell','screenrecord','--bit-rate','2000000','--time-limit','20',$DeviceVideo) | Out-Null
            Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'pull',$DeviceVideo,(Join-Path $Output 'noah-actions.mp4')) | Out-Null
        }catch{$Report.video_error=$_.Exception.Message}
        finally{try{Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'shell','rm',$DeviceVideo) | Out-Null}catch{Write-Host 'Capture temporary video could not be removed from /sdcard.'}}
        $Report.manual_stutter=Read-Host 'Stutter? none / observed / unknown';$Report.manual_tap_response=Read-Host 'Taps? responsive / delayed / unknown';$Report.manual_heat=Read-Host 'Heat? similar / hotter / unknown'
        $Report.phase='finished'
    }catch{$Failure=$_;$Report.measurement_error=$_.Exception.Message;[IO.File]::WriteAllText((Join-Path $Output 'trial-error.txt'),$_.Exception.ToString())}
    finally {
        if($DeviceVerified){
            try {
                $Raw=Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'logcat','-b','crash','-d','-v','epoch','-t','2000');[IO.File]::WriteAllText((Join-Path $Output 'crash-buffer.txt'),$Raw)
                if($null -ne $BootTime){
                    $Window=@($Raw -split "`r?`n" | Where-Object {$_ -match '^\s*(\d+\.\d+)\s' -and [double]$Matches[1] -ge [double]$BootTime})
                    [IO.File]::WriteAllLines((Join-Path $Output 'crash-window.txt'),[string[]]$Window)
                    $Events=@($Window | Where-Object {$_ -match 'FATAL EXCEPTION|Fatal signal|>>> com\.mofumachi\.merge\.qa\.townlife <<<|Process: com\.mofumachi\.merge\.qa\.townlife'})
                    $Report.crash_event_lines=$Events.Count;$Report.crash_buffer_status=if($Events.Count){'crash_buffer_events_require_review'}else{'no_crash_events_in_read_buffer'}
                }else{$Report.crash_buffer_status='unknown-start-time'}
            }catch{$Report.crash_buffer_status='unavailable';$Report.crash_event_lines=$null;[IO.File]::WriteAllText((Join-Path $Output 'crash-read-error.txt'),$_.Exception.Message)}
            try{$EndPid=(Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'shell','pidof',$Info.package)).Trim();if($EndPid -match '^\d+$'){$Report.pid_end=$EndPid}}catch{[IO.File]::WriteAllText((Join-Path $Output 'process-end-error.txt'),$_.Exception.Message)}
            if($null -ne $Report.pid_start -and $null -ne $Report.pid_end){$Report.same_process=$Report.pid_start -eq $Report.pid_end}
            try{$Memory=Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'shell','dumpsys','meminfo',$Info.package);[IO.File]::WriteAllText((Join-Path $Output 'meminfo-after-session.txt'),$Memory);if($Memory -match 'TOTAL PSS:\s*(\d+)'){$Report.total_pss_after_session_kb=[long]$Matches[1]}}catch{[IO.File]::WriteAllText((Join-Path $Output 'memory-read-error.txt'),$_.Exception.Message)}
        }
        $Metrics=Join-Path $Capture 'pixel-evidence.json'
        if(Test-Path -LiteralPath $Metrics){try{$M=Get-Content -Raw -Encoding UTF8 -LiteralPath $Metrics | ConvertFrom-Json;$Report.surface_metrics=$M;if($M.installed_apk_sha256 -ne $Info.sha256){$Report.measurement_error+=' Installed APK changed; discard attribution.'}}catch{$Report.measurement_error+=' Cannot parse surface metrics.'}}
        $Report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $Output 'noah-pixel-result.json') -Encoding UTF8
        $Zip=$Output+'.zip';Compress-Archive -Path (Join-Path $Output '*') -DestinationPath $Zip
        Write-Host "Noah Pixel result ZIP: $Zip. Unknown values stay null; no adoption. Saves/app data were not cleared."
    }
    if($Failure){throw $Failure};if($Report.measurement_error){throw $Report.measurement_error}
}
if($MyInvocation.InvocationName -eq '.'){return}
if(-not $BuildDirectory){throw 'Use the exact directory printed by capture-noah-final-rig.ps1 -BuildApks.'}
$BuildDirectory=(Resolve-Path -LiteralPath $BuildDirectory).Path;$Pair=Read-NoahTrialPair $BuildDirectory;$Info=$Pair[$Mode]
. (Join-Path $PSScriptRoot 'install-pixel3a.ps1') -Adb $Adb -Serial $Serial
Invoke-NoahPixelTrial $BuildDirectory $Info $Mode $Serial $Adb $Seconds
