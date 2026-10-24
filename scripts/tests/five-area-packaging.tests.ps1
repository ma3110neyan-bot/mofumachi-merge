$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../build-android.ps1')
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$Root=Join-Path $Repo ('Builds/FiveAreaReview/19990101-000000-'+[guid]::NewGuid().ToString('N').Substring(0,8))
try {
    $Review=Join-Path $Root 'five-area-review';New-Item -ItemType Directory -Path $Review -Force | Out-Null
    $Header=[byte[]]@(137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,2,208,0,0,5,0,0)
    foreach($District in @('Central','West','East','North','South')) {
        $Area=$District.ToLowerInvariant();$Frames=Join-Path $Review "$Area/screenshots";New-Item -ItemType Directory -Path $Frames -Force | Out-Null
        foreach($Level in @(1,3,6)){[IO.File]::WriteAllBytes((Join-Path $Frames "04-town-lv$Level-$District.png"),$Header)}
        if($Area -ne 'central') {
            $Last=if($Area -eq 'south'){'rest'}else{'door'}
            foreach($Name in @("$Area-connection.png","$Area-walking.png","$Area-$Last-arrived.png")){[IO.File]::WriteAllBytes((Join-Path $Frames $Name),$Header)}
        }
        $Ids=Get-FiveAreaRouteIds $District
        $Routes=@(for($i=0;$i -lt 6;$i++){@{id=$Ids[$i];points=@(@{x=80+$i*80;y=900},@{x=360;y=600})}})
        @{image="04-town-lv1-$District.png";image_sha256=(Get-FileHash -LiteralPath (Join-Path $Frames "04-town-lv1-$District.png") -Algorithm SHA256).Hash.ToLowerInvariant();kind='navigation-route-overlay-not-gameplay-trace';origin='top-left';width=720;height=1280;routes=$Routes} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Review "$Area/$Area-route-projection.json") -Encoding UTF8
    }
    New-Item -ItemType Directory -Path (Join-Path $Review 'before') -Force | Out-Null
    foreach($District in @('Central','West','East','North','South')){foreach($Level in @(1,3,6)){[IO.File]::WriteAllBytes((Join-Path $Review "before/04-town-lv$Level-$District.png"),$Header)}}
    # A recorded hidden parent is explicitly accessible and must be archived.
    New-Item -ItemType Directory -Path (Join-Path $Review '.capture-notes') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $Review '.capture-notes/source.txt') -Value 'Synthetic packaging fixture; no Unity or device execution.'
    $Info=@{gitRevision=Read-SourceRevision $Repo;productFingerprint=('b'*64);centralInputFingerprint=Get-CentralInputFingerprint $Repo}
    $Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Root 'central-review-info.json') -Encoding UTF8
    Copy-Item -LiteralPath (Join-Path $Root 'central-review-info.json') -Destination $Review
    $Counts=[ordered]@{ExperienceCaptureTests=1;CentralMasterReviewTests=2;WestMasterReviewTests=3;EastMasterReviewTests=3;NorthMasterReviewTests=3;SouthMasterReviewTests=3;FiveAreaIntegrationReviewTests=1}
    $Suites=foreach($Name in $Counts.Keys){"<test-suite name='$Name' result='Passed'>"+('<test-case result="Passed"/>'*$Counts[$Name])+"</test-suite>"}
    "<test-run total='16' passed='16' failed='0' skipped='0' result='Passed'>$($Suites -join '')</test-run>" | Set-Content -LiteralPath (Join-Path $Review 'central-playmode.xml')
    Set-Content -LiteralPath (Join-Path $Review 'central-playmode.log') -Value 'Synthetic pipeline fixture, not a Unity execution log.'
    Set-Content -LiteralPath (Join-Path $Review 'south/capture-world.txt') -Value 'Synthetic coordinate fixture.'
    [IO.File]::WriteAllBytes((Join-Path $Review 'south/south-walking-routes.jpg'),[byte[]]@(255,216,255,217))
    Copy-Item -LiteralPath (Join-Path $Repo 'References/Preview/design-first-2026-10-21/design-proposal.png') -Destination (Join-Path $Review 'approved-design.png')
    $Files=@(Get-ChildItem -LiteralPath $Review -Recurse -File -Force)
    $Inventory=@(foreach($File in $Files){@{file=$File.FullName.Substring($Review.Length+1).Replace('\','/');bytes=$File.Length;sha256=(Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
    @{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Info.centralInputFingerprint;native_image_count=27;before_image_count=15;native_test_count=16;native_unity=$true;user_approved=$false;apk_generated=$false;pixel_verified=$false;adopted=$false;files=$Inventory} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Review 'review-manifest.json') -Encoding UTF8
    $Packed=Write-FiveAreaReviewArchives $Repo $Root (Join-Path $Root 'packed')
    $Zip=[IO.Compression.ZipFile]::OpenRead((Join-Path $Packed 'five-area-review-01.zip'))
    try {
        foreach($File in $Inventory){if(@($Zip.Entries | Where-Object {$_.FullName -eq $File.file}).Count -ne 1){throw "Recorded evidence omitted from ZIP: $($File.file)"}}
    } finally {$Zip.Dispose()}
    # Exercise the user-facing recovery entry too. Only Explorer is stubbed;
    # the actual entry, source gates, inventory, XML and ZIP writing all run.
    function Start-Process {param($FilePath,$ArgumentList);if($FilePath -ne 'explorer.exe'){throw 'Packaging-only recovery attempted to launch a process other than Explorer.'}}
    & (Join-Path $PSScriptRoot '../repack-five-area-review.ps1') -RunDirectory $Root
    $Recovered=@(Get-ChildItem -LiteralPath (Join-Path $Root 'repacked') -File -Recurse -Filter 'five-area-review-01.zip')
    if($Recovered.Count -ne 1){throw 'Recovery CLI failed to create a fresh archive directory.'}
    function Write-SouthRevisionComparisons([string]$RepositoryPath,[string]$Directory){foreach($Name in @('south-before-held-revised.jpg','south-entry-before-held-revised.jpg')){[IO.File]::WriteAllBytes((Join-Path $Directory $Name),[byte[]]@(255,216,255,217))}}
    Write-SouthFinalReview $Repo $Root
    $Expanded=Join-Path $Root 'south-expanded'
    foreach($Part in Get-ChildItem -LiteralPath $Root -Filter 'south-final-review-*.zip'){Expand-Archive -LiteralPath $Part.FullName -DestinationPath $Expanded}
    $References=Get-SouthRevisionReferences $Repo
    foreach($Image in $References.images){$Name=$Image.stage+'-'+[IO.Path]::GetFileName($Image.file);if(-not(Test-Path -LiteralPath (Join-Path $Expanded $Name))){throw "Comparison original omitted from South package: $Name"}}
    Write-Host 'Packaging-only archive preserves recorded hidden-parent evidence and older script fingerprints. Synthetic headers/XML, no Unity/Pixel.'
} finally {if(Test-Path -LiteralPath $Root){Remove-Item -LiteralPath $Root -Recurse -Force}}
