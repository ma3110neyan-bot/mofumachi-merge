# Non-APK integrated review. This authorization permits review, never adoption.
function Get-FiveAreaRouteIds([string]$District) {
    if($District -eq 'North'){return @('cottage-left','cottage-right','left-waiting','right-waiting','mosaic-cross','mosaic-loop')}
    if($District -eq 'South'){return @('gazebo-left','gazebo-right','left-rest','right-rest','rest-cross','garden-loop')}
    return @('route-1','route-2','route-3','route-4','route-5','route-6')
}
function Resolve-FiveAreaEvidencePath([string]$RepositoryPath,[string]$Relative) {
    if(-not $Relative -or [IO.Path]::IsPathRooted($Relative) -or $Relative -match '(^|[/\\])\.\.([/\\]|$)' -or $Relative.Contains(':')){throw 'Review evidence must stay inside the repository.'}
    Join-Path $RepositoryPath $Relative
}
function Assert-FiveAreaReviewPrerequisites([string]$RepositoryPath) {
    Assert-SouthReviewPrerequisites $RepositoryPath
    Assert-SouthFinalReviewConditions $RepositoryPath
}
function Assert-SouthFinalReviewConditions([string]$RepositoryPath) {
    $Stage=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/stage-acceptance-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $Approval=$Stage.user_review_checkpoints.south_garden
    if($Approval.user_approved -ne $true -or $Approval.approved_by -ne 'user' -or $Approval.conditional -ne $true -or -not $Approval.approval_reference){throw 'Five-area review needs the actual conditional South authorization.'}
    if($Approval.reviewed_candidate -ne '45f205d7873067f5fec3a26df1b32ff9f21602a7' -or $Approval.input_fingerprint -ne 'e4cb869ca5d32b4d0ecc3014a86468aa9e39774deb3ef64f57f1c753f0dc4dda' -or $Approval.native_unity -ne $true){throw 'Conditional South authorization identifies a different native candidate.'}
    $Lock=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/approved-design-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if($Approval.approved_visual_lock_sha256 -ne (Get-FileHash -LiteralPath (Resolve-FiveAreaEvidencePath $RepositoryPath $Lock.reference) -Algorithm SHA256).Hash.ToLowerInvariant()){throw 'Conditional South authorization has a different Visual Lock.'}
    if(@($Approval.images).Count -ne 3){throw 'Conditional South approval needs its actual three reviewed JPEGs.'}
    foreach($Level in @(1,3,6)) {
        $Image=@($Approval.images | Where-Object {$_.district -eq 'South' -and $_.level -eq $Level})
        if($Image.Count -ne 1 -or $Image[0].sha256 -notmatch '^[a-f0-9]{64}$' -or (Get-FileHash -LiteralPath (Resolve-FiveAreaEvidencePath $RepositoryPath $Image[0].file) -Algorithm SHA256).Hash -ne $Image[0].sha256){throw 'Conditional South JPEG changed or missing.'}
    }
    $Conditions=Get-Content -LiteralPath (Resolve-FiveAreaEvidencePath $RepositoryPath $Approval.conditions_record) -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach($Key in @('garden_rest_implemented','level_three_implemented','circulation_verified','ground_alignment_implemented','integration_review_authorized_by_user')){if($Conditions.$Key -isnot [bool] -or $Conditions.$Key -ne $true){throw "South condition incomplete: $Key"}}
    if($Conditions.approved_base_revision -ne $Approval.reviewed_candidate -or $Conditions.native_after_adjustments_verified -ne $false -or $Conditions.integration_user_approved -ne $false){throw 'Review authorization must distinguish implemented corrections from native/user acceptance.'}
    if(@($Conditions.adjusted_files).Count -ne 6 -or @($Conditions.frozen_files).Count -ne 16){throw 'South conditions must bind all adjusted inputs and the frozen four-area scenery/navigation.'}
    foreach($File in @($Conditions.adjusted_files)+@($Conditions.frozen_files)) {
        $Path=Resolve-FiveAreaEvidencePath $RepositoryPath $File.file
        $Bytes=if($File.file.EndsWith('.png')){[IO.File]::ReadAllBytes($Path)}else{[Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($Path).Replace("`r`n","`n"))}
        if($File.sha256 -notmatch '^[a-f0-9]{64}$' -or (Get-VisualLockDigest $Bytes) -ne $File.sha256){throw "South conditions or frozen quality changed: $($File.file)"}
    }
}
function Write-FiveAreaComparisonJpegs([string]$Directory) {
    Add-Type -AssemblyName System.Drawing
    foreach($Level in @(1,3,6)) {
        $Bitmap=New-Object Drawing.Bitmap 1440,6880;$Graphics=[Drawing.Graphics]::FromImage($Bitmap)
        $Font=New-Object Drawing.Font 'Meiryo',23;$Brush=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(80,47,57))
        try {
            $Graphics.Clear([Drawing.Color]::FromArgb(255,247,239))
            $Graphics.DrawString("5エリア / Lv.$Level / Before45f205d → Candidate（Unity隔離状態・未採用）",$Font,$Brush,12,8)
            $Names=@{Central='中央（噴水・集合）';West='西（カフェ）';East='東（花屋）';North='北（お願い・届け物）';South='南（庭園・休憩）'}
            $Row=0
            foreach($District in @('Central','West','East','North','South')) {
                $Area=$District.ToLowerInvariant();$Y=60+$Row*1360;$Name="04-town-lv$Level-$District.png"
                $Graphics.DrawString("$($Names[$District]) / Before",$Font,$Brush,12,$Y);$Graphics.DrawString("$($Names[$District]) / Candidate",$Font,$Brush,732,$Y)
                $Paths=@((Join-Path $Directory "before/$Name"),(Join-Path $Directory "$Area/screenshots/$Name"))
                for($Column=0;$Column -lt 2;$Column++){$Image=[Drawing.Image]::FromFile($Paths[$Column]);try{$Graphics.DrawImage($Image,[Drawing.Rectangle]::new($Column*720,$Y+45,720,1280))}finally{$Image.Dispose()}}
                $Row++
            }
            $Bitmap.Save((Join-Path $Directory "five-area-lv$Level-before-candidate.jpg"),[Drawing.Imaging.ImageFormat]::Jpeg)
        } finally {$Brush.Dispose();$Font.Dispose();$Graphics.Dispose();$Bitmap.Dispose()}
    }
}
function Write-FiveAreaReview([string]$RepositoryPath,[string]$RunDirectory) {
    Assert-FiveAreaReviewPrerequisites $RepositoryPath
    $Info=Get-Content -LiteralPath (Join-Path $RunDirectory 'central-review-info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if($Info.gitRevision -ne (Read-SourceRevision $RepositoryPath) -or $Info.productFingerprint -ne (Get-ProductFingerprint $RepositoryPath) -or $Info.centralInputFingerprint -ne (Get-CentralInputFingerprint $RepositoryPath)){throw 'Five-area capture identity differs from current source.'}
    Assert-TestRun (Join-Path $RunDirectory 'central-playmode.xml') 16 @('ExperienceCaptureTests','CentralMasterReviewTests','WestMasterReviewTests','EastMasterReviewTests','NorthMasterReviewTests','SouthMasterReviewTests','FiveAreaIntegrationReviewTests')
    [xml]$Xml=Get-Content -LiteralPath (Join-Path $RunDirectory 'central-playmode.xml') -Raw -Encoding UTF8
    if([int]$Xml.'test-run'.total -ne 16){throw 'Five-area review requires exactly16 native cases, counted once.'}
    $Previous=Join-Path $RepositoryPath 'References/Review/south-garden-2026-10-23-candidate'
    $Receipt=Get-Content -LiteralPath (Join-Path $Previous 'receipt-integrity.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if($Receipt.gitRevision -ne '45f205d7873067f5fec3a26df1b32ff9f21602a7'){throw 'Wrong five-area Before source.'}
    # Validate the complete raw tree and route bindings before creating output.
    $Screens=Join-Path $RunDirectory 'screenshots'
    foreach($District in @('Central','West','East','North','South')) {
        $Area=$District.ToLowerInvariant();$Frames=if($Area -eq 'central'){$Screens}else{Join-Path $Screens $Area}
        $Entries=if($Area -eq 'central'){@(Get-CentralCaptureEntries $Frames)}elseif($Area -eq 'west'){@(Get-WestCaptureEntries $Frames)}elseif($Area -eq 'east'){@(Get-EastCaptureEntries $Frames)}elseif($Area -eq 'north'){@(Get-NorthCaptureEntries $Frames)}else{@(Get-SouthCaptureEntries $Frames)}
        $ExpectedCount=if($Area -eq 'central'){3}else{6}
        if($Entries.Count -ne $ExpectedCount){throw 'Incomplete district captures.'}
        Assert-TownRouteProjection $Frames $District (Get-FiveAreaRouteIds $District)
        foreach($Level in @(1,3,6)) {
            $Name="04-town-lv$Level-$District.png";$Expected=@($Receipt.images | Where-Object {$_.area -eq $Area -and $_.file -eq $Name})
            if($Expected.Count -ne 1 -or (Get-FileHash -LiteralPath (Join-Path $Previous "$Area/screenshots/$Name") -Algorithm SHA256).Hash -ne $Expected[0].sha256){throw "Five-area Before changed: $Name"}
        }
    }
    $Review=Join-Path $RunDirectory 'five-area-review';New-Item -ItemType Directory -Path (Join-Path $Review 'before') -Force | Out-Null
    foreach($District in @('Central','West','East','North','South')) {
        $Area=$District.ToLowerInvariant();$Frames=if($Area -eq 'central'){$Screens}else{Join-Path $Screens $Area}
        $Destination=Join-Path $Review "$Area/screenshots";New-Item -ItemType Directory -Path $Destination -Force | Out-Null
        Copy-Item -Path (Join-Path $Frames '*.png') -Destination $Destination
        Copy-Item -LiteralPath (Join-Path $Frames 'capture-world.txt') -Destination (Join-Path $Review "$Area/capture-world.txt")
        Copy-Item -LiteralPath (Join-Path $Frames "$Area-route-projection.json") -Destination (Join-Path $Review "$Area/$Area-route-projection.json")
        $Ids=Get-FiveAreaRouteIds $District;$Labels=@{};for($i=0;$i -lt 6;$i++){$Labels[$Ids[$i]]=@('左側の道','右側の道','左から滞在場所','右から滞在場所','横断','回遊')[$i]}
        Write-TownRoutesJpeg (Join-Path $Review $Area) $District $District $Labels
        foreach($Level in @(1,3,6)){Copy-Item -LiteralPath (Join-Path $Previous "$Area/screenshots/04-town-lv$Level-$District.png") -Destination (Join-Path $Review "before/04-town-lv$Level-$District.png")}
    }
    foreach($Name in @('central-playmode.xml','central-playmode.log','central-review-info.json')){Copy-Item -LiteralPath (Join-Path $RunDirectory $Name) -Destination (Join-Path $Review $Name)}
    if(Test-Path -LiteralPath (Join-Path $Screens 'texture-memory.txt')){Copy-Item -LiteralPath (Join-Path $Screens 'texture-memory.txt') -Destination $Review}
    $Lock=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/approved-design-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    Copy-Item -LiteralPath (Resolve-FiveAreaEvidencePath $RepositoryPath $Lock.reference) -Destination (Join-Path $Review 'approved-design.png')
    @'
5エリア統合レビュー（QA APK・Pixel最終受入ではありません）
中央＝噴水・集合、西＝カフェ、東＝花屋、北＝お願い・届け物、南＝庭園・休憩。
Lv別の比較JPEGは前回45f205dと今回の同じカメラ・同じLvです。原寸PNGも保持します。
Lv1は完成した可愛さ、Lv3は鉢・灯り・席・掲示／花飾り、Lv6は花入口・アーチ等の特別感を比較してください。
南はベンチの鉢と東屋の灯り／花をLv3で強化し、左右小道・東屋前・花壇前・休憩場所を接続しました。
歩行導線JPEGは実Lv1画像から投影した実ナビ経路の注釈です。操作軌跡ではありません。
central-playmode.xmlは16件を1回計上します。複数ZIPに分けてもテスト件数は増えません。
全five-area-review-*.zipをまとめて提出してください。各原本のSHAはreview-manifest.jsonに記録します。
景観・密度・成長差・道と絵の一致・名前の可読性は、この実画像で別途判定します。
実画像のユーザー承認前にAPK・Pixel最終受入・main統合へ進みません。
'@ | Set-Content -LiteralPath (Join-Path $Review 'review-guide.txt') -Encoding UTF8
    Write-FiveAreaComparisonJpegs $Review
    $Files=@(Get-ChildItem -LiteralPath $Review -File -Recurse | Sort-Object FullName)
    $Inventory=@(foreach($File in $Files){[ordered]@{file=$File.FullName.Substring($Review.Length+1).Replace('\','/');bytes=$File.Length;sha256=(Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
    [ordered]@{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Info.centralInputFingerprint;before_revision=$Receipt.gitRevision;native_image_count=27;before_image_count=15;native_test_count=16;scope='Five-area native fixture review, not Pixel';native_unity=$true;visual=$null;user_approved=$false;apk_generated=$false;pixel_verified=$false;adopted=$false;files=$Inventory} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Review 'review-manifest.json') -Encoding UTF8
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $Files=@(Get-ChildItem -LiteralPath $Review -File -Recurse | Sort-Object FullName)
    $Part=0;$Total=0L;$Archive=$null;$Parts=New-Object 'System.Collections.Generic.List[object]'
    try {
        foreach($File in $Files) {
            if($File.Length -gt 20MB){throw 'A review file exceeds the upload budget.'}
            if($null -eq $Archive -or $Total+$File.Length -gt 20MB) {
                if($null -ne $Archive){$Archive.Dispose();$Archive=$null}
                $Part++;$Total=0L;$Path=Join-Path $RunDirectory ('five-area-review-{0:D2}.zip' -f $Part)
                if(Test-Path -LiteralPath $Path){throw 'Review parts already exist; do not mix runs or overwrite prior evidence.'}
                $Parts.Add($Path);$Archive=[IO.Compression.ZipFile]::Open($Path,[IO.Compression.ZipArchiveMode]::Create)
            }
            $Relative=$File.FullName.Substring($Review.Length+1).Replace('\','/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($Archive,$File.FullName,$Relative,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
            $Total+=$File.Length
        }
    } finally {if($null -ne $Archive){$Archive.Dispose()}}
    $PartRecords=@(foreach($Path in $Parts){if((Get-Item -LiteralPath $Path).Length -gt 21MB){throw 'Review part exceeds upload budget.'};[ordered]@{file=[IO.Path]::GetFileName($Path);sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant();bytes=(Get-Item -LiteralPath $Path).Length}})
    [ordered]@{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;parts=$PartRecords} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $RunDirectory 'five-area-parts.json') -Encoding UTF8
    Write-Host "Five-area review: $RunDirectory\five-area-review-*.zip. Submit all parts; user approval pending. No APK or Pixel acceptance."
}
