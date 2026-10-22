$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../visual-lock.ps1')
. (Join-Path $PSScriptRoot '../west-review.ps1')
. (Join-Path $PSScriptRoot '../east-review.ps1')
if(Test-Path (Join-Path $PSScriptRoot '../north-review.ps1')){. (Join-Path $PSScriptRoot '../north-review.ps1')}
function Reject([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Invalid north native evidence accepted.'}}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-north-review-'+[guid]::NewGuid())
New-Item -ItemType Directory -Path $Root -Force | Out-Null
try {
    $Names=@('04-town-lv1-North.png','04-town-lv3-North.png','04-town-lv6-North.png','north-connection.png','north-walking.png','north-door-arrived.png')
    $Header=[byte[]]@(137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,2,208,0,0,5,0,0)
    foreach($Name in $Names){[IO.File]::WriteAllBytes((Join-Path $Root $Name),$Header)}
    $Images=@(Get-NorthCaptureEntries $Root)
    if($Images.Count -ne 6){throw 'Exactly six north frames required.'}
    $Info=@{gitRevision=('a'*40);productFingerprint=('b'*64);centralInputFingerprint=('c'*64)}
    $Manifest=@{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Info.centralInputFingerprint;images=$Images}
    Assert-NorthCaptureIdentity $Info $Manifest $Root
    $Manifest.gitRevision=('d'*40);Reject {Assert-NorthCaptureIdentity $Info $Manifest $Root};$Manifest.gitRevision=$Info.gitRevision
    $Manifest.centralInputFingerprint=('d'*64);Reject {Assert-NorthCaptureIdentity $Info $Manifest $Root};$Manifest.centralInputFingerprint=$Info.centralInputFingerprint
    [IO.File]::AppendAllText((Join-Path $Root $Names[0]),'changed');Reject {Assert-NorthCaptureIdentity $Info $Manifest $Root};[IO.File]::WriteAllBytes((Join-Path $Root $Names[0]),$Header)
    Remove-Item -LiteralPath (Join-Path $Root $Names[5]);Reject {Get-NorthCaptureEntries $Root};[IO.File]::WriteAllBytes((Join-Path $Root $Names[5]),$Header)
    [IO.File]::WriteAllBytes((Join-Path $Root 'extra.png'),$Header);Reject {Get-NorthCaptureEntries $Root};Remove-Item -LiteralPath (Join-Path $Root 'extra.png')
    $Bad=$Header.Clone();$Bad[19]=0;[IO.File]::WriteAllBytes((Join-Path $Root $Names[0]),$Bad);Reject {Get-NorthCaptureEntries $Root}
    Write-Host 'North exact frame list, dimensions, source/input identity and changed evidence rejected. Synthetic PNGs only; no Unity or Pixel execution.'
} finally {Remove-Item -LiteralPath $Root -Recurse -Force}
# Exercise the real move/Before checks/metadata/archive pipeline on Linux.
# Only Windows JPEG drawing is stubbed; these are synthetic headers, not renders.
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-north-package-'+[guid]::NewGuid())
try {
    foreach($Relative in Get-CentralInputPaths) {
        $Path=Join-Path $Root $Relative;New-Item -ItemType Directory -Path (Split-Path -Parent $Path) -Force | Out-Null
        [IO.File]::WriteAllText($Path,'synthetic input')
    }
    $Art=Join-Path $Root 'approved.png';[IO.File]::WriteAllBytes($Art,[byte[]]@(1,2,3,4));$ArtHash=(Get-FileHash $Art -Algorithm SHA256).Hash.ToLowerInvariant()
    @{reference='approved.png';files=@(@{path='approved.png';mode='binary';sha256=$ArtHash})} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Root 'References/VisualLock/approved-design-2026-10-21.json')
    $Run=Join-Path $Root 'Builds/NorthReview/synthetic';$Screens=Join-Path $Run 'screenshots';$NorthScreens=Join-Path $Screens 'north'
    $Before=Join-Path $Root 'Builds/Android/baseline/screenshots'
    foreach($Path in @($Screens,$NorthScreens,$Before)){New-Item -ItemType Directory -Path $Path -Force | Out-Null}
    foreach($Level in @(1,3,6)){[IO.File]::WriteAllBytes((Join-Path $Screens "04-town-lv$Level-Central.png"),$Header);[IO.File]::WriteAllBytes((Join-Path $Before "04-town-lv$Level-North.png"),$Header)}
    foreach($Name in $Names){[IO.File]::WriteAllBytes((Join-Path $NorthScreens $Name),$Header)}
    $Central=@(Get-CentralCaptureEntries $Screens);$NorthEntries=@(Get-NorthCaptureEntries $NorthScreens)
    @{run='baseline';capture=@{images=($Central+@($NorthEntries | Where-Object {$_.file -like '04-town*'}))}} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $Root 'References/VisualLock/baseline-native-2026-10-21.json')
    foreach($Entry in $Central){Copy-Item -LiteralPath (Join-Path $Screens $Entry.file) -Destination (Join-Path $Run ('before-'+$Entry.file))}
    Copy-Item -LiteralPath $Art -Destination (Join-Path $Run 'approved-design.png')
    $Comparison=Join-Path $Run 'central-before-lock-candidate.jpg';[IO.File]::WriteAllBytes($Comparison,[byte[]]@(5,6,7))
    $Digest=Get-CentralInputFingerprint $Root
    $Info=@{gitRevision=('a'*40);productFingerprint=('b'*64);centralInputFingerprint=$Digest}
    $Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'central-review-info.json')
    @{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Digest;images=$Central} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Run 'capture-manifest.json')
    @{candidate_revision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Digest;native_unity=$true;native_pairs_verified=3;approved_design_sha256=$ArtHash;comparison_sha256=(Get-FileHash $Comparison -Algorithm SHA256).Hash.ToLowerInvariant()} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'central-adoption.json')
    foreach($Name in @('central-playmode.xml','central-playmode.log')){Set-Content -LiteralPath (Join-Path $Run $Name) -Value 'synthetic native metadata'}
    $West=Join-Path $Run 'west';$WestScreens=Join-Path $West 'screenshots';New-Item -ItemType Directory -Path $WestScreens -Force | Out-Null
    $WestNames=@('04-town-lv1-West.png','04-town-lv3-West.png','04-town-lv6-West.png','west-connection.png','west-walking.png','west-door-arrived.png')
    foreach($Name in $WestNames){[IO.File]::WriteAllBytes((Join-Path $WestScreens $Name),$Header)}
    $Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $West 'west-review-info.json')
    @{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Digest;images=@(Get-WestCaptureEntries $WestScreens)} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $West 'west-capture-manifest.json')
    $East=Join-Path $Run 'east';$EastScreens=Join-Path $East 'screenshots';New-Item -ItemType Directory -Path $EastScreens -Force | Out-Null
    foreach($Name in @('04-town-lv1-East.png','04-town-lv3-East.png','04-town-lv6-East.png','east-connection.png','east-walking.png','east-door-arrived.png')){[IO.File]::WriteAllBytes((Join-Path $EastScreens $Name),$Header)}
    $Info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $East 'east-review-info.json')
    @{gitRevision=$Info.gitRevision;productFingerprint=$Info.productFingerprint;centralInputFingerprint=$Digest;images=@(Get-EastCaptureEntries $EastScreens)} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $East 'east-capture-manifest.json')
    function Write-NorthComparisonJpeg([string]$Directory){[IO.File]::WriteAllBytes((Join-Path $Directory 'north-before-candidate.jpg'),[byte[]]@(5,6,7))}
    $WestInfoPath=Join-Path $West 'west-review-info.json'
    $WrongInfo=@{gitRevision=$Info.gitRevision;productFingerprint=('d'*64);centralInputFingerprint=$Digest}
    $WrongInfo | ConvertTo-Json | Set-Content -LiteralPath $WestInfoPath
    Reject {Write-NorthReview $Root $Run}
    $Info | ConvertTo-Json | Set-Content -LiteralPath $WestInfoPath
    $EastInfoPath=Join-Path $East 'east-review-info.json'
    $WrongInfo | ConvertTo-Json | Set-Content -LiteralPath $EastInfoPath
    Reject {Write-NorthReview $Root $Run}
    $Info | ConvertTo-Json | Set-Content -LiteralPath $EastInfoPath
    $Missing=Join-Path $Before '04-town-lv3-North.png';Remove-Item -LiteralPath $Missing
    Reject {Write-NorthReview $Root $Run}
    if(Test-Path (Join-Path $Run 'north-quest-review.zip')){throw 'Incomplete Before produced a review archive.'}
    [IO.File]::WriteAllBytes($Missing,$Header)
    Move-Item -LiteralPath (Join-Path $Run 'north/screenshots') -Destination $NorthScreens
    Write-NorthReview $Root $Run
    $Expanded=Join-Path $Root 'expanded';Expand-Archive -LiteralPath (Join-Path $Run 'north-quest-review.zip') -DestinationPath $Expanded
    foreach($Name in $Names){if(-not(Test-Path (Join-Path $Expanded $Name))){throw "Archive lost north frame: $Name"}}
    $Meta=Get-Content -LiteralPath (Join-Path $Expanded 'north-adoption.json') -Raw | ConvertFrom-Json
    if($Meta.adopted -ne $false -or $Meta.pixel_verified -ne $false -or $Meta.native_pairs_verified -ne 3){throw 'Archive wrongly adopted or lost Before pairs.'}
    if(@(Get-CentralCaptureEntries $Screens).Count -ne 3){throw 'North archive disturbed companion central evidence.'}
    Write-Host 'Real north export/archive pipeline checked, missing Before refused; JPEG drawing stubbed; no native renders or Pixel execution.'
} finally {if(Test-Path $Root){Remove-Item -LiteralPath $Root -Recurse -Force}}

# The previous native checkpoint authorizes this sequence step; it never
# accepts the changed source. A Boolean without the original images is refused.
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-north-prerequisite-'+[guid]::NewGuid())
try {
    $Previous=Join-Path $Root 'References/Review/east-florist-2026-10-22-accepted'
    $Images=@(foreach($Area in @('central','west','east')) {
        $Directory=Join-Path $Previous "$Area/screenshots";New-Item -ItemType Directory -Path $Directory -Force | Out-Null
        $AreaNames=if($Area -eq 'central'){@('04-town-lv1-Central.png','04-town-lv3-Central.png','04-town-lv6-Central.png')}elseif($Area -eq 'west'){$WestNames}else{@('04-town-lv1-East.png','04-town-lv3-East.png','04-town-lv6-East.png','east-connection.png','east-walking.png','east-door-arrived.png')}
        foreach($Name in $AreaNames){$Path=Join-Path $Directory $Name;[IO.File]::WriteAllBytes($Path,$Header);@{file="$Area/screenshots/$Name";sha256=(Get-FileHash $Path -Algorithm SHA256).Hash.ToLowerInvariant()}}
    })
    $Checkpoint=@{reviewed_revision=('a'*40);identity_verified=$true;central=@{unity_stage_accepted=$true};west=@{unity_stage_accepted=$true};east=@{unity_stage_accepted=$true;nameplate_readability=$true};images=$Images}
    $Record=Join-Path $Previous 'native-review.json';$Checkpoint | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Record
    Assert-NorthReviewPrerequisites $Root
    $Checkpoint.west.unity_stage_accepted=$false;$Checkpoint | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Record
    Reject {Assert-NorthReviewPrerequisites $Root}
    $Checkpoint.west.unity_stage_accepted=$true;$Checkpoint | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Record
    $Checkpoint.east.nameplate_readability=$false;$Checkpoint | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Record
    Reject {Assert-NorthReviewPrerequisites $Root}
    $Checkpoint.east.nameplate_readability=$true;$Checkpoint | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Record
    [IO.File]::AppendAllText((Join-Path $Previous $Images[0].file),'changed');Reject {Assert-NorthReviewPrerequisites $Root}
    Write-Host 'North sequence prerequisite requires the accepted prior central/west/east checkpoint and unchanged fifteen images; no current-source adoption.'
} finally {if(Test-Path $Root){Remove-Item -LiteralPath $Root -Recurse -Force}}
