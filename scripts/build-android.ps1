[CmdletBinding()]
param([string]$UnityEditor = "$env:ProgramFiles\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe", [switch]$Qa)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
function Assert-TestRun([string]$ResultPath,[int]$MinimumTests,[string[]]$RequiredSuites) {
    if (-not (Test-Path -LiteralPath $ResultPath)) { throw "Missing current test XML: $ResultPath" }
    [xml]$Results = Get-Content -LiteralPath $ResultPath -Raw
    $Run = $Results.'test-run'
    if ($null -eq $Run -or [int]$Run.total -lt $MinimumTests -or $Run.result -ne 'Passed' -or [int]$Run.failed -ne 0 -or [int]$Run.skipped -ne 0 -or [int]$Run.passed -ne [int]$Run.total) { throw "Incomplete or failed tests: $ResultPath" }
    $Cases=@($Results.SelectNodes('//test-case'))
    if($Cases.Count -ne [int]$Run.total -or @($Cases | Where-Object { $_.result -ne 'Passed' }).Count -gt 0) { throw "Not every test case passed: $ResultPath" }
    foreach($Suite in $RequiredSuites) {
        if(@($Results.SelectNodes('//test-suite') | Where-Object { $_.GetAttribute('name') -eq $Suite -and $_.GetAttribute('result') -eq 'Passed' }).Count -eq 0) { throw "Missing passed suite: $Suite" }
    }
    Write-Host "Tests passed: $($Run.passed)/$($Run.total)"
}
function Invoke-Unity([string[]]$UnityArguments) {
    $ArgumentText=($UnityArguments | ForEach-Object { '"'+$_+'"' }) -join ' '
    $Process=Start-Process -FilePath $UnityEditor -ArgumentList $ArgumentText -Wait -PassThru
    if($Process.ExitCode -ne 0){throw "Unity exit $($Process.ExitCode). Inspect $OutputDir"}
}
# Dot sourcing exposes the same gates to script tests without starting Unity.
if($MyInvocation.InvocationName -eq '.'){return}
$ProjectPath=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if($ProjectPath -match '[^\x00-\x7F]'){throw 'Move the entire repository to an ASCII path before Android builds.'}
if(-not(Test-Path -LiteralPath $UnityEditor -PathType Leaf)){throw 'Pass -UnityEditor with Unity 6000.6.4f1 Unity.exe.'}
$UnityEditor=(Resolve-Path -LiteralPath $UnityEditor).Path
if((Get-Content (Join-Path $ProjectPath 'ProjectSettings\ProjectVersion.txt') -Raw) -notmatch 'm_EditorVersion: 6000\.6\.4f1\s'){throw 'Wrong project Unity version.'}
$RunName=(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8)
$OutputDir=Join-Path $ProjectPath "Builds\Android\$RunName"
New-Item -ItemType Directory -Path $OutputDir | Out-Null
$EditXml=Join-Path $OutputDir 'editmode.xml'
Invoke-Unity @('-batchmode','-nographics','-projectPath',$ProjectPath,'-buildTarget','Android','-runTests','-testPlatform','EditMode','-testResults',$EditXml,'-logFile',(Join-Path $OutputDir 'editmode.log'))
Assert-TestRun $EditXml 67 @('PreferencesTests','AndroidBuildTests','PresentationContentTests','QaIdentityTests','DeliveryTests','SaveServiceTests')
$PlayXml=Join-Path $OutputDir 'playmode.xml'
Invoke-Unity @('-batchmode','-projectPath',$ProjectPath,'-buildTarget','Android','-runTests','-testPlatform','PlayMode','-testResults',$PlayXml,'-logFile',(Join-Path $OutputDir 'playmode.log'))
Assert-TestRun $PlayXml 22 @('VerticalSliceFlowTests','LayoutNoticeTests','AudioManagerTests','MergeInteractionTests','RewardGrowthTests','SettingsScreenTests')
$ApkName='vertical-slice.apk';$BuildMethod='Mofumachi.Editor.AndroidBuild.BuildDevelopmentApk';$PackageId='com.mofumachi.merge'
if($Qa){$ApkName='vertical-slice-qa.apk';$BuildMethod='Mofumachi.Editor.AndroidBuild.BuildQaApk';$PackageId='com.mofumachi.merge.qa'}
$ApkPath=Join-Path $OutputDir $ApkName;$BuildLog=Join-Path $OutputDir 'android-build.log'
Invoke-Unity @('-batchmode','-nographics','-quit','-projectPath',$ProjectPath,'-buildTarget','Android','-executeMethod',$BuildMethod,'-mofumachiApkPath',$ApkPath,'-logFile',$BuildLog)
if(-not(Test-Path -LiteralPath $ApkPath) -or (Get-Item -LiteralPath $ApkPath).Length -le 0 -or -not(Select-String -LiteralPath $BuildLog -SimpleMatch 'MOFUMACHI_APK_SUCCESS' -Quiet)){throw "Fresh APK not produced: $BuildLog"}
$GitRevision='unknown (git unavailable)';$Git=Get-Command git -ErrorAction SilentlyContinue
if($null -ne $Git){$Revision=& $Git.Source -C $ProjectPath rev-parse HEAD 2>$null;if($LASTEXITCODE -eq 0 -and "$Revision" -match '^[a-f0-9]{40}$'){$GitRevision="$Revision"}}
$ProjectSettings=Get-Content (Join-Path $ProjectPath 'ProjectSettings\ProjectSettings.asset') -Raw
$VersionCode=[regex]::Match($ProjectSettings,'(?m)^\s+AndroidBundleVersionCode: (\d+)\s*$').Groups[1].Value
if(-not $VersionCode){throw 'Cannot read versionCode for build identity.'}
$Hash=(Get-FileHash -LiteralPath $ApkPath -Algorithm SHA256).Hash.ToLowerInvariant()
[ordered]@{gitRevision=$GitRevision;unity='6000.6.4f1';appId=$PackageId;versionCode=[int]$VersionCode;apk=$ApkName;sha256=$Hash;bytes=(Get-Item -LiteralPath $ApkPath).Length;builtAtUtc=[DateTime]::UtcNow.ToString('o');qa=[bool]$Qa} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDir 'build-info.json') -Encoding UTF8
Write-Host "APK: $ApkPath`nSHA256: $Hash"
