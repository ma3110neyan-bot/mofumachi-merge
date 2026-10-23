[CmdletBinding()]
param([string]$RunDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'build-android.ps1')
$Project=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Runs=Join-Path $Project 'Builds/FiveAreaReview'
if(-not $RunDirectory) {
    $Latest=Get-ChildItem -LiteralPath $Runs -Directory | Where-Object {$_.Name -match '^\d{8}-\d{6}-[a-f0-9]{8}$'} | Sort-Object Name -Descending | Select-Object -First 1
    if(-not $Latest){throw 'No five-area review run exists. Run -FiveAreaReview first.'}
    $RunDirectory=$Latest.FullName
}
$RunDirectory=(Resolve-Path -LiteralPath $RunDirectory).Path
if([IO.Path]::GetFullPath((Split-Path -Parent $RunDirectory)).TrimEnd('/','\') -ne [IO.Path]::GetFullPath($Runs).TrimEnd('/','\')){throw 'Select a direct run folder inside Builds/FiveAreaReview.'}
$Output=Join-Path $RunDirectory ('repacked/'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
$null=Write-FiveAreaReviewArchives $Project $RunDirectory $Output
if($env:OS -eq 'Windows_NT'){Start-Process explorer.exe -ArgumentList @($Output)}
