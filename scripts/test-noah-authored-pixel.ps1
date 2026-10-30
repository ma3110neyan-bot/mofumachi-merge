[CmdletBinding()]
param([string]$BuildDirectory,[ValidateSet('current','authored')][string]$Mode='authored',[ValidateRange(0,600)][int]$Seconds=0,[string]$Serial='99RAY1BELH',[string]$Adb="$env:ProgramFiles\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe")
$ErrorActionPreference='Stop'
$AuthoredDirectory=$BuildDirectory;$AuthoredMode=$Mode;$AuthoredSeconds=$Seconds;$AuthoredSerial=$Serial;$AuthoredAdb=$Adb
. (Join-Path $PSScriptRoot 'prepare-noah-pixel-preview.ps1')
. (Join-Path $PSScriptRoot 'install-pixel3a.ps1') -Adb $AuthoredAdb -Serial $AuthoredSerial
function Assert-NoahAuthoredMode([string]$Raw,[string]$Expected,[long]$Since) {
    $Last=''
    foreach($Line in ($Raw -split "`r?`n")){
        if($Line -match '^\s*(\d+\.\d+)\s'){$When=[double]$Matches[1];if($When -ge $Since -and $Line -match 'MOFUMACHI_NOAH_AUTHORED_MODE (current|authored)\b'){$Last=$Matches[1]}}
    }
    if($Last -ne $Expected){throw 'Fresh running Noah mode is absent/wrong. Do not attribute this screen or measurements to the new preview.'}
}
function Wait-NoahAuthoredSurface([string]$AdbPath,[string]$DeviceSerial,[string]$EvidenceDirectory) {
    for($Attempt=0;$Attempt -lt 20;$Attempt++){
        $Raw=Invoke-CheckedNative $AdbPath @('-s',$DeviceSerial,'shell','dumpsys','SurfaceFlinger','--list')
        [IO.File]::WriteAllText((Join-Path $EvidenceDirectory ('surface-{0:D2}.txt' -f $Attempt)),$Raw)
        $Layers=@($Raw -split "`r?`n" | Where-Object {$_ -match 'SurfaceView' -and $_.Contains('com.mofumachi.merge.qa.townlife')})
        if($Layers.Count -eq 1){return $Layers[0].Trim()};Start-Sleep -Seconds 1
    }
    throw 'No unique town Unity SurfaceView; do not use Android window FPS instead.'
}
function Invoke-NoahAuthoredPixel([string]$AuthoredDirectory,[string]$AuthoredMode,[int]$AuthoredSeconds,[string]$AuthoredSerial,[string]$AuthoredAdb) {
if(-not $AuthoredDirectory){throw 'Specify the exact completed noah-authored run using -BuildDirectory.'}
$AuthoredDirectory=(Resolve-Path -LiteralPath $AuthoredDirectory).Path;$Pair=Read-NoahAuthoredPair $AuthoredDirectory;$Info=$Pair[$AuthoredMode]
if($AuthoredSeconds -gt 0 -and $AuthoredSeconds -lt 5){throw 'Choose0for visual preview or5..600seconds for metrics.'}
$Output=Join-Path $AuthoredDirectory ('pixel-'+$AuthoredMode+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory -Path $Output | Out-Null
$Record=[ordered]@{scope='experimental-noah-authored-not-release';mode=$AuthoredMode;gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;input_sha256=$Info.input_sha256;expected_apk_sha256=$Info.sha256;serial=$AuthoredSerial;installation_verified=$false;renderer_mode_verified=$false;pixel_verified=$false;visual_accepted=$false;adopted=$false;surface_metrics=$null;total_pss_start_kb=$null;total_pss_end_kb=$null;pid_start=$null;pid_end=$null;same_process=$null;manual_stutter='unknown';manual_tap='unknown';manual_heat='unknown';error=''};$Failure=$null;$Since=$null
try{
    $AuthoredSerial=Select-AuthorizedDevice (Invoke-CheckedNative $AuthoredAdb @('devices')) $AuthoredSerial;$Record.serial=$AuthoredSerial
    $Model=(Invoke-CheckedNative $AuthoredAdb @('-s',$AuthoredSerial,'shell','getprop','ro.product.model')).Trim();if($Model -ne 'Pixel 3a'){throw 'Select Pixel 3a for this preview.'}
    $Since=(Invoke-CheckedNative $AuthoredAdb @('-s',$AuthoredSerial,'shell','date','+%s')).Trim();if($Since -notmatch '^\d+$'){throw 'Device time unavailable.'}
    $Sdk=Split-Path (Split-Path $AuthoredAdb -Parent) -Parent;$Aapt=Join-Path $Sdk 'build-tools/36.0.0/aapt2.exe'
    Invoke-PixelInstall (Join-Path $AuthoredDirectory $Info.apk) $AuthoredAdb $AuthoredSerial $Info.package $Aapt -ExpectedSha256 $Info.sha256
    $Record.installation_verified=$true
    Write-Host 'On Pixel: open town, select Noah with the existing character controls, then walk left/right/away/toward. Existing save is preserved.'
    Write-Host 'This is unapproved art: front independent rig and new-direction expressions/blink remain unfinished.'
    $Ready=Read-Host 'When Noah and town are visible, press Enter here'
    $Layer=Wait-NoahAuthoredSurface $AuthoredAdb $AuthoredSerial $Output
    $Log=Invoke-CheckedNative $AuthoredAdb @('-s',$AuthoredSerial,'logcat','-d','-v','epoch','-s','Unity');[IO.File]::WriteAllText((Join-Path $Output 'renderer-mode.txt'),$Log)
    Assert-NoahAuthoredMode $Log $AuthoredMode ([long]$Since);$Record.renderer_mode_verified=$true
    $Memory=Invoke-CheckedNative $AuthoredAdb @('-s',$AuthoredSerial,'shell','dumpsys','meminfo',$Info.package);[IO.File]::WriteAllText((Join-Path $Output 'memory-start.txt'),$Memory)
    if($Memory -match 'TOTAL PSS:\s*(\d+)'){$Record.total_pss_start_kb=[long]$Matches[1]}
    $ProcessIdText=(Invoke-CheckedNative $AuthoredAdb @('-s',$AuthoredSerial,'shell','pidof',$Info.package)).Trim();if($ProcessIdText -match '^\d+$'){$Record.pid_start=$ProcessIdText}
    if($AuthoredSeconds -gt 0){
        $Capture=Join-Path $Output 'surface-capture';$Label=if($AuthoredMode -eq 'current'){'Before'}else{'After'}
        & (Join-Path $PSScriptRoot 'capture-pixel-visual-check.ps1') -Label $Label -Seconds $AuthoredSeconds -Serial $AuthoredSerial -Adb $AuthoredAdb -Layer $Layer -OutputDirectory $Capture
        $Metrics=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Capture 'pixel-evidence.json') | ConvertFrom-Json
        if($Metrics.installed_apk_sha256 -ne $Info.sha256){throw 'Installed APK changed during measurement.'};$Record.surface_metrics=$Metrics
        $Record.manual_stutter=Read-Host 'Stutter: none / observed / unknown';$Record.manual_tap=Read-Host 'Taps: responsive / delayed / unknown';$Record.manual_heat=Read-Host 'Heat: similar / hotter / unknown'
    }
}catch{$Failure=$_;$Record.error=$_.Exception.Message}
finally{
    if($null -ne $Since){
        try{$Raw=Invoke-CheckedNative $AuthoredAdb @('-s',$AuthoredSerial,'logcat','-b','crash','-d','-v','epoch','-t','2000');[IO.File]::WriteAllText((Join-Path $Output 'crash-buffer.txt'),$Raw)}catch{[IO.File]::WriteAllText((Join-Path $Output 'crash-read-error.txt'),$_.Exception.Message)}
        try{$Memory=Invoke-CheckedNative $AuthoredAdb @('-s',$AuthoredSerial,'shell','dumpsys','meminfo',$Info.package);[IO.File]::WriteAllText((Join-Path $Output 'memory-end.txt'),$Memory);if($Memory -match 'TOTAL PSS:\s*(\d+)'){$Record.total_pss_end_kb=[long]$Matches[1]}}catch{}
        try{$ProcessIdText=(Invoke-CheckedNative $AuthoredAdb @('-s',$AuthoredSerial,'shell','pidof',$Info.package)).Trim();if($ProcessIdText -match '^\d+$'){$Record.pid_end=$ProcessIdText};if($null -ne $Record.pid_start -and $null -ne $Record.pid_end){$Record.same_process=$Record.pid_start -eq $Record.pid_end}}catch{}
    }
    $Record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $Output 'noah-authored-pixel.json') -Encoding UTF8
    try{Compress-Archive -Path (Join-Path $Output '*') -DestinationPath ($Output+'.zip');Write-Host ('Pixel preview result: '+$Output+'.zip')}
    catch{if($null -eq $Failure){$Failure=$_};$Record.error+=' Archive: '+$_.Exception.Message;$Record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $Output 'noah-authored-pixel.json') -Encoding UTF8;Write-Host "Archive failed; original records remain: $Output"}
}
if($Failure){throw $Failure}
Write-Host 'Preview installed and mode identified; user visual acceptance and performance remain pending. No save reset or release adoption.'
return $Output
}
if($MyInvocation.InvocationName -eq '.'){return}
Invoke-NoahAuthoredPixel $AuthoredDirectory $AuthoredMode $AuthoredSeconds $AuthoredSerial $AuthoredAdb
