# Read-only bridge from the original full run plus its test-only focused repair.
# Never rewrite a failed NUnit XML or relabel the no-test APK child builder.
. (Join-Path $PSScriptRoot 'diagnose-merge-reward-failed-eight.ps1')
function Read-MergeReviewedQa([string]$Directory,[string]$Repository){
    $Receipt=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Repository 'References/Review/merge-reward-repair-eight-passed-2026-11-03-141127/receipt.json')|ConvertFrom-Json
    $Required=@('source-info.json','failed-eight.xml','failed-eight.log','original/focus.xml','original/EditMode.xml','original/PlayMode.xml','original/merge-reward-source-info.json')
    if($Receipt.kind -cne 'received-merge-repair-eight-passed' -or @($Receipt.files).Count -ne 7 -or @($Receipt.files.path|Select-Object -Unique).Count -ne 7 -or @(Compare-Object $Required @($Receipt.files.path)).Count){throw 'Exact seven reviewed QA evidence receipts required.'}
    foreach($Row in @($Receipt.files)){
        $File=Join-Path $Directory $Row.path
        if(-not(Test-Path -LiteralPath $File -PathType Leaf) -or ((Get-Item -LiteralPath $File -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Get-Item -LiteralPath $File).Length -ne $Row.bytes -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$' -or (Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash -ine $Row.sha256){throw 'Reviewed native evidence missing/changed.'}
    }
    $Original=Read-MergeOriginalFullFailure (Join-Path $Directory 'original') $Repository 6>$null
    Assert-MergeFailedEightResults (Join-Path $Directory 'failed-eight.xml') 6>$null
    $Repair=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'source-info.json')|ConvertFrom-Json
    if($Repair.scope -cne 'merge-reward-failed-eight-repair' -or $Repair.gitRevision -cne $Receipt.source_revision -or $Repair.productFingerprint -cne $Receipt.product_fingerprint -or $Repair.packed_resource_sha256 -cne $Receipt.packed_resource_sha256 -or $Repair.error){throw 'Reviewed focused repair identity differs.'}
    foreach($Flag in @('focused_native_verified','source_unchanged','production_inputs_unchanged')){if($Repair.$Flag -isnot [bool] -or -not $Repair.$Flag){throw 'Focused repair failed/unfinished.'}}
    foreach($Flag in @('full_qa_verified','build_generated','pixel_verified')){if($Repair.$Flag -isnot [bool] -or $Repair.$Flag){throw 'Focused repair must retain its actual scope.'}}
    [xml]$Play=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'original/PlayMode.xml')
    $Names=@($Play.SelectNodes('//test-case')|ForEach-Object {$_.GetAttribute('fullname')})
    if($Names.Count -ne 260 -or @($Names|Select-Object -Unique).Count -ne 260){throw 'Original full coverage must contain260 unique cases.'}
    return @{basis='original-full-plus-test-only-focused-repair';original_revision=$Original.original_revision;original_fingerprint=$Original.original_fingerprint;repair_revision=$Repair.gitRevision;repair_fingerprint=$Repair.productFingerprint;repair_packed_resource_sha256=$Repair.packed_resource_sha256;original_focus_passed=53;original_editmode_passed=144;original_playmode_passed=252;original_playmode_failed=8;repair_passed=8;resolved_playmode_cases=260;full_playmode_rerun=$false;files=@($Receipt.files)}
}
function Get-MergeReviewedProductHash([string]$Repository,[string]$Git,[string]$Revision){
    if($Revision -cnotmatch '^[a-f0-9]{40}$'){throw 'Exact product revision required.'}
    $Rows=@(& $Git -c core.quotepath=true -C $Repository ls-tree -r $Revision -- Assets ProjectSettings Packages References/Characters)
    if($LASTEXITCODE -ne 0 -or -not $Rows.Count){throw 'Cannot identify the exact tracked product tree.'}
    [string[]]$Product=@($Rows|Where-Object {$_ -notmatch '\t"?Assets/Mofumachi/Tests/'})
    [Array]::Sort($Product,[StringComparer]::Ordinal)
    $Hasher=[Security.Cryptography.SHA256]::Create()
    try{return ([BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes(($Product -join "`n"))))).Replace('-','').ToLowerInvariant()}finally{$Hasher.Dispose()}
}
function Assert-MergeReviewedProduct([string]$Repository,[string]$Git,[string]$Revision){
    $Receipt=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Repository 'References/Review/merge-reward-repair-eight-passed-2026-11-03-141127/receipt.json')|ConvertFrom-Json
    $Original=Get-MergeReviewedProductHash $Repository $Git $Receipt.original_revision
    $Repair=Get-MergeReviewedProductHash $Repository $Git $Receipt.source_revision
    $Current=Get-MergeReviewedProductHash $Repository $Git $Revision
    if($Original -cne $Repair -or $Repair -cne $Current){throw 'Product bytes differ from the native full/focused evidence; reviewed QA cannot be reused.'}
    $Tests=@(& $Git -C $Repository diff --name-only $Receipt.source_revision $Revision -- Assets/Mofumachi/Tests)
    if($LASTEXITCODE -ne 0 -or $Tests.Count){throw 'Native test source differs from the reviewed eight-method run.'}
    return $Current
}
function Get-MergeReviewedResourceMethods {return @(Get-MergeFailedEightMethods|Where-Object {$_ -clike 'Mofumachi.Tests.ExperienceCaptureTests.*'})}
function Get-MergeReviewedResourceArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    $A=@(Get-MergeFailedEightArguments $Repository $Directory $Revision $Fingerprint $Packed)
    $A[[Array]::IndexOf($A,'-testResults')+1]=Join-Path $Directory 'resource-check.xml'
    $A[[Array]::IndexOf($A,'-logFile')+1]=Join-Path $Directory 'resource-check.log'
    $A[[Array]::IndexOf($A,'-testFilter')+1]=(Get-MergeReviewedResourceMethods)-join ';'
    return $A
}
function Assert-MergeReviewedResources([string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    [xml]$Xml=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'resource-check.xml')
    $Names=@($Xml.SelectNodes('//test-case')|ForEach-Object {$_.GetAttribute('fullname')});$Expected=@(Get-MergeReviewedResourceMethods)
    if($Names.Count -ne 6 -or @($Names|Select-Object -Unique).Count -ne 6 -or @(Compare-Object $Expected $Names).Count){throw 'Exactly six prepared-resource checks required before APK generation.'}
    Assert-TestRun (Join-Path $Directory 'resource-check.xml') 6 @() 6>$null
    $Lines=@(Get-Content -Encoding UTF8 -LiteralPath (Join-Path $Directory 'resource-check.log'))
    $Values=@{'-mofumachiSourceRevision'=$Revision;'-mofumachiProductFingerprint'=$Fingerprint;'-mofumachiPackedResourceSha'=$Packed;'-testFilter'=($Expected-join ';')}
    foreach($Flag in $Values.Keys){$At=[Array]::IndexOf($Lines,$Flag);if($At -lt 0 -or @($Lines|Where-Object {$_ -ceq $Flag}).Count -ne 1 -or $At+1 -ge $Lines.Count -or $Lines[$At+1] -cne $Values[$Flag]){throw 'Prebuild resource checks do not identify this exact source/material set.'}}
    if($Lines -cnotcontains '-mofumachiMergePreparedQa' -or -not(@($Lines|Where-Object {$_ -clike '*Test run completed. Exiting with code 0*'}).Count)){throw 'Prepared-resource QA context or successful native exit missing.'}
    return @(foreach($Name in @('resource-check.xml','resource-check.log')){@{path=$Name;sha256=(Get-FileHash -LiteralPath (Join-Path $Directory $Name) -Algorithm SHA256).Hash.ToLowerInvariant()}})
}
function Read-MergeReviewedBuild([string]$Directory,[string]$Repository,$Approval){
    $S=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'merge-reward-source-info.json')|ConvertFrom-Json
    if($S.scope -cne 'merge-reward-reviewed-qa-build' -or $S.error -or $S.qa_basis -cne 'original-full-plus-test-only-focused-repair' -or $S.ui_policy -cne 'fix-only' -or $S.approved_design_revision -cne $Approval.source_revision){throw 'Reviewed Merge build identity missing.'}
    foreach($Flag in @('qa_coverage_verified','resource_check_verified','build_generated','source_unchanged')){if($S.$Flag -isnot [bool] -or -not $S.$Flag){throw 'Reviewed Merge build is unfinished.'}}
    foreach($Flag in @('full_qa_verified','full_playmode_rerun','pixel_verified')){if($S.$Flag -isnot [bool] -or $S.$Flag){throw 'Reviewed Merge record cannot claim a new full run or device acceptance.'}}
    $Qa=Read-MergeReviewedQa (Join-Path $Directory 'qa-evidence') $Repository
    foreach($Key in @('basis','original_revision','original_fingerprint','repair_revision','repair_fingerprint','repair_packed_resource_sha256','original_focus_passed','original_editmode_passed','original_playmode_passed','original_playmode_failed','repair_passed','resolved_playmode_cases','full_playmode_rerun')){
        if($S.qa_evidence.$Key -cne $Qa[$Key]){throw 'Reviewed QA coverage ledger differs from the original native runs.'}
    }
    if(@($S.qa_evidence.files).Count -ne 7 -or @($S.qa_evidence.files.path|Select-Object -Unique).Count -ne 7){throw 'Reviewed QA artifact receipts missing.'}
    foreach($Row in @($S.qa_evidence.files)){
        $Expected=@($Qa.files|Where-Object {$_.path -ceq $Row.path})
        if($Expected.Count -ne 1 -or $Row.sha256 -cne $Expected[0].sha256 -or $Row.bytes -ne $Expected[0].bytes){throw 'Reviewed QA artifact receipts differ.'}
    }
    $Git=Find-SourceGit;$Product=Assert-MergeReviewedProduct $Repository $Git $S.gitRevision
    if($S.product_tree_sha256 -cne $Product -or (Get-MergeReviewedProductHash $Repository $Git (Read-SourceRevision $Repository)) -cne $Product){throw 'Reviewed APK product tree identity differs.'}
    $Resources=@(Assert-MergeReviewedResources $Directory $S.gitRevision $S.productFingerprint $S.packed_resource_sha256)
    if(@($S.resource_check_artifacts).Count -ne 2 -or @($S.resource_check_artifacts.path|Select-Object -Unique).Count -ne 2){throw 'Exact prebuild resource evidence receipts required.'}
    foreach($Row in @($S.resource_check_artifacts)){$Expected=@($Resources|Where-Object {$_.path -ceq $Row.path});if($Expected.Count -ne 1 -or $Row.sha256 -cne $Expected[0].sha256){throw 'Prebuild resource evidence changed.'}}
    $Apk=Assert-MergeRewardApk $Directory $S.gitRevision $S.productFingerprint $S.packed_resource_sha256
    return [pscustomobject]@{source=$S;apk=$Apk}
}
