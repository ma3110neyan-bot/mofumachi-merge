#requires -Version 5.1
[CmdletBinding()]
param([switch]$Build, [ValidateSet('Both','Aab','Apk')][string]$Format = 'Both', [string]$UnityEditor = '')
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ReleaseProductCommit = '46f65b24d58531d5934035fbe4cdb4d5b00104a6'
$ReleaseRoot = 'E:\Mofumachi-Validation'
$ReleaseSigningNames = @('MOFUMACHI_KEYSTORE_PATH','MOFUMACHI_KEYSTORE_PASSWORD','MOFUMACHI_KEY_ALIAS','MOFUMACHI_KEY_ALIAS_PASSWORD')
$ReleaseProcessEnvironment = @{}

function Get-AndroidReleaseSigningPreflight([hashtable]$Config) {
    $Problems = @()
    foreach ($Name in $ReleaseSigningNames) {
        if (-not $Config.ContainsKey($Name) -or [string]::IsNullOrWhiteSpace([string]$Config[$Name])) { $Problems += ('Missing ' + $Name) }
    }
    if ($Problems.Count -eq 0) {
        try {
            $File = Get-Item -LiteralPath $Config.MOFUMACHI_KEYSTORE_PATH -Force -ErrorAction Stop
            if ($File.PSIsContainer -or $File.Length -eq 0 -or ($File.Attributes -band [IO.FileAttributes]::ReparsePoint)) { $Problems += 'Keystore must be a nonempty regular local file.' }
        } catch { $Problems += 'Keystore is not an accessible regular local file.' }
    }
    return [pscustomobject]@{passed=($Problems.Count -eq 0);problems=$Problems;secret_values_included=$false}
}
function Protect-AndroidReleaseText([string]$Text, [object[]]$Secrets) {
    foreach ($Value in @($Secrets | Where-Object {-not [string]::IsNullOrEmpty([string]$_)} | Sort-Object {([string]$_).Length} -Descending)) {
        $Text = $Text.Replace([string]$Value, '[REDACTED]')
        $Escaped = ([string]$Value | ConvertTo-Json -Compress).Trim('"')
        if ($Escaped) { $Text = $Text.Replace($Escaped, '[REDACTED]') }
    }
    return $Text
}
function Invoke-AndroidReleaseProcess([string]$Executable,[string[]]$Arguments,[hashtable]$EnvironmentValues=@{},[object[]]$Secrets=@()) {
    $Start = New-Object Diagnostics.ProcessStartInfo
    $Start.FileName=$Executable;$Start.UseShellExecute=$false;$Start.CreateNoWindow=$true
    $Start.RedirectStandardOutput=$true;$Start.RedirectStandardError=$true
    $Start.StandardOutputEncoding=New-Object Text.UTF8Encoding($false);$Start.StandardErrorEncoding=New-Object Text.UTF8Encoding($false)
    if ($null -ne $Start.PSObject.Properties['ArgumentList']) { foreach ($Arg in $Arguments) { $Start.ArgumentList.Add($Arg) } }
    else {
        $Start.Arguments=(@($Arguments | ForEach-Object {'"'+[regex]::Replace([regex]::Replace($_,'(\\*)"','$1$1\"'),'(\\+)$','$1$1')+'"'}) -join ' ')
    }
    foreach ($Name in $ReleaseSigningNames) { $Start.EnvironmentVariables.Remove($Name) }
    foreach ($Name in $ReleaseProcessEnvironment.Keys) { $Start.EnvironmentVariables[$Name]=[string]$ReleaseProcessEnvironment[$Name] }
    foreach ($Name in $EnvironmentValues.Keys) { $Start.EnvironmentVariables[$Name]=[string]$EnvironmentValues[$Name] }
    $Process=New-Object Diagnostics.Process;$Process.StartInfo=$Start
    try {
        if (-not $Process.Start()) { throw 'Could not start the requested tool.' }
        $Stdout=$Process.StandardOutput.ReadToEndAsync();$Stderr=$Process.StandardError.ReadToEndAsync()
        $Process.WaitForExit()
        return [pscustomobject]@{exit_code=$Process.ExitCode;stdout=(Protect-AndroidReleaseText $Stdout.GetAwaiter().GetResult() $Secrets);stderr=(Protect-AndroidReleaseText $Stderr.GetAwaiter().GetResult() $Secrets)}
    } finally { $Process.Dispose() }
}
function Invoke-AndroidReleaseGit([string]$Git,[string]$Project,[string[]]$Arguments) {
    return Invoke-AndroidReleaseProcess $Git (@('--no-pager','--no-optional-locks','-c','core.longpaths=true','-c','core.fsmonitor=false','-C',$Project)+$Arguments)
}
function Find-AndroidReleaseGit {
    foreach ($Name in @('git.exe','git')) {
        $Command=Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($Command) { return $Command.Source }
    }
    if ($env:LOCALAPPDATA) {
        $Command=Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA 'GitHubDesktop/app-*/resources/app/git/cmd/git.exe') -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($Command) { return $Command.FullName }
    }
    throw 'Git was not found; no Unity start.'
}
function Get-AndroidReleaseSourceCheck([string]$Git,[string]$Project,[string]$Baseline,[string]$Directory) {
    $null=[IO.Directory]::CreateDirectory($Directory)
    $Head=Invoke-AndroidReleaseGit $Git $Project @('rev-parse','HEAD')
    $Working=Invoke-AndroidReleaseGit $Git $Project @('diff','--quiet','--no-ext-diff','--no-textconv','--')
    $Index=Invoke-AndroidReleaseGit $Git $Project @('diff','--cached','--quiet','--no-ext-diff','--no-textconv',$Baseline,'--')
    $Untracked=Invoke-AndroidReleaseGit $Git $Project @('ls-files','--others','--exclude-standard','-z')
    $Problems=@();$Allowed=@();$Other=@()
    if ($Head.exit_code -ne 0 -or $Head.stdout.Trim() -cne $Baseline) { $Problems+='Source HEAD differs from the committed tooling baseline.' }
    if ($Working.exit_code -ne 0) { $Problems+='Tracked working content differs, or its comparison failed.' }
    if ($Index.exit_code -ne 0) { $Problems+='Tracked index content differs, or its comparison failed.' }
    if ($Untracked.exit_code -ne 0) { $Problems+='Untracked enumeration failed.' }
    foreach ($Name in @($Untracked.stdout.Split([char]0) | Where-Object {$_.Length -gt 0})) {
        if ($Name -ceq '.vsconfig') {
            $File=Get-Item -LiteralPath (Join-Path $Project $Name) -Force
            if ($File.PSIsContainer -or ($File.Attributes -band [IO.FileAttributes]::ReparsePoint)) { $Problems+='Root .vsconfig is not a regular file.' }
            else { $Allowed+=@{path=$Name;sha256=(Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash.ToLowerInvariant()} }
        } else { $Other+=$Name }
    }
    if ($Other.Count) { $Problems+='Unexpected untracked paths: '+($Other -join ', ') }
    $Report=[ordered]@{baseline=$Baseline;head=$Head.stdout.Trim();unchanged=($Problems.Count -eq 0);problems=$Problems;
        working_diff_exit=$Working.exit_code;index_diff_exit=$Index.exit_code;untracked_exit=$Untracked.exit_code;
        allowlist=@('.vsconfig');allowed_untracked=$Allowed;disallowed_untracked=$Other;
        tracked_scope='All tracked paths; content comparison, no whitespace ignore or path exclusions'}
    foreach ($Row in @(@('working',$Working),@('index',$Index),@('untracked',$Untracked))) {
        [IO.File]::WriteAllText((Join-Path $Directory ($Row[0]+'.stderr.log')), $Row[1].stderr)
    }
    Write-AndroidReleaseJson (Join-Path $Directory 'source-check.json') $Report
    return [pscustomobject]$Report
}
function Get-AndroidReleaseMutationPaths {
    return @('Assets/Settings/UniversalRP.asset','Assets/Settings/UniversalRenderPipelineGlobalSettings.asset','ProjectSettings/ProjectSettings.asset')
}
function Get-AndroidReleaseBytesHash([byte[]]$Bytes) {
    $Hash=[Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($Hash.ComputeHash($Bytes))).Replace('-','').ToLowerInvariant() }
    finally { $Hash.Dispose() }
}
function Assert-AndroidReleaseNoLink([string]$Path) {
    $Ancestor=[IO.Path]::GetFullPath($Path)
    while ($Ancestor) {
        if (Test-Path -LiteralPath $Ancestor) {
            if ((Get-Item -LiteralPath $Ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Linked recovery paths are not allowed.' }
        }
        $Ancestor=[IO.Path]::GetDirectoryName($Ancestor)
    }
}
function Test-AndroidReleaseUnder([string]$Path,[string]$Root) {
    $Boundary=[IO.Path]::GetFullPath($Root).TrimEnd([char[]]@('/','\'))+[IO.Path]::DirectorySeparatorChar
    return [IO.Path]::GetFullPath($Path).StartsWith($Boundary,[StringComparison]::OrdinalIgnoreCase)
}
function Assert-AndroidReleaseRecoveryScope([string]$Git,[string]$Project,[string]$Baseline,[string]$Directory,[string]$Formal) {
    if ($Baseline -cnotmatch '^[0-9a-f]{40}$' -or
        -not (Test-AndroidReleaseUnder $Project (Join-Path $ReleaseRoot 'projects')) -or
        -not (Test-AndroidReleaseUnder $Directory (Join-Path $ReleaseRoot 'Diagnostics/release-candidate')) -or
        (Test-AndroidReleaseUnder $Project $Formal) -or [IO.Path]::GetFullPath($Project) -ieq [IO.Path]::GetFullPath($Formal)) { throw 'Recovery is restricted to an isolated release project and separate Diagnostics.' }
    $Leaf=[IO.Path]::GetFileName([IO.Path]::GetFullPath($Project).TrimEnd([char[]]@('/','\')))
    if ($Leaf -cnotmatch '^rc-[0-9]{8}-[0-9]{6}-[0-9a-f]{8}$' -or
        -not (Test-AndroidReleaseUnder $Directory (Join-Path $ReleaseRoot ('Diagnostics/release-candidate/'+$Leaf)))) { throw 'Recovery run identity does not match the isolated project.' }
    foreach ($Path in @($Project,$Directory,$Formal)) { Assert-AndroidReleaseNoLink $Path }
    $Registration=Invoke-AndroidReleaseGit $Git $Formal @('worktree','list','--porcelain')
    if ($Registration.exit_code -ne 0) { throw 'Cannot verify recovery worktree registration.' }
    $Registered=@($Registration.stdout -split '\r?\n\r?\n' | Where-Object {
        $Line=[regex]::Match($_,'(?m)^worktree (.+)\r?$')
        $Line.Success -and $Line.Groups[1].Value.Trim().Replace('\','/') -ieq [IO.Path]::GetFullPath($Project).Replace('\','/')
    })
    if ($Registered.Count -ne 1 -or $Registered[0] -notmatch ('(?m)^HEAD '+$Baseline+'\r?$') -or
        $Registered[0] -notmatch '(?m)^detached\r?$' -or $Registered[0] -match '(?m)^(locked|prunable)') { throw 'Recovery target is not the registered detached pinned worktree.' }
    $Head=Invoke-AndroidReleaseGit $Git $Formal @('rev-parse','HEAD')
    $Status=Invoke-AndroidReleaseGit $Git $Formal @('status','--porcelain=v1','--untracked-files=all')
    $Index=Invoke-AndroidReleaseGit $Git $Formal @('diff','--cached','--quiet','--no-ext-diff','--no-textconv',$Baseline,'--')
    if ($Head.exit_code -ne 0 -or $Head.stdout.Trim() -cne $Baseline -or $Status.exit_code -ne 0 -or $Status.stdout -or $Index.exit_code -ne 0) { throw 'Formal repository changed; no recovery mutation is allowed.' }
}
function Read-AndroidReleaseRegularBytes([string]$Path) {
    Assert-AndroidReleaseNoLink $Path
    $File=Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($File.PSIsContainer -or $File.Length -gt 67108864) { throw 'Recovery input must be a bounded regular file.' }
    return ,([IO.File]::ReadAllBytes($File.FullName))
}
function Write-AndroidReleaseNewBytes([string]$Path,[byte[]]$Bytes) {
    $Stream=[IO.File]::Open($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try { $Stream.Write($Bytes,0,$Bytes.Length);$Stream.Flush() } finally { $Stream.Dispose() }
}
function New-AndroidReleaseBuildSnapshot([string]$Git,[string]$Project,[string]$Baseline,[string]$Directory,[string]$Formal) {
    Assert-AndroidReleaseRecoveryScope $Git $Project $Baseline $Directory $Formal
    $Before=Get-AndroidReleaseSourceCheck $Git $Project $Baseline (Join-Path $Directory 'before')
    if (-not $Before.unchanged) { throw 'Snapshot requires the ordinary clean source guard.' }
    foreach ($Name in @('.utmp','build/outputs/logs/unity---stop-build.log')) {
        if (Test-Path -LiteralPath (Join-Path $Project $Name)) { throw 'Generated cleanup roots must not exist before this build.' }
    }
    $Paths=@(Get-AndroidReleaseMutationPaths)
    $Tracked=Invoke-AndroidReleaseGit $Git $Project (@('ls-files','--error-unmatch','--')+$Paths)
    if ($Tracked.exit_code -ne 0) { throw 'Snapshot inputs are not all tracked.' }
    $Files=@()
    for ($i=0;$i -lt $Paths.Count;$i++) {
        $Target=Join-Path $Project $Paths[$i]
        $Bytes=Read-AndroidReleaseRegularBytes $Target
        $Snapshot=Join-Path $Directory ('snapshot-'+$i+'.bin')
        Write-AndroidReleaseNewBytes $Snapshot $Bytes
        $Sha=Get-AndroidReleaseBytesHash $Bytes
        if ((Get-AndroidReleaseBytesHash (Read-AndroidReleaseRegularBytes $Snapshot)) -cne $Sha -or
            (Get-AndroidReleaseBytesHash (Read-AndroidReleaseRegularBytes $Target)) -cne $Sha) { throw 'Pre-build byte snapshot verification failed.' }
        $Files+=@{relative=$Paths[$i];snapshot=$Snapshot;sha256=$Sha;length=$Bytes.Length}
    }
    $Plan=[pscustomobject]@{project=$Project;baseline=$Baseline;directory=$Directory;formal=$Formal;files=$Files}
    Write-AndroidReleaseJson (Join-Path $Directory 'snapshot.json') $Plan
    return $Plan
}
function Get-AndroidReleaseComparable([string]$Relative,[byte[]]$Bytes,[string]$Reference) {
    $Utf8=New-Object Text.UTF8Encoding($false,$true)
    $Text=$Utf8.GetString($Bytes).Replace("`r`n","`n")
    if ($Text.Contains("`r")) { throw 'Unsupported serialized line endings.' }
    if ($Relative -ceq 'Assets/Settings/UniversalRP.asset') {
        $Keys=@('m_PrefilteringModeMainLightShadows','m_PrefilteringModeAdditionalLight','m_PrefilteringModeAdditionalLightShadows',
            'm_PrefilterXRKeywords','m_PrefilteringModeForwardPlus','m_PrefilteringModeDeferredRendering','m_PrefilteringModeScreenSpaceOcclusion','m_PrefilteringModeScreenSpaceReflection',
            'm_PrefilterWriteSmoothness','m_PrefilterDebugKeywords','m_PrefilterWriteRenderingLayers','m_PrefilterHDROutput','m_PrefilterAlphaOutput','m_PrefilterSSAODepthNormals',
            'm_PrefilterSSAOSourceDepthLow','m_PrefilterSSAOSourceDepthMedium','m_PrefilterSSAOSourceDepthHigh','m_PrefilterSSAOInterleaved','m_PrefilterSSAOBlueNoise',
            'm_PrefilterSSAOSampleCountLow','m_PrefilterSSAOSampleCountMedium','m_PrefilterSSAOSampleCountHigh','m_PrefilterDBufferMRT1','m_PrefilterDBufferMRT2','m_PrefilterDBufferMRT3',
            'm_PrefilterSoftShadowsQualityLow','m_PrefilterSoftShadowsQualityMedium','m_PrefilterSoftShadowsQualityHigh','m_PrefilterSoftShadows','m_PrefilterScreenCoord',
            'm_PrefilterScreenSpaceIrradiance','m_PrefilterNativeRenderPass','m_PrefilterUseLegacyLightmaps','m_PrefilterBicubicLightmapSampling','m_PrefilterReflectionProbeRotation',
            'm_PrefilterReflectionProbeBlending','m_PrefilterReflectionProbeBoxProjection','m_PrefilterReflectionProbeAtlas','m_PrefilterPointSamplingUpsampling','m_Light2DPrefilteringMode')
        foreach ($Key in $Keys) {
            $Pattern='(?m)^  '+[regex]::Escape($Key)+': ([0-4])$'
            if ([regex]::Matches($Text,$Pattern).Count -ne 1) { throw 'Unknown or duplicate shader prefilter serialization.' }
            $Text=[regex]::Replace($Text,$Pattern,('  '+$Key+': <prefilter>'))
        }
        $Pattern='(?m)^  m_Light2DKeptVariantCombos:(?: \[\]\n| [0-9a-fA-F]+\n|\n(?:  - [0-9]+\n)+)'
        $Combos=[regex]::Matches($Text,$Pattern)
        if ($Combos.Count -ne 1) { throw 'Unknown Light2D variant metadata.' }
        $Sequence=@([regex]::Matches($Combos[0].Value,'(?m)^  - ([0-9]+)$'))
        if ($Sequence.Count -gt 256) { throw 'Unexpected Light2D variant count.' }
        foreach ($Number in $Sequence) {
            $Parsed=[uint32]0
            if (-not [uint32]::TryParse($Number.Groups[1].Value,[ref]$Parsed)) { throw 'Invalid Light2D variant value.' }
        }
        $Hex=[regex]::Match($Combos[0].Value,'^  m_Light2DKeptVariantCombos: ([0-9a-fA-F]+)\n$')
        if ($Hex.Success -and ($Hex.Groups[1].Value.Length % 2 -ne 0 -or $Hex.Groups[1].Value.Length -gt 4096)) { throw 'Invalid Light2D serialized bytes.' }
        $Text=[regex]::Replace($Text,$Pattern,"  m_Light2DKeptVariantCombos: <variants>`n")
    } elseif ($Relative -ceq 'Assets/Settings/UniversalRenderPipelineGlobalSettings.asset') {
        $Pattern='(?m)^    m_RuntimeSettings:\n      m_List:(?: \[\]\n|\n(?:      - rid: [0-9]+\n)+)'
        $Block=[regex]::Matches($Text,$Pattern)
        if ($Block.Count -ne 1) { throw 'Unknown runtime settings list serialization.' }
        $Defined=@([regex]::Matches($Reference,'(?m)^    - rid: ([0-9]+)$') | ForEach-Object {$_.Groups[1].Value})
        $Seen=New-Object 'Collections.Generic.HashSet[string]'
        foreach ($Rid in [regex]::Matches($Block[0].Value,'rid: ([0-9]+)')) {
            if ($Defined -cnotcontains $Rid.Groups[1].Value -or -not $Seen.Add($Rid.Groups[1].Value)) { throw 'Runtime list contains an unresolved or duplicate RID.' }
        }
        $Text=[regex]::Replace($Text,$Pattern,"    m_RuntimeSettings:`n      m_List: <runtime>`n")
    } elseif ($Relative -ceq 'ProjectSettings/ProjectSettings.asset') {
        $Pattern='(?m)^  AndroidKeystoreName:([^\n]*)$'
        $Key=[regex]::Matches($Text,$Pattern)
        if ($Key.Count -ne 1) { throw 'Unknown keystore name serialization.' }
        $Value=$Key[0].Groups[1].Value.Trim()
        if ($Value.Length -ge 2 -and (($Value[0] -eq [char]39 -and $Value[$Value.Length-1] -eq [char]39) -or
            ($Value[0] -eq [char]34 -and $Value[$Value.Length-1] -eq [char]34))) { $Value=$Value.Substring(1,$Value.Length-2) }
        if ($Value -cne '' -and $Value -cne '{inproject}: ') { throw 'A nonempty or unknown keystore binding is not a serialization-only mutation.' }
        $Text=[regex]::Replace($Text,$Pattern,'  AndroidKeystoreName: <empty>')
        $Graphics=[regex]::Matches($Text,'(?m)^  - m_BuildTarget: Android\n    m_APIs: ([0-9a-fA-F]{8})\n    m_Automatic: ([01])\n')
        if ($Graphics.Count -ne 1 -or $Graphics[0].Groups[1].Value.ToLowerInvariant() -cne '0b000000') { throw 'Android graphics value is not the known OpenGLES3 representation.' }
        $Text=$Text.Replace($Graphics[0].Value,$Graphics[0].Value.Replace($Graphics[0].Groups[1].Value,'0b000000'))
    } else { throw 'Tracked mutation path is not allowed.' }
    return $Text
}
function Test-AndroidReleaseGeneratedFile([string]$Relative,[byte[]]$Bytes) {
    if ($Relative -ceq 'build/outputs/logs/unity---stop-build.log') {
        $Text=(New-Object Text.UTF8Encoding($false,$true)).GetString($Bytes)
        return $Text -match 'Stopping Daemon\(s\)|[0-9]+ Daemons? stopped|No Gradle daemons are running'
    }
    $Prefix='^\.utmp/RelWithDebInfo/[a-zA-Z0-9_-]+/'
    $Arm=$Prefix+'arm64-v8a/'
    $Known=$Relative -cmatch '^\.utmp/tools/release/arm64-v8a/compile_commands\.json$' -or
        $Relative -cmatch ($Prefix+'hash_key\.txt$') -or
        $Relative -cmatch ($Arm+'\.cmake/api/v1/query/client-agp/(cache-v2|cmakeFiles-v1|codemodel-v2)$') -or
        $Relative -cmatch ($Arm+'\.cmake/api/v1/reply/(cache-v2|cmakeFiles-v1|codemodel-v2|directory-\.-RelWithDebInfo|directory-FramePacing-RelWithDebInfo|directory-GameActivity-RelWithDebInfo|target-game-RelWithDebInfo|target-swappywrapper-RelWithDebInfo)-[a-f0-9]+\.json$') -or
        $Relative -cmatch ($Arm+'\.cmake/api/v1/reply/index-[0-9TZ:-]+\.json$') -or
        $Relative -cmatch ($Arm+'(\.ninja_deps|\.ninja_log|CMakeCache\.txt|android_gradle_build(?:_mini)?\.json|build\.ninja|build_file_index\.txt|cmake_install\.cmake|compile_commands\.json(?:\.bin)?|configure_fingerprint\.bin|metadata_generation_command\.txt|prefab_config\.json|symbol_folder_index\.txt)$') -or
        $Relative -cmatch ($Arm+'CMakeFiles/(cmake\.check_cache|rules\.ninja|TargetDirectories\.txt|CMakeOutput\.log|3\.22\.1-g37088a8-dirty/(CMakeCCompiler\.cmake|CMakeCXXCompiler\.cmake|CMakeDetermineCompilerABI_C\.bin|CMakeDetermineCompilerABI_CXX\.bin|CMakeSystem\.cmake|CompilerIdC/CMakeCCompilerId\.(c|o)|CompilerIdCXX/CMakeCXXCompilerId\.(cpp|o)))$') -or
        $Relative -cmatch ($Arm+'FramePacing/(cmake_install\.cmake|CMakeFiles/swappywrapper\.dir/UnitySwappyWrapper\.cpp\.o)$') -or
        $Relative -cmatch ($Arm+'GameActivity/(cmake_install\.cmake|CMakeFiles/game\.dir/UGA(Application|Configuration|Debug|Entry|Input|InputKeyEvent|InputMotionEvent|SoftKeyboard)\.cpp\.o)$') -or
        $Relative -cmatch ($Prefix+'prefab/arm64-v8a/prefab/lib/aarch64-linux-android/cmake/(game-activity/game-activityConfig|games-frame-pacing/games-frame-pacingConfig)\.cmake$')
    if (-not $Known) { return $false }
    if ($Relative -match '/query/client-agp/') { return $Bytes.Length -eq 0 }
    if ($Bytes.Length -eq 0) { return $false }
    if ($Relative.EndsWith('.json',[StringComparison]::Ordinal)) {
        $Text=(New-Object Text.UTF8Encoding($false,$true)).GetString($Bytes)
        if ($Text.TrimStart() -notmatch '^[\[{]') { return $false }
        try { $null=$Text | ConvertFrom-Json -ErrorAction Stop } catch { return $false }
    }
    if ($Relative.EndsWith('.o',[StringComparison]::Ordinal) -or $Relative -match 'CMakeDetermineCompilerABI_.*\.bin$') {
        return $Bytes.Length -ge 4 -and [BitConverter]::ToString($Bytes,0,4) -ceq '7F-45-4C-46'
    }
    return $true
}
function Restore-AndroidReleaseSnapshotFile([string]$Payload,[string]$Target,[string]$Backup,[string]$SnapshotSha,[string]$TargetSha) {
    if ([string]::IsNullOrWhiteSpace($Payload) -or [string]::IsNullOrWhiteSpace($Target) -or [string]::IsNullOrWhiteSpace($Backup) -or
        $SnapshotSha -cnotmatch '^[0-9a-f]{64}$' -or $TargetSha -cnotmatch '^[0-9a-f]{64}$') { throw 'Restore paths and expected SHA256 must be nonempty.' }
    $Directory=[IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($Payload))
    $Diagnostics=[IO.Path]::GetFullPath((Join-Path $ReleaseRoot 'Diagnostics/release-candidate'))
    if (-not (Test-AndroidReleaseUnder $Payload $Diagnostics) -or -not (Test-AndroidReleaseUnder $Backup $Directory)) { throw 'Restore backup must stay inside this run Diagnostics.' }
    $Run=[IO.Path]::GetFullPath($Payload).Substring($Diagnostics.Length+1).Replace('\','/').Split('/')[0]
    if ($Run -cnotmatch '^rc-[0-9]{8}-[0-9]{6}-[0-9a-f]{8}$') { throw 'Restore Diagnostics run identity is invalid.' }
    $Targets=@(Get-AndroidReleaseMutationPaths | ForEach-Object {[IO.Path]::GetFullPath((Join-Path $ReleaseRoot ('projects/'+$Run+'/'+$_)))})
    if ($Targets -notcontains [IO.Path]::GetFullPath($Target)) { throw 'Restore target must be a known file of the matching isolated worktree.' }
    foreach ($Path in @($Payload,$Target,$Backup)) { Assert-AndroidReleaseNoLink $Path }
    $Parent=Split-Path -Parent $Backup
    if (-not (Test-Path -LiteralPath $Parent -PathType Container) -or (Test-Path -LiteralPath $Backup)) { throw 'Backup parent must exist and backup path must not exist.' }
    $PayloadBytes=Read-AndroidReleaseRegularBytes $Payload
    $TargetBytes=Read-AndroidReleaseRegularBytes $Target
    if ((Get-AndroidReleaseBytesHash $PayloadBytes) -cne $SnapshotSha -or (Get-AndroidReleaseBytesHash $TargetBytes) -cne $TargetSha) { throw 'Restore payload or pre-restore target SHA mismatch.' }
    $Record=Join-Path $Directory ('restore-backup-'+[guid]::NewGuid().ToString('N')+'.json')
    $Proof=[ordered]@{state='Prepared';payload=$Payload;target=$Target;backup=$Backup;record=$Record;
        snapshot_sha256=$SnapshotSha;target_sha256_before=$TargetSha;target_size_before=$TargetBytes.Length;
        payload_sha256=$SnapshotSha;target_sha256_after=$null;backup_sha256=$null;backup_size=$null;
        backup_exists_after=$false;payload_exists_after=$true;backup_cleanup_performed=$false;
        backup_cleanup_policy='Retain this run backup in Diagnostics as evidence; no explicit backup or payload cleanup';
        error=$null;error_stack=$null;error_position=$null;inspection_error=$null}
    Write-AndroidReleaseNewBytes $Record ([Text.Encoding]::UTF8.GetBytes(($Proof|ConvertTo-Json -Depth 6)))
    $Failure=$null
    try {
        if (Test-Path -LiteralPath $Backup) { throw 'Backup path appeared before replacement; no overwrite is allowed.' }
        [IO.File]::Replace($Payload,$Target,$Backup)
        $Proof.target_sha256_after=Get-AndroidReleaseBytesHash (Read-AndroidReleaseRegularBytes $Target)
        $BackupBytes=Read-AndroidReleaseRegularBytes $Backup
        $Proof.backup_sha256=Get-AndroidReleaseBytesHash $BackupBytes
        $Proof.backup_size=$BackupBytes.Length
        if ($Proof.target_sha256_after -cne $SnapshotSha -or $Proof.backup_sha256 -cne $TargetSha -or $Proof.backup_size -ne $TargetBytes.Length) { throw 'Restored target or backup SHA/size mismatch.' }
        $Proof.state='Verified'
    } catch {
        $Failure=$_;$Proof.state='Failed';$Proof.error=$_.Exception.Message
        $Proof.error_stack=$_.ScriptStackTrace;$Proof.error_position=$_.InvocationInfo.PositionMessage
    } finally {
        $Proof.backup_exists_after=Test-Path -LiteralPath $Backup -PathType Leaf
        $Proof.payload_exists_after=Test-Path -LiteralPath $Payload -PathType Leaf
        try {
            if ($Proof.backup_exists_after) {
                $BackupBytes=Read-AndroidReleaseRegularBytes $Backup
                $Proof.backup_sha256=Get-AndroidReleaseBytesHash $BackupBytes;$Proof.backup_size=$BackupBytes.Length
            }
            if (Test-Path -LiteralPath $Target -PathType Leaf) { $Proof.target_sha256_after=Get-AndroidReleaseBytesHash (Read-AndroidReleaseRegularBytes $Target) }
            if ($null -eq $Failure -and (-not $Proof.backup_exists_after -or $Proof.target_sha256_after -cne $SnapshotSha -or
                $Proof.backup_sha256 -cne $TargetSha -or $Proof.backup_size -ne $TargetBytes.Length)) { throw 'Restore evidence changed before verification completed.' }
        } catch { $Proof.inspection_error=$_.Exception.Message;if ($null -eq $Failure) {$Failure=$_;$Proof.state='Failed'} }
        try { Write-AndroidReleaseJson $Record $Proof } catch { if ($null -eq $Failure) { $Failure=$_ } }
    }
    if ($null -ne $Failure) { throw $Failure }
    return [pscustomobject]$Proof
}
function Remove-AndroidReleaseGeneratedItem([string]$Path,[bool]$Directory) {
    if ($Directory) { [IO.Directory]::Delete($Path,$false) } else { [IO.File]::Delete($Path) }
}
function Repair-AndroidReleaseBuildSource([string]$Git,$Plan,[object[]]$Secrets=@()) {
    $Report=[ordered]@{passed=$false;problem=$null;restored=@();cleaned=@();final_guard=@{unchanged=$false}}
    $CanWriteReport=$false
    try {
        Assert-AndroidReleaseRecoveryScope $Git $Plan.project $Plan.baseline $Plan.directory $Plan.formal
        $CanWriteReport=$true
        $Paths=@(Get-AndroidReleaseMutationPaths)
        if ($Plan.files.Count -ne $Paths.Count) { throw 'Snapshot inventory mismatch.' }
        $BytesBefore=@{};$BytesAfter=@{};$AfterHashes=@{}
        for ($i=0;$i -lt $Paths.Count;$i++) {
            $Row=$Plan.files[$i]
            if ($Row.relative -cne $Paths[$i] -or $Row.snapshot -cne (Join-Path $Plan.directory ('snapshot-'+$i+'.bin'))) { throw 'Snapshot path mismatch.' }
            $Bytes=Read-AndroidReleaseRegularBytes $Row.snapshot
            if ($Bytes.Length -ne $Row.length -or (Get-AndroidReleaseBytesHash $Bytes) -cne $Row.sha256) { throw 'Snapshot SHA mismatch.' }
            $BytesBefore[$Row.relative]=$Bytes
        }
        $Head=Invoke-AndroidReleaseGit $Git $Plan.project @('rev-parse','HEAD')
        $Index=Invoke-AndroidReleaseGit $Git $Plan.project @('diff','--cached','--quiet','--no-ext-diff','--no-textconv',$Plan.baseline,'--')
        $Names=Invoke-AndroidReleaseGit $Git $Plan.project @('diff','--name-only','-z','--no-ext-diff','--no-textconv',$Plan.baseline,'--')
        $Diff=Invoke-AndroidReleaseGit $Git $Plan.project @('diff','--no-ext-diff','--no-textconv','--no-color',$Plan.baseline,'--')
        $Other=Invoke-AndroidReleaseGit $Git $Plan.project @('ls-files','--others','--exclude-standard','-z')
        [IO.File]::WriteAllText((Join-Path $Plan.directory 'pre-restore.diff'),(Protect-AndroidReleaseText $Diff.stdout $Secrets))
        Write-AndroidReleaseJson (Join-Path $Plan.directory 'pre-restore-checks.json') @{
            head=$Head.stdout.Trim();head_exit=$Head.exit_code;index_exit=$Index.exit_code;names_exit=$Names.exit_code;
            diff_exit=$Diff.exit_code;untracked_exit=$Other.exit_code;
            changed_paths=@($Names.stdout.Split([char]0) | Where-Object {$_});
            untracked_paths=@($Other.stdout.Split([char]0) | Where-Object {$_});
            errors=(Protect-AndroidReleaseText ($Head.stderr+$Index.stderr+$Names.stderr+$Diff.stderr+$Other.stderr) $Secrets)
        }
        if ($Head.exit_code -ne 0 -or $Head.stdout.Trim() -cne $Plan.baseline -or $Index.exit_code -ne 0 -or $Names.exit_code -ne 0 -or $Diff.exit_code -ne 0 -or $Other.exit_code -ne 0) { throw 'Pre-restore HEAD/index/diff/untracked inspection failed.' }
        foreach ($Name in @($Names.stdout.Split([char]0) | Where-Object {$_})) {
            if ($Paths -cnotcontains $Name) { throw 'Tracked content outside the three known files changed.' }
        }
        $Utf8=New-Object Text.UTF8Encoding($false,$true)
        foreach ($Name in $Paths) {
            $BytesAfter[$Name]=Read-AndroidReleaseRegularBytes (Join-Path $Plan.project $Name)
            $AfterHashes[$Name]=Get-AndroidReleaseBytesHash $BytesAfter[$Name]
            $Reference=$Utf8.GetString($BytesBefore[$Name]).Replace("`r`n","`n")
            $Left=Get-AndroidReleaseComparable $Name $BytesBefore[$Name] $Reference
            $Right=Get-AndroidReleaseComparable $Name $BytesAfter[$Name] $Reference
            if ($Left -cne $Right) { throw ('Unknown tracked mutation: '+$Name) }
        }
        $TrackedGenerated=Invoke-AndroidReleaseGit $Git $Plan.project @('ls-files','-z','--','.utmp','build/outputs/logs/unity---stop-build.log')
        if ($TrackedGenerated.exit_code -ne 0 -or $TrackedGenerated.stdout) { throw 'Cleanup candidates must not be tracked.' }
        foreach ($Name in @($Other.stdout.Split([char]0) | Where-Object {$_})) {
            if ($Name -ceq '.vsconfig') { $null=Read-AndroidReleaseRegularBytes (Join-Path $Plan.project $Name) }
            elseif ($Name -cnotmatch '^\.utmp/' -and $Name -cne 'build/outputs/logs/unity---stop-build.log') { throw 'Unknown untracked path; no restore or cleanup is allowed.' }
        }
        $Items=@();$Directories=@();$Utmp=Join-Path $Plan.project '.utmp'
        if (Test-Path -LiteralPath $Utmp) {
            $Pending=New-Object 'Collections.Generic.Stack[string]';$Pending.Push($Utmp)
            while ($Pending.Count) {
                $Current=$Pending.Pop();Assert-AndroidReleaseNoLink $Current
                if (-not (Get-Item -LiteralPath $Current -Force).PSIsContainer) { throw 'Generated root must be a directory.' }
                $Directories+=$Current
                foreach ($Child in Get-ChildItem -LiteralPath $Current -Force) {
                    Assert-AndroidReleaseNoLink $Child.FullName
                    if ($Child.PSIsContainer) { $Pending.Push($Child.FullName) }
                    else { $Items+=@{path=$Child.FullName;relative=$Child.FullName.Substring($Plan.project.Length+1).Replace('\','/')} }
                }
            }
        }
        $StopLog=Join-Path $Plan.project 'build/outputs/logs/unity---stop-build.log'
        if (Test-Path -LiteralPath $StopLog) { $Items+=@{path=$StopLog;relative='build/outputs/logs/unity---stop-build.log'} }
        foreach ($Item in $Items) {
            $Bytes=Read-AndroidReleaseRegularBytes $Item.path
            if (-not (Test-AndroidReleaseGeneratedFile $Item.relative $Bytes)) { throw 'Unknown generated file path or content; no cleanup is allowed.' }
            $Item['sha256']=Get-AndroidReleaseBytesHash $Bytes;$Item['length']=$Bytes.Length
        }
        foreach ($Directory in $Directories) {
            if ($Directory -cne $Utmp -and @($Items | Where-Object {Test-AndroidReleaseUnder $_.path $Directory}).Count -eq 0) { throw 'Unknown empty generated directory.' }
        }
        Write-AndroidReleaseJson (Join-Path $Plan.directory 'cleanup-inventory.json') @{files=$Items;directories=$Directories}
        # All paths, contents, Git state and snapshots pass before the first write or delete.
        foreach ($Row in $Plan.files) {
            $Target=Join-Path $Plan.project $Row.relative
            if ((Get-AndroidReleaseBytesHash (Read-AndroidReleaseRegularBytes $Target)) -cne $AfterHashes[$Row.relative]) { throw 'Tracked bytes changed during validation.' }
            if ($AfterHashes[$Row.relative] -cne $Row.sha256) {
                $Payload=Join-Path $Plan.directory ('restore-'+[guid]::NewGuid().ToString('N')+'.tmp')
                Write-AndroidReleaseNewBytes $Payload $BytesBefore[$Row.relative]
                $BackupDirectory=Join-Path $Plan.directory ('restore-backups/'+[guid]::NewGuid().ToString('N'))
                Assert-AndroidReleaseNoLink $BackupDirectory
                if (Test-Path -LiteralPath $BackupDirectory) { throw 'Unique backup directory already exists.' }
                $null=[IO.Directory]::CreateDirectory($BackupDirectory)
                $Backup=Join-Path $BackupDirectory ([IO.Path]::GetFileName($Target)+'.bak')
                $Proof=Restore-AndroidReleaseSnapshotFile $Payload $Target $Backup $Row.sha256 $AfterHashes[$Row.relative]
                if ((Get-AndroidReleaseBytesHash (Read-AndroidReleaseRegularBytes $Target)) -cne $Row.sha256) { throw 'Restore SHA mismatch.' }
                if ($null -eq $Proof -or $Proof.state -cne 'Verified' -or (Get-AndroidReleaseBytesHash (Read-AndroidReleaseRegularBytes $Backup)) -cne $AfterHashes[$Row.relative]) { throw 'Restore backup verification failed.' }
                $Report.restored+=@{path=$Row.relative;sha256=$Row.sha256;before_sha256=$AfterHashes[$Row.relative];backup=$Backup;backup_record=$Proof.record}
            }
            if ((Get-AndroidReleaseBytesHash (Read-AndroidReleaseRegularBytes $Target)) -cne $Row.sha256) { throw 'Restore SHA mismatch.' }
        }
        foreach ($Item in $Items) {
            if ((Get-AndroidReleaseBytesHash (Read-AndroidReleaseRegularBytes $Item.path)) -cne $Item.sha256) { throw 'Cleanup candidate changed during validation.' }
            Remove-AndroidReleaseGeneratedItem $Item.path $false
            if (Test-Path -LiteralPath $Item.path) { throw 'Generated file cleanup failed.' }
            $Report.cleaned+=$Item.relative
        }
        foreach ($Directory in @($Directories | Sort-Object Length -Descending)) {
            Assert-AndroidReleaseNoLink $Directory
            Remove-AndroidReleaseGeneratedItem $Directory $true
        }
        Assert-AndroidReleaseRecoveryScope $Git $Plan.project $Plan.baseline $Plan.directory $Plan.formal
        $Report.final_guard=Get-AndroidReleaseSourceCheck $Git $Plan.project $Plan.baseline (Join-Path $Plan.directory 'final')
        if (-not $Report.final_guard.unchanged) { throw 'Final ordinary source guard did not pass.' }
        $Report.passed=$true
    } catch { $Report.problem=Protect-AndroidReleaseText $_.Exception.Message $Secrets }
    if ($CanWriteReport) {
        Assert-AndroidReleaseNoLink $Plan.directory
        Write-AndroidReleaseJson (Join-Path $Plan.directory 'recovery-result.json') $Report
    }
    return [pscustomobject]$Report
}
function Write-AndroidReleaseJson([string]$Path,$Value) {
    [IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 12),(New-Object Text.UTF8Encoding($false)))
}
function New-AndroidReleaseReceipt([string]$Path,[string]$Run) {
    $null=[IO.Directory]::CreateDirectory((Split-Path -Parent $Path))
    $Bytes=[Text.Encoding]::UTF8.GetBytes($Run)
    $Stream=[IO.File]::Open($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try { $Stream.Write($Bytes,0,$Bytes.Length);$Stream.Flush() } finally { $Stream.Dispose() }
}
function Get-AndroidReleaseArguments([string]$Project,[string]$Kind,[string]$Output,[string]$Info,[string]$Revision) {
    return @('-batchmode','-nographics','-quit','-projectPath',$Project,'-buildTarget','Android','--burst-disable-compilation',
        '-executeMethod',('Mofumachi.Editor.AndroidReleaseBuild.BuildRelease'+$Kind),'-mofumachiReleaseOutput',$Output,
        '-mofumachiReleaseInfo',$Info,'-mofumachiReleaseRevision',$Revision,'-logFile','-')
}
function Get-AndroidReleaseFirstError([string]$Text) {
    $Lines=@($Text -split '\r?\n')
    for ($i=0;$i -lt $Lines.Count;$i++) {
        if ($Lines[$i] -match '(?i)error CS\d+|error [A-Z]+\d+:|Execution failed for task|FAILURE: Build failed|IL2CPP.*(?:error|exception)|Gradle.*(?:error|failed)|Keystore was tampered|password was incorrect|Cannot recover key|BuildFailedException:') {
            return (($Lines[$i..([Math]::Min($i+18,$Lines.Count-1))]) -join [Environment]::NewLine)
        }
    }
    return $null
}
function Get-AndroidReleaseArtifactCertificate([string]$Kind,[string]$Artifact,[string]$EditorRoot,[string]$Directory) {
    $Jdk=Join-Path $EditorRoot 'Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin'
    if ($Kind -eq 'Aab') {
        $Verify=Invoke-AndroidReleaseProcess (Join-Path $Jdk 'jarsigner.exe') @('-J-Duser.language=en','-verify',$Artifact)
        [IO.File]::WriteAllText((Join-Path $Directory 'signature-verification.log'),$Verify.stdout+"`n"+$Verify.stderr)
        if ($Verify.exit_code -ne 0 -or $Verify.stdout -notmatch 'jar verified\.') { throw 'AAB JAR signature verification failed.' }
        $Certificate=Invoke-AndroidReleaseProcess (Join-Path $Jdk 'keytool.exe') @('-J-Duser.language=en','-printcert','-jarfile',$Artifact)
        $Text=$Certificate.stdout+"`n"+$Certificate.stderr
        [IO.File]::WriteAllText((Join-Path $Directory 'artifact-certificate.log'),$Text)
        $Matches=[regex]::Matches($Text,'(?im)^\s*SHA256:\s*([0-9a-f:]{95})\s*$')
        if ($Certificate.exit_code -ne 0 -or $Matches.Count -ne 1) { throw 'Cannot identify one AAB signer certificate.' }
        return $Matches[0].Groups[1].Value.Replace(':','').ToLowerInvariant()
    }
    $Tools=Join-Path $EditorRoot 'Data/PlaybackEngines/AndroidPlayer/SDK/build-tools'
    $Signer=Get-ChildItem -LiteralPath $Tools -Directory | Where-Object {$_.Name -match '^\d+\.\d+\.\d+$'} | Sort-Object {[version]$_.Name} -Descending |
        ForEach-Object {Join-Path $_.FullName 'lib/apksigner.jar'} | Where-Object {Test-Path -LiteralPath $_ -PathType Leaf} | Select-Object -First 1
    if (-not $Signer) { throw 'Android apksigner.jar was not found.' }
    $Verify=Invoke-AndroidReleaseProcess (Join-Path $Jdk 'java.exe') @('-jar',$Signer,'verify','--print-certs',$Artifact)
    $Text=$Verify.stdout+"`n"+$Verify.stderr
    [IO.File]::WriteAllText((Join-Path $Directory 'signature-verification.log'),$Text)
    $Matches=[regex]::Matches($Text,'(?im)^Signer #\d+ certificate SHA-256 digest:\s*([0-9a-f]{64})\s*$')
    if ($Verify.exit_code -ne 0 -or $Matches.Count -ne 1) { throw 'APK signature verification failed or signer identity is ambiguous.' }
    return $Matches[0].Groups[1].Value.ToLowerInvariant()
}
function Assert-AndroidReleaseEPath([string]$Path) {
    $Full=[IO.Path]::GetFullPath($Path)
    if (-not $Full.StartsWith('E:\Mofumachi-Validation\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Release workspace must be under E:\Mofumachi-Validation.' }
    $Ancestor=$Full
    while ($Ancestor) {
        if (Test-Path -LiteralPath $Ancestor) {
            if ((Get-Item -LiteralPath $Ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Linked E-drive validation paths are not allowed.' }
        }
        $Ancestor=Split-Path -Parent $Ancestor
    }
}

# Helpers are safe to inspect in limited script checks; dot sourcing never starts a build.
if ($MyInvocation.InvocationName -eq '.') { return }
if ($env:OS -ne 'Windows_NT') { throw 'Use the release entry on Windows 11; no Unity process was started.' }
$Repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$RunId='rc-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8)
$Run=Join-Path $ReleaseRoot ('Diagnostics\release-candidate\'+$RunId)
$Project=Join-Path $ReleaseRoot ('projects\'+$RunId)
$OutputDir=Join-Path $ReleaseRoot ('release-candidate\'+$RunId)
foreach ($Path in @($Run,$Project,$OutputDir)) { Assert-AndroidReleaseEPath $Path }
$null=[IO.Directory]::CreateDirectory($Run)
$ProcessTemp=Join-Path $ReleaseRoot ('temp/'+$RunId+'/tools')
$null=[IO.Directory]::CreateDirectory($ProcessTemp)
$ReleaseProcessEnvironment=@{TEMP=$ProcessTemp;TMP=$ProcessTemp;UPM_CACHE_ROOT=(Join-Path $ReleaseRoot 'upm-cache')}
$Config=@{};foreach ($Name in $ReleaseSigningNames) { $Config[$Name]=[Environment]::GetEnvironmentVariable($Name) }
$Secrets=@($Config.Values)
$Result=[ordered]@{result='Blocked';product_commit=$ReleaseProductCommit;source_commit=$null;stage8_complete=$false;
    unity_invocations=0;automatic_retries=0;first_error=$null;runner_error=$null;failed_phase='preflight';
    source_unchanged=$null;signing_preflight=$null;signing_certificate_sha256=$null;aab=$null;apk=$null;
    build_requested=[bool]$Build;output_root=$OutputDir;run=$Run;package='com.mofumachi.merge';unity_version='6000.6.4f1'}
$Git=$null;$Revision=$null;$BaselineKeyHash=$null;$FormalWasClean=$false;$ToolIdentity=$null
try {
    $Git=Find-AndroidReleaseGit
    $Head=Invoke-AndroidReleaseGit $Git $Repository @('rev-parse','HEAD')
    if ($Head.exit_code -ne 0 -or $Head.stdout.Trim() -notmatch '^[a-f0-9]{40}$') { throw 'Cannot read committed tooling HEAD.' }
    $Revision=$Head.stdout.Trim();$Result.source_commit=$Revision
    [IO.File]::WriteAllText((Join-Path $Run 'source-revision.txt'),$Revision+"`n")
    $Result.signing_preflight=Get-AndroidReleaseSigningPreflight $Config
    if (-not $Result.signing_preflight.passed) { throw ($Result.signing_preflight.problems -join '; ') }
    foreach ($Name in @('GIT_DIR','GIT_WORK_TREE','GIT_INDEX_FILE','GIT_COMMON_DIR')) {
        if ([Environment]::GetEnvironmentVariable($Name)) { throw ('Custom '+$Name+' is set; no source operations will start.') }
    }
    $State=Invoke-AndroidReleaseGit $Git $Repository @('status','--porcelain=v1','--untracked-files=all')
    if ($State.exit_code -ne 0 -or $State.stdout) { throw 'Formal repository must be clean; no reset, stash or discard was attempted.' }
    $FormalWasClean=$true
    $Branch=Invoke-AndroidReleaseGit $Git $Repository @('branch','--show-current')
    if ($Branch.exit_code -ne 0 -or $Branch.stdout.Trim() -cne 'feat/daily-world-pass-20261016') { throw 'Wrong formal repository branch.' }
    $Relation=Invoke-AndroidReleaseGit $Git $Repository @('merge-base','--is-ancestor',$ReleaseProductCommit,$Revision)
    if ($Relation.exit_code -ne 0) { throw 'Pinned Stage7 product is not an ancestor of this tooling commit.' }
    $Changes=Invoke-AndroidReleaseGit $Git $Repository @('diff','--name-status',$ReleaseProductCommit,$Revision,'--','Assets','Packages','ProjectSettings','References/VisualLock')
    if ($Changes.exit_code -ne 0) { throw 'Cannot compare the pinned product tree.' }
    foreach ($Line in @($Changes.stdout -split '\r?\n' | Where-Object {$_})) {
        if ($Line -cnotmatch '^A\tAssets/Mofumachi/Editor/AndroidReleaseBuild\.cs(?:\.meta)?$') { throw 'Product or Visual Lock differs beyond the two explicit new release Editor files.' }
    }
    $Qa=Invoke-AndroidReleaseGit $Git $Repository @('diff','--quiet','--no-ext-diff','--no-textconv',$ReleaseProductCommit,$Revision,'--','Assets/Mofumachi/Editor/AndroidBuild.cs','scripts/build-android.ps1')
    if ($Qa.exit_code -ne 0) { throw 'An existing QA/Development build entry differs from the pinned version.' }
    $ToolHashes=@(foreach ($Name in @('scripts/build-android-release.ps1','Assets/Mofumachi/Editor/AndroidReleaseBuild.cs','Assets/Mofumachi/Editor/AndroidReleaseBuild.cs.meta')) {
        (Get-FileHash -LiteralPath (Join-Path $Repository $Name) -Algorithm SHA256).Hash.ToLowerInvariant()
    })
    $Hasher=[Security.Cryptography.SHA256]::Create()
    try { $ToolIdentity=([BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes(($ToolHashes -join ':'))))).Replace('-','').ToLowerInvariant() } finally { $Hasher.Dispose() }
    $Result['build_entry_identity']=$ToolIdentity
    $Result.source_unchanged=$true
    $Settings=[IO.File]::ReadAllText((Join-Path $Repository 'ProjectSettings/ProjectSettings.asset'))
    if ($Settings -notmatch '(?m)^\s+Android: com\.mofumachi\.merge\s*$') { throw 'Release package mismatch.' }
    $Profile=[IO.File]::ReadAllText((Join-Path $Repository 'Assets/Settings/Build Profiles/Android™.asset'))
    foreach ($Flag in @('m_Development','m_AllowDebugging','m_BuildWithDeepProfilingSupport','m_ConnectProfiler')) {
        if ($Profile -notmatch ('(?m)^\s+'+[regex]::Escape($Flag)+': 0\s*$')) { throw ('Release profile flag must be OFF: '+$Flag) }
    }
    if (-not $UnityEditor) { $UnityEditor=Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe' }
    if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw 'Unity 6000.6.4f1 Editor was not found; no Unity start.' }
    $EditorRoot=Split-Path -Parent $UnityEditor
    $Keytool=Join-Path $EditorRoot 'Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool.exe'
    if (-not (Test-Path -LiteralPath $Keytool -PathType Leaf)) { throw 'Unity Android JDK/keytool is required for public signing-certificate verification.' }
    $KeyCheck=Invoke-AndroidReleaseProcess $Keytool @('-J-Duser.language=en','-J-Duser.country=US','-list','-v','-keystore',$Config.MOFUMACHI_KEYSTORE_PATH,'-alias',$Config.MOFUMACHI_KEY_ALIAS,'-storepass:env','MOFUMACHI_KEYSTORE_PASSWORD') @{MOFUMACHI_KEYSTORE_PASSWORD=$Config.MOFUMACHI_KEYSTORE_PASSWORD} $Secrets
    $KeyText=$KeyCheck.stdout+[Environment]::NewLine+$KeyCheck.stderr
    [IO.File]::WriteAllText((Join-Path $Run 'signing-preflight.log'),$KeyText)
    $Match=[regex]::Match($KeyText,'(?im)^\s*SHA256:\s*([0-9a-f:]{95})\s*$')
    if ($KeyCheck.exit_code -ne 0 -or -not $Match.Success) { throw 'Signing certificate could not be verified; inspect the redacted signing-preflight log.' }
    $Result.signing_certificate_sha256=$Match.Groups[1].Value.Replace(':','').ToLowerInvariant()
    $BaselineKeyHash=(Get-FileHash -LiteralPath $Config.MOFUMACHI_KEYSTORE_PATH -Algorithm SHA256).Hash
    [IO.File]::WriteAllText((Join-Path $Run 'source-revision.txt'),$Revision+"`n")
    Write-AndroidReleaseJson (Join-Path $Run 'build-settings.json') ([ordered]@{product_commit=$ReleaseProductCommit;source_commit=$Revision;
        version=([regex]::Match($Settings,'(?m)^\s+bundleVersion: (.+)$').Groups[1].Value.Trim());
        version_code=([regex]::Match($Settings,'(?m)^\s+AndroidBundleVersionCode: (\d+)').Groups[1].Value);
        release_flags='Development/Script Debugging/Deep Profiling/Autoconnect OFF; BuildOptions.None';
        package='com.mofumachi.merge';backend='IL2CPP';architecture='ARM64';orientation='Portrait';
        signing_certificate_sha256=$Result.signing_certificate_sha256;effective_unity_snapshot='Each build phase unity-build-info.json; absent until an actual Unity build'})
    if (-not $Build) { $Result.result='PreflightPassed';$Result.failed_phase=$null;Write-Host 'Release preflight passed. No Unity start: -Build was not specified.' }
    else {
        $Add=Invoke-AndroidReleaseGit $Git $Repository @('worktree','add','--detach',$Project,$Revision)
        [IO.File]::WriteAllText((Join-Path $Run 'worktree-add.stdout.log'),$Add.stdout)
        [IO.File]::WriteAllText((Join-Path $Run 'worktree-add.stderr.log'),$Add.stderr)
        Write-AndroidReleaseJson (Join-Path $Run 'worktree-add.exit.json') @{exit_code=$Add.exit_code}
        if ($Add.exit_code -ne 0) { throw 'Release worktree checkout failed; original logs retained; no retry or Unity start.' }
        foreach ($Name in @('Assets','Packages','ProjectSettings','scripts')) {
            if (-not (Test-Path -LiteralPath (Join-Path $Project $Name) -PathType Container)) { throw ('Incomplete checkout, missing '+$Name) }
        }
        $Registration=Invoke-AndroidReleaseGit $Git $Repository @('worktree','list','--porcelain')
        if ($Registration.exit_code -ne 0 -or -not $Registration.stdout.Replace('\','/').Contains('worktree '+$Project.Replace('\','/'))) { throw 'Release worktree registration is not normal.' }
        $Initial=Get-AndroidReleaseSourceCheck $Git $Project $Revision (Join-Path $Run 'checkout-source')
        if (-not $Initial.unchanged) { throw 'Release checkout is not complete and clean; no Unity start.' }
        $Kinds=if ($Format -eq 'Both') {@('Aab','Apk')} else {@($Format)}
        foreach ($Kind in $Kinds) {
            $Phase=$Kind.ToLowerInvariant();$Result.failed_phase=$Phase
            $Directory=Join-Path $Run $Phase;$null=[IO.Directory]::CreateDirectory($Directory)
            $Before=Get-AndroidReleaseSourceCheck $Git $Project $Revision (Join-Path $Directory 'before')
            if (-not $Before.unchanged) { throw 'Source changed before the next output; no Unity start.' }
            if ((Get-FileHash -LiteralPath $Config.MOFUMACHI_KEYSTORE_PATH -Algorithm SHA256).Hash -cne $BaselineKeyHash) { throw 'Local signing file changed between phases.' }
            $Snapshot=New-AndroidReleaseBuildSnapshot $Git $Project $Revision (Join-Path $Directory 'source-recovery') $Repository
            # Review-only commits do not grant another attempt of the same product/tooling.
            $Receipt=Join-Path $ReleaseRoot ('receipts/release-'+$ReleaseProductCommit+'-'+$ToolIdentity+'-'+$Phase+'.attempted')
            New-AndroidReleaseReceipt $Receipt $Run
            $null=[IO.Directory]::CreateDirectory($OutputDir)
            $Output=Join-Path $OutputDir ('mofumachi-merge-'+$RunId+$(if($Kind -eq 'Apk'){'-pixel3a'}else{''})+'.'+$Phase)
            if (Test-Path -LiteralPath $Output) { throw 'Output already exists; overwrite is forbidden.' }
            $Temp=Join-Path $ReleaseRoot ('temp/'+$RunId+'/'+$Phase);$Cache=Join-Path $ReleaseRoot 'upm-cache'
            $null=[IO.Directory]::CreateDirectory($Temp);$null=[IO.Directory]::CreateDirectory($Cache)
            $Child=$Config.Clone();$Child['TEMP']=$Temp;$Child['TMP']=$Temp;$Child['UPM_CACHE_ROOT']=$Cache
            $Child['MOFUMACHI_SIGNING_CERT_SHA256']=$Result.signing_certificate_sha256
            $Info=Join-Path $Directory 'unity-build-info.json'
            $Arguments=@(Get-AndroidReleaseArguments $Project $Kind $Output $Info $Revision)
            [IO.File]::WriteAllLines((Join-Path $Directory 'unity-arguments.txt'),[string[]]$Arguments)
            $Result.result='Running';$Result.unity_invocations++
            Write-AndroidReleaseJson (Join-Path $Run 'build-info.json') $Result
            Write-Host ($Kind+': one release build, no retries. Sanitized log: '+(Join-Path $Directory 'build.log'))
            $Native=Invoke-AndroidReleaseProcess $UnityEditor $Arguments $Child $Secrets
            $Log=$Native.stdout+[Environment]::NewLine+$Native.stderr
            [IO.File]::WriteAllText((Join-Path $Directory 'build.log'),$Log)
            $Recovery=$null
            if ($Native.exit_code -eq 0) { $Recovery=Repair-AndroidReleaseBuildSource $Git $Snapshot $Secrets }
            $After=Get-AndroidReleaseSourceCheck $Git $Project $Revision (Join-Path $Directory 'after')
            $Result.source_unchanged=$After.unchanged -and ($null -eq $Recovery -or $Recovery.passed)
            $Row=[ordered]@{exit_code=$Native.exit_code;path=$Output;size=$null;sha256=$null;source_unchanged=$Result.source_unchanged;result='Failed';build_log=(Join-Path $Directory 'build.log')}
            $Result[$Phase]=$Row
            if ($Native.exit_code -ne 0) { $Result.first_error=Get-AndroidReleaseFirstError $Log;throw 'Unity release build failed; first native error is preserved separately.' }
            if ($null -ne $Recovery -and -not $Recovery.passed) { throw ('Known build-side-effect recovery failed: '+$Recovery.problem) }
            if (-not $After.unchanged) { throw 'Tracked content or non-allowlisted files changed after the release build.' }
            if (-not (Test-Path -LiteralPath $Info -PathType Leaf)) { throw 'Missing native release build settings/result snapshot.' }
            $NativeInfo=[IO.File]::ReadAllText($Info)|ConvertFrom-Json
            if ($NativeInfo.result -cne 'Passed' -or -not $NativeInfo.temporary_settings_restored -or
                $NativeInfo.source_commit -cne $Revision -or $NativeInfo.signing_certificate_sha256 -cne $Result.signing_certificate_sha256 -or
                $NativeInfo.development -or $NativeInfo.script_debugging -or $NativeInfo.deep_profiling -or $NativeInfo.autoconnect_profiler -or
                -not $NativeInfo.custom_signing -or $NativeInfo.build_options -cne 'None' -or $NativeInfo.build_app_bundle -ne ($Kind -eq 'Aab')) { throw 'Native release metadata does not satisfy the exact release gate.' }
            $Artifact=Get-Item -LiteralPath $Output -ErrorAction Stop
            if ($Artifact.Length -le 0) { throw 'Release artifact is empty.' }
            $ActualCertificate=Get-AndroidReleaseArtifactCertificate $Kind $Output $EditorRoot $Directory
            if ($ActualCertificate -cne $Result.signing_certificate_sha256) { throw 'Artifact signer certificate differs from the verified local release signing identity.' }
            $Row['signing_certificate_sha256']=$ActualCertificate
            $Row.size=$Artifact.Length;$Row.sha256=(Get-FileHash -LiteralPath $Output -Algorithm SHA256).Hash.ToLowerInvariant();$Row.result='Passed'
        }
        $Result.result='Passed';$Result.failed_phase=$null
        Write-Host 'Requested release outputs passed. Pixel3a checks and Stage8 acceptance are still pending.'
    }
} catch {
    $Result.runner_error=Protect-AndroidReleaseText $_.Exception.Message $Secrets
    $Result.result=$(if($Result.unity_invocations -gt 0){'Failed'}else{'Blocked'})
    $Text=$(if($Result.first_error){$Result.first_error}else{'No native Unity/build error available. Stop reason: '+$Result.runner_error})
    [IO.File]::WriteAllText((Join-Path $Run 'first-error.log'),$Text)
    Write-Host ('STOP: '+$Result.runner_error+' No automatic retry.')
} finally {
    if ($Git -and $Revision -and $FormalWasClean) {
        $Formal=Invoke-AndroidReleaseGit $Git $Repository @('status','--porcelain=v1','--untracked-files=all')
        $FormalHead=Invoke-AndroidReleaseGit $Git $Repository @('rev-parse','HEAD')
        $Result['formal_repo_unchanged']=($Formal.exit_code -eq 0 -and -not $Formal.stdout -and $FormalHead.exit_code -eq 0 -and $FormalHead.stdout.Trim() -ceq $Revision)
        if (-not $Result.formal_repo_unchanged) { $Result.result='Failed';$Result.runner_error='Formal repository changed during the operation; no changes were discarded.' }
    }
    Write-AndroidReleaseJson (Join-Path $Run 'build-info.json') $Result
    Write-AndroidReleaseJson (Join-Path $Run 'status.json') $Result
    $Hashes=@(foreach ($File in (Get-ChildItem -LiteralPath $Run -File -Recurse)) {
        if ($File.Name -ne 'checksums.sha256') { (Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$File.FullName.Substring($Run.Length+1).Replace('\','/') }
    })
    foreach ($Kind in @('aab','apk')) { if ($Result[$Kind] -and $Result[$Kind].sha256) { $Hashes+=($Result[$Kind].sha256+'  '+$Result[$Kind].path) } }
    [IO.File]::WriteAllLines((Join-Path $Run 'checksums.sha256'),[string[]]$Hashes)
    Write-Host ('Evidence retained on E: '+$Run)
}
if ($Result.result -notin @('Passed','PreflightPassed')) { exit 1 }
