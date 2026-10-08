[CmdletBinding()]
param(
    [string]$UnityEditor = "$env:ProgramFiles\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe"
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) {
    throw "Unity 6000.6.4f1 was not found. Pass -UnityEditor with the full Unity.exe path."
}
$UnityEditor = (Resolve-Path -LiteralPath $UnityEditor).Path
if ((Get-Content (Join-Path $ProjectPath 'ProjectSettings\ProjectVersion.txt') -Raw) -notmatch 'm_EditorVersion: 6000\.6\.4f1\s') {
    throw 'This script requires the project-pinned Unity 6000.6.4f1.'
}
$RunName = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$OutputDir = Join-Path $ProjectPath "Builds\Android\$RunName"
New-Item -ItemType Directory -Path $OutputDir | Out-Null

function Invoke-Unity([string[]]$UnityArguments) {
    # Windows paths cannot contain double quotes. Quote each argument so spaces survive Start-Process.
    $ArgumentText = ($UnityArguments | ForEach-Object { '"' + $_ + '"' }) -join ' '
    $Process = Start-Process -FilePath $UnityEditor -ArgumentList $ArgumentText -Wait -PassThru
    if ($Process.ExitCode -ne 0) {
        throw "Unity exited with code $($Process.ExitCode). Inspect the logs in $OutputDir."
    }
}
function Assert-TestRun([string]$ResultPath, [int]$MinimumTests) {
    if (-not (Test-Path -LiteralPath $ResultPath)) { throw "Test results are missing: $ResultPath" }
    [xml]$Results = Get-Content -LiteralPath $ResultPath -Raw
    $Run = $Results.'test-run'
    if ($null -eq $Run -or [int]$Run.total -lt $MinimumTests -or $Run.result -ne 'Passed' -or [int]$Run.failed -ne 0 -or [int]$Run.skipped -ne 0) {
        throw "Tests did not all pass: $ResultPath"
    }
    Write-Host "Tests passed: $($Run.passed)/$($Run.total) ($ResultPath)"
}

# Close this project's Editor before running. Each step uses new logs and results.
$EditXml = Join-Path $OutputDir 'editmode.xml'
Invoke-Unity @('-batchmode', '-nographics', '-projectPath', $ProjectPath, '-buildTarget', 'Android', '-runTests', '-testPlatform', 'EditMode', '-testResults', $EditXml, '-logFile', (Join-Path $OutputDir 'editmode.log'))
Assert-TestRun $EditXml 40
$PlayXml = Join-Path $OutputDir 'playmode.xml'
Invoke-Unity @('-batchmode', '-projectPath', $ProjectPath, '-buildTarget', 'Android', '-runTests', '-testPlatform', 'PlayMode', '-testResults', $PlayXml, '-logFile', (Join-Path $OutputDir 'playmode.log'))
Assert-TestRun $PlayXml 4

$ApkPath = Join-Path $OutputDir 'vertical-slice.apk'
$BuildLog = Join-Path $OutputDir 'android-build.log'
Invoke-Unity @('-batchmode', '-nographics', '-quit', '-projectPath', $ProjectPath, '-buildTarget', 'Android', '-executeMethod', 'Mofumachi.Editor.AndroidBuild.BuildDevelopmentApk', '-mofumachiApkPath', $ApkPath, '-logFile', $BuildLog)
if (-not (Test-Path -LiteralPath $ApkPath) -or (Get-Item -LiteralPath $ApkPath).Length -le 0 -or -not (Select-String -LiteralPath $BuildLog -SimpleMatch 'MOFUMACHI_APK_SUCCESS' -Quiet)) {
    throw "A fresh successful APK was not produced. Inspect $BuildLog."
}
Write-Host "APK: $ApkPath"
Get-FileHash -LiteralPath $ApkPath -Algorithm SHA256
