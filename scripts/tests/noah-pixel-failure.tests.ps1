param([ValidateSet('startup','restore')][string]$Case='startup')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../capture-noah-final-rig.ps1')
. (Join-Path $PSScriptRoot '../capture-noah-final-pixel.ps1')
if($Case -eq 'restore'){
    $Message='';$script:Restored=$false
    try{Invoke-NoahTrialPairActions {throw 'original capture failure'} {throw 'rig must not run'} {$script:Restored=$true;throw 'secondary restore failure'}}catch{$Message=$_.Exception.Message}
    if(-not $script:Restored -or $Message -notmatch '^original capture failure$'){throw "Original capture failure was masked: $Message"}
    $Message='';try{Invoke-NoahTrialPairActions {} {} {throw 'restore-only failure'}}catch{$Message=$_.Exception.Message}
    if($Message -notmatch '^restore-only failure$'){throw 'A failed restore was silently accepted.'}
    Write-Host 'Noah restore: original failure retained; independent restore failure still stops.';return
}
$D=Join-Path ([IO.Path]::GetTempPath()) ('mofu-pixel-startup-'+[guid]::NewGuid());New-Item -ItemType Directory -Path $D | Out-Null
try{
    $Info=[pscustomobject]@{mode='rig';gitRevision=('a'*40);productFingerprint=('b'*64);sha256=('c'*64);package='com.mofumachi.merge.qa.townlife';apk='noah-rig.apk'}
    $FakeAdb=Join-Path $D 'sdk/platform-tools/adb.exe'
    function Select-AuthorizedDevice($Raw,$Requested){return $Requested}
    function Invoke-PixelInstall($ApkPath,$AdbPath,$RequestedSerial,$ExpectedPackage,$AaptPath,$ExpectedSha256){if($script:Fault -eq 'launch'){throw 'launch failure'}}
    function Start-Sleep {param($Seconds)}
    function Read-Host {param($Prompt);return 'unknown'}
    function Invoke-CheckedNative($Exe,[string[]]$NativeArguments){
        if('devices' -in $NativeArguments){return "SERIAL`tdevice"}
        if('getprop' -in $NativeArguments){return 'Pixel 3a'}
        if('date' -in $NativeArguments){return '100'}
        if('SurfaceFlinger' -in $NativeArguments){if($script:Fault -eq 'surface'){return 'launcher'};return 'SurfaceView[com.mofumachi.merge.qa.townlife/UnityPlayerActivity]#0'}
        if('meminfo' -in $NativeArguments){if($script:Fault -eq 'success'){return 'TOTAL PSS: 12345'};return 'No process found'}
        if('pidof' -in $NativeArguments){if($script:Fault -eq 'success'){return '123'};throw 'pid failure'}
        if('crash' -in $NativeArguments){if($script:NoCrashBuffer){throw 'crash buffer unavailable'};if($script:Fault -eq 'success'){return 'no crash lines'};return '100.5 123 123 E AndroidRuntime: FATAL EXCEPTION: main'}
        if('Unity' -in $NativeArguments){$M=if($script:Fault -eq 'mode'){'current'}else{'rig'};return "100.5 123 123 I Unity: MOFUMACHI_NOAH_TRIAL_MODE $M"}
        if('screenrecord' -in $NativeArguments -or 'rm' -in $NativeArguments){return ''}
        if('pull' -in $NativeArguments){[IO.File]::WriteAllText($NativeArguments[-1],'synthetic video bytes; not device evidence');return ''}
        throw "Unexpected native call: $NativeArguments"
    }
    foreach($Fault in @('launch','surface','mode','pid')){
        $script:Fault=$Fault;$script:NoCrashBuffer=$false;$Before=@(Get-ChildItem -LiteralPath $D -Filter '*.zip').Count
        $Message='';try{Invoke-NoahPixelTrial $D $Info 'rig' 'SERIAL' $FakeAdb 30}catch{$Message=$_.Exception.Message}
        $Expected=if($Fault -eq 'launch'){'launch failure'}elseif($Fault -eq 'surface'){'No unique town QA Unity surface'}elseif($Fault -eq 'mode'){'renderer mode'}else{'pid failure'}
        if($Message -notmatch [regex]::Escape($Expected)){throw "Fixture did not reach $Fault failure: $Message"}
        $Zips=@(Get-ChildItem -LiteralPath $D -Filter '*.zip');if($Zips.Count -ne $Before+1){throw "Startup $Fault produced no result ZIP: $Message"}
        $Latest=Get-ChildItem -LiteralPath $D -Directory | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
        $R=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Latest.FullName 'noah-pixel-result.json') | ConvertFrom-Json
        if(-not $Message -or -not $R.measurement_error -or $null -ne $R.surface_metrics -or $null -ne $R.same_process -or $R.adopted){throw "Startup $Fault became a measurement/acceptance instead of unknown."}
        if(-not(Test-Path -LiteralPath (Join-Path $Latest.FullName 'crash-buffer.txt'))){throw 'Launch crash evidence lost.'}
    }
    $script:Fault='pid';$script:NoCrashBuffer=$true
    try{Invoke-NoahPixelTrial $D $Info 'rig' 'SERIAL' $FakeAdb 30}catch{}
    $Latest=Get-ChildItem -LiteralPath $D -Directory | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    $R=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Latest.FullName 'noah-pixel-result.json') | ConvertFrom-Json
    if($null -ne $R.crash_event_lines -or $null -ne $R.same_process -or $R.crash_buffer_status -ne 'unavailable'){throw 'Unavailable crash/PID evidence was converted to zero/false.'}
    function Invoke-NoahSurfaceCapture($Directory,$DeviceSerial,$AdbPath,$SampleSeconds,$Label,$Layer){
        New-Item -ItemType Directory -Path $Directory | Out-Null
        @{installed_apk_sha256=('c'*64);measurements_complete=$true;frames=@{presented_fps=30};total_pss_kb=12345} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Directory 'pixel-evidence.json')
    }
    $script:Fault='success';$script:NoCrashBuffer=$false;Invoke-NoahPixelTrial $D $Info 'rig' 'SERIAL' $FakeAdb 30
    $Latest=Get-ChildItem -LiteralPath $D -Directory | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    $R=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Latest.FullName 'noah-pixel-result.json') | ConvertFrom-Json
    if($R.measurement_error -or -not $R.renderer_mode_verified -or $R.surface_metrics.frames.presented_fps -ne 30 -or $R.same_process -ne $true -or $R.crash_event_lines -ne 0 -or $R.total_pss_start_kb -ne 12345 -or $R.adopted){throw 'Success fixture lost verified data or claimed adoption.'}
    Write-Host 'Noah startup failures: launch/surface/mode/PID always archive; unknown outcomes stay null; valid data survives success. Synthetic fixtures only, no device contacted.'
}finally{Remove-Item -LiteralPath $D -Recurse -Force}
