$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../merge-reward-reviewed-evidence.ps1')
function RejectReviewed([scriptblock]$Action){$Rejected=$false;try{& $Action 6>$null|Out-Null}catch{$Rejected=$true};if(-not $Rejected){throw 'Incomplete reviewed QA/APK evidence accepted.'}}
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path;$Git=Find-SourceGit
$Repair=Join-Path $Repo 'References/Review/merge-reward-repair-eight-passed-2026-11-03-141127'
$Q=Read-MergeReviewedQa $Repair $Repo
if($Q.original_focus_passed -ne 53 -or $Q.original_editmode_passed -ne 144 -or $Q.original_playmode_passed -ne 252 -or $Q.original_playmode_failed -ne 8 -or $Q.repair_passed -ne 8 -or $Q.resolved_playmode_cases -ne 260 -or $Q.full_playmode_rerun){throw 'Reviewed QA coverage misrepresented the original native runs.'}
$Product=Assert-MergeReviewedProduct $Repo $Git (Read-SourceRevision $Repo)
RejectReviewed {Assert-MergeReviewedProduct $Repo $Git 'a18e59814894316341e7e3db6c47b19b6bdee24e'}
$ResourceArgs=@(Get-MergeReviewedResourceArguments $Repo ([IO.Path]::GetTempPath()) ('a'*40) ('b'*64) ('c'*64))
$FilterIndex=[Array]::IndexOf($ResourceArgs,'-testFilter')
if($FilterIndex -lt 0 -or $ResourceArgs[$FilterIndex+1] -cne ((Get-MergeReviewedResourceMethods)-join ';') -or $ResourceArgs -contains '-executeMethod' -or $ResourceArgs -contains '-mofumachiCharacterPreview' -or $ResourceArgs -contains '-mofumachiNoahAuthoredPreview'){throw 'Prebuild validation escaped the six prepared-input methods.'}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-reviewed-build-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path (Join-Path $Root 'qa-evidence') -Force|Out-Null
try{
    foreach($Row in $Q.files){$Target=Join-Path $Root ('qa-evidence/'+$Row.path);New-Item -ItemType Directory -Path (Split-Path $Target -Parent) -Force|Out-Null;Copy-Item -LiteralPath (Join-Path $Repair $Row.path) -Destination $Target}
    $null=Read-MergeReviewedQa (Join-Path $Root 'qa-evidence') $Repo
    [IO.File]::AppendAllText((Join-Path $Root 'qa-evidence/failed-eight.xml'),' ');RejectReviewed {Read-MergeReviewedQa (Join-Path $Root 'qa-evidence') $Repo}
    Copy-Item -LiteralPath (Join-Path $Repair 'failed-eight.xml') -Destination (Join-Path $Root 'qa-evidence/failed-eight.xml') -Force
    $Rev=Read-SourceRevision $Repo;$Fp='b'*64;$Packed='c'*64
    $Apk=Join-Path $Root 'characters-authored.apk';[IO.File]::WriteAllText($Apk,'synthetic APK contract fixture, not an Android build')
    $Build=@{scope='characters-direct-pixel-no-tests';mode='authored';package='com.mofumachi.merge.qa.townlife';apk='characters-authored.apk';gitRevision=$Rev;productFingerprint=$Fp;packed_resource_sha256=$Packed;sha256=(Get-FileHash $Apk -Algorithm SHA256).Hash.ToLowerInvariant();tests_run=$false;full_qa_verified=$false;pixel_verified=$false}
    $Direct=@{scope='characters-direct-pixel-no-tests';gitRevision=$Rev;productFingerprint=$Fp;packed_resource_sha256=$Packed;build_generated=$true;source_unchanged=$true;tests_run=$false;full_qa_verified=$false;pixel_verified=$false;error=''}
    $Build|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $Root 'characters-authored-build.json') -Encoding UTF8
    $Direct|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $Root 'source-info.json') -Encoding UTF8
    $Approval=Assert-MergeRewardVisualApproval $Repo
    $Source=@{scope='merge-reward-reviewed-qa-build';gitRevision=$Rev;productFingerprint=$Fp;packed_resource_sha256=$Packed;approved_design_revision=$Approval.source_revision;ui_policy='fix-only';qa_basis='original-full-plus-test-only-focused-repair';qa_coverage_verified=$true;full_qa_verified=$false;full_playmode_rerun=$false;pixel_verified=$false;build_generated=$true;source_unchanged=$true;product_tree_sha256=$Product;qa_evidence=$Q;error=''}
    $Path=Join-Path $Root 'merge-reward-source-info.json';$Good=$Source|ConvertTo-Json -Depth 14
    [IO.File]::WriteAllText($Path,$Good);RejectReviewed {Read-MergeReviewedBuild $Root $Repo $Approval}
    $ResourceNames=@(Get-MergeReviewedResourceMethods)
    $Xml='<test-run result="Passed" total="6" passed="6" failed="0" skipped="0">'+(($ResourceNames|ForEach-Object {'<test-case fullname="'+$_+'" result="Passed"/>'})-join '')+'</test-run>'
    [IO.File]::WriteAllText((Join-Path $Root 'resource-check.xml'),$Xml)
    $Log=@('COMMAND LINE ARGUMENTS:','-mofumachiSourceRevision',$Rev,'-mofumachiProductFingerprint',$Fp,'-mofumachiPackedResourceSha',$Packed,'-testFilter',($ResourceNames-join ';'),'-mofumachiMergePreparedQa','Test run completed. Exiting with code 0 (Passed).')-join "`n"
    [IO.File]::WriteAllText((Join-Path $Root 'resource-check.log'),$Log)
    $Source.resource_check_verified=$true
    $Source.resource_check_artifacts=@(foreach($Name in @('resource-check.xml','resource-check.log')){@{path=$Name;sha256=(Get-FileHash (Join-Path $Root $Name) -Algorithm SHA256).Hash.ToLowerInvariant()}})
    $Good=$Source|ConvertTo-Json -Depth 14;[IO.File]::WriteAllText($Path,$Good)
    $Result=Read-MergeReviewedBuild $Root $Repo $Approval
    if($Result.apk.sha256 -cne $Build.sha256){throw 'Reviewed build returned another APK.'}
    foreach($Case in @('scope','coverage','full-claim','rerun-claim','device-claim','failed','unbuilt','source','product','design','qa-count','qa-source','resource-unverified','resource-bytes','different-packed','apk-bytes','repair-bytes')){
        $Bad=$Good|ConvertFrom-Json
        switch($Case){
            scope {$Bad.scope='old'} coverage {$Bad.qa_coverage_verified=$false} full-claim {$Bad.full_qa_verified=$true}
            rerun-claim {$Bad.full_playmode_rerun=$true} device-claim {$Bad.pixel_verified=$true} failed {$Bad.error='failed'}
            unbuilt {$Bad.build_generated=$false} source {$Bad.source_unchanged=$false} product {$Bad.product_tree_sha256=('e'*64)}
            design {$Bad.approved_design_revision=('e'*40)} qa-count {$Bad.qa_evidence.resolved_playmode_cases=252}
            qa-source {$Bad.qa_evidence.repair_revision=('e'*40)}
            resource-unverified {$Bad.resource_check_verified=$false}
            resource-bytes {[IO.File]::AppendAllText((Join-Path $Root 'resource-check.xml'),'changed')}
            different-packed {
                $Bad.packed_resource_sha256=('e'*64);$OtherBuild=($Build|ConvertTo-Json)|ConvertFrom-Json;$OtherBuild.packed_resource_sha256=$Bad.packed_resource_sha256
                $OtherBuild|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $Root 'characters-authored-build.json') -Encoding UTF8
                $OtherDirect=($Direct|ConvertTo-Json)|ConvertFrom-Json;$OtherDirect.packed_resource_sha256=$Bad.packed_resource_sha256
                $OtherDirect|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $Root 'source-info.json') -Encoding UTF8
            }
            apk-bytes {[IO.File]::AppendAllText($Apk,'changed')}
            repair-bytes {[IO.File]::AppendAllText((Join-Path $Root 'qa-evidence/failed-eight.log'),'changed')}
        }
        $Bad|ConvertTo-Json -Depth 14|Set-Content -LiteralPath $Path -Encoding UTF8
        RejectReviewed {Read-MergeReviewedBuild $Root $Repo $Approval}
        if($Case -eq 'apk-bytes'){[IO.File]::WriteAllText($Apk,'synthetic APK contract fixture, not an Android build')}
        if($Case -eq 'repair-bytes'){Copy-Item -LiteralPath (Join-Path $Repair 'failed-eight.log') -Destination (Join-Path $Root 'qa-evidence/failed-eight.log') -Force}
        if($Case -eq 'resource-bytes'){[IO.File]::WriteAllText((Join-Path $Root 'resource-check.xml'),$Xml)}
        if($Case -eq 'different-packed'){$Build|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $Root 'characters-authored-build.json') -Encoding UTF8;$Direct|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $Root 'source-info.json') -Encoding UTF8}
    }
    . (Join-Path $PSScriptRoot '../test-merge-reward-pixel.ps1')
    $Completed=Read-MergeRewardCompletedRun $Root $Approval
    if($Completed.apk.sha256 -cne $Build.sha256){throw 'Pixel-only resume did not validate the reviewed QA build.'}
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Reviewed-build contracts passed: exact native coverage, original failure retained, product equivalence and APK identity; false full/device claims rejected. Synthetic APK only, no Unity/device run.'
