[CmdletBinding()]
param([string]$RepairDirectory,[string]$UnityEditorPath='',[switch]$Install,[string]$Serial='99RAY1BELH',[ValidateRange(5,600)][int]$MeasureSeconds=180)
$ErrorActionPreference='Stop'
$ReviewedOptions=@{Repair=$RepairDirectory;Editor=$UnityEditorPath;Install=[bool]$Install;Serial=$Serial;Seconds=$MeasureSeconds}
. (Join-Path $PSScriptRoot 'merge-reward-reviewed-evidence.ps1')
if(-not $ReviewedOptions.Repair){throw 'Specify the exact passed eight-method run using -RepairDirectory.'}
$ReviewedRepo=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$ReviewedGit=Find-SourceGit
$ReviewedRepair=(Resolve-Path -LiteralPath $ReviewedOptions.Repair).Path
$ReviewedQa=Read-MergeReviewedQa $ReviewedRepair $ReviewedRepo
$ReviewedApproval=Assert-MergeRewardVisualApproval $ReviewedRepo
$ReviewedPreserved=Move-PreviousCharacterInputs $ReviewedRepo $ReviewedGit
Assert-CleanProductSource $ReviewedRepo $ReviewedGit;Assert-CharactersVisualLock $ReviewedRepo;Assert-NoahAuthoredMasterFixed $ReviewedRepo;Assert-NoahAuthoredFrozenInputs $ReviewedRepo
$ReviewedRevision=Read-SourceRevision $ReviewedRepo
$ReviewedProduct=Assert-MergeReviewedProduct $ReviewedRepo $ReviewedGit $ReviewedRevision
$ReviewedFingerprint=Get-CharacterAuthoredSourceFingerprint $ReviewedRepo
$ReviewedEditor=if($ReviewedOptions.Editor){$ReviewedOptions.Editor}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $ReviewedEditor -PathType Leaf) -or -not(Test-Path -LiteralPath (Join-Path (Split-Path -Parent $ReviewedEditor) 'Data/PlaybackEngines/AndroidPlayer')) -or (Get-Process -Name Unity -ErrorAction SilentlyContinue)){throw 'Unity6000.6.4f1 Android support required, with the Editor closed.'}
Write-Host 'Reviewed native coverage: focus53/53, Edit144/144, original Play252/260 plus repaired8/8. No new full-suite run or rewritten XML.'
Write-Host 'Preparing existing resources, checking exactly six resource inputs, then building with those same bytes. No full suites or visual recapture.'
$ReviewedRun=Join-Path $ReviewedRepo ('Builds/Diagnostics/merge-reward-reviewed-pixel-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory -Path $ReviewedRun|Out-Null
foreach($Row in @($ReviewedQa.files)){
    $Target=Join-Path $ReviewedRun ('qa-evidence/'+$Row.path);New-Item -ItemType Directory -Path (Split-Path -Parent $Target) -Force|Out-Null
    Copy-Item -LiteralPath (Join-Path $ReviewedRepair $Row.path) -Destination $Target
}
$ReviewedRecord=[ordered]@{scope='merge-reward-reviewed-qa-build';gitRevision=$ReviewedRevision;productFingerprint=$ReviewedFingerprint;packed_resource_sha256='';approved_design_revision=$ReviewedApproval.source_revision;ui_policy='fix-only';qa_basis='original-full-plus-test-only-focused-repair';qa_coverage_verified=$true;qa_evidence=$ReviewedQa;product_tree_sha256=$ReviewedProduct;preserved_inputs=$ReviewedPreserved;resource_check_verified=$false;resource_check_artifacts=@();full_qa_verified=$false;full_playmode_rerun=$false;build_generated=$false;pixel_verified=$false;source_unchanged=$false;error=''}
$ReviewedFailure=$null;Write-Host ('Exact reviewed Merge run: '+$ReviewedRun)
try{
    foreach($Step in @('prepare-noah','prepare-five')){Invoke-NoahAuthoredUnity $ReviewedEditor (Get-CharactersDirectArguments $ReviewedRepo $ReviewedRun $Step) $ReviewedRun}
    $ReviewedRecord.packed_resource_sha256=Get-PreparedCharactersDigest $ReviewedRepo
    Invoke-NoahAuthoredUnity $ReviewedEditor (Get-MergeReviewedResourceArguments $ReviewedRepo $ReviewedRun $ReviewedRevision $ReviewedFingerprint $ReviewedRecord.packed_resource_sha256) $ReviewedRun
    $ReviewedRecord.resource_check_artifacts=@(Assert-MergeReviewedResources $ReviewedRun $ReviewedRevision $ReviewedFingerprint $ReviewedRecord.packed_resource_sha256);$ReviewedRecord.resource_check_verified=$true
    if((Get-PreparedCharactersDigest $ReviewedRepo) -cne $ReviewedRecord.packed_resource_sha256){throw 'Prepared resource bytes changed after native checking.'}
    @{gitRevision=$ReviewedRevision;productFingerprint=$ReviewedFingerprint;packed_resource_sha256=$ReviewedRecord.packed_resource_sha256}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $ReviewedRun 'direct-context.json') -Encoding UTF8
    Invoke-NoahAuthoredUnity $ReviewedEditor (Get-CharactersDirectArguments $ReviewedRepo $ReviewedRun 'build') $ReviewedRun
    if((Get-PreparedCharactersDigest $ReviewedRepo) -cne $ReviewedRecord.packed_resource_sha256){throw 'Native-checked resource bytes changed during APK generation.'}
    $null=Assert-MergeRewardApk $ReviewedRun $ReviewedRevision $ReviewedFingerprint $ReviewedRecord.packed_resource_sha256;$ReviewedRecord.build_generated=$true
}catch{$ReviewedFailure=$_;$ReviewedRecord.error=$_.Exception.Message}
finally{
    try{
        Remove-OwnedCharacterResources $ReviewedRepo $ReviewedRun;Remove-OwnedNoahAuthoredResources $ReviewedRepo $ReviewedRun
        if((Read-SourceRevision $ReviewedRepo) -cne $ReviewedRevision -or (Get-CharacterAuthoredSourceFingerprint $ReviewedRepo) -cne $ReviewedFingerprint -or (Assert-MergeReviewedProduct $ReviewedRepo $ReviewedGit $ReviewedRevision) -cne $ReviewedProduct){throw 'Reviewed source changed during APK generation.'}
        $null=Read-MergeReviewedQa (Join-Path $ReviewedRun 'qa-evidence') $ReviewedRepo
        $null=Assert-MergeRewardVisualApproval $ReviewedRepo;Assert-CharactersVisualLock $ReviewedRepo;Assert-NoahAuthoredMasterFixed $ReviewedRepo;Assert-NoahAuthoredFrozenInputs $ReviewedRepo;$ReviewedRecord.source_unchanged=$true
    }catch{if(-not $ReviewedFailure){$ReviewedFailure=$_};$ReviewedRecord.error+=' Cleanup/source: '+$_.Exception.Message}
    $ReviewedRecord|ConvertTo-Json -Depth 14|Set-Content -LiteralPath (Join-Path $ReviewedRun 'merge-reward-source-info.json') -Encoding UTF8
    try{$Files=@(Get-ChildItem -LiteralPath $ReviewedRun -Force|Where-Object {$_.Extension -notin @('.apk','.zip')});Compress-Archive -LiteralPath @($Files.FullName) -DestinationPath (Join-Path $ReviewedRun 'build-review-01.zip');Write-Host ('Build evidence ZIP: '+(Join-Path $ReviewedRun 'build-review-01.zip'))}catch{Write-Host ('Archive failed; original build evidence preserved: '+$ReviewedRun)}
}
if($ReviewedFailure){throw $ReviewedFailure}
$null=Read-MergeReviewedBuild $ReviewedRun $ReviewedRepo $ReviewedApproval
Write-Host ('Reviewed Merge QA APK run: '+$ReviewedRun)
if($ReviewedOptions.Install){& (Join-Path $PSScriptRoot 'test-merge-reward-pixel.ps1') -BuildDirectory $ReviewedRun -UnityEditorPath $ReviewedEditor -Serial $ReviewedOptions.Serial -MeasureSeconds $ReviewedOptions.Seconds}
Write-Host 'Reviewed Merge APK prepared. Physical Pixel visual/input/performance acceptance remains separate. No uninstall or save reset.'
return $ReviewedRun
