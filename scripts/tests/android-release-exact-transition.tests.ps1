param([Parameter(Mandatory=$true)][string]$ResultDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version 2.0
if(Test-Path -LiteralPath $ResultDirectory){throw 'Fresh result directory required; no overwrite or automatic retry.'}
$null=[IO.Directory]::CreateDirectory($ResultDirectory)
$Entry=Join-Path $PSScriptRoot '../build-android-release.ps1'
$Tokens=$null;$Errors=$null
$Ast=[Management.Automation.Language.Parser]::ParseFile($Entry,[ref]$Tokens,[ref]$Errors)
if($Errors.Count){throw 'Release script parser failure.'}
foreach($Statement in $Ast.EndBlock.Statements){
    if($Statement -is [Management.Automation.Language.FunctionDefinitionAst]){. ([scriptblock]::Create($Statement.Extent.Text))}
}
# Only function declarations are imported: no release entry, preflight, process or build is run.
$Utf8=New-Object Text.UTF8Encoding($false,$true)
$ReleaseRoot=Join-Path $ResultDirectory 'private-fixtures/validation'
$Formal=Join-Path $ResultDirectory 'private-fixtures/formal'
$Paths=@(Get-AndroidReleaseMutationPaths)
$BeforeSha='d334127167318fe7278575eb17fe2621ab2c0e39c3ab2c53b7c1f39cc555e7fc'
$AfterSha='266f17baf9d7101401580553aab9d0ede0390f6c859e137e31f6274d4713f07a'
$DiffSha='06237adb85a8490fbe6ea1f32b59fdbf8f0548cdbc706d9cd1d6d15a8e87b596'
$SourceFile=Join-Path $PSScriptRoot '../../ProjectSettings/ProjectSettings.asset'
# Build controlled fixture bytes in memory. The real repository asset is read only.
$Lf=$Utf8.GetString([IO.File]::ReadAllBytes($SourceFile)).Replace("`r`n","`n")
$Key="  AndroidKeystoreName: `n"
$Graphics="  - m_BuildTarget: Android`n    m_APIs: 0b000000`n    m_Automatic: 0`n"
if([regex]::Matches($Lf,[regex]::Escape($Key)).Count -ne 1 -or [regex]::Matches($Lf,[regex]::Escape($Graphics)).Count -ne 1){throw 'Pinned fixture anchors differ.'}
$BeforeBytes=$Utf8.GetBytes($Lf.Replace("`n","`r`n"))
$AfterText=$Lf.Replace($Key,"  AndroidKeystoreName: '{inproject}: '`n").Replace($Graphics,"  - m_BuildTarget: Android`n    m_APIs: 0b000000i`n    m_Automatic: 0`n")
$AfterBytes=$Utf8.GetBytes($AfterText)
if($BeforeBytes.Length -ne 24343 -or (Get-AndroidReleaseBytesHash $BeforeBytes) -cne $BeforeSha -or $AfterBytes.Length -ne 23513 -or (Get-AndroidReleaseBytesHash $AfterBytes) -cne $AfterSha){throw 'Exact fixture fingerprints differ.'}
$Rows=New-Object Collections.Generic.List[object]
$script:FixtureNumber=0;$script:Fixture=$null
$OriginalRestore=(Get-Command Restore-AndroidReleaseSnapshotFile).ScriptBlock
function Check([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Case([string]$Name,[scriptblock]$Body){
    try{& $Body;$Rows.Add(@{name=$Name;result='Passed'});Write-Host ('PASS '+$Name)}
    catch{$Rows.Add(@{name=$Name;result='Failed';error=$_.Exception.Message});throw}
}
function Fixture([byte[]]$Original=$BeforeBytes,[byte[]]$Current=$AfterBytes){
    $script:FixtureNumber++
    $Run='rc-20261113-000000-'+$script:FixtureNumber.ToString('x8')
    $Project=Join-Path $ReleaseRoot ('projects/'+$Run)
    $Directory=Join-Path $ReleaseRoot ('Diagnostics/release-candidate/'+$Run+'/aab/source-recovery')
    $null=[IO.Directory]::CreateDirectory($Directory)
    $Files=@();$Expected=@{}
    for($i=0;$i -lt $Paths.Count;$i++){
        $Bytes=if($i -eq 2){$Original}else{[IO.File]::ReadAllBytes((Join-Path $PSScriptRoot ('../../'+$Paths[$i])))}
        $Target=Join-Path $Project $Paths[$i]
        $null=[IO.Directory]::CreateDirectory((Split-Path -Parent $Target))
        [IO.File]::WriteAllBytes($Target,$Bytes)
        $Snapshot=Join-Path $Directory ('snapshot-'+$i+'.bin')
        [IO.File]::WriteAllBytes($Snapshot,$Bytes)
        $Sha=Get-AndroidReleaseBytesHash $Bytes
        $Files+=@{relative=$Paths[$i];snapshot=$Snapshot;sha256=$Sha;length=$Bytes.Length}
        $Expected[$Paths[$i]]=$Sha
    }
    $Target=Join-Path $Project $Paths[2]
    [IO.File]::WriteAllBytes($Target,$Current)
    $F=[pscustomobject]@{project=$Project;directory=$Directory;target=$Target;baseline=('1'*40);formal=$Formal;files=$Files;expected=$Expected;untracked=@();extra_tracked=@();final_guard_calls=0}
    $script:Fixture=$F
    return $F
}
# Test-only transport/scope doubles. The production ordinary guard, recovery,
# File.Replace and filesystem/hash functions execute unchanged. No Git is invoked.
function Assert-AndroidReleaseRecoveryScope([string]$Git,[string]$Project,[string]$Baseline,[string]$Directory,[string]$Formal){
    if(-not(Test-AndroidReleaseUnder $Project $ReleaseRoot) -or -not(Test-AndroidReleaseUnder $Directory $ReleaseRoot)){throw 'Fixture escaped private test root.'}
    foreach($Path in @($Project,$Directory)){Assert-AndroidReleaseNoLink $Path}
}
function Invoke-AndroidReleaseGit([string]$Git,[string]$Project,[string[]]$Arguments){
    $F=$script:Fixture
    $Changed=@($Paths | Where-Object {(Get-AndroidReleaseBytesHash ([IO.File]::ReadAllBytes((Join-Path $F.project $_)))) -cne $F.expected[$_]})+@($F.extra_tracked)
    $Code=0;$Out=''
    if($Arguments[0] -ceq 'rev-parse'){$Out=$F.baseline+"`n"}
    elseif($Arguments[0] -ceq 'diff'){
        if($Arguments -contains '--cached'){}
        elseif($Arguments -contains '--name-only'){$Out=($Changed -join [char]0);if($Out){$Out+=[char]0}}
        elseif($Arguments -contains '--quiet'){if($Changed.Count){$Code=1}}
        else{$Out='Fixture transport; physical full-file diff is independently checked by production recovery.'}
    }elseif($Arguments[0] -ceq 'ls-files'){
        if($Arguments -contains '--others'){$Out=($F.untracked -join [char]0);if($Out){$Out+=[char]0}}
    }else{throw 'Unexpected fixture Git operation.'}
    return [pscustomobject]@{exit_code=$Code;stdout=$Out;stderr=''}
}
function Recover($F){return Repair-AndroidReleaseBuildSource 'fixture-transport' $F}
function Reject([byte[]]$Original,[byte[]]$Current,[string[]]$Untracked=@(),[string[]]$ExtraTracked=@()){
    $F=Fixture $Original $Current;$F.untracked=$Untracked;$F.extra_tracked=$ExtraTracked
    $Initial=Get-AndroidReleaseBytesHash ([IO.File]::ReadAllBytes($F.target))
    $Report=Recover $F
    Check (-not $Report.passed) 'Unknown transition was accepted.'
    Check ((Get-AndroidReleaseBytesHash ([IO.File]::ReadAllBytes($F.target))) -ceq $Initial) 'Rejected input was restored.'
    Check ($Report.restored.Count -eq 0) 'Rejected input generated a restore record.'
    Check (-not(Test-Path -LiteralPath (Join-Path $F.directory 'restore-backups'))) 'Rejected input created backups.'
}
$Failure=$null
try{
    Case '1 exact physical fingerprints and known diff are recognized' {
        $F=Fixture;$R=Recover $F
        Check $R.passed ('Known transition rejected: '+$R.problem)
        Check ($R.projectsettings_transition.diff_sha256 -ceq $DiffSha) 'Known diff fingerprint differs.'
        Check ($R.projectsettings_transition.changed_lines.Count -eq 2) 'Known diff gained a field.'
        Check $R.final_guard.unchanged 'Final ordinary guard did not run successfully.'
    }
    Case '2 a one-byte after mutation is rejected before restoration' {
        $Bad=[byte[]]$AfterBytes.Clone();$Bad[$Bad.Length-1]=[byte]32;Reject $BeforeBytes $Bad
    }
    Case '3 another graphics suffix is rejected' {Reject $BeforeBytes ($Utf8.GetBytes($AfterText.Replace('0b000000i','0b000000j')))}
    Case '4 another keystore binding is rejected' {Reject $BeforeBytes ($Utf8.GetBytes($AfterText.Replace("'{inproject}: '","'other-binding'")))}
    Case '5 an additional unknown field is rejected' {Reject $BeforeBytes ($Utf8.GetBytes($AfterText+"  unknownMutation: 1`n"))}
    Case '6 real File.Replace restores byte-perfect snapshot bytes' {
        $F=Fixture;$R=Recover $F
        Check $R.passed $R.problem
        $Restored=[IO.File]::ReadAllBytes($F.target)
        Check ($Restored.Length -eq 24343 -and (Get-AndroidReleaseBytesHash $Restored) -ceq $BeforeSha) 'Restored SHA/size differs.'
        Check ([Convert]::ToBase64String($Restored) -ceq [Convert]::ToBase64String($BeforeBytes)) 'Restored bytes differ.'
    }
    Case '7 backup retains exact postbuild SHA and remains in existing Diagnostics' {
        $F=Fixture;$R=Recover $F;Check $R.passed $R.problem
        $Backup=$R.restored[0].backup
        Check (Test-AndroidReleaseUnder $Backup $F.directory) 'Backup escaped Diagnostics.'
        $Bytes=[IO.File]::ReadAllBytes($Backup)
        Check ($Bytes.Length -eq 23513 -and (Get-AndroidReleaseBytesHash $Bytes) -ceq $AfterSha) 'Backup SHA/size differs.'
        Check ([Convert]::ToBase64String($Bytes) -ceq [Convert]::ToBase64String($AfterBytes)) 'Backup bytes differ.'
        $Proof=Get-Content -LiteralPath $R.restored[0].backup_record -Raw | ConvertFrom-Json
        Check ($Proof.state -ceq 'Verified' -and $Proof.backup_sha256 -ceq $AfterSha -and $Proof.target_sha256_after -ceq $BeforeSha) 'Atomic restore proof incomplete.'
    }
    Case '8 corruption after real replacement fails restored-SHA verification' {
        $F=Fixture
        try{
            Set-Item Function:Restore-AndroidReleaseSnapshotFile {
                param($Payload,$Target,$Backup,$SnapshotSha,$TargetSha)
                $Proof=& $OriginalRestore $Payload $Target $Backup $SnapshotSha $TargetSha
                [IO.File]::WriteAllBytes($Target,[byte[]]@(1,2,3));return $Proof
            }
            $R=Recover $F
            Check (-not $R.passed -and $R.problem -match 'Restore SHA mismatch') 'Post-restore corruption passed.'
        }finally{Set-Item Function:Restore-AndroidReleaseSnapshotFile $OriginalRestore}
    }
    Case '9 corruption of the retained backup fails recovery' {
        $F=Fixture
        try{
            Set-Item Function:Restore-AndroidReleaseSnapshotFile {
                param($Payload,$Target,$Backup,$SnapshotSha,$TargetSha)
                $Proof=& $OriginalRestore $Payload $Target $Backup $SnapshotSha $TargetSha
                [IO.File]::WriteAllBytes($Backup,[byte[]]@(1,2,3));return $Proof
            }
            $R=Recover $F
            Check (-not $R.passed -and $R.problem -match 'backup verification failed') 'Corrupt backup passed.'
        }finally{Set-Item Function:Restore-AndroidReleaseSnapshotFile $OriginalRestore}
    }
    Case '10 wrong before SHA with the exact after is rejected' {
        $Bad=[byte[]]$BeforeBytes.Clone();$Bad[0]=[byte]33;Reject $Bad $AfterBytes
    }
    Case '11 wrong after size is rejected' {Reject $BeforeBytes ([byte[]]$AfterBytes[0..($AfterBytes.Length-2)])}
    Case '12 unexpected untracked path blocks every restore' {Reject $BeforeBytes $AfterBytes @('unexpected.txt')}
    Case '13 ordinary guard still rejects the changed physical file before recovery' {
        $F=Fixture
        $G=Get-AndroidReleaseSourceCheck 'fixture-transport' $F.project $F.baseline (Join-Path $F.directory 'ordinary-before')
        Check (-not $G.unchanged -and $G.working_diff_exit -eq 1) 'Ordinary guard was bypassed.'
    }
    Case '14 removing the suffix still fails the observed-before exact gate' {Reject $BeforeBytes ($Utf8.GetBytes($AfterText.Replace('0b000000i','0b000000')))}
    Case '15 unexpected fourth tracked file blocks every restore' {Reject $BeforeBytes $AfterBytes @() @('Assets/unknown.txt')}
    Case '16 suffix-free changes with unknown full fingerprints are rejected' {
        $LegacyBefore=$Utf8.GetBytes($Lf)
        $LegacyAfter=$Utf8.GetBytes($Lf.Replace($Key,"  AndroidKeystoreName: '{inproject}: '`n").Replace('    m_APIs: 0b000000','    m_APIs: 0B000000'))
        Reject $LegacyBefore $LegacyAfter
    }
    Case '17 updated existing suite fixtures retain known URP and exact ProjectSettings recovery' {
        $SuiteAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'android-release.tests.ps1'),[ref]$Tokens,[ref]$Errors)
        Check ($Errors.Count -eq 0) 'Existing suite does not parse.'
        $Helpers=$SuiteAst.FindAll({param($Node) $Node -is [Management.Automation.Language.FunctionDefinitionAst] -and $Node.Name -in @('Change','Known')},$true)
        Check ($Helpers.Count -eq 2) 'Existing suite fixture helpers differ.'
        foreach($Helper in $Helpers){. ([scriptblock]::Create($Helper.Extent.Text))}
        $F=Fixture $BeforeBytes $BeforeBytes
        Known $F
        Check ((Get-AndroidReleaseBytesHash ([IO.File]::ReadAllBytes($F.target))) -ceq $AfterSha) 'Existing suite known-after bytes differ.'
        $R=Recover $F;Check $R.passed $R.problem
        Check ($R.restored.Count -eq 3 -and $R.final_guard.unchanged) 'Known three-file recovery changed.'
    }
}catch{$Failure=$_}
$Summary=[ordered]@{scope='Exact source-recovery recognition and real filesystem restore only; transport/scope doubles are test-only';total=$Rows.Count;passed=@($Rows|Where-Object {$_.result -ceq 'Passed'}).Count;failed=@($Rows|Where-Object {$_.result -ceq 'Failed'}).Count;cases=$Rows.ToArray();before_sha256=$BeforeSha;after_sha256=$AfterSha;diff_sha256=$DiffSha;unity_invocations=0;aab_count=0;apk_count=0;pixel_operations=0;automatic_retries=0;native_git_config_changes=0;native_git_commits=0;worktree_creations=0;private_fixture_bodies_shared=$false;windows_native_restore_verified=$false;error=$(if($Failure){$Failure.Exception.Message}else{$null});error_stack=$(if($Failure){$Failure.ScriptStackTrace}else{$null});error_position=$(if($Failure){$Failure.InvocationInfo.PositionMessage}else{$null})}
[IO.File]::WriteAllText((Join-Path $ResultDirectory 'results.json'),($Summary|ConvertTo-Json -Depth 8),(New-Object Text.UTF8Encoding($false)))
Write-Host ('TOTAL='+$Summary.total+' PASS='+$Summary.passed+' FAIL='+$Summary.failed+'; Unity/build/device=0')
if($Failure){throw $Failure}
