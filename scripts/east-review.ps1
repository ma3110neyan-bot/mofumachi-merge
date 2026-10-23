# Source-bound east native review. Dot-sourcing never builds, renders or adopts.
function Get-EastCaptureEntries([string]$Directory) {
    $Expected=@('04-town-lv1-East.png','04-town-lv3-East.png','04-town-lv6-East.png','east-connection.png','east-walking.png','east-door-arrived.png')
    $Files=@(Get-ChildItem -LiteralPath $Directory -Filter '*.png' -File)
    if($Files.Count -ne $Expected.Count){throw 'East review requires exactly three levels, the connection, walking and doorway.'}
    foreach($Name in $Expected) {
        $Path=Join-Path $Directory $Name
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "Missing east native image: $Name"}
        $Stream=[IO.File]::OpenRead($Path)
        try{$Header=New-Object byte[] 24;$Read=$Stream.Read($Header,0,24)}finally{$Stream.Dispose()}
        if($Read -ne 24 -or (Get-Item -LiteralPath $Path).Length -le 24 -or ($Header[0..7] -join ',') -ne '137,80,78,71,13,10,26,10' -or [Text.Encoding]::ASCII.GetString($Header,12,4) -ne 'IHDR'){throw "Invalid east PNG: $Name"}
        $Width=([int64]$Header[16]*16777216)+([int64]$Header[17]*65536)+([int64]$Header[18]*256)+$Header[19]
        $Height=([int64]$Header[20]*16777216)+([int64]$Header[21]*65536)+([int64]$Header[22]*256)+$Header[23]
        if($Width -ne 720 -or $Height -ne 1280){throw "East PNG must be 720x1280: $Name"}
        [ordered]@{file=$Name;bytes=(Get-Item -LiteralPath $Path).Length;sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
    }
}
function Assert-EastCaptureIdentity($Info,$Manifest,[string]$Directory) {
    if($Info.gitRevision -notmatch '^[a-fA-F0-9]{40}$' -or $Info.productFingerprint -notmatch '^[a-fA-F0-9]{64}$' -or $Info.centralInputFingerprint -notmatch '^[a-fA-F0-9]{64}$' -or $Manifest.gitRevision -ne $Info.gitRevision -or $Manifest.productFingerprint -ne $Info.productFingerprint -or $Manifest.centralInputFingerprint -ne $Info.centralInputFingerprint){throw 'East evidence has different or missing source identity.'}
    $Actual=@(Get-EastCaptureEntries $Directory)
    if(@($Manifest.images).Count -ne 6){throw 'East image manifest incomplete.'}
    foreach($Image in $Actual) {
        $Recorded=@($Manifest.images | Where-Object {$_.file -eq $Image.file})
        if($Recorded.Count -ne 1 -or $Recorded[0].sha256 -ne $Image.sha256){throw "East image changed: $($Image.file)"}
    }
}
function Write-EastComparisonJpeg([string]$Directory) {
    Add-Type -AssemblyName System.Drawing
    $Bitmap=New-Object Drawing.Bitmap 1440,4250
    $Graphics=[Drawing.Graphics]::FromImage($Bitmap)
    $Font=New-Object Drawing.Font 'Arial',26
    $Small=New-Object Drawing.Font 'Arial',20
    $Brush=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(80,47,57))
    try {
        $Graphics.Clear([Drawing.Color]::FromArgb(255,247,239))
        $Graphics.DrawString('EAST FLORIST / NATIVE CANDIDATE / NOT ACCEPTED / PIXEL PENDING',$Font,$Brush,16,12)
        for($Row=0;$Row -lt 3;$Row++) {
            $Level=@(1,3,6)[$Row];$Y=80+$Row*1380
            $Graphics.DrawString("Before / Unity Lv.$Level",$Font,$Brush,16,$Y)
            $Graphics.DrawString("Candidate / Unity Lv.$Level",$Font,$Brush,736,$Y)
            $Name="04-town-lv$Level-East.png"
            foreach($Column in @(0,1)) {
                $Path=if($Column -eq 0){Join-Path $Directory ('before-'+$Name)}else{Join-Path (Join-Path $Directory 'screenshots') $Name}
                $Image=[Drawing.Image]::FromFile($Path)
                try{$Graphics.DrawImage($Image,[Drawing.Rectangle]::new($Column*720,$Y+48,720,1280))}finally{$Image.Dispose()}
            }
        }
        $Graphics.DrawString('Original PNGs and separate approved design included. Walking frames have no Before pairing.',$Small,$Brush,16,4220)
        $Encoder=[Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object {$_.MimeType -eq 'image/jpeg'}
        $Parameters=New-Object Drawing.Imaging.EncoderParameters 1
        try{$Parameters.Param[0]=[Drawing.Imaging.EncoderParameter]::new([Drawing.Imaging.Encoder]::Quality,[long]92);$Bitmap.Save((Join-Path $Directory 'east-before-candidate.jpg'),$Encoder,$Parameters)}finally{$Parameters.Dispose()}
    } finally {$Brush.Dispose();$Small.Dispose();$Font.Dispose();$Graphics.Dispose();$Bitmap.Dispose()}
}
function Write-EastReview([string]$RepositoryPath,[string]$RunDirectory) {
    $CentralInfo=Assert-CentralReviewEvidence $RepositoryPath $RunDirectory
    $West=Join-Path $RunDirectory 'west'
    $WestInfo=Get-Content -LiteralPath (Join-Path $West 'west-review-info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $WestManifest=Get-Content -LiteralPath (Join-Path $West 'west-capture-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    Assert-WestCaptureIdentity $WestInfo $WestManifest (Join-Path $West 'screenshots')
    foreach($Key in @('gitRevision','productFingerprint','centralInputFingerprint')) {
        if($WestInfo.$Key -ne $CentralInfo.$Key){throw 'Companion central and west source identities differ.'}
    }
    $East=Join-Path $RunDirectory 'east';New-Item -ItemType Directory -Path $East -Force | Out-Null
    Move-Item -LiteralPath (Join-Path $RunDirectory 'screenshots/east') -Destination (Join-Path $East 'screenshots')
    $Entries=@(Get-EastCaptureEntries (Join-Path $East 'screenshots'))
    Assert-ApprovedVisualLock $RepositoryPath
    $Baseline=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/baseline-native-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $Before=Join-Path $RepositoryPath ('Builds/Android/'+$Baseline.run)
    if(Test-Path (Join-Path $Before 'screenshots')){$Before=Join-Path $Before 'screenshots'}
    foreach($Level in @(1,3,6)) {
        $Name="04-town-lv$Level-East.png";$Path=Join-Path $Before $Name
        $Expected=@($Baseline.capture.images | Where-Object {$_.file -eq $Name})
        if($Expected.Count -ne 1 -or -not(Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Expected[0].sha256){throw "Missing or changed native east Before: $Name"}
        Copy-Item -LiteralPath $Path -Destination (Join-Path $East ('before-'+$Name))
    }
    $Lock=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/approved-design-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    Copy-Item -LiteralPath (Join-Path $RepositoryPath $Lock.reference) -Destination (Join-Path $East 'approved-design.png')
    foreach($Name in @('central-playmode.xml','central-playmode.log','central-review-info.json')){Copy-Item -LiteralPath (Join-Path $RunDirectory $Name) -Destination (Join-Path $East $Name)}
    $Info=[ordered]@{gitRevision=$CentralInfo.gitRevision;productFingerprint=$CentralInfo.productFingerprint;centralInputFingerprint=$CentralInfo.centralInputFingerprint;nativeCaptureCount=6;scope='east native checkpoint; central and west recheck in companion ZIPs';apk_generated=$false;pixel_verified=$false;adopted=$false}
    $Manifest=[ordered]@{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Info.centralInputFingerprint;width=720;height=1280;images=$Entries}
    Assert-EastCaptureIdentity $Info $Manifest (Join-Path $East 'screenshots')
    if((Get-CentralInputFingerprint $RepositoryPath) -ne $Info.centralInputFingerprint){throw 'World inputs changed before east review export.'}
    Copy-Item -LiteralPath (Join-Path $West 'west-review-info.json') -Destination (Join-Path $East 'west-review-info.json')
    $Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $East 'east-review-info.json') -Encoding UTF8
    $Manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $East 'east-capture-manifest.json') -Encoding UTF8
    Write-EastComparisonJpeg $East
    [ordered]@{candidate_revision=$Info.gitRevision;centralInputFingerprint=$Info.centralInputFingerprint;native_pairs_verified=3;native_unity=$true;visual=$null;walking=$null;town_density=$null;pixel_verified=$false;adopted=$false;comparison_sha256=(Get-FileHash -LiteralPath (Join-Path $East 'east-before-candidate.jpg') -Algorithm SHA256).Hash.ToLowerInvariant();approved_design_sha256=(Get-FileHash -LiteralPath (Join-Path $East 'approved-design.png') -Algorithm SHA256).Hash.ToLowerInvariant();reason='Review central, west and east actual images before accepting all three checkpoints or advancing. No APK.'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $East 'east-adoption.json') -Encoding UTF8
    $Files=@(Get-ChildItem -LiteralPath $East -File)+@(Get-ChildItem -LiteralPath (Join-Path $East 'screenshots') -File)
    $Archive=Join-Path $RunDirectory 'east-florist-review.zip';Compress-Archive -LiteralPath $Files.FullName -DestinationPath $Archive
    if((Get-Item -LiteralPath $Archive).Length -gt 32MB){throw 'East review exceeds 32 MiB; split review evidence before upload.'}
    Write-Host "East native review: $Archive. Upload this, central-master-review.zip and west-cafe-review.zip; no APK, Pixel pending."
}

function Assert-EastReviewPrerequisites([string]$RepositoryPath) {
    # This is permission to proceed in the fixed sequence, not acceptance of
    # current rendering inputs. All three areas are rechecked in this new run.
    $Previous=Join-Path $RepositoryPath 'References/Review/west-cafe-2026-10-22-accepted'
    $Record=Get-Content -LiteralPath (Join-Path $Previous 'native-review.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if($Record.reviewed_revision -notmatch '^[a-fA-F0-9]{40}$' -or $Record.identity_verified -ne $true -or $Record.central.unity_stage_accepted -ne $true -or $Record.west.unity_stage_accepted -ne $true){throw 'East requires the previous central and west native checkpoints.'}
    $Actual=@(foreach($Area in @('central','west')) {
        $Directory=Join-Path $Previous "$Area/screenshots"
        $Entries=if($Area -eq 'central'){@(Get-CentralCaptureEntries $Directory)}else{@(Get-WestCaptureEntries $Directory)}
        foreach($Entry in $Entries){@{file="$Area/screenshots/$($Entry.file)";sha256=$Entry.sha256}}
    })
    if(@($Record.images).Count -ne 9){throw 'Previous central/west image record is incomplete.'}
    foreach($Image in $Actual){$Expected=@($Record.images | Where-Object {$_.file -eq $Image.file});if($Expected.Count -ne 1 -or $Expected[0].sha256 -ne $Image.sha256){throw 'Previous central/west native evidence changed.'}}
}
