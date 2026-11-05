#requires -Version 5.1
[CmdletBinding()]
param()
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$SaveCheckSource = '2b81174496a5b4703bad0516645829893d4297b9'
$SaveCheckFixtures = @('SaveServiceTests', 'PreferencesTests', 'CharacterPreferenceTests')
$SaveCheckCounts = @{ SaveServiceTests = 33; PreferencesTests = 23; CharacterPreferenceTests = 7 }

function Find-SaveCheckGit {
    foreach ($Name in @('git.exe', 'git')) {
        $Command = Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($Command) { return $Command.Source }
    }
    if ($env:LOCALAPPDATA) {
        $Bundled = Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA 'GitHubDesktop/app-*/resources/app/git/cmd/git.exe') -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
        if ($Bundled) { return $Bundled.FullName }
    }
    throw 'Git for Windows or GitHub Desktop Git is required. Unity was not started.'
}
function Invoke-SaveCheckGit([string[]]$Arguments) {
    $SavedPreference = $ErrorActionPreference
    $SavedConsole = [Console]::OutputEncoding
    $SavedOutput = $OutputEncoding
    try {
        [Console]::OutputEncoding = New-Object Text.UTF8Encoding($false)
        $OutputEncoding = [Console]::OutputEncoding
        $ErrorActionPreference = 'Continue'
        $Lines = @(& $Git -C $Repo @Arguments 2>&1 | ForEach-Object { $_.ToString() })
        $Code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $SavedPreference
        [Console]::OutputEncoding = $SavedConsole
        $OutputEncoding = $SavedOutput
    }
    if ($Code -ne 0) { throw ('Git operation failed: ' + ($Arguments -join ' ') + "`n" + ($Lines -join "`n")) }
    return ($Lines -join "`n").Trim()
}
function Get-SaveCheckArguments([string]$Project, [string]$Run) {
    $Filters = @($SaveCheckFixtures | ForEach-Object { '^Mofumachi\.Tests\.' + $_ + '\..*' })
    return @('-batchmode', '-nographics', '-projectPath', $Project, '-buildTarget', 'Android',
        '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'Mofumachi.EditModeTests',
        '-testFilter', ($Filters -join ';'), '-testResults', (Join-Path $Run 'editmode.xml'),
        '-logFile', (Join-Path $Run 'unity.log'))
}
function Read-SaveCheckXml([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw 'Unity did not create test result XML.' }
    $Settings = New-Object Xml.XmlReaderSettings
    $Settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $Settings.XmlResolver = $null
    $Reader = [Xml.XmlReader]::Create($Path, $Settings)
    try {
        $Doc = New-Object Xml.XmlDocument
        $Doc.XmlResolver = $null
        $Doc.Load($Reader)
        return $Doc
    } finally { $Reader.Dispose() }
}
function Assert-SaveCheckResults([string]$Path) {
    $Doc = Read-SaveCheckXml $Path
    $Root = $Doc.DocumentElement
    if ($Root.Name -cne 'test-run' -or $Root.GetAttribute('result') -cne 'Passed' -or
        $Root.GetAttribute('total') -ne '63' -or $Root.GetAttribute('passed') -ne '63' -or
        $Root.GetAttribute('failed') -ne '0' -or $Root.GetAttribute('skipped') -ne '0' -or
        $Root.GetAttribute('inconclusive') -ne '0') { throw 'All 63 cases in the three pinned SaveData fixtures must pass, without skipped/inconclusive cases.' }
    $Cases = @($Doc.SelectNodes('//test-case'))
    if ($Cases.Count -ne 63) { throw 'Missing or extra SaveData test-case results.' }
    foreach ($Name in $SaveCheckFixtures) {
        $Prefix = 'Mofumachi.Tests.' + $Name + '.'
        $Matched = @($Cases | Where-Object { $_.GetAttribute('fullname').StartsWith($Prefix, [StringComparison]::Ordinal) })
        if ($Matched.Count -ne $SaveCheckCounts[$Name]) { throw ('Incomplete or unexpected fixture: ' + $Name) }
        foreach ($Case in $Matched) {
            if ($Case.GetAttribute('result') -cne 'Passed') { throw ('Non-passed test: ' + $Case.GetAttribute('fullname')) }
        }
        $Names = @($Matched | ForEach-Object { $_.GetAttribute('fullname') } | Select-Object -Unique)
        if ($Names.Count -ne $Matched.Count) { throw ('Duplicate results: ' + $Name) }
    }
}
function Get-SaveCheckFirstError([string]$Run) {
    # Prefer a causal compiler/startup error over an exit-code or wrapper message.
    foreach ($Name in @('unity.log', 'stderr.log', 'stdout.log')) {
        $Path = Join-Path $Run $Name
        if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { continue }
        $Lines = [IO.File]::ReadAllLines($Path)
        for ($I = 0; $I -lt $Lines.Length; $I++) {
            if ($Lines[$I] -match 'error\s+CS\d{4}|No valid Unity Editor license|No valid license found|Failed to activate/update license|Failed to initialize graphics|Build target.*not supported|Unhandled Exception|Fatal error') {
                $Start = [Math]::Max(0, $I - 2)
                $End = [Math]::Min($Lines.Length - 1, $I + 5)
                return ($Name + ':' + ($I + 1) + "`n" + ($Lines[$Start..$End] -join "`n"))
            }
        }
    }
    $Path = Join-Path $Run 'editmode.xml'
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        try {
            $Doc = Read-SaveCheckXml $Path
            $Failures = @($Doc.SelectNodes('//test-case[failure]') | Sort-Object { $_.GetAttribute('start-time') })
            if (-not $Failures.Count) { $Failures = @($Doc.SelectNodes('//test-suite[failure]') | Sort-Object { $_.GetAttribute('start-time') }) }
            if ($Failures.Count) {
                $First = $Failures[0]
                $Message = $First.SelectSingleNode('failure/message')
                $Stack = $First.SelectSingleNode('failure/stack-trace')
                return ($First.GetAttribute('fullname') + "`n" + $(if ($Message) { $Message.InnerText }) + "`n" + $(if ($Stack) { $Stack.InnerText }))
            }
        } catch { }
    }
    return 'Unity root error unconfirmed. Preserve unity.log, stdout.log, stderr.log, editmode.xml and source-info.json; do not retry. A launcher/preflight failure, if any, is recorded separately in source-info.json.'
}
function Quote-SaveCheckArgument([string]$Value) {
    if ($Value.Contains('"') -or $Value.Contains("`n") -or $Value.Contains("`r")) { throw 'Unsafe command-line argument.' }
    return '"' + $Value + '"'
}

# Dot sourcing defines helpers only; it never starts Unity or creates a run.
if ($MyInvocation.InvocationName -eq '.') { return }
if ($env:OS -ne 'Windows_NT') { throw 'Windows 11 / Windows PowerShell 5.1 is required. Unity was not started.' }
$Repo = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$Git = Find-SaveCheckGit
foreach ($Name in @('GIT_DIR', 'GIT_WORK_TREE', 'GIT_COMMON_DIR', 'GIT_INDEX_FILE')) {
    if ([Environment]::GetEnvironmentVariable($Name)) { throw ('Custom ' + $Name + ' is set; original repository preserved.') }
}
if ((Invoke-SaveCheckGit @('rev-parse', '--show-toplevel')).Replace('/', '\').TrimEnd('\') -ine $Repo.Replace('/', '\').TrimEnd('\')) { throw 'Launcher must be inside its repository scripts folder.' }
if ((Invoke-SaveCheckGit @('branch', '--show-current')) -cne 'feat/daily-world-pass-20261016') { throw 'Unexpected feature branch. No checkout or synchronization was attempted.' }
$null = Invoke-SaveCheckGit @('cat-file', '-e', ($SaveCheckSource + '^{commit}'))
$Editor = Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'
if (-not (Test-Path -LiteralPath $Editor -PathType Leaf)) { throw 'Unity 6000.6.4f1 is required at the Unity Hub default location.' }
if ((Get-Item -LiteralPath $Editor).VersionInfo.ProductVersion -notmatch '(?<![0-9])6000\.6\.4f1(?![0-9])') { throw 'Unexpected Unity executable version.' }
if (-not (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $Editor) 'Data/PlaybackEngines/AndroidPlayer') -PathType Container)) { throw 'Android Build Support is required. Unity was not started.' }
if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { throw 'Close Unity Editor before starting this one-shot check.' }
$Diagnostics = Join-Path $Repo 'Builds/Diagnostics'
$Attempt = Join-Path $Diagnostics ('savedata-v4-' + $SaveCheckSource + '.attempted')
if (Test-Path -LiteralPath $Attempt) { throw ('This source already has a recorded Unity attempt. Do not retry; preserve ' + $Attempt) }
$Suffix = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$Run = Join-Path $Diagnostics ('savedata-v4-' + $Suffix)
$Project = Join-Path (Split-Path -Parent $Repo) ('savedata-v4-check-' + $Suffix)
$null = [IO.Directory]::CreateDirectory($Run)
$Utf8 = New-Object Text.UTF8Encoding($true)
$RecordPath = Join-Path $Run 'source-info.json'
$Record = [ordered]@{
    scope = 'savedata-v4-stage1-editmode-only'; gitRevision = $SaveCheckSource; save_version = 4
    source_repository = $Repo; project_path = $Project; unity_version = '6000.6.4f1'; build_target = 'Android'
    launcher_sha256 = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
    requested_fixtures = $SaveCheckFixtures; expected_cases = $SaveCheckCounts
    existing_guard = 'Assert-CharactersVisualLock'; guard_passed = $false
    guard_modified = $false; visual_lock_original_modified = $false
    unity_started = $false; unity_exit_code = $null; test_invocations = 0; automatic_retries = 0
    tests_passed = $false; source_unchanged = $false; success = $false; error = ''
    playmode_executed = $false; capture_verified = $false; build_generated = $false; pixel_verified = $false
}
$Failure = $null
try {
    [IO.File]::WriteAllText((Join-Path $Run 'source-revision.txt'), $SaveCheckSource + "`r`n", $Utf8)
    # A detached worktree preserves the caller's branch, index, files and existing saves.
    # Existing Git hooks are honored. No hooks/guards are disabled or altered.
    $null = Invoke-SaveCheckGit @('worktree', 'add', '--detach', $Project, $SaveCheckSource)
    if ((Invoke-SaveCheckGit @('-C', $Project, 'rev-parse', 'HEAD')) -cne $SaveCheckSource) { throw 'Pinned source commit differs.' }
    if ((Invoke-SaveCheckGit @('-C', $Project, 'status', '--porcelain=v1', '--untracked-files=all'))) { throw 'Isolated worktree is not clean.' }
    if ([IO.File]::ReadAllText((Join-Path $Project 'ProjectSettings/ProjectVersion.txt')) -notmatch 'm_EditorVersion: 6000\.6\.4f1\s') { throw 'Pinned project Unity version differs.' }
    # This existing guard protects the CURRENT character snapshot, not the retired Noah trial.
    # It does not grant permission to change Core, build an APK, or accept a visual change.
    . (Join-Path $Project 'scripts/character-finish-scope.ps1')
    Assert-CharactersVisualLock $Project
    $Record.guard_passed = $true
    $Arguments = @(Get-SaveCheckArguments $Project $Run)
    [IO.File]::WriteAllLines((Join-Path $Run 'unity-arguments.txt'), [string[]]$Arguments, $Utf8)
    # Exclusive one-shot receipt is retained even if Unity fails. No automatic cleanup/retry.
    $Receipt = [IO.File]::Open($Attempt, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $Bytes = $Utf8.GetBytes($Run + "`r`n"); $Receipt.Write($Bytes, 0, $Bytes.Length); $Receipt.Flush() } finally { $Receipt.Dispose() }
    $Record.test_invocations = 1
    $Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $RecordPath -Encoding UTF8
    Write-Host 'SaveData v4 only: 3 EditMode fixtures, 63 cases, ONE Unity invocation. First import may take several minutes.'
    Write-Host ('Preserved results: ' + $Run)
    $Start = New-Object Diagnostics.ProcessStartInfo
    $Start.FileName = $Editor
    $Start.WorkingDirectory = $Project
    $Start.UseShellExecute = $false
    $Start.RedirectStandardOutput = $true
    $Start.RedirectStandardError = $true
    $Start.Arguments = @($Arguments | ForEach-Object { Quote-SaveCheckArgument $_ }) -join ' '
    $Process = [Diagnostics.Process]::Start($Start)
    $Record.unity_started = $true
    $Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $RecordPath -Encoding UTF8
    $Stdout = $Process.StandardOutput.ReadToEndAsync()
    $Stderr = $Process.StandardError.ReadToEndAsync()
    $Ticks = 0
    while (-not $Process.WaitForExit(1000)) {
        $Ticks++
        if ($Ticks % 30 -eq 0) { Write-Host 'Unity is still running. Progress is recorded in unity.log. Do not start another run.' }
    }
    [IO.File]::WriteAllText((Join-Path $Run 'stdout.log'), $Stdout.Result, $Utf8)
    [IO.File]::WriteAllText((Join-Path $Run 'stderr.log'), $Stderr.Result, $Utf8)
    $Record.unity_exit_code = $Process.ExitCode
    if ($Process.ExitCode -ne 0) { throw ('Unity exit ' + $Process.ExitCode + '. No retry will be performed.') }
    Assert-SaveCheckResults (Join-Path $Run 'editmode.xml')
    # Unity configuration rewrites stay inside the isolated worktree and are recorded separately.
    $SourcePaths = @('Assets/Mofumachi', 'Assets/Scenes', 'Packages', 'References/VisualLock')
    $Changes = Invoke-SaveCheckGit (@('-C', $Project, 'status', '--porcelain=v1', '--untracked-files=all', '--') + $SourcePaths)
    if ($Changes) { throw ('Pinned product/lock inputs changed in the isolated worktree: ' + $Changes) }
    $Record.source_unchanged = $true
    $Record.unity_configuration_changes = Invoke-SaveCheckGit @('-C', $Project, 'status', '--porcelain=v1', '--', 'ProjectSettings', 'Assets/Settings')
    $Record.tests_passed = $true
    $Record.success = $true
} catch {
    $Failure = $_
    $Record.error = $_.Exception.Message
    [IO.File]::WriteAllText((Join-Path $Run 'first-error.log'), (Get-SaveCheckFirstError $Run), $Utf8)
} finally {
    $Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $RecordPath -Encoding UTF8
}
if ($Failure) { Write-Host ('Stopped. Preserve this folder: ' + $Run); throw $Failure }
Write-Host ('PASS: SaveData v4 direct tests only. Results: ' + $Run)
