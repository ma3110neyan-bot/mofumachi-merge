& {
    # Diagnostic only: one logging test, then at most one worktree add. No editor/build entry point.
    $ErrorActionPreference='Stop'
    $Source='ddbb93f3e6b5a45795be87561f61de5d4194f65c'
    $Branch='feat/daily-world-pass-20261016'
    $Formal='C:\Users\User\Desktop\Mofumachi\mofumachi-merge'
    $Root='E:\Mofumachi-Validation'
    $Names=@('MOFUMACHI_KEYSTORE_PATH','MOFUMACHI_KEYSTORE_PASSWORD','MOFUMACHI_KEY_ALIAS','MOFUMACHI_KEY_ALIAS_PASSWORD')
    $Secrets=@($Names | ForEach-Object {[Environment]::GetEnvironmentVariable($_,'Process')} | Where-Object {$_})
    $Utf8=New-Object Text.UTF8Encoding($false)
    $Version='create-worktree-audit-20261112-v1'

    function No-Link([string]$Path) {
        for($Part=[IO.Path]::GetFullPath($Path);$Part;$Part=[IO.Path]::GetDirectoryName($Part)) {
            if((Test-Path -LiteralPath $Part) -and ((Get-Item -LiteralPath $Part -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {throw 'Linked audit path rejected.'}
        }
    }
    function Redact([string]$Text) {
        foreach($Secret in $Secrets) {
            $Text=$Text.Replace($Secret,'[REDACTED]')
            $Encoded=[Uri]::EscapeDataString($Secret)
            if($Encoded -cne $Secret){$Text=$Text.Replace($Encoded,'[REDACTED]')}
        }
        $Text=[regex]::Replace($Text,'(?i)(https?://)[^\s/@:]+:[^\s/@]+@','$1[REDACTED]@')
        $Text=[regex]::Replace($Text,'\b(?:github_pat_[A-Za-z0-9_]+|gh[pousr]_[A-Za-z0-9]+)\b','[REDACTED]')
        $Text=[regex]::Replace($Text,'(?i)([?&](?:access_token|token|key|sig|signature)=)[^&\s"'']+','$1[REDACTED]')
        return [regex]::Replace($Text,'(?i)(password|passwd|secret|token|authorization|credential|api[_-]?key)(\s*[:=]\s*)("[^"]*"|''[^'']*''|[^\s]+)','$1$2[REDACTED]')
    }
    function Save-Json([string]$Path,$Value) {
        [IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 12),$Utf8)
    }
    function Quote-Arguments([string[]]$Words) {
        return (@(foreach($Word in $Words) {
            if($Word -match '["\r\n]' -or $Word.EndsWith('\')){throw 'Unsupported diagnostic argument.'}
            '"'+$Word+'"'
        }) -join ' ')
    }
    function Logged-Git([string]$Group,[int]$Number,[string]$Directory,[string[]]$Words,[switch]$ExpectedFailure,[switch]$ReadOnly,[switch]$ReturnFailure) {
        $Folder=if($Group -eq 'target'){$Run}else{Join-Path $Run $Group}
        $null=[IO.Directory]::CreateDirectory($Folder)
        $Stem=Join-Path $Folder ('git-step-'+$Number.ToString('00'))
        foreach($Suffix in @('.json','.stdout.log','.stderr.log')) {
            if(Test-Path -LiteralPath ($Stem+$Suffix)){throw 'Existing command log; no command retry.'}
        }
        $Native=Quote-Arguments $Words
        $Info=New-Object Diagnostics.ProcessStartInfo
        $Info.FileName=$GitExe;$Info.WorkingDirectory=$Directory;$Info.Arguments=$Native
        $Info.UseShellExecute=$false;$Info.CreateNoWindow=$true
        $Info.RedirectStandardOutput=$true;$Info.RedirectStandardError=$true
        $Info.StandardOutputEncoding=$Utf8;$Info.StandardErrorEncoding=$Utf8
        $Info.EnvironmentVariables['GIT_TERMINAL_PROMPT']='0'
        if($ReadOnly){$Info.EnvironmentVariables['GIT_OPTIONAL_LOCKS']='0'}
        foreach($Name in $Names){$Info.EnvironmentVariables.Remove($Name)}
        $Record=[ordered]@{
            group=$Group;sequence_number=$Number;executable=(Redact $GitExe)
            arguments=@($Words|ForEach-Object{Redact $_});arguments_native=(Redact $Native)
            working_directory=(Redact $Directory);command=(Redact ('"'+$GitExe+'" '+$Native))
            process_started=$false;exit_code=$null;first_fatal_line=$null;first_error_line=$null;error_type=$null
            started_utc=[DateTime]::UtcNow.ToString('o');finished_utc=$null
            stdout_log=([IO.Path]::GetFileName($Stem)+'.stdout.log')
            stderr_log=([IO.Path]::GetFileName($Stem)+'.stderr.log');stdout='';stderr='';redaction_occurred=$false
        }
        [IO.File]::WriteAllText(($Stem+'.stdout.log'),'',$Utf8)
        [IO.File]::WriteAllText(($Stem+'.stderr.log'),'',$Utf8)
        Save-Json ($Stem+'.json') $Record
        $Process=New-Object Diagnostics.Process;$Process.StartInfo=$Info
        $RawOut='';$RawErr=''
        try {
            $null=$Process.Start();$Record.process_started=$true
            $OutTask=$Process.StandardOutput.ReadToEndAsync();$ErrTask=$Process.StandardError.ReadToEndAsync()
            $Process.WaitForExit()
            $RawOut=$OutTask.GetAwaiter().GetResult();$RawErr=$ErrTask.GetAwaiter().GetResult()
            $Record.exit_code=$Process.ExitCode
        } catch {$Record.error_type=$_.Exception.GetType().FullName}
        finally {$Process.Dispose()}
        $SafeOut=Redact $RawOut;$SafeErr=Redact $RawErr
        $Fatal=[regex]::Match($RawErr,'(?im)^[^\r\n]*\bfatal:[^\r\n]*')
        if($Fatal.Success){$Record.first_fatal_line=Redact $Fatal.Value}
        $FirstError=[regex]::Match($RawErr,'(?im)^[^\r\n]*\berror:[^\r\n]*')
        if($FirstError.Success){$Record.first_error_line=Redact $FirstError.Value}
        $Record.stdout=$SafeOut;$Record.stderr=$SafeErr
        $Record.redaction_occurred=($SafeOut -cne $RawOut -or $SafeErr -cne $RawErr)
        $Record.finished_utc=[DateTime]::UtcNow.ToString('o')
        $Failed=(-not $Record.process_started) -or $Record.exit_code -ne 0 -or $Record.error_type
        if($Group -eq 'target'){$State.last_target=$Record}
        if($Failed -and -not $ExpectedFailure -and -not $State.first_failure){$State.first_failure=$Record}
        [IO.File]::WriteAllText(($Stem+'.stdout.log'),$SafeOut,$Utf8)
        [IO.File]::WriteAllText(($Stem+'.stderr.log'),$SafeErr,$Utf8)
        Save-Json ($Stem+'.json') $Record
        # Capture both complete streams and command metadata before propagating failure.
        if($Failed -and -not $ExpectedFailure -and -not $ReturnFailure) {
            throw 'STOP: Git failure captured. No retry.'
        }
        return [pscustomobject]@{metadata=$Record;stdout=$SafeOut;stderr=$SafeErr;stem=$Stem}
    }
    function Formal-State([string]$Group,[switch]$Final) {
        $Queries=@(@('rev-parse','HEAD'),@('branch','--show-current'),@('status','--porcelain=v1','--untracked-files=all'),@('diff','--cached','--name-only'))
        $Values=@{};$Complete=$true
        for($Number=1;$Number -le 4;$Number++) {
            try {
                $Item=Logged-Git $Group $Number $Formal $Queries[$Number-1] -ReadOnly -ReturnFailure:$Final
                if(-not $Item.metadata.process_started -or $Item.metadata.exit_code -ne 0 -or $Item.metadata.error_type){$Complete=$false;$Values[$Number]=$null}
                else {$Values[$Number]=$Item.stdout}
            } catch {
                if(-not $Final){throw}
                $Complete=$false;$Values[$Number]=$null
            }
        }
        return @{
            complete=$Complete
            head=$(if($null -ne $Values[1]){$Values[1].Trim()}else{$null})
            branch=$(if($null -ne $Values[2]){$Values[2].Trim()}else{$null})
            working_tree_clean=$(if($null -ne $Values[3]){[string]::IsNullOrWhiteSpace($Values[3])}else{$null})
            staged_empty=$(if($null -ne $Values[4]){[string]::IsNullOrWhiteSpace($Values[4])}else{$null})
        }
    }

    if($env:OS -ne 'Windows_NT'){throw 'Windows only. No test or worktree operation started.'}
    $GitExe=(Get-Command git.exe -CommandType Application -ErrorAction Stop).Source
    $ReceiptFolder=Join-Path $Root 'Diagnostics\audit-attempts'
    $Receipt=Join-Path $ReceiptFolder ('create-worktree-audit-'+$Source+'.json')
    foreach($Path in @($Formal,$Root,$GitExe,$ReceiptFolder)){No-Link $Path}
    if(Test-Path -LiteralPath $Receipt){throw 'STOP: this audit already reserved. No second logging test or worktree attempt.'}
    $Tag=[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N')
    $Run=Join-Path $Root ('Diagnostics\create-worktree-audit-'+$Tag)
    $Destination=Join-Path $Root ('projects\serialization-probe-'+$Tag)
    foreach($Path in @($Run,$Destination)) {
        No-Link $Path
        if(Test-Path -LiteralPath $Path){throw 'STOP: new unique audit and worktree paths required.'}
    }
    $null=[IO.Directory]::CreateDirectory($Run)
    $State=@{phase='formal-before';first_failure=$null;last_target=$null;succeeded_steps=@();failure_reason=$null;limited_test='NotRun';limited_test_count=0;create_count=0;worktree_head=$null;worktree_clean=$null;success=$false}
    $Before=$null;$After=$null;$Unchanged=$null
    try {
        $Before=Formal-State 'formal-before'
        if($Before.head -cne $Source -or $Before.branch -cne $Branch -or -not $Before.working_tree_clean -or -not $Before.staged_empty) {throw 'STOP: formal repo must be clean at the pinned HEAD and branch.'}
        $null=[IO.Directory]::CreateDirectory($ReceiptFolder)
        $Stream=New-Object IO.FileStream($Receipt,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
        try {
            $Data=$Utf8.GetBytes((@{source_head=$Source;run=$Run;code_version=$Version;reserved_utc=[DateTime]::UtcNow.ToString('o');maximum_logging_tests=1;maximum_worktree_attempts=1}|ConvertTo-Json))
            $Stream.Write($Data,0,$Data.Length)
        } finally {$Stream.Dispose()}

        $State.phase='logging-test';$State.limited_test_count=1
        # Guaranteed read-only argument error; this expected fatal is never the target failure.
        $Test=Logged-Git 'logging-test' 1 $Formal @('rev-parse','--verify') -ExpectedFailure -ReadOnly
        $Saved=Get-Content -LiteralPath ($Test.stem+'.json') -Raw -Encoding UTF8|ConvertFrom-Json
        $TestOk=$Test.metadata.process_started -and $null -ne $Test.metadata.exit_code -and $Test.metadata.exit_code -ne 0 -and -not $Test.metadata.error_type -and
            -not [string]::IsNullOrWhiteSpace($Test.stderr) -and -not [string]::IsNullOrWhiteSpace($Test.metadata.first_fatal_line) -and
            $Saved.exit_code -eq $Test.metadata.exit_code -and $Saved.first_fatal_line -ceq $Test.metadata.first_fatal_line -and
            $Saved.sequence_number -eq 1 -and $Saved.executable -ceq $Test.metadata.executable -and
            $Saved.arguments_native -ceq $Test.metadata.arguments_native -and $Saved.working_directory -ceq $Test.metadata.working_directory -and
            $Saved.stdout -ceq $Test.stdout -and $Saved.stderr -ceq $Test.stderr -and
            [IO.File]::ReadAllText(($Test.stem+'.stdout.log')) -ceq $Test.stdout -and
            [IO.File]::ReadAllText(($Test.stem+'.stderr.log')) -ceq $Test.stderr -and $Test.stderr.Contains($Test.metadata.first_fatal_line)
        $State.limited_test=if($TestOk){'PASS'}else{'FAIL'}
        Save-Json (Join-Path $Run 'limited-logging-test.json') @{result=$State.limited_test;invocation_count=1;expected_failure=$true;exit_code=$Saved.exit_code;fatal_line_retained=$TestOk;automatic_retries=0}
        if(-not $TestOk){throw 'STOP: limited logging test failed. No worktree attempt.'}
        Write-Host 'Limited logging test: PASS'

        $State.phase='CreateWorktree';$State.create_count=1
        $null=Logged-Git 'target' 1 $Formal @('worktree','add','--detach',$Destination,$Source)
        $State.succeeded_steps+='target/1'
        if(-not [IO.Directory]::Exists($Destination)){throw 'STOP: successful worktree command did not leave a destination directory.'}
        $State.phase='VerifyWorktreeHead'
        $Head=Logged-Git 'target' 2 $Destination @('rev-parse','HEAD') -ReadOnly
        $State.succeeded_steps+='target/2'
        $State.worktree_head=$Head.stdout.Trim()
        if($State.worktree_head -cne $Source){throw 'STOP: created worktree HEAD differs from expected.'}
        $State.phase='VerifyWorktreeClean'
        $Status=Logged-Git 'target' 3 $Destination @('status','--porcelain=v1','--untracked-files=all') -ReadOnly
        $State.succeeded_steps+='target/3'
        $State.worktree_clean=[string]::IsNullOrWhiteSpace($Status.stdout)
        if(-not $State.worktree_clean){throw 'STOP: created worktree is not clean.'}
        $State.success=$true
    } catch {$State.failure_reason=$_.Exception.Message;$State.success=$false}
    finally {
        # Required post-state reads only. No repair, retry, cleanup or second target operation.
        try {
            $After=Formal-State 'formal-after' -Final
            $Unchanged=if($After.complete -and $null -ne $Before){$After.head -ceq $Before.head -and $After.branch -ceq $Before.branch -and $After.working_tree_clean -and $After.staged_empty}else{$null}
        } catch {
            $State.success=$false
            if(-not $State.failure_reason){$State.failure_reason='Formal final-state read failed.'}
        }
        if(-not $Unchanged){
            $State.success=$false
            if(-not $State.failure_reason){$State.phase='FormalFinalStateMismatch';$State.failure_reason='Formal repo final state does not match the clean initial state.'}
        }
        $Failure=$State.first_failure
        $FailedStep=if($Failure){$Failure.group+'/'+$Failure.sequence_number}else{if($State.failure_reason){$State.phase}else{$null}}
        $Conclusion=@{
            source_head=$Source;code_version=$Version;run_id=('create-worktree-audit-'+$Tag)
            result=$(if($State.success){'PASS'}else{'FAIL'});formal_repo_unchanged=$Unchanged
            limited_logging_test=$State.limited_test;limited_logging_test_count=$State.limited_test_count
            create_worktree_invocation_count=$State.create_count;failed_step=$FailedStep;succeeded_git_steps=$State.succeeded_steps
            failed_command=$(if($Failure){$Failure.command}else{if($State.failure_reason -and $State.last_target){$State.last_target.command}else{$null}})
            exit_code=$(if($Failure){$Failure.exit_code}else{if($State.last_target){$State.last_target.exit_code}else{$null}})
            first_fatal_line=$(if($Failure){$Failure.first_fatal_line}else{$null})
            first_error_line=$(if($Failure){$Failure.first_error_line}else{$null})
            stderr_captured=$(if($Failure){$Failure.process_started -and -not $Failure.error_type}else{$State.limited_test -eq 'PASS'})
            worktree_destination=$Destination;worktree_directory_created=[IO.Directory]::Exists($Destination)
            worktree_head=$State.worktree_head;worktree_clean=$State.worktree_clean
            failure_reason=(Redact ([string]$State.failure_reason));core_longpaths_changed=$false;git_config_changed=$false
            unity_invocations=0;serialization_probe_invocations=0;aab_count=0;apk_count=0;pixel_operations=0;signing_verifications=0
            automatic_retries=0;stage8_complete=$false;next_action='STOP. Review the captured result; further actions require separate instructions.'
            root_cause_identified=$null;root_cause_status='Requires review of actual target stderr; the intentional logging-test fatal is not the target cause.'
        }
        Save-Json (Join-Path $Run 'formal-repo-state.json') @{before=$Before;after=$After;unchanged=$Unchanged}
        Save-Json (Join-Path $Run 'worktree-state.json') @{destination=$Destination;exists=[IO.Directory]::Exists($Destination);head=$State.worktree_head;clean=$State.worktree_clean;repair_operations=0;cleanup_operations=0}
        Save-Json (Join-Path $Run 'conclusion.json') $Conclusion
        [IO.File]::WriteAllText((Join-Path $Run 'summary.txt'),(($Conclusion.GetEnumerator()|Sort-Object Key|ForEach-Object{$_.Key+': '+[string]$_.Value}) -join [Environment]::NewLine),$Utf8)
        $Manifest=@(Get-ChildItem -LiteralPath $Run -File -Recurse|Sort-Object FullName|ForEach-Object{@{name=$_.FullName.Substring($Run.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant();bytes=$_.Length}})
        Save-Json (Join-Path $Run 'public-manifest.json') @{files=$Manifest;redaction='Known signing values and common credential/token forms are replaced; fatal lines retained.'}
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $Zip=$Run+'-public.zip'
        [IO.Compression.ZipFile]::CreateFromDirectory($Run,$Zip)
        Write-Host ('RESULT: '+$Conclusion.result+'; failed step: '+$Conclusion.failed_step)
        if($Conclusion.first_fatal_line){Write-Host ('FIRST FATAL: '+$Conclusion.first_fatal_line)}
        Write-Host ('PUBLIC AUDIT ZIP: '+$Zip)
        Write-Host 'STOP HERE. No Unity, probe, build, config change, repair or additional worktree attempt.'
        [Array]::Clear($Secrets,0,$Secrets.Length)
    }
    if(-not $State.success){throw ('STOP: '+$Conclusion.failure_reason+' Evidence has been saved; no retry.')}
}
