#requires -Version 5.1
[CmdletBinding()]
param()
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
# Reuse process redaction and Git discovery only. Dot sourcing does not start a build.
. (Join-Path $PSScriptRoot 'build-android-release.ps1')
$SigningFolder = 'E:\Mofumachi-Signing'
$SigningKey = 'E:\Mofumachi-Signing\mofumachi-release.jks'
$SigningAlias = 'mofumachi-release'
$SigningKeytool = 'C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe'

function Get-SigningRegularFiles([string]$Folder, [switch]$Repository) {
    if (-not (Test-Path -LiteralPath $Folder)) { return }
    $Root = Get-Item -LiteralPath $Folder -Force
    if (-not $Root.PSIsContainer -or ($Root.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Signing/repository root must be a regular directory.' }
    $Queue = New-Object 'Collections.Generic.Queue[string]'
    $Queue.Enqueue($Root.FullName)
    while ($Queue.Count -gt 0) {
        foreach ($Item in (Get-ChildItem -LiteralPath $Queue.Dequeue() -Force -ErrorAction Stop)) {
            if ($Repository -and $Item.Name -ceq '.git') { continue }
            if ($Item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'A linked path prevents safe signing-file inspection; nothing will be followed or removed.' }
            if ($Item.PSIsContainer) { $Queue.Enqueue($Item.FullName) } else { $Item }
        }
    }
}
function Test-ExistingSigningKey([string]$Folder) {
    foreach ($File in (Get-SigningRegularFiles $Folder)) {
        if ($File.Extension -imatch '^\.(jks|keystore)$') { return $true }
    }
    return $false
}
function Test-SigningRepositoryFiles([string]$Folder) {
    foreach ($File in (Get-SigningRegularFiles $Folder -Repository)) {
        if ($File.Extension -imatch '^\.(jks|keystore)$' -or $File.Name -ieq '.env' -or
            $File.Name -imatch '(passwords?|secrets?|private[-_.]?key|パスワード|秘密)(?:\.[^.]+)?$') { return $false }
    }
    return $true
}
function Convert-SigningSecureValue([Security.SecureString]$Value) {
    $Pointer = [IntPtr]::Zero
    try { $Pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value); return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($Pointer) }
    finally { if ($Pointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($Pointer) } }
}
function Read-SigningPassword([string]$Label) {
    $First = Read-Host ($Label + ' (6文字以上・非表示)') -AsSecureString
    $Second = $null
    try {
        $Second = Read-Host ($Label + ' をもう一度 (非表示)') -AsSecureString
        $A = Convert-SigningSecureValue $First
        $B = Convert-SigningSecureValue $Second
        if ([string]::IsNullOrWhiteSpace($A) -or $A.Length -lt 6 -or -not [string]::Equals($A, $B, [StringComparison]::Ordinal)) { throw 'Password confirmation failed or the required minimum was not met. No keytool retry.' }
        return $A
    } finally { $First.Dispose(); if ($null -ne $Second) { $Second.Dispose() }; $A = $null; $B = $null }
}
function Get-SigningGenerationArguments([string]$Output) {
    return @('-J-Duser.language=en','-J-Duser.country=US','-genkeypair','-storetype','JKS','-keyalg','RSA',
        '-keysize','2048','-validity','10000','-alias',$SigningAlias,'-dname','CN=Mofumachi Release',
        '-keystore',$Output,'-storepass:env','MOFUMACHI_KEYSTORE_PASSWORD','-keypass:env','MOFUMACHI_KEY_ALIAS_PASSWORD')
}
function New-SigningPublicStatus([string]$CertificateSha, [string]$KeystoreSha) {
    if ($CertificateSha -notmatch '^[a-f0-9]{64}$' -or $KeystoreSha -notmatch '^[a-f0-9]{64}$') { throw 'Public signing fingerprints are invalid.' }
    return [ordered]@{keystore_prepared=$true;alias_configured=$true;env_vars_configured=$true;
        certificate_sha256=$CertificateSha;keystore_sha256=$KeystoreSha;backup_pending=$true;secrets_recorded=$false}
}
function Invoke-SigningGit([string]$Git,[string]$Repo,[string[]]$Arguments) {
    $Result = Invoke-AndroidReleaseGit $Git $Repo $Arguments
    if ($Result.exit_code -ne 0) { throw ('Git operation failed (no retry): '+$Result.stderr) }
    return $Result.stdout.Trim()
}
function Assert-SigningFormalRepo([string]$Git,[string]$Repo,[string]$ExpectedHead='') {
    if ((Invoke-SigningGit $Git $Repo @('branch','--show-current')) -cne 'feat/daily-world-pass-20261016') { throw 'Wrong formal repository branch.' }
    if (Invoke-SigningGit $Git $Repo @('status','--porcelain=v1','--untracked-files=all')) { throw 'Formal repository is dirty; nothing was reset, stashed or discarded.' }
    $Head = Invoke-SigningGit $Git $Repo @('rev-parse','HEAD')
    if ($ExpectedHead -and $Head -cne $ExpectedHead) { throw 'Formal repository HEAD changed during signing setup.' }
    if (-not (Test-SigningRepositoryFiles $Repo)) { throw 'A keystore or secret-like file exists inside the formal repository. Stop; do not delete it automatically.' }
    return $Head
}
function Assert-SigningFolderAncestors {
    if (-not (Test-Path -LiteralPath 'E:\' -PathType Container)) { throw 'E drive is not available.' }
    $Parent = $SigningFolder
    while ($Parent) {
        if ((Test-Path -LiteralPath $Parent) -and ((Get-Item -LiteralPath $Parent -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Linked signing-folder ancestor refused.' }
        $Parent = Split-Path -Parent $Parent
    }
}
function Publish-SigningPublicStatus([string]$Git,[string]$Repo,$Public) {
    # Only these seven public fields are serialized. Never copy the signing directory.
    $Expected=@('keystore_prepared','alias_configured','env_vars_configured','certificate_sha256','keystore_sha256','backup_pending','secrets_recorded')
    if (@($Public.Keys).Count -ne 7 -or @($Public.Keys | Where-Object {$_ -cnotin $Expected}).Count) { throw 'Public status field allowlist mismatch.' }
    $null = Invoke-SigningGit $Git $Repo @('fetch','--no-tags','origin','feat/daily-world-pass-20261016')
    $Remote = Invoke-SigningGit $Git $Repo @('rev-parse','FETCH_HEAD')
    $Publication = Join-Path $ReleaseRoot ('sharing/signing-'+[guid]::NewGuid().ToString('N'))
    Assert-AndroidReleaseEPath $Publication
    $null=[IO.Directory]::CreateDirectory((Split-Path -Parent $Publication))
    $null=Invoke-SigningGit $Git $Repo @('worktree','add','--detach','--no-checkout',$Publication,$Remote)
    $null=Invoke-SigningGit $Git $Publication @('read-tree','HEAD')
    $Relative='References/Review/inbox/latest-release-candidate/signing-status.json'
    $Target=Join-Path $Publication $Relative
    $null=[IO.Directory]::CreateDirectory((Split-Path -Parent $Target))
    Write-AndroidReleaseJson $Target $Public
    $null=Invoke-SigningGit $Git $Publication @('add','--',$Relative)
    if ((Invoke-SigningGit $Git $Publication @('diff','--cached','--name-only')) -cne $Relative) { throw 'Unexpected staged publication paths; nothing pushed.' }
    $null=Invoke-SigningGit $Git $Publication @('commit','-m','Share public release-signing preparation status')
    $Revision=Invoke-SigningGit $Git $Publication @('rev-parse','HEAD')
    if ((Invoke-SigningGit $Git $Publication @('diff-tree','--no-commit-id','--name-only','-r','HEAD')) -cne $Relative) { throw 'Publication commit contains an unexpected path; nothing pushed.' }
    $null=Invoke-SigningGit $Git $Publication @('push','--no-follow-tags','origin',($Revision+':refs/heads/feat/daily-world-pass-20261016'))
    return $Revision
}

if ($MyInvocation.InvocationName -eq '.') { return }
if ($env:OS -ne 'Windows_NT') { throw 'Signing setup is Windows-local only; no keytool was started.' }
Set-PSDebug -Off
$Repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ($Repository -ine 'C:\Users\User\Desktop\Mofumachi\mofumachi-merge') { throw 'Use the helper in the approved formal Windows repository.' }
$Original=@{};$Secrets=@();$EnvironmentChanged=$false;$Prepared=$false;$Head=$null;$Git=$null
$StorePassword=$null;$AliasPassword=$null
try {
    Assert-SigningFolderAncestors
    if (Test-ExistingSigningKey $SigningFolder) { throw 'Existing keystore found. Existence only checked; no file was overwritten, opened, deleted or regenerated.' }
    if (Test-Path -LiteralPath $SigningKey) { throw 'The intended output path already exists; no overwrite.' }
    $Receipt=Join-Path $SigningFolder '.mofumachi-release.creation-attempted'
    if (Test-Path -LiteralPath $Receipt) { throw 'A previous creation-attempt receipt exists. No deletion or retry.' }
    if (-not (Test-Path -LiteralPath $SigningKeytool -PathType Leaf)) { throw 'The specified Unity6000.6.4f1 Android JDK keytool is missing; no other Java was selected.' }
    foreach ($Name in @('GIT_DIR','GIT_WORK_TREE','GIT_INDEX_FILE','GIT_COMMON_DIR')) {
        if ([Environment]::GetEnvironmentVariable($Name)) { throw 'A custom Git environment is set; signing setup stopped.' }
    }
    foreach ($Name in $ReleaseSigningNames) {
        $Original[$Name]=[Environment]::GetEnvironmentVariable($Name,[EnvironmentVariableTarget]::Process)
        if (-not [string]::IsNullOrEmpty($Original[$Name])) { throw 'An existing signing environment value is set; it was not changed. Do not recreate a key.' }
    }
    $Git=Find-AndroidReleaseGit
    $Head=Assert-SigningFormalRepo $Git $Repository
    $StorePassword=Read-SigningPassword 'Keystore password'
    $AliasPassword=Read-SigningPassword 'Alias password'
    $Secrets=@($StorePassword,$AliasPassword)
    if (Test-ExistingSigningKey $SigningFolder) { throw 'A keystore appeared during input; no generation.' }
    New-AndroidReleaseReceipt $Receipt 'signing-setup-v1; one key generation only; no automatic retry'
    $Staging=Join-Path $SigningFolder ('.mofumachi-release-'+[guid]::NewGuid().ToString('N')+'.jks')
    if (Test-Path -LiteralPath $Staging) { throw 'Staging output exists; no overwrite or retry.' }
    $PrivateEnvironment=@{MOFUMACHI_KEYSTORE_PASSWORD=$StorePassword;MOFUMACHI_KEY_ALIAS_PASSWORD=$AliasPassword}
    $Generated=Invoke-AndroidReleaseProcess $SigningKeytool @(Get-SigningGenerationArguments $Staging) $PrivateEnvironment $Secrets
    if ($Generated.exit_code -ne 0) { throw ('keytool generation failed, exit '+$Generated.exit_code+': '+$Generated.stderr) }
    $Created=Get-Item -LiteralPath $Staging -Force -ErrorAction Stop
    if ($Created.Length -le 0 -or ($Created.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Generated keystore is not a nonempty regular file.' }
    [IO.File]::Move($Staging,$SigningKey)
    $Listing=Invoke-AndroidReleaseProcess $SigningKeytool @('-J-Duser.language=en','-J-Duser.country=US','-list','-v','-keystore',$SigningKey,
        '-alias',$SigningAlias,'-storepass:env','MOFUMACHI_KEYSTORE_PASSWORD') $PrivateEnvironment $Secrets
    if ($Listing.exit_code -ne 0) { throw ('keytool alias verification failed, exit '+$Listing.exit_code+': '+$Listing.stderr) }
    $PublicText=$Listing.stdout+"`n"+$Listing.stderr
    $Certificate=[regex]::Match($PublicText,'(?im)^\s*SHA256:\s*([0-9a-f:]{95})\s*$')
    if (-not $Certificate.Success -or $PublicText -notmatch 'PrivateKeyEntry') { throw 'Alias/private-key entry or certificate SHA256 could not be verified.' }
    $CertificateSha=$Certificate.Groups[1].Value.Replace(':','').ToLowerInvariant()
    $KeyHash=(Get-FileHash -LiteralPath $SigningKey -Algorithm SHA256).Hash.ToLowerInvariant()
    $Values=@{MOFUMACHI_KEYSTORE_PATH=$SigningKey;MOFUMACHI_KEYSTORE_PASSWORD=$StorePassword;MOFUMACHI_KEY_ALIAS=$SigningAlias;MOFUMACHI_KEY_ALIAS_PASSWORD=$AliasPassword}
    $EnvironmentChanged=$true
    foreach ($Name in $ReleaseSigningNames) { [Environment]::SetEnvironmentVariable($Name,$Values[$Name],[EnvironmentVariableTarget]::Process) }
    foreach ($Name in $ReleaseSigningNames) {
        if ([string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable($Name,[EnvironmentVariableTarget]::Process))) { throw 'One signing environment variable was not set; no retry.' }
    }
    $null=Assert-SigningFormalRepo $Git $Repository $Head
    $Public=New-SigningPublicStatus $CertificateSha $KeyHash
    $ReportFolder=Join-Path $ReleaseRoot 'Diagnostics/signing-setup'
    Assert-AndroidReleaseEPath $ReportFolder
    $null=[IO.Directory]::CreateDirectory($ReportFolder)
    $Report=Join-Path $ReportFolder ('signing-status-'+[guid]::NewGuid().ToString('N')+'.json')
    $Bytes=[Text.Encoding]::UTF8.GetBytes(($Public|ConvertTo-Json))
    $Stream=[IO.File]::Open($Report,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try { $Stream.Write($Bytes,0,$Bytes.Length);$Stream.Flush() } finally { $Stream.Dispose() }
    $Prepared=$true
    Write-Host 'Signing setup succeeded. No plaintext password was printed or written to a file. Backup is pending.'
    Write-Host ('Keystore: '+$SigningKey)
    Write-Host ('Alias: '+$SigningAlias)
    Write-Host ('Bytes: '+(Get-Item -LiteralPath $SigningKey).Length)
    Write-Host ('Keystore SHA256: '+$KeyHash)
    Write-Host ('Certificate SHA256: '+$CertificateSha)
    foreach ($Name in $ReleaseSigningNames) { Write-Host ($Name+': SET') }
    Write-Host ('Public status only: '+$Report)
    try {
        $Shared=Publish-SigningPublicStatus $Git $Repository $Public
        Write-Host ('Public status shared at '+$Shared)
    } catch { Write-Host ('Public sharing stopped without retry. Key and session remain prepared; local public report retained. '+(Protect-AndroidReleaseText $_.Exception.Message $Secrets)) }
    $null=Assert-SigningFormalRepo $Git $Repository $Head
    Write-Host 'Formal repo clean: YES (HEAD unchanged). Keystore in Git: NO.'
    Write-Host 'Signing prerequisites ready: YES. Release preflight has not been executed.'
    Write-Host 'Keep this PowerShell window open: the variables exist only in this process. Do not rerun key creation.'
    Write-Host 'No backup copy, release preflight, Unity build, AAB/APK or Pixel action was performed.'
} catch {
    Write-Host ('STOP: '+(Protect-AndroidReleaseText $_.Exception.Message $Secrets))
    Write-Host 'No retry, regeneration, overwrite or cleanup was performed. Existing files are retained.'
} finally {
    if ($EnvironmentChanged -and -not $Prepared) {
        foreach ($Name in $ReleaseSigningNames) { [Environment]::SetEnvironmentVariable($Name,$Original[$Name],[EnvironmentVariableTarget]::Process) }
    }
    $StorePassword=$null;$AliasPassword=$null;$Secrets=@();$Values=$null;$PrivateEnvironment=$null
}
