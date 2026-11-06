#requires -Version 5.1
[CmdletBinding()]
param()
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProductCommit = '97be3fafe5d73fda055ae3eba34c4ff8c45f85b7'
$Fixture = 'Mofumachi.Tests.MofuPointServiceTests'
$ExpectedCases = 68

function Invoke-PointGit([string[]]$Arguments) {
    $Old = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $Text = @(& $Git -C $Repository @Arguments 2>&1 | ForEach-Object { $_.ToString() })
        $Code = $LASTEXITCODE
    } finally { $ErrorActionPreference = $Old }
    if ($Code -ne 0) { throw ('Git failed: ' + ($Arguments -join ' ') + "`n" + ($Text -join "`n")) }
    return ($Text -join "`n").Trim()
}
function Read-PointXml([string]$Path) {
    $Settings = New-Object Xml.XmlReaderSettings
    $Settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit; $Settings.XmlResolver = $null
    $Reader = [Xml.XmlReader]::Create($Path, $Settings)
    try { $Doc = New-Object Xml.XmlDocument; $Doc.XmlResolver = $null; $Doc.Load($Reader); return $Doc }
    finally { $Reader.Dispose() }
}
function Get-PointFirstError([string]$Directory) {
    $Log = Join-Path $Directory 'unity.log'
    if (Test-Path -LiteralPath $Log) {
        $Lines = [IO.File]::ReadAllLines($Log)
        for ($I = 0; $I -lt $Lines.Length; $I++) {
            if ($Lines[$I] -match 'error\s+CS\d{4}|No valid Unity Editor license|No valid license found|Failed to initialize graphics|Build target.*not supported|Unhandled Exception|Fatal error') {
                $End = [Math]::Min($Lines.Length - 1, $I + 5)
                return ($Lines[$I..$End] -join "`n")
            }
        }
    }
    $Xml = Join-Path $Directory 'results.xml'
    if (Test-Path -LiteralPath $Xml) {
        try {
            $Doc = Read-PointXml $Xml
            $Failures = @($Doc.SelectNodes('//test-case[failure]') | Sort-Object { $_.GetAttribute('start-time') })
            if (-not $Failures.Count) { $Failures = @($Doc.SelectNodes('//test-suite[failure]')) }
            if ($Failures.Count) { return ($Failures[0].GetAttribute('fullname') + "`n" + $Failures[0].SelectSingleNode('failure').InnerText) }
        } catch { }
    }
    return 'Unity root error unconfirmed. Preserve this run; no automatic retry.'
}
function Get-PointArguments([string]$Project, [string]$Directory) {
    return @('-batchmode', '-nographics', '-projectPath', $Project, '-buildTarget', 'Android',
        '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'Mofumachi.EditModeTests',
        '-testFilter', '^Mofumachi\.Tests\.MofuPointServiceTests\..*',
        '-testResults', (Join-Path $Directory 'results.xml'), '-logFile', (Join-Path $Directory 'unity.log'))
}
if ($MyInvocation.InvocationName -eq '.') { return }
if ($env:OS -ne 'Windows_NT') { throw 'Windows 11 / Windows PowerShell 5.1 is required. Unity was not started.' }
if ($ProductCommit -cnotmatch '^[a-f0-9]{40}$') { throw 'The reviewed product commit must be pinned before use.' }
$Repository = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$Once = Join-Path $Repository ('Builds/Diagnostics/mofu-points-' + $ProductCommit + '.attempted')
if (Test-Path -LiteralPath $Once) { throw ('This commit already has a Unity attempt. Preserve ' + $Once + '; do not retry.') }
$Id = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,8)
$Run = Join-Path $Repository ('Builds/Diagnostics/mofu-points-' + $Id)
$Project = Join-Path (Split-Path -Parent $Repository) ('mofu-points-check-' + $Id)
$null = [IO.Directory]::CreateDirectory($Run)
$Encoding = New-Object Text.UTF8Encoding($true)
$Result = [ordered]@{
    source_commit = $ProductCommit; unity_version = '6000.6.4f1'; save_version = 4
    requested_tests = @($Fixture); expected_cases = $ExpectedCases; test_platform = 'EditMode'
    total = $null; passed = $null; failed = $null; skipped = $null; automatic_retries = 0
    unity_started = $false; unity_exit_code = $null; unity_invocations = 0
    validation_passed = $false; result = 'NotRun'; first_error = $null; runner_error = $null
    source_unchanged = $false; project_path = $Project; stage3_safe = $false; review_required = $true
}
$Failure = $null
try {
    [IO.File]::WriteAllText((Join-Path $Run 'source-revision.txt'), $ProductCommit + "`r`n", $Encoding)
    foreach ($Name in @('GIT_DIR','GIT_WORK_TREE','GIT_INDEX_FILE','GIT_COMMON_DIR')) {
        if ([Environment]::GetEnvironmentVariable($Name)) { throw ('Custom ' + $Name + ' is set; original repository preserved.') }
    }
    $GitCommand = Get-Command git.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($GitCommand) { $Git = $GitCommand.Source }
    elseif ($env:LOCALAPPDATA) {
        $Bundled = Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA 'GitHubDesktop/app-*/resources/app/git/cmd/git.exe') -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
        if (-not $Bundled) { throw 'Git for Windows or GitHub Desktop Git is required.' }; $Git = $Bundled.FullName
    } else { throw 'Git for Windows or GitHub Desktop Git is required.' }
    $Editor = Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'
    if (-not (Test-Path -LiteralPath $Editor -PathType Leaf)) { throw 'Install Unity6000.6.4f1 at the Unity Hub default location.' }
    if ((Get-Item -LiteralPath $Editor).VersionInfo.ProductVersion -notmatch '6000\.6\.4f1') { throw 'Wrong Unity executable version.' }
    $Available = @($ProductCommit | & $Git -C $Repository cat-file --batch-check)
    if ($LASTEXITCODE -ne 0 -or $Available.Count -ne 1) { throw 'Cannot inspect pinned commit.' }
    if ([string]$Available[0] -ceq ($ProductCommit + ' missing')) { $null = Invoke-PointGit @('fetch','--no-tags','--no-recurse-submodules','origin',$ProductCommit) }
    $null = Invoke-PointGit @('worktree','add','--detach',$Project,$ProductCommit)
    if ((Invoke-PointGit @('-C',$Project,'rev-parse','HEAD')) -cne $ProductCommit) { throw 'Product SHA differs.' }
    if (Invoke-PointGit @('-C',$Project,'status','--porcelain=v1','--untracked-files=all')) { throw 'Isolated test worktree is not clean.' }
    if ([IO.File]::ReadAllText((Join-Path $Project 'ProjectSettings/ProjectVersion.txt')) -notmatch 'm_EditorVersion: 6000\.6\.4f1\s') { throw 'Project Unity version differs.' }
    $Arguments = @(Get-PointArguments $Project $Run)
    [IO.File]::WriteAllLines((Join-Path $Run 'unity-arguments.txt'), [string[]]$Arguments, $Encoding)
    $Receipt = [IO.File]::Open($Once,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try { $Bytes = $Encoding.GetBytes($Run); $Receipt.Write($Bytes,0,$Bytes.Length); $Receipt.Flush() } finally { $Receipt.Dispose() }
    $Result.unity_invocations = 1; $Result.result = 'Running'
    $Result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Run 'mofu-points-result.json') -Encoding UTF8
    $Start = New-Object Diagnostics.ProcessStartInfo
    $Start.FileName = $Editor; $Start.WorkingDirectory = $Project; $Start.UseShellExecute = $false
    $Start.Arguments = (@($Arguments | ForEach-Object { if ($_.Contains('"')) { throw 'Unsafe argument.' }; '"' + $_ + '"' }) -join ' ')
    Write-Host ('Mofu points: exactly one EditMode fixture / 68 cases / one Unity invocation. Results: ' + $Run)
    $Process = [Diagnostics.Process]::Start($Start); $Result.unity_started = $true
    $Process.WaitForExit(); $Result.unity_exit_code = $Process.ExitCode
    if (Test-Path -LiteralPath (Join-Path $Run 'results.xml')) {
        $Doc = Read-PointXml (Join-Path $Run 'results.xml'); $Root = $Doc.DocumentElement
        foreach ($Name in @('total','passed','failed','skipped')) {
            $Count = 0
            if (-not [int]::TryParse($Root.GetAttribute($Name),[ref]$Count) -or $Count -lt 0) { throw 'Invalid XML result count.' }
            $Result[$Name] = $Count
        }
    }
    if ($Process.ExitCode -ne 0) { throw ('Unity exit ' + $Process.ExitCode + '; no retry.') }
    if (-not (Test-Path -LiteralPath (Join-Path $Run 'results.xml'))) { throw 'No native test XML was produced.' }
    $Cases = @($Doc.SelectNodes('//test-case'))
    if ($Root.Name -cne 'test-run' -or $Root.GetAttribute('result') -cne 'Passed' -or $Result.total -ne 68 -or
        $Result.passed -ne 68 -or $Result.failed -ne 0 -or $Result.skipped -ne 0 -or $Root.GetAttribute('inconclusive') -ne '0' -or $Cases.Count -ne 68) { throw 'All 68 requested cases must pass with no skipped/inconclusive cases.' }
    foreach ($Case in $Cases) {
        if (-not $Case.GetAttribute('fullname').StartsWith(($Fixture+'.'),[StringComparison]::Ordinal) -or $Case.GetAttribute('result') -cne 'Passed') { throw 'Unexpected or non-passed case.' }
    }
    if (@($Cases | ForEach-Object { $_.GetAttribute('fullname') } | Select-Object -Unique).Count -ne 68) { throw 'Duplicate/missing test-case evidence.' }
    if (Invoke-PointGit @('-C',$Project,'status','--porcelain=v1','--untracked-files=all','--','Assets/Mofumachi','Assets/Scenes','Packages','References/VisualLock')) { throw 'Product inputs changed in the isolated worktree.' }
    $Result.source_unchanged = $true; $Result.validation_passed = $true; $Result.result = 'Passed'
} catch {
    $Failure = $_
    $Result.runner_error = $_.Exception.Message
    $Result.result = $(if ($Result.unity_started) { 'Failed' } else { 'Blocked' })
    $Result.first_error = $(if ($Result.unity_started) { Get-PointFirstError $Run } else { $_.Exception.Message })
    [IO.File]::WriteAllText((Join-Path $Run 'first-error.log'),$Result.first_error,$Encoding)
} finally {
    $Result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Run 'mofu-points-result.json') -Encoding UTF8
}
if ($Failure) { Write-Host ('STOP: '+$Result.first_error); Write-Host ('Preserve '+$Run); exit 1 }
Write-Host ('PASS: points foundation only. Review this run before stage3: '+$Run)
exit 0
