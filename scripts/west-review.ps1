# Source-bound west native review. Dot-sourcing never builds, renders or adopts.
function Get-WestCaptureEntries([string]$Directory) {
    $Expected=@('04-town-lv1-West.png','04-town-lv3-West.png','04-town-lv6-West.png','west-connection.png','west-walking.png','west-door-arrived.png')
    $Files=@(Get-ChildItem -LiteralPath $Directory -Filter '*.png' -File)
    if($Files.Count -ne $Expected.Count){throw 'West review requires exactly three levels, the connection, walking and doorway.'}
    foreach($Name in $Expected) {
        $Path=Join-Path $Directory $Name
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "Missing west native image: $Name"}
        $Stream=[IO.File]::OpenRead($Path)
        try{$Header=New-Object byte[] 24;$Read=$Stream.Read($Header,0,24)}finally{$Stream.Dispose()}
        if($Read -ne 24 -or (Get-Item -LiteralPath $Path).Length -le 24 -or ($Header[0..7] -join ',') -ne '137,80,78,71,13,10,26,10' -or [Text.Encoding]::ASCII.GetString($Header,12,4) -ne 'IHDR'){throw "Invalid west PNG: $Name"}
        $Width=([int64]$Header[16]*16777216)+([int64]$Header[17]*65536)+([int64]$Header[18]*256)+$Header[19]
        $Height=([int64]$Header[20]*16777216)+([int64]$Header[21]*65536)+([int64]$Header[22]*256)+$Header[23]
        if($Width -ne 720 -or $Height -ne 1280){throw "West PNG must be 720x1280: $Name"}
        [ordered]@{file=$Name;bytes=(Get-Item -LiteralPath $Path).Length;sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
    }
}
function Assert-WestCaptureIdentity($Info,$Manifest,[string]$Directory) {
    if($Info.gitRevision -notmatch '^[a-fA-F0-9]{40}$' -or $Info.productFingerprint -notmatch '^[a-fA-F0-9]{64}$' -or $Info.centralInputFingerprint -notmatch '^[a-fA-F0-9]{64}$' -or $Manifest.gitRevision -ne $Info.gitRevision -or $Manifest.productFingerprint -ne $Info.productFingerprint -or $Manifest.centralInputFingerprint -ne $Info.centralInputFingerprint){throw 'West evidence has different or missing source identity.'}
    $Actual=@(Get-WestCaptureEntries $Directory)
    if(@($Manifest.images).Count -ne 6){throw 'West image manifest incomplete.'}
    foreach($Image in $Actual) {
        $Recorded=@($Manifest.images | Where-Object {$_.file -eq $Image.file})
        if($Recorded.Count -ne 1 -or $Recorded[0].sha256 -ne $Image.sha256){throw "West image changed: $($Image.file)"}
    }
}
function Write-WestComparisonJpeg([string]$Directory) {
    Add-Type -AssemblyName System.Drawing
    $Bitmap=New-Object Drawing.Bitmap 1440,4250
    $Graphics=[Drawing.Graphics]::FromImage($Bitmap)
    $Font=New-Object Drawing.Font 'Arial',26
    $Small=New-Object Drawing.Font 'Arial',20
    $Brush=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(80,47,57))
    try {
        $Graphics.Clear([Drawing.Color]::FromArgb(255,247,239))
        $Graphics.DrawString('WEST CAFE / NATIVE CANDIDATE / NOT ACCEPTED / PIXEL PENDING',$Font,$Brush,16,12)
        for($Row=0;$Row -lt 3;$Row++) {
            $Level=@(1,3,6)[$Row];$Y=80+$Row*1380
            $Graphics.DrawString("Before / Unity Lv.$Level",$Font,$Brush,16,$Y)
            $Graphics.DrawString("Candidate / Unity Lv.$Level",$Font,$Brush,736,$Y)
            $Name="04-town-lv$Level-West.png"
            foreach($Column in @(0,1)) {
                $Path=if($Column -eq 0){Join-Path $Directory ('before-'+$Name)}else{Join-Path (Join-Path $Directory 'screenshots') $Name}
                $Image=[Drawing.Image]::FromFile($Path)
                try{$Graphics.DrawImage($Image,[Drawing.Rectangle]::new($Column*720,$Y+48,720,1280))}finally{$Image.Dispose()}
            }
        }
        $Graphics.DrawString('Original PNGs and separate approved design included. Walking frames have no Before pairing.',$Small,$Brush,16,4220)
        $Encoder=[Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object {$_.MimeType -eq 'image/jpeg'}
        $Parameters=New-Object Drawing.Imaging.EncoderParameters 1
        try{$Parameters.Param[0]=[Drawing.Imaging.EncoderParameter]::new([Drawing.Imaging.Encoder]::Quality,[long]92);$Bitmap.Save((Join-Path $Directory 'west-before-candidate.jpg'),$Encoder,$Parameters)}finally{$Parameters.Dispose()}
    } finally {$Brush.Dispose();$Small.Dispose();$Font.Dispose();$Graphics.Dispose();$Bitmap.Dispose()}
}
function Write-WestReview([string]$RepositoryPath,[string]$RunDirectory) {
    $CentralInfo=Assert-CentralReviewEvidence $RepositoryPath $RunDirectory
    $West=Join-Path $RunDirectory 'west';New-Item -ItemType Directory -Path $West -Force | Out-Null
    Move-Item -LiteralPath (Join-Path $RunDirectory 'screenshots/west') -Destination (Join-Path $West 'screenshots')
    $Entries=@(Get-WestCaptureEntries (Join-Path $West 'screenshots'))
    Assert-BaseVisualLock $RepositoryPath (Join-Path $RepositoryPath 'References/VisualLock/west-cafe-design-2026-10-22.json')
    $Baseline=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/baseline-native-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $Before=Join-Path $RepositoryPath ('Builds/Android/'+$Baseline.run)
    if(Test-Path (Join-Path $Before 'screenshots')){$Before=Join-Path $Before 'screenshots'}
    foreach($Level in @(1,3,6)) {
        $Name="04-town-lv$Level-West.png";$Path=Join-Path $Before $Name
        $Expected=@($Baseline.capture.images | Where-Object {$_.file -eq $Name})
        if($Expected.Count -ne 1 -or -not(Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Expected[0].sha256){throw "Missing or changed native west Before: $Name"}
        Copy-Item -LiteralPath $Path -Destination (Join-Path $West ('before-'+$Name))
    }
    Copy-Item -LiteralPath (Join-Path $RepositoryPath 'References/Preview/west-cafe-2026-10-22/west-cafe-proposal.jpg') -Destination (Join-Path $West 'approved-west-design.jpg')
    foreach($Name in @('central-playmode.xml','central-playmode.log','central-review-info.json')){Copy-Item -LiteralPath (Join-Path $RunDirectory $Name) -Destination (Join-Path $West $Name)}
    $Info=[ordered]@{gitRevision=$CentralInfo.gitRevision;productFingerprint=$CentralInfo.productFingerprint;centralInputFingerprint=$CentralInfo.centralInputFingerprint;nativeCaptureCount=6;scope='west native checkpoint; central recheck in companion ZIP';apk_generated=$false;pixel_verified=$false;adopted=$false}
    $Manifest=[ordered]@{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Info.centralInputFingerprint;width=720;height=1280;images=$Entries}
    Assert-WestCaptureIdentity $Info $Manifest (Join-Path $West 'screenshots')
    if((Get-CentralInputFingerprint $RepositoryPath) -ne $Info.centralInputFingerprint){throw 'World inputs changed before west review export.'}
    $Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $West 'west-review-info.json') -Encoding UTF8
    $Manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $West 'west-capture-manifest.json') -Encoding UTF8
    Write-WestComparisonJpeg $West
    [ordered]@{candidate_revision=$Info.gitRevision;centralInputFingerprint=$Info.centralInputFingerprint;native_pairs_verified=3;native_unity=$true;visual=$null;walking=$null;town_density=$null;pixel_verified=$false;adopted=$false;comparison_sha256=(Get-FileHash -LiteralPath (Join-Path $West 'west-before-candidate.jpg') -Algorithm SHA256).Hash.ToLowerInvariant();approved_design_sha256=(Get-FileHash -LiteralPath (Join-Path $West 'approved-west-design.jpg') -Algorithm SHA256).Hash.ToLowerInvariant();reason='Review central and west actual images before accepting either checkpoint or advancing. No APK.'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $West 'west-adoption.json') -Encoding UTF8
    $Files=@(Get-ChildItem -LiteralPath $West -File)+@(Get-ChildItem -LiteralPath (Join-Path $West 'screenshots') -File)
    $Archive=Join-Path $RunDirectory 'west-cafe-review.zip';Compress-Archive -LiteralPath $Files.FullName -DestinationPath $Archive
    if((Get-Item -LiteralPath $Archive).Length -gt 32MB){throw 'West review exceeds 32 MiB; split review evidence before upload.'}
    Write-Host "West native review: $Archive. Upload this and central-master-review.zip; no APK, Pixel pending."
}
