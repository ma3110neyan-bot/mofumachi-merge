[CmdletBinding()]
param([string]$UnityEditor = "$env:ProgramFiles\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe", [switch]$Qa,[switch]$TownQa)
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
        if(@($Results.SelectNodes('//test-suite') | Where-Object { $_.GetAttribute('name') -eq $Suite -and $_.GetAttribute('result') -eq 'Passed' -and $_.SelectNodes('.//test-case').Count -gt 0 }).Count -eq 0) { throw "Missing passed suite: $Suite" }
    }
    Write-Host "Tests passed: $($Run.passed)/$($Run.total)"
}
function Read-SourceRevision([string]$RepositoryPath) {
    $GitDirectory=Join-Path $RepositoryPath '.git'
    if(Test-Path -LiteralPath $GitDirectory -PathType Leaf) {
        $GitLink=(Get-Content -LiteralPath $GitDirectory -Raw).Trim()
        if($GitLink -notmatch '^gitdir: (.+)$'){throw 'Invalid .git link; source SHA is required.'}
        $LinkedDirectory=$Matches[1]
        if(-not [IO.Path]::IsPathRooted($LinkedDirectory)){$LinkedDirectory=Join-Path $RepositoryPath $LinkedDirectory}
        $GitDirectory=[IO.Path]::GetFullPath($LinkedDirectory)
    }
    $HeadPath=Join-Path $GitDirectory 'HEAD'
    if(-not(Test-Path -LiteralPath $HeadPath -PathType Leaf)){throw 'Cannot read source HEAD; source SHA is required.'}
    $SourceHead=(Get-Content -LiteralPath $HeadPath -Raw).Trim()
    if($SourceHead -match '^[a-fA-F0-9]{40}$'){return $SourceHead.ToLowerInvariant()}
    if($SourceHead -notmatch '^ref: (refs/[A-Za-z0-9_./-]+)$'){throw 'Invalid source HEAD.'}
    $ReferenceName=$Matches[1]
    if($ReferenceName.Contains('..')){throw 'Invalid source reference.'}
    $CommonDirectory=$GitDirectory;$CommonPath=Join-Path $GitDirectory 'commondir'
    if(Test-Path -LiteralPath $CommonPath -PathType Leaf){$CommonDirectory=[IO.Path]::GetFullPath((Join-Path $GitDirectory (Get-Content -LiteralPath $CommonPath -Raw).Trim()))}
    foreach($Directory in @($GitDirectory,$CommonDirectory)) {
        $RefPath=Join-Path $Directory $ReferenceName
        if(Test-Path -LiteralPath $RefPath -PathType Leaf) {
            $SourceSha=(Get-Content -LiteralPath $RefPath -Raw).Trim()
            if($SourceSha -match '^[a-fA-F0-9]{40}$'){return $SourceSha.ToLowerInvariant()}
            throw 'Invalid source SHA.'
        }
        $PackedPath=Join-Path $Directory 'packed-refs'
        if(Test-Path -LiteralPath $PackedPath -PathType Leaf) {
            $PackedPattern='^([a-fA-F0-9]{40}) '+[regex]::Escape($ReferenceName)+'$'
            foreach($PackedLine in Get-Content -LiteralPath $PackedPath) {if($PackedLine -match $PackedPattern){return $Matches[1].ToLowerInvariant()}}
        }
    }
    throw 'Source SHA not found; do not build an unidentified APK.'
}
function Assert-SourceUnchanged([string]$RepositoryPath,[string]$ExpectedRevision) {
    if((Read-SourceRevision $RepositoryPath) -ne $ExpectedRevision){throw 'Source commit changed during acceptance; repeat the same-run tests and build.'}
}
function Invoke-Unity([string[]]$UnityArguments) {
    $ArgumentText=($UnityArguments | ForEach-Object { '"'+$_+'"' }) -join ' '
    $Process=Start-Process -FilePath $UnityEditor -ArgumentList $ArgumentText -Wait -PassThru
    if($Process.ExitCode -ne 0){throw "Unity exit $($Process.ExitCode). Inspect $OutputDir"}
}
# Dot sourcing exposes the same gates to script tests without starting Unity.
if($MyInvocation.InvocationName -eq '.'){return}
if($Qa -and $TownQa){throw 'Select -Qa or -TownQa, not both.'}
$ProjectPath=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if($ProjectPath -match '[^\x00-\x7F]'){throw 'Move the entire repository to an ASCII path before Android builds.'}
if(-not(Test-Path -LiteralPath $UnityEditor -PathType Leaf)){throw 'Pass -UnityEditor with Unity 6000.6.4f1 Unity.exe.'}
$UnityEditor=(Resolve-Path -LiteralPath $UnityEditor).Path
if((Get-Content (Join-Path $ProjectPath 'ProjectSettings\ProjectVersion.txt') -Raw) -notmatch 'm_EditorVersion: 6000\.6\.4f1\s'){throw 'Wrong project Unity version.'}
$GitRevision=Read-SourceRevision $ProjectPath
$RunName=(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8)
$OutputDir=Join-Path $ProjectPath "Builds\Android\$RunName"
New-Item -ItemType Directory -Path $OutputDir | Out-Null
$EditXml=Join-Path $OutputDir 'editmode.xml'
Invoke-Unity @('-batchmode','-nographics','-projectPath',$ProjectPath,'-buildTarget','Android','-runTests','-testPlatform','EditMode','-testResults',$EditXml,'-logFile',(Join-Path $OutputDir 'editmode.log'))
Assert-TestRun $EditXml 141 @('PreferencesTests','AndroidBuildTests','PresentationContentTests','QaIdentityTests','DeliveryTests','SaveServiceTests','CharacterPreferenceTests','QuestCatalogTests','ItemRemovalTests','TownMovementTests','ChapterProgressTests','TownFrameClockTests','TownPlaceCatalogTests','TownViewportTests','TownRegionCatalogTests','TownRegionJourneyTests','TownRegionContentTests')
$PlayXml=Join-Path $OutputDir 'playmode.xml'
Invoke-Unity @('-batchmode','-projectPath',$ProjectPath,'-buildTarget','Android','-runTests','-testPlatform','PlayMode','-testResults',$PlayXml,'-mofumachiScreenshots',(Join-Path $OutputDir 'screenshots'),'-logFile',(Join-Path $OutputDir 'playmode.log'))
Assert-TestRun $PlayXml 161 @('VerticalSliceFlowTests','LayoutNoticeTests','AudioManagerTests','MergeInteractionTests','RewardGrowthTests','SettingsScreenTests','PopPresentationTests','CharacterSelectionTests','MultiQuestPresentationTests','TownLifeTests','StoryTypographyTests','TownMotionPresentationTests','ChapterFlowTests','MergePolishTests','MergeProgressionUiTests','MergeGuidanceTests','ProductionFrameUiTests','TownViewportPresentationTests','TownHabitTests','TownRegionFlowTests','TownRegionLifeTests','QualityPassTravelTests','QualityPassStoryGrowthTests','QualityPassMergeTests','ExperienceWalkingTests','ExperienceMergeTests','ExperienceMusicTests','ExperienceGrowthTests','ExperienceCaptureTests','DailyWorldTests')
$ApkName='vertical-slice.apk';$BuildMethod='Mofumachi.Editor.AndroidBuild.BuildDevelopmentApk';$PackageId='com.mofumachi.merge'
if($Qa){$ApkName='vertical-slice-qa.apk';$BuildMethod='Mofumachi.Editor.AndroidBuild.BuildQaApk';$PackageId='com.mofumachi.merge.qa'}
if($TownQa){$ApkName='town-life-qa.apk';$BuildMethod='Mofumachi.Editor.AndroidBuild.BuildTownQaApk';$PackageId='com.mofumachi.merge.qa.townlife'}
$ApkPath=Join-Path $OutputDir $ApkName;$BuildLog=Join-Path $OutputDir 'android-build.log'
Invoke-Unity @('-batchmode','-nographics','-quit','-projectPath',$ProjectPath,'-buildTarget','Android','-executeMethod',$BuildMethod,'-mofumachiApkPath',$ApkPath,'-logFile',$BuildLog)
if(-not(Test-Path -LiteralPath $ApkPath) -or (Get-Item -LiteralPath $ApkPath).Length -le 0 -or -not(Select-String -LiteralPath $BuildLog -SimpleMatch 'MOFUMACHI_APK_SUCCESS' -Quiet)){throw "Fresh APK not produced: $BuildLog"}
Assert-SourceUnchanged $ProjectPath $GitRevision
$ProjectSettings=Get-Content (Join-Path $ProjectPath 'ProjectSettings\ProjectSettings.asset') -Raw
$VersionCode=[regex]::Match($ProjectSettings,'(?m)^\s+AndroidBundleVersionCode: (\d+)\s*$').Groups[1].Value
if(-not $VersionCode){throw 'Cannot read versionCode for build identity.'}
$Hash=(Get-FileHash -LiteralPath $ApkPath -Algorithm SHA256).Hash.ToLowerInvariant()
[ordered]@{gitRevision=$GitRevision;revisionSource='git-metadata';unity='6000.6.4f1';appId=$PackageId;versionCode=[int]$VersionCode;apk=$ApkName;sha256=$Hash;bytes=(Get-Item -LiteralPath $ApkPath).Length;builtAtUtc=[DateTime]::UtcNow.ToString('o');qa=[bool]($Qa -or $TownQa);townQa=[bool]$TownQa} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDir 'build-info.json') -Encoding UTF8
Write-Host "APK: $ApkPath`nSHA256: $Hash"
