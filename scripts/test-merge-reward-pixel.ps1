[CmdletBinding()]
param([string]$BuildDirectory,[string]$UnityEditorPath='',[string]$Serial='99RAY1BELH',[ValidateRange(5,600)][int]$MeasureSeconds=180)
$ErrorActionPreference='Stop'
$MergePixelOptions=@{Directory=$BuildDirectory;Editor=$UnityEditorPath;Serial=$Serial;Seconds=$MeasureSeconds}
. (Join-Path $PSScriptRoot 'prepare-merge-reward-preview.ps1')
function Read-MergeRewardCompletedRun([string]$Directory,$Approval){
    $S=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'merge-reward-source-info.json')|ConvertFrom-Json
    if($S.scope -cne 'merge-reward-ui-candidate' -or $S.error -or $S.gitRevision -cnotmatch '^[a-f0-9]{40}$' -or $S.productFingerprint -cnotmatch '^[a-f0-9]{64}$' -or $S.packed_resource_sha256 -cnotmatch '^[a-f0-9]{64}$' -or $S.approved_design_revision -cne $Approval.source_revision){throw 'Completed approved Merge run identity required.'}
    foreach($Flag in @('capture_verified','focus_verified','full_qa_verified','build_generated','source_unchanged','visual_gate_approved_by_user')){if($S.$Flag -isnot [bool] -or -not $S.$Flag){throw "Merge run not ready for Pixel: $Flag"}}
    $Names=@('focus.xml','EditMode.xml','PlayMode.xml')
    if(@($S.qa_artifacts).Count -ne 3 -or @($S.qa_artifacts.path|Select-Object -Unique).Count -ne 3 -or @(Compare-Object $Names @($S.qa_artifacts.path)).Count){throw 'Exact three original QA XML receipts required.'}
    foreach($Row in @($S.qa_artifacts)){
        $File=Join-Path $Directory $Row.path
        if($Row.sha256 -cnotmatch '^[a-f0-9]{64}$' -or (Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash -ine $Row.sha256){throw 'Original QA XML missing/changed.'}
    }
    Assert-TestRun (Join-Path $Directory 'focus.xml') 5 @('MergeRewardCaptureTests','MergeRewardUiTests','MergeInteractionTests','ProductionFrameUiTests')
    Assert-TestRun (Join-Path $Directory 'EditMode.xml') 144 @('DeliveryTests','SaveServiceTests','QuestCatalogTests')
    Assert-TestRun (Join-Path $Directory 'PlayMode.xml') 240 @('MergeRewardCaptureTests','MergeRewardUiTests','MergeInteractionTests','ProductionFrameUiTests','VerticalSliceFlowTests')
    $Artifacts=@(Assert-MergeRewardArtifacts $Directory $S.gitRevision $S.productFingerprint $S.packed_resource_sha256)
    if(-not $Artifacts.Count -or @($S.artifacts).Count -ne $Artifacts.Count -or @($S.artifacts.path|Select-Object -Unique).Count -ne $Artifacts.Count -or @(Compare-Object @($Artifacts.path) @($S.artifacts.path)).Count){throw 'Exact original capture artifact receipts required.'}
    foreach($Row in @($S.artifacts)){
        $Current=@($Artifacts|Where-Object {$_.path -ceq $Row.path})
        if($Current.Count -ne 1 -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$' -or $Row.sha256 -cne $Current[0].sha256 -or [long]$Row.bytes -ne [long]$Current[0].bytes){throw 'Original completed-run capture artifacts changed.'}
    }
    $Apk=Assert-MergeRewardApk $Directory $S.gitRevision $S.productFingerprint $S.packed_resource_sha256
    return [pscustomobject]@{source=$S;apk=$Apk}
}
if($MyInvocation.InvocationName -eq '.'){return}
if(-not $MergePixelOptions.Directory){throw 'Pass the exact completed Merge -Final run using -BuildDirectory. This continuation never runs Unity tests or builds.'}
$PixelRun=(Resolve-Path -LiteralPath $MergePixelOptions.Directory).Path
$PixelRepo=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$PixelApproval=Assert-MergeRewardVisualApproval $PixelRepo
$Completed=Read-MergeRewardCompletedRun $PixelRun $PixelApproval
$PixelEditor=if($MergePixelOptions.Editor){$MergePixelOptions.Editor}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
$PixelSdk=Join-Path (Split-Path -Parent $PixelEditor) 'Data/PlaybackEngines/AndroidPlayer/SDK'
$PixelAdb=Join-Path $PixelSdk 'platform-tools/adb.exe';$PixelAapt=Join-Path $PixelSdk 'build-tools/36.0.0/aapt2.exe'
$PixelSerial=Select-AuthorizedDevice (Invoke-CheckedNative $PixelAdb @('devices')) $MergePixelOptions.Serial
if((Invoke-CheckedNative $PixelAdb @('-s',$PixelSerial,'shell','getprop','ro.product.model')).Trim() -cne 'Pixel 3a'){throw 'Connect the selected Pixel 3a, unlock it and allow USB debugging.'}
$PixelOutput=Join-Path $PixelRun ('pixel-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $PixelOutput|Out-Null
$PixelRecord=[ordered]@{scope='merge-reward-pixel-evidence';gitRevision=$Completed.source.gitRevision;productFingerprint=$Completed.source.productFingerprint;apk_sha256=$Completed.apk.sha256;package=$Completed.apk.package;serial=$PixelSerial;started_utc=[DateTimeOffset]::UtcNow.ToString('o');measurements_complete=$false;pixel_verified=$false;release_accepted=$false;manual_visual='pending';manual_input='pending';manual_stutter='pending';manual_heat='pending';manual_crash='pending';pid_start='';pid_end='';same_process=$false;crash_log_captured=$false;surface_metrics=$null;error=''}
$PixelFailure=$null
try{
    Invoke-PixelInstall (Join-Path $PixelRun $Completed.apk.apk) $PixelAdb $PixelSerial $Completed.apk.package $PixelAapt -ExpectedSha256 $Completed.apk.sha256
    Write-Host 'On Pixel open Merge. Check generation (tea/bread/flower), selection, drag, Merge/Lv.UP, request/delivery, full board and invalid Merge where available. Preserve existing progress.'
    $null=Read-Host 'When the Merge board is visible, press Enter here; then keep playing during measurement'
    $PixelRecord.pid_start=(Invoke-CheckedNative $PixelAdb @('-s',$PixelSerial,'shell','pidof',$Completed.apk.package)).Trim()
    if($PixelRecord.pid_start -notmatch '^\d+$'){throw 'One running QA process required.'}
    & (Join-Path $PSScriptRoot 'capture-pixel-visual-check.ps1') -Label After -Seconds $MergePixelOptions.Seconds -Serial $PixelSerial -Adb $PixelAdb -OutputDirectory (Join-Path $PixelOutput 'surface')
    $PixelRecord.surface_metrics=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PixelOutput 'surface/pixel-evidence.json')|ConvertFrom-Json
    if($PixelRecord.surface_metrics.installed_apk_sha256 -cne $Completed.apk.sha256 -or -not $PixelRecord.surface_metrics.measurements_complete){throw 'Installed APK identity or measurements differ from the tested Merge build.'}
    $PixelRecord.pid_end=(Invoke-CheckedNative $PixelAdb @('-s',$PixelSerial,'shell','pidof',$Completed.apk.package)).Trim()
    $PixelRecord.same_process=$PixelRecord.pid_end -ceq $PixelRecord.pid_start
    if(-not $PixelRecord.same_process){throw 'QA process restarted during measurement; inspect crash logs.'}
    $PixelRecord.measurements_complete=$true
}catch{$PixelFailure=$_;$PixelRecord.error=$_.Exception.Message}
finally{
    try{
        $Raw=Invoke-CheckedNative $PixelAdb @('-s',$PixelSerial,'logcat','-b','crash','-d','-v','epoch','-t','2000')
        [IO.File]::WriteAllText((Join-Path $PixelOutput 'crash-buffer.txt'),$Raw);$PixelRecord.crash_log_captured=$true
    }catch{if(-not $PixelFailure){$PixelFailure=$_};$PixelRecord.measurements_complete=$false;$PixelRecord.error+=' Crash log: '+$_.Exception.Message}
    $PixelRecord|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $PixelOutput 'merge-reward-pixel.json') -Encoding UTF8
    try{Compress-Archive -Path (Join-Path $PixelOutput '*') -DestinationPath ($PixelOutput+'.zip');Write-Host ('Pixel evidence ZIP: '+$PixelOutput+'.zip')}catch{Write-Host ('Archive failed; original evidence preserved: '+$PixelOutput)}
    Write-Host ('Completed QA run preserved (do not repeat full suites): '+$PixelRun)
}
if($PixelFailure){throw $PixelFailure}
Write-Host 'Measurements captured. Visual/input/stutter/heat/crash review remains pending; install or FPS alone is not Pixel acceptance. No save reset or full-suite rerun.'
