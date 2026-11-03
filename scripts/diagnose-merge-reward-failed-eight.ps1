[CmdletBinding()]
param([string]$OriginalDirectory='',[string]$UnityEditorPath='')
$ErrorActionPreference='Stop'
$Repair8Options=@{Original=$OriginalDirectory;Editor=$UnityEditorPath}
. (Join-Path $PSScriptRoot 'prepare-merge-reward-preview.ps1')
function Get-MergeFailedEightMethods {
    return @(
        'Mofumachi.Tests.DailyWorldTests.FlatCellsHaveStableIndexesAndRealNonOverlappingHitRects',
        'Mofumachi.Tests.MajorPolishTests.FlatBoardKeepsAlignedEdgesAndAllThirtyCellsActuallyReceiveRaycasts',
        'Mofumachi.Tests.ExperienceCaptureTests.NoahAuthoredPartsPreviewPreservesState',
        'Mofumachi.Tests.ExperienceCaptureTests.MomoAuthoredPartsPreserveIdentityInputAndState',
        'Mofumachi.Tests.ExperienceCaptureTests.LuluAuthoredPartsPreserveIdentityInputAndState',
        'Mofumachi.Tests.ExperienceCaptureTests.PomAuthoredPartsPreserveIdentityInputAndState',
        'Mofumachi.Tests.ExperienceCaptureTests.FioAuthoredPartsPreserveIdentityInputAndState',
        'Mofumachi.Tests.ExperienceCaptureTests.MielAuthoredPartsPreserveIdentityInputAndState'
    )
}
function Get-MergeFailedEightArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    $A=@(Get-MergeRewardArguments $Repository $Directory $Revision $Fingerprint $Packed 'PlayMode')
    $A[[Array]::IndexOf($A,'-testResults')+1]=Join-Path $Directory 'failed-eight.xml'
    $A[[Array]::IndexOf($A,'-logFile')+1]=Join-Path $Directory 'failed-eight.log'
    return $A+@('-testFilter',((Get-MergeFailedEightMethods)-join ';'))
}
function Assert-MergeFailedEightResults([string]$Path){
    [xml]$Xml=Get-Content -Raw -Encoding UTF8 -LiteralPath $Path
    $Names=@($Xml.SelectNodes('//test-case')|ForEach-Object {$_.GetAttribute('fullname')})
    if($Names.Count -ne 8 -or @($Names|Select-Object -Unique).Count -ne 8 -or @(Compare-Object @(Get-MergeFailedEightMethods) $Names).Count){throw 'Exactly the eight failed layout/prepared-input methods are required.'}
    Assert-TestRun $Path 8 @()
}
function Read-MergeOriginalFullFailure([string]$Directory,[string]$Repository){
    $Receipt=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Repository 'References/Review/merge-reward-full-failed-2026-11-03-112005/receipt.json')|ConvertFrom-Json
    $Required=@('focus.xml','EditMode.xml','PlayMode.xml','merge-reward-source-info.json')
    if($Receipt.kind -cne 'received-merge-full-qa-failure' -or @($Receipt.files).Count -ne 4 -or @($Receipt.files.path|Select-Object -Unique).Count -ne 4 -or @(Compare-Object $Required @($Receipt.files.path)).Count){throw 'Exact four original QA file receipts required.'}
    foreach($Row in @($Receipt.files)){
        if($Row.path -cnotin @('focus.xml','EditMode.xml','PlayMode.xml','merge-reward-source-info.json') -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$' -or (Get-FileHash -LiteralPath (Join-Path $Directory $Row.path) -Algorithm SHA256).Hash -ine $Row.sha256){throw 'Original native QA evidence missing/changed; preserve the failed run.'}
    }
    Assert-TestRun (Join-Path $Directory 'focus.xml') 53 @('MergeRewardCaptureTests','MergeRewardUiTests','MergePolishTests')
    Assert-TestRun (Join-Path $Directory 'EditMode.xml') 144 @('DeliveryTests','SaveServiceTests','QuestCatalogTests')
    [xml]$Play=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'PlayMode.xml')
    $Cases=@($Play.SelectNodes('//test-case'));$Failed=@($Cases|Where-Object {$_.GetAttribute('result') -ne 'Passed'}|ForEach-Object {$_.GetAttribute('fullname')})
    if($Cases.Count -ne 260 -or [int]$Play.'test-run'.passed -ne 252 -or [int]$Play.'test-run'.failed -ne 8 -or [int]$Play.'test-run'.skipped -ne 0 -or $Failed.Count -ne 8 -or @(Compare-Object @(Get-MergeFailedEightMethods) $Failed).Count){throw 'Original failed method coverage differs.'}
    $S=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'merge-reward-source-info.json')|ConvertFrom-Json
    if($S.gitRevision -cne $Receipt.source_revision -or $S.productFingerprint -cne $Receipt.product_fingerprint -or $S.packed_resource_sha256 -cne $Receipt.packed_resource_sha256 -or $S.source_unchanged -isnot [bool] -or -not $S.source_unchanged -or $S.full_qa_verified -or $S.build_generated -or $S.pixel_verified){throw 'Original failed run identity/status differs.'}
    return @{original_revision=$S.gitRevision;original_fingerprint=$S.productFingerprint;original_playmode_passed=252;original_playmode_failed=8;original_files=@($Receipt.files)}
}
if($MyInvocation.InvocationName -eq '.'){return}
if(-not $Repair8Options.Original){throw 'Specify the exact failed Merge run with -OriginalDirectory. No full QA or APK is run by this diagnostic.'}
$RepairRepo=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$RepairGit=Find-SourceGit
$Original=(Resolve-Path -LiteralPath $Repair8Options.Original).Path
Write-Host 'Reading original QA XML only; these are not new full-suite executions.'
$OriginalProof=Read-MergeOriginalFullFailure $Original $RepairRepo
$RepairEditor=if($Repair8Options.Editor){$Repair8Options.Editor}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $RepairEditor -PathType Leaf) -or (Get-Process -Name Unity -ErrorAction SilentlyContinue)){throw 'Unity6000.6.4f1 must be available, with the Editor closed.'}
$Changed=@(& $RepairGit -C $RepairRepo diff --name-only $OriginalProof.original_revision HEAD -- Assets ProjectSettings Packages References/Characters ':(exclude)Assets/Mofumachi/Tests/**')
if($LASTEXITCODE -ne 0 -or $Changed.Count){throw 'Production inputs differ from the original full QA; this test-only recovery cannot be used.'}
$null=Assert-MergeRewardVisualApproval $RepairRepo
$Preserved=Move-PreviousCharacterInputs $RepairRepo $RepairGit
Assert-CleanProductSource $RepairRepo $RepairGit;Assert-CharactersVisualLock $RepairRepo;Assert-NoahAuthoredMasterFixed $RepairRepo;Assert-NoahAuthoredFrozenInputs $RepairRepo
$Revision=Read-SourceRevision $RepairRepo;$Fingerprint=Get-CharacterAuthoredSourceFingerprint $RepairRepo
$RepairRun=Join-Path $RepairRepo ('Builds/Diagnostics/merge-reward-repair8-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory -Path (Join-Path $RepairRun 'original') -Force|Out-Null
foreach($Name in @('focus.xml','EditMode.xml','PlayMode.xml','merge-reward-source-info.json')){Copy-Item -LiteralPath (Join-Path $Original $Name) -Destination (Join-Path $RepairRun ('original/'+$Name))}
$RepairRecord=[ordered]@{scope='merge-reward-failed-eight-repair';gitRevision=$Revision;productFingerprint=$Fingerprint;packed_resource_sha256='';original_directory=$Original;original_evidence=$OriginalProof;production_inputs_unchanged=$true;expected_methods=@(Get-MergeFailedEightMethods);preserved_inputs=$Preserved;focused_native_verified=$false;full_qa_verified=$false;build_generated=$false;pixel_verified=$false;source_unchanged=$false;error=''}
$RepairFailure=$null;Write-Host ('Exact failed-eight run: '+$RepairRun)
try{
    foreach($Step in @('prepare-noah','prepare-five')){Invoke-NoahAuthoredUnity $RepairEditor (Get-CharactersDirectArguments $RepairRepo $RepairRun $Step) $RepairRun}
    $RepairRecord.packed_resource_sha256=Get-PreparedCharactersDigest $RepairRepo
    Invoke-NoahAuthoredUnity $RepairEditor (Get-MergeFailedEightArguments $RepairRepo $RepairRun $Revision $Fingerprint $RepairRecord.packed_resource_sha256) $RepairRun
    Assert-MergeFailedEightResults (Join-Path $RepairRun 'failed-eight.xml');$RepairRecord.focused_native_verified=$true
    if((Get-PreparedCharactersDigest $RepairRepo) -cne $RepairRecord.packed_resource_sha256){throw 'Prepared resource bytes changed during the eight-method diagnostic.'}
}catch{$RepairFailure=$_;$RepairRecord.error=$_.Exception.Message}
finally{
    try{
        Remove-OwnedCharacterResources $RepairRepo $RepairRun;Remove-OwnedNoahAuthoredResources $RepairRepo $RepairRun
        if((Read-SourceRevision $RepairRepo) -cne $Revision -or (Get-CharacterAuthoredSourceFingerprint $RepairRepo) -cne $Fingerprint){throw 'Source changed during focused recovery.'}
        $null=Assert-MergeRewardVisualApproval $RepairRepo;Assert-CharactersVisualLock $RepairRepo;Assert-NoahAuthoredMasterFixed $RepairRepo;Assert-NoahAuthoredFrozenInputs $RepairRepo;$RepairRecord.source_unchanged=$true
    }catch{if(-not $RepairFailure){$RepairFailure=$_};$RepairRecord.error+=' Cleanup/source: '+$_.Exception.Message}
    $RepairRecord|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $RepairRun 'source-info.json') -Encoding UTF8
    try{Compress-Archive -Path (Join-Path $RepairRun '*') -DestinationPath ($RepairRun+'-review-01.zip');Write-Host ('Focused result ZIP: '+$RepairRun+'-review-01.zip');if($env:OS -eq 'Windows_NT'){Start-Process -FilePath explorer.exe -ArgumentList ('/select,"'+$RepairRun+'-review-01.zip"')}}catch{Write-Host ('Archive/open failed; all original results remain: '+$RepairRun)}
    Write-Host 'Exactly eight PlayMode methods. No full suites, character visual capture, APK, install, uninstall or save reset. Original full QA remains preserved separately.'
}
if($RepairFailure){throw $RepairFailure}
Write-Host 'Focused recovery passed; original full PlayMode XML remains252/260. APK/physical Pixel acceptance is still pending.'
