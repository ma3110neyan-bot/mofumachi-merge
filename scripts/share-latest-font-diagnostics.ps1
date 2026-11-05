#requires -Version 5.1
[CmdletBinding()]
param()

# Publish existing diagnostics only. Never invoke Unity, capture or game tools.
$ShareRelative = 'References/Review/inbox/latest'
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$MaxFileBytes = 4MB
$MaxTotalBytes = 16MB
$MaxFiles = 64

function Assert-UnlinkedPath([string]$Path) {
    $At = [IO.Path]::GetFullPath($Path)
    while ($At) {
        if (Test-Path -LiteralPath $At) {
            if ((Get-Item -LiteralPath $At -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Linked path is not supported; nothing will be written there: $At"
            }
        }
        $Parent = [IO.Directory]::GetParent($At)
        if ($null -eq $Parent) { break }
        $At = $Parent.FullName
    }
}

function Assert-UnlinkedTree([string]$Path) {
    Assert-UnlinkedPath $Path
    $Pending = New-Object 'System.Collections.Generic.Stack[string]'
    $Pending.Push($Path)
    while ($Pending.Count) {
        foreach ($Item in Get-ChildItem -LiteralPath $Pending.Pop() -Force) {
            if ($Item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Existing inbox contains a link; preserving it: $($Item.FullName)"
            }
            if ($Item.PSIsContainer) { $Pending.Push($Item.FullName) }
        }
    }
}

function Test-FailedFontRun([IO.DirectoryInfo]$Run) {
    $InfoPath = Join-Path $Run.FullName 'font-only-source-info.json'
    if (Test-Path -LiteralPath $InfoPath -PathType Leaf) {
        $File = Get-Item -LiteralPath $InfoPath -Force
        if (-not ($File.Attributes -band [IO.FileAttributes]::ReparsePoint) -and $File.Length -le $MaxFileBytes) {
            try {
                $Info = [IO.File]::ReadAllText($InfoPath) | ConvertFrom-Json
                $Failure = $Info.PSObject.Properties['error']
                if ($Failure -and -not [string]::IsNullOrWhiteSpace([string]$Failure.Value)) { return $true }
                $Verified = $Info.PSObject.Properties['capture_verified']
                if ($Verified -and $Verified.Value -is [bool]) { return -not $Verified.Value }
            } catch { Write-Warning "Cannot classify run from JSON: $InfoPath" }
        }
    }
    foreach ($File in Get-ChildItem -LiteralPath $Run.FullName -File -Force) {
        if (($File.Attributes -band [IO.FileAttributes]::ReparsePoint) -or $File.Length -gt $MaxFileBytes) { continue }
        if ($File.Name -ieq 'capture.xml') {
            $Text = [IO.File]::ReadAllText($File.FullName)
            if ($Text -match '(?is)<test-run\b[^>]*\bresult\s*=\s*[\x22\x27]Failed(?:\([^)]*\))?[\x22\x27]') { return $true }
        } elseif ($File.Extension -in @('.log','.txt','.stdout','.stderr','.out','.err') -or $File.Name -in @('stdout','stderr')) {
            $Text = [IO.File]::ReadAllText($File.FullName)
            if ($Text -match '(?im)error\s+CS\d{4}\b|Scripts have compiler errors|ScriptCompilation[^\r\n]*failed|Test run completed[^\r\n]*code\s+[1-9]\d*|No valid Unity Editor license found|^Unhandled Exception') { return $true }
        }
    }
    return $false
}

function Find-LatestFailedFontRun([string]$Repository) {
    $Repository = [IO.Path]::GetFullPath($Repository)
    $Boundary = [IO.Directory]::GetParent($Repository)
    if ($null -eq $Boundary -or $null -eq $Boundary.Parent) { throw 'Refusing to search from a filesystem root.' }
    Assert-UnlinkedPath $Repository
    $Worktrees = New-Object 'System.Collections.Generic.List[IO.DirectoryInfo]'
    $Worktrees.Add((Get-Item -LiteralPath $Repository -Force))
    foreach ($Work in Get-ChildItem -LiteralPath $Boundary.FullName -Directory -Force) {
        if ($Work.FullName -ine $Repository -and -not ($Work.Attributes -band [IO.FileAttributes]::ReparsePoint)) { $Worktrees.Add($Work) }
    }
    $Candidates = New-Object 'System.Collections.Generic.List[object]'
    foreach ($Work in $Worktrees) {
        $Builds = Join-Path $Work.FullName 'Builds'
        $Diagnostics = Join-Path $Builds 'Diagnostics'
        $Linked = $false
        foreach ($Part in @($Builds,$Diagnostics)) {
            if ((Test-Path -LiteralPath $Part) -and ((Get-Item -LiteralPath $Part -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { $Linked = $true; break }
        }
        if ($Linked -or -not (Test-Path -LiteralPath $Diagnostics -PathType Container)) { continue }
        Assert-UnlinkedPath $Diagnostics
        foreach ($Run in Get-ChildItem -LiteralPath $Diagnostics -Directory -Filter 'font-only-*' -Force) {
            if ($Run.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
            $Candidates.Add([pscustomobject]@{SourceWorktree=$Work.FullName;Run=$Run;SearchBoundary=$Boundary.FullName})
        }
    }
    $Ordered = @($Candidates | Sort-Object -Property @{Expression={$_.Run.LastWriteTimeUtc};Descending=$true},@{Expression={$_.Run.FullName};Descending=$true})
    foreach ($Candidate in $Ordered) {
        if (Test-FailedFontRun $Candidate.Run) { return $Candidate }
    }
    throw "No failed font-only-* run found within $($Boundary.FullName) (current repository and immediate siblings only). Nothing was shared."
}

function Get-RunTextFiles([string]$Path, [Collections.Generic.List[string]]$Skipped) {
    $Pending = New-Object 'System.Collections.Generic.Stack[string]'
    $Pending.Push($Path)
    while ($Pending.Count) {
        foreach ($Item in Get-ChildItem -LiteralPath $Pending.Pop() -Force) {
            if ($Item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                $Skipped.Add(($Item.FullName.Substring($Path.Length + 1) + ' | linked path'))
                continue
            }
            if ($Item.PSIsContainer) {
                if ($Item.Name -notin @('Library','Temp','Build','Builds','bin','obj','.git','.utmp','screenshots','images')) {
                    $Pending.Push($Item.FullName)
                }
            } elseif ($Item.Extension.ToLowerInvariant() -in @('.log','.xml','.json','.txt','.stdout','.stderr','.out','.err') -or $Item.Name -in @('stdout','stderr')) {
                $Item
            }
        }
    }
}

function Test-TextBytes([string]$Path) {
    $Bytes = [IO.File]::ReadAllBytes($Path)
    try {
        # Preserve UTF-16/32 console logs, UTF-8 and legacy Japanese bytes as-is.
        if ($Bytes.Length -ge 4 -and $Bytes[0] -eq 255 -and $Bytes[1] -eq 254 -and $Bytes[2] -eq 0 -and $Bytes[3] -eq 0) {
            $Text = [Text.UTF32Encoding]::new($false,$true,$true).GetString($Bytes,4,$Bytes.Length-4)
        } elseif ($Bytes.Length -ge 4 -and $Bytes[0] -eq 0 -and $Bytes[1] -eq 0 -and $Bytes[2] -eq 254 -and $Bytes[3] -eq 255) {
            $Text = [Text.UTF32Encoding]::new($true,$true,$true).GetString($Bytes,4,$Bytes.Length-4)
        } elseif ($Bytes.Length -ge 2 -and $Bytes[0] -eq 255 -and $Bytes[1] -eq 254) {
            $Text = [Text.UnicodeEncoding]::new($false,$true,$true).GetString($Bytes,2,$Bytes.Length-2)
        } elseif ($Bytes.Length -ge 2 -and $Bytes[0] -eq 254 -and $Bytes[1] -eq 255) {
            $Text = [Text.UnicodeEncoding]::new($true,$true,$true).GetString($Bytes,2,$Bytes.Length-2)
        } else {
            $Text = [Text.Encoding]::GetEncoding(28591).GetString($Bytes)
        }
        return -not [regex]::IsMatch($Text, '[\x00-\x08\x0B\x0E-\x1A\x1C-\x1F]')
    } catch { return $false }
}

function Find-ShareGit {
    foreach ($Name in @('git.exe','git')) {
        $Command = Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($Command) { return $Command.Source }
    }
    if ($env:LOCALAPPDATA) {
        $Bundled = Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA 'GitHubDesktop/app-*/resources/app/git/cmd/git.exe') -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
        if ($Bundled) { return $Bundled.FullName }
    }
    throw 'Git was not found. Install Git for Windows or GitHub Desktop. Nothing was staged.'
}

function Invoke-ShareGit([string[]]$GitArguments, [switch]$AllowFailure, [switch]$WithoutLocalHooks) {
    $Options = @('-C',$Repo,'--literal-pathspecs','-c','core.quotepath=false','-c','push.followTags=false')
    if ($WithoutLocalHooks) { $Options += @('-c',('core.hooksPath=' + $ShareHooks)) }
    # Windows PowerShell 5.1 treats redirected native stderr as ErrorRecord objects.
    # Git progress on stderr is not a failure: use the native exit code instead.
    $SavedPreference = $ErrorActionPreference
    $SavedConsoleEncoding = [Console]::OutputEncoding
    $SavedOutputEncoding = $OutputEncoding
    $SavedTerminalPrompt = [Environment]::GetEnvironmentVariable('GIT_TERMINAL_PROMPT')
    try {
        # Git emits UTF-8 paths; PS5.1 otherwise decodes them as the OEM code page.
        [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
        $OutputEncoding = [Console]::OutputEncoding
        [Environment]::SetEnvironmentVariable('GIT_TERMINAL_PROMPT','0')
        $ErrorActionPreference = 'Continue'
        $Lines = @(& $GitExe @Options @GitArguments 2>&1 | ForEach-Object { $_.ToString() })
        $Code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $SavedPreference
        [Console]::OutputEncoding = $SavedConsoleEncoding
        $OutputEncoding = $SavedOutputEncoding
        [Environment]::SetEnvironmentVariable('GIT_TERMINAL_PROMPT',$SavedTerminalPrompt)
    }
    if ($Code -ne 0 -and -not $AllowFailure) {
        throw ("Git failed (exit {0}): git {1}`n{2}`nStopped; no automatic retry." -f $Code,($GitArguments -join ' '),($Lines -join "`n"))
    }
    return [pscustomobject]@{Code=$Code;Lines=$Lines}
}

function Get-ShareGitText([string[]]$GitArguments) {
    return ((Invoke-ShareGit -GitArguments $GitArguments).Lines -join "`n").Trim()
}

function Assert-InboxCommit([string]$Revision) {
    $Parents = (Get-ShareGitText @('rev-list','--parents','-n','1',$Revision)) -split ' '
    if ($Parents.Count -gt 2) { throw 'An unpushed merge commit exists. It will not be published by this tool.' }
    $Result = Invoke-ShareGit -GitArguments @('diff-tree','--root','--no-commit-id','--name-only','-r','-z',$Revision)
    $Paths = @(($Result.Lines -join "`n").Split([char]0) | Where-Object { $_.Length -gt 0 })
    if ($Paths.Count -eq 0) { throw 'An empty unpushed commit exists. It will not be published by this tool.' }
    foreach ($Path in $Paths) {
        if (-not $Path.StartsWith(($ShareRelative + '/'),[StringComparison]::Ordinal)) {
            throw "Unpushed changes outside the diagnostics inbox exist: $Path. Nothing will be pushed."
        }
    }
}

function Initialize-SharePublication {
    foreach ($Name in @('GIT_DIR','GIT_WORK_TREE','GIT_COMMON_DIR','GIT_INDEX_FILE','GIT_OBJECT_DIRECTORY','GIT_ALTERNATE_OBJECT_DIRECTORIES')) {
        if ([Environment]::GetEnvironmentVariable($Name)) { throw "Custom $Name is set. Publication stopped without changing it." }
    }
    $Top = Get-ShareGitText @('rev-parse','--show-toplevel')
    if ([IO.Path]::GetFullPath($Top).TrimEnd([char[]]'\/') -ine $Repo.TrimEnd([char[]]'\/')) {
        throw 'The script directory is not the Git repository root. Nothing was staged.'
    }
    $BranchResult = Invoke-ShareGit -GitArguments @('symbolic-ref','--quiet','--short','HEAD') -AllowFailure
    if ($BranchResult.Code -ne 0) { throw 'Detached HEAD: run this launcher in a repository checked out on its feature branch. No branch was switched.' }
    $Branch = ($BranchResult.Lines -join "`n").Trim()
    if ($Branch -notmatch '^(feat|feature)/') { throw "Expected a feature branch; current branch is $Branch. No branch was switched." }
    foreach ($State in @('MERGE_HEAD','CHERRY_PICK_HEAD','REVERT_HEAD','rebase-merge','rebase-apply')) {
        $StatePath = Get-ShareGitText @('rev-parse','--git-path',$State)
        if (-not [IO.Path]::IsPathRooted($StatePath)) { $StatePath = Join-Path $Repo $StatePath }
        if (Test-Path -LiteralPath $StatePath) { throw "A Git operation is in progress ($State). Existing changes are preserved." }
    }
    if (Get-ShareGitText @('ls-files','--unmerged')) { throw 'Unmerged index entries exist. Existing changes are preserved.' }
    $Head = Get-ShareGitText @('rev-parse','HEAD')
    $null = Invoke-ShareGit -GitArguments @('fetch','--no-tags','--no-recurse-submodules','origin',('refs/heads/' + $Branch))
    $RemoteHead = Get-ShareGitText @('rev-parse','FETCH_HEAD')
    if ($RemoteHead -ne $Head) {
        $Ancestor = Invoke-ShareGit -GitArguments @('merge-base','--is-ancestor',$RemoteHead,$Head) -AllowFailure
        if ($Ancestor.Code -ne 0) { throw 'The remote feature branch is newer or diverged. No merge, reset or retry was performed; existing changes are preserved.' }
        $Pending = Get-ShareGitText @('rev-list',($RemoteHead + '..' + $Head))
        foreach ($Revision in ($Pending -split "`n")) { Assert-InboxCommit $Revision }
    }
    return [pscustomobject]@{Branch=$Branch;Head=$Head}
}

function Publish-DiagnosticsInbox($Context, [string]$Latest, [string]$RunName) {
    if ((Get-ShareGitText @('rev-parse','HEAD')) -ne $Context.Head -or
        (Get-ShareGitText @('symbolic-ref','--quiet','--short','HEAD')) -ne $Context.Branch) {
        throw 'HEAD or branch changed while copying. Nothing was staged or pushed.'
    }
    # Force-add only this literal directory so ignored .log files are included.
    $null = Invoke-ShareGit -GitArguments @('add','--force','--',$ShareRelative) -WithoutLocalHooks
    foreach ($File in Get-ChildItem -LiteralPath $Latest -Recurse -File -Force) {
        $Relative = $ShareRelative + '/' + $File.FullName.Substring($Latest.Length + 1).Replace('\','/')
        $Raw = Get-ShareGitText @('hash-object','--no-filters','--',$File.FullName)
        $Staged = Get-ShareGitText @('rev-parse','--verify',(':' + $Relative))
        if ($Raw -ne $Staged) { throw "Git transformed diagnostic bytes: $Relative. No commit or push was performed." }
    }
    $Diff = Invoke-ShareGit -GitArguments @('diff','--cached','--quiet','--exit-code','--',$ShareRelative) -AllowFailure
    if ($Diff.Code -eq 1) {
        # --only excludes all unrelated staged entries while preserving them.
        $null = Invoke-ShareGit -GitArguments @('commit','--only','--no-gpg-sign','-m',('chore: share font diagnostics ' + $RunName),'--',$ShareRelative) -WithoutLocalHooks
    } elseif ($Diff.Code -ne 0) { throw 'Cannot inspect staged diagnostics. No commit or push was performed.' }
    $Revision = Get-ShareGitText @('rev-parse','HEAD')
    if ($Revision -ne $Context.Head) {
        if ((Get-ShareGitText @('rev-parse',($Revision + '^'))) -ne $Context.Head) { throw 'Unexpected commit parent. Nothing was pushed.' }
        Assert-InboxCommit $Revision
    }
    if ((Get-ShareGitText @('symbolic-ref','--quiet','--short','HEAD')) -ne $Context.Branch) { throw 'Feature branch changed. Nothing was pushed.' }
    # Exact SHA and branch, no force, no tags, no retries. Server hooks still apply.
    $null = Invoke-ShareGit -GitArguments @('push','--porcelain','--no-verify','origin',($Revision + ':refs/heads/' + $Context.Branch))
    Write-Host ("Published diagnostics to origin/{0} at {1}" -f $Context.Branch,$Revision)
}

function Invoke-FontDiagnosticShare {
$Repo = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$Selection = Find-LatestFailedFontRun $Repo
$Run = $Selection.Run
Assert-UnlinkedPath $Run.FullName
$Skipped = New-Object 'System.Collections.Generic.List[string]'
$Files = @(Get-RunTextFiles $Run.FullName $Skipped | Sort-Object -Property @{Expression={
    if ($_.Name -in @('capture.log','capture.xml','font-only-source-info.json')) { 0 }
    elseif ($_.Name -match 'editor|stdout|stderr|compil|batchmode') { 1 }
    else { 2 }
}},FullName)
if ($Files.Count -eq 0) { throw 'Latest run has no eligible diagnostic text. Existing inbox is unchanged.' }

$GitExe = Find-ShareGit
$Publication = Initialize-SharePublication

$Inbox = Join-Path $Repo 'References/Review/inbox'
$Latest = Join-Path $Inbox 'latest'
Assert-UnlinkedPath $Latest
if (Test-Path -LiteralPath $Latest) {
    if (-not (Test-Path -LiteralPath $Latest -PathType Container)) { throw 'Inbox latest is not a directory.' }
    Assert-UnlinkedTree $Latest
}
$null = [IO.Directory]::CreateDirectory($Inbox)
$Stage = Join-Path $Inbox ('.font-diagnostics-stage-' + [guid]::NewGuid().ToString('N'))
$Backup = Join-Path $Inbox ('.font-diagnostics-previous-' + [guid]::NewGuid().ToString('N'))
$Copied = New-Object 'System.Collections.Generic.List[object]'
$TotalBytes = 0L
try {
    $null = [IO.Directory]::CreateDirectory($Stage)
    foreach ($File in $Files) {
        $Relative = $File.FullName.Substring($Run.FullName.Length + 1)
        $Reason = $null
        if ($Relative -ieq 'run-info.txt') { $Reason = 'reserved provenance filename' }
        elseif ($File.Length -gt $MaxFileBytes) { $Reason = 'over per-file size limit' }
        elseif ($Copied.Count -ge $MaxFiles) { $Reason = 'over file-count limit' }
        elseif ($TotalBytes + $File.Length -gt $MaxTotalBytes) { $Reason = 'over total size limit' }
        elseif (-not (Test-TextBytes $File.FullName)) { $Reason = 'binary or invalid Unicode text' }
        if ($Reason) { $Skipped.Add(($Relative + ' | ' + $Reason)); continue }
        Assert-UnlinkedPath $File.FullName
        $SourceHash = (Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $Target = Join-Path $Stage $Relative
        $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $Target))
        [IO.File]::Copy($File.FullName,$Target,$false)
        $Bytes = (Get-Item -LiteralPath $Target -Force).Length
        if ($Bytes -ne $File.Length -or (Get-FileHash -LiteralPath $Target -Algorithm SHA256).Hash -ine $SourceHash) {
            throw "Source changed while copying; existing inbox preserved: $Relative"
        }
        $Copied.Add([pscustomobject]@{Relative=$Relative;Source=$File.FullName;Bytes=$Bytes;Sha256=$SourceHash})
        $TotalBytes += $Bytes
    }
    if ($Copied.Count -eq 0) { throw 'No small diagnostic text was copied. Existing inbox is unchanged.' }
    foreach ($File in $Copied) {
        Assert-UnlinkedPath $File.Source
        if ((Get-FileHash -LiteralPath $File.Source -Algorithm SHA256).Hash -ine $File.Sha256) {
            throw "Source changed before publication; existing inbox preserved: $($File.Relative)"
        }
    }
    $Info = New-Object 'System.Collections.Generic.List[string]'
    $Info.Add('Source run folder: ' + $Run.Name)
    $Info.Add('Source Windows path: ' + $Run.FullName)
    $Info.Add('Source repository/worktree: ' + $Selection.SourceWorktree)
    $Info.Add('Source run full path: ' + $Run.FullName)
    $Info.Add('Selected run name: ' + $Run.Name)
    $Info.Add('Search boundary: ' + $Selection.SearchBoundary)
    $Info.Add('Source folder modified UTC: ' + $Run.LastWriteTimeUtc.ToString('o'))
    $Info.Add('Copied UTC: ' + [DateTime]::UtcNow.ToString('o'))
    $Info.Add('Limits: 4 MiB per file; 16 MiB total; 64 files. No truncation.')
    $Info.Add('Copied files (relative path | bytes | SHA256):')
    foreach ($File in $Copied) { $Info.Add(('{0} | {1} | {2}' -f $File.Relative,$File.Bytes,$File.Sha256)) }
    $Info.Add('Skipped eligible paths (if any):')
    foreach ($Line in $Skipped) { $Info.Add($Line) }
    $Info.Add('Publication target: origin/refs/heads/' + $Publication.Branch)
    $Info.Add('Publishing repository HEAD: ' + $Publication.Head)
    [IO.File]::WriteAllLines((Join-Path $Stage 'run-info.txt'),$Info.ToArray(),[Text.UTF8Encoding]::new($true))
    # Preserve raw evidence bytes even with core.autocrlf or parent attributes.
    [IO.File]::WriteAllText((Join-Path $Stage '.gitattributes'),"* -text -filter -ident -working-tree-encoding`n",[Text.UTF8Encoding]::new($false))
    Assert-UnlinkedPath $Latest
    if (Test-Path -LiteralPath $Latest) {
        Assert-UnlinkedTree $Latest
        [IO.Directory]::Move($Latest,$Backup)
    }
    try { [IO.Directory]::Move($Stage,$Latest) }
    catch {
        if ((Test-Path -LiteralPath $Backup) -and -not (Test-Path -LiteralPath $Latest)) { [IO.Directory]::Move($Backup,$Latest) }
        throw
    }
    if (Test-Path -LiteralPath $Backup) {
        try { [IO.Directory]::Delete($Backup,$true) }
        catch { Write-Warning "New inbox is ready; old inbox retained at $Backup" }
    }
} finally {
    if (Test-Path -LiteralPath $Stage) { [IO.Directory]::Delete($Stage,$true) }
}
Write-Host ("Copied {0} diagnostic text files ({1} bytes) from {2} to {3}" -f $Copied.Count,$TotalBytes,$Run.Name,$Latest)
if ($Skipped.Count) { Write-Warning 'Some paths were skipped; see run-info.txt for reasons.' }
Publish-DiagnosticsInbox $Publication $Latest $Run.Name
}

$ShareHooks = Join-Path ([IO.Path]::GetTempPath()) ('font-share-hooks-' + [guid]::NewGuid().ToString('N'))
$null = [IO.Directory]::CreateDirectory($ShareHooks)
try { Invoke-FontDiagnosticShare }
finally { if (Test-Path -LiteralPath $ShareHooks) { [IO.Directory]::Delete($ShareHooks,$true) } }
