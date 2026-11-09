$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$Entry = Join-Path $PSScriptRoot '../prepare-release-signing-windows.ps1'
if (-not (Test-Path -LiteralPath $Entry)) { throw 'Signing setup helper is not implemented yet (expected RED).' }
. $Entry
$script:SigningChecks = 0
function CheckSigning([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:SigningChecks++
}
$Scratch = Join-Path ([IO.Path]::GetTempPath()) ('signing-helper-fixture-' + [guid]::NewGuid().ToString('N'))
$null = [IO.Directory]::CreateDirectory($Scratch)
$Absent = Join-Path $Scratch 'absent'
CheckSigning (-not (Test-ExistingSigningKey $Absent)) 'Absent folder incorrectly reports a key.'
CheckSigning (-not (Test-Path -LiteralPath $Absent)) 'Existence check created a folder.'
$Folder = Join-Path $Scratch 'folder'
$null = [IO.Directory]::CreateDirectory($Folder)
CheckSigning (-not (Test-ExistingSigningKey $Folder)) 'Empty folder incorrectly reports a key.'
$FakeKey = Join-Path $Folder 'existing.JKS'
[IO.File]::WriteAllText($FakeKey, 'synthetic extension fixture; not a real key')
$Before = (Get-FileHash -LiteralPath $FakeKey -Algorithm SHA256).Hash
CheckSigning (Test-ExistingSigningKey $Folder) 'Existing JKS was missed.'
CheckSigning ((Get-FileHash -LiteralPath $FakeKey -Algorithm SHA256).Hash -ceq $Before) 'Existence check changed an existing file.'
$Nested = Join-Path $Scratch 'nested'
$null = [IO.Directory]::CreateDirectory((Join-Path $Nested 'child'))
[IO.File]::WriteAllText((Join-Path $Nested 'child/existing.keystore'), 'synthetic extension fixture')
CheckSigning (Test-ExistingSigningKey $Nested) 'Nested keystore was missed.'
$Repo = Join-Path $Scratch 'repo'
$null = [IO.Directory]::CreateDirectory($Repo)
[IO.File]::WriteAllText((Join-Path $Repo 'normal.cs'), '// fixture')
CheckSigning (Test-SigningRepositoryFiles $Repo) 'Normal source rejected.'
[IO.File]::WriteAllText((Join-Path $Repo 'passwords.txt'), 'synthetic, no real secrets')
CheckSigning (-not (Test-SigningRepositoryFiles $Repo)) 'Password file in repo was accepted.'
$RepoKey = Join-Path $Scratch 'repo-key'
$null = [IO.Directory]::CreateDirectory($RepoKey)
[IO.File]::WriteAllText((Join-Path $RepoKey 'private.keystore'), 'synthetic extension fixture')
CheckSigning (-not (Test-SigningRepositoryFiles $RepoKey)) 'Keystore in repo was accepted.'
$Public = New-SigningPublicStatus ('a' * 64) ('b' * 64)
$Expected = @('keystore_prepared','alias_configured','env_vars_configured','certificate_sha256','keystore_sha256','backup_pending','secrets_recorded')
CheckSigning (@($Public.Keys).Count -eq 7) 'Public status has unexpected fields.'
foreach ($Name in $Expected) { CheckSigning ($Public.Contains($Name)) ('Missing public field: ' + $Name) }
CheckSigning ($Public.secrets_recorded -eq $false -and $Public.backup_pending -eq $true) 'Public status flags incorrect.'
$Gen = @(Get-SigningGenerationArguments 'synthetic-staging.jks')
CheckSigning ($Gen -contains '-storepass:env') 'Store password does not use an environment reference.'
CheckSigning ($Gen -contains '-keypass:env') 'Alias password does not use an environment reference.'
CheckSigning ($Gen -notcontains '-storepass' -and $Gen -notcontains '-keypass') 'Plain password argument is possible.'
CheckSigning (($Gen -contains 'RSA') -and ($Gen -contains '2048') -and ($Gen -contains '10000') -and ($Gen -contains 'JKS')) 'Generation policy mismatch.'
CheckSigning (($Gen -join ' ') -notmatch 'synthetic-store-secret|synthetic-alias-secret') 'Synthetic secret leaked into args.'
$Secure = ConvertTo-SecureString 'synthetic-secret-only' -AsPlainText -Force
try { CheckSigning ((Convert-SigningSecureValue $Secure) -ceq 'synthetic-secret-only') 'Masked input conversion failed.' } finally { $Secure.Dispose() }
$Receipt = Join-Path $Scratch 'attempted'
New-AndroidReleaseReceipt $Receipt 'signing-setup-v1'
$Rejected = $false
try { New-AndroidReleaseReceipt $Receipt 'replacement' } catch { $Rejected = $true }
CheckSigning $Rejected 'Second creation attempt accepted.'
CheckSigning ([IO.File]::ReadAllText($Receipt) -ceq 'signing-setup-v1') 'Creation receipt overwritten.'
Write-Host ("PASS signing-setup helper checks: $script:SigningChecks. Synthetic files only; no keytool, real key, Windows setup, Unity or build execution. Scratch: $Scratch")
