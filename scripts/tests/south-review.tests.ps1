$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../visual-lock.ps1')
. (Join-Path $PSScriptRoot '../west-review.ps1')
. (Join-Path $PSScriptRoot '../east-review.ps1')
. (Join-Path $PSScriptRoot '../north-review.ps1')
if(Test-Path (Join-Path $PSScriptRoot '../south-review.ps1')){. (Join-Path $PSScriptRoot '../south-review.ps1')}
if(-not(Get-Command Get-SouthCaptureEntries -ErrorAction SilentlyContinue)){throw 'South needs a source-bound native review exporter, never an APK shortcut.'}
function Reject([scriptblock]$Action){$Bad=$false;try{& $Action}catch{$Bad=$true};if(-not $Bad){throw 'Incomplete South evidence was accepted.'}}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-south-review-'+[guid]::NewGuid())
try {
    New-Item -ItemType Directory -Path $Root -Force | Out-Null
    $Names=@('04-town-lv1-South.png','04-town-lv3-South.png','04-town-lv6-South.png','south-connection.png','south-walking.png','south-rest-arrived.png')
    $Header=[byte[]]@(137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,2,208,0,0,5,0,0)
    foreach($Name in $Names){[IO.File]::WriteAllBytes((Join-Path $Root $Name),$Header)}
    $Info=@{gitRevision=('a'*40);productFingerprint=('b'*64);centralInputFingerprint=('c'*64)}
    $Manifest=@{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Info.centralInputFingerprint;images=@(Get-SouthCaptureEntries $Root)}
    Assert-SouthCaptureIdentity $Info $Manifest $Root
    $Route=@{image=$Names[0];image_sha256=$Manifest.images[0].sha256;kind='navigation-route-overlay-not-gameplay-trace';origin='top-left';width=720;height=1280;routes=@()}
    $Ids=@('gazebo-left','gazebo-right','left-rest','right-rest','rest-cross','garden-loop')
    for($i=0;$i -lt 6;$i++){$Route.routes+=@{id=$Ids[$i];points=@(@{x=(80+$i*80);y=1000},@{x=360;y=600})}}
    $RoutePath=Join-Path $Root 'south-route-projection.json';$Route | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $RoutePath
    Assert-SouthRouteProjection $Root
    $Route.image_sha256=('f'*64);$Route | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $RoutePath;Reject {Assert-SouthRouteProjection $Root};$Route.image_sha256=$Manifest.images[0].sha256
    $Route.routes[5].id='rest-cross';$Route | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $RoutePath;Reject {Assert-SouthRouteProjection $Root};$Route.routes[5].id='garden-loop'
    $Route.routes[0].points[0].x=1000;$Route | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $RoutePath;Reject {Assert-SouthRouteProjection $Root}
    $Manifest.gitRevision=('d'*40);Reject {Assert-SouthCaptureIdentity $Info $Manifest $Root};$Manifest.gitRevision=$Info.gitRevision
    [IO.File]::AppendAllText((Join-Path $Root $Names[0]),'changed');Reject {Assert-SouthCaptureIdentity $Info $Manifest $Root};[IO.File]::WriteAllBytes((Join-Path $Root $Names[0]),$Header)
    Remove-Item -LiteralPath (Join-Path $Root $Names[5]);Reject {Get-SouthCaptureEntries $Root};[IO.File]::WriteAllBytes((Join-Path $Root $Names[5]),$Header)
    [IO.File]::WriteAllBytes((Join-Path $Root 'extra.png'),$Header);Reject {Get-SouthCaptureEntries $Root}
    Write-Host 'South native6 identity/dimensions/changed/missing/extra frames and actual-image-bound routes checked; synthetic files, no Unity/Pixel.'
} finally {if(Test-Path $Root){Remove-Item -LiteralPath $Root -Recurse -Force}}

# Check real prerequisite semantics in a temporary copy of only the bound evidence.
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-south-package-'+[guid]::NewGuid())
function Copy-BoundFile([string]$Relative) {
    $Target=Join-Path $Root $Relative;New-Item -ItemType Directory -Path (Split-Path -Parent $Target) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $Repo $Relative) -Destination $Target
}
try {
    $LockPath='References/VisualLock/approved-design-2026-10-21.json';Copy-BoundFile $LockPath
    $Lock=Get-Content -LiteralPath (Join-Path $Repo $LockPath) -Raw | ConvertFrom-Json
    foreach($File in $Lock.files){Copy-BoundFile $File.path}
    $Previous='References/Review/east-florist-2026-10-22-accepted';Copy-BoundFile "$Previous/native-review.json"
    $Record=Get-Content -LiteralPath (Join-Path $Repo "$Previous/native-review.json") -Raw | ConvertFrom-Json
    foreach($Image in $Record.images){Copy-BoundFile "$Previous/$($Image.file)"}
    $StagePath='References/VisualLock/stage-acceptance-2026-10-21.json';Copy-BoundFile $StagePath
    $Stage=Get-Content -LiteralPath (Join-Path $Root $StagePath) -Raw | ConvertFrom-Json
    foreach($Image in $Stage.user_review_checkpoints.north_quest.images){Copy-BoundFile $Image.file}
    $ConditionsPath=$Stage.user_review_checkpoints.north_quest.conditions_record;Copy-BoundFile $ConditionsPath
    Copy-BoundFile 'Assets/Mofumachi/Presentation/TownNorthMasterLayout.cs'
    Assert-SouthReviewPrerequisites $Root
    $ConditionFile=Join-Path $Root $ConditionsPath;$OriginalCondition=[IO.File]::ReadAllBytes($ConditionFile)
    $Conditions=Get-Content -LiteralPath $ConditionFile -Raw | ConvertFrom-Json;$Conditions.circulation_verified=$false
    $Conditions | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ConditionFile;Reject {Assert-SouthReviewPrerequisites $Root};[IO.File]::WriteAllBytes($ConditionFile,$OriginalCondition)
    $Stage.user_review_checkpoints.north_quest.user_approved=$false;$Stage | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $Root $StagePath);Reject {Assert-SouthReviewPrerequisites $Root}
    Copy-BoundFile $StagePath
    $Layout=Join-Path $Root 'Assets/Mofumachi/Presentation/TownNorthMasterLayout.cs';[IO.File]::AppendAllText($Layout,'changed');Reject {Assert-SouthReviewPrerequisites $Root};Copy-BoundFile 'Assets/Mofumachi/Presentation/TownNorthMasterLayout.cs'
    $NorthJpeg=$Stage.user_review_checkpoints.north_quest.images[0].file;[IO.File]::AppendAllText((Join-Path $Root $NorthJpeg),'changed');Reject {Assert-SouthReviewPrerequisites $Root};Copy-BoundFile $NorthJpeg
    foreach($Relative in Get-CentralInputPaths) {
        $Path=Join-Path $Root $Relative
        if(-not(Test-Path -LiteralPath $Path)){New-Item -ItemType Directory -Path (Split-Path -Parent $Path) -Force | Out-Null;[IO.File]::WriteAllText($Path,'synthetic input')}
    }
    Copy-BoundFile 'References/VisualLock/baseline-native-2026-10-21.json'
    foreach($Level in @(1,3,6)){Copy-BoundFile "References/Review/south-before-2026-10-21/screenshots/04-town-lv$Level-South.png"}
    $Run=Join-Path $Root 'Builds/SouthReview/synthetic';$Screens=Join-Path $Run 'screenshots';$SouthScreens=Join-Path $Screens 'south';New-Item -ItemType Directory -Path $SouthScreens -Force | Out-Null
    foreach($Level in @(1,3,6)) {
        [IO.File]::WriteAllBytes((Join-Path $Screens "04-town-lv$Level-Central.png"),$Header)
        Copy-Item -LiteralPath (Join-Path $Repo "References/Review/north-quest-2026-10-23-mosaic-candidate/central/before-04-town-lv$Level-Central.png") -Destination (Join-Path $Run "before-04-town-lv$Level-Central.png")
    }
    Copy-Item -LiteralPath (Join-Path $Repo $Lock.reference) -Destination (Join-Path $Run 'approved-design.png')
    [IO.File]::WriteAllBytes((Join-Path $Run 'central-before-lock-candidate.jpg'),[byte[]]@(5,6,7))
    $Digest=Get-CentralInputFingerprint $Root;$Info=@{gitRevision=('a'*40);productFingerprint=('b'*64);centralInputFingerprint=$Digest}
    $Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'central-review-info.json')
    @{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Digest;images=@(Get-CentralCaptureEntries $Screens)} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Run 'capture-manifest.json')
    @{candidate_revision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Digest;native_pairs_verified=3;native_unity=$true;approved_design_sha256=(Get-FileHash (Join-Path $Repo $Lock.reference) -Algorithm SHA256).Hash.ToLowerInvariant();comparison_sha256=(Get-FileHash (Join-Path $Run 'central-before-lock-candidate.jpg') -Algorithm SHA256).Hash.ToLowerInvariant()} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'central-adoption.json')
    foreach($Name in @('central-playmode.xml','central-playmode.log')){Set-Content -LiteralPath (Join-Path $Run $Name) -Value 'synthetic pipeline metadata, not native tests'}
    foreach($Area in @('west','east','north')) {
        $Directory=Join-Path $Run $Area;$Frames=Join-Path $Directory 'screenshots';New-Item -ItemType Directory -Path $Frames -Force | Out-Null
        $District=(Get-Culture).TextInfo.ToTitleCase($Area)
        $FrameNames=@("04-town-lv1-$District.png","04-town-lv3-$District.png","04-town-lv6-$District.png","$Area-connection.png","$Area-walking.png","$Area-door-arrived.png")
        foreach($Name in $FrameNames){[IO.File]::WriteAllBytes((Join-Path $Frames $Name),$Header)}
        $Entries=if($Area -eq 'west'){@(Get-WestCaptureEntries $Frames)}elseif($Area -eq 'east'){@(Get-EastCaptureEntries $Frames)}else{@(Get-NorthCaptureEntries $Frames)}
        $Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Directory "$Area-review-info.json")
        @{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Digest;images=$Entries} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Directory "$Area-capture-manifest.json")
    }
    foreach($Name in $Names){[IO.File]::WriteAllBytes((Join-Path $SouthScreens $Name),$Header)}
    Set-Content -LiteralPath (Join-Path $SouthScreens 'capture-world.txt') -Value 'South projected coordinates'
    $Route.image_sha256=(Get-FileHash (Join-Path $SouthScreens $Names[0]) -Algorithm SHA256).Hash.ToLowerInvariant();$Route.routes[0].points[0].x=80;$Route.routes[5].id='garden-loop'
    $Route | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $SouthScreens 'south-route-projection.json')
    function Write-SouthComparisonJpeg([string]$Directory){[IO.File]::WriteAllBytes((Join-Path $Directory 'south-before-candidate.jpg'),[byte[]]@(5,6,7))}
    function Write-SouthRoutesJpeg([string]$Directory){Assert-SouthRouteProjection $Directory;[IO.File]::WriteAllBytes((Join-Path $Directory 'south-walking-routes.jpg'),[byte[]]@(5,6,7))}
    $Wrong=@{gitRevision=$Info.gitRevision;productFingerprint=('d'*64);centralInputFingerprint=$Digest};$WestInfo=Join-Path $Run 'west/west-review-info.json';$Wrong | ConvertTo-Json | Set-Content -LiteralPath $WestInfo
    Reject {Write-SouthReview $Root $Run};$Info | ConvertTo-Json | Set-Content -LiteralPath $WestInfo
    $Missing=Join-Path $Root 'References/Review/south-before-2026-10-21/screenshots/04-town-lv3-South.png';Remove-Item -LiteralPath $Missing
    Reject {Write-SouthReview $Root $Run};if(Test-Path (Join-Path $Run 'south-garden-review.zip')){throw 'Missing South Before produced an archive.'}
    Copy-BoundFile 'References/Review/south-before-2026-10-21/screenshots/04-town-lv3-South.png'
    Move-Item -LiteralPath (Join-Path $Run 'south/screenshots') -Destination $SouthScreens
    Move-Item -LiteralPath (Join-Path $Run 'south/south-route-projection.json') -Destination (Join-Path $SouthScreens 'south-route-projection.json')
    Write-SouthReview $Root $Run
    $Expanded=Join-Path $Root 'expanded';Expand-Archive -LiteralPath (Join-Path $Run 'south-garden-review.zip') -DestinationPath $Expanded
    foreach($Name in $Names){if(-not(Test-Path (Join-Path $Expanded $Name))){throw "Archive lost South frame: $Name"}}
    foreach($Name in @('south-walking-routes.jpg','south-route-projection.json','south-before-candidate.jpg','capture-world.txt')){if(-not(Test-Path (Join-Path $Expanded $Name))){throw "Archive lost source-bound evidence: $Name"}}
    $Meta=Get-Content -LiteralPath (Join-Path $Expanded 'south-adoption.json') -Raw | ConvertFrom-Json
    if($Meta.adopted -ne $false -or $Meta.pixel_verified -ne $false -or $Meta.native_pairs_verified -ne 3){throw 'South export adopted the candidate or lost native pairs.'}
    Write-Host 'South conditional approval/layout hash, real Before/hash checks, companion identities and archive preservation passed. JPEG drawing stubbed; synthetic candidates, no native/device execution.'
} finally {if(Test-Path $Root){Remove-Item -LiteralPath $Root -Recurse -Force}}
