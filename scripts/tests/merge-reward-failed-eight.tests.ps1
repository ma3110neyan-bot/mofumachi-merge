$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../diagnose-merge-reward-failed-eight.ps1')
function RejectRepair([scriptblock]$Action){$Rejected=$false;try{& $Action 6>$null|Out-Null}catch{$Rejected=$true};if(-not $Rejected){throw 'Invalid eight-method repair evidence accepted.'}}
$Expected=@('Mofumachi.Tests.DailyWorldTests.FlatCellsHaveStableIndexesAndRealNonOverlappingHitRects','Mofumachi.Tests.MajorPolishTests.FlatBoardKeepsAlignedEdgesAndAllThirtyCellsActuallyReceiveRaycasts','Mofumachi.Tests.ExperienceCaptureTests.NoahAuthoredPartsPreviewPreservesState','Mofumachi.Tests.ExperienceCaptureTests.MomoAuthoredPartsPreserveIdentityInputAndState','Mofumachi.Tests.ExperienceCaptureTests.LuluAuthoredPartsPreserveIdentityInputAndState','Mofumachi.Tests.ExperienceCaptureTests.PomAuthoredPartsPreserveIdentityInputAndState','Mofumachi.Tests.ExperienceCaptureTests.FioAuthoredPartsPreserveIdentityInputAndState','Mofumachi.Tests.ExperienceCaptureTests.MielAuthoredPartsPreserveIdentityInputAndState')
$Actual=@(Get-MergeFailedEightMethods)
if($Actual.Count -ne 8 -or @(Compare-Object $Expected $Actual).Count){throw 'Repair must target exactly the eight reported failures.'}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-merge-repair8-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $Root|Out-Null
try{
    $Args8=@(Get-MergeFailedEightArguments 'repo' $Root ('a'*40) ('b'*64) ('c'*64));$Index=[Array]::IndexOf($Args8,'-testFilter')
    if($Index -lt 0 -or $Args8[$Index+1] -cne ($Actual -join ';') -or $Args8 -notcontains '-mofumachiMergePreparedQa' -or $Args8 -contains '-executeMethod' -or $Args8 -contains '-quit' -or $Args8 -contains '-mofumachiCharacterPreview' -or $Args8 -contains '-mofumachiNoahAuthoredPreview'){throw 'Repair escaped the eight input/layout checks into full or character-capture execution.'}
    $Body=(@($Expected|ForEach-Object {'<test-case fullname="'+$_+'" result="Passed"/>'}) -join '')
    $Good='<test-run result="Passed" total="8" passed="8" failed="0" skipped="0">'+$Body+'</test-run>'
    $Path=Join-Path $Root 'failed-eight.xml';[IO.File]::WriteAllText($Path,$Good);Assert-MergeFailedEightResults $Path 6>$null
    foreach($Case in @('missing','duplicate','extra','failed','skipped')){
        [xml]$Bad=$Good;$Nodes=@($Bad.SelectNodes('//test-case'))
        switch($Case){
            missing {$null=$Bad.'test-run'.RemoveChild($Nodes[0])}
            duplicate {$Nodes[1].SetAttribute('fullname',$Nodes[0].GetAttribute('fullname'))}
            extra {$Nodes[0].SetAttribute('fullname','Mofumachi.Tests.Unrelated.Test')}
            failed {$Nodes[0].SetAttribute('result','Failed')}
            skipped {$Bad.'test-run'.SetAttribute('skipped','1')}
        }
        $Bad.Save($Path);RejectRepair {Assert-MergeFailedEightResults $Path}
    }
    $Repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
    $Original=Join-Path $Repo 'References/Review/merge-reward-full-failed-2026-11-03-112005'
    $Proof=Read-MergeOriginalFullFailure $Original $Repo 6>$null
    if($Proof.original_playmode_passed -ne 252 -or $Proof.original_playmode_failed -ne 8){throw 'Original native results were relabelled.'}
    foreach($Name in @('focus.xml','EditMode.xml','PlayMode.xml','merge-reward-source-info.json')){Copy-Item -LiteralPath (Join-Path $Original $Name) -Destination (Join-Path $Root $Name)}
    [IO.File]::AppendAllText((Join-Path $Root 'PlayMode.xml'),' ');RejectRepair {Read-MergeOriginalFullFailure $Root $Repo}
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Repair contracts: exactly eight methods, original failed XML preserved, no full-suite/capture/APK route; malformed or altered evidence rejected. Synthetic checks, not native results.'
