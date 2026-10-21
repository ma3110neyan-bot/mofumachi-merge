$ErrorActionPreference='Stop'
$Script=Join-Path $PSScriptRoot '../capture-pixel-visual-check.ps1'
if(-not(Test-Path -LiteralPath $Script)){throw 'Pixel comparison capture helper is missing.'}
. $Script
$Raw="16666666`n0 0 0`n100 1000000000 100`n100 1033333333 100`n100 9223372036854775807 100`n"
$Times=@(Read-PresentTimes $Raw)
if($Times.Count -ne 2){throw 'Invalid SurfaceFlinger timestamps accepted.'}
$Frames=@(0..899 | ForEach-Object {1000000000L+33333333L*$_})
$Metrics=Measure-PresentFrames ($Frames+$Frames) 30
if(-not $Metrics.complete -or [Math]::Abs($Metrics.presented_fps-30) -gt .01){throw 'Duplicate frame timestamps changed FPS.'}
if((Measure-PresentFrames @() 30).complete){throw 'Missing measurements accepted.'}
if((Measure-PresentFrames $Frames 60).complete){throw 'A stalled/incomplete session was accepted.'}
if((Read-TotalPss "App Summary`n TOTAL PSS: 234567 TOTAL RSS: 456789") -ne 234567){throw 'PSS parse failed.'}
if($null -ne (Read-TotalPss 'No process found')){throw 'Missing PSS measurement became zero.'}
$Fixture=Join-Path ([IO.Path]::GetTempPath()) ('mofu-pixel-args-'+[guid]::NewGuid())
New-Item -ItemType Directory -Path $Fixture | Out-Null
try {
    Copy-Item -LiteralPath $Script -Destination (Join-Path $Fixture 'capture.ps1')
    # The installer has a param block with defaults. Dot sourcing it must not
    # silently replace the SDK path and chosen serial passed by the caller.
    'param([string]$Adb="DEFAULT-ADB",[string]$Serial="DEFAULT-SERIAL"); throw ("FORWARDED|"+$Adb+"|"+$Serial)' | Set-Content -LiteralPath (Join-Path $Fixture 'install-pixel3a.ps1')
    $ExpectedAdb=Join-Path $Fixture 'custom sdk adb.exe';$Message=''
    try {& (Join-Path $Fixture 'capture.ps1') -Adb $ExpectedAdb -Serial 'CUSTOM-PIXEL'}catch{$Message=$_.Exception.Message}
    if($Message -ne ('FORWARDED|'+$ExpectedAdb+'|CUSTOM-PIXEL')){throw "Capture dependency replaced caller device arguments: $Message"}
} finally {Remove-Item -LiteralPath $Fixture -Recurse -Force}
Write-Host 'Pixel metrics: invalid/duplicate/missing/stalled samples checked; 30 FPS timing and PSS parsed. No device contacted.'
