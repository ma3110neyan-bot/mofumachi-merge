[CmdletBinding()]
param([string]$UnityEditor = "$env:ProgramFiles\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe", [switch]$Qa,[switch]$TownQa,[switch]$CentralReview,[switch]$WestReview,[switch]$EastReview,[switch]$NorthReview,[switch]$SouthReview,[switch]$FiveAreaReview,[switch]$SouthFinalReview)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'visual-lock.ps1')
. (Join-Path $PSScriptRoot 'west-review.ps1')
. (Join-Path $PSScriptRoot 'east-review.ps1')
. (Join-Path $PSScriptRoot 'north-review.ps1')
. (Join-Path $PSScriptRoot 'south-review.ps1')
. (Join-Path $PSScriptRoot 'five-area-review.ps1')
. (Join-Path $PSScriptRoot 'south-final-review.ps1')
. (Join-Path $PSScriptRoot 'fountain-review.ps1')
. (Join-Path $PSScriptRoot 'north-noah-review.ps1')
function Assert-TestRun([string]$ResultPath,[int]$MinimumTests,[string[]]$RequiredSuites) {
    if (-not (Test-Path -LiteralPath $ResultPath)) { throw "Missing current test XML: $ResultPath" }
    [xml]$Results = Get-Content -LiteralPath $ResultPath -Raw
    $Run = $Results.'test-run'
    if ($null -eq $Run -or [int]$Run.total -lt $MinimumTests -or $Run.result -ne 'Passed' -or [int]$Run.failed -ne 0 -or [int]$Run.skipped -ne 0 -or [int]$Run.passed -ne [int]$Run.total) { throw "Incomplete or failed tests: $ResultPath" }
    $Cases=@($Results.SelectNodes('//test-case'))
    if($Cases.Count -ne [int]$Run.total -or @($Cases | Where-Object { $_.result -ne 'Passed' }).Count -gt 0) { throw "Not every test case passed: $ResultPath" }
    foreach($Suite in $RequiredSuites) {
        if(@($Results.SelectNodes('//test-suite') | Where-Object { $_.GetAttribute('name') -eq $Suite -and $_.GetAttribute('result') -eq 'Passed' -and $_.SelectNodes('.//test-case').Count -gt 0 }).Count -eq 0) { throw "Missing passed suite: $Suite" }
    }
    foreach($Gate in @(@{Name='CharacterSelectionTests';Count=4},@{Name='DailyWorldTests';Count=13},@{Name='MajorPolishTests';Count=6},@{Name='SeamlessFieldTests';Count=8},@{Name='EastWestHomeTests';Count=4},@{Name='ResidentPresenceTests';Count=5},@{Name='FivePlazaTests';Count=5},@{Name='BaseProductLoopTests';Count=5},@{Name='VisualLockPresentationTests';Count=2},@{Name='CentralMasterReviewTests';Count=2},@{Name='WestMasterReviewTests';Count=3},@{Name='EastMasterReviewTests';Count=3},@{Name='NorthMasterReviewTests';Count=3},@{Name='SouthMasterReviewTests';Count=3},@{Name='FiveAreaIntegrationReviewTests';Count=1},@{Name='NorthEntryBlendReviewTests';Count=3},@{Name='Noah25DTrialPresentationTests';Count=4})) {
        if($Gate.Name -in $RequiredSuites) {
            $Suite=$Results.SelectNodes('//test-suite') | Where-Object {$_.GetAttribute('name') -eq $Gate.Name} | Select-Object -First 1
            if($Suite.SelectNodes('.//test-case').Count -ne $Gate.Count){throw "Incomplete suite: $($Gate.Name), expected $($Gate.Count) cases."}
        }
    }
    Write-Host "Tests passed: $($Run.passed)/$($Run.total)"
}
function Read-SourceRevision([string]$RepositoryPath) {
    $GitDirectory=Join-Path $RepositoryPath '.git'
    if(Test-Path -LiteralPath $GitDirectory -PathType Leaf) {
        $GitLink=(Get-Content -LiteralPath $GitDirectory -Raw).Trim()
        if($GitLink -notmatch '^gitdir: (.+)$'){throw 'Invalid .git link; source SHA is required.'}
        $LinkedDirectory=$Matches[1]
        if(-not [IO.Path]::IsPathRooted($LinkedDirectory)){$LinkedDirectory=Join-Path $RepositoryPath $LinkedDirectory}
        $GitDirectory=[IO.Path]::GetFullPath($LinkedDirectory)
    }
    $HeadPath=Join-Path $GitDirectory 'HEAD'
    if(-not(Test-Path -LiteralPath $HeadPath -PathType Leaf)){throw 'Cannot read source HEAD; source SHA is required.'}
    $SourceHead=(Get-Content -LiteralPath $HeadPath -Raw).Trim()
    if($SourceHead -match '^[a-fA-F0-9]{40}$'){return $SourceHead.ToLowerInvariant()}
    if($SourceHead -notmatch '^ref: (refs/[A-Za-z0-9_./-]+)$'){throw 'Invalid source HEAD.'}
    $ReferenceName=$Matches[1]
    if($ReferenceName.Contains('..')){throw 'Invalid source reference.'}
    $CommonDirectory=$GitDirectory;$CommonPath=Join-Path $GitDirectory 'commondir'
    if(Test-Path -LiteralPath $CommonPath -PathType Leaf){$CommonDirectory=[IO.Path]::GetFullPath((Join-Path $GitDirectory (Get-Content -LiteralPath $CommonPath -Raw).Trim()))}
    foreach($Directory in @($GitDirectory,$CommonDirectory)) {
        $RefPath=Join-Path $Directory $ReferenceName
        if(Test-Path -LiteralPath $RefPath -PathType Leaf) {
            $SourceSha=(Get-Content -LiteralPath $RefPath -Raw).Trim()
            if($SourceSha -match '^[a-fA-F0-9]{40}$'){return $SourceSha.ToLowerInvariant()}
            throw 'Invalid source SHA.'
        }
        $PackedPath=Join-Path $Directory 'packed-refs'
        if(Test-Path -LiteralPath $PackedPath -PathType Leaf) {
            $PackedPattern='^([a-fA-F0-9]{40}) '+[regex]::Escape($ReferenceName)+'$'
            foreach($PackedLine in Get-Content -LiteralPath $PackedPath) {if($PackedLine -match $PackedPattern){return $Matches[1].ToLowerInvariant()}}
        }
    }
    throw 'Source SHA not found; do not build an unidentified APK.'
}
function Assert-NativeCaptures([string]$Directory,[switch]$RequireBoundary,[switch]$RequireField,[switch]$RequireGrowth,[switch]$RequireFountain,[switch]$RequireFountainShow,[switch]$RequireNorthNoah,[string]$ExpectedRevision='',[string]$ExpectedFingerprint='') {
    $Images=@(Get-ChildItem -LiteralPath $Directory -Filter '*.png' -File -ErrorAction SilentlyContinue)
    if($Images.Count -lt 102){throw "Native captures incomplete: $($Images.Count)/102. Inspect the same-run PlayMode log."}
    foreach($Name in @('15-wide-park-left','15-wide-park-right','16-residents-passing-00','17-finger-follow-00','17-finger-released','18-continuous-walk-west','18-continuous-walk-central','18-continuous-walk-east','19-book-new-in-town','19-book-discovery-first-page','20-quest-cabin','20-quest-door-opening','20-quest-inside','21-shop-cabin','21-shop-door-opening','21-shop-inside','22-title-candidates','22-merge-candidates','23-title-letter','23-title-garden','23-merge-picnic','23-merge-ribbon','23-merge-pop-promenade','24-home-unobstructed','24-menu-open','24-menu-return','25-character-idle','25-character-step-a','25-character-step-b','25-character-greeting','25-character-behind-fountain','25-character-in-front-fountain','04-town-lv1-North','04-town-lv1-South','04-town-lv6-North','04-town-lv6-South','09-connected-path-north','09-connected-path-south','26-cardinal-North-join','26-cardinal-North-arrival','26-cardinal-South-join','26-cardinal-South-arrival','27-merge-finale-live-purpose','20-quest-arrived-door-prompt','21-shop-arrived-door-prompt','21b-cafe-cabin','21b-cafe-walking','21b-cafe-arrived-door-prompt','21b-cafe-door-opening','21b-cafe-inside','28-meet-friend-walking','28-meet-friend-arrived','29-merge-drag-lift','29-merge-drag-match','29-merge-landed')) {
        if(-not(Test-Path -LiteralPath (Join-Path $Directory ($Name+'.png')) -PathType Leaf)){throw "Missing native capture: $Name"}
    }
    foreach($Image in $Images) {
        $Stream=[IO.File]::OpenRead($Image.FullName)
        try {$Header=New-Object byte[] 24;$Read=$Stream.Read($Header,0,24)} finally {$Stream.Dispose()}
        if($Read -ne 24 -or $Image.Length -le 24 -or ($Header[0..7] -join ',') -ne '137,80,78,71,13,10,26,10' -or [Text.Encoding]::ASCII.GetString($Header,12,4) -ne 'IHDR'){throw "Invalid PNG header: $($Image.Name)"}
        $Width=([int64]$Header[16]*16777216)+([int64]$Header[17]*65536)+([int64]$Header[18]*256)+$Header[19]
        $Height=([int64]$Header[20]*16777216)+([int64]$Header[21]*65536)+([int64]$Header[22]*256)+$Header[23]
        if($Width -ne 720 -or $Height -ne 1280){throw "Wrong native capture size: $($Image.Name) ($Width x $Height)"}
    }
    Write-Host "Native Unity images: $($Images.Count), 720x1280 (visual review still required)."
    if($RequireNorthNoah){
        if($Images.Count -lt 256){throw 'North/Noah TownQa requires the existing219 full captures plus36 candidate frames and one actual historical North original; natural-caption context PNGs are additional.'}
        Assert-NorthNoahEvidence $Directory $ExpectedRevision $ExpectedFingerprint -RequireQuality
    }
    $BoundaryComparison=Join-Path $Directory 'street-boundary-before-after.html'
    if($RequireBoundary) {
        if($Images.Count -lt 124 -or -not(Test-Path -LiteralPath $BoundaryComparison -PathType Leaf)){throw 'Town boundary comparison requires124 captures and its comparison HTML.'}
        foreach($View in @('west-upper','east-upper','west-lower','east-lower','north-left','north-right','south-left','south-right','south-entry','west-join','east-join')) {
            foreach($Phase in @('before-reconstructed','after')) {
                if(-not(Test-Path -LiteralPath (Join-Path $Directory "30-boundary-$View-$Phase.png") -PathType Leaf)){throw "Missing town boundary comparison: $View/$Phase"}
            }
        }
        $Boundary=Get-Content -LiteralPath (Join-Path $Directory 'street-boundary-comparison.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        if($Boundary.kind -ne 'same-state-native-renderer-comparison' -or $Boundary.pairs -ne 11 -or $Boundary.after -ne 'product code' -or $Boundary.pixel_verified -isnot [bool] -or $Boundary.pixel_verified -ne $false -or $Boundary.adopted -isnot [bool] -or $Boundary.adopted -ne $false){throw 'Town boundary comparison is incomplete or incorrectly claims adoption.'}
    }
    if($RequireField) {
        if($Images.Count -lt 138){throw 'Field stability requires the existing124 and14 focused native captures.'}
        $FieldFiles=@('31-field-fountain-front.png','31-field-fountain-behind.png','31-field-east-corner-out.png','31-field-east-corner-return.png')+@(foreach($Area in @('East','West','North','South','Central')){foreach($Phase in @('edge','return')){"31-field-$Area-$Phase.png"}})
        foreach($Name in $FieldFiles){if(-not(Test-Path -LiteralPath (Join-Path $Directory $Name) -PathType Leaf)){throw "Missing field stability capture: $Name"}}
        $Field=Get-Content -LiteralPath (Join-Path $Directory 'field-stability.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        if($Field.kind -ne 'native-walking-and-depth-fixture' -or $Field.edge_return_pairs -ne 5){throw 'Incomplete field stability evidence.'}
        foreach($Name in @('east_corner_return','fountain_front_and_behind')){if($Field.$Name -isnot [bool] -or $Field.$Name -ne $true){throw 'Field stability walking/depth verification is incomplete.'}}
        foreach($Name in @('teleport_rescue','pixel_verified','adopted')){if($Field.$Name -isnot [bool] -or $Field.$Name -ne $false){throw 'Field fixture must not use teleport rescue or claim Pixel/adoption.'}}
    }
    if($RequireGrowth) {
        if($Images.Count -lt 198){throw 'All-level growth requires138 field captures plus60 actual delivery/five-district/reload captures.'}
        foreach($Level in 0..5){foreach($District in @('Central','West','East','North','South')) {
            foreach($Name in @("32-growth-$Level-to-$($Level+1)-$District.png","32-growth-$($Level+1)-$District-reloaded.png")) {
                if(-not(Test-Path -LiteralPath (Join-Path $Directory $Name) -PathType Leaf)){throw "Missing actual progression capture: $Name"}
            }
        }}
        $Growth=Get-Content -LiteralPath (Join-Path $Directory 'town-growth-progression.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        if($Growth.kind -ne 'isolated-real-delivery-growth-and-reload' -or @($Growth.transitions).Count -ne 6 -or $Growth.initial_level -ne 0 -or $Growth.initial_coins -ne 0 -or $Growth.direct_level_assignment -isnot [bool] -or $Growth.direct_level_assignment){throw 'All six actual-delivery transitions must start from the initial town without direct level assignment.'}
        $ExpectedCoins=@(30,70,110,180,250,330);$ExpectedQuests=@('tea-01','bread-01','flower-01','tea-02','flower-02','bread-02')
        $ExpectedNeeds=@('tea:2:1','bread:2:1','flower:2:1','bread:2:1|tea:3:1','flower:3:1|tea:2:1','bread:3:1|flower:2:1')
        foreach($Level in 0..5) {
            $Rows=@($Growth.transitions | Where-Object {$_.from -eq $Level -and $_.to -eq ($Level+1)})
            if($Rows.Count -ne 1){throw 'Missing or duplicate town level transition.'}
            $Row=$Rows[0]
            if($Row.coins -ne $ExpectedCoins[$Level] -or $Row.quest_id -ne $ExpectedQuests[$Level] -or $Row.completed_quests -ne ($Level+1) -or (@($Row.requirements | Sort-Object) -join '|') -ne $ExpectedNeeds[$Level] -or $Row.districts_checked -ne 5 -or $Row.reload_matched -isnot [bool] -or -not $Row.reload_matched -or $Row.reward_replay_rejected -isnot [bool] -or -not $Row.reward_replay_rejected -or @($Row.districts).Count -ne 5){throw 'Delivery requirements/reward/five-district reload proof is incomplete.'}
            foreach($District in @('Central','West','East','North','South')) {
                $Areas=@($Row.districts | Where-Object {$_.id -eq $District})
                $Changed=-not(($Level -le 1 -and $District -in @('North','South')) -or ($Level -in @(3,4) -and $District -eq 'Central'))
                if($Areas.Count -ne 1 -or $Areas[0].changed -isnot [bool] -or $Areas[0].changed -ne $Changed -or $Areas[0].reload_matched -isnot [bool] -or -not $Areas[0].reload_matched){throw 'Each district must prove its actual authored change/unchanged stage and persisted reload.'}
            }
        }
        foreach($Name in @('pixel_verified','adopted')){if($Growth.$Name -isnot [bool] -or $Growth.$Name -ne $false){throw 'Isolated native growth cannot claim Pixel or final adoption.'}}
    }
    if($RequireFountain) {
        if($Images.Count -lt 208){throw 'Fountain water requires198 field/growth and10 paired/time-sample captures.'}
        foreach($View in @('overview','front','behind')){foreach($Phase in @('before','after')){if(-not(Test-Path -LiteralPath (Join-Path $Directory "33-fountain-$View-$Phase.png") -PathType Leaf)){throw "Missing fountain pair: $View/$Phase"}}}
        foreach($Frame in 0..3){if(-not(Test-Path -LiteralPath (Join-Path $Directory ('33-fountain-phase-'+$Frame.ToString('00')+'.png')) -PathType Leaf)){throw 'Missing fountain animation time sample.'}}
        if(-not(Test-Path -LiteralPath (Join-Path $Directory 'fountain-water-before-after.html') -PathType Leaf)){throw 'Missing fountain comparison document.'}
        $Water=Get-Content -LiteralPath (Join-Path $Directory 'fountain-water-comparison.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        if($Water.kind -ne 'same-state-native-additive-water-comparison' -or $Water.before -ne 'new layer OFF; original fountain and original water retained' -or $Water.after -ne 'product code' -or @($Water.pairs).Count -ne 3 -or @($Water.phases).Count -ne 4 -or $Water.added_vertices -ne 784 -or $Water.max_update_hz -ne 30){throw 'Incomplete fountain comparison or mesh/update budget.'}
        foreach($View in @('overview','front','behind')) {
            $Rows=@($Water.pairs | Where-Object {$_.view -eq $View})
            if($Rows.Count -ne 1 -or $Rows[0].changed_pixels -le 200 -or $Rows[0].outside_fountain_pixels -ne 0 -or $Rows[0].state_unchanged -isnot [bool] -or -not $Rows[0].state_unchanged){throw 'Fountain pair must substantially change only the fountain, preserving the same state.'}
        }
        if(@($Water.phases | Select-Object -Unique).Count -ne 4){throw 'Fountain animation samples must use four different phases.'}
        foreach($Phase in $Water.phases){$Value=[double]$Phase;if([double]::IsNaN($Value) -or [double]::IsInfinity($Value) -or $Value -lt 0 -or $Value -ge 10){throw 'Invalid fountain animation phase.'}}
        foreach($Name in @('pixel_verified','adopted')){if($Water.$Name -isnot [bool] -or $Water.$Name -ne $false){throw 'Native fountain comparison cannot claim Pixel or adoption.'}}
    }
    if($RequireFountainShow) {
        if($Images.Count -lt 219){throw 'Timed fountain show requires219 full native captures plus real-time crops and actual prior Before originals.'}
        foreach($Name in @('34-fountain-overview-prelude.png','34-fountain-overview-show.png','34-fountain-front-show.png','34-fountain-behind-show.png','34-fountain-overview-show-off.png','34-fountain-front-show-off.png','34-fountain-behind-show-off.png','34-fountain-overview-recovered.png','fountain-show-before-after.html')){if(-not(Test-Path -LiteralPath (Join-Path $Directory $Name) -PathType Leaf)){throw "Missing timed fountain evidence: $Name"}}
        $Show=Get-Content -LiteralPath (Join-Path $Directory 'fountain-show-comparison.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        if($Show.kind -ne 'prior-native-before-normal-and-timed-show' -or $Show.display_scale -ne 1.14 -or $Show.cycle_seconds -ne 45 -or $Show.release_cycle_seconds -ne 180 -or $Show.prelude_seconds -ne 2 -or $Show.show_seconds -ne 6 -or $Show.recovery_seconds -ne 1 -or $Show.show_elapsed -ne 41 -or $Show.jet_evidence -ne 'same-instant-added-layer-off-on-above-normal-uv084' -or @($Show.show_pairs).Count -ne 3){throw 'Timed show duration/scale or native comparison is incomplete.'}
        foreach($View in @('overview','front','behind')) {
            $Rows=@($Show.show_pairs | Where-Object {$_.view -eq $View})
            if($Rows.Count -ne 1 -or $Rows[0].changed_pixels -le 300 -or $Rows[0].state_unchanged -isnot [bool] -or -not $Rows[0].state_unchanged){throw 'Show must visibly differ from normal while preserving the same gameplay state.'}
            if($Rows[0].show_jet_pixels -le 40 -or $Rows[0].outside_fountain_pixels -ne 0 -or $Rows[0].clock_unchanged -isnot [bool] -or -not $Rows[0].clock_unchanged -or $Rows[0].original_water_phase_unchanged -isnot [bool] -or -not $Rows[0].original_water_phase_unchanged){throw 'Show jets require isolated above-normal pixels with unchanged clocks and zero outside the fountain.'}
        }
        $Before=Get-Content -LiteralPath (Join-Path $PSScriptRoot '../References/VisualLock/fountain-final-current-before-2026-10-27.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        if($Show.before.source_revision -ne $Before.source_revision -or $Show.before.run -ne $Before.run -or @($Show.before.images).Count -ne 3){throw 'Current Before must identify the actual98596daf native run.'}
        foreach($Image in $Before.images) {
            $Rows=@($Show.before.images | Where-Object {$_.view -eq $Image.view})
            if($Rows.Count -ne 1 -or $Rows[0].file -ne $Image.file -or $Rows[0].sha256 -ne $Image.sha256 -or $Rows[0].bytes -ne $Image.bytes){throw 'Prior Before image identity was changed.'}
            $Path=Join-Path $Directory ('fountain-before/'+$Image.file)
            if(-not(Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Image.sha256){throw 'Prior Before PNG is missing or changed.'}
        }
        foreach($Name in @('pixel_verified','adopted')){if($Show.$Name -isnot [bool] -or $Show.$Name -ne $false){throw 'Native show cannot claim Pixel or adoption.'}}
        foreach($Name in @('35-fountain-live-normal.png','35-fountain-live-show.png','35-fountain-live-recovered.png')){if(-not(Test-Path -LiteralPath (Join-Path $Directory $Name) -PathType Leaf)){throw "Missing actual live full frame: $Name"}}
        Assert-FountainLiveSequence $Directory
    }
    if(Test-Path -LiteralPath $BoundaryComparison){Write-Host "Street boundary comparison: $BoundaryComparison (Boundary-only renderer comparison; Pixel acceptance pending)."}
}
function Assert-SourceUnchanged([string]$RepositoryPath,[string]$ExpectedRevision) {
    if((Read-SourceRevision $RepositoryPath) -ne $ExpectedRevision){throw 'Source commit changed during acceptance; repeat the same-run tests and build.'}
}
function Write-NativeReviewArchives([string]$Directory) {
    # Keep review uploads under 32 MiB without asking the player to copy a long
    # packing command. APK bytes stay in the run directory, outside review ZIPs.
    $Files=@(foreach($Name in @('editmode.xml','playmode.xml','build-info.json','growth-build-info.json','capture-manifest.json','android-build.log','growth-android-build.log','playmode.log','before-after.html','visual-adoption.json','screenshots/texture-memory.txt','screenshots/README.txt','screenshots/street-boundary-before-after.html','screenshots/street-boundary-comparison.json','screenshots/field-stability.json','screenshots/town-growth-progression.json','screenshots/fountain-water-before-after.html','screenshots/fountain-water-comparison.json','screenshots/fountain-show-comparison.json','screenshots/fountain-show-before-after.html','screenshots/north-noah-comparison.json','screenshots/north-noah-before-after.html','screenshots/noah-before-after.html')) {
        $Path=Join-Path $Directory $Name
        if(Test-Path -LiteralPath $Path -PathType Leaf){Get-Item -LiteralPath $Path}
    })+@(Get-ChildItem -LiteralPath (Join-Path $Directory 'screenshots') -Filter '*.png' -File | Sort-Object Name)+@(Get-ChildItem -LiteralPath (Join-Path $Directory 'screenshots') -Filter '*.jpg' -File | Sort-Object Name)
    $BeforeDir=Join-Path $Directory 'screenshots/fountain-before'
    if(Test-Path -LiteralPath $BeforeDir){$Files+=@(Get-ChildItem -LiteralPath $BeforeDir -Filter '*.png' -File | Sort-Object Name)}
    $LiveDir=Join-Path $Directory 'screenshots/fountain-live'
    if(Test-Path -LiteralPath $LiveDir){$Files+=@(Get-ChildItem -LiteralPath $LiveDir -File | Sort-Object Name)}
    $NoahDir=Join-Path $Directory 'screenshots/noah-sequence'
    if(Test-Path -LiteralPath $NoahDir){$Files+=@(Get-ChildItem -LiteralPath $NoahDir -Filter '*.png' -File | Sort-Object Name)}
    $Batch=New-Object 'System.Collections.Generic.List[string]';$Bytes=0L;$Part=1
    foreach($File in $Files) {
        if($File.Length -gt 20MB){throw "Review file exceeds upload budget: $($File.FullName)"}
        if($Batch.Count -gt 0 -and $Bytes+$File.Length -gt 20MB) {
            Compress-Archive -LiteralPath $Batch.ToArray() -DestinationPath (Join-Path $Directory ('five-plaza-review-{0:D2}.zip' -f $Part)) -Force
            $Part++;$Batch.Clear();$Bytes=0L
        }
        $Batch.Add($File.FullName);$Bytes+=$File.Length
    }
    if($Batch.Count -gt 0){Compress-Archive -LiteralPath $Batch.ToArray() -DestinationPath (Join-Path $Directory ('five-plaza-review-{0:D2}.zip' -f $Part)) -Force}
    Write-Host "Review ZIPs: $Directory\five-plaza-review-*.zip (Unity fixture images; Pixel acceptance pending)."
}
function Find-SourceGit {
    foreach($Name in @('git.exe','git')) {
        $Command=Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if($Command){return $Command.Source}
    }
    if($env:LOCALAPPDATA) {
        $Bundled=Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA 'GitHubDesktop/app-*/resources/app/git/cmd/git.exe') -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if($Bundled){return $Bundled.FullName}
    }
    throw 'Git was not found, including GitHub Desktop bundled Git. Cannot verify clean product inputs.'
}
function Source-InputPaths {
    # Unity rewrites local graphics/build-profile caches. Product inputs must be
    # committed; cache deviations are recorded separately instead of discarded.
    return @('Assets/Mofumachi','Assets/Mofumachi.meta','Assets/Scenes','Assets/Scenes.meta','Packages','ProjectSettings/ProjectVersion.txt','scripts','References/VisualLock')
}
function Assert-CleanProductSource([string]$RepositoryPath,[string]$GitPath) {
    $Changes=@(& $GitPath -C $RepositoryPath status --porcelain=v1 --untracked-files=all -- @(Source-InputPaths))
    if($LASTEXITCODE -ne 0){throw 'Cannot inspect product source status.'}
    if($Changes.Count -gt 0){throw ('Product inputs have uncommitted changes. Commit the candidate before same-run acceptance; no files were changed by this check.'+"`n"+($Changes -join "`n"))}
}
function Get-ProductFingerprint([string]$RepositoryPath) {
    $Entries=@(foreach($Relative in Source-InputPaths) {
        $Path=Join-Path $RepositoryPath $Relative
        if(-not(Test-Path -LiteralPath $Path)){continue}
        $Files=if(Test-Path -LiteralPath $Path -PathType Leaf){Get-Item -LiteralPath $Path -Force}else{Get-ChildItem -LiteralPath $Path -File -Recurse -Force}
        foreach($File in $Files){$Name=$File.FullName.Substring($RepositoryPath.TrimEnd('/','\').Length+1).Replace('\','/');$Name+':'+(Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash}
    })
    $Hasher=[Security.Cryptography.SHA256]::Create()
    try {return ([BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes((($Entries | Sort-Object) -join "`n"))))).Replace('-','').ToLowerInvariant()} finally {$Hasher.Dispose()}
}
function Assert-ProductUnchanged([string]$RepositoryPath,[string]$ExpectedRevision,[string]$ExpectedFingerprint) {
    Assert-SourceUnchanged $RepositoryPath $ExpectedRevision
    if((Get-ProductFingerprint $RepositoryPath) -ne $ExpectedFingerprint){throw 'Product inputs changed during acceptance; repeat tests, captures and APK together.'}
    $LockPath=Join-Path $RepositoryPath 'References/VisualLock/base-2026-10-21.json'
    if(Test-Path -LiteralPath $LockPath){Assert-OriginalBaseForCandidate $RepositoryPath}
    if(Test-Path -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/approved-design-2026-10-21.json')){Assert-ApprovedVisualLock $RepositoryPath}
}
function Invoke-Unity([string[]]$UnityArguments) {
    $ArgumentText=($UnityArguments | ForEach-Object { '"'+$_+'"' }) -join ' '
    $Process=Start-Process -FilePath $UnityEditor -ArgumentList $ArgumentText -Wait -PassThru
    if($Process.ExitCode -ne 0){throw "Unity exit $($Process.ExitCode). Inspect $OutputDir"}
}
# Dot sourcing exposes the same gates to script tests without starting Unity.
if($MyInvocation.InvocationName -eq '.'){return}
if($SouthFinalReview -and ($CentralReview -or $WestReview -or $EastReview -or $NorthReview -or $SouthReview -or $FiveAreaReview -or $Qa -or $TownQa)){throw 'Select SouthFinalReview on its own.'}
if($SouthFinalReview){$FiveAreaReview=$true}
if($Qa -and $TownQa){throw 'Select -Qa or -TownQa, not both.'}
if(($CentralReview -or $WestReview -or $EastReview -or $NorthReview -or $SouthReview -or $FiveAreaReview) -and ($Qa -or $TownQa)){throw 'Central review is a native image checkpoint, separate from APK generation.'}
if(@($CentralReview,$WestReview,$EastReview,$NorthReview,$SouthReview,$FiveAreaReview | Where-Object {$_}).Count -gt 1){throw 'Select one native review mode.'}
$ProjectPath=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if($ProjectPath -match '[^\x00-\x7F]'){throw 'Move the entire repository to an ASCII path before Android builds.'}
if(-not(Test-Path -LiteralPath $UnityEditor -PathType Leaf)){throw 'Pass -UnityEditor with Unity 6000.6.4f1 Unity.exe.'}
$UnityEditor=(Resolve-Path -LiteralPath $UnityEditor).Path
if((Get-Content (Join-Path $ProjectPath 'ProjectSettings\ProjectVersion.txt') -Raw) -notmatch 'm_EditorVersion: 6000\.6\.4f1\s'){throw 'Wrong project Unity version.'}
$GitRevision=Read-SourceRevision $ProjectPath
$SourceGit=Find-SourceGit
Assert-CleanProductSource $ProjectPath $SourceGit
Assert-OriginalBaseForCandidate $ProjectPath
Assert-ApprovedVisualLock $ProjectPath
if(-not ($CentralReview -or $WestReview -or $EastReview -or $NorthReview -or $SouthReview -or $FiveAreaReview)){Assert-BaseStagesReadyForAcceptance $ProjectPath}
if($FiveAreaReview){Assert-FiveAreaReviewPrerequisites $ProjectPath}elseif($SouthReview){Assert-SouthReviewPrerequisites $ProjectPath}elseif($NorthReview){Assert-NorthReviewPrerequisites $ProjectPath}elseif($EastReview){Assert-EastReviewPrerequisites $ProjectPath}
$ProductFingerprint=Get-ProductFingerprint $ProjectPath
$ConfigurationChanges=@(& $SourceGit -C $ProjectPath diff --name-only HEAD -- Assets/Settings ProjectSettings)
if($LASTEXITCODE -ne 0){throw 'Cannot record local Unity configuration deviations.'}
$RunName=(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8)
$OutputDir=if($FiveAreaReview){Join-Path $ProjectPath "Builds\FiveAreaReview\$RunName"}elseif($SouthReview){Join-Path $ProjectPath "Builds\SouthReview\$RunName"}elseif($NorthReview){Join-Path $ProjectPath "Builds\NorthReview\$RunName"}elseif($EastReview){Join-Path $ProjectPath "Builds\EastReview\$RunName"}elseif($WestReview){Join-Path $ProjectPath "Builds\WestReview\$RunName"}elseif($CentralReview){Join-Path $ProjectPath "Builds\CentralReview\$RunName"}else{Join-Path $ProjectPath "Builds\Android\$RunName"}
New-Item -ItemType Directory -Path $OutputDir | Out-Null
if($CentralReview -or $WestReview -or $EastReview -or $NorthReview -or $SouthReview -or $FiveAreaReview) {
    $CentralInputFingerprint=Get-CentralInputFingerprint $ProjectPath
    $CentralXml=Join-Path $OutputDir 'central-playmode.xml'
    $ReviewFlag=if($FiveAreaReview){'-mofumachiFiveAreaReview'}elseif($SouthReview){'-mofumachiSouthReview'}elseif($NorthReview){'-mofumachiNorthReview'}elseif($EastReview){'-mofumachiEastReview'}elseif($WestReview){'-mofumachiWestReview'}else{'-mofumachiCentralReview'}
    $Filter='Mofumachi.Tests.ExperienceCaptureTests.PortraitScreensRenderFromIsolatedStateWithoutWritingProgress;Mofumachi.Tests.CentralMasterReviewTests'
    $Suites=@('ExperienceCaptureTests','CentralMasterReviewTests');$ExpectedTests=3
    if($WestReview -or $EastReview -or $NorthReview -or $SouthReview -or $FiveAreaReview){$Filter+=';Mofumachi.Tests.WestMasterReviewTests';$Suites+='WestMasterReviewTests';$ExpectedTests=6}
    if($EastReview -or $NorthReview -or $SouthReview -or $FiveAreaReview){$Filter+=';Mofumachi.Tests.EastMasterReviewTests';$Suites+='EastMasterReviewTests';$ExpectedTests=9}
    if($NorthReview -or $SouthReview -or $FiveAreaReview){$Filter+=';Mofumachi.Tests.NorthMasterReviewTests';$Suites+='NorthMasterReviewTests';$ExpectedTests=12}
    if($SouthReview -or $FiveAreaReview){$Filter+=';Mofumachi.Tests.SouthMasterReviewTests';$Suites+='SouthMasterReviewTests';$ExpectedTests=15}
    if($FiveAreaReview){$Filter+=';Mofumachi.Tests.FiveAreaIntegrationReviewTests';$Suites+='FiveAreaIntegrationReviewTests';$ExpectedTests=16}
    Write-Host 'Rendering the central master (Lv1/3/6); district review modes also capture their levels, connection, walking and door. No APK will be generated.'
    Invoke-Unity @('-batchmode','-projectPath',$ProjectPath,'-buildTarget','Android','-runTests','-testPlatform','PlayMode','-testFilter',$Filter,'-testResults',$CentralXml,$ReviewFlag,'-mofumachiScreenshots',(Join-Path $OutputDir 'screenshots'),'-logFile',(Join-Path $OutputDir 'central-playmode.log'))
    Assert-TestRun $CentralXml $ExpectedTests $Suites
    [xml]$CentralResults=Get-Content -LiteralPath $CentralXml -Raw
    if([int]$CentralResults.'test-run'.total -ne $ExpectedTests){throw 'Native review did not run the exact expected tests.'}
    Assert-ProductUnchanged $ProjectPath $GitRevision $ProductFingerprint
    $CaptureEntries=@(Get-CentralCaptureEntries (Join-Path $OutputDir 'screenshots'))
    [ordered]@{gitRevision=$GitRevision;productFingerprint=$ProductFingerprint;centralInputFingerprint=$CentralInputFingerprint;scope='central native Unity fixture; not Pixel';width=720;height=1280;images=$CaptureEntries} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDir 'capture-manifest.json') -Encoding UTF8
    $ReviewScope=if($FiveAreaReview){'five-area native fixture review; not Pixel'}else{'central review only'}
    $ReviewImageCount=if($FiveAreaReview){27}else{3}
    [ordered]@{gitRevision=$GitRevision;productFingerprint=$ProductFingerprint;centralInputFingerprint=$CentralInputFingerprint;unity='6000.6.4f1';scope=$ReviewScope;nativeCaptureCount=$ReviewImageCount;localUnityConfigurationChanges=$ConfigurationChanges;apk_generated=$false;pixel_verified=$false;adopted=$false} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDir 'central-review-info.json') -Encoding UTF8
    if($SouthFinalReview){Write-FiveAreaReview $ProjectPath $OutputDir -StageOnly;Write-SouthFinalReview $ProjectPath $OutputDir}elseif($FiveAreaReview){Write-FiveAreaReview $ProjectPath $OutputDir}else{
    Write-CentralReview $ProjectPath $OutputDir
    if($WestReview -or $EastReview -or $NorthReview -or $SouthReview -or $FiveAreaReview){Write-WestReview $ProjectPath $OutputDir}
    if($EastReview -or $NorthReview -or $SouthReview -or $FiveAreaReview){Write-EastReview $ProjectPath $OutputDir}
    if($NorthReview -or $SouthReview -or $FiveAreaReview){Write-NorthReview $ProjectPath $OutputDir}
    if($SouthReview){Write-SouthReview $ProjectPath $OutputDir}
    }
    if(($WestReview -or $EastReview -or $NorthReview -or $SouthReview -or $FiveAreaReview) -and $env:OS -eq "Windows_NT"){Start-Process explorer.exe -ArgumentList @($OutputDir)}
    return
}
$EditXml=Join-Path $OutputDir 'editmode.xml'
Write-Host '[1/4] Running all EditMode tests...'
Invoke-Unity @('-batchmode','-nographics','-projectPath',$ProjectPath,'-buildTarget','Android','-runTests','-testPlatform','EditMode','-testResults',$EditXml,'-logFile',(Join-Path $OutputDir 'editmode.log'))
Assert-TestRun $EditXml 144 @('PreferencesTests','AndroidBuildTests','PresentationContentTests','QaIdentityTests','DeliveryTests','SaveServiceTests','CharacterPreferenceTests','QuestCatalogTests','ItemRemovalTests','TownMovementTests','ChapterProgressTests','TownFrameClockTests','TownPlaceCatalogTests','TownViewportTests','TownRegionCatalogTests','TownRegionJourneyTests','TownRegionContentTests')
Assert-ProductUnchanged $ProjectPath $GitRevision $ProductFingerprint
$PlayXml=Join-Path $OutputDir 'playmode.xml'
Write-Host '[2/4] Running all PlayMode tests and capturing native Unity images...'
Invoke-Unity @('-batchmode','-projectPath',$ProjectPath,'-buildTarget','Android','-runTests','-testPlatform','PlayMode','-testResults',$PlayXml,'-mofumachiScreenshots',(Join-Path $OutputDir 'screenshots'),'-mofumachiSourceRevision',$GitRevision,'-mofumachiProductFingerprint',$ProductFingerprint,'-logFile',(Join-Path $OutputDir 'playmode.log'))
Assert-TestRun $PlayXml 234 @('VerticalSliceFlowTests','LayoutNoticeTests','AudioManagerTests','MergeInteractionTests','RewardGrowthTests','SettingsScreenTests','PopPresentationTests','CharacterSelectionTests','MultiQuestPresentationTests','TownLifeTests','StoryTypographyTests','TownMotionPresentationTests','ChapterFlowTests','MergePolishTests','MergeProgressionUiTests','MergeGuidanceTests','ProductionFrameUiTests','TownViewportPresentationTests','TownHabitTests','TownRegionFlowTests','TownRegionLifeTests','QualityPassTravelTests','QualityPassStoryGrowthTests','QualityPassMergeTests','ExperienceWalkingTests','ExperienceMergeTests','ExperienceMusicTests','ExperienceGrowthTests','ExperienceCaptureTests','DailyWorldTests','MajorPolishTests','SeamlessFieldTests','EastWestHomeTests','ResidentPresenceTests','FivePlazaTests','BaseProductLoopTests','VisualLockPresentationTests','CentralMasterReviewTests','WestMasterReviewTests','EastMasterReviewTests','NorthMasterReviewTests','SouthMasterReviewTests','FiveAreaIntegrationReviewTests','NorthEntryBlendReviewTests','Noah25DTrialPresentationTests')
$CaptureDir=Join-Path $OutputDir 'screenshots'
Assert-ProductUnchanged $ProjectPath $GitRevision $ProductFingerprint
Assert-NativeCaptures $CaptureDir -RequireBoundary:$TownQa -RequireField:$TownQa -RequireGrowth:$TownQa -RequireFountain:$TownQa -RequireFountainShow:$TownQa -RequireNorthNoah:$TownQa -ExpectedRevision $GitRevision -ExpectedFingerprint $ProductFingerprint
$CaptureEntries=@(Get-ChildItem -LiteralPath $CaptureDir -Filter '*.png' -File | Sort-Object Name | ForEach-Object {[ordered]@{file=$_.Name;bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
[ordered]@{gitRevision=$GitRevision;productFingerprint=$ProductFingerprint;width=720;height=1280;count=$CaptureEntries.Count;scope='Unity fixture renders; not Pixel captures';images=$CaptureEntries} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDir 'capture-manifest.json') -Encoding UTF8
$ApkName='vertical-slice.apk';$BuildMethod='Mofumachi.Editor.AndroidBuild.BuildDevelopmentApk';$PackageId='com.mofumachi.merge'
if($Qa){$ApkName='vertical-slice-qa.apk';$BuildMethod='Mofumachi.Editor.AndroidBuild.BuildQaApk';$PackageId='com.mofumachi.merge.qa'}
if($TownQa){$ApkName='town-life-qa.apk';$BuildMethod='Mofumachi.Editor.AndroidBuild.BuildTownQaApk';$PackageId='com.mofumachi.merge.qa.townlife'}
$ApkPath=Join-Path $OutputDir $ApkName;$BuildLog=Join-Path $OutputDir 'android-build.log'
Write-Host '[3/4] Building the QA APK from this same source revision...'
Invoke-Unity @('-batchmode','-nographics','-quit','-projectPath',$ProjectPath,'-buildTarget','Android','-executeMethod',$BuildMethod,'-mofumachiApkPath',$ApkPath,'-mofumachiRevision',$GitRevision,'-logFile',$BuildLog)
if(-not(Test-Path -LiteralPath $ApkPath) -or (Get-Item -LiteralPath $ApkPath).Length -le 0 -or -not(Select-String -LiteralPath $BuildLog -SimpleMatch 'MOFUMACHI_APK_SUCCESS' -Quiet)){throw "Fresh APK not produced: $BuildLog"}
Assert-ProductUnchanged $ProjectPath $GitRevision $ProductFingerprint
$ProjectSettings=Get-Content (Join-Path $ProjectPath 'ProjectSettings\ProjectSettings.asset') -Raw
$VersionCode=[regex]::Match($ProjectSettings,'(?m)^\s+AndroidBundleVersionCode: (\d+)\s*$').Groups[1].Value
if(-not $VersionCode){throw 'Cannot read versionCode for build identity.'}
$Hash=(Get-FileHash -LiteralPath $ApkPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host '[4/4] Recording APK identity and SHA256...'
[ordered]@{gitRevision=$GitRevision;revisionSource='git-metadata + clean product inputs';productFingerprint=$ProductFingerprint;localUnityConfigurationChanges=$ConfigurationChanges;nativeCaptureCount=$CaptureEntries.Count;unity='6000.6.4f1';appId=$PackageId;versionCode=[int]$VersionCode;apk=$ApkName;sha256=$Hash;bytes=(Get-Item -LiteralPath $ApkPath).Length;builtAtUtc=[DateTime]::UtcNow.ToString('o');qa=[bool]($Qa -or $TownQa);townQa=[bool]$TownQa} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDir 'build-info.json') -Encoding UTF8
Write-Host "APK: $ApkPath`nSHA256: $Hash"
if($TownQa) {
    Write-Host 'Building separate-save growth-check APK from the same tested source...'
    $GrowthApk=Join-Path $OutputDir 'town-growth-check.apk';$GrowthLog=Join-Path $OutputDir 'growth-android-build.log'
    Invoke-Unity @('-batchmode','-nographics','-quit','-projectPath',$ProjectPath,'-buildTarget','Android','-executeMethod','Mofumachi.Editor.AndroidBuild.BuildGrowthQaApk','-mofumachiRevision',$GitRevision,'-mofumachiApkPath',$GrowthApk,'-logFile',$GrowthLog)
    if(-not(Test-Path -LiteralPath $GrowthApk -PathType Leaf) -or -not(Select-String -LiteralPath $GrowthLog -SimpleMatch 'MOFUMACHI_APK_SUCCESS' -Quiet)){throw 'Fresh separate-save growth APK not produced.'}
    Assert-ProductUnchanged $ProjectPath $GitRevision $ProductFingerprint
    $GrowthHash=(Get-FileHash -LiteralPath $GrowthApk -Algorithm SHA256).Hash.ToLowerInvariant()
    [ordered]@{gitRevision=$GitRevision;productFingerprint=$ProductFingerprint;appId='com.mofumachi.merge.qa.growthcheck';apk='town-growth-check.apk';sha256=$GrowthHash;bytes=(Get-Item -LiteralPath $GrowthApk).Length;initial_state='normal initial game, separate Android app save';pixel_verified=$false;adopted=$false} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDir 'growth-build-info.json') -Encoding UTF8
    Write-Host "Growth APK: $GrowthApk`nSHA256: $GrowthHash"
    Write-VisualLockComparison $ProjectPath $OutputDir;Write-NativeReviewArchives $OutputDir
}
