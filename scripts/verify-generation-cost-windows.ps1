#requires -Version 5.1
[CmdletBinding()]
param()
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProductCommit = 'b9e7f1a2e2384998878edbe97cba82885587e3dd'
$Fixture = 'Mofumachi.Tests.GenerationCostTests'
$ExpectedCases = 33
$script:FetchedGenerationHead = $null

function Invoke-GenerationGit([string[]]$Arguments) {
    # Protect the original index/worktree on every path, including share-only after failure.
    foreach ($Name in @('GIT_DIR','GIT_WORK_TREE','GIT_INDEX_FILE','GIT_COMMON_DIR')) {
        if ([Environment]::GetEnvironmentVariable($Name)) { throw ('Custom ' + $Name + ' is set; Git sharing stopped; original repository preserved.') }
    }
    $Old = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $Text = @(& $Git -C $Repository --literal-pathspecs @Arguments 2>&1 | ForEach-Object { $_.ToString() })
        $Code = $LASTEXITCODE
    } finally { $ErrorActionPreference = $Old }
    if ($Code -ne 0) { throw ('Git failed: ' + ($Arguments -join ' ') + "`n" + ($Text -join "`n")) }
    return ($Text -join "`n").Trim()
}
function Read-GenerationXml([string]$Path) {
    $Settings = New-Object Xml.XmlReaderSettings
    $Settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit; $Settings.XmlResolver = $null
    $Reader = [Xml.XmlReader]::Create($Path, $Settings)
    try { $Doc = New-Object Xml.XmlDocument; $Doc.XmlResolver = $null; $Doc.Load($Reader); return $Doc }
    finally { $Reader.Dispose() }
}
function Get-GenerationFirstError([string]$Directory) {
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
            $Doc = Read-GenerationXml $Xml
            $Failures = @($Doc.SelectNodes('//test-case[failure]') | Sort-Object { $_.GetAttribute('start-time') })
            if (-not $Failures.Count) { $Failures = @($Doc.SelectNodes('//test-suite[failure]')) }
            if ($Failures.Count) { return ($Failures[0].GetAttribute('fullname') + "`n" + $Failures[0].SelectSingleNode('failure').InnerText) }
        } catch { }
    }
    return 'Unity root error unconfirmed. Preserve this run; no automatic retry.'
}
function Get-GenerationArguments([string]$Project, [string]$Directory) {
    return @('-batchmode', '-nographics', '-projectPath', $Project, '-buildTarget', 'Android',
        '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'Mofumachi.EditModeTests',
        '-testFilter', '^Mofumachi\.Tests\.GenerationCostTests\..*',
        '-testResults', (Join-Path $Directory 'results.xml'), '-logFile', (Join-Path $Directory 'unity.log'))
}
function Find-GenerationGit {
    foreach ($Name in @('git.exe','git')) {
        $Command = Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($Command) { return $Command.Source }
    }
    if ($env:LOCALAPPDATA) {
        $Bundled = Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA 'GitHubDesktop/app-*/resources/app/git/cmd/git.exe') -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
        if ($Bundled) { return $Bundled.FullName }
    }
    throw 'Git for Windows or GitHub Desktop Git is required.'
}
function Get-GenerationRemoteHead {
    if (-not $script:FetchedGenerationHead) {
        $null = Invoke-GenerationGit @('fetch','origin','refs/heads/feat/daily-world-pass-20261016','--no-tags','--no-recurse-submodules')
        $script:FetchedGenerationHead = Invoke-GenerationGit @('rev-parse','FETCH_HEAD')
    }
    return $script:FetchedGenerationHead
}
function Publish-GenerationResult([string]$Directory) {
    $Branch = Invoke-GenerationGit @('branch','--show-current')
    if ($Branch -cne 'feat/daily-world-pass-20261016') { throw 'Sharing stopped: original feature branch changed; results retained.' }
    $Native = [IO.File]::ReadAllText((Join-Path $Directory 'generation-cost-result.json')) | ConvertFrom-Json
    if ($Native.source_commit -cne $ProductCommit -or $Native.result -notin @('Passed','Failed','Blocked')) { throw 'No terminal result for the pinned source. Do not start another Unity process.' }
    $Remote = Get-GenerationRemoteHead
    $Publication = Join-Path (Split-Path -Parent $Repository) ('generation-cost-share-' + [guid]::NewGuid().ToString('N'))
    $null = Invoke-GenerationGit @('worktree','add','--detach','--no-checkout',$Publication,$Remote)
    $null = Invoke-GenerationGit @('-C',$Publication,'read-tree','HEAD')
    $Relative = 'References/Review/inbox/latest-generation-cost'
    $Inbox = Join-Path $Publication $Relative
    $null = [IO.Directory]::CreateDirectory($Inbox)
    $Rows = @(); $Skipped = @(); $TotalBytes = 0L
    foreach ($Name in @('generation-cost-result.json','source-revision.txt','results.xml','first-error.log','unity.log','unity-arguments.txt')) {
        $Source = Join-Path $Directory $Name
        if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) { continue }
        $File = Get-Item -LiteralPath $Source -Force
        if ($File.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Refuse linked evidence; original results retained.' }
        if ($File.Length -gt 4MB -or $TotalBytes + $File.Length -gt 16MB) {
            if ($Name -eq 'unity.log') { $Skipped += @{file=$Name;bytes=$File.Length;reason='Full log retained locally; XML/JSON/first error shared'}; continue }
            throw ('Required evidence exceeds sharing budget: '+$Name+'. Original results retained.')
        }
        $Hash = (Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash.ToLowerInvariant()
        $Target = Join-Path $Inbox $Name
        [IO.File]::Copy($Source,$Target,$false)
        if ((Get-FileHash -LiteralPath $Target -Algorithm SHA256).Hash -ine $Hash -or
            (Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash -ine $Hash) { throw ('Evidence changed while copying: '+$Name) }
        $Rows += @{file=$Name;bytes=$File.Length;sha256=$Hash}; $TotalBytes += $File.Length
    }
    $Info = [ordered]@{source_run=$Directory;source_commit=$ProductCommit;fixture=$Fixture;copied_at_utc=[DateTime]::UtcNow.ToString('o');remote_parent=$Remote;files=$Rows;skipped=$Skipped}
    $Info | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Inbox 'run-info.json') -Encoding UTF8
    [IO.File]::WriteAllText((Join-Path $Inbox '.gitattributes'),"* -text -filter -ident -working-tree-encoding`n",(New-Object Text.UTF8Encoding($false)))
    $null = Invoke-GenerationGit @('-C',$Publication,'add','--force','--',$Relative)
    foreach ($Row in $Rows) {
        $Raw = Invoke-GenerationGit @('-C',$Publication,'hash-object','--no-filters','--',(Join-Path $Inbox $Row.file))
        $Index = Invoke-GenerationGit @('-C',$Publication,'rev-parse',(':'+$Relative+'/'+$Row.file))
        if ($Raw -cne $Index) { throw ('Git transformed evidence bytes: '+$Row.file+'. Nothing was pushed.') }
    }
    $null = Invoke-GenerationGit @('-C',$Publication,'commit','--only','-m',('chore: share generation cost results '+(Split-Path -Leaf $Directory)),'--',$Relative)
    $Revision = Invoke-GenerationGit @('-C',$Publication,'rev-parse','HEAD')
    if ((Invoke-GenerationGit @('-C',$Publication,'rev-parse','HEAD^')) -cne $Remote) { throw 'Unexpected publication parent; nothing pushed.' }
    $Changed = Invoke-GenerationGit @('-C',$Publication,'diff-tree','--no-commit-id','--name-only','-r','-z',$Remote,$Revision)
    $Paths = @($Changed.Split([char]0) | Where-Object {$_.Length -gt 0})
    if (-not $Paths.Count) { throw 'Empty publication commit; nothing pushed.' }
    foreach ($Path in $Paths) { if (-not $Path.StartsWith(($Relative+'/'),[StringComparison]::Ordinal)) { throw 'Unrelated publication path; nothing pushed.' } }
    # One normal push, existing hooks honored. No local branch/index changes or retry.
    $null = Invoke-GenerationGit @('-C',$Publication,'push','--porcelain','--no-follow-tags','origin',($Revision+':refs/heads/'+$Branch))
    return @{success=$true;commit=$Revision;source_run=$Directory;automatic_retries=0}
}
function Share-GenerationResult([string]$Directory) {
    $ReceiptPath = Join-Path $Directory 'share-result.json'
    if (Test-Path -LiteralPath $ReceiptPath) {
        $Previous = [IO.File]::ReadAllText($ReceiptPath) | ConvertFrom-Json
        if ($Previous.success -eq $true -and $Previous.source_run -ceq $Directory) { Write-Host 'Existing result was already shared. No Unity or push repeated.'; return $true }
    }
    $Sharing = $null
    try {
        $Sharing = Publish-GenerationResult $Directory
        Write-Host ('Results shared at '+$Sharing.commit)
    } catch {
        $Sharing = @{success=$false;error=$_.Exception.Message;source_run=$Directory;automatic_retries=0}
        Write-Host ('Sharing stopped: '+$Sharing.error)
    } finally {
        $Sharing | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $ReceiptPath -Encoding UTF8
    }
    return [bool]$Sharing.success
}
if ($MyInvocation.InvocationName -eq '.') { return }
if ($env:OS -ne 'Windows_NT') { throw 'Windows 11 / Windows PowerShell 5.1 is required. Unity was not started.' }
if ($ProductCommit -cnotmatch '^[a-f0-9]{40}$') { throw 'The reviewed product commit must be pinned before use.' }
$Repository = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$Once = Join-Path $Repository ('Builds/Diagnostics/generation-cost-' + $ProductCommit + '.attempted')
if (Test-Path -LiteralPath $Once) {
    $Existing = [IO.File]::ReadAllText($Once).Trim()
    $Boundary = [IO.Path]::GetFullPath((Join-Path $Repository 'Builds/Diagnostics')).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not [IO.Path]::GetFullPath($Existing).StartsWith($Boundary,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe existing run receipt; original files preserved.' }
    Write-Host 'Existing Unity attempt found. Sharing the saved terminal result only; no test restart.'
    $Git = Find-GenerationGit
    if (-not (Share-GenerationResult $Existing)) { exit 2 }
    $SavedResult = [IO.File]::ReadAllText((Join-Path $Existing 'generation-cost-result.json')) | ConvertFrom-Json
    if ($SavedResult.validation_passed -ne $true) { exit 1 }
    exit 0
}
$Id = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,8)
$Run = Join-Path $Repository ('Builds/Diagnostics/generation-cost-' + $Id)
$Project = Join-Path (Split-Path -Parent $Repository) ('generation-cost-check-' + $Id)
$null = [IO.Directory]::CreateDirectory($Run)
$Encoding = New-Object Text.UTF8Encoding($true)
$Result = [ordered]@{
    source_commit = $ProductCommit; unity_version = '6000.6.4f1'; save_version = 4
    generation_cost = 2; generation_types = @('tea','bread','flower'); generation_level = 1
    initial_board_free = $true; merge_free = $true; move_free = $true; delivery_free = $true
    requested_tests = @($Fixture); expected_cases = $ExpectedCases; test_platform = 'EditMode'
    total = $null; passed = $null; failed = $null; skipped = $null; automatic_retries = 0
    unity_started = $false; unity_exit_code = $null; unity_invocations = 0
    validation_passed = $false; result = 'NotRun'; first_error = $null; runner_error = $null
    source_unchanged = $false; project_path = $Project; stage4_safe = $false; review_required = $true
}
$Failure = $null
try {
    [IO.File]::WriteAllText((Join-Path $Run 'source-revision.txt'), $ProductCommit + "`r`n", $Encoding)
    foreach ($Name in @('GIT_DIR','GIT_WORK_TREE','GIT_INDEX_FILE','GIT_COMMON_DIR')) {
        if ([Environment]::GetEnvironmentVariable($Name)) { throw ('Custom ' + $Name + ' is set; original repository preserved.') }
    }
    $Git = Find-GenerationGit
    if ((Invoke-GenerationGit @('branch','--show-current')) -cne 'feat/daily-world-pass-20261016') { throw 'Use the current feature repository; original branch was not changed.' }
    $Editor = Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'
    if (-not (Test-Path -LiteralPath $Editor -PathType Leaf)) { throw 'Install Unity6000.6.4f1 at the Unity Hub default location.' }
    if ((Get-Item -LiteralPath $Editor).VersionInfo.ProductVersion -notmatch '6000\.6\.4f1') { throw 'Wrong Unity executable version.' }
    $Available = @($ProductCommit | & $Git -C $Repository cat-file --batch-check)
    if ($LASTEXITCODE -ne 0 -or $Available.Count -ne 1) { throw 'Cannot inspect pinned commit.' }
    if ([string]$Available[0] -ceq ($ProductCommit + ' missing')) { $null = Get-GenerationRemoteHead }
    $null = Invoke-GenerationGit @('cat-file','-e',($ProductCommit+'^{commit}'))
    $null = Invoke-GenerationGit @('worktree','add','--detach',$Project,$ProductCommit)
    if ((Invoke-GenerationGit @('-C',$Project,'rev-parse','HEAD')) -cne $ProductCommit) { throw 'Product SHA differs.' }
    if (Invoke-GenerationGit @('-C',$Project,'status','--porcelain=v1','--untracked-files=all')) { throw 'Isolated test worktree is not clean.' }
    if ([IO.File]::ReadAllText((Join-Path $Project 'ProjectSettings/ProjectVersion.txt')) -notmatch 'm_EditorVersion: 6000\.6\.4f1\s') { throw 'Project Unity version differs.' }
    $Arguments = @(Get-GenerationArguments $Project $Run)
    [IO.File]::WriteAllLines((Join-Path $Run 'unity-arguments.txt'), [string[]]$Arguments, $Encoding)
    $Receipt = [IO.File]::Open($Once,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try { $Bytes = $Encoding.GetBytes($Run); $Receipt.Write($Bytes,0,$Bytes.Length); $Receipt.Flush() } finally { $Receipt.Dispose() }
    $Result.unity_invocations = 1; $Result.result = 'Running'
    $Result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Run 'generation-cost-result.json') -Encoding UTF8
    $Start = New-Object Diagnostics.ProcessStartInfo
    $Start.FileName = $Editor; $Start.WorkingDirectory = $Project; $Start.UseShellExecute = $false
    $Start.Arguments = (@($Arguments | ForEach-Object { if ($_.Contains('"')) { throw 'Unsafe argument.' }; '"' + $_ + '"' }) -join ' ')
    Write-Host ('Generation cost: exactly one EditMode fixture / 33 cases / one Unity invocation. Results: ' + $Run)
    $Process = [Diagnostics.Process]::Start($Start); $Result.unity_started = $true
    $Process.WaitForExit(); $Result.unity_exit_code = $Process.ExitCode
    if (Test-Path -LiteralPath (Join-Path $Run 'results.xml')) {
        $Doc = Read-GenerationXml (Join-Path $Run 'results.xml'); $Root = $Doc.DocumentElement
        foreach ($Name in @('total','passed','failed','skipped')) {
            $Count = 0
            if (-not [int]::TryParse($Root.GetAttribute($Name),[ref]$Count) -or $Count -lt 0) { throw 'Invalid XML result count.' }
            $Result[$Name] = $Count
        }
    }
    if ($Process.ExitCode -ne 0) { throw ('Unity exit ' + $Process.ExitCode + '; no retry.') }
    if (-not (Test-Path -LiteralPath (Join-Path $Run 'results.xml'))) { throw 'No native test XML was produced.' }
    $Cases = @($Doc.SelectNodes('//test-case'))
    if ($Root.Name -cne 'test-run' -or $Root.GetAttribute('result') -cne 'Passed' -or $Result.total -ne 33 -or
        $Result.passed -ne 33 -or $Result.failed -ne 0 -or $Result.skipped -ne 0 -or $Root.GetAttribute('inconclusive') -ne '0' -or $Cases.Count -ne 33) { throw 'All 33 requested cases must pass with no skipped/inconclusive cases.' }
    foreach ($Case in $Cases) {
        if (-not $Case.GetAttribute('fullname').StartsWith(($Fixture+'.'),[StringComparison]::Ordinal) -or $Case.GetAttribute('result') -cne 'Passed') { throw 'Unexpected or non-passed case.' }
    }
    if (@($Cases | ForEach-Object { $_.GetAttribute('fullname') } | Select-Object -Unique).Count -ne 33) { throw 'Duplicate/missing test-case evidence.' }
    if (Invoke-GenerationGit @('-C',$Project,'status','--porcelain=v1','--untracked-files=all','--','Assets/Mofumachi','Assets/Scenes','Packages','References/VisualLock')) { throw 'Product inputs changed in the isolated worktree.' }
    $Result.source_unchanged = $true; $Result.validation_passed = $true; $Result.result = 'Passed'
} catch {
    $Failure = $_
    $Result.runner_error = $_.Exception.Message
    $Result.result = $(if ($Result.unity_started) { 'Failed' } else { 'Blocked' })
    $Result.first_error = $(if ($Result.unity_started) { Get-GenerationFirstError $Run } else { $_.Exception.Message })
    [IO.File]::WriteAllText((Join-Path $Run 'first-error.log'),$Result.first_error,$Encoding)
} finally {
    $Result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Run 'generation-cost-result.json') -Encoding UTF8
}
if ($Failure) { Write-Host ('STOP: '+$Result.first_error); Write-Host ('Preserve '+$Run) }
else { Write-Host ('PASS: generation cost only. Review this run before stage4: '+$Run) }
if (-not (Get-Variable Git -ErrorAction SilentlyContinue)) { $Git = Find-GenerationGit }
if (-not (Share-GenerationResult $Run)) { exit 2 }
if ($Failure) { exit 1 }
exit 0
