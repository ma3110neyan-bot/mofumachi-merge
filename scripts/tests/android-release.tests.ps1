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
