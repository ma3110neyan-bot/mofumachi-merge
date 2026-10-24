# South-first review documents; original runtime captures remain unchanged.
function Get-SouthRevisionReferences([string]$RepositoryPath) {
    $Record=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/south-revision-baseline-2026-10-24.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if($Record.before_revision -ne '45f205d7873067f5fec3a26df1b32ff9f21602a7' -or $Record.held_revision -ne 'd8e649b7802292ca552d564427bc4b720fa8dca4' -or @($Record.images).Count -ne 10){throw 'South revision needs the exact Before and held native references.'}
    foreach($Stage in @('before','held')){foreach($Scene in @('lv1','lv3','lv6','connection','walking')) {
        $Image=@($Record.images | Where-Object {$_.stage -eq $Stage -and $_.scene -eq $Scene})
        if($Image.Count -ne 1){throw 'South reference scene missing or duplicated.'}
        $Path=Resolve-FiveAreaEvidencePath $RepositoryPath $Image[0].file
        if($Image[0].sha256 -notmatch '^[a-fA-F0-9]{64}$' -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Image[0].sha256){throw 'South native reference changed.'}
    }}
    return $Record
}
function Write-SouthRevisionComparisons([string]$RepositoryPath,[string]$Directory) {
    $References=Get-SouthRevisionReferences $RepositoryPath
    Add-Type -AssemblyName System.Drawing
    function Draw-SouthRows([string]$Name,[object[]]$Rows) {
        $Bitmap=New-Object Drawing.Bitmap 2160,(80+$Rows.Count*1380);$Graphics=[Drawing.Graphics]::FromImage($Bitmap)
        $Font=New-Object Drawing.Font 'Arial',25;$Brush=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(80,47,57))
        try {
            $Graphics.Clear([Drawing.Color]::FromArgb(255,247,239));$Graphics.DrawString('SOUTH ONLY / BEFORE45f205d - HELDd8e649b - REVISED UNITY / NOT ADOPTED',$Font,$Brush,12,10)
            for($Row=0;$Row -lt $Rows.Count;$Row++) {
                $Scene=$Rows[$Row].scene;$Y=80+$Row*1380
                $Before=@($References.images | Where-Object {$_.stage -eq 'before' -and $_.scene -eq $Scene})[0]
                $Held=@($References.images | Where-Object {$_.stage -eq 'held' -and $_.scene -eq $Scene})[0]
                $Paths=@((Resolve-FiveAreaEvidencePath $RepositoryPath $Before.file),(Resolve-FiveAreaEvidencePath $RepositoryPath $Held.file),(Join-Path $Directory $Rows[$Row].candidate))
                for($Column=0;$Column -lt 3;$Column++) {
                    $Graphics.DrawString((@('Before','Held Candidate','Revised Candidate')[$Column]+' / '+$Scene),$Font,$Brush,$Column*720+12,$Y)
                    $Image=[Drawing.Image]::FromFile($Paths[$Column]);try{$Graphics.DrawImage($Image,[Drawing.Rectangle]::new($Column*720,$Y+45,720,1280))}finally{$Image.Dispose()}
                }
            }
            $Bitmap.Save((Join-Path $Directory $Name),[Drawing.Imaging.ImageFormat]::Jpeg)
        } finally {$Brush.Dispose();$Font.Dispose();$Graphics.Dispose();$Bitmap.Dispose()}
    }
    Draw-SouthRows 'south-before-held-revised.jpg' @(@{scene='lv1';candidate='04-town-lv1-South.png'},@{scene='lv3';candidate='04-town-lv3-South.png'},@{scene='lv6';candidate='04-town-lv6-South.png'})
    Draw-SouthRows 'south-entry-before-held-revised.jpg' @(@{scene='connection';candidate='south-connection.png'},@{scene='walking';candidate='south-walking.png'})
}
function Write-SouthFinalReview([string]$RepositoryPath,[string]$RunDirectory) {
    $Evidence=Assert-FiveAreaStagedReview $RepositoryPath $RunDirectory
    $References=Get-SouthRevisionReferences $RepositoryPath
    $Directory=Join-Path $RunDirectory 'south-final-review';New-Item -ItemType Directory -Path $Directory -Force | Out-Null
    Copy-Item -Path (Join-Path $Evidence.Directory 'south/screenshots/*.png') -Destination $Directory
    foreach($Name in @('south-route-projection.json','south-walking-routes.jpg','capture-world.txt')){Copy-Item -LiteralPath (Join-Path $Evidence.Directory "south/$Name") -Destination $Directory}
    foreach($Image in $References.images) {
        $Name=$Image.stage+'-'+[IO.Path]::GetFileName($Image.file)
        Copy-Item -LiteralPath (Resolve-FiveAreaEvidencePath $RepositoryPath $Image.file) -Destination (Join-Path $Directory $Name)
        $Image | Add-Member -NotePropertyName archive_file -NotePropertyValue $Name
    }
    foreach($Name in @('central-playmode.xml','central-playmode.log','central-review-info.json','approved-design.png')){Copy-Item -LiteralPath (Join-Path $Evidence.Directory $Name) -Destination $Directory}
    $References | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Directory 'south-native-references.json') -Encoding UTF8
    $HeldRoot=Join-Path $RepositoryPath 'References/Review/five-area-2026-10-24-candidate'
    $Checks=@(foreach($Area in @('central','west','east','north')) {
        foreach($Frame in Get-ChildItem -LiteralPath (Join-Path $Evidence.Directory "$Area/screenshots") -Filter '*.png' -File) {
            $Old=Join-Path $HeldRoot "$Area/screenshots/$($Frame.Name)"
            $Hash=(Get-FileHash -LiteralPath $Frame.FullName -Algorithm SHA256).Hash.ToLowerInvariant();$OldHash=(Get-FileHash -LiteralPath $Old -Algorithm SHA256).Hash.ToLowerInvariant()
            [ordered]@{area=$Area;file=$Frame.Name;sha256=$Hash;held_sha256=$OldHash;byte_identical=($Hash -eq $OldHash)}
        }
    })
    [ordered]@{scope='Frozen four-area frame hashes; idle/water differences require visual inspection, not automatic adoption';capture_revision=$Evidence.Info.gitRevision;images=$Checks;visual_review_required=$true;adopted=$false} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Directory 'frozen-four-frame-checks.json') -Encoding UTF8
    Write-SouthRevisionComparisons $RepositoryPath $Directory
    $Files=@(Get-ChildItem -LiteralPath $Directory -File | Sort-Object Name)
    $Inventory=@(foreach($File in $Files){[ordered]@{file=$File.Name;bytes=$File.Length;sha256=(Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
    [ordered]@{gitRevision=$Evidence.Info.gitRevision;productFingerprint=$Evidence.Info.productFingerprint;centralInputFingerprint=$Evidence.Info.centralInputFingerprint;source_manifest_sha256=(Get-FileHash -LiteralPath $Evidence.ManifestPath -Algorithm SHA256).Hash.ToLowerInvariant();before_revision=$References.before_revision;held_revision=$References.held_revision;scope='South-first revised native images; whole-town approval pending';native_test_count=16;native_south_image_count=6;native_unity=$true;user_approved=$false;apk_generated=$false;pixel_verified=$false;adopted=$false;files=$Inventory} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Directory 'south-final-manifest.json') -Encoding UTF8
    # Flat, uniquely named documents keep Compress-Archive compatible with5.1.
    # Each original is present once; no duplicate XML test-count claims.
    $Batch=New-Object 'System.Collections.Generic.List[string]';$Bytes=0L;$Part=1
    foreach($File in Get-ChildItem -LiteralPath $Directory -File | Sort-Object Name) {
        if($File.Length -gt 20MB){throw 'South review file exceeds upload budget.'}
        if($Batch.Count -gt 0 -and $Bytes+$File.Length -gt 20MB) {
            Compress-Archive -LiteralPath $Batch.ToArray() -DestinationPath (Join-Path $RunDirectory ('south-final-review-{0:D2}.zip' -f $Part))
            $Part++;$Batch.Clear();$Bytes=0L
        }
        $Batch.Add($File.FullName);$Bytes+=$File.Length
    }
    if($Batch.Count -gt 0){Compress-Archive -LiteralPath $Batch.ToArray() -DestinationPath (Join-Path $RunDirectory ('south-final-review-{0:D2}.zip' -f $Part))}
    foreach($PartFile in Get-ChildItem -LiteralPath $RunDirectory -Filter 'south-final-review-*.zip' -File){if($PartFile.Length -gt 21MB){throw 'South review part exceeds upload budget.'}}
    Write-Host "South-first review: $RunDirectory\south-final-review-*.zip. Full five-area evidence is staged for later review; no APK/Pixel."
}
