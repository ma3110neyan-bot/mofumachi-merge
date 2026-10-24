# Central-only review helpers. Loading this file never renders, builds or adopts.
function Get-CentralInputPaths {
    'Assets/Mofumachi/Presentation/TownSouthEntryBlendLayout.cs'
    'Assets/Mofumachi/Presentation/TownSouthEntryBlendGraphic.cs'
    'Assets/Mofumachi/Tests/PlayMode/TownReviewRoutes.cs'
    'Assets/Mofumachi/Resources/Mofumachi/TownSouthGround.png'
    'Assets/Mofumachi/Resources/Mofumachi/TownSouthGround.png.meta'
    # Shared files are intentional: later district-only commits may retain the
    # review, but changing projection/scenery/navigation requires central review.
    foreach($Name in @('TownCentralMasterLayout','TownCentralGroundGraphic','TownWestMasterLayout','TownWestGroundGraphic','TownEastMasterLayout','TownEastGroundGraphic','TownNorthMasterLayout','TownNorthGroundGraphic','TownSouthMasterLayout','TownSouthGroundGraphic','TownFountainGraphic','TownContinuousWorldView','TownWalkableArea','TownSceneryView','TownSceneryLayout','TownVillageDressing','TownVillageDetailGraphic','TownWorldSurface','TownWorldLayout','TownWorldMap','TownWorldWalkModel','TownGroundGraphic','TownLifeView','TownActor','TownStreetHud','TownGrowthView','TownGrowthPresentation','TownGrowthComparisonView','TownFacilityView')) {
        'Assets/Mofumachi/Presentation/'+$Name+'.cs'
    }
    foreach($Name in @('TownCentralGround.png','TownCentralGround.png.meta','TownCentralFountain.png','TownCentralFountain.png.meta','TownWestGround.png','TownWestGround.png.meta')) {'Assets/Mofumachi/Resources/Mofumachi/'+$Name}
    foreach($Name in @('TownDeliveryParcels.png','TownDeliveryParcels.png.meta','TownNorthCourtMosaic.png','TownNorthCourtMosaic.png.meta','TownDecorationAtlas.png','TownDecorationAtlas.png.meta')) {'Assets/Mofumachi/Resources/Mofumachi/'+$Name}
    'Assets/Mofumachi/Presentation/TownFacilityEntry.cs'
    'Assets/Mofumachi/Tests/PlayMode/ExperienceCaptureTests.cs'
    'Assets/Mofumachi/Tests/PlayMode/CaptureWorldTrace.cs'
    'Assets/Mofumachi/Tests/PlayMode/CaptureWorldTrace.cs.meta'
    'References/VisualLock/approved-design-2026-10-21.json'
    foreach($Name in @('TownBoundaryGardenLayout.cs','TownBoundaryGardenLayout.cs.meta','TownBoundaryGardenGraphic.cs','TownBoundaryGardenGraphic.cs.meta')) {'Assets/Mofumachi/Presentation/'+$Name}
}
function Get-CentralInputFingerprint([string]$RepositoryPath,[hashtable]$ReviewedHashes=@{}) {
    $Lines=foreach($Relative in Get-CentralInputPaths) {
        if($ReviewedHashes.ContainsKey($Relative) -and $null -eq $ReviewedHashes[$Relative]){continue}
        $Path=Join-Path $RepositoryPath $Relative
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "Missing central dependency: $Relative"}
        $Bytes=if($Relative.EndsWith('.png')){[IO.File]::ReadAllBytes($Path)}else{[Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($Path).Replace("`r`n","`n"))}
        $Hash=if($ReviewedHashes.ContainsKey($Relative)){$ReviewedHashes[$Relative]}else{Get-VisualLockDigest $Bytes}
        $Relative+':'+$Hash
    }
    Get-VisualLockDigest ([Text.Encoding]::UTF8.GetBytes(($Lines -join "`n")))
}
function Assert-CentralReviewEvidence([string]$RepositoryPath,[string]$RunDirectory,[switch]$HistoricalApproval) {
    $Info=Get-Content -LiteralPath (Join-Path $RunDirectory 'central-review-info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $Manifest=Get-Content -LiteralPath (Join-Path $RunDirectory 'capture-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    Assert-CentralCaptureIdentity $Info $Manifest (Join-Path $RunDirectory 'screenshots')
    $Expected=if($HistoricalApproval){Get-ReviewedCentralInputFingerprint $RepositoryPath}else{Get-CentralInputFingerprint $RepositoryPath}
    if($Info.centralInputFingerprint -notmatch '^[a-fA-F0-9]{64}$' -or $Manifest.centralInputFingerprint -ne $Info.centralInputFingerprint -or $Expected -ne $Info.centralInputFingerprint){throw 'Central inputs changed since the reviewed native capture.'}
    $Evidence=Get-Content -LiteralPath (Join-Path $RunDirectory 'central-adoption.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if($Evidence.candidate_revision -ne $Info.gitRevision -or $Evidence.productFingerprint -ne $Info.productFingerprint -or $Evidence.centralInputFingerprint -ne $Info.centralInputFingerprint -or $Evidence.native_unity -isnot [bool] -or $Evidence.native_unity -ne $true -or [int]$Evidence.native_pairs_verified -ne 3){throw 'Central comparison evidence is incomplete or has different source identity.'}
    $Lock=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/approved-design-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $ReferenceHash=(Get-FileHash -LiteralPath (Join-Path $RepositoryPath $Lock.reference) -Algorithm SHA256).Hash.ToLowerInvariant()
    if($Evidence.approved_design_sha256 -ne $ReferenceHash -or (Get-FileHash -LiteralPath (Join-Path $RunDirectory 'approved-design.png') -Algorithm SHA256).Hash -ne $ReferenceHash){throw 'Central comparison uses a different approved design.'}
    $Comparison=Join-Path $RunDirectory 'central-before-lock-candidate.jpg'
    if($Evidence.comparison_sha256 -notmatch '^[a-fA-F0-9]{64}$' -or (Get-FileHash -LiteralPath $Comparison -Algorithm SHA256).Hash -ne $Evidence.comparison_sha256){throw 'Central comparison JPEG is missing or changed.'}
    $Baseline=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/baseline-native-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach($Image in $Manifest.images) {
        $Expected=@($Baseline.capture.images | Where-Object {$_.file -eq $Image.file})
        if($Expected.Count -ne 1 -or (Get-FileHash -LiteralPath (Join-Path $RunDirectory ('before-'+$Image.file)) -Algorithm SHA256).Hash -ne $Expected[0].sha256){throw 'Central Before image is missing or differs from the actual baseline.'}
    }
    return $Info
}
function Get-CentralCaptureEntries([string]$Directory) {
    $Expected=@('04-town-lv1-Central.png','04-town-lv3-Central.png','04-town-lv6-Central.png')
    $Files=@(Get-ChildItem -LiteralPath $Directory -Filter '*.png' -File)
    if($Files.Count -ne 3){throw 'Central review requires exactly Lv1, Lv3 and Lv6 native PNGs.'}
    foreach($Name in $Expected) {
        $Path=Join-Path $Directory $Name
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "Missing central native image: $Name"}
        $Stream=[IO.File]::OpenRead($Path)
        try{$Header=New-Object byte[] 24;$Read=$Stream.Read($Header,0,24)}finally{$Stream.Dispose()}
        if($Read -ne 24 -or (Get-Item -LiteralPath $Path).Length -le 24 -or ($Header[0..7] -join ',') -ne '137,80,78,71,13,10,26,10' -or [Text.Encoding]::ASCII.GetString($Header,12,4) -ne 'IHDR'){throw "Invalid central PNG: $Name"}
        $Width=([int64]$Header[16]*16777216)+([int64]$Header[17]*65536)+([int64]$Header[18]*256)+$Header[19]
        $Height=([int64]$Header[20]*16777216)+([int64]$Header[21]*65536)+([int64]$Header[22]*256)+$Header[23]
        if($Width -ne 720 -or $Height -ne 1280){throw "Central PNG must be 720x1280: $Name"}
        [ordered]@{file=$Name;bytes=(Get-Item -LiteralPath $Path).Length;sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
    }
}
function Assert-CentralCaptureIdentity($Info,$Manifest,[string]$Directory) {
    if($Info.gitRevision -notmatch '^[a-fA-F0-9]{40}$' -or $Info.productFingerprint -notmatch '^[a-fA-F0-9]{64}$' -or $Manifest.gitRevision -ne $Info.gitRevision -or $Manifest.productFingerprint -ne $Info.productFingerprint){throw 'Central images and review have different or missing source identity.'}
    $Actual=@(Get-CentralCaptureEntries $Directory)
    if(@($Manifest.images).Count -ne 3){throw 'Central image manifest is incomplete.'}
    foreach($File in $Actual) {
        $Recorded=@($Manifest.images | Where-Object {$_.file -eq $File.file})
        if($Recorded.Count -ne 1 -or $Recorded[0].sha256 -ne $File.sha256){throw "Central image has changed: $($File.file)"}
    }
}
function Write-CentralComparisonJpeg([string]$Directory,[string]$Reference) {
    # Windows ships System.Drawing. This is a review-document export, never a
    # repaint of the captures; the source PNG bytes stay separately in the ZIP.
    Add-Type -AssemblyName System.Drawing
    $Bitmap=New-Object Drawing.Bitmap 2160,4250
    $Graphics=[Drawing.Graphics]::FromImage($Bitmap)
    $Font=New-Object Drawing.Font 'Arial',26
    $Small=New-Object Drawing.Font 'Arial',20
    $Brush=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(80,47,57))
    $Lock=[Drawing.Image]::FromFile($Reference)
    try {
        $Graphics.Clear([Drawing.Color]::FromArgb(255,247,239))
        $Graphics.DrawString('CENTRAL MASTER REVIEW / NOT ACCEPTED / NO APK / PIXEL PENDING',$Font,$Brush,18,12)
        for($Row=0;$Row -lt 3;$Row++) {
            $Level=@(1,3,6)[$Row];$Y=80+$Row*1380
            $Graphics.DrawString("Before / Unity Lv.$Level",$Font,$Brush,18,$Y)
            $Graphics.DrawString('Approved Visual Lock (design)',$Font,$Brush,738,$Y)
            $Graphics.DrawString("Candidate / Unity Lv.$Level",$Font,$Brush,1458,$Y)
            $Name="04-town-lv$Level-Central.png"
            foreach($Column in @(0,2)) {
                $Path=if($Column -eq 0){Join-Path $Directory ('before-'+$Name)}else{Join-Path (Join-Path $Directory 'screenshots') $Name}
                $Image=[Drawing.Image]::FromFile($Path)
                try{$Graphics.DrawImage($Image,[Drawing.Rectangle]::new($Column*720,$Y+48,720,1280))}finally{$Image.Dispose()}
            }
            # Only the central phone portion of the immutable approved board.
            $Graphics.DrawImage($Lock,[Drawing.Rectangle]::new(720,$Y+48,720,1280),[Drawing.Rectangle]::new(27,146,572,1042),[Drawing.GraphicsUnit]::Pixel)
        }
        $Graphics.DrawString('Original PNGs included. Fixture states, not player saves. Visual/operation acceptance still pending.',$Small,$Brush,18,4220)
        $Encoder=[Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object {$_.MimeType -eq 'image/jpeg'}
        $Parameters=New-Object Drawing.Imaging.EncoderParameters 1
        try {
            $Parameters.Param[0]=[Drawing.Imaging.EncoderParameter]::new([Drawing.Imaging.Encoder]::Quality,[long]92)
            $Bitmap.Save((Join-Path $Directory 'central-before-lock-candidate.jpg'),$Encoder,$Parameters)
        } finally {$Parameters.Dispose()}
    } finally {$Lock.Dispose();$Brush.Dispose();$Small.Dispose();$Font.Dispose();$Graphics.Dispose();$Bitmap.Dispose()}
}
function Write-CentralReview([string]$RepositoryPath,[string]$RunDirectory) {
    Assert-ApprovedVisualLock $RepositoryPath
    $Info=Get-Content -LiteralPath (Join-Path $RunDirectory 'central-review-info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $Manifest=Get-Content -LiteralPath (Join-Path $RunDirectory 'capture-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    Assert-CentralCaptureIdentity $Info $Manifest (Join-Path $RunDirectory 'screenshots')
    $Baseline=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/baseline-native-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $Before=Join-Path $RepositoryPath ('Builds/Android/'+$Baseline.run)
    if(Test-Path -LiteralPath (Join-Path $Before 'screenshots')){$Before=Join-Path $Before 'screenshots'}
    $Pairs=0
    foreach($File in $Manifest.images) {
        $Path=Join-Path $Before $File.file
        $Expected=@($Baseline.capture.images | Where-Object {$_.file -eq $File.file})
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){continue}
        if($Expected.Count -ne 1 -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Expected[0].sha256){throw 'Before central image does not match the recorded real Unity baseline.'}
        Copy-Item -LiteralPath $Path -Destination (Join-Path $RunDirectory ('before-'+$File.file));$Pairs++
    }
    if($Info.centralInputFingerprint -notmatch '^[a-fA-F0-9]{64}$' -or $Manifest.centralInputFingerprint -ne $Info.centralInputFingerprint -or (Get-CentralInputFingerprint $RepositoryPath) -ne $Info.centralInputFingerprint){throw 'Central inputs changed during capture.'}
    $Reference=Join-Path $RepositoryPath 'References/Preview/design-first-2026-10-21/design-proposal.png'
    Copy-Item -LiteralPath $Reference -Destination (Join-Path $RunDirectory 'approved-design.png')
    $ComparisonHash=$null
    if($Pairs -eq 3){Write-CentralComparisonJpeg $RunDirectory $Reference;$ComparisonHash=(Get-FileHash -LiteralPath (Join-Path $RunDirectory 'central-before-lock-candidate.jpg') -Algorithm SHA256).Hash.ToLowerInvariant()}
    [ordered]@{candidate_revision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Info.centralInputFingerprint;comparison_sha256=$ComparisonHash;approved_design_sha256=(Get-FileHash -LiteralPath $Reference -Algorithm SHA256).Hash.ToLowerInvariant();native_pairs_verified=$Pairs;native_unity=$true;visual=$null;town_density=$null;walking=$null;pixel_verified=$false;adopted=$false;reason='Review central against the approved design before advancing to west/east/north/south/Merge. No APK was built.'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $RunDirectory 'central-adoption.json') -Encoding UTF8
    $Files=@(Get-ChildItem -LiteralPath $RunDirectory -File | Where-Object {$_.Extension -ne '.zip'})+@(Get-ChildItem -LiteralPath (Join-Path $RunDirectory 'screenshots') -File)
    $Archive=Join-Path $RunDirectory 'central-master-review.zip'
    Compress-Archive -LiteralPath $Files.FullName -DestinationPath $Archive
    if((Get-Item -LiteralPath $Archive).Length -gt 32MB){throw 'Central review archive exceeds 32 MiB; split review evidence before upload.'}
    Write-Host "Central review: $Archive. Baseline pairs: $Pairs/3. No APK; central/Pixel acceptance pending."
}
