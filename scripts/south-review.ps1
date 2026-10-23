# Source-bound south native review. Dot-sourcing never builds, renders or adopts.
function Get-SouthCaptureEntries([string]$Directory) {
    $Expected=@('04-town-lv1-South.png','04-town-lv3-South.png','04-town-lv6-South.png','south-connection.png','south-walking.png','south-rest-arrived.png')
    $Files=@(Get-ChildItem -LiteralPath $Directory -Filter '*.png' -File)
    if($Files.Count -ne $Expected.Count){throw 'South review requires exactly three levels, the connection, walking and rest arrival.'}
    foreach($Name in $Expected) {
        $Path=Join-Path $Directory $Name
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "Missing south native image: $Name"}
        $Stream=[IO.File]::OpenRead($Path)
        try{$Header=New-Object byte[] 24;$Read=$Stream.Read($Header,0,24)}finally{$Stream.Dispose()}
        if($Read -ne 24 -or (Get-Item -LiteralPath $Path).Length -le 24 -or ($Header[0..7] -join ',') -ne '137,80,78,71,13,10,26,10' -or [Text.Encoding]::ASCII.GetString($Header,12,4) -ne 'IHDR'){throw "Invalid south PNG: $Name"}
        $Width=([int64]$Header[16]*16777216)+([int64]$Header[17]*65536)+([int64]$Header[18]*256)+$Header[19]
        $Height=([int64]$Header[20]*16777216)+([int64]$Header[21]*65536)+([int64]$Header[22]*256)+$Header[23]
        if($Width -ne 720 -or $Height -ne 1280){throw "South PNG must be 720x1280: $Name"}
        [ordered]@{file=$Name;bytes=(Get-Item -LiteralPath $Path).Length;sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
    }
}
function Assert-SouthCaptureIdentity($Info,$Manifest,[string]$Directory) {
    if($Info.gitRevision -notmatch '^[a-fA-F0-9]{40}$' -or $Info.productFingerprint -notmatch '^[a-fA-F0-9]{64}$' -or $Info.centralInputFingerprint -notmatch '^[a-fA-F0-9]{64}$' -or $Manifest.gitRevision -ne $Info.gitRevision -or $Manifest.productFingerprint -ne $Info.productFingerprint -or $Manifest.centralInputFingerprint -ne $Info.centralInputFingerprint){throw 'South evidence has different or missing source identity.'}
    $Actual=@(Get-SouthCaptureEntries $Directory)
    if(@($Manifest.images).Count -ne 6){throw 'South image manifest incomplete.'}
    foreach($Image in $Actual) {
        $Recorded=@($Manifest.images | Where-Object {$_.file -eq $Image.file})
        if($Recorded.Count -ne 1 -or $Recorded[0].sha256 -ne $Image.sha256){throw "South image changed: $($Image.file)"}
    }
}

function Assert-SouthRouteProjection([string]$Directory) {
    Assert-TownRouteProjection $Directory 'South' @('gazebo-left','gazebo-right','left-rest','right-rest','rest-cross','garden-loop')
}
function Write-SouthRoutesJpeg([string]$Directory) {
    Write-TownRoutesJpeg $Directory 'South' '南の庭園' @{'gazebo-left'='東屋 ↔ 左小道';'gazebo-right'='東屋 ↔ 右小道';'left-rest'='左の休憩場所';'right-rest'='右の休憩場所';'rest-cross'='休憩場所の横断';'garden-loop'='庭園の回遊'}
}
function Assert-SouthReviewPrerequisites([string]$RepositoryPath) {
    Assert-NorthReviewPrerequisites $RepositoryPath
    Assert-TownCheckpointApproved $RepositoryPath 'north_quest'
    $Stage=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/stage-acceptance-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $Approval=$Stage.user_review_checkpoints.north_quest
    $Relative=[string]$Approval.conditions_record
    if(-not $Relative -or [IO.Path]::IsPathRooted($Relative) -or $Relative -match '(^|[/\\])\.\.([/\\]|$)' -or $Relative.Contains(':')){throw 'Conditional North approval needs its repository-relative adjustment record.'}
    $Conditions=Get-Content -LiteralPath (Join-Path $RepositoryPath $Relative) -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach($Axis in @('circulation_verified','level_three_decoration_verified','south_start_authorized_by_user_after_these_conditions')) {
        if($Conditions.$Axis -isnot [bool] -or $Conditions.$Axis -ne $true){throw "North condition incomplete: $Axis"}
    }
    if($Conditions.approved_base_revision -ne $Approval.reviewed_candidate -or $Conditions.local_presentation_tests.passed -ne 15 -or $Conditions.local_presentation_tests.total -ne 15 -or $Conditions.local_presentation_tests.failed -ne 0){throw 'North conditional authorization lacks its exact CPU verification record.'}
    $Path=Join-Path $RepositoryPath 'Assets/Mofumachi/Presentation/TownNorthMasterLayout.cs'
    $Bytes=[Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($Path).Replace("`r`n","`n"))
    if((Get-VisualLockDigest $Bytes) -ne $Conditions.north_layout_normalized_sha256){throw 'North adjusted layout changed since conditions were checked.'}
}
function Write-SouthComparisonJpeg([string]$Directory) {
    Add-Type -AssemblyName System.Drawing
    $Bitmap=New-Object Drawing.Bitmap 1440,4250;$Graphics=[Drawing.Graphics]::FromImage($Bitmap)
    $Font=New-Object Drawing.Font 'Meiryo',24;$Brush=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(80,47,57))
    try {
        $Graphics.Clear([Drawing.Color]::FromArgb(255,247,239))
        $Graphics.DrawString('南の庭園／Before・Candidate／ユーザー承認待ち',$Font,$Brush,16,12)
        for($Row=0;$Row -lt 3;$Row++) {
            $Level=@(1,3,6)[$Row];$Y=80+$Row*1380;$Name="04-town-lv$Level-South.png"
            $Graphics.DrawString("Before / Unity Lv.$Level",$Font,$Brush,16,$Y);$Graphics.DrawString("Candidate / Unity Lv.$Level",$Font,$Brush,736,$Y)
            $Paths=@((Join-Path $Directory ('before-'+$Name)),(Join-Path (Join-Path $Directory 'screenshots') $Name))
            for($Column=0;$Column -lt 2;$Column++) {$Image=[Drawing.Image]::FromFile($Paths[$Column]);try{$Graphics.DrawImage($Image,[Drawing.Rectangle]::new($Column*720,$Y+48,720,1280))}finally{$Image.Dispose()}}
        }
        $Encoder=[Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object {$_.MimeType -eq 'image/jpeg'}
        $Parameters=New-Object Drawing.Imaging.EncoderParameters 1
        try{$Parameters.Param[0]=[Drawing.Imaging.EncoderParameter]::new([Drawing.Imaging.Encoder]::Quality,[long]92);$Bitmap.Save((Join-Path $Directory 'south-before-candidate.jpg'),$Encoder,$Parameters)}finally{$Parameters.Dispose()}
    } finally {$Brush.Dispose();$Font.Dispose();$Graphics.Dispose();$Bitmap.Dispose()}
}
function Write-SouthReview([string]$RepositoryPath,[string]$RunDirectory) {
    Assert-SouthReviewPrerequisites $RepositoryPath
    $CentralInfo=Assert-CentralReviewEvidence $RepositoryPath $RunDirectory
    foreach($Area in @('west','east','north')) {
        $Directory=Join-Path $RunDirectory $Area
        $Info=Get-Content -LiteralPath (Join-Path $Directory "$Area-review-info.json") -Raw -Encoding UTF8 | ConvertFrom-Json
        $Manifest=Get-Content -LiteralPath (Join-Path $Directory "$Area-capture-manifest.json") -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach($Key in @('gitRevision','productFingerprint','centralInputFingerprint')){if($Info.$Key -ne $CentralInfo.$Key){throw "South companion identity mismatch: $Area/$Key"}}
        if($Area -eq 'west'){Assert-WestCaptureIdentity $Info $Manifest (Join-Path $Directory 'screenshots')}
        elseif($Area -eq 'east'){Assert-EastCaptureIdentity $Info $Manifest (Join-Path $Directory 'screenshots')}
        else{Assert-NorthCaptureIdentity $Info $Manifest (Join-Path $Directory 'screenshots')}
    }
    $South=Join-Path $RunDirectory 'south';New-Item -ItemType Directory -Path $South -Force | Out-Null
    Move-Item -LiteralPath (Join-Path $RunDirectory 'screenshots/south') -Destination (Join-Path $South 'screenshots')
    $Entries=@(Get-SouthCaptureEntries (Join-Path $South 'screenshots'))
    Move-Item -LiteralPath (Join-Path $South 'screenshots/south-route-projection.json') -Destination (Join-Path $South 'south-route-projection.json')
    Assert-SouthRouteProjection $South
    $Baseline=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/baseline-native-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach($Level in @(1,3,6)) {
        $Name="04-town-lv$Level-South.png"
        $Path=Join-Path $RepositoryPath ("Builds/Android/$($Baseline.run)/screenshots/$Name")
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){$Path=Join-Path $RepositoryPath "References/Review/south-before-2026-10-21/screenshots/$Name"}
        $Expected=@($Baseline.capture.images | Where-Object {$_.file -eq $Name})
        if($Expected.Count -ne 1 -or -not(Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Expected[0].sha256){throw "Missing or changed native South Before: $Name"}
        Copy-Item -LiteralPath $Path -Destination (Join-Path $South ('before-'+$Name))
    }
    $Lock=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/approved-design-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    Copy-Item -LiteralPath (Join-Path $RepositoryPath $Lock.reference) -Destination (Join-Path $South 'approved-design.png')
    foreach($Name in @('central-playmode.xml','central-playmode.log','central-review-info.json')){Copy-Item -LiteralPath (Join-Path $RunDirectory $Name) -Destination (Join-Path $South $Name)}
    foreach($Area in @('west','east','north')){Copy-Item -LiteralPath (Join-Path $RunDirectory "$Area/$Area-review-info.json") -Destination (Join-Path $South "$Area-review-info.json")}
    $Info=[ordered]@{gitRevision=$CentralInfo.gitRevision;productFingerprint=$CentralInfo.productFingerprint;centralInputFingerprint=$CentralInfo.centralInputFingerprint;scope='South garden candidate only; companions for regression, not five-area integration';nativeCaptureCount=6;apk_generated=$false;pixel_verified=$false;adopted=$false}
    $Manifest=[ordered]@{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Info.centralInputFingerprint;width=720;height=1280;images=$Entries}
    Assert-SouthCaptureIdentity $Info $Manifest (Join-Path $South 'screenshots')
    if((Get-CentralInputFingerprint $RepositoryPath) -ne $Info.centralInputFingerprint){throw 'World inputs changed before South export.'}
    $Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $South 'south-review-info.json') -Encoding UTF8
    $Manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $South 'south-capture-manifest.json') -Encoding UTF8
    Write-SouthComparisonJpeg $South;Write-SouthRoutesJpeg $South
    [ordered]@{candidate_revision=$Info.gitRevision;centralInputFingerprint=$Info.centralInputFingerprint;native_pairs_verified=3;native_unity=$true;visual=$null;walking=$null;town_density=$null;pixel_verified=$false;adopted=$false;comparison_sha256=(Get-FileHash -LiteralPath (Join-Path $South 'south-before-candidate.jpg') -Algorithm SHA256).Hash.ToLowerInvariant();route_projection_sha256=(Get-FileHash -LiteralPath (Join-Path $South 'south-route-projection.json') -Algorithm SHA256).Hash.ToLowerInvariant();route_jpeg_sha256=(Get-FileHash -LiteralPath (Join-Path $South 'south-walking-routes.jpg') -Algorithm SHA256).Hash.ToLowerInvariant();approved_design_sha256=(Get-FileHash -LiteralPath (Join-Path $South 'approved-design.png') -Algorithm SHA256).Hash.ToLowerInvariant();reason='Review South actual images and unchanged four districts before South user approval; no integration/APK/Pixel.'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $South 'south-adoption.json') -Encoding UTF8
    $Files=@(Get-ChildItem -LiteralPath $South -File)+@(Get-ChildItem -LiteralPath (Join-Path $South 'screenshots') -File)
    $Archive=Join-Path $RunDirectory 'south-garden-review.zip';Compress-Archive -LiteralPath $Files.FullName -DestinationPath $Archive
    if((Get-Item -LiteralPath $Archive).Length -gt 32MB){throw 'South review exceeds32MiB; split evidence before upload.'}
    Write-Host "South candidate: $Archive. Upload same-run5ZIP; no five-area integration,APK or Pixel acceptance."
}
