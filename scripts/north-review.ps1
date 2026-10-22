# Source-bound north native review. Dot-sourcing never builds, renders or adopts.
function Get-NorthCaptureEntries([string]$Directory) {
    $Expected=@('04-town-lv1-North.png','04-town-lv3-North.png','04-town-lv6-North.png','north-connection.png','north-walking.png','north-door-arrived.png')
    $Files=@(Get-ChildItem -LiteralPath $Directory -Filter '*.png' -File)
    if($Files.Count -ne $Expected.Count){throw 'North review requires exactly three levels, the connection, walking and doorway.'}
    foreach($Name in $Expected) {
        $Path=Join-Path $Directory $Name
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "Missing north native image: $Name"}
        $Stream=[IO.File]::OpenRead($Path)
        try{$Header=New-Object byte[] 24;$Read=$Stream.Read($Header,0,24)}finally{$Stream.Dispose()}
        if($Read -ne 24 -or (Get-Item -LiteralPath $Path).Length -le 24 -or ($Header[0..7] -join ',') -ne '137,80,78,71,13,10,26,10' -or [Text.Encoding]::ASCII.GetString($Header,12,4) -ne 'IHDR'){throw "Invalid north PNG: $Name"}
        $Width=([int64]$Header[16]*16777216)+([int64]$Header[17]*65536)+([int64]$Header[18]*256)+$Header[19]
        $Height=([int64]$Header[20]*16777216)+([int64]$Header[21]*65536)+([int64]$Header[22]*256)+$Header[23]
        if($Width -ne 720 -or $Height -ne 1280){throw "North PNG must be 720x1280: $Name"}
        [ordered]@{file=$Name;bytes=(Get-Item -LiteralPath $Path).Length;sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
    }
}
function Assert-NorthCaptureIdentity($Info,$Manifest,[string]$Directory) {
    if($Info.gitRevision -notmatch '^[a-fA-F0-9]{40}$' -or $Info.productFingerprint -notmatch '^[a-fA-F0-9]{64}$' -or $Info.centralInputFingerprint -notmatch '^[a-fA-F0-9]{64}$' -or $Manifest.gitRevision -ne $Info.gitRevision -or $Manifest.productFingerprint -ne $Info.productFingerprint -or $Manifest.centralInputFingerprint -ne $Info.centralInputFingerprint){throw 'North evidence has different or missing source identity.'}
    $Actual=@(Get-NorthCaptureEntries $Directory)
    if(@($Manifest.images).Count -ne 6){throw 'North image manifest incomplete.'}
    foreach($Image in $Actual) {
        $Recorded=@($Manifest.images | Where-Object {$_.file -eq $Image.file})
        if($Recorded.Count -ne 1 -or $Recorded[0].sha256 -ne $Image.sha256){throw "North image changed: $($Image.file)"}
    }
}
function Write-NorthComparisonJpeg([string]$Directory) {
    Add-Type -AssemblyName System.Drawing
    $Bitmap=New-Object Drawing.Bitmap 2160,4250
    $Graphics=[Drawing.Graphics]::FromImage($Bitmap)
    $Font=New-Object Drawing.Font 'Arial',26
    $Small=New-Object Drawing.Font 'Arial',20
    $Brush=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(80,47,57))
    $Lock=[Drawing.Image]::FromFile((Join-Path $Directory 'approved-design.png'))
    try {
        $Graphics.Clear([Drawing.Color]::FromArgb(255,247,239))
        $Graphics.DrawString('NORTH QUEST COTTAGE / NATIVE CANDIDATE / USER APPROVAL AND PIXEL PENDING',$Font,$Brush,16,12)
        for($Row=0;$Row -lt 3;$Row++) {
            $Level=@(1,3,6)[$Row];$Y=80+$Row*1380
            $Graphics.DrawString("Before / Unity Lv.$Level",$Font,$Brush,16,$Y)
            $Graphics.DrawString("Candidate / Unity Lv.$Level",$Font,$Brush,736,$Y)
            $Graphics.DrawString('Visual Lock / central design',$Font,$Brush,1456,$Y)
            $Name="04-town-lv$Level-North.png"
            foreach($Column in @(0,1)) {
                $Path=if($Column -eq 0){Join-Path $Directory ('before-'+$Name)}else{Join-Path (Join-Path $Directory 'screenshots') $Name}
                $Image=[Drawing.Image]::FromFile($Path)
                try{$Graphics.DrawImage($Image,[Drawing.Rectangle]::new($Column*720,$Y+48,720,1280))}finally{$Image.Dispose()}
            }
            # This immutable central design is a quality reference for North,
            # never represented as a native North screenshot.
            $Graphics.DrawImage($Lock,[Drawing.Rectangle]::new(1440,$Y+48,720,1280),[Drawing.Rectangle]::new(27,146,572,1042),[Drawing.GraphicsUnit]::Pixel)
        }
        $Graphics.DrawString('Visual Lock is CENTRAL DESIGN, not a North render. Original PNGs included. Walking frames have no Before pairing.',$Small,$Brush,16,4220)
        $Encoder=[Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object {$_.MimeType -eq 'image/jpeg'}
        $Parameters=New-Object Drawing.Imaging.EncoderParameters 1
        try{$Parameters.Param[0]=[Drawing.Imaging.EncoderParameter]::new([Drawing.Imaging.Encoder]::Quality,[long]92);$Bitmap.Save((Join-Path $Directory 'north-before-candidate.jpg'),$Encoder,$Parameters)}finally{$Parameters.Dispose()}
    } finally {$Lock.Dispose();$Brush.Dispose();$Small.Dispose();$Font.Dispose();$Graphics.Dispose();$Bitmap.Dispose()}
}
function Write-NorthReview([string]$RepositoryPath,[string]$RunDirectory) {
    $CentralInfo=Assert-CentralReviewEvidence $RepositoryPath $RunDirectory
    $West=Join-Path $RunDirectory 'west'
    $WestInfo=Get-Content -LiteralPath (Join-Path $West 'west-review-info.json') -Raw | ConvertFrom-Json
    $WestManifest=Get-Content -LiteralPath (Join-Path $West 'west-capture-manifest.json') -Raw | ConvertFrom-Json
    Assert-WestCaptureIdentity $WestInfo $WestManifest (Join-Path $West 'screenshots')
    foreach($Key in @('gitRevision','productFingerprint','centralInputFingerprint')) {
        if($WestInfo.$Key -ne $CentralInfo.$Key){throw 'Companion central and west source identities differ.'}
    }
    $East=Join-Path $RunDirectory 'east'
    $EastInfo=Get-Content -LiteralPath (Join-Path $East 'east-review-info.json') -Raw | ConvertFrom-Json
    $EastManifest=Get-Content -LiteralPath (Join-Path $East 'east-capture-manifest.json') -Raw | ConvertFrom-Json
    Assert-EastCaptureIdentity $EastInfo $EastManifest (Join-Path $East 'screenshots')
    foreach($Key in @('gitRevision','productFingerprint','centralInputFingerprint')) {
        if($EastInfo.$Key -ne $CentralInfo.$Key){throw 'Companion central and east source identities differ.'}
    }
    $North=Join-Path $RunDirectory 'north';New-Item -ItemType Directory -Path $North -Force | Out-Null
    Move-Item -LiteralPath (Join-Path $RunDirectory 'screenshots/north') -Destination (Join-Path $North 'screenshots')
    $Entries=@(Get-NorthCaptureEntries (Join-Path $North 'screenshots'))
    Assert-ApprovedVisualLock $RepositoryPath
    $Baseline=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/baseline-native-2026-10-21.json') -Raw | ConvertFrom-Json
    $Before=Join-Path $RepositoryPath ('Builds/Android/'+$Baseline.run)
    if(Test-Path (Join-Path $Before 'screenshots')){$Before=Join-Path $Before 'screenshots'}
    foreach($Level in @(1,3,6)) {
        $Name="04-town-lv$Level-North.png";$Path=Join-Path $Before $Name
        $Expected=@($Baseline.capture.images | Where-Object {$_.file -eq $Name})
        if($Expected.Count -ne 1 -or -not(Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Expected[0].sha256){throw "Missing or changed native north Before: $Name"}
        Copy-Item -LiteralPath $Path -Destination (Join-Path $North ('before-'+$Name))
    }
    $Lock=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/approved-design-2026-10-21.json') -Raw | ConvertFrom-Json
    Copy-Item -LiteralPath (Join-Path $RepositoryPath $Lock.reference) -Destination (Join-Path $North 'approved-design.png')
    foreach($Name in @('central-playmode.xml','central-playmode.log','central-review-info.json')){Copy-Item -LiteralPath (Join-Path $RunDirectory $Name) -Destination (Join-Path $North $Name)}
    $Info=[ordered]@{gitRevision=$CentralInfo.gitRevision;productFingerprint=$CentralInfo.productFingerprint;centralInputFingerprint=$CentralInfo.centralInputFingerprint;nativeCaptureCount=6;scope='north native checkpoint; central, west and east rechecked in companion ZIPs';apk_generated=$false;pixel_verified=$false;adopted=$false}
    $Manifest=[ordered]@{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Info.centralInputFingerprint;width=720;height=1280;images=$Entries}
    Assert-NorthCaptureIdentity $Info $Manifest (Join-Path $North 'screenshots')
    if((Get-CentralInputFingerprint $RepositoryPath) -ne $Info.centralInputFingerprint){throw 'World inputs changed before north review export.'}
    Copy-Item -LiteralPath (Join-Path $West 'west-review-info.json') -Destination (Join-Path $North 'west-review-info.json')
    Copy-Item -LiteralPath (Join-Path $East 'east-review-info.json') -Destination (Join-Path $North 'east-review-info.json')
    $Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $North 'north-review-info.json') -Encoding UTF8
    $Manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $North 'north-capture-manifest.json') -Encoding UTF8
    Write-NorthComparisonJpeg $North
    [ordered]@{candidate_revision=$Info.gitRevision;centralInputFingerprint=$Info.centralInputFingerprint;native_pairs_verified=3;native_unity=$true;visual=$null;walking=$null;town_density=$null;pixel_verified=$false;adopted=$false;comparison_sha256=(Get-FileHash -LiteralPath (Join-Path $North 'north-before-candidate.jpg') -Algorithm SHA256).Hash.ToLowerInvariant();approved_design_sha256=(Get-FileHash -LiteralPath (Join-Path $North 'approved-design.png') -Algorithm SHA256).Hash.ToLowerInvariant();reason='Review central, west, east and north actual images before accepting all four checkpoints or advancing. No APK.'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $North 'north-adoption.json') -Encoding UTF8
    $Files=@(Get-ChildItem -LiteralPath $North -File)+@(Get-ChildItem -LiteralPath (Join-Path $North 'screenshots') -File)
    $Archive=Join-Path $RunDirectory 'north-quest-review.zip';Compress-Archive -LiteralPath $Files.FullName -DestinationPath $Archive
    if((Get-Item -LiteralPath $Archive).Length -gt 32MB){throw 'North review exceeds 32 MiB; split review evidence before upload.'}
    Write-Host "North native review: $Archive. Upload this, central-master-review.zip, west-cafe-review.zip and east-florist-review.zip; no APK, Pixel pending."
}

function Assert-NorthReviewPrerequisites([string]$RepositoryPath) {
    # This is permission to proceed in the fixed sequence, not acceptance of
    # current rendering inputs. All four areas are rechecked in this new run.
    $Previous=Join-Path $RepositoryPath 'References/Review/east-florist-2026-10-22-accepted'
    $Record=Get-Content -LiteralPath (Join-Path $Previous 'native-review.json') -Raw | ConvertFrom-Json
    if($Record.reviewed_revision -notmatch '^[a-fA-F0-9]{40}$' -or $Record.identity_verified -ne $true -or $Record.central.unity_stage_accepted -ne $true -or $Record.west.unity_stage_accepted -ne $true -or $Record.east.unity_stage_accepted -ne $true -or $Record.east.nameplate_readability -ne $true){throw 'North requires the previous central, west and east native checkpoints.'}
    $Actual=@(foreach($Area in @('central','west','east')) {
        $Directory=Join-Path $Previous "$Area/screenshots"
        $Entries=if($Area -eq 'central'){@(Get-CentralCaptureEntries $Directory)}elseif($Area -eq 'west'){@(Get-WestCaptureEntries $Directory)}else{@(Get-EastCaptureEntries $Directory)}
        foreach($Entry in $Entries){@{file="$Area/screenshots/$($Entry.file)";sha256=$Entry.sha256}}
    })
    if(@($Record.images).Count -ne 15){throw 'Previous central/west/east image record is incomplete.'}
    foreach($Image in $Actual){$Expected=@($Record.images | Where-Object {$_.file -eq $Image.file});if($Expected.Count -ne 1 -or $Expected[0].sha256 -ne $Image.sha256){throw 'Previous central/west/east native evidence changed.'}}
}
