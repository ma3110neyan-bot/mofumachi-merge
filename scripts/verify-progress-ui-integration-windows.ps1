#requires -Version 5.1
[CmdletBinding()]
param()
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProductCommit = '46f65b24d58531d5934035fbe4cdb4d5b00104a6'
$Fixture = 'Mofumachi.Tests.ProgressUiIntegrationTests'
$ValidationId = 'progress-ui-e-drive-v3'
$ValidationRoot = 'E:\Mofumachi-Validation'
$FixtureCounts = [ordered]@{
    'Mofumachi.Tests.SaveServiceTests' = 33
    'Mofumachi.Tests.PreferencesTests' = 23
    'Mofumachi.Tests.CharacterPreferenceTests' = 7
    'Mofumachi.Tests.MofuPointServiceTests' = 68
    'Mofumachi.Tests.GenerationCostTests' = 33
    'Mofumachi.Tests.TownPointRewardTests' = 69
    'Mofumachi.Tests.RecoveryExtraHelpTests' = 71
    'Mofumachi.Tests.QuestTownLevelTests' = 85
    'Mofumachi.Tests.ProgressUiIntegrationTests' = 55
}
$EvidenceNames = @('progress-ui-integration-result.json','source-revision.txt','first-error.log',
    'direct/results.xml','direct/unity.log','direct/unity-arguments.txt',
    'integration/results.xml','integration/unity.log','integration/unity-arguments.txt',
    'history/preflight.json','history/v1-receipt.txt','history/v1-result.json','history/v1-source.txt','history/v1-error.log',
    'history/v2-receipt.txt','history/v2-result.json','history/v2-source.txt','history/v2-error.log')
$script:FetchedProgressUiCheckHead = $null

function Get-ProgressUiCheckPaths([string]$Root,[string]$RunId) {
    $Root = [IO.Path]::GetFullPath($Root)
    $Label = 'progress-ui-integration-'+$ValidationId+'-'+$RunId
    $Projects = Join-Path (Join-Path $Root 'projects') $Label
    return @{
        Root=$Root;DiagnosticsRoot=(Join-Path $Root 'Diagnostics')
        Run=(Join-Path (Join-Path $Root 'Diagnostics') $Label)
        Once=(Join-Path (Join-Path $Root 'receipts') ('progress-ui-integration-'+$ProductCommit+'-'+$ValidationId+'.attempted'))
        DirectProject=(Join-Path $Projects 'direct');IntegrationProject=(Join-Path $Projects 'integration')
        TempRoot=(Join-Path (Join-Path $Root 'temp') $Label)
        PackageCache=(Join-Path $Root 'upm-cache')
    }
}
function Read-ProgressUiCheckAttempt([string]$Directory,[string]$DiagnosticsRoot,[string]$Variant) {
    $Attempt=@{is_terminal=$false;classification='pre-Unity/incomplete attempt';run_path=$Directory;result_path=$null;saved=$null;reason='invalid_receipt_path'}
    if([string]::IsNullOrWhiteSpace($Directory)){return $Attempt}
    try {
        $Full=[IO.Path]::GetFullPath($Directory)
        $Boundary=[IO.Path]::GetFullPath($DiagnosticsRoot).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
        if(-not $Full.StartsWith($Boundary,[StringComparison]::OrdinalIgnoreCase)){return $Attempt}
    } catch {return $Attempt}
    $Attempt.run_path=$Full;$Attempt.result_path=Join-Path $Full 'progress-ui-integration-result.json'
    if(-not(Test-Path -LiteralPath $Attempt.result_path -PathType Leaf)){$Attempt.reason='missing_terminal_result_json';return $Attempt}
    try {
        $Saved=[IO.File]::ReadAllText($Attempt.result_path)|ConvertFrom-Json
        foreach($Name in @('source_commit','validation_id','result','validation_passed')){
            if($null -eq $Saved -or $null -eq $Saved.PSObject.Properties[$Name]){$Attempt.reason='invalid_terminal_result_json';return $Attempt}
        }
        if($Saved.source_commit -cne $ProductCommit -or $Saved.validation_id -cne $Variant){$Attempt.reason='result_identity_mismatch';return $Attempt}
        if($Saved.result -notin @('Passed','Failed','Blocked')){$Attempt.reason='non_terminal_result_json';return $Attempt}
        if($Saved.validation_passed -isnot [bool] -or (($Saved.result -ceq 'Passed') -ne $Saved.validation_passed)){$Attempt.reason='invalid_terminal_result_json';return $Attempt}
        $Attempt.is_terminal=$true;$Attempt.classification='saved terminal result';$Attempt.saved=$Saved;$Attempt.reason=$null
    } catch {$Attempt.reason='unreadable_terminal_result_json'}
    return $Attempt
}
function Write-ProgressUiCheckIncompleteStop([string]$Receipt,$Attempt) {
    # This observation is separate from the original run and is not a native test result.
    $Folder=Join-Path $ValidationRoot 'attempt-stops';$null=[IO.Directory]::CreateDirectory($Folder)
    $Path=Join-Path $Folder ('incomplete-attempt-'+[guid]::NewGuid().ToString('N')+'.json')
    $Report=[ordered]@{classification='pre-Unity/incomplete attempt';reason=$Attempt.reason;receipt_path=$Receipt;
        source_run=$Attempt.run_path;expected_result_path=$Attempt.result_path;source_commit=$ProductCommit;validation_id=$ValidationId;
        native_state='unconfirmed';unity_invocations_this_detection=0;automatic_retries=0;
        validation_passed=$false;observed_at_utc=[DateTime]::UtcNow.ToString('o')}
    $Encoding=New-Object Text.UTF8Encoding($true);$Bytes=$Encoding.GetBytes(($Report|ConvertTo-Json -Depth 5))
    $Stream=[IO.File]::Open($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$Stream.Write($Bytes,0,$Bytes.Length);$Stream.Flush()}finally{$Stream.Dispose()}
    Write-Host ('STOP: pre-Unity/incomplete attempt ('+$Attempt.reason+'). No saved terminal result; no new Unity, checkout or retry. Original files retained. Record: '+$Path)
}
function Get-ProgressUiCheckPreviousAttempt($Paths,[string]$Variant) {
    $Receipt=Join-Path (Join-Path $Paths.Root 'receipts') ('progress-ui-integration-'+$ProductCommit+'-'+$Variant+'.attempted')
    $Prior=[ordered]@{validation_id=$Variant;receipt_path=$Receipt;source_run=$null;saved_state=$null;
        files=@();blocked_reasons=@();observation='Read-only; original receipt, Diagnostics and stop records retained'}
    try {
        if(-not(Test-Path -LiteralPath $Receipt -PathType Leaf)){throw ('Missing old receipt: '+$Receipt)}
        $Run=[IO.File]::ReadAllText($Receipt).Trim()
        $Attempt=Read-ProgressUiCheckAttempt $Run $Paths.DiagnosticsRoot $Variant
        if($Attempt.reason -eq 'invalid_receipt_path'){throw ('Old receipt is outside E Diagnostics or empty: '+$Receipt)}
        $Prior.source_run=$Attempt.run_path
        if(-not(Test-Path -LiteralPath $Prior.source_run -PathType Container)){throw ('Missing old Diagnostics directory: '+$Prior.source_run)}
        $Short=$(if($Variant -ceq 'progress-ui-e-drive-v1'){'v1'}else{'v2'})
        $Names=@(@{source=$Receipt;relative=('history/'+$Short+'-receipt.txt')},
            @{source=$Attempt.result_path;relative=('history/'+$Short+'-result.json')},
            @{source=(Join-Path $Prior.source_run 'source-revision.txt');relative=('history/'+$Short+'-source.txt')},
            @{source=(Join-Path $Prior.source_run 'first-error.log');relative=('history/'+$Short+'-error.log')})
        foreach($Row in $Names){
            if(-not(Test-Path -LiteralPath $Row.source -PathType Leaf)){continue}
            $File=Get-Item -LiteralPath $Row.source -Force
            if($File.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Linked old evidence refused: '+$Row.source)}
            $Prior.files+=@{source=$Row.source;relative=$Row.relative;bytes=$File.Length;sha256=(Get-FileHash -LiteralPath $Row.source -Algorithm SHA256).Hash}
        }
        foreach($Name in @('direct/results.xml','integration/results.xml','direct/unity.log','integration/unity.log')){
            if(Test-Path -LiteralPath (Join-Path $Prior.source_run $Name) -PathType Leaf){
                $Prior.blocked_reasons+=('Old Unity evidence contradicts the unexecuted history: '+(Join-Path $Prior.source_run $Name))
            }
        }
        $Revision=Join-Path $Prior.source_run 'source-revision.txt'
        if((Test-Path -LiteralPath $Revision -PathType Leaf) -and [IO.File]::ReadAllText($Revision).Trim() -cne $ProductCommit){
            $Prior.blocked_reasons+=('Old source revision differs: '+$Revision)
        }
        if(Test-Path -LiteralPath $Attempt.result_path -PathType Leaf){
            $Saved=[IO.File]::ReadAllText($Attempt.result_path)|ConvertFrom-Json
            $Prior.saved_state=@{result=$Saved.result;validation_passed=$Saved.validation_passed;unity_invocations=$Saved.unity_invocations;
                direct=$Saved.direct;integration=$Saved.integration;first_error=$Saved.first_error;runner_error=$Saved.runner_error}
            if($Saved.source_commit -cne $ProductCommit -or $Saved.validation_id -cne $Variant){throw ('Old result identity differs: '+$Attempt.result_path)}
            if($Saved.validation_passed -isnot [bool] -or $Saved.validation_passed -ne $false -or $Saved.unity_invocations -ne 0 -or
                $Saved.result -notin @('NotRun','Preparing','Blocked')){throw ('Old result is not a confirmed pre-Unity attempt: '+$Attempt.result_path)}
            foreach($Phase in @('direct','integration')){
                if($Saved.$Phase.unity_started -isnot [bool] -or $Saved.$Phase.unity_started -ne $false -or
                    $Saved.$Phase.unity_invocations -ne 0 -or $Saved.$Phase.result -notin @('NotRun','Blocked')){
                    throw ('Old '+$Phase+' state contradicts no Unity start: '+$Attempt.result_path)
                }
            }
            if($Short -ceq 'v2' -and ($Saved.result -cne 'Preparing' -or $Saved.direct.result -cne 'NotRun' -or
                $Saved.integration.result -cne 'NotRun' -or $null -ne $Saved.first_error -or $null -ne $Saved.runner_error)){
                throw ('V2 differs from the reported Preparing/NotRun state: '+$Attempt.result_path)
            }
        } elseif($Short -ceq 'v2'){throw ('Missing reported v2 Preparing JSON: '+$Attempt.result_path)}
    } catch {$Prior.blocked_reasons+=$_.Exception.Message}
    return $Prior
}
function Read-ProgressUiCheckRestartState($Paths) {
    $Check=[ordered]@{observed_at_utc=[DateTime]::UtcNow.ToString('o');repository=$Repository;branch=$null;head=$null;
        working_tree=$null;worktree_list=$null;previous_attempts=@();diagnostics=@();attempt_stops=@();files=@();blocked_reasons=@();passed=$false}
    try {
        $Check.branch=Invoke-ProgressUiCheckGit @('branch','--show-current')
        $Check.head=Invoke-ProgressUiCheckGit @('rev-parse','HEAD')
        $Check.working_tree=Invoke-ProgressUiCheckGit @('--no-optional-locks','status','--porcelain=v1','--untracked-files=all')
        $Check.worktree_list=Invoke-ProgressUiCheckGit @('worktree','list','--porcelain')
        if($Check.branch -cne 'feat/daily-world-pass-20261016'){$Check.blocked_reasons+='Formal repository is not on the requested feature branch.'}
        if($Check.working_tree){$Check.blocked_reasons+='Formal repository is not clean; no changes were reset, stashed or discarded.'}
        if($Check.worktree_list -match 'progress-ui-e-drive-v[12]'){$Check.blocked_reasons+='Old v1/v2 worktree registration still exists; no automatic cleanup.'}
    } catch {$Check.blocked_reasons+=$_.Exception.Message}
    foreach($Variant in @('progress-ui-e-drive-v1','progress-ui-e-drive-v2')){
        $Prior=Get-ProgressUiCheckPreviousAttempt $Paths $Variant
        $Check.previous_attempts+=$Prior;$Check.files+=@($Prior.files);$Check.blocked_reasons+=@($Prior.blocked_reasons)
    }
    try {
        $Check.diagnostics=@(Get-ChildItem -LiteralPath $Paths.DiagnosticsRoot -Directory |
            Where-Object {$_.Name -match 'progress-ui-e-drive-v[12]-'} | ForEach-Object {$_.FullName})
        $Stops=Join-Path $Paths.Root 'attempt-stops'
        if(Test-Path -LiteralPath $Stops -PathType Container){
            foreach($File in (Get-ChildItem -LiteralPath $Stops -File -Filter '*.json')){
                $Text=[IO.File]::ReadAllText($File.FullName);$Saved=$Text|ConvertFrom-Json
                if($null -ne $Saved.PSObject.Properties['source_commit'] -and $null -ne $Saved.PSObject.Properties['validation_id'] -and
                    $Saved.source_commit -ceq $ProductCommit -and $Saved.validation_id -in @('progress-ui-e-drive-v1','progress-ui-e-drive-v2')){
                    $Check.attempt_stops+=@{path=$File.FullName;bytes=$File.Length;sha256=(Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash;raw_text=$Text}
                }
            }
        }
    } catch {$Check.blocked_reasons+=('Cannot read old Diagnostics/attempt-stops: '+$_.Exception.Message)}
    $Check.passed=($Check.blocked_reasons.Count -eq 0)
    return $Check
}
function Save-ProgressUiCheckRestartState([string]$Directory,$Check) {
    # Copies only into this NEW Diagnostics run. Original histories are never written.
    $Total=0L
    foreach($Row in $Check.files){
        $Total+=$Row.bytes
        if($Row.bytes -gt 4MB -or $Total -gt 12MB){throw 'Old history exceeds the small evidence budget; originals retained; no Unity start.'}
        if((Get-Item -LiteralPath $Row.source).Length -ne $Row.bytes -or
            (Get-FileHash -LiteralPath $Row.source -Algorithm SHA256).Hash -cne $Row.sha256){throw 'Old history changed after inspection; no Unity start.'}
        $Target=Join-Path $Directory $Row.relative
        $null=[IO.Directory]::CreateDirectory((Split-Path -Parent $Target))
        [IO.File]::Copy($Row.source,$Target,$false)
        if((Get-FileHash -LiteralPath $Target -Algorithm SHA256).Hash -cne $Row.sha256 -or
            (Get-FileHash -LiteralPath $Row.source -Algorithm SHA256).Hash -cne $Row.sha256){throw 'Old history copy differs; originals retained; no Unity start.'}
    }
    $Target=Join-Path (Join-Path $Directory 'history') 'preflight.json'
    $null=[IO.Directory]::CreateDirectory((Split-Path -Parent $Target))
    $Bytes=(New-Object Text.UTF8Encoding($true)).GetBytes(($Check|ConvertTo-Json -Depth 12))
    if($Bytes.Length -gt 4MB){throw 'Read-only history snapshot exceeds the small evidence budget; no Unity start.'}
    $Stream=[IO.File]::Open($Target,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$Stream.Write($Bytes,0,$Bytes.Length);$Stream.Flush()}finally{$Stream.Dispose()}
}
function Invoke-ProgressUiCheckGit([string[]]$Arguments) {
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
function Read-ProgressUiCheckXml([string]$Path) {
    $Settings = New-Object Xml.XmlReaderSettings
    $Settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit; $Settings.XmlResolver = $null
    $Reader = [Xml.XmlReader]::Create($Path, $Settings)
    try { $Doc = New-Object Xml.XmlDocument; $Doc.XmlResolver = $null; $Doc.Load($Reader); return $Doc }
    finally { $Reader.Dispose() }
}
function Get-ProgressUiCheckFirstError([string]$Directory) {
    $Log = Join-Path $Directory 'unity.log'
    if (Test-Path -LiteralPath $Log) {
        $Lines = [IO.File]::ReadAllLines($Log)
        for ($I = 0; $I -lt $Lines.Length; $I++) {
            if ($Lines[$I] -match 'error\s+CS\d{4}|No valid Unity Editor license|No valid license found|Failed to initialize graphics|Build target.*not supported|Unhandled Exception|Fatal error|^Crash!!!|Unexpected error in Burst compilation') {
                $End = [Math]::Min($Lines.Length - 1, $I + 5)
                return ($Lines[$I..$End] -join "`n")
            }
        }
    }
    $Xml = Join-Path $Directory 'results.xml'
    if (Test-Path -LiteralPath $Xml) {
        try {
            $Doc = Read-ProgressUiCheckXml $Xml
            $Failures = @($Doc.SelectNodes('//test-case[failure]') | Sort-Object { $_.GetAttribute('start-time') })
            if (-not $Failures.Count) { $Failures = @($Doc.SelectNodes('//test-suite[failure]')) }
            if ($Failures.Count) { return ($Failures[0].GetAttribute('fullname') + "`n" + $Failures[0].SelectSingleNode('failure').InnerText) }
        } catch { }
    }
    return 'Unity root error unconfirmed. Preserve this run; no automatic retry.'
}
function Get-ProgressUiCheckArguments([string]$Project,[string]$Directory,[string]$Phase) {
    $Names = @($Fixture)
    if ($Phase -eq 'integration') { $Names = @($FixtureCounts.Keys) }
    elseif ($Phase -ne 'direct') { throw 'Unknown verification phase.' }
    $Filters = @($Names | ForEach-Object { '^' + [regex]::Escape($_) + '\..*' })
    return @('-batchmode','-nographics','--burst-disable-compilation','-projectPath',$Project,'-buildTarget','Android',
        '-runTests','-testPlatform','EditMode','-assemblyNames','Mofumachi.EditModeTests','-testFilter',($Filters -join ';'),
        '-testResults',(Join-Path $Directory 'results.xml'),'-logFile',(Join-Path $Directory 'unity.log'))
}
function Find-ProgressUiCheckGit {
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
function Get-ProgressUiCheckRemoteHead {
    if (-not $script:FetchedProgressUiCheckHead) {
        $null = Invoke-ProgressUiCheckGit @('fetch','origin','refs/heads/feat/daily-world-pass-20261016','--no-tags','--no-recurse-submodules')
        $script:FetchedProgressUiCheckHead = Invoke-ProgressUiCheckGit @('rev-parse','FETCH_HEAD')
    }
    return $script:FetchedProgressUiCheckHead
}
function Preserve-ProgressUiCheckInbox([string]$Publication, [string]$Relative, [string]$Directory) {
    if (-not (Invoke-ProgressUiCheckGit @('-C',$Publication,'ls-tree','-r','--name-only','HEAD','--',$Relative))) { return }
    # Restore only the evidence scope into this isolated sharing worktree, including prior archives.
    $null = Invoke-ProgressUiCheckGit @('-C',$Publication,'checkout','HEAD','--',$Relative)
    $Inbox = Join-Path $Publication $Relative
    $InfoPath = Join-Path $Inbox 'run-info.json'
    if (-not (Test-Path -LiteralPath $InfoPath -PathType Leaf)) { throw 'Previous evidence has no run identity; sharing stopped without overwriting it.' }
    $Previous = [IO.File]::ReadAllText($InfoPath) | ConvertFrom-Json
    if ($Previous.source_run -ceq $Directory) { return }
    $Name = ($Previous.source_run -split '[\\/]')[-1]
    if ($Name -cnotmatch '^progress-ui-integration-[a-zA-Z0-9-]+$') { throw 'Unsafe previous run name; evidence retained.' }
    $Archive = Join-Path (Join-Path $Inbox 'previous-runs') $Name
    $null = [IO.Directory]::CreateDirectory($Archive)
    foreach ($FileName in @($EvidenceNames + @('run-info.json','.gitattributes'))) {
        $Source = Join-Path $Inbox $FileName
        if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) { continue }
        $Target = Join-Path $Archive $FileName
        $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $Target))
        $Hash = (Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash
        if (-not (Test-Path -LiteralPath $Target)) { [IO.File]::Copy($Source,$Target,$false) }
        if ((Get-FileHash -LiteralPath $Target -Algorithm SHA256).Hash -cne $Hash) { throw 'Previous evidence archive differs; nothing overwritten or pushed.' }
    }
    # These are copies in the sharing worktree only. Original Diagnostics and archives remain intact.
    foreach ($FileName in @($EvidenceNames + @('run-info.json','.gitattributes'))) {
        $Path = Join-Path $Inbox $FileName
        if (Test-Path -LiteralPath $Path -PathType Leaf) { [IO.File]::Delete($Path) }
    }
}
function Publish-ProgressUiCheckResult([string]$Directory) {
    $Branch = Invoke-ProgressUiCheckGit @('branch','--show-current')
    if ($Branch -cne 'feat/daily-world-pass-20261016') { throw 'Sharing stopped: original feature branch changed; results retained.' }
    $Native = [IO.File]::ReadAllText((Join-Path $Directory 'progress-ui-integration-result.json')) | ConvertFrom-Json
    if ($Native.source_commit -cne $ProductCommit -or $Native.result -notin @('Passed','Failed','Blocked')) { throw 'No terminal result for the pinned source. Do not start another Unity process.' }
    if ($Native.validation_id -cne $ValidationId) { throw 'The saved result belongs to another verification variant; Unity was not restarted.' }
    $Remote = Get-ProgressUiCheckRemoteHead
    $Publication = Join-Path (Join-Path $ValidationRoot 'sharing') ([guid]::NewGuid().ToString('N'))
    $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $Publication))
    $null = Invoke-ProgressUiCheckGit @('worktree','add','--detach','--no-checkout',$Publication,$Remote)
    $null = Invoke-ProgressUiCheckGit @('-C',$Publication,'read-tree','HEAD')
    $Relative = 'References/Review/inbox/latest-progress-ui-integration'
    Preserve-ProgressUiCheckInbox $Publication $Relative $Directory
    $Inbox = Join-Path $Publication $Relative
    $null = [IO.Directory]::CreateDirectory($Inbox)
    $Rows = @(); $Skipped = @(); $TotalBytes = 0L
    foreach ($Name in $EvidenceNames) {
        $Source = Join-Path $Directory $Name
        if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) { continue }
        $File = Get-Item -LiteralPath $Source -Force
        if ($File.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Refuse linked evidence; original results retained.' }
        if ($File.Length -gt 4MB -or $TotalBytes + $File.Length -gt 16MB) {
            if ($Name.EndsWith('/unity.log',[StringComparison]::Ordinal)) { $Skipped += @{file=$Name;bytes=$File.Length;reason='Full log retained locally; XML/JSON/first error shared'}; continue }
            throw ('Required evidence exceeds sharing budget: '+$Name+'. Original results retained.')
        }
        $Hash = (Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash.ToLowerInvariant()
        $Target = Join-Path $Inbox $Name
        $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $Target))
        [IO.File]::Copy($Source,$Target,$false)
        if ((Get-FileHash -LiteralPath $Target -Algorithm SHA256).Hash -ine $Hash -or
            (Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash -ine $Hash) { throw ('Evidence changed while copying: '+$Name) }
        $Rows += @{file=$Name;bytes=$File.Length;sha256=$Hash}; $TotalBytes += $File.Length
    }
    $Info = [ordered]@{source_run=$Directory;source_commit=$ProductCommit;source_repository=$Repository;validation_root=$ValidationRoot;publication_worktree=$Publication;fixture=$Fixture;requested_fixtures=@($FixtureCounts.Keys);validation_id=$ValidationId;copied_at_utc=[DateTime]::UtcNow.ToString('o');remote_parent=$Remote;files=$Rows;skipped=$Skipped}
    $Info | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Inbox 'run-info.json') -Encoding UTF8
    [IO.File]::WriteAllText((Join-Path $Inbox '.gitattributes'),"* -text -filter -ident -working-tree-encoding`n",(New-Object Text.UTF8Encoding($false)))
    $null = Invoke-ProgressUiCheckGit @('-C',$Publication,'add','--force','--',$Relative)
    foreach ($Row in $Rows) {
        $Raw = Invoke-ProgressUiCheckGit @('-C',$Publication,'hash-object','--no-filters','--',(Join-Path $Inbox $Row.file))
        $Index = Invoke-ProgressUiCheckGit @('-C',$Publication,'rev-parse',(':'+$Relative+'/'+$Row.file))
        if ($Raw -cne $Index) { throw ('Git transformed evidence bytes: '+$Row.file+'. Nothing was pushed.') }
    }
    $null = Invoke-ProgressUiCheckGit @('-C',$Publication,'commit','--only','-m',('chore: share progress UI integration results '+(Split-Path -Leaf $Directory)),'--',$Relative)
    $Revision = Invoke-ProgressUiCheckGit @('-C',$Publication,'rev-parse','HEAD')
    if ((Invoke-ProgressUiCheckGit @('-C',$Publication,'rev-parse','HEAD^')) -cne $Remote) { throw 'Unexpected publication parent; nothing pushed.' }
    $Changed = Invoke-ProgressUiCheckGit @('-C',$Publication,'diff-tree','--no-commit-id','--name-only','-r','-z',$Remote,$Revision)
    $Paths = @($Changed.Split([char]0) | Where-Object {$_.Length -gt 0})
    if (-not $Paths.Count) { throw 'Empty publication commit; nothing pushed.' }
    foreach ($Path in $Paths) { if (-not $Path.StartsWith(($Relative+'/'),[StringComparison]::Ordinal)) { throw 'Unrelated publication path; nothing pushed.' } }
    # One normal push, existing hooks honored. No local branch/index changes or retry.
    $null = Invoke-ProgressUiCheckGit @('-C',$Publication,'push','--porcelain','--no-follow-tags','origin',($Revision+':refs/heads/'+$Branch))
    return @{success=$true;commit=$Revision;source_run=$Directory;automatic_retries=0}
}
function Share-ProgressUiCheckResult([string]$Directory) {
    $ReceiptPath = Join-Path $Directory 'share-result.json'
    if (Test-Path -LiteralPath $ReceiptPath) {
        $Previous = [IO.File]::ReadAllText($ReceiptPath) | ConvertFrom-Json
        if ($Previous.success -eq $true -and $Previous.source_run -ceq $Directory) { Write-Host 'Existing result was already shared. No Unity or push repeated.'; return $true }
    }
    $Sharing = $null
    try {
        $Sharing = Publish-ProgressUiCheckResult $Directory
        Write-Host ('Results shared at '+$Sharing.commit)
    } catch {
        $Sharing = @{success=$false;error=$_.Exception.Message;source_run=$Directory;automatic_retries=0}
        Write-Host ('Sharing stopped: '+$Sharing.error)
    } finally {
        $Sharing | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $ReceiptPath -Encoding UTF8
    }
    return [bool]$Sharing.success
}
function Assert-ProgressUiCheckResult([string]$Directory,[string]$Phase,$PhaseResult) {
    $Doc = Read-ProgressUiCheckXml (Join-Path $Directory 'results.xml')
    $Root = $Doc.DocumentElement
    foreach ($Name in @('total','passed','failed','skipped','inconclusive')) {
        $Count = 0
        if (-not [int]::TryParse($Root.GetAttribute($Name),[ref]$Count) -or $Count -lt 0) { throw 'Invalid XML result count.' }
        $PhaseResult[$Name] = $Count
    }
    $Names = @($Fixture)
    if ($Phase -eq 'integration') { $Names = @($FixtureCounts.Keys) }
    $Expected = 0; foreach ($Name in $Names) { $Expected += $FixtureCounts[$Name] }
    if ($Root.Name -cne 'test-run' -or $Root.GetAttribute('result') -cne 'Passed' -or
        $PhaseResult.total -ne $Expected -or $PhaseResult.passed -ne $Expected -or
        $PhaseResult.failed -ne 0 -or $PhaseResult.skipped -ne 0 -or $PhaseResult.inconclusive -ne 0) { throw ('All '+$Expected+' requested '+$Phase+' cases must pass; no retry.') }
    $Cases = @($Doc.SelectNodes('//test-case'))
    if ($Cases.Count -ne $Expected -or @($Cases | ForEach-Object {$_.GetAttribute('fullname')} | Select-Object -Unique).Count -ne $Expected) { throw 'Missing/duplicate native test evidence.' }
    foreach ($Name in $Names) {
        $Matched = @($Cases | Where-Object {$_.GetAttribute('fullname').StartsWith(($Name+'.'),[StringComparison]::Ordinal)})
        if ($Matched.Count -ne $FixtureCounts[$Name]) { throw ('Missing/extra cases for '+$Name) }
        foreach ($Case in $Matched) { if ($Case.GetAttribute('result') -cne 'Passed') { throw ('Non-passed '+$Name) } }
    }
    $PhaseResult.validation_passed = $true
}
function New-ProgressUiCheckPhase {
    return [ordered]@{result='NotRun';total=$null;passed=$null;failed=$null;skipped=$null;inconclusive=$null;
        unity_started=$false;unity_exit_code=$null;unity_invocations=0;automatic_retries=0;validation_passed=$false;first_error=$null;failed_fixture=$null;
        source_unchanged=$false;project_path=$null;library_path=$null}
}
if ($MyInvocation.InvocationName -eq '.') { return }
if ($env:OS -ne 'Windows_NT') { throw 'Windows11 / Windows PowerShell5.1 is required. Unity was not started.' }
if ($ProductCommit -cnotmatch '^[a-f0-9]{40}$') { throw 'The reviewed Stage7 implementation commit must be pinned.' }
$Repository = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$Drive = New-Object IO.DriveInfo ([IO.Path]::GetPathRoot($ValidationRoot))
if (-not $Drive.IsReady) { throw 'E: validation drive is unavailable. Nothing was deleted and Unity was not started.' }
$Id = (Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8)
$Paths = Get-ProgressUiCheckPaths $ValidationRoot $Id
$Once = $Paths.Once
if (Test-Path -LiteralPath $Once) {
    $Existing = [IO.File]::ReadAllText($Once).Trim()
    $Attempt=Read-ProgressUiCheckAttempt $Existing $Paths.DiagnosticsRoot $ValidationId
    if(-not $Attempt.is_terminal){Write-ProgressUiCheckIncompleteStop $Once $Attempt;exit 1}
    $Git = Find-ProgressUiCheckGit
    Write-Host 'Existing attempt: saved results only; neither Unity phase will restart.'
    if (-not (Share-ProgressUiCheckResult $Existing)) { exit 2 }
    $Saved = $Attempt.saved
    if ($Saved.validation_passed -ne $true) { exit 1 };exit 0
}
# Read all restart prerequisites before creating any new v3 receipt, project or Unity process.
$Git = Find-ProgressUiCheckGit
$RestartState = Read-ProgressUiCheckRestartState $Paths
if($Repository.TrimEnd('\') -ine 'C:\Users\User\Desktop\Mofumachi\mofumachi-merge'){
    $RestartState.blocked_reasons+='Run the launcher from the one formal C-drive repository.';$RestartState.passed=$false
}
$Run = $Paths.Run
if(Test-Path -LiteralPath $Run){throw 'New Diagnostics path already exists; nothing overwritten.'}
$null = [IO.Directory]::CreateDirectory($Run)
$Encoding = New-Object Text.UTF8Encoding($true)
$Result = [ordered]@{source_commit=$ProductCommit;unity_version='6000.6.4f1';save_version=4;validation_id=$ValidationId;
    validation_root=$Paths.Root;diagnostics_path=$Run;original_repository=$Repository;available_validation_bytes=$Drive.AvailableFreeSpace;
    process_temp_root=$Paths.TempRoot;upm_cache_root=$Paths.PackageCache;
    previous_e_v1_attempt=$RestartState.previous_attempts[0];previous_e_v2_attempt=$RestartState.previous_attempts[1];
    restart_preflight_passed=$RestartState.passed;restart_history_preserved=$false;attempt_receipt_created=$false;
    requested_tests=@($FixtureCounts.Keys);expected_direct_cases=$FixtureCounts[$Fixture];expected_integration_cases=($FixtureCounts.Values|Measure-Object -Sum).Sum;
    test_platform='EditMode';burst_compilation_disabled=$true;automatic_retries=0;unity_invocations=0;unity_start_limit=2;
    direct=(New-ProgressUiCheckPhase);integration=(New-ProgressUiCheckPhase);validation_passed=$false;result='NotRun';
    first_error=$null;runner_error=$null;first_failed_phase=$null;source_unchanged=$false;stage8_safe=$false;review_required=$true}
$Result.direct.project_path=$Paths.DirectProject;$Result.integration.project_path=$Paths.IntegrationProject
$Result.direct.library_path=Join-Path $Paths.DirectProject 'Library';$Result.integration.library_path=Join-Path $Paths.IntegrationProject 'Library'
$Phase = 'direct';$Failure = $null
try {
    [IO.File]::WriteAllText((Join-Path $Run 'source-revision.txt'),$ProductCommit+"`r`n",$Encoding)
    Save-ProgressUiCheckRestartState $Run $RestartState
    $Result.restart_history_preserved=$true
    if(-not $RestartState.passed){throw ('Restart preflight blocked before receipt/checkout/Unity: '+($RestartState.blocked_reasons -join '; '))}
    $Git = Find-ProgressUiCheckGit
    if ((Invoke-ProgressUiCheckGit @('branch','--show-current')) -cne 'feat/daily-world-pass-20261016') { throw 'Use the current feature repository; original branch was not changed.' }
    $Editor = Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'
    if (-not (Test-Path -LiteralPath $Editor -PathType Leaf)) { throw 'Unity6000.6.4f1 at the default Hub location is required.' }
    if ((Get-Item -LiteralPath $Editor).VersionInfo.ProductVersion -notmatch '6000\.6\.4f1') { throw 'Wrong Unity executable version.' }
    $Available = @($ProductCommit | & $Git -C $Repository cat-file --batch-check)
    if ($LASTEXITCODE -ne 0 -or $Available.Count -ne 1) { throw 'Cannot inspect pinned source.' }
    if ([string]$Available[0] -ceq ($ProductCommit+' missing')) { $null = Get-ProgressUiCheckRemoteHead }
    $null = Invoke-ProgressUiCheckGit @('cat-file','-e',($ProductCommit+'^{commit}'))
    # Preparing is explicitly non-terminal; persist it before the receipt and expensive checkout.
    $Result.result='Preparing'
    $Result|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $Run 'progress-ui-integration-result.json') -Encoding UTF8
    # One CreateNew receipt covers checkout and both phases; it is never deleted or reset.
    $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $Once))
    $Receipt = [IO.File]::Open($Once,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try { $Bytes=$Encoding.GetBytes($Run);$Receipt.Write($Bytes,0,$Bytes.Length);$Receipt.Flush() } finally { $Receipt.Dispose() }
    $Result.attempt_receipt_created=$true
    foreach ($Phase in @('direct','integration')) {
        # Reaching integration is possible only after every direct-phase condition passed.
        if ($Phase -eq 'integration' -and ($Result.direct.validation_passed -ne $true -or $Result.direct.source_unchanged -ne $true)) { throw 'Direct verification did not pass with unchanged source; integration must not start.' }
        $Project=$Result[$Phase].project_path
        $null = Invoke-ProgressUiCheckGit @('worktree','add','--detach',$Project,$ProductCommit)
        if ((Invoke-ProgressUiCheckGit @('-C',$Project,'rev-parse','HEAD')) -cne $ProductCommit) { throw 'Pinned source differs.' }
        if (Invoke-ProgressUiCheckGit @('-C',$Project,'status','--porcelain=v1','--untracked-files=all')) { throw 'Isolated source not clean.' }
        if ([IO.File]::ReadAllText((Join-Path $Project 'ProjectSettings/ProjectVersion.txt')) -notmatch 'm_EditorVersion: 6000\.6\.4f1\s') { throw 'Project Unity version differs.' }
        $ProcessTemp=Join-Path $Paths.TempRoot $Phase
        $null=[IO.Directory]::CreateDirectory($ProcessTemp)
        $null=[IO.Directory]::CreateDirectory($Paths.PackageCache)
        $PhaseDir = Join-Path $Run $Phase;$null=[IO.Directory]::CreateDirectory($PhaseDir)
        $Arguments = @(Get-ProgressUiCheckArguments $Project $PhaseDir $Phase)
        [IO.File]::WriteAllLines((Join-Path $PhaseDir 'unity-arguments.txt'),[string[]]$Arguments,$Encoding)
        $Current=$Result[$Phase];$Current.result='Running';$Current.unity_invocations=1;$Result.unity_invocations++
        $Result.result='Running'
        $Result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Run 'progress-ui-integration-result.json') -Encoding UTF8
        $Start=New-Object Diagnostics.ProcessStartInfo
        $Start.FileName=$Editor;$Start.WorkingDirectory=$Project;$Start.UseShellExecute=$false
        # Child process only: no machine/user environment or package settings are changed.
        $Start.EnvironmentVariables['TEMP']=$ProcessTemp;$Start.EnvironmentVariables['TMP']=$ProcessTemp
        $Start.EnvironmentVariables['UPM_CACHE_ROOT']=$Paths.PackageCache
        $Start.Arguments=(@($Arguments|ForEach-Object {if($_.Contains('"')){throw 'Unsafe argument.'};'"'+$_+'"'}) -join ' ')
        Write-Host ($Phase+': exactly the pinned EditMode fixtures, one Unity invocation, Burst disabled. '+$PhaseDir)
        $Process=[Diagnostics.Process]::Start($Start);$Current.unity_started=$true
        $Process.WaitForExit();$Current.unity_exit_code=$Process.ExitCode
        if ($Process.ExitCode -ne 0) { throw ('Unity exit '+$Process.ExitCode+'; no retry.') }
        Assert-ProgressUiCheckResult $PhaseDir $Phase $Current
        if (Invoke-ProgressUiCheckGit @('-C',$Project,'status','--porcelain=v1','--untracked-files=all','--','Assets/Mofumachi','Assets/Scenes','Packages','ProjectSettings','References/VisualLock')) { throw 'Pinned product inputs changed; do not start another phase.' }
        if ((Invoke-ProgressUiCheckGit @('-C',$Project,'rev-parse','HEAD')) -cne $ProductCommit) { throw 'Pinned product revision changed; no further phase.' }
        $Current.source_unchanged=$true;$Current.result='Passed'
    }
    $Result.source_unchanged=$true;$Result.validation_passed=$true;$Result.result='Passed'
} catch {
    $Failure=$_;$Result.runner_error=$_.Exception.Message;$Result.first_failed_phase=$(if($Result.attempt_receipt_created){$Phase}else{'preflight'})
    $Current=$Result[$Phase];$Current.result=$(if($Current.unity_started){'Failed'}else{'Blocked'})
    $Result.result=$Current.result
    $Result.first_error=$(if($Current.unity_started){Get-ProgressUiCheckFirstError (Join-Path $Run $Phase)}else{$_.Exception.Message})
    $Current.first_error=$Result.first_error
    $Xml=Join-Path (Join-Path $Run $Phase) 'results.xml'
    if(Test-Path -LiteralPath $Xml){try{
        $Doc=Read-ProgressUiCheckXml $Xml
        foreach($Name in @('total','passed','failed','skipped','inconclusive')){ $Number=0;if([int]::TryParse($Doc.DocumentElement.GetAttribute($Name),[ref]$Number)){$Current[$Name]=$Number} }
        $Failed=@($Doc.SelectNodes('//test-case[failure]') | Sort-Object {$_.GetAttribute('start-time')})
        if($Failed.Count){$Full=$Failed[0].GetAttribute('fullname');foreach($Name in $FixtureCounts.Keys){if($Full.StartsWith(($Name+'.'),[StringComparison]::Ordinal)){$Current.failed_fixture=$Name;break}}}
    }catch{}}
    [IO.File]::WriteAllText((Join-Path $Run 'first-error.log'),$Result.first_error,$Encoding)
} finally {
    $Result|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $Run 'progress-ui-integration-result.json') -Encoding UTF8
}
if($Failure){Write-Host ('STOP: '+$Result.first_error+' Preserve '+$Run)}else{Write-Host ('PASS both phases. Await independent review before stage8. '+$Run)}
if(-not(Get-Variable Git -ErrorAction SilentlyContinue)){$Git=Find-ProgressUiCheckGit}
if(-not(Share-ProgressUiCheckResult $Run)){exit 2}
if($Failure){exit 1};exit 0
