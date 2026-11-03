[CmdletBinding()]
param([string]$UnityEditorPath='',[switch]$Final,[switch]$Install,[string]$Serial='99RAY1BELH',[ValidateRange(5,600)][int]$MeasureSeconds=180)
$ErrorActionPreference='Stop'
$UxEditorOption=$UnityEditorPath
$UxOptions=@{Final=[bool]$Final;Install=[bool]$Install;Serial=$Serial;Seconds=$MeasureSeconds}
. (Join-Path $PSScriptRoot 'prepare-merge-reward-preview.ps1')
function Get-ReleaseUxArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    $A=@(Get-MergeRewardArguments $Repository $Directory $Revision $Fingerprint $Packed 'capture')
    $At=[Array]::IndexOf($A,'-testFilter')
    if($At -lt 0){throw 'Native UX focus requires an explicit test filter.'}
    $Cases=@('ReleaseUxNativeTests','StoryTypographyTests','MergeRewardCaptureTests.MergeRewardCandidateScreens',
        'DailyWorldTests.TodayClaimAndConversationNeverWriteOrRewardTheMainSave',
        'DailyWorldTests.TodayReadyAndCollectedKeepAllGoalsAndTheFlowerStateOnTheOpeningPage',
        'QualityPassMergeTests.ThirtyRoundedSquaresKeepRectangularInputAndReadableLevelBadges',
        'LayoutNoticeTests.RequiredJapaneseTextFitsAndHomeUsesActualHudValues')
    $A[$At+1]=($Cases | ForEach-Object {'Mofumachi.Tests.'+$_}) -join ';'
    return $A
}
if($MyInvocation.InvocationName -eq '.'){return}
if($UxOptions.Install -and -not $UxOptions.Final){throw 'Pixel installation requires -Final and the new complete QA evidence.'}
$Repo=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$Git=Find-SourceGit
$Editor=if($UxEditorOption){$UxEditorOption}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf)){throw 'Unity6000.6.4f1 is required.'}
if(Get-Process -Name Unity -ErrorAction SilentlyContinue){throw 'Close Unity Editor before this isolated run.'}
$null=Assert-MergeRewardVisualApproval $Repo
$Preserved=Move-PreviousCharacterInputs $Repo $Git
Assert-CleanProductSource $Repo $Git;Assert-NoahAuthoredMasterFixed $Repo;Assert-NoahAuthoredFrozenInputs $Repo
$Revision=Read-SourceRevision $Repo;$Fingerprint=Get-CharacterAuthoredSourceFingerprint $Repo
$Run=Join-Path $Repo ('Builds/Diagnostics/release-ux-focus-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $Run|Out-Null
$Record=[ordered]@{scope='release-ux-focus-only';gitRevision=$Revision;productFingerprint=$Fingerprint;packed_resource_sha256='';preserved_inputs=$Preserved;source_unchanged=$false;capture_verified=$false;image_count=0;artifact_directory='';artifacts=@();focus_verified=$false;full_qa_verified=$false;build_generated=$false;pixel_verified=$false;logo_applied=$false;error=''}
$Failure=$null;$Ready=$false
try{
    foreach($Step in @('prepare-noah','prepare-five')){Invoke-NoahAuthoredUnity $Editor (Get-CharactersDirectArguments $Repo $Run $Step) $Run}
    $Record.packed_resource_sha256=Get-PreparedCharactersDigest $Repo
    Invoke-NoahAuthoredUnity $Editor (Get-ReleaseUxArguments $Repo $Run $Revision $Fingerprint $Record.packed_resource_sha256) $Run
    Assert-TestRun (Join-Path $Run 'capture.xml') 8 @('ReleaseUxNativeTests','StoryTypographyTests','MergeRewardCaptureTests')
    $Proof=Assert-MergeRewardProof $Run $Revision $Fingerprint $Record.packed_resource_sha256
    Write-MergeRewardViewer $Run (Join-Path $Repo 'References/Merge/merge_visual_lock_reward.png') $Proof -ReleaseUx
    if((Get-PreparedCharactersDigest $Repo) -cne $Record.packed_resource_sha256){throw 'Prepared resources changed during the focused run.'}
    $Ready=$true
}catch{$Failure=$_;$Record.error=$_.Exception.Message}
finally{
    try{Remove-OwnedCharacterResources $Repo $Run;Remove-OwnedNoahAuthoredResources $Repo $Run}catch{if(-not $Failure){$Failure=$_};$Record.error+=' Cleanup: '+$_.Exception.Message}
    try{
        if((Read-SourceRevision $Repo) -cne $Revision -or (Get-CharacterAuthoredSourceFingerprint $Repo) -cne $Fingerprint){throw 'Source changed during the focused run.'}
        Assert-NoahAuthoredFrozenInputs $Repo;$Record.source_unchanged=$true
        if(-not $Failure -and $Ready){Confirm-MergeRewardCapture $Record $Run $Revision $Fingerprint $Record.packed_resource_sha256;$Record.focus_verified=$true}
    }catch{if(-not $Failure){$Failure=$_};$Record.error+=' '+$_.Exception.Message}
    $Record|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $Run 'release-ux-source-info.json') -Encoding UTF8
    Write-Host "Focused UX evidence preserved: $Run"
    if($env:OS -eq 'Windows_NT'){Start-Process -FilePath explorer.exe -ArgumentList ('"'+$Run+'"')}
}
if($Failure){throw $Failure}
if($UxOptions.Final){
    # The existing candidate route runs Merge focus, then each full native suite
    # once, and builds exactly one APK. It never adopts the logo color study.
    $Outputs=@(& (Join-Path $PSScriptRoot 'prepare-merge-reward-preview.ps1') -UnityEditorPath $Editor -Final)
    $FinalRun=[string]$Outputs[-1]
    $Full=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $FinalRun 'merge-reward-source-info.json')|ConvertFrom-Json
    if($Full.gitRevision -cne $Revision -or $Full.productFingerprint -cne $Fingerprint -or -not $Full.full_qa_verified -or -not $Full.build_generated){throw 'Full QA/APK differs from the focused UX candidate.'}
    Assert-TestRun (Join-Path $FinalRun 'PlayMode.xml') 240 @('ReleaseUxNativeTests','StoryTypographyTests','MergeRewardCaptureTests','VerticalSliceFlowTests')
    @{scope='release-ux-qa-parent';gitRevision=$Revision;productFingerprint=$Fingerprint;focus_directory=$Run;focus_record_sha256=(Get-FileHash -LiteralPath (Join-Path $Run 'release-ux-source-info.json') -Algorithm SHA256).Hash.ToLowerInvariant();pixel_verified=$false;logo_applied=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $FinalRun 'release-ux-parent.json') -Encoding UTF8
    if($UxOptions.Install){& (Join-Path $PSScriptRoot 'test-merge-reward-pixel.ps1') -BuildDirectory $FinalRun -UnityEditorPath $Editor -Serial $UxOptions.Serial -MeasureSeconds $UxOptions.Seconds}
    return $FinalRun
}
Write-Host 'UX focus passed. No full suites, APK, installation or data reset ran. Native font/receipt review and Pixel acceptance are separate.'
return $Run
