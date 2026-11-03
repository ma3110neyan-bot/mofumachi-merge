$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../prepare-merge-reward-preview.ps1')
if(-not(Get-Command Assert-MergeRewardVisualApproval -ErrorAction SilentlyContinue)){throw 'Approved Merge design is not checked before release QA.'}
. (Join-Path $PSScriptRoot '../test-merge-reward-pixel.ps1')
function RejectRelease([scriptblock]$Action){$Rejected=$false;try{& $Action 6>$null | Out-Null}catch{$Rejected=$true};if(-not $Rejected){throw 'Incomplete Merge release evidence accepted.'}}
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$Approval=Assert-MergeRewardVisualApproval $Repo
if(-not $Approval.visual_gate_approved_by_user -or $Approval.ui_policy -cne 'fix-only'){throw 'Current user Visual approval missing.'}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-merge-release-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $Root | Out-Null
function PutReleaseJson([string]$Name,$Object){$Object|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $Root $Name) -Encoding UTF8}
function WriteReleaseXml([string]$Name,[int]$Total,[string[]]$Suites){
    $Body=@(foreach($Suite in $Suites){'<test-suite name="'+$Suite+'" result="Passed"><test-case result="Passed"/></test-suite>'}) -join ''
    $Body+=('<test-case result="Passed"/>'*($Total-$Suites.Count))
    [IO.File]::WriteAllText((Join-Path $Root $Name),('<test-run total="'+$Total+'" passed="'+$Total+'" failed="0" skipped="0" result="Passed">'+$Body+'</test-run>'))
}
try{
    # Separate synthetic contract fixture; never presented as Unity/device evidence.
    $VisualRoot=Join-Path $Root 'visual';New-Item -ItemType Directory -Path (Join-Path $VisualRoot 'References/VisualLock') -Force | Out-Null
    Copy-Item (Join-Path $Repo 'References/VisualLock/merge-reward-visual-approved-2026-11-03.json') (Join-Path $VisualRoot 'References/VisualLock/')
    foreach($Row in @($Approval.files)+@(@{path=$Approval.reference_path})){
        $Target=Join-Path $VisualRoot $Row.path;New-Item -ItemType Directory -Path (Split-Path $Target -Parent) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $Repo $Row.path) -Destination $Target
    }
    $null=Assert-MergeRewardVisualApproval $VisualRoot
    $Changed=Join-Path $VisualRoot $Approval.files[0].path;[IO.File]::AppendAllText($Changed,'drift')
    RejectRelease {Assert-MergeRewardVisualApproval $VisualRoot}

    WriteReleaseXml 'focus.xml' 5 @('MergeRewardCaptureTests','MergeRewardUiTests','MergeInteractionTests','ProductionFrameUiTests')
    WriteReleaseXml 'EditMode.xml' 144 @('DeliveryTests','SaveServiceTests','QuestCatalogTests')
    WriteReleaseXml 'PlayMode.xml' 240 @('MergeRewardCaptureTests','MergeRewardUiTests','MergeInteractionTests','ProductionFrameUiTests','VerticalSliceFlowTests')
    $Rev='a'*40;$Fp='b'*64;$Packed='c'*64
    $Apk=Join-Path $Root 'characters-authored.apk';[IO.File]::WriteAllText($Apk,'synthetic, not an APK')
    $Build=@{scope='characters-direct-pixel-no-tests';mode='authored';package='com.mofumachi.merge.qa.townlife';apk='characters-authored.apk';gitRevision=$Rev;productFingerprint=$Fp;packed_resource_sha256=$Packed;sha256=(Get-FileHash $Apk -Algorithm SHA256).Hash.ToLowerInvariant();tests_run=$false;full_qa_verified=$false;pixel_verified=$false}
    PutReleaseJson 'characters-authored-build.json' $Build
    New-Item -ItemType Directory -Path (Join-Path $Root 'screenshots')|Out-Null
    foreach($Name in @('merge-reward.html','merge-reward-native.json')){[IO.File]::WriteAllText((Join-Path $Root ('screenshots/'+$Name)),'synthetic')}
    function FixtureArtifactReceipts {
        foreach($Name in @('merge-reward.html','merge-reward-native.json')){
            $File=Join-Path $Root ('screenshots/'+$Name);@{path=('screenshots/'+$Name);bytes=(Get-Item $File).Length;sha256=(Get-FileHash $File -Algorithm SHA256).Hash.ToLowerInvariant()}
        }
    }
    $Source=@{scope='merge-reward-ui-candidate';gitRevision=$Rev;productFingerprint=$Fp;packed_resource_sha256=$Packed;capture_verified=$true;focus_verified=$true;full_qa_verified=$true;build_generated=$true;source_unchanged=$true;visual_gate_approved_by_user=$true;approved_design_revision=$Approval.source_revision;error='';artifacts=@(FixtureArtifactReceipts);qa_artifacts=@(foreach($Name in @('focus.xml','EditMode.xml','PlayMode.xml')){@{path=$Name;sha256=(Get-FileHash (Join-Path $Root $Name) -Algorithm SHA256).Hash.ToLowerInvariant()}})}
    PutReleaseJson 'merge-reward-source-info.json' $Source
    # Disk image rejection is exercised by merge-capture-artifacts.tests.ps1.
    # Here require that the existing validator is called with this run's identity.
    $script:ArtifactsChecked=0
    function Assert-MergeRewardArtifacts([string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
        if($Directory -cne $Root -or $Revision -cne $Rev -or $Fingerprint -cne $Fp -or $Packed -cne ('c'*64)){throw 'Capture validation routed to another run.'};$script:ArtifactsChecked++;FixtureArtifactReceipts
    }
    $Result=Read-MergeRewardCompletedRun $Root $Approval 6>$null
    if($Result.apk.sha256 -cne $Build.sha256 -or $script:ArtifactsChecked -ne 1){throw 'Exact completed APK/capture identity not checked.'}
    foreach($Flag in @('capture_verified','focus_verified','full_qa_verified','build_generated','source_unchanged','visual_gate_approved_by_user')){
        $Source[$Flag]=$false;PutReleaseJson 'merge-reward-source-info.json' $Source
        RejectRelease {Read-MergeRewardCompletedRun $Root $Approval};$Source[$Flag]=$true
    }
    foreach($Case in @('error','approval','identity','missing-receipt','duplicate-receipt','missing-artifact-receipt','duplicate-artifact-receipt','artifact-bytes','artifact-sha','changed-artifact','changed-xml','changed-apk','failed-xml')){
        $Bad=($Source|ConvertTo-Json -Depth 12)|ConvertFrom-Json
        switch($Case){
            error {$Bad.error='failed'}
            approval {$Bad.approved_design_revision=('d'*40)}
            identity {$Bad.gitRevision=('d'*40)}
            missing-receipt {$Bad.qa_artifacts=$Bad.qa_artifacts[0..1]}
            duplicate-receipt {$Bad.qa_artifacts[2]=$Bad.qa_artifacts[1]}
            missing-artifact-receipt {$Bad.artifacts=@($Bad.artifacts[0])}
            duplicate-artifact-receipt {$Bad.artifacts[1]=$Bad.artifacts[0]}
            artifact-bytes {$Bad.artifacts[0].bytes++}
            artifact-sha {$Bad.artifacts[0].sha256=('d'*64)}
            changed-artifact {[IO.File]::AppendAllText((Join-Path $Root 'screenshots/merge-reward.html'),'changed')}
            changed-xml {[IO.File]::AppendAllText((Join-Path $Root 'focus.xml'),' ')}
            changed-apk {[IO.File]::AppendAllText($Apk,'changed')}
            failed-xml {
                $Xml=Join-Path $Root 'PlayMode.xml';$Text=[IO.File]::ReadAllText($Xml).Replace('result="Passed"','result="Failed"');[IO.File]::WriteAllText($Xml,$Text)
                $Bad.qa_artifacts[2].sha256=(Get-FileHash $Xml -Algorithm SHA256).Hash.ToLowerInvariant()
            }
        }
        PutReleaseJson 'merge-reward-source-info.json' $Bad;RejectRelease {Read-MergeRewardCompletedRun $Root $Approval}
        if($Case -eq 'changed-artifact'){[IO.File]::WriteAllText((Join-Path $Root 'screenshots/merge-reward.html'),'synthetic')}
        if($Case -eq 'changed-xml'){WriteReleaseXml 'focus.xml' 5 @('MergeRewardCaptureTests','MergeRewardUiTests','MergeInteractionTests','ProductionFrameUiTests')}
        if($Case -eq 'changed-apk'){[IO.File]::WriteAllText($Apk,'synthetic, not an APK')}
    }
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Merge release: approved source drift, incomplete QA, XML changes/failures and wrong APK rejected. Synthetic contracts only; no Unity or Pixel execution.'
