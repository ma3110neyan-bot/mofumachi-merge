$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../build-android.ps1')
if(-not(Get-Command Write-FiveAreaReview -ErrorAction SilentlyContinue)){throw 'Five-area review must export source-bound images without an APK.'}
if(-not(Get-Command Write-FiveAreaReviewArchives -ErrorAction SilentlyContinue)){throw 'A passed native run must resume ZIP packaging without rerendering or changing its capture identity.'}
if(-not(Get-Command Write-SouthFinalReview -ErrorAction SilentlyContinue)){throw 'South needs its Before/held/revised native review before publishing the integrated candidate.'}
function Reject([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Invalid five-area review was accepted.'}}
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Assert-FiveAreaReviewPrerequisites $Repo
Assert-TownCheckpointApproved $Repo 'five_area'
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-five-area-'+[guid]::NewGuid())
try {
    New-Item -ItemType Directory -Path $Root -Force | Out-Null
    $ConditionsRepo=Join-Path $Root 'conditions';$StageRelative='References/VisualLock/stage-acceptance-2026-10-21.json'
    $Stage=Get-Content -LiteralPath (Join-Path $Repo $StageRelative) -Raw -Encoding UTF8 | ConvertFrom-Json
    $Approval=$Stage.user_review_checkpoints.south_garden
    $ConditionRelative=$Approval.conditions_record
    $Conditions=Get-Content -LiteralPath (Join-Path $Repo $ConditionRelative) -Raw -Encoding UTF8 | ConvertFrom-Json
    $DesignRelative='References/VisualLock/approved-design-2026-10-21.json'
    $Design=Get-Content -LiteralPath (Join-Path $Repo $DesignRelative) -Raw -Encoding UTF8 | ConvertFrom-Json
    $Paths=@($StageRelative,$ConditionRelative,$DesignRelative,$Design.reference)+@($Approval.images | ForEach-Object {$_.file})+@($Conditions.adjusted_files | ForEach-Object {$_.file})+@($Conditions.frozen_files | ForEach-Object {$_.file})
    $Polish=Get-BasePolishPatch $Repo
    if($Polish){
        $Paths+=@($Stage.base_polish,$Polish.Record.base_lock)+@($Polish.Record.changes | ForEach-Object {$_.path})
        if($Polish.Record.scope_id -eq 'town_fountain_water_2026_10_25'){$Paths+=@('References/VisualLock/town-field-frozen-2026-10-25.json','References/VisualLock/town-field-final-candidate-2026-10-25.json')}
        foreach($Key in @('new_files','capture_inputs')) {
            $Property=$Polish.Record.PSObject.Properties[$Key]
            if($Property){$Paths+=@($Property.Value | ForEach-Object {$_.path})}
        }
    }
    foreach($Relative in $Paths){$Target=Join-Path $ConditionsRepo $Relative;New-Item -ItemType Directory -Path (Split-Path -Parent $Target) -Force | Out-Null;Copy-Item -LiteralPath (Join-Path $Repo $Relative) -Destination $Target}
    Assert-SouthFinalReviewConditions $ConditionsRepo
    $ConditionPath=Join-Path $ConditionsRepo $ConditionRelative;$Saved=[IO.File]::ReadAllBytes($ConditionPath)
    # A completed South native review must not block the subsequent integrated
    # packaging-only review; this authorization still never adopts the town.
    $Conditions.native_after_adjustments_verified=$true;$Conditions | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $ConditionPath -Encoding UTF8
    Assert-SouthFinalReviewConditions $ConditionsRepo
    $Conditions.native_after_adjustments_verified=$false;[IO.File]::WriteAllBytes($ConditionPath,$Saved)
    # A later explicit integrated approval is a legitimate recorded state.
    # Keep source-bound review packaging usable without granting adoption.
    $StageFixture=Join-Path $ConditionsRepo $StageRelative;$SavedStage=[IO.File]::ReadAllBytes($StageFixture)
    $Stage.user_review_checkpoints.five_area.user_approved=$true
    $Stage | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $StageFixture -Encoding UTF8
    $Conditions.integration_user_approved=$true;$Conditions | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $ConditionPath -Encoding UTF8
    Assert-SouthFinalReviewConditions $ConditionsRepo
    $Conditions.integration_user_approved=$false;$Conditions | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $ConditionPath -Encoding UTF8
    Reject {Assert-SouthFinalReviewConditions $ConditionsRepo}
    [IO.File]::WriteAllBytes($StageFixture,$SavedStage);[IO.File]::WriteAllBytes($ConditionPath,$Saved)
    $Stage=Get-Content -LiteralPath $StageFixture -Raw -Encoding UTF8 | ConvertFrom-Json
    $Conditions=Get-Content -LiteralPath $ConditionPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach($Key in @('garden_rest_implemented','level_three_implemented','circulation_verified','ground_alignment_implemented','integration_review_authorized_by_user')) {
        $Conditions.$Key=$false;$Conditions | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $ConditionPath -Encoding UTF8
        Reject {Assert-SouthFinalReviewConditions $ConditionsRepo};$Conditions.$Key=$true
    }
    $Conditions.adjusted_files=@();$Conditions | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $ConditionPath -Encoding UTF8
    Reject {Assert-SouthFinalReviewConditions $ConditionsRepo};[IO.File]::WriteAllBytes($ConditionPath,$Saved)
    $Changed=Join-Path $ConditionsRepo 'Assets/Mofumachi/Presentation/TownSouthMasterLayout.cs';[IO.File]::AppendAllText($Changed,'changed');Reject {Assert-SouthFinalReviewConditions $ConditionsRepo}
    Copy-Item -LiteralPath (Join-Path $Repo 'Assets/Mofumachi/Presentation/TownSouthMasterLayout.cs') -Destination $Changed
    $Stage.user_review_checkpoints.south_garden.user_approved=$false;$Stage | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $ConditionsRepo $StageRelative) -Encoding UTF8
    Reject {Assert-SouthFinalReviewConditions $ConditionsRepo}
    $Run=Join-Path $Root 'run';$Screens=Join-Path $Run 'screenshots';New-Item -ItemType Directory -Path $Screens -Force | Out-Null
    $Previous=Join-Path $Repo 'References/Review/south-garden-2026-10-23-candidate'
    foreach($Area in @('central','west','east','north','south')) {
        $Destination=if($Area -eq 'central'){$Screens}else{Join-Path $Screens $Area}
        New-Item -ItemType Directory -Path $Destination -Force | Out-Null
        Copy-Item -Path (Join-Path $Previous "$Area/screenshots/*.png") -Destination $Destination
        Set-Content -LiteralPath (Join-Path $Destination 'capture-world.txt') -Value 'Pipeline fixture; older actual PNGs, synthetic current metadata; NOT a native run.'
        $District=(Get-Culture).TextInfo.ToTitleCase($Area)
        $Image="04-town-lv1-$District.png"
        $Ids=Get-FiveAreaRouteIds $District
        $Routes=@(for($i=0;$i -lt 6;$i++){@{id=$Ids[$i];points=@(@{x=80+$i*80;y=900},@{x=360;y=600})}})
        @{image=$Image;image_sha256=(Get-FileHash (Join-Path $Destination $Image) -Algorithm SHA256).Hash.ToLowerInvariant();kind='navigation-route-overlay-not-gameplay-trace';origin='top-left';width=720;height=1280;routes=$Routes} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Destination "$Area-route-projection.json") -Encoding UTF8
    }
    $Info=@{gitRevision=Read-SourceRevision $Repo;productFingerprint=Get-ProductFingerprint $Repo;centralInputFingerprint=Get-CentralInputFingerprint $Repo}
    $Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'central-review-info.json') -Encoding UTF8
    # XML is intentionally a pipeline fixture. Never register this as native evidence.
    $SuiteCounts=[ordered]@{ExperienceCaptureTests=1;CentralMasterReviewTests=2;WestMasterReviewTests=3;EastMasterReviewTests=3;NorthMasterReviewTests=3;SouthMasterReviewTests=3;FiveAreaIntegrationReviewTests=1}
    $Suites=foreach($Key in $SuiteCounts.Keys){$Cases=for($i=0;$i -lt $SuiteCounts[$Key];$i++){"<test-case name='$Key-$i' result='Passed'/>"};"<test-suite name='$Key' result='Passed'>$($Cases -join '')</test-suite>"}
    "<test-run total='16' passed='16' failed='0' skipped='0' result='Passed'>$($Suites -join '')</test-run>" | Set-Content -LiteralPath (Join-Path $Run 'central-playmode.xml')
    Set-Content -LiteralPath (Join-Path $Run 'central-playmode.log') -Value 'Synthetic pipeline metadata, not Unity execution.'
    function Write-FiveAreaComparisonJpegs([string]$Directory){foreach($Level in @(1,3,6)){[IO.File]::WriteAllBytes((Join-Path $Directory "five-area-lv$Level-before-candidate.jpg"),[byte[]]@(255,216,255,217))}}
    function Write-TownRoutesJpeg([string]$Directory,[string]$District,[string]$Title,[hashtable]$Labels){Assert-TownRouteProjection $Directory $District @($Labels.Keys);[IO.File]::WriteAllBytes((Join-Path $Directory ($District.ToLowerInvariant()+'-walking-routes.jpg')),[byte[]]@(255,216,255,217))}
    $Missing=Join-Path $Screens 'south/south-walking.png';$Bytes=[IO.File]::ReadAllBytes($Missing);Remove-Item -LiteralPath $Missing
    Reject {Write-FiveAreaReview $Repo $Run};[IO.File]::WriteAllBytes($Missing,$Bytes)
    $Bad=Join-Path $Screens 'west/west-route-projection.json';$Bytes=[IO.File]::ReadAllBytes($Bad)
    $Route=Get-Content -LiteralPath $Bad -Raw -Encoding UTF8 | ConvertFrom-Json;$Route.image_sha256='0'*64;$Route | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Bad
    Reject {Write-FiveAreaReview $Repo $Run};[IO.File]::WriteAllBytes($Bad,$Bytes)
    $Info.centralInputFingerprint='f'*64;$Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'central-review-info.json');Reject {Write-FiveAreaReview $Repo $Run}
    $Info.centralInputFingerprint=Get-CentralInputFingerprint $Repo;$Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'central-review-info.json')
    Write-FiveAreaReview $Repo $Run
    $Parts=@(Get-ChildItem -LiteralPath $Run -Filter 'five-area-review-*.zip');if($Parts.Count -lt 2){throw 'Expected size-bounded review parts.'}
    $Expanded=Join-Path $Root 'expanded';foreach($Part in $Parts){if($Part.Length -gt 21MB){throw 'Archive exceeds review upload budget.'};Expand-Archive -LiteralPath $Part.FullName -DestinationPath $Expanded}
    $Manifest=Get-Content -LiteralPath (Join-Path $Expanded 'review-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if($Manifest.native_image_count -ne 27 -or $Manifest.before_image_count -ne 15 -or $Manifest.user_approved -ne $false -or $Manifest.pixel_verified -ne $false -or $Manifest.adopted -ne $false){throw 'Review lost its scope or falsely adopted the candidate.'}
    foreach($File in $Manifest.files){if((Get-FileHash -LiteralPath (Join-Path $Expanded $File.file) -Algorithm SHA256).Hash -ne $File.sha256){throw 'Part split lost or changed evidence.'}}
    if(@(Get-ChildItem -LiteralPath $Expanded -Filter 'central-playmode.xml' -File -Recurse).Count -ne 1){throw 'Native results must be counted once.'}
    # Stage complete five-area evidence, but publish only the requested South
    # revision package. Comparison drawing is stubbed, not native rendering.
    $StageRun=Join-Path $Root 'south-first';New-Item -ItemType Directory -Path $StageRun -Force | Out-Null
    Copy-Item -LiteralPath $Screens -Destination $StageRun -Recurse
    foreach($Name in @('central-review-info.json','central-playmode.xml','central-playmode.log')){Copy-Item -LiteralPath (Join-Path $Run $Name) -Destination $StageRun}
    Write-FiveAreaReview $Repo $StageRun -StageOnly
    if(@(Get-ChildItem -LiteralPath $StageRun -Filter 'five-area-review-*.zip').Count -ne 0){throw 'South-first mode published the integrated review too early.'}
    function Write-SouthRevisionComparisons([string]$RepositoryPath,[string]$Directory){foreach($Name in @('south-before-held-revised.jpg','south-entry-before-held-revised.jpg')){[IO.File]::WriteAllBytes((Join-Path $Directory $Name),[byte[]]@(255,216,255,217))}}
    Write-SouthFinalReview $Repo $StageRun
    $SouthParts=@(Get-ChildItem -LiteralPath $StageRun -Filter 'south-final-review-*.zip');if($SouthParts.Count -lt 1){throw 'South-first evidence package missing.'}
    $SouthExpanded=Join-Path $Root 'south-expanded';foreach($Part in $SouthParts){Expand-Archive -LiteralPath $Part.FullName -DestinationPath $SouthExpanded}
    $SouthMeta=Get-Content -LiteralPath (Join-Path $SouthExpanded 'south-final-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if($SouthMeta.adopted -ne $false -or $SouthMeta.pixel_verified -ne $false -or $SouthMeta.native_test_count -ne 16 -or $SouthMeta.before_revision -ne '45f205d7873067f5fec3a26df1b32ff9f21602a7' -or $SouthMeta.held_revision -ne 'd8e649b7802292ca552d564427bc4b720fa8dca4'){throw 'South package mixed source identities or adoption state.'}
    foreach($File in $SouthMeta.files){if((Get-FileHash -LiteralPath (Join-Path $SouthExpanded $File.file) -Algorithm SHA256).Hash -ne $File.sha256){throw 'South revision part lost source evidence.'}}
    # Packaging-only recovery preserves a completed capture from before a
    # script-only update; current render inputs must still be identical.
    $Review=Join-Path $Run 'five-area-review';$ManifestPath=Join-Path $Review 'review-manifest.json'
    # Simulate the older capture's different whole-product/script fingerprint.
    # Only synthetic identity metadata is changed before the recovery fixture;
    # all rendered PNGs/JPEGs and XML bytes remain untouched.
    $OlderInfo=Get-Content -LiteralPath (Join-Path $Run 'central-review-info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $OlderInfo.productFingerprint='e'*64
    $OlderInfo | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'central-review-info.json') -Encoding UTF8
    Copy-Item -LiteralPath (Join-Path $Run 'central-review-info.json') -Destination (Join-Path $Review 'central-review-info.json')
    $Manifest=Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json;$Manifest.productFingerprint=$OlderInfo.productFingerprint
    $InfoEntry=$Manifest.files | Where-Object {$_.file -eq 'central-review-info.json'}
    $InfoEntry.bytes=(Get-Item -LiteralPath (Join-Path $Review $InfoEntry.file)).Length
    $InfoEntry.sha256=(Get-FileHash -LiteralPath (Join-Path $Review $InfoEntry.file) -Algorithm SHA256).Hash.ToLowerInvariant()
    $Manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $ManifestPath -Encoding UTF8
    $OriginalManifest=[IO.File]::ReadAllBytes($ManifestPath)
    $BackupManifest=Join-Path $Root 'original-source-manifest.json';[IO.File]::WriteAllBytes($BackupManifest,$OriginalManifest)
    $OriginalInfo=[IO.File]::ReadAllBytes((Join-Path $Run 'central-review-info.json'))
    function Write-FiveAreaComparisonJpegs([string]$Directory){throw 'Recovery must not redraw or relabel existing comparisons.'}
    function Write-TownRoutesJpeg([string]$Directory,[string]$District,[string]$Title,[hashtable]$Labels){throw 'Recovery must reuse the already drawn route JPEGs.'}
    $Recovery=Join-Path $Run 'repacked/test'
    $BadManifest=Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $BadManifest.centralInputFingerprint='0'*64;$BadManifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $ManifestPath
    Reject {Write-FiveAreaReviewArchives $Repo $Run $Recovery};[IO.File]::WriteAllBytes($ManifestPath,$OriginalManifest)
    $BadInfo=Get-Content -LiteralPath (Join-Path $Run 'central-review-info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $BadInfo.gitRevision='a'*40;$BadInfo | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'central-review-info.json')
    Reject {Write-FiveAreaReviewArchives $Repo $Run $Recovery};[IO.File]::WriteAllBytes((Join-Path $Run 'central-review-info.json'),$OriginalInfo)
    $Changed=Join-Path $Review 'south/screenshots/south-walking.png';$Saved=[IO.File]::ReadAllBytes($Changed)
    [IO.File]::AppendAllText($Changed,'changed');Reject {Write-FiveAreaReviewArchives $Repo $Run $Recovery};[IO.File]::WriteAllBytes($Changed,$Saved)
    $Extra=Join-Path $Review 'unrecorded.txt';Set-Content -LiteralPath $Extra -Value 'unbound';Reject {Write-FiveAreaReviewArchives $Repo $Run $Recovery};Remove-Item -LiteralPath $Extra
    $null=Write-FiveAreaReviewArchives $Repo $Run $Recovery
    if(-not(Test-Path -LiteralPath (Join-Path $Recovery 'five-area-review-01.zip'))){throw 'Recovery did not pack the already verified run.'}
    if((Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $BackupManifest -Algorithm SHA256).Hash){throw 'Recovery rewrote the native source manifest.'}
    foreach($File in $Manifest.files){if((Get-FileHash -LiteralPath (Join-Path $Review $File.file) -Algorithm SHA256).Hash -ne $File.sha256){throw 'Recovery changed a native/capture artifact.'}}
    Write-Host 'Five-area rejection/export/part SHA checks passed. Older PNG fixtures; synthetic XML and stubbed JPEG drawing, no native/device execution.'
} finally {if(Test-Path -LiteralPath $Root){Remove-Item -LiteralPath $Root -Recurse -Force}}
