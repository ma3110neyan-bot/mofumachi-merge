[CmdletBinding()]
param([string]$UnityEditorPath='')
$ErrorActionPreference='Stop'
$FinishFocusedEditor=$UnityEditorPath
. (Join-Path $PSScriptRoot 'prepare-characters-pixel-preview.ps1')

function Get-CharacterFinishFocusedMethods {
    return @(
        'Mofumachi.Tests.TownHabitTests.NamesBubblesAndFacilitiesStayInsideSafeArea',
        'Mofumachi.Tests.TownLifeTests.NpcsVisitSeveralStreetStopsWithoutChangingProgress',
        'Mofumachi.Tests.TownMotionPresentationTests.StationaryDragAndLifecycleStopGait',
        'Mofumachi.Tests.TownRegionLifeTests.TwoResidentsMeetAndResumeInVisibleRegion',
        'Mofumachi.Tests.VisualLockPresentationTests.CloseResidentLabelsNeverCoverAnotherCharactersBody',
        'Mofumachi.Tests.TownHabitTests.NearbyResidentsStaySilentUntilAnExplicitTap',
        'Mofumachi.Tests.TownHabitTests.HeroPressReleaseRepeatsPreserveSpeechUntilDragging'
    )
}
function Get-CharacterFinishFocusedArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint) {
    if(-not $Repository -or -not $Directory -or $Revision -notmatch '^[a-f0-9]{40}$' -or $Fingerprint -notmatch '^[a-f0-9]{64}$'){throw 'Exact final-finish source identity required.'}
    return @('-batchmode','-projectPath',$Repository,'-buildTarget','Android','-runTests','-testPlatform','PlayMode','-testFilter',((Get-CharacterFinishFocusedMethods) -join ';'),'-testResults',(Join-Path $Directory 'finish-focused-playmode.xml'),'-logFile',(Join-Path $Directory 'finish-focused-playmode.log'),'-mofumachiSourceRevision',$Revision,'-mofumachiProductFingerprint',$Fingerprint)
}
function Assert-CharacterFinishFocusedResults([string]$Path) {
    [xml]$Xml=Get-Content -Raw -Encoding UTF8 -LiteralPath $Path
    $Actual=@($Xml.SelectNodes('//test-case') | ForEach-Object {$_.GetAttribute('fullname')})
    $Expected=@(Get-CharacterFinishFocusedMethods)
    if($Actual.Count -ne 7 -or @($Actual | Select-Object -Unique).Count -ne 7 -or @(Compare-Object $Expected $Actual).Count){throw 'Exactly five failed cases plus the two tap/repeat regressions are required.'}
    Assert-TestRun $Path 7 @()
}
if($MyInvocation.InvocationName -eq '.'){return}
$Repository=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$Git=Find-SourceGit
Assert-CleanProductSource $Repository $Git;Assert-NoahAuthoredMasterFixed $Repository;Assert-NoahAuthoredFrozenInputs $Repository
foreach($Relative in @('Assets/Mofumachi/Resources/Mofumachi/CharacterAuthored','Assets/Mofumachi/Resources/Mofumachi/NoahAuthoredTrial')){
    if((Test-Path -LiteralPath (Join-Path $Repository $Relative)) -or (Test-Path -LiteralPath (Join-Path $Repository ($Relative+'.meta')))){throw 'Existing prepared resources preserved; inspect their owning run first.'}
}
$Editor=if($FinishFocusedEditor){$FinishFocusedEditor}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf) -or -not(Test-Path -LiteralPath (Join-Path (Split-Path -Parent $Editor) 'Data/PlaybackEngines/AndroidPlayer'))){throw 'Unity6000.6.4f1 with Android support required.'}
$Revision=Read-SourceRevision $Repository;$Fingerprint=Get-CharacterAuthoredSourceFingerprint $Repository
$Run=Join-Path $Repository ('Builds/Diagnostics/characters-finish-focus-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $Run | Out-Null
$Record=[ordered]@{scope='five-failed-plus-two-tap-repeat-regressions';gitRevision=$Revision;productFingerprint=$Fingerprint;expected_methods=@(Get-CharacterFinishFocusedMethods);focused_native_verified=$false;full_qa_verified=$false;builds_generated=$false;pixel_verified=$false;formal_fixed=$false;adopted=$false;source_unchanged=$false;error=''}
$Failure=$null;$FirstZip=''
Write-Host "Character finish focused run: $Run"
try{
    Invoke-NoahAuthoredUnity $Editor (Get-CharacterFinishFocusedArguments $Repository $Run $Revision $Fingerprint) $Run
    Assert-CharacterFinishFocusedResults (Join-Path $Run 'finish-focused-playmode.xml');$Record.focused_native_verified=$true
}catch{$Failure=$_;$Record.error=$_.Exception.Message}
finally{
    try{
        if((Read-SourceRevision $Repository) -cne $Revision -or (Get-CharacterAuthoredSourceFingerprint $Repository) -cne $Fingerprint){throw 'Source changed during focused final-finish QA.'}
        Assert-NoahAuthoredMasterFixed $Repository;Assert-NoahAuthoredFrozenInputs $Repository;$Record.source_unchanged=$true
    }catch{if(-not $Failure){$Failure=$_};$Record.error+=' '+$_.Exception.Message}
    $Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Run 'source-info.json') -Encoding UTF8
    try{foreach($Zip in @(Write-CharactersReviewArchives $Run)){if(-not $FirstZip){$FirstZip=$Zip};Write-Host "Character finish focused ZIP: $Zip"}}
    catch{if(-not $Failure){$Failure=$_};$Record.error+=' Archive: '+$_.Exception.Message;$Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Run 'source-info.json') -Encoding UTF8}
    Write-Host 'Exactly seven PlayMode methods. No full suites, single-character captures, APK, install, uninstall or save reset.'
    if($FirstZip -and $env:OS -eq 'Windows_NT'){Start-Process -FilePath explorer.exe -ArgumentList ('/select,"'+$FirstZip+'"')}
}
if($Failure){throw $Failure}
Write-Host 'Focused methods passed. Current full QA, six-character comparison and physical Pixel acceptance remain pending.'
