& {
    $ErrorActionPreference='Stop'
    $Source='ddbb93f3e6b5a45795be87561f61de5d4194f65c'
    $Shared='ec5284c3c741113298df5a6efd3aabeb2a88f75d'
    $Formal='C:\Users\User\Desktop\Mofumachi\mofumachi-merge'
    $Root='E:\Mofumachi-Validation'
    $Unity='C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe'
    $Names=@('MOFUMACHI_KEYSTORE_PATH','MOFUMACHI_KEYSTORE_PASSWORD','MOFUMACHI_KEY_ALIAS','MOFUMACHI_KEY_ALIAS_PASSWORD')
    $Private=@($Names|ForEach-Object{[Environment]::GetEnvironmentVariable($_,'Process')})
    $Utf8=New-Object Text.UTF8Encoding($false)
    $Progress=@{phase='prepare';worktrees=0;unity=0;git_sequence=0;first_git_failure=$null;error=$null;worktree_exit_code=$null;worktree_head=$null;creation_clean=$null}
    $Editor=$null;$OutTask=$null;$ErrTask=$null;$Exited=$false;$ExitRead=$false;$Captured=$false
    $Report=$null;$Before=$null;$After=$null;$Unchanged=$null
    $Skip=$null;$E0=$null;$ControlCase='Inconclusive';$ExitUtc=$null

    function No-Link([string]$Path) {
        for($Part=[IO.Path]::GetFullPath($Path);$Part;$Part=[IO.Path]::GetDirectoryName($Part)) {
            if((Test-Path -LiteralPath $Part) -and ((Get-Item -LiteralPath $Part -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Linked path rejected.'}
        }
    }
    function Redact([string]$Text) {
        foreach($Value in $Private){if($Value){
            $Text=$Text.Replace($Value,'[REDACTED]')
            $Encoded=[Uri]::EscapeDataString($Value)
            if($Encoded -cne $Value){$Text=$Text.Replace($Encoded,'[REDACTED]')}
        }}
        $Text=[regex]::Replace($Text,'(?i)(https?://)[^\s/@:]+:[^\s/@]+@','$1[REDACTED]@')
        $Text=[regex]::Replace($Text,'\b(?:github_pat_[A-Za-z0-9_]+|gh[pousr]_[A-Za-z0-9]+)\b','[REDACTED]')
        $Text=[regex]::Replace($Text,'(?i)([?&](?:access_token|token|key|sig|signature)=)[^&\s"'']+','$1[REDACTED]')
        return [regex]::Replace($Text,'(?i)(password|passwd|secret|token|authorization|credential|api[_-]?key)(\s*[:=]\s*)("[^"]*"|''[^'']*''|[^\s]+)','$1$2[REDACTED]')
    }
    function Json([string]$Name,$Value){[IO.File]::WriteAllText((Join-Path $Public $Name),($Value|ConvertTo-Json -Depth 15),$Utf8)}
    function Arguments([string[]]$Words){
        return (@(foreach($Word in $Words){
            if($Word -match '["\r\n]' -or $Word.EndsWith('\')){throw 'Unsupported command argument.'}
            '"'+$Word+'"'
        }) -join ' ')
    }
    function Git([string]$Label,[string]$Directory,[string[]]$Words,[switch]$ReadOnly,[switch]$ReturnFailure){
        $Progress.git_sequence++
        $Native=Arguments $Words
        $Info=New-Object Diagnostics.ProcessStartInfo
        $Info.FileName=$GitExe;$Info.WorkingDirectory=$Directory;$Info.Arguments=$Native
        $Info.UseShellExecute=$false;$Info.CreateNoWindow=$true
        $Info.RedirectStandardOutput=$true;$Info.RedirectStandardError=$true
        $Info.StandardOutputEncoding=$Utf8;$Info.StandardErrorEncoding=$Utf8
        $Info.EnvironmentVariables['GIT_TERMINAL_PROMPT']='0'
        if($ReadOnly){$Info.EnvironmentVariables['GIT_OPTIONAL_LOCKS']='0'}
        foreach($Name in $Names){$Info.EnvironmentVariables.Remove($Name)}
        $Row=[ordered]@{sequence_number=$Progress.git_sequence;label=$Label;executable=(Redact $GitExe);arguments=@($Words|ForEach-Object{Redact $_});arguments_native=(Redact $Native);working_directory=(Redact $Directory);command=(Redact ('"'+$GitExe+'" '+$Native));exit_code=$null;process_started=$false;error_type=$null;stdout='';stderr='';first_fatal_line=$null;first_error_line=$null}
        $Process=New-Object Diagnostics.Process;$Process.StartInfo=$Info
        try {
            $null=$Process.Start();$Row.process_started=$true
            $Out=$Process.StandardOutput.ReadToEndAsync();$Err=$Process.StandardError.ReadToEndAsync()
            $Process.WaitForExit();$Row.exit_code=$Process.ExitCode
            $Row.stdout=Redact $Out.GetAwaiter().GetResult();$Row.stderr=Redact $Err.GetAwaiter().GetResult()
        }catch{$Row.error_type=$_.Exception.GetType().FullName}finally{$Process.Dispose()}
        $Fatal=[regex]::Match($Row.stderr,'(?i)(?:^|[\r\n])([^\r\n]*\bfatal:[^\r\n]*)')
        if($Fatal.Success){$Row.first_fatal_line=$Fatal.Groups[1].Value}
        $ErrorLine=[regex]::Match($Row.stderr,'(?i)(?:^|[\r\n])([^\r\n]*\berror:[^\r\n]*)')
        if($ErrorLine.Success){$Row.first_error_line=$ErrorLine.Groups[1].Value}
        $Failed=-not $Row.process_started -or $Row.exit_code -ne 0 -or $Row.error_type
        if($Label -eq 'git-step-01'){$Progress.worktree_exit_code=$Row.exit_code}
        if($Failed -and -not $Progress.first_git_failure){$Progress.first_git_failure=$Row}
        [IO.File]::WriteAllText((Join-Path $Public ($Label+'.stdout.log')),$Row.stdout,$Utf8)
        [IO.File]::WriteAllText((Join-Path $Public ($Label+'.stderr.log')),$Row.stderr,$Utf8)
        Json ($Label+'.json') $Row
        if($Failed -and -not $ReturnFailure){throw 'Git failure captured; no retry.'}
        return $Row
    }
    function Formal-State([string]$Prefix,[switch]$Final){
        $Queries=@(@('rev-parse','HEAD'),@('branch','--show-current'),@('status','--porcelain=v1','--untracked-files=all'),@('diff','--cached','--name-only'))
        $Values=@{};$Complete=$true
        for($N=1;$N -le 4;$N++){
            try{
                $Item=Git ($Prefix+'-'+$N) $Formal $Queries[$N-1] -ReadOnly -ReturnFailure:$Final
                if(-not $Item.process_started -or $Item.exit_code -ne 0 -or $Item.error_type){$Complete=$false;$Values[$N]=$null}else{$Values[$N]=$Item.stdout}
            }catch{if(-not $Final){throw};$Complete=$false;$Values[$N]=$null}
        }
        return @{complete=$Complete;head=$(if($null -ne $Values[1]){$Values[1].Trim()}else{$null});branch=$(if($null -ne $Values[2]){$Values[2].Trim()}else{$null});clean=$(if($null -ne $Values[3]){[string]::IsNullOrWhiteSpace($Values[3])}else{$null});staged_empty=$(if($null -ne $Values[4]){[string]::IsNullOrWhiteSpace($Values[4])}else{$null})}
    }
    function Reserve([string]$Path,$Value){
        $Stream=New-Object IO.FileStream($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
        try{$Bytes=$Utf8.GetBytes(($Value|ConvertTo-Json));$Stream.Write($Bytes,0,$Bytes.Length)}finally{$Stream.Dispose()}
    }
    function Classify-P6Control($Report,$E0,$Skip,$ExitCode){
        if(-not $Report -or -not $Report.baseline){return 'Inconclusive'}
        $Base=$Report.baseline
        if(-not $Base.metadata_stable -or $Base.sha256 -notmatch '^[0-9a-f]{64}$' -or $Base.m_APIs -cne '0b000000' -or $Base.android_keystore_name -cne '<empty>'){return 'Inconclusive'}
        $Native=@($Report.stages | Where-Object {$_.stage -cne 'E0'})
        if(@($Native | Where-Object {$_.stage -ceq 'P6'}).Count){return 'Inconclusive'}
        foreach($Sample in @($Base)+$Native){
            if(-not $Sample.metadata_stable -or $Sample.sha256 -notmatch '^[0-9a-f]{64}$'){return 'Inconclusive'}
            try{$Decoded=[Text.Encoding]::ASCII.GetString([byte[]]@(([string]$Sample.api_raw_hex -split '\s+') | ForEach-Object {[Convert]::ToByte($_,16)}))}catch{return 'Inconclusive'}
            if($Decoded -cne $Sample.m_APIs){return 'Inconclusive'}
        }
        if(@($Native | Where-Object {$_.sha256 -cne $Base.sha256}).Count){return 'Case C'}
        $Expected='P0,P1,P2,P3a,P3b,P3c,P4a,P4b,P4c,P4d,P4e,P5a,P5b,P5c,P5d,P5e,P5'
        if((@($Native | ForEach-Object {$_.stage}) -join ',') -cne $Expected){return 'Inconclusive'}
        if(-not $Report.completed_stages -or -not $Report.original_signing_restored -or $ExitCode -ne 0 -or -not $Skip -or $Skip.p6_skipped -ne $true -or $Skip.after_stage -cne 'P5'){return 'Inconclusive'}
        if($Report.unity_invocations -ne 1 -or $Report.save_assets_calls -ne 1 -or $Report.graphics_setters -ne 0 -or $Report.graphics_restore_setters -ne 0 -or $Report.non_graphics_setters -ne 3 -or $Report.signing_setting_setters -ne 5 -or $Report.signing_restore_setters -ne 5 -or $Report.build_player_calls -ne 0 -or $Report.aab_count -ne 0 -or $Report.apk_count -ne 0 -or $Report.automatic_retries -ne 0){return 'Inconclusive'}
        if(-not $E0 -or -not $E0.metadata_stable -or $E0.sha256 -notmatch '^[0-9a-f]{64}$'){return 'Inconclusive'}
        try{$Decoded=[Text.Encoding]::ASCII.GetString([byte[]]@(([string]$E0.api_raw_hex -split '\s+') | ForEach-Object {[Convert]::ToByte($_,16)}))}catch{return 'Inconclusive'}
        if($Decoded -cne $E0.m_APIs){return 'Inconclusive'}
        if($E0.sha256 -ceq $Base.sha256 -and $E0.byte_size -eq $Base.byte_size -and $E0.m_APIs -ceq '0b000000' -and $E0.android_keystore_name -ceq '<empty>'){return 'Case A'}
        if($E0.m_APIs -ceq '0b000000i'){return 'Case B'}
        return 'Inconclusive'
    }
    function Patch-Probe([string]$Text,[string]$From,[string]$To){
        $From=$From.Replace([string][char]13,'');$To=$To.Replace([string][char]13,'')
        if([regex]::Matches($Text,[regex]::Escape($From)).Count -ne 1){throw 'Dedicated C# patch anchor differs.'}
        return $Text.Replace($From,$To)
    }

    if($env:OS -ne 'Windows_NT'){throw 'Windows only; no worktree or Unity call made.'}
    $GitExe=(Get-Command git.exe -CommandType Application -ErrorAction Stop).Source
    foreach($Value in $Private){if([string]::IsNullOrWhiteSpace($Value)){throw 'Four process signing variables must be SET in this window.'}}
    if(-not(Test-Path -LiteralPath $Private[0] -PathType Leaf)){throw 'Existing key file not found; no key creation.'}
    $ReceiptFolder=Join-Path $Root 'Diagnostics\probe-attempts'
    $Receipt=Join-Path $ReceiptFolder 'skip-p6-ddbb93f3-task.json'
    $UnityReceipt=Join-Path $ReceiptFolder 'skip-p6-ddbb93f3-unity.json'
    foreach($Path in @($Root,$Formal,$Unity,$GitExe,$ReceiptFolder,$Private[0])){No-Link $Path}
    if((Test-Path -LiteralPath $Receipt) -or (Test-Path -LiteralPath $UnityReceipt)){throw 'Attempt receipt already exists. Do not create another worktree or launch Unity.'}
    $Expected=@{'ProjectSettingsSerializationObservation.cs'='bf59c1133776b9f7a7a2d6a3bd73c9f392c9493d05798284b8da8fc5c1e48b4f';'AndroidProjectSettingsSerializationProbe.cs'='b3989b8e8ea68d5b97d0760d4ec42e7204a24db7eb709b1d4b9a4de556596d35'}
    $Old=$null
    foreach($Folder in @(Get-ChildItem -LiteralPath (Join-Path $Root 'tools') -Directory -Filter 'serialization-probe-*'|Sort-Object LastWriteTimeUtc -Descending)){
        if($Folder.Attributes -band [IO.FileAttributes]::ReparsePoint){continue}
        $Valid=$true
        foreach($Name in $Expected.Keys){
            $File=Join-Path $Folder.FullName $Name
            if(-not(Test-Path -LiteralPath $File -PathType Leaf)){$Valid=$false;break}
            No-Link $File
            if((Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant() -cne $Expected[$Name]){$Valid=$false;break}
        }
        if($Valid){$Old=$Folder.FullName;break}
    }
    if(-not $Old){throw 'Original verified two-file probe package not found.'}
    $Driver=[IO.File]::ReadAllText((Join-Path $Old 'AndroidProjectSettingsSerializationProbe.cs'))
    if([regex]::Matches($Driver,'"projects"').Count -ne 1){throw 'Expected dedicated project boundary not found.'}
    $Driver=$Driver.Replace('"projects"','"p"')
    $Driver=Patch-Probe $Driver '            Action<string> observe = null;' @'
            Action<string> observe = null;
            bool skipP6 = Array.IndexOf(Environment.GetCommandLineArgs(), "-mofumachiProbeSkipP6") >= 0;
'@
    $Driver=Patch-Probe $Driver '                    Debug.Log("MOFUMACHI_SERIALIZATION_PROBE_STAGE " + id);' @'
                    Debug.Log("MOFUMACHI_SERIALIZATION_PROBE_STAGE " + id);
                    if (skipP6 && current.sha256 != report.baseline.sha256)
                        throw new InvalidOperationException("Control pre-P6 mutation observed.");
'@
    $Driver=Patch-Probe $Driver '                stage = "P6"; report.save_assets_calls++; AssetDatabase.SaveAssets(); observe("P6");' @'
                if (skipP6)
                {
                    SettingsObservation.WriteNew(Path.Combine(directory, "p6-skipped.json"), "{\"p6_skipped\":true,\"after_stage\":\"P5\"}");
                    Debug.Log("MOFUMACHI_SERIALIZATION_PROBE_P6_SKIPPED");
                }
                else
                {
                    stage = "P6"; report.save_assets_calls++; AssetDatabase.SaveAssets(); observe("P6");
                }
'@
    $Hash= [Security.Cryptography.SHA256]::Create()
    try{$DriverHash=[BitConverter]::ToString($Hash.ComputeHash($Utf8.GetBytes($Driver))).Replace('-','').ToLowerInvariant()}finally{$Hash.Dispose()}
    if($DriverHash -cne '32fb309ea46b8d8a2ec9f4f53fa5168dff13e020754432d4251d47b94985eb6d'){throw 'Skip-P6 driver checksum differs.'}
    $Id=[guid]::NewGuid().ToString('N').Substring(0,8)
    $RunId='sc-'+$Id
    $Project=Join-Path $Root ('p\'+$RunId)
    $Run=Join-Path $Root ('Diagnostics\'+$RunId)
    $Public=Join-Path $Run 'public'
    foreach($Path in @($Project,$Run)){No-Link $Path;if(Test-Path -LiteralPath $Path){throw 'Fresh short paths required; no ID retry.'}}
    $null=[IO.Directory]::CreateDirectory($Public)
    try{
        $Progress.phase='formal-before';$Before=Formal-State 'formal-before'
        if(-not $Before.complete -or $Before.branch -cne 'feat/daily-world-pass-20261016' -or -not $Before.clean -or -not $Before.staged_empty -or $Before.head -cnotin @($Source,$Shared)){throw 'Formal branch/HEAD/clean check failed; no formal repo update is performed.'}
        if(-not(Test-Path -LiteralPath $Unity -PathType Leaf)){throw 'Pinned Unity editor not found.'}
        $Existing=@(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'")
        foreach($Entry in $Existing){
            $Image=[string]$Entry.ExecutablePath
            if(-not $Image){$Native=$null;try{$Native=[Diagnostics.Process]::GetProcessById([int]$Entry.ProcessId);$Image=$Native.MainModule.FileName}catch{$Image=''}finally{if($Native){$Native.Dispose()}}}
            $Hub='C:\Program Files\Unity Hub\resources\unity.exe';$Escaped=[regex]::Escape($Hub)
            if(-not [string]::Equals($Image,$Hub,[StringComparison]::OrdinalIgnoreCase) -or -not [regex]::IsMatch([string]$Entry.CommandLine,('^\s*(?i:"'+$Escaped+'"|'+$Escaped+')\s+(?:"serve"|serve)\s*$'))){throw 'Real Editor or unidentified Unity process is running.'}
        }
        $null=[IO.Directory]::CreateDirectory($ReceiptFolder)
        Reserve $Receipt @{source_head=$Source;run_id=$RunId;project=$Project;diagnostics=$Run;mode='skip_p6';max_worktrees=1;max_unity=1;automatic_retries=0}
        $Progress.phase='create-worktree';$Progress.worktrees=1
        Write-Host ('ONE short worktree: '+$Project)
        $null=Git 'git-step-01' $Formal @('worktree','add','--detach',$Project,$Source)
        if(-not [IO.Directory]::Exists($Project)){throw 'Worktree directory missing after add.'}
        $Head=Git 'git-step-02' $Project @('rev-parse','HEAD') -ReadOnly
        $Progress.worktree_head=$Head.stdout.Trim()
        if($Progress.worktree_head -cne $Source){throw 'Worktree HEAD differs.'}
        $Clean=Git 'git-step-03' $Project @('status','--porcelain=v1','--untracked-files=all') -ReadOnly
        $Progress.creation_clean=[string]::IsNullOrWhiteSpace($Clean.stdout)
        if(-not $Progress.creation_clean){throw 'New worktree is not clean. No cleanup or repair.'}
        Json 'worktree-created.json' @{path=$Project;head=$Progress.worktree_head;clean=$true;creation_count=1}
        $Progress.phase='inject-dedicated-probe'
        $Code=Join-Path $Project 'Assets\Mofumachi\Editor\Diagnostics';$null=[IO.Directory]::CreateDirectory($Code)
        foreach($Name in $Expected.Keys){if(Test-Path -LiteralPath (Join-Path $Code $Name)){throw 'Probe target already exists.'}}
        [IO.File]::Copy((Join-Path $Old 'ProjectSettingsSerializationObservation.cs'),(Join-Path $Code 'ProjectSettingsSerializationObservation.cs'),$false)
        [IO.File]::WriteAllText((Join-Path $Code 'AndroidProjectSettingsSerializationProbe.cs'),$Driver,$Utf8)
        Json 'probe-code.json' @{source_head=$Source;driver_sha256=$DriverHash;observer_sha256=$Expected['ProjectSettingsSerializationObservation.cs'];driver_change='short E boundary; skip-P6 mode and pre-P6 mutation stop';code_version='skip-p6-20261112-v1';mode='skip_p6'}
        if(-not('Mofumachi.Probe.SettingsObservation' -as [type])){
            $Common=Join-Path $Code 'ProjectSettingsSerializationObservation.cs'
            if($PSVersionTable.PSEdition -eq 'Core'){Add-Type -Path $Common}else{Add-Type -Path $Common -ReferencedAssemblies @('System.dll','System.Core.dll','System.Runtime.Serialization.dll','System.Xml.dll')}
        }
        $Settings=Join-Path $Project 'ProjectSettings\ProjectSettings.asset'
        $Baseline=[Mofumachi.Probe.SettingsObservation]::Read('B0',$Settings,[string[]]$Private)
        if($Baseline.m_APIs -cne '0b000000' -or $Baseline.automatic -cne '0' -or $Baseline.android_keystore_name -cne '<empty>' -or $Baseline.android_keyalias_name -cne '<empty>'){throw 'Expected initial source settings differ.'}
        [Mofumachi.Probe.SettingsObservation]::WriteStage($Public,$Baseline,$null)
        $Report=New-Object Mofumachi.Probe.ProbeReport;$Report.source_head=$Source;$Report.run_id=$RunId;$Report.baseline=$Baseline
        [Mofumachi.Probe.SettingsObservation]::WriteReport($Public,$Report)
        $Temp=Join-Path $Root ('temp\'+$RunId);$Cache=Join-Path $Root 'upm-cache'
        foreach($Path in @($Temp,$Cache)){No-Link $Path;$null=[IO.Directory]::CreateDirectory($Path)}
        $Info=New-Object Diagnostics.ProcessStartInfo
        $Info.FileName=$Unity;$Info.WorkingDirectory=$Project;$Info.UseShellExecute=$false;$Info.CreateNoWindow=$true
        $Info.RedirectStandardOutput=$true;$Info.RedirectStandardError=$true
        $Info.Arguments=Arguments @('-batchmode','-nographics','-quit','-buildTarget','Android','--burst-disable-compilation','-projectPath',$Project,'-logFile','-','-executeMethod','Mofumachi.Editor.Diagnostics.AndroidProjectSettingsSerializationProbe.Run','-mofumachiProbeDirectory',$Public,'-mofumachiProbeSource',$Source,'-mofumachiProbeSkipP6')
        if(-not $Info.Arguments.Contains('"-mofumachiProbeSkipP6"')){throw 'Required control argument missing.'}
        $Info.EnvironmentVariables['TEMP']=$Temp;$Info.EnvironmentVariables['TMP']=$Temp;$Info.EnvironmentVariables['UPM_CACHE_ROOT']=$Cache
        $Progress.phase='unity-start'
        Reserve $UnityReceipt @{run_id=$RunId;project=$Project;mode='skip_p6';maximum_unity=1;automatic_retries=0}
        $Editor=New-Object Diagnostics.Process;$Editor.StartInfo=$Info
        Write-Host 'Worktree verified. ONE skip-P6 control probe starts now. Keep PowerShell open.'
        $null=$Editor.Start();$Progress.unity=1
        $OutTask=$Editor.StandardOutput.ReadToEndAsync();$ErrTask=$Editor.StandardError.ReadToEndAsync()
        $Started=[DateTime]::UtcNow
        $Launch=@{pid=$Editor.Id;unity_invocations=1;project=$Project;started_utc=$Started.ToString('o');specified_executable=(Redact $Info.FileName);specified_arguments=(Redact $Info.Arguments);launching_powershell_pid=$PID;process_start_utc=$null;os_executable=$null;os_command_line=$null;os_parent_pid=$null;os_identity_read=$false}
        try{$Launch.process_start_utc=$Editor.StartTime.ToUniversalTime().ToString('o')}catch{}
        try{
            $Identity=Get-CimInstance Win32_Process -Filter ('ProcessId='+$Editor.Id)
            if($Identity){$Launch.os_executable=Redact ([string]$Identity.ExecutablePath);$Launch.os_command_line=Redact ([string]$Identity.CommandLine);$Launch.os_parent_pid=$Identity.ParentProcessId;$Launch.os_identity_read=$true}
        }catch{}
        Json 'unity-start.json' $Launch
        Write-Host ('Actual Unity PID: '+$Editor.Id+'; started UTC: '+$Launch.process_start_utc+'; P6 skip requested.')
        $Progress.phase='unity-running';$Next=[DateTime]::UtcNow;$Last=''
        while(-not $Editor.WaitForExit(1000)){
            if([DateTime]::UtcNow -ge $Next){
                $Latest=Get-ChildItem -LiteralPath (Join-Path $Public 'stages') -Filter '*.json'|Sort-Object LastWriteTimeUtc|Select-Object -Last 1
                if($Latest){$Last=$Latest.BaseName}
                Write-Host ('Unity PID '+$Editor.Id+' has not exited; elapsed minutes='+[math]::Round(([DateTime]::UtcNow-$Started).TotalMinutes,1)+'; latest observation='+$Last)
                $Next=[DateTime]::UtcNow.AddSeconds(30)
            }
        }
        $Editor.WaitForExit();$Exited=$true
    }catch{$Progress.error=@{phase=$Progress.phase;type=$_.Exception.GetType().FullName;message=(Redact $_.Exception.Message)}}
    finally{
        if($Progress.unity -eq 1 -and -not $Exited){
            Write-Host 'Waiting for the existing editor to exit; no relaunch.'
            if(-not $OutTask){$OutTask=$Editor.StandardOutput.ReadToEndAsync()};if(-not $ErrTask){$ErrTask=$Editor.StandardError.ReadToEndAsync()}
            $Editor.WaitForExit();$Exited=$true
        }
        if($Progress.unity -eq 1 -and $Exited){
            try{$ExitUtc=$Editor.ExitTime.ToUniversalTime().ToString('o')}catch{}
            try{
                $Progress.phase='read-exit';$ExitRead=$true
                $E0=[Mofumachi.Probe.SettingsObservation]::Read('E0',$Settings,[string[]]$Private)
                Json 'E0-independent.json' $E0
                $Report=[Mofumachi.Probe.SettingsObservation]::LoadReport((Join-Path $Public 'probe-report.json'))
                if($Report.source_head -cne $Source -or $Report.run_id -cne $RunId){throw 'Exit report source/run differs.'}
                $Previous=if($Report.stages.Count){$Report.stages[$Report.stages.Count-1]}else{$Report.baseline}
                [Mofumachi.Probe.SettingsObservation]::WriteStage($Public,$E0,$Previous);$Report.stages.Add($E0);$Report.unity_invocations=1
                $Report.result=if($Editor.ExitCode -eq 0 -and $Report.completed_stages -and $Report.original_signing_restored){'ProbeCompleted'}else{'Failed'}
                [Mofumachi.Probe.SettingsObservation]::WriteReport($Public,$Report)
                $P5=@($Report.stages | Where-Object {$_.stage -ceq 'P5'})
                if($P5.Count -eq 1){
                    [Mofumachi.Probe.SettingsObservation]::WriteNew((Join-Path $Public 'p5-to-e0.diff'),[Mofumachi.Probe.SettingsObservation]::RelevantDiff($P5[0],$E0))
                    Json 'p5-to-e0.json' @{p5_sha256=$P5[0].sha256;e0_sha256=$E0.sha256;file_changed=($P5[0].sha256 -cne $E0.sha256);p5_m_APIs=$P5[0].m_APIs;e0_m_APIs=$E0.m_APIs;p5_keystore=$P5[0].android_keystore_name;e0_keystore=$E0.android_keystore_name}
                }
                if(Test-Path -LiteralPath (Join-Path $Public 'p6-skipped.json') -PathType Leaf){$Skip=Get-Content -LiteralPath (Join-Path $Public 'p6-skipped.json') -Raw | ConvertFrom-Json}
            }catch{if(-not $Progress.error){$Progress.error=@{phase='read-exit';type=$_.Exception.GetType().FullName;message=(Redact $_.Exception.Message)}}}
            try{
                $Captured=$true;$RawOut=$OutTask.GetAwaiter().GetResult();$RawErr=$ErrTask.GetAwaiter().GetResult();$Text=$RawOut+"`n"+$RawErr
                $Markers=@([regex]::Matches($Text,'MOFUMACHI_SERIALIZATION_PROBE_(?:STAGE P[0-6][a-e]?|STAGE R[0-9]+|P6_SKIPPED|COMPLETE|FAILED|REPORT_FAILED)')|ForEach-Object{$_.Value})
                $Compiler=@([regex]::Matches($Text,'(?m)^[^\r\n]*\berror CS[0-9]{4}:[^\r\n]*')|ForEach-Object{Redact $_.Value}|Select-Object -Unique)
                Json 'unity-exit.json' @{exit_code=$Editor.ExitCode;process_exit_utc=$ExitUtc;exit_observed_utc=[DateTime]::UtcNow.ToString('o');unity_invocations=1;markers=$Markers;compiler_errors=$Compiler;stdout_sha256=[Mofumachi.Probe.SettingsObservation]::Hash($Utf8.GetBytes($RawOut));stderr_sha256=[Mofumachi.Probe.SettingsObservation]::Hash($Utf8.GetBytes($RawErr));raw_native_log_exported=$false}
                $RawOut=$null;$RawErr=$null;$Text=$null
            }catch{if(-not $Progress.error){$Progress.error=@{phase='native-exit-metadata';type=$_.Exception.GetType().FullName;message=(Redact $_.Exception.Message)}}}
        }
        $After=Formal-State 'formal-after' -Final
        $Unchanged=if($Before -and $After.complete){$After.head -ceq $Before.head -and $After.branch -ceq $Before.branch -and $After.clean -and $After.staged_empty}else{$null}
        Json 'formal-repo-state.json' @{before=$Before;after=$After;unchanged=$Unchanged}
        if($Progress.creation_clean){
            try{$Post=Git 'worktree-after-status' $Project @('status','--porcelain=v1','--untracked-files=all') -ReadOnly -ReturnFailure}catch{}
        }
        $ExitCode=if($Progress.unity -eq 1 -and $Exited){$Editor.ExitCode}else{$null}
        $ControlCase=Classify-P6Control $Report $E0 $Skip $ExitCode
        $Passed=$Progress.unity -eq 1 -and $ControlCase -in @('Case A','Case B') -and $Report.result -eq 'ProbeCompleted' -and $Unchanged -and -not $Progress.error -and -not $Progress.first_git_failure
        Json 'summary.json' @{source_commit=$Source;run_id=$RunId;mode='skip_p6';formal_initial_head=$(if($Before){$Before.head}else{$null});short_worktree=$Project;worktree_creation_count=$Progress.worktrees;worktree_git_exit_code=$Progress.worktree_exit_code;worktree_head=$Progress.worktree_head;worktree_clean_before_injection=$Progress.creation_clean;unity_invocation_count=$Progress.unity;unity_exit_code=$ExitCode;process_exit_utc=$ExitUtc;result=$(if($Passed){'ProbeCompleted'}else{'Failed'});error=$Progress.error;first_git_failure=$Progress.first_git_failure;classification=$ControlCase;classification_authority='summary.json: skip-P6 control A/B/C';legacy_stage_classification=$(if($Report){$Report.classification}else{'NotRun'});p6_skip_requested=$true;p6_skipped=($null -ne $Skip -and $Skip.p6_skipped -eq $true);save_assets_calls=$(if($Report){$Report.save_assets_calls}else{$null});production_saveassets_deletion_candidate=($Passed -and $ControlCase -ceq 'Case A');first_suffix_i_stage=$(if($Report){$Report.first_i_stage}else{$null});formal_repo_unchanged=$Unchanged;automatic_retries=0;build_player_count=0;aab_count=0;apk_count=0;pixel_operations=0;signing_verifications=0;core_longpaths_changed=$false;git_config_changed=$false;source_guard_changed=$false;projectsettings_manual_edit=$false;cleanup_operations=0;stage8_complete=$false;next_action='STOP; await separate instructions.'}
        $Manifest=@(Get-ChildItem -LiteralPath $Public -File -Recurse|Sort-Object FullName|ForEach-Object{@{name=$_.FullName.Substring($Public.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant();bytes=$_.Length}})
        Json 'public-manifest.json' @{files=$Manifest;excluded='Raw settings, snapshots, native logs, private environment values and key/build files.'}
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $Zip=$Run+'-public.zip';[IO.Compression.ZipFile]::CreateFromDirectory($Public,$Zip)
        Write-Host ('RESULT: '+$(if($Passed){'ProbeCompleted'}else{'Failed'})+'; worktrees='+$Progress.worktrees+'; Unity='+$Progress.unity)
        Write-Host ('CONTROL CLASSIFICATION: '+$ControlCase+'; P6 skipped='+($null -ne $Skip -and $Skip.p6_skipped -eq $true))
        Write-Host ('PUBLIC PROBE ZIP: '+$Zip)
        Write-Host 'STOP HERE. No retry, repair, config change, AAB/APK or Pixel.'
        if($Editor -and $Exited){$Editor.Dispose()};[Array]::Clear($Private,0,$Private.Length)
    }
    if(-not $Passed){throw 'STOP: evidence saved. Do not create another worktree or start Unity again.'}
}

