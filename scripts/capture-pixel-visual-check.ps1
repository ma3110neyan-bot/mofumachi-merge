[CmdletBinding()]
param(
    [ValidateSet('Before','After')][string]$Label='After',
    [ValidateRange(5,60)][int]$Seconds=30,
    [string]$OutputDirectory,
    [string]$Serial='99RAY1BELH',
    [string]$Layer,
    [string]$Adb="$env:ProgramFiles\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
)
$ErrorActionPreference='Stop'
function Read-PresentTimes([string]$Raw) {
    foreach($Line in ($Raw -split "`r?`n")) {
        if($Line -match '^\s*\d+\s+(\d+)\s+\d+\s*$') {
            $Time=0L
            if([long]::TryParse($Matches[1],[ref]$Time) -and $Time -gt 0 -and $Time -lt [long]::MaxValue){$Time}
        }
    }
}
function Measure-PresentFrames([long[]]$Frames,[double]$SampleSeconds) {
    $Sorted=@($Frames | Sort-Object -Unique)
    if($Sorted.Count -lt 60){return @{complete=$false;reason='Too few actual surface frames';frame_count=$Sorted.Count}}
    $Span=($Sorted[-1]-$Sorted[0])/1e9
    if($Span -lt 3 -or $Span -lt $SampleSeconds*.8){return @{complete=$false;reason='Stalled or incomplete presentation interval';frame_count=$Sorted.Count}}
    $Intervals=@(for($i=1;$i -lt $Sorted.Count;$i++){($Sorted[$i]-$Sorted[$i-1])/1e6}) | Sort-Object
    return @{complete=$true;frame_count=$Sorted.Count;frame_span_seconds=$Span;presented_fps=($Sorted.Count-1)/$Span;p95_present_interval_ms=$Intervals[[Math]::Ceiling($Intervals.Count*.95)-1]}
}
function Read-TotalPss([string]$Raw) {
    if($Raw -match 'TOTAL PSS:\s*(\d+)'){return [long]$Matches[1]}
    return $null
}
if($MyInvocation.InvocationName -eq '.'){return}
. (Join-Path $PSScriptRoot 'install-pixel3a.ps1') -Adb $Adb -Serial $Serial
if(-not(Test-Path -LiteralPath $Adb -PathType Leaf)){throw 'ADB missing. Pass -Adb with the Unity Android SDK adb.exe.'}
$Serial=Select-AuthorizedDevice (Invoke-CheckedNative $Adb @('devices')) $Serial
$Package='com.mofumachi.merge.qa.townlife'
$Layers=@((Invoke-CheckedNative $Adb @('-s',$Serial,'shell','dumpsys','SurfaceFlinger','--list')) -split "`r?`n" | Where-Object {$_ -match 'SurfaceView' -and $_.Contains($Package)})
if(-not $Layer) {
    if($Layers.Count -ne 1){throw 'Open the town QA in the foreground. A unique Unity SurfaceView is required; no Android window FPS substitute is used.'}
    $Layer=$Layers[0].Trim()
}
if($Layer -notmatch '^[A-Za-z0-9 ._:/#@+\[\]()=-]+$' -or -not $Layer.Contains($Package) -or $Layer -notmatch 'SurfaceView'){throw 'Unexpected surface name. Select the town QA Unity SurfaceView.'}
$QuotedLayer='"'+$Layer+'"'
if(-not $OutputDirectory){$OutputDirectory=Join-Path $PSScriptRoot ('../Builds/PixelVisualLock/'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+$Label)}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Choose a fresh evidence directory. Existing Before/After records are never overwritten.'}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory=(Resolve-Path -LiteralPath $OutputDirectory).Path
$Installed=Invoke-CheckedNative $Adb @('-s',$Serial,'shell','pm','path',$Package)
$ApkLine=@($Installed -split "`r?`n" | Where-Object {$_ -match '^package:/data/app/.*/base\.apk$'})
if($ApkLine.Count -ne 1){throw 'Cannot identify the installed QA APK.'}
$InstalledPath=$ApkLine[0].Substring(8).Trim()
if($InstalledPath -notmatch '^/data/app/[A-Za-z0-9_./=~+!-]+\.apk$'){throw 'Unexpected installed APK path.'}
$HashOutput=Invoke-CheckedNative $Adb @('-s',$Serial,'shell','sha256sum',('"'+$InstalledPath+'"'))
if($HashOutput -notmatch '^([a-fA-F0-9]{64})\s'){throw 'Installed APK hash could not be read; evidence has no reliable identity.'}
$ApkHash=$Matches[1].ToLowerInvariant()
$Initial=Invoke-CheckedNative $Adb @('-s',$Serial,'shell','dumpsys','SurfaceFlinger','--latency',$QuotedLayer)
$Seen=New-Object 'System.Collections.Generic.HashSet[long]'
foreach($Time in @(Read-PresentTimes $Initial)){$null=$Seen.Add($Time)}
$Frames=New-Object 'System.Collections.Generic.List[long]'
$Clock=[Diagnostics.Stopwatch]::StartNew();$Index=0
Write-Host "Recording $Label for $Seconds seconds. Repeat the same town route or Merge actions used for the other build. Keep the app in the foreground."
while($Clock.Elapsed.TotalSeconds -lt $Seconds) {
    Start-Sleep -Milliseconds 1000
    $Raw=Invoke-CheckedNative $Adb @('-s',$Serial,'shell','dumpsys','SurfaceFlinger','--latency',$QuotedLayer)
    [IO.File]::WriteAllText((Join-Path $OutputDirectory ('surface-{0:D2}.txt' -f $Index++)),$Raw)
    foreach($Time in @(Read-PresentTimes $Raw)){if($Seen.Add($Time)){$Frames.Add($Time)}}
}
$Clock.Stop()
$Memory=Invoke-CheckedNative $Adb @('-s',$Serial,'shell','dumpsys','meminfo',$Package)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'meminfo.txt'),$Memory)
$Thermal=Invoke-CheckedNative $Adb @('-s',$Serial,'shell','dumpsys','thermalservice')
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'thermal.txt'),$Thermal)
$DeviceImage='/sdcard/mofu-qa-'+[guid]::NewGuid().ToString('N')+'.png'
try {
    Invoke-CheckedNative $Adb @('-s',$Serial,'shell','screencap','-p',$DeviceImage) | Out-Null
    Invoke-CheckedNative $Adb @('-s',$Serial,'pull',$DeviceImage,(Join-Path $OutputDirectory 'pixel.png')) | Out-Null
} finally {Invoke-CheckedNative $Adb @('-s',$Serial,'shell','rm',$DeviceImage) | Out-Null}
$Metrics=Measure-PresentFrames $Frames.ToArray() $Clock.Elapsed.TotalSeconds
$Pss=Read-TotalPss $Memory
$Report=[ordered]@{label=$Label;serial=$Serial;package=$Package;installed_apk_sha256=$ApkHash;surface=$Layer;sample_seconds=$Clock.Elapsed.TotalSeconds;source='Unity SurfaceView actual presentation timestamps; not Android window gfxinfo';frames=$Metrics;total_pss_kb=$Pss;measurements_complete=([bool]$Metrics.complete -and $null -ne $Pss);pixel_png_sha256=(Get-FileHash -LiteralPath (Join-Path $OutputDirectory 'pixel.png') -Algorithm SHA256).Hash.ToLowerInvariant();visual_accepted=$false;input_accepted=$false;release_accepted=$false}
$Report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'pixel-evidence.json') -Encoding UTF8
Write-Host "Pixel evidence: $OutputDirectory. Captured measurement is not an acceptance decision. Saves/app data were not cleared or replaced."
if(-not $Report.measurements_complete){throw 'Performance evidence incomplete. Keep adoption pending; inspect raw SurfaceFlinger data instead of substituting window FPS.'}
