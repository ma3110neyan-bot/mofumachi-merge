param([switch]$SourceGuardOnly, [string]$ResultDirectory = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$Entry = Join-Path $PSScriptRoot '../build-android-release.ps1'
if (-not (Test-Path -LiteralPath $Entry)) { throw 'Missing release-only entry (expected before implementation).' }
. $Entry
$script:Checks = 0
function Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:Checks++
}
function Test-ReleaseSourceRecovery {
    $TestRoot = Join-Path ([IO.Path]::GetTempPath()) ('release-source-guard-' + [guid]::NewGuid().ToString('N'))
    $Formal = Join-Path $TestRoot 'formal'
    $OriginalRoot = $script:ReleaseRoot
    $script:ReleaseRoot = Join-Path $TestRoot 'validation'
    $Rows = New-Object Collections.Generic.List[object]
    $script:FixtureNumber = 0
    $Git = (Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
    function FixtureGit([string[]]$Arguments) {
        $Native = Invoke-AndroidReleaseGit $Git $Formal $Arguments
        if ($Native.exit_code -ne 0) { throw ('Fixture Git failed: ' + $Native.stderr) }
        return $Native.stdout
    }
    function Case([string]$Name, [scriptblock]$Body) {
        try {
            & $Body
            $Rows.Add(@{name=$Name;result='Passed'})
            Write-Host ('PASS ' + $Name)
        } catch {
            $Rows.Add(@{name=$Name;result='Failed';error=$_.Exception.Message;stack=$_.ScriptStackTrace;position=$_.InvocationInfo.PositionMessage})
            throw
        }
    }
    $Paths = @('Assets/Settings/UniversalRP.asset','Assets/Settings/UniversalRenderPipelineGlobalSettings.asset','ProjectSettings/ProjectSettings.asset')
    function Fixture {
        $script:FixtureNumber++
        $Id = 'rc-20261110-000000-' + $script:FixtureNumber.ToString('x8')
        $Project = Join-Path $ReleaseRoot ('projects/' + $Id)
        $Diagnostics = Join-Path $ReleaseRoot ('Diagnostics/release-candidate/' + $Id + '/aab/source-recovery')
        $null = FixtureGit @('worktree','add','--detach',$Project,$Revision)
        $Plan = New-AndroidReleaseBuildSnapshot $Git $Project $Revision $Diagnostics $Formal
        return @{project=$Project;plan=$Plan;diagnostics=$Diagnostics}
    }
    function Change($Fixture, [string]$Name, [string]$Old, [string]$New) {
        $Path = Join-Path $Fixture.project $Name
        $Text = [IO.File]::ReadAllText($Path)
        if (-not $Text.Contains($Old)) { throw ('Fixture literal not found: ' + $Name + ': ' + $Old) }
        [IO.File]::WriteAllText($Path,$Text.Replace($Old,$New),(New-Object Text.UTF8Encoding($false)))
    }
    function Known($Fixture) {
        Change $Fixture $Paths[0] '  m_PrefilterXRKeywords: 0' '  m_PrefilterXRKeywords: 1'
        Change $Fixture $Paths[1] "    m_RuntimeSettings:`n      m_List: []" "    m_RuntimeSettings:`n      m_List:`n      - rid: 3114554777721110528"
        Change $Fixture $Paths[2] "  AndroidKeystoreName: `n" "  AndroidKeystoreName: '{inproject}: '`n"
        Change $Fixture $Paths[2] '    m_APIs: 0b000000' '    m_APIs: 0B000000'
    }
    function Generated($Fixture) {
        $Path = Join-Path $Fixture.project '.utmp/tools/release/arm64-v8a/compile_commands.json'
        $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $Path))
        [IO.File]::WriteAllText($Path,'[]')
        return $Path
    }
    function NewFile($Fixture, [string]$Relative, [string]$Text) {
        $Path = Join-Path $Fixture.project $Relative
        $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $Path))
        [IO.File]::WriteAllText($Path,$Text)
        return $Path
    }
    function Recover($Fixture) { return Repair-AndroidReleaseBuildSource $Git $Fixture.plan }
    function BackupFixture {
        $F=Fixture;Known $F
        $Target=Join-Path $F.project $Paths[0]
        $Payload=Join-Path $F.diagnostics ('backup-test-'+[guid]::NewGuid().ToString('N')+'.tmp')
        [IO.File]::WriteAllBytes($Payload,[IO.File]::ReadAllBytes($F.plan.files[0].snapshot))
        $Parent=Join-Path $F.diagnostics ('restore-backups/'+[guid]::NewGuid().ToString('N'))
        $null=[IO.Directory]::CreateDirectory($Parent)
        return @{fixture=$F;payload=$Payload;target=$Target;backup=(Join-Path $Parent 'UniversalRP.asset.bak');snapshot_sha=(Get-FileHash -LiteralPath $Payload -Algorithm SHA256).Hash.ToLowerInvariant();target_sha=(Get-FileHash -LiteralPath $Target -Algorithm SHA256).Hash.ToLowerInvariant()}
    }
    function RejectBackup($B,$Path) {
        $Before=[Convert]::ToBase64String([IO.File]::ReadAllBytes($B.target))
        $Rejected=$false
        try { $null=Restore-AndroidReleaseSnapshotFile $B.payload $B.target $Path $B.snapshot_sha $B.target_sha } catch {$Rejected=$true}
        Check $Rejected 'Invalid backup conditions accepted.'
        Check ([Convert]::ToBase64String([IO.File]::ReadAllBytes($B.target)) -ceq $Before) 'Rejected backup changed the target.'
        Check (Test-Path -LiteralPath $B.payload) 'Rejected backup deleted the payload.'
    }
    function RejectUnchanged($Fixture, [string]$Message) {
        $Before = @($Paths | ForEach-Object {[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $Fixture.project $_)))})
        Check (-not (Recover $Fixture).passed) $Message
        for ($i=0;$i -lt $Paths.Count;$i++) {
            Check ([Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $Fixture.project $Paths[$i]))) -ceq $Before[$i]) 'Rejected input was restored before all validations passed.'
        }
    }
    $Failure = $null
    try {
        $null = [IO.Directory]::CreateDirectory($Formal)
        $null = FixtureGit @('init','--quiet')
        $null = FixtureGit @('config','user.name','SourceGuardFixture')
        $null = FixtureGit @('config','user.email','fixture@example.invalid')
        $null = FixtureGit @('config','core.autocrlf','false')
        foreach ($Name in $Paths) {
            $Destination = Join-Path $Formal $Name
            $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $Destination))
            $FixtureText=[IO.File]::ReadAllText((Join-Path $PSScriptRoot ('../../'+$Name))).Replace("`r`n","`n")
            [IO.File]::WriteAllText($Destination,$FixtureText,(New-Object Text.UTF8Encoding($false)))
        }
        [IO.File]::WriteAllText((Join-Path $Formal 'Assets/game.txt'),'unchanged game')
        $null = FixtureGit @('add','--','Assets','ProjectSettings')
        $null = FixtureGit @('commit','--quiet','-m','source fixture')
        $Revision = (FixtureGit @('rev-parse','HEAD')).Trim()
        $FormalBytes = @($Paths | ForEach-Object {[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $Formal $_)))})
        Case 'known three-file mutations restore exact snapshot bytes and final guard passes' {
            $F = Fixture; Known $F
            $Report = Recover $F
            Check $Report.passed 'Known mutations rejected.'
            Check $Report.final_guard.unchanged 'Final ordinary source guard did not pass.'
            foreach ($Name in $Paths) {
                Check ([Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $F.project $Name))) -ceq [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $Formal $Name)))) 'Snapshot restore did not preserve exact bytes.'
            }
            Check ($Report.restored.Count -eq 3) 'Expected three verified restore records.'
            foreach ($Row in $Report.restored) {
                Check (-not [string]::IsNullOrWhiteSpace($Row.backup)) 'Backup path empty.'
                Check ($Row.backup.StartsWith($F.diagnostics+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) 'Backup outside Diagnostics.'
                Check (Test-Path -LiteralPath $Row.backup -PathType Leaf) 'Verified backup was not retained.'
                Check ((Get-FileHash -LiteralPath $Row.backup -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $Row.before_sha256) 'Backup does not match restore-before target.'
                $Evidence=[IO.File]::ReadAllText($Row.backup_record)|ConvertFrom-Json
                Check ($Evidence.state -ceq 'Verified' -and $Evidence.backup_sha256 -ceq $Row.before_sha256 -and $Evidence.target_sha256_after -ceq $Row.sha256) 'Restore relation/SHA proof incomplete.'
            }
        }
        Case 'empty backup path is rejected without changing target or payload' { $B=BackupFixture;RejectBackup $B '' }
        Case 'null backup path is rejected without changing target or payload' { $B=BackupFixture;RejectBackup $B $null }
        Case 'backup outside Diagnostics is rejected' {
            $B=BackupFixture;$Parent=Join-Path $TestRoot 'outside-backup';$null=[IO.Directory]::CreateDirectory($Parent)
            RejectBackup $B (Join-Path $Parent 'outside.bak')
        }
        Case 'existing backup file is never overwritten' {
            $B=BackupFixture;[IO.File]::WriteAllText($B.backup,'retain existing backup')
            RejectBackup $B $B.backup
            Check ([IO.File]::ReadAllText($B.backup) -ceq 'retain existing backup') 'Existing backup overwritten.'
        }
        Case 'missing backup parent is rejected before replacement' {
            $B=BackupFixture;RejectBackup $B (Join-Path (Split-Path -Parent $B.backup) 'missing/backup.bak')
        }
        Case 'payload SHA mismatch is rejected before replacement' {
            $B=BackupFixture;[IO.File]::WriteAllText($B.payload,'not snapshot bytes')
            RejectBackup $B $B.backup
            Check (-not (Test-Path -LiteralPath $B.backup)) 'Backup created for corrupt payload.'
        }
        Case 'target SHA mismatch is rejected before replacement' {
            $B=BackupFixture;$B.target_sha='0'*64;RejectBackup $B $B.backup
            Check (-not (Test-Path -LiteralPath $B.backup)) 'Backup created for changed target.'
        }
        Case 'backup corruption after replacement cannot pass recovery' {
            $F=Fixture;Known $F
            $script:OriginalBackupRestore=(Get-Command Restore-AndroidReleaseSnapshotFile).ScriptBlock
            try {
                Set-Item Function:script:Restore-AndroidReleaseSnapshotFile {
                    param($Payload,$Target,$Backup,$SnapshotSha,$TargetSha)
                    $Proof=& $script:OriginalBackupRestore $Payload $Target $Backup $SnapshotSha $TargetSha
                    [IO.File]::WriteAllText($Backup,'corrupted backup')
                    return $Proof
                }
                Check (-not (Recover $F).passed) 'Corrupt backup accepted.'
            } finally { Set-Item Function:script:Restore-AndroidReleaseSnapshotFile $script:OriginalBackupRestore }
        }
        Case 'known utmp intermediate is inventoried and removed' {
            $F=Fixture; $Generated=Generated $F
            $Report=Recover $F
            Check $Report.passed 'Known .utmp rejected.'
            Check (-not (Test-Path -LiteralPath $Generated)) 'Known .utmp not removed.'
            Check (Test-Path -LiteralPath (Join-Path $F.diagnostics 'cleanup-inventory.json')) 'Cleanup inventory absent.'
        }
        Case 'exact Gradle shutdown log is removed' {
            $F=Fixture; $Log=NewFile $F 'build/outputs/logs/unity---stop-build.log' "Stopping Daemon(s)`n1 Daemon stopped`n"
            Check (Recover $F).passed 'Known shutdown log rejected.'
            Check (-not (Test-Path -LiteralPath $Log)) 'Shutdown log remains.'
        }
        Case 'root vsconfig is preserved after combined recovery and cleanup' {
            $F=Fixture;Known $F;$null=Generated $F
            $File=NewFile $F '.vsconfig' '{"version":"1.0"}'
            Check (Recover $F).passed 'Combined recovery failed.'
            Check ([IO.File]::ReadAllText($File) -ceq '{"version":"1.0"}') 'Root .vsconfig changed.'
        }
        Case 'post-build CRLF and Light2D list retain exact original snapshot bytes' {
            $F=Fixture;Known $F
            Change $F $Paths[0] '  m_Light2DKeptVariantCombos: []' "  m_Light2DKeptVariantCombos:`n  - 1`n  - 4"
            foreach ($Name in $Paths) {
                $File=Join-Path $F.project $Name
                [IO.File]::WriteAllText($File,[IO.File]::ReadAllText($File).Replace("`n","`r`n"),(New-Object Text.UTF8Encoding($false)))
            }
            Check (Recover $F).passed 'Known metadata with CRLF rejected.'
            foreach ($Name in $Paths) { Check ([Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $F.project $Name))) -ceq [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $Formal $Name)))) 'Original line-ending bytes not restored.' }
        }
        Case 'atomic restore does not change another hard-linked file' {
            $F=Fixture;Known $F
            $Target=Join-Path $F.project $Paths[0]
            $Outside=Join-Path $TestRoot 'outside-hardlink.asset'
            $Before=[IO.File]::ReadAllBytes($Target)
            [IO.File]::WriteAllBytes($Outside,$Before)
            [IO.File]::Delete($Target)
            $null=New-Item -ItemType HardLink -Path $Target -Target $Outside
            Check (Recover $F).passed 'Hard-linked target was not safely replaced.'
            Check ([Convert]::ToBase64String([IO.File]::ReadAllBytes($Outside)) -ceq [Convert]::ToBase64String($Before)) 'Restore changed the other hard-linked file.'
        }
        Case 'fourth tracked file change blocks all restoration' {
            $F=Fixture;Known $F;$null=NewFile $F 'Assets/game.txt' 'changed game'
            RejectUnchanged $F 'Fourth tracked path accepted.'
        }
        Case 'renderer setting outside shader prefilters is rejected' {
            $F=Fixture;Known $F;Change $F $Paths[0] '  m_RenderScale: 1' '  m_RenderScale: 0.5'
            RejectUnchanged $F 'Renderer mutation accepted.'
        }
        Case 'unknown shader prefilter field cannot hide as known metadata' {
            $F=Fixture;Change $F $Paths[0] '  m_PrefilterXRKeywords: 0' "  m_PrefilterXRKeywords: 0`n  m_PrefilterUnknown: 1"
            RejectUnchanged $F 'Unknown prefilter key accepted.'
        }
        Case 'invalid Light2D serialized variant is rejected' {
            $F=Fixture;Change $F $Paths[0] '  m_Light2DKeptVariantCombos: []' "  m_Light2DKeptVariantCombos:`n  - 999999999999999999999999"
            RejectUnchanged $F 'Invalid variant serialization accepted.'
        }
        Case 'global URP setting outside runtime RID list is rejected' {
            $F=Fixture;Known $F;Change $F $Paths[1] '  m_StripUnusedVariants: 1' '  m_StripUnusedVariants: 0'
            RejectUnchanged $F 'Global URP mutation accepted.'
        }
        Case 'unresolved runtime RID is rejected' {
            $F=Fixture;Change $F $Paths[1] "    m_RuntimeSettings:`n      m_List: []" "    m_RuntimeSettings:`n      m_List:`n      - rid: 123456789"
            RejectUnchanged $F 'Unknown runtime RID accepted.'
        }
        Case 'application identifier change is rejected' {
            $F=Fixture;Known $F;Change $F $Paths[2] 'Android: com.mofumachi.merge' 'Android: com.example.changed'
            RejectUnchanged $F 'Package mutation accepted.'
        }
        Case 'version code change is rejected' {
            $F=Fixture;Known $F;Change $F $Paths[2] '  AndroidBundleVersionCode: 1' '  AndroidBundleVersionCode: 2'
            RejectUnchanged $F 'Version code mutation accepted.'
        }
        Case 'non-hex graphics suffix is rejected before restore' {
            $F=Fixture;Known $F;Change $F $Paths[2] '    m_APIs: 0B000000' '    m_APIs: 0b000000i'
            RejectUnchanged $F 'Unconfirmed non-hex graphics value accepted.'
        }
        Case 'different graphics API is rejected' {
            $F=Fixture;Change $F $Paths[2] '    m_APIs: 0b000000' '    m_APIs: 15000000'
            RejectUnchanged $F 'Different graphics API accepted.'
        }
        Case 'nonempty keystore path change is rejected' {
            $F=Fixture;Change $F $Paths[2] "  AndroidKeystoreName: `n" "  AndroidKeystoreName: unrelated-fixture.jks`n"
            RejectUnchanged $F 'Unknown keystore binding accepted.'
        }
        foreach ($Relative in @('.utmp2/file.json','nested/.utmp/file.json','build/outputs/logs/other.log','unrelated.txt','.utmp/unrelated.cs')) {
            Case ('unknown untracked path rejected: '+$Relative) {
                $F=Fixture;Known $F;$File=NewFile $F $Relative 'unrelated'
                RejectUnchanged $F 'Unknown untracked path accepted.'
                Check (Test-Path -LiteralPath $File) 'Unknown file deleted.'
            }
        }
        Case 'invalid generated JSON fails without deleting it' {
            $F=Fixture;$File=Generated $F;[IO.File]::WriteAllText($File,'not JSON')
            RejectUnchanged $F 'Invalid generated content accepted.'
            Check (Test-Path -LiteralPath $File) 'Invalid generated file deleted.'
        }
        Case 'corrupted snapshot is rejected before any restoration' {
            $F=Fixture;Known $F;[IO.File]::WriteAllText($F.plan.files[0].snapshot,'corrupt snapshot')
            RejectUnchanged $F 'Corrupt snapshot accepted.'
        }
        Case 'restore SHA mismatch cannot pass final guard' {
            $F=Fixture;Known $F
            $Original=(Get-Command Restore-AndroidReleaseSnapshotFile).ScriptBlock
            try {
                Set-Item Function:script:Restore-AndroidReleaseSnapshotFile { param($Payload,$Target) [IO.File]::WriteAllText($Target,'bad restore') }
                Check (-not (Recover $F).passed) 'Restore readback mismatch accepted.'
            } finally { Set-Item Function:script:Restore-AndroidReleaseSnapshotFile $Original }
        }
        Case 'cleanup failure cannot pass final guard' {
            $F=Fixture;$File=Generated $F
            $Original=(Get-Command Remove-AndroidReleaseGeneratedItem).ScriptBlock
            try {
                Set-Item Function:script:Remove-AndroidReleaseGeneratedItem { param($Path,$Directory) throw 'fixture cleanup failure' }
                Check (-not (Recover $F).passed) 'Cleanup failure accepted.'
                Check (Test-Path -LiteralPath $File) 'Failure fixture unexpectedly deleted.'
            } finally { Set-Item Function:script:Remove-AndroidReleaseGeneratedItem $Original }
        }
        Case 'Git diff retrieval failure blocks restore and cleanup' {
            $F=Fixture;Known $F
            $OriginalGit=(Get-Command Invoke-AndroidReleaseGit).ScriptBlock
            $script:OriginalSourceGuardGit=$OriginalGit
            try {
                Set-Item Function:script:Invoke-AndroidReleaseGit {
                    param($Git,$Project,$Arguments)
                    if ($Arguments -contains '--name-only') { return [pscustomobject]@{exit_code=128;stdout='';stderr='fixture diff retrieval failure'} }
                    return & $script:OriginalSourceGuardGit $Git $Project $Arguments
                }
                RejectUnchanged $F 'Diff retrieval error accepted.'
            } finally { Set-Item Function:script:Invoke-AndroidReleaseGit $OriginalGit }
        }
        Case 'staged change blocks recovery' {
            $F=Fixture;Known $F
            $Stage=Invoke-AndroidReleaseGit $Git $F.project @('add','--','Assets/Settings/UniversalRP.asset')
            Check ($Stage.exit_code -eq 0) 'Fixture staging failed.'
            RejectUnchanged $F 'Staged change accepted.'
        }
        Case 'worktree HEAD drift blocks recovery' {
            $F=Fixture;Known $F
            $Tree=(FixtureGit @('rev-parse',($Revision+'^{tree}'))).Trim()
            $OtherRevision=(FixtureGit @('commit-tree',$Tree,'-p',$Revision,'-m','detached drift')).Trim()
            $Move=Invoke-AndroidReleaseGit $Git $F.project @('update-ref','HEAD',$OtherRevision)
            Check ($Move.exit_code -eq 0) 'Fixture HEAD movement failed.'
            RejectUnchanged $F 'Worktree HEAD drift accepted.'
        }
        Case 'linked generated directory is not followed or cleaned' {
            $F=Fixture
            $Outside=Join-Path $TestRoot 'outside-generated'
            $OutsideFile=Join-Path $Outside 'release/arm64-v8a/compile_commands.json'
            $null=[IO.Directory]::CreateDirectory((Split-Path -Parent $OutsideFile))
            [IO.File]::WriteAllText($OutsideFile,'[]')
            $null=[IO.Directory]::CreateDirectory((Join-Path $F.project '.utmp'))
            $Kind=if ($env:OS -eq 'Windows_NT') {'Junction'} else {'SymbolicLink'}
            $null=New-Item -ItemType $Kind -Path (Join-Path $F.project '.utmp/tools') -Target $Outside
            $Report=Recover $F
            Check (-not $Report.passed -and $Report.problem -match 'Linked') 'Linked generated directory accepted or followed.'
            Check ([IO.File]::ReadAllText($OutsideFile) -ceq '[]') 'Link target was modified.'
        }
        Case 'formal repository change blocks recovery before any restore' {
            $F=Fixture;Known $F
            $File=Join-Path $Formal 'Assets/game.txt'
            try {
                [IO.File]::WriteAllText($File,'concurrent formal change')
                RejectUnchanged $F 'Formal repository change accepted.'
            } finally { [IO.File]::WriteAllText($File,'unchanged game') }
        }
        Case 'formal repository cannot be used as recovery target' {
            $Rejected=$false
            try { $null=New-AndroidReleaseBuildSnapshot $Git $Formal $Revision (Join-Path $ReleaseRoot 'Diagnostics/release-candidate/formal-reject') $Formal } catch {$Rejected=$true}
            Check $Rejected 'Formal repository accepted as recovery target.'
        }
        Case 'invalid diagnostic target cannot write into formal repository' {
            $F=Fixture;Known $F;$F.plan.directory=$Formal
            RejectUnchanged $F 'Formal directory accepted as recovery Diagnostics.'
            Check (-not (Test-Path -LiteralPath (Join-Path $Formal 'recovery-result.json'))) 'Recovery error report wrote to formal repository.'
        }
        Case 'formal repository contents and Git state remain unchanged' {
            for ($i=0;$i -lt $Paths.Count;$i++) { Check ([Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $Formal $Paths[$i]))) -ceq $FormalBytes[$i]) 'Formal source bytes changed.' }
            Check ([string]::IsNullOrWhiteSpace((FixtureGit @('status','--porcelain=v1','--untracked-files=all')))) 'Formal repository dirty.'
        }
    } catch { $Failure=$_.Exception.Message; Write-Host ('FIRST FAILURE: '+$Failure) }
    finally {
        $script:ReleaseRoot=$OriginalRoot
        if ([string]::IsNullOrWhiteSpace($ResultDirectory)) { $ResultDirectory=Join-Path $TestRoot 'results' }
        $null=[IO.Directory]::CreateDirectory($ResultDirectory)
        $Result=@{scope='release source guard only';total=$Rows.Count;passed=@($Rows | Where-Object {$_.result -eq 'Passed'}).Count;failed=@($Rows | Where-Object {$_.result -eq 'Failed'}).Count;checks=$script:Checks;first_error=$Failure;automatic_retries=0;unity_invocations=0;build_invocations=0;cases=@($Rows.ToArray());scratch=$TestRoot}
        Write-AndroidReleaseJson (Join-Path $ResultDirectory 'source-guard-tests-result.json') $Result
        if ($Failure) { [IO.File]::WriteAllText((Join-Path $ResultDirectory 'first-error.log'),$Failure) }
    }
    if ($Failure) { throw $Failure }
    Write-Host ('PASS source-guard cases: '+$Rows.Count+'; checks: '+$script:Checks+'. No Unity, preflight, AAB/APK or signing tools.')
}
if ($SourceGuardOnly) { Test-ReleaseSourceRecovery; return }
$Scratch = Join-Path ([IO.Path]::GetTempPath()) ('android-release-check-' + [guid]::NewGuid().ToString('N'))
$null = [IO.Directory]::CreateDirectory($Scratch)
$Key = Join-Path $Scratch 'fixture.keystore'
[IO.File]::WriteAllText($Key, 'synthetic fixture, not a private key')
$Config = @{
    MOFUMACHI_KEYSTORE_PATH = $Key
    MOFUMACHI_KEYSTORE_PASSWORD = 'fixture-store-secret'
    MOFUMACHI_KEY_ALIAS = 'fixture-alias'
    MOFUMACHI_KEY_ALIAS_PASSWORD = 'fixture-key-secret'
}
Check (Get-AndroidReleaseSigningPreflight $Config).passed 'Complete local signing inputs rejected.'
foreach ($Name in @($Config.Keys)) {
    $Bad = $Config.Clone(); $Bad[$Name] = ''
    $Report = Get-AndroidReleaseSigningPreflight $Bad
    Check (-not $Report.passed) ('Missing signing input accepted: ' + $Name)
    Check (($Report | ConvertTo-Json -Depth 5) -notmatch 'fixture-(store-secret|key-secret)') 'Signing preflight leaked a value.'
}
$Bad = $Config.Clone(); $Bad.MOFUMACHI_KEYSTORE_PATH = $Scratch
Check (-not (Get-AndroidReleaseSigningPreflight $Bad).passed) 'Directory accepted as keystore.'
$Bad.MOFUMACHI_KEYSTORE_PATH = Join-Path $Scratch 'absent.keystore'
Check (-not (Get-AndroidReleaseSigningPreflight $Bad).passed) 'Missing keystore accepted.'
$Protected = Protect-AndroidReleaseText ('error: ' + $Config.MOFUMACHI_KEYSTORE_PASSWORD + ' ' + $Config.MOFUMACHI_KEY_ALIAS_PASSWORD) @($Config.Values)
Check ($Protected -notmatch 'fixture-(store-secret|key-secret)') 'Subprocess text was not redacted.'
$ErrorText = "normal startup`nAssets/Example.cs(9,1): error CS1002: ; expected`nBuildFailedException: wrapper"
Check ((Get-AndroidReleaseFirstError $ErrorText) -match 'CS1002') 'Compiler error not preferred to wrapper.'
$ArgsAab = @(Get-AndroidReleaseArguments 'project' 'Aab' 'out.aab' 'info.json' ('a' * 40))
$ArgsApk = @(Get-AndroidReleaseArguments 'project' 'Apk' 'out.apk' 'info.json' ('a' * 40))
Check ($ArgsAab -contains 'Mofumachi.Editor.AndroidReleaseBuild.BuildReleaseAab') 'AAB method not separate.'
Check ($ArgsApk -contains 'Mofumachi.Editor.AndroidReleaseBuild.BuildReleaseApk') 'APK method not separate.'
Check ($ArgsAab -contains '--burst-disable-compilation') 'Burst switch missing.'
Check (($ArgsAab -notcontains '-runTests') -and ($ArgsApk -notcontains '-runTests')) 'Build route invokes game tests.'
Check (($ArgsAab -join ' ') -notmatch 'PASSWORD|fixture-store-secret|fixture-key-secret') 'Secret appears in process arguments.'
$Receipt = Join-Path $Scratch 'once.attempted'
New-AndroidReleaseReceipt $Receipt 'fixture-run'
$Rejected = $false
try { New-AndroidReleaseReceipt $Receipt 'replacement' } catch { $Rejected = $true }
Check $Rejected 'Second attempt was accepted.'
Check ([IO.File]::ReadAllText($Receipt) -eq 'fixture-run') 'Existing receipt overwritten.'
$GitCommand = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
if ($null -eq $GitCommand -or [string]::IsNullOrWhiteSpace($GitCommand.Source)) { throw 'Git Application was not found or has an empty executable path.' }
$Git = $GitCommand.Source
$Repo = Join-Path $Scratch 'repo'
$null = [IO.Directory]::CreateDirectory((Join-Path $Repo 'Assets'))
& $Git -C $Repo init --quiet
& $Git -C $Repo config core.autocrlf true
& $Git -C $Repo config user.name ReleaseFixture
& $Git -C $Repo config user.email fixture@example.invalid
$File = Join-Path $Repo 'Assets/fixture.txt'
[IO.File]::WriteAllText($File, "original`n")
& $Git -C $Repo add -- Assets
& $Git -C $Repo commit --quiet -m fixture
if ($LASTEXITCODE -ne 0) { throw 'Fixture Git setup failed.' }
$Revision = (& $Git -C $Repo rev-parse HEAD).Trim()
$GuardDir = Join-Path $Scratch 'guards'
Check (Get-AndroidReleaseSourceCheck $Git $Repo $Revision $GuardDir).unchanged 'Clean source rejected.'
[IO.File]::WriteAllText($File, "original`r`n")
Check (Get-AndroidReleaseSourceCheck $Git $Repo $Revision $GuardDir).unchanged 'Equivalent CRLF rejected.'
[IO.File]::WriteAllText((Join-Path $Repo '.vsconfig'), '{}')
Check (Get-AndroidReleaseSourceCheck $Git $Repo $Revision $GuardDir).unchanged 'Root .vsconfig rejected.'
[IO.File]::WriteAllText($File, "changed`n")
Check (-not (Get-AndroidReleaseSourceCheck $Git $Repo $Revision $GuardDir).unchanged) 'Tracked real content change hidden.'
& $Git -C $Repo add -- Assets
Check (-not (Get-AndroidReleaseSourceCheck $Git $Repo $Revision $GuardDir).unchanged) 'Staged content change hidden.'
[IO.File]::WriteAllText($File, "original`n")
& $Git -C $Repo add -- Assets
[IO.File]::WriteAllText((Join-Path $Repo 'other.txt'), 'not allowlisted')
Check (-not (Get-AndroidReleaseSourceCheck $Git $Repo $Revision $GuardDir).unchanged) 'Other untracked path silently ignored.'
$CSharp = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../Assets/Mofumachi/Editor/AndroidReleaseBuild.cs'))
foreach ($Needle in @('BuildOptions.None','development = false','allowDebugging = false','connectProfiler = false','buildWithDeepProfilingSupport = false','useCustomKeystore = true')) {
    Check ($CSharp.Contains($Needle)) ('Release flag missing: ' + $Needle)
}
Check (-not $CSharp.Contains('ConfigureDeviceSettings(')) 'Release builder calls Development configuration.'
Check (-not $CSharp.Contains('BuildOptions.Development')) 'Development BuildOptions in release builder.'
Write-Host ("PASS limited release-entry checks: $script:Checks. No Unity, game tests, APK/AAB or signing key used. Scratch: $Scratch")
