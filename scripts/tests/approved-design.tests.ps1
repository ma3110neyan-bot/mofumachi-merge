$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../visual-lock.ps1')
function Reject([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Unreviewed or changed approved design was accepted.'}}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-approved-design-'+[guid]::NewGuid())
New-Item -ItemType Directory -Path (Join-Path $Root 'References/VisualLock') -Force | Out-Null
try {
    $Art=Join-Path $Root 'approved.png';[IO.File]::WriteAllBytes($Art,[byte[]]@(1,2,3,4))
    $Lock=Join-Path $Root 'References/VisualLock/approved-design-2026-10-21.json'
    @{files=@(@{path='approved.png';mode='binary';sha256=(Get-FileHash -LiteralPath $Art -Algorithm SHA256).Hash.ToLowerInvariant()})} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Lock
    Assert-ApprovedVisualLock $Root
    [IO.File]::WriteAllBytes($Art,[byte[]]@(1,2,3,5));Reject {Assert-ApprovedVisualLock $Root}
    [IO.File]::WriteAllBytes($Art,[byte[]]@(1,2,3,4))
    Reject {Assert-CentralStageApproved $Root}
    $Stage=Join-Path $Root 'References/VisualLock/stage-acceptance-2026-10-21.json'
    @{central=@{native_unity=$false;visual=$true;town_density=$true;walking=$true;reviewed_candidate=('a'*40);native_image_count=3}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Stage
    Reject {Assert-CentralStageApproved $Root}
    $Approved=Get-Content -LiteralPath $Stage -Raw | ConvertFrom-Json;$Approved.central.native_unity=$true
    $Approved | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Stage
    # A collection of true flags without evidence must never unlock the stage.
    Reject {Assert-CentralStageApproved $Root}
    foreach($Relative in Get-CentralInputPaths) {
        $Path=Join-Path $Root $Relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $Path) -Force | Out-Null
        [IO.File]::WriteAllText($Path,'synthetic central dependency')
    }
    # Restore the real fixture lock after creating dependency files.
    @{reference='approved.png';files=@(@{path='approved.png';mode='binary';sha256=(Get-FileHash -LiteralPath $Art -Algorithm SHA256).Hash.ToLowerInvariant()})} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Lock
    $Run=Join-Path $Root 'Builds/CentralReview/synthetic'
    $Screens=Join-Path $Run 'screenshots';New-Item -ItemType Directory -Path $Screens -Force | Out-Null
    $Header=[byte[]]@(137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,2,208,0,0,5,0,0)
    foreach($Level in @(1,3,6)){[IO.File]::WriteAllBytes((Join-Path $Screens "04-town-lv$Level-Central.png"),$Header)}
    $Entries=@(Get-CentralCaptureEntries $Screens)
    $Digest=Get-CentralInputFingerprint $Root
    $Info=@{gitRevision=('a'*40);productFingerprint=('c'*64);centralInputFingerprint=$Digest}
    $Manifest=@{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Digest;images=$Entries}
    $Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'central-review-info.json')
    $Manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Run 'capture-manifest.json')
    $Baseline=Join-Path $Root 'References/VisualLock/baseline-native-2026-10-21.json'
    @{capture=@{images=$Entries}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Baseline
    foreach($Entry in $Entries){Copy-Item -LiteralPath (Join-Path $Screens $Entry.file) -Destination (Join-Path $Run ('before-'+$Entry.file))}
    Copy-Item -LiteralPath $Art -Destination (Join-Path $Run 'approved-design.png')
    $Comparison=Join-Path $Run 'central-before-lock-candidate.jpg'
    [IO.File]::WriteAllBytes($Comparison,[byte[]]@(10,20,30))
    $Evidence=@{candidate_revision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Digest;native_unity=$true;native_pairs_verified=3;approved_design_sha256=(Get-FileHash -LiteralPath $Art -Algorithm SHA256).Hash.ToLowerInvariant();comparison_sha256=(Get-FileHash -LiteralPath $Comparison -Algorithm SHA256).Hash.ToLowerInvariant()}
    $Evidence | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'central-adoption.json')
    $Approved=@{central=@{native_unity=$true;visual=$true;town_density=$true;walking=$true;reviewed_candidate=$Info.gitRevision;native_image_count=3;review_directory='Builds/CentralReview/synthetic';central_input_fingerprint=$Digest};completed_after_central=@()}
    $Approved | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Stage
    Assert-CentralStageApproved $Root
    Reject {Assert-BaseStagesReadyForAcceptance $Root}
    $Expected=@('west_cafe','east_florist_shop','north_quest','south_garden','connections','merge_visual_lock','ui','basic_loop')
    $Approved.completed_after_central=@('east_florist_shop','west_cafe')+$Expected[2..7]
    $Approved | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Stage
    Reject {Assert-BaseStagesReadyForAcceptance $Root}
    $Approved.completed_after_central=$Expected
    $Approved | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Stage
    Assert-BaseStagesReadyForAcceptance $Root
    $OldSource=[IO.File]::ReadAllText((Join-Path $Root (Get-CentralInputPaths)[0]))
    [IO.File]::AppendAllText((Join-Path $Root (Get-CentralInputPaths)[0]),'changed')
    Reject {Assert-CentralStageApproved $Root}
    [IO.File]::WriteAllText((Join-Path $Root (Get-CentralInputPaths)[0]),$OldSource)
    $Image=Join-Path $Screens '04-town-lv1-Central.png';[IO.File]::AppendAllText($Image,'changed')
    Reject {Assert-CentralStageApproved $Root};[IO.File]::WriteAllBytes($Image,$Header)
    $Before=Join-Path $Run 'before-04-town-lv1-Central.png';Remove-Item -LiteralPath $Before
    Reject {Assert-CentralStageApproved $Root};[IO.File]::WriteAllBytes($Before,$Header)
    [IO.File]::AppendAllText($Comparison,'changed');Reject {Assert-CentralStageApproved $Root}
    [IO.File]::WriteAllBytes($Comparison,[byte[]]@(10,20,30))
    $Approved.central.reviewed_candidate=('b'*40);$Approved | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Stage
    Reject {Assert-CentralStageApproved $Root}
    $Approved.central.reviewed_candidate=$Info.gitRevision;$Approved.central.walking=$false
    $Approved | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Stage
    Reject {Assert-CentralStageApproved $Root}
    $Adoption=Join-Path $Root 'adoption.json'
    $Complete=@{native_pairs_verified=102;visual=$true;input=$true;information=$true;world=$true;fps=$true;pixel_load=$true;pixel_verified=$true;town_density=$null;merge_fun=$null}
    $Complete | ConvertTo-Json | Set-Content -LiteralPath $Adoption
    Reject {Assert-BaseAdoption $Adoption}
    $Complete.town_density=$true;$Complete.merge_fun=$true;$Complete | ConvertTo-Json | Set-Content -LiteralPath $Adoption
    Assert-BaseAdoption $Adoption
    Assert-ApprovedVisualLock (Join-Path $PSScriptRoot '../..')
    Reject {Assert-CentralStageApproved (Join-Path $PSScriptRoot '../..')}
    Write-Host 'Approved design, source/evidence hashes and mandatory stage order checked. Synthetic images only; no Unity or Pixel execution.'
} finally {Remove-Item -LiteralPath $Root -Recurse -Force}
