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
    foreach($Gate in @(@{Name='DailyWorldTests';Count=10},@{Name='MajorPolishTests';Count=6},@{Name='SeamlessFieldTests';Count=7})) {
        if($Gate.Name -in $RequiredSuites) {
            $Suite=$Results.SelectNodes('//test-suite') | Where-Object {$_.GetAttribute('name') -eq $Gate.Name} | Select-Object -First 1
            if($Suite.SelectNodes('.//test-case').Count -ne $Gate.Count){throw "Incomplete suite: $($Gate.Name), expected $($Gate.Count) cases."}
        }
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
function Assert-NativeCaptures([string]$Directory) {
    $Images=@(Get-ChildItem -LiteralPath $Directory -Filter '*.png' -File -ErrorAction SilentlyContinue)
    if($Images.Count -lt 67){throw "Native captures incomplete: $($Images.Count)/67. Inspect the same-run PlayMode log."}
    foreach($Name in @('15-wide-park-left','15-wide-park-right','16-residents-passing-00','17-finger-follow-00','17-finger-released','18-continuous-walk-north','18-continuous-walk-central','18-continuous-walk-south','19-book-new-in-town','19-book-discovery-first-page','20-quest-cabin','20-quest-door-opening','20-quest-inside','21-shop-cabin','21-shop-door-opening','21-shop-inside','22-title-candidates','22-merge-candidates','23-title-letter','23-title-garden','23-merge-picnic','23-merge-ribbon')) {
        if(-not(Test-Path -LiteralPath (Join-Path $Directory ($Name+'.png')) -PathType Leaf)){throw "Missing native capture: $Name"}
    }
    foreach($Image in $Images) {
        $Stream=[IO.File]::OpenRead($Image.FullName)
        try {$Header=New-Object byte[] 24;$Read=$Stream.Read($Header,0,24)} finally {$Stream.Dispose()}
        if($Read -ne 24 -or $Image.Length -le 24 -or ($Header[0..7] -join ',') -ne '137,80,78,71,13,10,26,10' -or [Text.Encoding]::ASCII.GetString($Header,12,4) -ne 'IHDR'){throw "Invalid PNG header: $($Image.Name)"}
        $Width=([int64]$Header[16]*16777216)+([int64]$Header[17]*65536)+([int64]$Header[18]*256)+$Header[19]
        $Height=([int64]$Header[20]*16777216)+([int64]$Header[21]*65536)+([int64]$Header[22]*256)+$Header[23]
        if($Width -ne 720 -or $Height -ne 1280){throw "Wrong native capture size: $($Image.Name) ($Width x $Height)"}
    }
    Write-Host "Native Unity images: $($Images.Count), 720x1280 (visual review still required)."
}
function Assert-SourceUnchanged([string]$RepositoryPath,[string]$ExpectedRevision) {
    if((Read-SourceRevision $RepositoryPath) -ne $ExpectedRevision){throw 'Source commit changed during acceptance; repeat the same-run tests and build.'}
}
function Find-SourceGit {
    foreach($Name in @('git.exe','git')) {
        $Command=Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if($Command){return $Command.Source}
    }
    if($env:LOCALAPPDATA) {
        $Bundled=Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA 'GitHubDesktop/app-*/resources/app/git/cmd/git.exe') -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if($Bundled){return $Bundled.FullName}
    }
    throw 'Git was not found, including GitHub Desktop bundled Git. Cannot verify clean product inputs.'
}
function Source-InputPaths {
    # Unity rewrites local graphics/build-profile caches. Product inputs must be
    # committed; cache deviations are recorded separately instead of discarded.
    return @('Assets/Mofumachi','Assets/Mofumachi.meta','Assets/Scenes','Assets/Scenes.meta','Packages','ProjectSettings/ProjectVersion.txt','scripts')
}
function Assert-CleanProductSource([string]$RepositoryPath,[string]$GitPath) {
    $Changes=@(& $GitPath -C $RepositoryPath status --porcelain=v1 --untracked-files=all -- @(Source-InputPaths))
    if($LASTEXITCODE -ne 0){throw 'Cannot inspect product source status.'}
    if($Changes.Count -gt 0){throw ('Product inputs have uncommitted changes. Commit the candidate before same-run acceptance; no files were changed by this check.'+"`n"+($Changes -join "`n"))}
}
function Get-ProductFingerprint([string]$RepositoryPath) {
    $Entries=@(foreach($Relative in Source-InputPaths) {
        $Path=Join-Path $RepositoryPath $Relative
        if(-not(Test-Path -LiteralPath $Path)){continue}
        $Files=if(Test-Path -LiteralPath $Path -PathType Leaf){Get-Item -LiteralPath $Path -Force}else{Get-ChildItem -LiteralPath $Path -File -Recurse -Force}
        foreach($File in $Files){$Name=$File.FullName.Substring($RepositoryPath.TrimEnd('/','\').Length+1).Replace('\','/');$Name+':'+(Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash}
    })
    $Hasher=[Security.Cryptography.SHA256]::Create()
    try {return ([BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes((($Entries | Sort-Object) -join "`n"))))).Replace('-','').ToLowerInvariant()} finally {$Hasher.Dispose()}
}
function Assert-ProductUnchanged([string]$RepositoryPath,[string]$ExpectedRevision,[string]$ExpectedFingerprint) {
    Assert-SourceUnchanged $RepositoryPath $ExpectedRevision
    if((Get-ProductFingerprint $RepositoryPath) -ne $ExpectedFingerprint){throw 'Product inputs changed during acceptance; repeat tests, captures and APK together.'}
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
$SourceGit=Find-SourceGit
Assert-CleanProductSource $ProjectPath $SourceGit
$ProductFingerprint=Get-ProductFingerprint $ProjectPath
$ConfigurationChanges=@(& $SourceGit -C $ProjectPath diff --name-only HEAD -- Assets/Settings ProjectSettings)
if($LASTEXITCODE -ne 0){throw 'Cannot record local Unity configuration deviations.'}
$RunName=(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8)
$OutputDir=Join-Path $ProjectPath "Builds\Android\$RunName"
New-Item -ItemType Directory -Path $OutputDir | Out-Null
$EditXml=Join-Path $OutputDir 'editmode.xml'
Write-Host '[1/4] Running all EditMode tests...'
Invoke-Unity @('-batchmode','-nographics','-projectPath',$ProjectPath,'-buildTarget','Android','-runTests','-testPlatform','EditMode','-testResults',$EditXml,'-logFile',(Join-Path $OutputDir 'editmode.log'))
Assert-TestRun $EditXml 142 @('PreferencesTests','AndroidBuildTests','PresentationContentTests','QaIdentityTests','DeliveryTests','SaveServiceTests','CharacterPreferenceTests','QuestCatalogTests','ItemRemovalTests','TownMovementTests','ChapterProgressTests','TownFrameClockTests','TownPlaceCatalogTests','TownViewportTests','TownRegionCatalogTests','TownRegionJourneyTests','TownRegionContentTests')
Assert-ProductUnchanged $ProjectPath $GitRevision $ProductFingerprint
$PlayXml=Join-Path $OutputDir 'playmode.xml'
Write-Host '[2/4] Running all PlayMode tests and capturing native Unity images...'
Invoke-Unity @('-batchmode','-projectPath',$ProjectPath,'-buildTarget','Android','-runTests','-testPlatform','PlayMode','-testResults',$PlayXml,'-mofumachiScreenshots',(Join-Path $OutputDir 'screenshots'),'-logFile',(Join-Path $OutputDir 'playmode.log'))
Assert-TestRun $PlayXml 174 @('VerticalSliceFlowTests','LayoutNoticeTests','AudioManagerTests','MergeInteractionTests','RewardGrowthTests','SettingsScreenTests','PopPresentationTests','CharacterSelectionTests','MultiQuestPresentationTests','TownLifeTests','StoryTypographyTests','TownMotionPresentationTests','ChapterFlowTests','MergePolishTests','MergeProgressionUiTests','MergeGuidanceTests','ProductionFrameUiTests','TownViewportPresentationTests','TownHabitTests','TownRegionFlowTests','TownRegionLifeTests','QualityPassTravelTests','QualityPassStoryGrowthTests','QualityPassMergeTests','ExperienceWalkingTests','ExperienceMergeTests','ExperienceMusicTests','ExperienceGrowthTests','ExperienceCaptureTests','DailyWorldTests','MajorPolishTests','SeamlessFieldTests')
$CaptureDir=Join-Path $OutputDir 'screenshots'
Assert-ProductUnchanged $ProjectPath $GitRevision $ProductFingerprint
Assert-NativeCaptures $CaptureDir
$CaptureEntries=@(Get-ChildItem -LiteralPath $CaptureDir -Filter '*.png' -File | Sort-Object Name | ForEach-Object {[ordered]@{file=$_.Name;bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
[ordered]@{gitRevision=$GitRevision;productFingerprint=$ProductFingerprint;width=720;height=1280;count=$CaptureEntries.Count;scope='Unity fixture renders; not Pixel captures';images=$CaptureEntries} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDir 'capture-manifest.json') -Encoding UTF8
$ApkName='vertical-slice.apk';$BuildMethod='Mofumachi.Editor.AndroidBuild.BuildDevelopmentApk';$PackageId='com.mofumachi.merge'
if($Qa){$ApkName='vertical-slice-qa.apk';$BuildMethod='Mofumachi.Editor.AndroidBuild.BuildQaApk';$PackageId='com.mofumachi.merge.qa'}
if($TownQa){$ApkName='town-life-qa.apk';$BuildMethod='Mofumachi.Editor.AndroidBuild.BuildTownQaApk';$PackageId='com.mofumachi.merge.qa.townlife'}
$ApkPath=Join-Path $OutputDir $ApkName;$BuildLog=Join-Path $OutputDir 'android-build.log'
Write-Host '[3/4] Building the QA APK from this same source revision...'
Invoke-Unity @('-batchmode','-nographics','-quit','-projectPath',$ProjectPath,'-buildTarget','Android','-executeMethod',$BuildMethod,'-mofumachiApkPath',$ApkPath,'-mofumachiRevision',$GitRevision,'-logFile',$BuildLog)
if(-not(Test-Path -LiteralPath $ApkPath) -or (Get-Item -LiteralPath $ApkPath).Length -le 0 -or -not(Select-String -LiteralPath $BuildLog -SimpleMatch 'MOFUMACHI_APK_SUCCESS' -Quiet)){throw "Fresh APK not produced: $BuildLog"}
Assert-ProductUnchanged $ProjectPath $GitRevision $ProductFingerprint
$ProjectSettings=Get-Content (Join-Path $ProjectPath 'ProjectSettings\ProjectSettings.asset') -Raw
$VersionCode=[regex]::Match($ProjectSettings,'(?m)^\s+AndroidBundleVersionCode: (\d+)\s*$').Groups[1].Value
if(-not $VersionCode){throw 'Cannot read versionCode for build identity.'}
$Hash=(Get-FileHash -LiteralPath $ApkPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host '[4/4] Recording APK identity and SHA256...'
[ordered]@{gitRevision=$GitRevision;revisionSource='git-metadata + clean product inputs';productFingerprint=$ProductFingerprint;localUnityConfigurationChanges=$ConfigurationChanges;nativeCaptureCount=$CaptureEntries.Count;unity='6000.6.4f1';appId=$PackageId;versionCode=[int]$VersionCode;apk=$ApkName;sha256=$Hash;bytes=(Get-Item -LiteralPath $ApkPath).Length;builtAtUtc=[DateTime]::UtcNow.ToString('o');qa=[bool]($Qa -or $TownQa);townQa=[bool]$TownQa} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDir 'build-info.json') -Encoding UTF8
Write-Host "APK: $ApkPath`nSHA256: $Hash"
