[CmdletBinding()]
param([string]$UnityEditorPath='')
$ErrorActionPreference='Stop'
$FocusedEditor=$UnityEditorPath
. (Join-Path $PSScriptRoot 'prepare-characters-pixel-preview.ps1')

function Get-CharactersFocusedMethods {
    return @(
        'Mofumachi.Tests.ExperienceCaptureTests.FountainShowRunsFromRealUpdatesWithinSixtySeconds',
        'Mofumachi.Tests.ExperienceCaptureTests.PortraitScreensRenderFromIsolatedStateWithoutWritingProgress',
        'Mofumachi.Tests.LayoutNoticeTests.PopulatedPortraitScreensKeepCharactersAndFeedbackAboveFixedActions',
        'Mofumachi.Tests.Noah25DTrialPresentationTests.OnlyTownNoahOptsInAndTheCausalSwitchRestoresTheExactLegacyFrame',
        'Mofumachi.Tests.PopPresentationTests.FullBodyActorsFollowTheWorldMapOnShortAndTallScreens',
        'Mofumachi.Tests.ExperienceCaptureTests.PortraitAcceptsPrewarmedCullingButRejectsVisibleMissingGeometry'
    )
}
function Get-CharactersFocusedArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint) {
    if(-not $Repository -or -not $Directory -or $Revision -notmatch '^[a-f0-9]{40}$' -or $Fingerprint -notmatch '^[a-f0-9]{64}$'){throw 'Exact focused source identity required.'}
    return @('-batchmode','-projectPath',$Repository,'-buildTarget','Android','-runTests','-testPlatform','PlayMode','-testFilter',((Get-CharactersFocusedMethods) -join ';'),'-testResults',(Join-Path $Directory 'focused-playmode.xml'),'-logFile',(Join-Path $Directory 'focused-playmode.log'),'-mofumachiScreenshots',(Join-Path $Directory 'full-screenshots'),'-mofumachiSourceRevision',$Revision,'-mofumachiProductFingerprint',$Fingerprint)
}
function Assert-CharactersFocusedResults([string]$Path) {
    [xml]$Xml=Get-Content -Raw -LiteralPath $Path
    $Actual=@($Xml.SelectNodes('//test-case') | ForEach-Object {$_.GetAttribute('fullname')})
    $Expected=@(Get-CharactersFocusedMethods)
    if($Actual.Count -ne $Expected.Count -or @($Actual | Select-Object -Unique).Count -ne $Expected.Count -or @(Compare-Object $Expected $Actual).Count){throw 'Focused QA must run exactly the five failed cases and the culling regression.'}
    Assert-TestRun $Path 6 @()
}
if($MyInvocation.InvocationName -eq '.'){return}
$Repository=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$Git=Find-SourceGit
Assert-CleanProductSource $Repository $Git;Assert-NoahAuthoredMasterFixed $Repository;Assert-NoahAuthoredFrozenInputs $Repository
foreach($Relative in @('Assets/Mofumachi/Resources/Mofumachi/CharacterAuthored','Assets/Mofumachi/Resources/Mofumachi/NoahAuthoredTrial')){
    if((Test-Path -LiteralPath (Join-Path $Repository $Relative)) -or (Test-Path -LiteralPath (Join-Path $Repository ($Relative+'.meta')))){throw 'Preserve existing prepared resources; inspect their owning run first.'}
}
$Editor=if($FocusedEditor){$FocusedEditor}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf) -or -not(Test-Path -LiteralPath (Join-Path (Split-Path -Parent $Editor) 'Data/PlaybackEngines/AndroidPlayer'))){throw 'Pinned Unity6000.6.4f1 with Android support required.'}
$Revision=Read-SourceRevision $Repository;$Fingerprint=Get-CharacterAuthoredSourceFingerprint $Repository
$Run=Join-Path $Repository ('Builds/Diagnostics/characters-qa-focus-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $Run | Out-Null
$Record=[ordered]@{scope='five-failed-cases-plus-culling-regression';gitRevision=$Revision;productFingerprint=$Fingerprint;expected_methods=@(Get-CharactersFocusedMethods);focused_native_verified=$false;full_qa_verified=$false;builds_generated=$false;pixel_verified=$false;adopted=$false;source_unchanged=$false;error=''};$Failure=$null
Write-Host "Focused native run: $Run"
try{
    Invoke-NoahAuthoredUnity $Editor (Get-CharactersFocusedArguments $Repository $Run $Revision $Fingerprint) $Run
    Assert-CharactersFocusedResults (Join-Path $Run 'focused-playmode.xml');$Record.focused_native_verified=$true
}catch{$Failure=$_;$Record.error=$_.Exception.Message}
finally{
    try{
        if((Read-SourceRevision $Repository) -cne $Revision -or (Get-CharacterAuthoredSourceFingerprint $Repository) -cne $Fingerprint){throw 'Source changed during focused QA.'}
        Assert-NoahAuthoredMasterFixed $Repository;Assert-NoahAuthoredFrozenInputs $Repository;$Record.source_unchanged=$true
    }catch{if(-not $Failure){$Failure=$_};$Record.error+=' '+$_.Exception.Message}
    $Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Run 'source-info.json') -Encoding UTF8
    try{foreach($Zip in @(Write-CharactersReviewArchives $Run)){Write-Host "Focused review ZIP: $Zip"}}
    catch{if(-not $Failure){$Failure=$_};$Record.error+=' Archive: '+$_.Exception.Message;$Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Run 'source-info.json') -Encoding UTF8}
    Write-Host "Original images remain in: $Run\full-screenshots"
    Write-Host 'Only six PlayMode methods. No full suites, single-character reruns, APK, install, uninstall or save reset.'
}
if($Failure){throw $Failure}
Write-Host 'Focused native QA passed. Full QA, six-character rendering, APK and Pixel remain pending.'
