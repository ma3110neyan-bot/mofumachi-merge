#requires -Version 5.1
[CmdletBinding()]
param()
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$SaveCheckSource = '2b81174496a5b4703bad0516645829893d4297b9'
$SaveCheckFixtures = @('SaveServiceTests', 'PreferencesTests', 'CharacterPreferenceTests')
$SaveCheckCounts = @{ SaveServiceTests = 33; PreferencesTests = 23; CharacterPreferenceTests = 7 }
$SaveCheckProofs = [ordered]@{
    new_v4_24p = @('NewV4HasUnclaimedEconomyAnd24Points')
    v1_to_v4 = @('LegacyDiskMigrationKeepsEveryFieldAndOriginalBytes(1)')
    v2_to_v4 = @('LegacyDiskMigrationKeepsEveryFieldAndOriginalBytes(2)')
    v3_to_v4 = @('LegacyDiskMigrationKeepsEveryFieldAndOriginalBytes(3)')
    existing_fields_preserved = @('LegacyDiskMigrationKeepsEveryFieldAndOriginalBytes')
    initial_24p_once = @('LegacyDiskMigrationKeepsEveryFieldAndOriginalBytes', 'V4ZeroBalanceIsNotInitializedAgain')
    v4_reload_no_regrant = @('V4ZeroBalanceIsNotInitializedAgain')
    zero_balance_no_reinitialize = @('V4ZeroBalanceIsNotInitializedAgain')
    pre_v4_raw_bytes_preserved = @('LegacyDiskMigrationKeepsEveryFieldAndOriginalBytes')
    existing_pre_v4_not_overwritten = @('ExistingPreV4IsNeverOverwrittenOrRetried')
    invalid_v4_not_repaired = @('InvalidV4BlocksEvenWhenHealthyBackupExists', 'MissingRequiredV4EconomyMemberCannotFallbackOrRepair')
    unsupported_version_blocked = @('UnknownVersionIsNotLoaded')
    migration_failure_no_new_save = @('FailedCommitKeepsLegacySourceProtectionAndCandidate', 'ProtectionCreationFailureDoesNotTouchLegacyPrimary')
    copyfrom_and_rollback = @('CopyAndFailedMutationRestoreCompleteEconomyWithoutAliasing')
}

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
        $Lines = @(& $Git -C $Repo --literal-pathspecs @Arguments 2>&1 | ForEach-Object { $_.ToString() })
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
        '-testFilter', ($Filters -join ';'), '-testResults', (Join-Path $Run 'results.xml'),
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
    return $Doc
}
function Set-SaveCheckCounts([xml]$Doc, $Result) {
    if ($Doc.DocumentElement.Name -cne 'test-run') { throw 'Unexpected test result XML root.' }
    foreach ($Pair in @(@('total', 'total'), @('total_tests', 'total'), @('passed', 'passed'), @('failed', 'failed'), @('skipped', 'skipped'), @('inconclusive', 'inconclusive'))) {
        $Count = 0
        if (-not [int]::TryParse($Doc.DocumentElement.GetAttribute($Pair[1]), [ref]$Count) -or $Count -lt 0) { throw ('Invalid test result count: ' + $Pair[1]) }
        $Result[$Pair[0]] = $Count
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
    $Path = Join-Path $Run 'results.xml'
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
    return 'Unity root error unconfirmed. Preserve unity.log, stdout.log, stderr.log, results.xml and savedata-v4-result.json; do not retry. A launcher/preflight failure, if any, is recorded separately in savedata-v4-result.json.'
}
function Quote-SaveCheckArgument([string]$Value) {
    if ($Value.Contains('"') -or $Value.Contains("`n") -or $Value.Contains("`r")) { throw 'Unsafe command-line argument.' }
    return '"' + $Value + '"'
}

function Publish-SaveCheckResults([string]$Run) {
    $Branch = Invoke-SaveCheckGit @('branch', '--show-current')
    if ($Branch -cne 'feat/daily-world-pass-20261016') { throw 'Sharing stopped: original feature branch changed. Results are preserved.' }
    # Build a diagnostics-only child of the CURRENT remote branch. Never commit,
    # merge, checkout or push the user's local branch/index/unpublished changes.
    $null = Invoke-SaveCheckGit @('fetch', '--no-tags', '--no-recurse-submodules', 'origin', ('refs/heads/' + $Branch))
    $Remote = Invoke-SaveCheckGit @('rev-parse', 'FETCH_HEAD')
    $Publication = Join-Path (Split-Path -Parent $Repo) ('savedata-v4-share-' + [guid]::NewGuid().ToString('N'))
    $null = Invoke-SaveCheckGit @('worktree', 'add', '--detach', '--no-checkout', $Publication, $Remote)
    # Populate only this new worktree's index; no product files need checkout.
    $null = Invoke-SaveCheckGit @('-C', $Publication, 'read-tree', 'HEAD')
    $Relative = 'References/Review/inbox/latest-savedata'
    $Inbox = Join-Path $Publication $Relative
    $null = [IO.Directory]::CreateDirectory($Inbox)
    $Copies = @()
    $Bytes = 0L
    foreach ($Name in @('savedata-v4-result.json', 'source-revision.txt', 'results.xml', 'first-error.log', 'unity.log', 'stdout.log', 'stderr.log', 'unity-arguments.txt')) {
        $Source = Join-Path $Run $Name
        if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) { continue }
        $File = Get-Item -LiteralPath $Source -Force
        if (($File.Attributes -band [IO.FileAttributes]::ReparsePoint) -or $File.Length -gt 4MB -or $Bytes + $File.Length -gt 16MB) {
            throw ('Sharing stopped: linked or oversized diagnostic text: ' + $Name + '. Original results are preserved.')
        }
        $Hash = (Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash.ToLowerInvariant()
        $Target = Join-Path $Inbox $Name
        [IO.File]::Copy($Source, $Target, $false)
        if ((Get-FileHash -LiteralPath $Target -Algorithm SHA256).Hash -ine $Hash -or
            (Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash -ine $Hash) { throw ('Diagnostic changed while sharing: ' + $Name) }
        $Copies += @{ file = $Name; bytes = $File.Length; sha256 = $Hash }
        $Bytes += $File.Length
    }
    if (-not (Test-Path -LiteralPath (Join-Path $Inbox 'savedata-v4-result.json'))) { throw 'Missing result JSON. Nothing will be committed or pushed.' }
    $Info = [ordered]@{ source_run = $Run; source_commit = $SaveCheckSource; shared_at_utc = [DateTime]::UtcNow.ToString('o'); remote_parent = $Remote; branch = $Branch; files = $Copies }
    $Info | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $Inbox 'run-info.json') -Encoding UTF8
    [IO.File]::WriteAllText((Join-Path $Inbox '.gitattributes'), "* -text -filter -ident -working-tree-encoding`n", (New-Object Text.UTF8Encoding($false)))
    $null = Invoke-SaveCheckGit @('-C', $Publication, 'add', '--force', '--', $Relative)
    foreach ($Copy in $Copies) {
        $Raw = Invoke-SaveCheckGit @('-C', $Publication, 'hash-object', '--no-filters', '--', (Join-Path $Inbox $Copy.file))
        $Staged = Invoke-SaveCheckGit @('-C', $Publication, 'rev-parse', (':' + $Relative + '/' + $Copy.file))
        if ($Raw -cne $Staged) { throw ('Git transformed diagnostic bytes: ' + $Copy.file + '. Nothing was committed or pushed.') }
    }
    # --only and the subsequent tree comparison prevent unrelated files entering
    # the commit. Existing hooks remain enabled; no user index is touched.
    $null = Invoke-SaveCheckGit @('-C', $Publication, 'commit', '--only', '-m', ('chore: share SaveData v4 results ' + (Split-Path -Leaf $Run)), '--', $Relative)
    $Revision = Invoke-SaveCheckGit @('-C', $Publication, 'rev-parse', 'HEAD')
    if ((Invoke-SaveCheckGit @('-C', $Publication, 'rev-parse', 'HEAD^')) -cne $Remote) { throw 'Unexpected sharing commit parent. Nothing was pushed.' }
    $Changed = Invoke-SaveCheckGit @('-C', $Publication, 'diff-tree', '--no-commit-id', '--name-only', '-r', '-z', $Remote, $Revision)
    $Paths = @($Changed.Split([char]0) | Where-Object { $_.Length -gt 0 })
    if (-not $Paths.Count) { throw 'Empty sharing commit. Nothing was pushed.' }
    foreach ($Path in $Paths) {
        if (-not $Path.StartsWith(($Relative + '/'), [StringComparison]::Ordinal)) { throw ('Unrelated sharing commit path: ' + $Path + '. Nothing was pushed.') }
    }
    # Exactly one normal push; a concurrent remote update is allowed to reject it.
    $null = Invoke-SaveCheckGit @('-C', $Publication, 'push', '--porcelain', '--no-follow-tags', 'origin', ($Revision + ':refs/heads/' + $Branch))
    return @{ success = $true; commit = $Revision; branch = $Branch; inbox = $Relative; publication_worktree = $Publication; automatic_retries = 0 }
}

# Dot sourcing defines helpers only; it never starts Unity or creates a run.
if ($MyInvocation.InvocationName -eq '.') { return }
if ($env:OS -ne 'Windows_NT') { throw 'Windows 11 / Windows PowerShell 5.1 is required. Unity was not started.' }
$Repo = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$Diagnostics = Join-Path $Repo 'Builds/Diagnostics'
$Attempt = Join-Path $Diagnostics ('savedata-v4-' + $SaveCheckSource + '.attempted')
if (Test-Path -LiteralPath $Attempt) { throw ('This source already has a recorded Unity attempt. Do not retry; preserve ' + $Attempt) }
$Suffix = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$Run = Join-Path $Diagnostics ('savedata-v4-' + $Suffix)
$Project = Join-Path (Split-Path -Parent $Repo) ('savedata-v4-check-' + $Suffix)
$null = [IO.Directory]::CreateDirectory($Run)
$Utf8 = New-Object Text.UTF8Encoding($true)
$RecordPath = Join-Path $Run 'savedata-v4-result.json'
$Record = [ordered]@{
    scope = 'savedata-v4-stage1-editmode-only'; gitRevision = $SaveCheckSource; save_version = 4
    source_commit = $SaveCheckSource; requested_tests = @($SaveCheckFixtures | ForEach-Object { 'Mofumachi.Tests.' + $_ })
    total = $null; total_tests = $null; passed = $null; failed = $null; skipped = $null; inconclusive = $null
    first_error = $null; validation_passed = $false
    source_repository = $Repo; project_path = $Project; unity_version = '6000.6.4f1'; build_target = 'Android'
    launcher_sha256 = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
    requested_fixtures = $SaveCheckFixtures; expected_cases = $SaveCheckCounts
    visual_guard_required = $false; guard_scope = 'not applicable to independent SaveData EditMode tests'
    guard_modified = $false; visual_lock_original_modified = $false
    unity_started = $false; unity_exit_code = $null; test_invocations = 0; automatic_retries = 0
    tests_passed = $false; source_unchanged = $false; success = $false; error = ''
    result = 'NotRun'; stage1_complete = $false; stage2_safe = $false; stage2_candidate = $false; review_required = $true; first_failure = $null
    verification_items = [ordered]@{}
    playmode_executed = $false; capture_verified = $false; build_generated = $false; pixel_verified = $false
}
foreach ($Key in $SaveCheckProofs.Keys) { $Record.verification_items[$Key] = @{ result = 'NotRun'; required_methods = $SaveCheckProofs[$Key]; cases = @() } }
$Failure = $null
try {
    $Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $RecordPath -Encoding UTF8
    [IO.File]::WriteAllText((Join-Path $Run 'source-revision.txt'), $SaveCheckSource + "`r`n", $Utf8)
    $Git = Find-SaveCheckGit
    foreach ($Name in @('GIT_DIR', 'GIT_WORK_TREE', 'GIT_COMMON_DIR', 'GIT_INDEX_FILE')) {
        if ([Environment]::GetEnvironmentVariable($Name)) { throw ('Custom ' + $Name + ' is set; original repository preserved.') }
    }
    if ((Invoke-SaveCheckGit @('rev-parse', '--show-toplevel')).Replace('/', '\').TrimEnd('\') -ine $Repo.Replace('/', '\').TrimEnd('\')) { throw 'Launcher must be inside its repository scripts folder.' }
    if ((Invoke-SaveCheckGit @('branch', '--show-current')) -cne 'feat/daily-world-pass-20261016') { throw 'Unexpected feature branch. No checkout or synchronization was attempted.' }
    $Available = @($SaveCheckSource | & $Git -C $Repo cat-file --batch-check)
    if ($LASTEXITCODE -ne 0 -or $Available.Count -ne 1) { throw 'Cannot inspect the pinned commit without changing the original worktree.' }
    if ([string]$Available[0] -ceq ($SaveCheckSource + ' missing')) {
        $null = Invoke-SaveCheckGit @('fetch', '--no-tags', '--no-recurse-submodules', 'origin', $SaveCheckSource)
    }
    $null = Invoke-SaveCheckGit @('cat-file', '-e', ($SaveCheckSource + '^{commit}'))
    $Editor = Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'
    if (-not (Test-Path -LiteralPath $Editor -PathType Leaf)) { throw 'Unity 6000.6.4f1 is required at the Unity Hub default location.' }
    if ((Get-Item -LiteralPath $Editor).VersionInfo.ProductVersion -notmatch '(?<![0-9])6000\.6\.4f1(?![0-9])') { throw 'Unexpected Unity executable version.' }
    if (-not (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $Editor) 'Data/PlaybackEngines/AndroidPlayer') -PathType Container)) { throw 'Android Build Support is required. Unity was not started.' }
    if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { throw 'Close Unity Editor before starting this one-shot check.' }
    # A detached worktree preserves the caller's branch, index, files and existing saves.
    # Existing Git hooks are honored. No hooks/guards are disabled or altered.
    $null = Invoke-SaveCheckGit @('worktree', 'add', '--detach', $Project, $SaveCheckSource)
    if ((Invoke-SaveCheckGit @('-C', $Project, 'rev-parse', 'HEAD')) -cne $SaveCheckSource) { throw 'Pinned source commit differs.' }
    if ((Invoke-SaveCheckGit @('-C', $Project, 'status', '--porcelain=v1', '--untracked-files=all'))) { throw 'Isolated worktree is not clean.' }
    if ([IO.File]::ReadAllText((Join-Path $Project 'ProjectSettings/ProjectVersion.txt')) -notmatch 'm_EditorVersion: 6000\.6\.4f1\s') { throw 'Pinned project Unity version differs.' }
    # Independent Core tests: no visual-candidate/build/preview guard is a prerequisite.
    # Existing guard implementations and all Visual Lock records remain untouched.
    $Arguments = @(Get-SaveCheckArguments $Project $Run)
    [IO.File]::WriteAllLines((Join-Path $Run 'unity-arguments.txt'), [string[]]$Arguments, $Utf8)
    # Exclusive one-shot receipt is retained even if Unity fails. No automatic cleanup/retry.
    $Receipt = [IO.File]::Open($Attempt, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $Bytes = $Utf8.GetBytes($Run + "`r`n"); $Receipt.Write($Bytes, 0, $Bytes.Length); $Receipt.Flush() } finally { $Receipt.Dispose() }
    $Record.test_invocations = 1
    $Record.result = 'Running'
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
    if (Test-Path -LiteralPath (Join-Path $Run 'results.xml') -PathType Leaf) {
        Set-SaveCheckCounts (Read-SaveCheckXml (Join-Path $Run 'results.xml')) $Record
    }
    if ($Process.ExitCode -ne 0) { throw ('Unity exit ' + $Process.ExitCode + '. No retry will be performed.') }
    $Results = Assert-SaveCheckResults (Join-Path $Run 'results.xml')
    # Unity configuration rewrites stay inside the isolated worktree and are recorded separately.
    $SourcePaths = @('Assets/Mofumachi', 'Assets/Scenes', 'Packages', 'References/VisualLock')
    $Changes = Invoke-SaveCheckGit (@('-C', $Project, 'status', '--porcelain=v1', '--untracked-files=all', '--') + $SourcePaths)
    if ($Changes) { throw ('Pinned product/lock inputs changed in the isolated worktree: ' + $Changes) }
    $Record.source_unchanged = $true
    $Record.unity_configuration_changes = Invoke-SaveCheckGit @('-C', $Project, 'status', '--porcelain=v1', '--', 'ProjectSettings', 'Assets/Settings')
    foreach ($Key in $SaveCheckProofs.Keys) {
        $Evidence = @()
        foreach ($Method in $SaveCheckProofs[$Key]) {
            $Prefix = 'Mofumachi.Tests.SaveServiceTests.' + $Method
            $Matched = @($Results.SelectNodes('//test-case') | Where-Object { $_.GetAttribute('fullname').StartsWith($Prefix, [StringComparison]::Ordinal) })
            if (-not $Matched.Count) { throw ('Missing required verification evidence: ' + $Key) }
            $Evidence += @($Matched | ForEach-Object { $_.GetAttribute('fullname') })
        }
        $Record.verification_items[$Key] = @{ result = 'Passed'; required_methods = $SaveCheckProofs[$Key]; cases = $Evidence }
    }
    $Record.tests_passed = $true
    $Record.validation_passed = $true
    $Record.success = $true
    $Record.result = 'Passed'
    $Record.stage1_complete = $true
    $Record.stage2_candidate = $true
} catch {
    $Failure = $_
    $Record.error = $_.Exception.Message
    $Record.result = $(if ($Record.unity_started) { 'Failed' } else { 'Blocked' })
    $Record.first_failure = $(if ($Record.unity_started) { Get-SaveCheckFirstError $Run } else { 'Launcher preflight; Unity not started: ' + $Record.error })
    $Record.first_error = $Record.first_failure
    [IO.File]::WriteAllText((Join-Path $Run 'first-error.log'), $Record.first_failure, $Utf8)
} finally {
    $Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $RecordPath -Encoding UTF8
}
if ($Failure) { Write-Host ('Verification stopped. Preserve: ' + $Run); Write-Host $Record.first_failure }
else { Write-Host ('PASS: SaveData v4 direct tests only. Next-stage decision requires review. Results: ' + $Run) }
$Sharing = $null
try {
    if (-not (Get-Variable Git -ErrorAction SilentlyContinue)) { $Git = Find-SaveCheckGit }
    $Sharing = Publish-SaveCheckResults $Run
    Write-Host ('Shared to origin/' + $Sharing.branch + ' at ' + $Sharing.commit)
} catch {
    $Sharing = @{ success = $false; error = $_.Exception.Message; automatic_retries = 0; source_run = $Run }
    Write-Host ('Sharing stopped: ' + $Sharing.error)
} finally {
    # Separate receipt keeps the verification JSON and shared evidence bytes immutable.
    $Sharing | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $Run 'share-result.json') -Encoding UTF8
}
if (-not $Sharing.success) { exit 2 }
if ($Failure) { exit 1 }
exit 0
