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
            $After=Get-AndroidReleaseSourceCheck $Git $Project $Revision (Join-Path $Directory 'after')
            $Result.source_unchanged=$After.unchanged
            $Row=[ordered]@{exit_code=$Native.exit_code;path=$Output;size=$null;sha256=$null;source_unchanged=$After.unchanged;result='Failed';build_log=(Join-Path $Directory 'build.log')}
            $Result[$Phase]=$Row
            if ($Native.exit_code -ne 0) { $Result.first_error=Get-AndroidReleaseFirstError $Log;throw 'Unity release build failed; first native error is preserved separately.' }
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
