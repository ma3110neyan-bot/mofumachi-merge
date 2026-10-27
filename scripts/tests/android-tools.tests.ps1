$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../build-android.ps1')
. (Join-Path $PSScriptRoot '../install-pixel3a.ps1')
function Expect-Rejection([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Expected rejection did not occur.'}}
$Temporary=Join-Path ([IO.Path]::GetTempPath()) ('mofumachi-script-test-'+[guid]::NewGuid())
New-Item -ItemType Directory -Path $Temporary | Out-Null
try {
    # Source identity must work on Windows without git on PATH.
    $FixtureRepo=Join-Path $Temporary 'repo';$FixtureGit=Join-Path $FixtureRepo '.git'
    New-Item -ItemType Directory -Path (Join-Path $FixtureGit 'refs/heads/feat') -Force | Out-Null
    $ShaA='a'*40;$ShaB='b'*40
    Set-Content (Join-Path $FixtureGit 'HEAD') 'ref: refs/heads/feat/town'
    Set-Content (Join-Path $FixtureGit 'refs/heads/feat/town') $ShaA
    if((Read-SourceRevision $FixtureRepo) -ne $ShaA){throw 'Loose branch SHA failed.'}
    Remove-Item (Join-Path $FixtureGit 'refs/heads/feat/town')
    Set-Content (Join-Path $FixtureGit 'packed-refs') "$ShaB refs/heads/feat/town"
    if((Read-SourceRevision $FixtureRepo) -ne $ShaB){throw 'Packed branch SHA failed.'}
    Expect-Rejection {Assert-SourceUnchanged $FixtureRepo $ShaA}
    Assert-SourceUnchanged $FixtureRepo $ShaB
    Set-Content (Join-Path $FixtureGit 'HEAD') $ShaA
    if((Read-SourceRevision $FixtureRepo) -ne $ShaA){throw 'Detached SHA failed.'}
    Set-Content (Join-Path $FixtureGit 'HEAD') 'invalid'
    Expect-Rejection {Read-SourceRevision $FixtureRepo}
    $Worktree=Join-Path $Temporary 'worktree';New-Item -ItemType Directory -Path $Worktree | Out-Null
    $WorkGit=Join-Path $FixtureGit 'worktrees/test';New-Item -ItemType Directory -Path $WorkGit -Force | Out-Null
    Set-Content (Join-Path $Worktree '.git') "gitdir: $WorkGit"
    Set-Content (Join-Path $WorkGit 'commondir') '../..'
    Set-Content (Join-Path $WorkGit 'HEAD') 'ref: refs/heads/feat/town'
    if((Read-SourceRevision $Worktree) -ne $ShaB){throw 'Worktree common packed refs failed.'}
    Write-Host 'Source SHA checks passed (loose, packed, detached, worktree, changed revision rejected).'
    $RealGit=Find-SourceGit;$CleanRepo=Join-Path $Temporary 'clean source'
    New-Item -ItemType Directory -Path (Join-Path $CleanRepo 'Assets/Mofumachi') -Force | Out-Null
    & $RealGit -C $CleanRepo init --quiet
    if($LASTEXITCODE -ne 0){throw 'Could not create the isolated source fixture.'}
    $ProductFile=Join-Path $CleanRepo 'Assets/Mofumachi/Fixture.cs';Set-Content -LiteralPath $ProductFile 'fixture'
    & $RealGit -C $CleanRepo add -- Assets
    & $RealGit -C $CleanRepo -c user.name=ScriptFixture -c user.email=fixture@example.invalid commit --quiet -m fixture
    if($LASTEXITCODE -ne 0){throw 'Could not commit the isolated source fixture.'}
    $Revision=Read-SourceRevision $CleanRepo;Assert-CleanProductSource $CleanRepo $RealGit;$Fingerprint=Get-ProductFingerprint $CleanRepo
    Assert-ProductUnchanged $CleanRepo $Revision $Fingerprint
    # Real hidden filesystem entries: Test-Path sees them but Get-Item and
    # recursive enumeration need -Force. Windows .meta files can carry this
    # attribute; dot names reproduce the same provider behavior on Linux.
    $HiddenFile=Join-Path $CleanRepo 'Assets/Mofumachi/.hidden-source.meta'
    [IO.File]::WriteAllText($HiddenFile,'hidden fixture')
    [IO.File]::SetAttributes($HiddenFile,([IO.File]::GetAttributes($HiddenFile) -bor [IO.FileAttributes]::Hidden))
    $HiddenLeafFingerprint={
        & {
            function Source-InputPaths {return @('Assets/Mofumachi/.hidden-source.meta')}
            Get-ProductFingerprint $CleanRepo
        }
    }
    $LeafBefore=& $HiddenLeafFingerprint
    $HiddenBefore=Get-ProductFingerprint $CleanRepo
    [IO.File]::WriteAllText($HiddenFile,'changed hidden fixture')
    if((& $HiddenLeafFingerprint) -eq $LeafBefore){throw 'Explicit hidden input was omitted from the fingerprint.'}
    if((Get-ProductFingerprint $CleanRepo) -eq $HiddenBefore){throw 'Recursive hidden input was omitted from the fingerprint.'}
    $HiddenDirectory=Join-Path $CleanRepo 'Assets/Mofumachi/.hidden-folder'
    New-Item -ItemType Directory -Path $HiddenDirectory | Out-Null
    [IO.File]::SetAttributes($HiddenDirectory,([IO.File]::GetAttributes($HiddenDirectory) -bor [IO.FileAttributes]::Hidden))
    $Nested=Join-Path $HiddenDirectory 'Source.cs';[IO.File]::WriteAllText($Nested,'nested fixture')
    $NestedBefore=Get-ProductFingerprint $CleanRepo
    [IO.File]::WriteAllText($Nested,'changed nested fixture')
    if((Get-ProductFingerprint $CleanRepo) -eq $NestedBefore){throw 'Files inside a hidden directory were omitted from the fingerprint.'}
    Remove-Item -LiteralPath $HiddenFile -Force;Remove-Item -LiteralPath $HiddenDirectory -Recurse -Force
    Assert-ProductUnchanged $CleanRepo $Revision $Fingerprint
    Write-Host 'Hidden source inputs are readable and fingerprinted (explicit file, recursive file and directory).'
    Set-Content -LiteralPath $ProductFile 'edited during acceptance'
    Expect-Rejection {Assert-CleanProductSource $CleanRepo $RealGit};Expect-Rejection {Assert-ProductUnchanged $CleanRepo $Revision $Fingerprint}
    & $RealGit -C $CleanRepo restore --worktree -- Assets
    $Untracked=Join-Path $CleanRepo 'Assets/Mofumachi/New.cs';Set-Content -LiteralPath $Untracked 'untracked'
    Expect-Rejection {Assert-CleanProductSource $CleanRepo $RealGit};Expect-Rejection {Assert-ProductUnchanged $CleanRepo $Revision $Fingerprint}
    Remove-Item -LiteralPath $Untracked
    New-Item -ItemType Directory -Path (Join-Path $CleanRepo 'Assets/Settings') -Force | Out-Null
    Set-Content (Join-Path $CleanRepo 'Assets/Settings/Generated.asset') 'local Unity cache'
    Assert-CleanProductSource $CleanRepo $RealGit;Assert-ProductUnchanged $CleanRepo $Revision $Fingerprint
    Remove-Item -LiteralPath $ProductFile;Expect-Rejection {Assert-ProductUnchanged $CleanRepo $Revision $Fingerprint}
    Write-Host 'Clean product and same-run fingerprint gates passed (isolated real Git fixture).'
    $CaptureDir=Join-Path $Temporary 'screenshots';New-Item -ItemType Directory -Path $CaptureDir | Out-Null
    Expect-Rejection {Assert-NativeCaptures $CaptureDir}
    # Header-only fixtures exercise metadata gates, not Unity rendering or CRC.
    [byte[]]$Header=137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,2,208,0,0,5,0,0
    $Names=@('15-wide-park-left','15-wide-park-right','16-residents-passing-00','17-finger-follow-00','17-finger-released','18-continuous-walk-west','18-continuous-walk-central','18-continuous-walk-east','19-book-new-in-town','19-book-discovery-first-page','20-quest-cabin','20-quest-door-opening','20-quest-inside','21-shop-cabin','21-shop-door-opening','21-shop-inside','22-title-candidates','22-merge-candidates','23-title-letter','23-title-garden','23-merge-picnic','23-merge-ribbon','23-merge-pop-promenade','24-home-unobstructed','24-menu-open','24-menu-return','25-character-idle','25-character-step-a','25-character-step-b','25-character-greeting','25-character-behind-fountain','25-character-in-front-fountain','04-town-lv1-North','04-town-lv1-South','04-town-lv6-North','04-town-lv6-South','09-connected-path-north','09-connected-path-south','26-cardinal-North-join','26-cardinal-North-arrival','26-cardinal-South-join','26-cardinal-South-arrival','27-merge-finale-live-purpose')
    $NewBaseCaptures=@('20-quest-arrived-door-prompt','21-shop-arrived-door-prompt','21b-cafe-cabin','21b-cafe-walking','21b-cafe-arrived-door-prompt','21b-cafe-door-opening','21b-cafe-inside','28-meet-friend-walking','28-meet-friend-arrived','29-merge-drag-lift','29-merge-drag-match','29-merge-landed')
    $Names+=$NewBaseCaptures
    $Names+=@(1..47 | ForEach-Object {'fixture-'+$_})
    foreach($Name in $Names){[IO.File]::WriteAllBytes((Join-Path $CaptureDir ($Name+'.png')),$Header)}
    Assert-NativeCaptures $CaptureDir
    foreach($RequiredBaseCapture in $NewBaseCaptures) {
        $CaptureFile=Join-Path $CaptureDir ($RequiredBaseCapture+'.png');Remove-Item -LiteralPath $CaptureFile
        Expect-Rejection {Assert-NativeCaptures $CaptureDir}
        [IO.File]::WriteAllBytes($CaptureFile,$Header)
    }
    Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireBoundary}
    $BoundaryFiles=@(foreach($View in @('west-upper','east-upper','west-lower','east-lower','north-left','north-right','south-left','south-right','south-entry','west-join','east-join')) {
        foreach($Phase in @('before-reconstructed','after')){"30-boundary-$View-$Phase.png"}
    })
    foreach($Name in $BoundaryFiles){[IO.File]::WriteAllBytes((Join-Path $CaptureDir $Name),$Header)}
    Set-Content -LiteralPath (Join-Path $CaptureDir 'street-boundary-before-after.html') -Value '<!doctype html><title>Test fixture, not a native render</title>'
    $BoundaryMeta=Join-Path $CaptureDir 'street-boundary-comparison.json'
    $Boundary=@{kind='same-state-native-renderer-comparison';pairs=11;after='product code';pixel_verified=$false;adopted=$false}
    $Boundary | ConvertTo-Json | Set-Content -LiteralPath $BoundaryMeta
    Assert-NativeCaptures $CaptureDir -RequireBoundary
    foreach($Name in $BoundaryFiles){$Path=Join-Path $CaptureDir $Name;Remove-Item -LiteralPath $Path;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireBoundary};[IO.File]::WriteAllBytes($Path,$Header)}
    $Boundary.pairs=10;$Boundary | ConvertTo-Json | Set-Content -LiteralPath $BoundaryMeta;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireBoundary}
    $Boundary.pairs=11;$Boundary.pixel_verified=$true;$Boundary | ConvertTo-Json | Set-Content -LiteralPath $BoundaryMeta;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireBoundary}
    $Boundary.pixel_verified=$false;$Boundary | ConvertTo-Json | Set-Content -LiteralPath $BoundaryMeta
    foreach($Name in @('street-boundary-before-after.html','street-boundary-comparison.json')){$Path=Join-Path $CaptureDir $Name;$Saved=[IO.File]::ReadAllBytes($Path);Remove-Item -LiteralPath $Path;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireBoundary};[IO.File]::WriteAllBytes($Path,$Saved)}
    Write-Host 'Boundary acceptance requires all22 comparison frames/documents; incomplete or falsely adopted fixture refused. No native/device execution.'
    $FieldFiles=@('31-field-fountain-front.png','31-field-fountain-behind.png','31-field-east-corner-out.png','31-field-east-corner-return.png')+@(foreach($Area in @('East','West','North','South','Central')){foreach($Phase in @('edge','return')){"31-field-$Area-$Phase.png"}})
    Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireField}
    foreach($Name in $FieldFiles){[IO.File]::WriteAllBytes((Join-Path $CaptureDir $Name),$Header)}
    $FieldMeta=Join-Path $CaptureDir 'field-stability.json'
    $Field=@{kind='native-walking-and-depth-fixture';edge_return_pairs=5;east_corner_return=$true;fountain_front_and_behind=$true;teleport_rescue=$false;pixel_verified=$false;adopted=$false}
    $Field | ConvertTo-Json | Set-Content -LiteralPath $FieldMeta
    Assert-NativeCaptures $CaptureDir -RequireBoundary -RequireField
    foreach($Name in $FieldFiles){$Path=Join-Path $CaptureDir $Name;Remove-Item -LiteralPath $Path;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireField};[IO.File]::WriteAllBytes($Path,$Header)}
    $Field.teleport_rescue=$true;$Field | ConvertTo-Json | Set-Content -LiteralPath $FieldMeta;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireField}
    $Field.teleport_rescue=$false;$Field.pixel_verified=$true;$Field | ConvertTo-Json | Set-Content -LiteralPath $FieldMeta;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireField}
    $Field.pixel_verified=$false;$Field | ConvertTo-Json | Set-Content -LiteralPath $FieldMeta
    Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireGrowth}
    $GrowthFiles=@(foreach($Level in 0..5){foreach($District in @('Central','West','East','North','South')){"32-growth-$Level-to-$($Level+1)-$District.png";"32-growth-$($Level+1)-$District-reloaded.png"}})
    foreach($Name in $GrowthFiles){[IO.File]::WriteAllBytes((Join-Path $CaptureDir $Name),$Header)}
    $GrowthMeta=Join-Path $CaptureDir 'town-growth-progression.json'
    $GrowthCoins=@(30,70,110,180,250,330)
    $GrowthQuests=@('tea-01','bread-01','flower-01','tea-02','flower-02','bread-02')
    $GrowthNeeds=@(@('tea:2:1'),@('bread:2:1'),@('flower:2:1'),@('tea:3:1','bread:2:1'),@('flower:3:1','tea:2:1'),@('bread:3:1','flower:2:1'))
    $Growth=@{kind='isolated-real-delivery-growth-and-reload';initial_level=0;initial_coins=0;direct_level_assignment=$false;pixel_verified=$false;adopted=$false;transitions=@(foreach($Level in 0..5){@{from=$Level;to=($Level+1);coins=$GrowthCoins[$Level];quest_id=$GrowthQuests[$Level];requirements=$GrowthNeeds[$Level];completed_quests=($Level+1);districts_checked=5;reload_matched=$true;reward_replay_rejected=$true;districts=@(foreach($District in @('Central','West','East','North','South')){@{id=$District;changed=(-not(($Level -le 1 -and $District -in @('North','South')) -or ($Level -in @(3,4) -and $District -eq 'Central')));reload_matched=$true}})}})}
    function SaveGrowth {$Growth | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $GrowthMeta -Encoding UTF8}
    SaveGrowth
    Assert-NativeCaptures $CaptureDir -RequireBoundary -RequireField -RequireGrowth
    foreach($Name in $GrowthFiles){$Path=Join-Path $CaptureDir $Name;Remove-Item -LiteralPath $Path;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireGrowth};[IO.File]::WriteAllBytes($Path,$Header)}
    $AllTransitions=$Growth.transitions
    foreach($Level in 0..5){$Growth.transitions=@($AllTransitions | Where-Object {$_.from -ne $Level});SaveGrowth;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireGrowth}}
    $Growth.transitions=$AllTransitions
    $Growth.transitions[3].coins=999;SaveGrowth;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireGrowth};$Growth.transitions[3].coins=180
    $Growth.transitions[4].reload_matched=$false;SaveGrowth;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireGrowth};$Growth.transitions[4].reload_matched=$true
    $Growth.transitions[3].districts[0].changed=$true;SaveGrowth;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireGrowth};$Growth.transitions[3].districts[0].changed=$false
    $Growth.transitions[4].districts[3].reload_matched=$false;SaveGrowth;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireGrowth};$Growth.transitions[4].districts[3].reload_matched=$true
    $Growth.transitions[3].requirements=@('tea:1:1');SaveGrowth;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireGrowth};$Growth.transitions[3].requirements=$GrowthNeeds[3]
    $Growth.transitions[4].reward_replay_rejected=$false;SaveGrowth;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireGrowth};$Growth.transitions[4].reward_replay_rejected=$true
    $Growth.direct_level_assignment=$true;SaveGrowth;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireGrowth};$Growth.direct_level_assignment=$false
    $Growth.pixel_verified=$true;SaveGrowth;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireGrowth};$Growth.pixel_verified=$false;SaveGrowth
    Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountain}
    $FountainFiles=@(foreach($View in @('overview','front','behind')){foreach($Phase in @('before','after')){"33-fountain-$View-$Phase.png"}})+@(0..3 | ForEach-Object {'33-fountain-phase-'+$_.ToString('00')+'.png'})
    foreach($Name in $FountainFiles){[IO.File]::WriteAllBytes((Join-Path $CaptureDir $Name),$Header)}
    $FountainMeta=Join-Path $CaptureDir 'fountain-water-comparison.json';$FountainHtml=Join-Path $CaptureDir 'fountain-water-before-after.html'
    $Fountain=@{kind='same-state-native-additive-water-comparison';before='new layer OFF; original fountain and original water retained';after='product code';pairs=@(foreach($View in @('overview','front','behind')){@{view=$View;changed_pixels=250;outside_fountain_pixels=0;state_unchanged=$true}});phases=@(.1,.4,.7,1.0);added_vertices=784;max_update_hz=30;pixel_verified=$false;adopted=$false}
    function SaveFountain {$Fountain | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $FountainMeta}
    SaveFountain;Set-Content -LiteralPath $FountainHtml -Value '<!doctype html><title>Metadata fixture, not native pixels</title>'
    Assert-NativeCaptures $CaptureDir -RequireBoundary -RequireField -RequireGrowth -RequireFountain
    foreach($Name in $FountainFiles){$Path=Join-Path $CaptureDir $Name;Remove-Item -LiteralPath $Path;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountain};[IO.File]::WriteAllBytes($Path,$Header)}
    $Fountain.pairs[0].outside_fountain_pixels=1;SaveFountain;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountain};$Fountain.pairs[0].outside_fountain_pixels=0
    $Fountain.pairs[1].changed_pixels=0;SaveFountain;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountain};$Fountain.pairs[1].changed_pixels=250
    $Fountain.pairs[2].state_unchanged=$false;SaveFountain;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountain};$Fountain.pairs[2].state_unchanged=$true
    $Fountain.added_vertices=785;SaveFountain;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountain};$Fountain.added_vertices=784
    $Fountain.phases=@(.1,.1,.1,.1);SaveFountain;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountain};$Fountain.phases=@(.1,.4,.7,1.0)
    $Fountain.pixel_verified=$true;SaveFountain;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountain};$Fountain.pixel_verified=$false;SaveFountain
    Remove-Item -LiteralPath $FountainHtml;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountain};Set-Content -LiteralPath $FountainHtml -Value '<!doctype html><title>Metadata fixture, not native pixels</title>'
    Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountain -RequireFountainShow}
    $ShowFiles=@('34-fountain-overview-prelude.png','34-fountain-overview-show.png','34-fountain-front-show.png','34-fountain-behind-show.png','34-fountain-overview-recovered.png','34-fountain-overview-show-off.png','34-fountain-front-show-off.png','34-fountain-behind-show-off.png')
    foreach($Name in $ShowFiles){[IO.File]::WriteAllBytes((Join-Path $CaptureDir $Name),$Header)}
    $Baseline=Get-Content -LiteralPath (Join-Path $PSScriptRoot '../../References/VisualLock/fountain-final-current-before-2026-10-27.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $BeforeDir=Join-Path $CaptureDir 'fountain-before';New-Item -ItemType Directory -Path $BeforeDir | Out-Null
    foreach($Image in $Baseline.images){Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('../../'+$Image.path)) -Destination (Join-Path $BeforeDir $Image.file)}
    $Show=@{kind='prior-native-before-normal-and-timed-show';display_scale=1.14;cycle_seconds=45;release_cycle_seconds=180;prelude_seconds=2;show_seconds=6;recovery_seconds=1;show_elapsed=41;jet_evidence='same-instant-added-layer-off-on-above-normal-uv084';show_pairs=@(foreach($View in @('overview','front','behind')){@{view=$View;changed_pixels=400;show_jet_pixels=80;outside_fountain_pixels=0;clock_unchanged=$true;original_water_phase_unchanged=$true;state_unchanged=$true}});before=$Baseline;pixel_verified=$false;adopted=$false}
    $ShowMeta=Join-Path $CaptureDir 'fountain-show-comparison.json';$ShowHtml=Join-Path $CaptureDir 'fountain-show-before-after.html'
    function SaveShow {$Show | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath $ShowMeta}
    SaveShow;Set-Content -LiteralPath $ShowHtml -Value '<!doctype html><title>Synthetic metadata fixture, not native</title>'
    foreach($Name in @('35-fountain-live-normal.png','35-fountain-live-show.png','35-fountain-live-recovered.png')){[IO.File]::WriteAllBytes((Join-Path $CaptureDir $Name),$Header)}
    $LiveDir=Join-Path $CaptureDir 'fountain-live';New-Item -ItemType Directory -Path $LiveDir | Out-Null
    $LiveFrames=@(for($n=0;$n -le 120;$n++){
        $At=$n*.5;$Clock=$At%45;$LiveStage=if($Clock -lt 36){0}elseif($Clock -lt 38){1}elseif($Clock -lt 44){2}else{3}
        $File=('35-fountain-live-crop-{0:D3}.png' -f $n);$Path=Join-Path $LiveDir $File;$Bytes=$Header+[byte]$n;[IO.File]::WriteAllBytes($Path,$Bytes)
        @{file=$File;bytes=$Bytes.Length;width=720;height=1280;sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant();realtime_seconds=$At;clock_seconds=$Clock;phase=$At%10;stage=$LiveStage}
    })
    @{kind='native-realtime-update-sequence';town_level=0;seconds=60;first_show_seconds=39;cycle_seconds=45;sample_period_seconds=.25;manual_advance=$false;pixel_verified=$false;adopted=$false;state_unchanged=$true;source_width=720;source_height=1280;crop_x=0;crop_y=0;crop_width=720;crop_height=1280;normal_mesh_changes=90;show_mesh_changes=10;frames=$LiveFrames} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $LiveDir 'fountain-live.json') -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $LiveDir 'fountain-live.html') '<!doctype html><title>Metadata fixture</title>'
    Assert-NativeCaptures $CaptureDir -RequireFountain -RequireFountainShow
    $LiveMetadata=Join-Path $LiveDir 'fountain-live.json';$LiveData=Get-Content -LiteralPath $LiveMetadata -Raw -Encoding UTF8 | ConvertFrom-Json
    $LiveData.town_level=6;$LiveData | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $LiveMetadata -Encoding UTF8
    Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountainShow}
    $LiveData.town_level=0;$LiveData | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $LiveMetadata -Encoding UTF8
    foreach($Name in $ShowFiles){$Path=Join-Path $CaptureDir $Name;Remove-Item -LiteralPath $Path;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountainShow};[IO.File]::WriteAllBytes($Path,$Header)}
    $Show.show_pairs[0].show_jet_pixels=0;SaveShow;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountainShow};$Show.show_pairs[0].show_jet_pixels=80
    $Show.show_pairs[0].outside_fountain_pixels=1;SaveShow;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountainShow};$Show.show_pairs[0].outside_fountain_pixels=0
    $Show.show_seconds=60;SaveShow;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountainShow};$Show.show_seconds=6
    $Show.show_pairs[1].changed_pixels=1;SaveShow;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountainShow};$Show.show_pairs[1].changed_pixels=400
    $Show.before.source_revision='0'*40;SaveShow;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountainShow};$Show.before.source_revision='98596daf798bcea54d39f6a41b58a565070e1370'
    $Show.adopted=$true;SaveShow;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountainShow};$Show.adopted=$false;SaveShow
    $BeforeFile=Join-Path $BeforeDir $Baseline.images[0].file;$BeforeBytes=[IO.File]::ReadAllBytes($BeforeFile)
    [IO.File]::WriteAllBytes($BeforeFile,$Header);Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireFountainShow};[IO.File]::WriteAllBytes($BeforeFile,$BeforeBytes)
    Write-NativeReviewArchives $Temporary
    $ZipContents=@(Get-ChildItem -LiteralPath $Temporary -Filter 'five-plaza-review-*.zip' | ForEach-Object {
        $Zip=[IO.Compression.ZipFile]::OpenRead($_.FullName);try {$Zip.Entries | ForEach-Object {$_.FullName}}finally {$Zip.Dispose()}
    })
    if('fountain-live.json' -notin $ZipContents -or '35-fountain-live-crop-120.png' -notin $ZipContents -or 'fountain-show-comparison.json' -notin $ZipContents -or $Baseline.images[0].file -notin $ZipContents){throw 'ZIP lost timed show or actual prior Before.'}
    if('field-stability.json' -notin $ZipContents){throw 'Review ZIPs lost the required field-stability fixture metadata.'}
    if('town-growth-progression.json' -notin $ZipContents){throw 'Review ZIPs lost real delivery/growth/reload evidence.'}
    if('fountain-water-comparison.json' -notin $ZipContents -or 'fountain-water-before-after.html' -notin $ZipContents){throw 'Review ZIPs lost fountain comparison evidence.'}
    Remove-Item -LiteralPath $FieldMeta;Expect-Rejection {Assert-NativeCaptures $CaptureDir -RequireField}
    [IO.File]::WriteAllBytes((Join-Path $CaptureDir '17-finger-released.png'),[byte[]](0,1,2));Expect-Rejection {Assert-NativeCaptures $CaptureDir}
    [IO.File]::WriteAllBytes((Join-Path $CaptureDir '17-finger-released.png'),$Header)
    $WrongHeader=$Header.Clone();$WrongHeader[19]=0;[IO.File]::WriteAllBytes((Join-Path $CaptureDir '17-finger-released.png'),$WrongHeader);Expect-Rejection {Assert-NativeCaptures $CaptureDir}
    Remove-Item (Join-Path $CaptureDir '19-book-new-in-town.png');Expect-Rejection {Assert-NativeCaptures $CaptureDir}
    # Exercise the real wrapper before the mocked installer commands below.
    # Windows PowerShell 5.1 turns redirected native stderr into ErrorRecords;
    # newer pwsh does not, so reproduce that legacy stream policy explicitly.
    function Legacy-NativeDiagnostic {
        $global:LASTEXITCODE=0
        Write-Error 'args: [-p, com.mofumachi.merge.qa, -c, android.intent.category.LAUNCHER, 1]' -ErrorId NativeCommandError
        'Events injected: 1'
    }
    $LegacyOutput=Invoke-CheckedNative 'Legacy-NativeDiagnostic' @()
    if($LegacyOutput -notmatch 'args:' -or $LegacyOutput -notmatch 'Events injected: 1'){throw 'Successful stderr diagnostic was lost.'}
    if($ErrorActionPreference -ne 'Stop'){throw 'Native invocation changed the caller error policy.'}
    $NativeScript=Join-Path $Temporary 'native diagnostic.ps1'
    @'
param([int]$Code=0)
[Console]::Error.WriteLine('args: [-p, com.mofumachi.merge.qa, -c, android.intent.category.LAUNCHER, 1]')
[Console]::Out.WriteLine('Events injected: 1')
exit $Code
'@ | Set-Content -LiteralPath $NativeScript -Encoding UTF8
    $ShellExecutable=(Get-Process -Id $PID).Path
    $NativeOutput=Invoke-CheckedNative $ShellExecutable @('-NoProfile','-NonInteractive','-File',$NativeScript,'-Code','0')
    if($NativeOutput -notmatch 'args:' -or $NativeOutput -notmatch 'Events injected: 1'){throw 'Native stdout/stderr capture failed.'}
    Expect-Rejection {Invoke-CheckedNative (Join-Path $Temporary 'missing adb.exe') @()}
    $ExitRejected=$false
    try {Invoke-CheckedNative $ShellExecutable @('-NoProfile','-NonInteractive','-File',$NativeScript,'-Code','7') | Out-Null}
    catch {$ExitRejected=$_.Exception.Message -match 'Command failed \(7\)' -and $_.Exception.Message -match 'args:'}
    if(-not $ExitRejected){throw 'A real nonzero native exit was not reported with its diagnostic.'}
    if($ErrorActionPreference -ne 'Stop'){throw 'Failed native invocation changed the caller error policy.'}
    Write-Host 'Native stdout/stderr and exit-code checks passed (legacy stderr policy simulated).'
    $Xml=Join-Path $Temporary 'results.xml'
    Expect-Rejection {Assert-TestRun $Xml 2 @('Required')}
    function Write-Xml([string]$Result='Passed',[int]$Total=2,[int]$Passed=2,[int]$Failed=0,[int]$Skipped=0,[string]$Suite='Required',[string]$Case='Passed'){
        "<test-run result='$Result' total='$Total' passed='$Passed' failed='$Failed' skipped='$Skipped'><test-suite name='$Suite' result='Passed'><test-case result='Passed'/><test-case result='$Case'/></test-suite></test-run>" | Set-Content $Xml
    }
    Write-Xml;Assert-TestRun $Xml 2 @('Required')
    Write-Xml -Result Failed -Passed 1 -Failed 1;Expect-Rejection {Assert-TestRun $Xml 2 @('Required')}
    Write-Xml -Passed 1 -Skipped 1;Expect-Rejection {Assert-TestRun $Xml 2 @('Required')}
    Write-Xml -Total 3 -Passed 3;Expect-Rejection {Assert-TestRun $Xml 2 @('Required')}
    Write-Xml -Suite Other;Expect-Rejection {Assert-TestRun $Xml 2 @('Required')}
    Write-Xml -Case Inconclusive;Expect-Rejection {Assert-TestRun $Xml 2 @('Required')}
    Write-Xml;Expect-Rejection {Assert-TestRun $Xml 67 @('Required')}
    # ChapterBuildGateRejectsMissingSuites: read the actual production call lists.
    $BuildSource=Get-Content (Join-Path $PSScriptRoot '../build-android.ps1') -Raw
    function Read-GateSuites([string]$XmlVariable,[int]$Minimum) {
        $Pattern='Assert-TestRun \$'+$XmlVariable+' '+$Minimum+' @\(([^)]+)\)'
        $Match=[regex]::Match($BuildSource,$Pattern)
        if(-not $Match.Success){throw 'Actual gate call was not found.'}
        return @([regex]::Matches($Match.Groups[1].Value,"'([^']+)'") | ForEach-Object {$_.Groups[1].Value})
    }
    function Write-SuiteXml([int]$Total,[string[]]$Suites,[string]$Case='Passed',[int]$Declared=0) {
        if($Declared -eq 0){$Declared=$Total}
        $SuiteCases=0
        $Nodes=foreach($Name in $Suites) {
            $Count=if($Name -eq 'CharacterSelectionTests'){4}elseif($Name -eq 'DailyWorldTests'){13}elseif($Name -eq 'MajorPolishTests'){6}elseif($Name -eq 'SeamlessFieldTests'){8}elseif($Name -eq 'EastWestHomeTests'){4}elseif($Name -eq 'ResidentPresenceTests'){5}elseif($Name -eq 'FivePlazaTests'){5}elseif($Name -eq 'BaseProductLoopTests'){5}elseif($Name -eq 'VisualLockPresentationTests'){2}elseif($Name -eq 'CentralMasterReviewTests'){2}elseif($Name -eq 'WestMasterReviewTests'){3}elseif($Name -eq 'EastMasterReviewTests'){3}elseif($Name -eq 'NorthMasterReviewTests'){3}elseif($Name -eq 'SouthMasterReviewTests'){3}else{1}
            $SuiteCases+=$Count;"<test-suite name='$Name' result='Passed'>"+('<test-case result="Passed"/>'*$Count)+"</test-suite>"
        }
        $Nodes=$Nodes -join ''
        $Extra=('<test-case result="Passed"/>'*($Total-$SuiteCases-1))+"<test-case result='$Case'/>"
        "<test-run result='Passed' total='$Declared' passed='$Declared' failed='0' skipped='0'>$Nodes$Extra</test-run>" | Set-Content $Xml
    }
    $EditSuites=Read-GateSuites 'EditXml' 144;$PlaySuites=Read-GateSuites 'PlayXml' 226
    foreach($Gate in @(@{Minimum=144;Suites=$EditSuites;New=@('ChapterProgressTests','TownFrameClockTests','TownPlaceCatalogTests','TownViewportTests','TownRegionCatalogTests','TownRegionJourneyTests','TownRegionContentTests')},@{Minimum=226;Suites=$PlaySuites;New=@('StoryTypographyTests','TownMotionPresentationTests','ChapterFlowTests','MergePolishTests','MergeProgressionUiTests','MergeGuidanceTests','ProductionFrameUiTests','TownViewportPresentationTests','TownHabitTests','TownRegionFlowTests','TownRegionLifeTests','QualityPassTravelTests','QualityPassStoryGrowthTests','QualityPassMergeTests','ExperienceWalkingTests','ExperienceMergeTests','ExperienceMusicTests','ExperienceGrowthTests','ExperienceCaptureTests','DailyWorldTests','MajorPolishTests','SeamlessFieldTests','EastWestHomeTests','ResidentPresenceTests','FivePlazaTests','BaseProductLoopTests','VisualLockPresentationTests','CentralMasterReviewTests','WestMasterReviewTests','EastMasterReviewTests','NorthMasterReviewTests','SouthMasterReviewTests','FiveAreaIntegrationReviewTests')})) {
        $Old=@($Gate.Suites | Where-Object {$_ -notin $Gate.New})
        Write-SuiteXml $Gate.Minimum $Old
        Expect-Rejection {Assert-TestRun $Xml $Gate.Minimum $Gate.Suites}
        foreach($Missing in $Gate.New) {
            if($Missing -notin $Gate.Suites){throw "Missing chapter suite in production gate: $Missing"}
            Write-SuiteXml $Gate.Minimum @($Gate.Suites | Where-Object {$_ -ne $Missing})
            Expect-Rejection {Assert-TestRun $Xml $Gate.Minimum $Gate.Suites}
        }
        Write-SuiteXml $Gate.Minimum $Old
        $EmptySuites=($Gate.New | ForEach-Object {"<test-suite name='$_' result='Passed'/>"}) -join ''
        (Get-Content $Xml -Raw).Replace('</test-run>',"$EmptySuites</test-run>") | Set-Content $Xml
        Expect-Rejection {Assert-TestRun $Xml $Gate.Minimum $Gate.Suites}
        Write-SuiteXml $Gate.Minimum $Gate.Suites;Assert-TestRun $Xml $Gate.Minimum $Gate.Suites
        foreach($BadCase in @('Skipped','Failed','Inconclusive')){Write-SuiteXml $Gate.Minimum $Gate.Suites $BadCase;Expect-Rejection {Assert-TestRun $Xml $Gate.Minimum $Gate.Suites}}
        Write-SuiteXml $Gate.Minimum $Gate.Suites -Declared ($Gate.Minimum+1);Expect-Rejection {Assert-TestRun $Xml $Gate.Minimum $Gate.Suites}
    }
    foreach($Name in @('CharacterSelectionTests','DailyWorldTests','MajorPolishTests','SeamlessFieldTests','EastWestHomeTests','ResidentPresenceTests','FivePlazaTests','BaseProductLoopTests','VisualLockPresentationTests','CentralMasterReviewTests','WestMasterReviewTests','EastMasterReviewTests','NorthMasterReviewTests','SouthMasterReviewTests','FiveAreaIntegrationReviewTests')) {
        Write-SuiteXml 223 $PlaySuites
        $Data=Get-Content -LiteralPath $Xml -Raw
        $SuiteStart="<test-suite name='$Name' result='Passed'>"
        $Data=$Data.Replace($SuiteStart+'<test-case result="Passed"/>','<test-case result="Passed"/>'+$SuiteStart)
        $Data | Set-Content -LiteralPath $Xml
        Expect-Rejection {Assert-TestRun $Xml 223 $PlaySuites}
    }
    Write-Host 'Chapter build gates passed (synthetic result XML, Unity tests not executed).'
    Expect-Rejection {Select-AuthorizedDevice 'List of devices attached' ''}
    Expect-Rejection {Select-AuthorizedDevice "Pixel`tunauthorized" ''}
    Expect-Rejection {Select-AuthorizedDevice "Pixel`tdevice`nOther`tdevice" ''}
    if((Select-AuthorizedDevice "Pixel`tdevice`nOther`tdevice" 'Pixel') -ne 'Pixel'){throw 'Wrong serial'}
    $Apk=Join-Path $Temporary 'test app.apk';$Adb=Join-Path $Temporary 'adb.exe';$Aapt=Join-Path $Temporary 'aapt2.exe'
    foreach($Path in @($Apk,$Adb,$Aapt)){Set-Content -LiteralPath $Path 'fixture'}
    $script:Calls=@();$script:Wrong=$false;$script:FailInstall=$false;$script:FixtureId='com.mofumachi.merge';$script:FixtureSerial='Pixel';$script:BadModel=$false;$script:FailUninstall=$false
    function Invoke-CheckedNative([string]$Executable,[string[]]$NativeArguments){
        $script:Calls+=,@($NativeArguments)
        if($Executable -eq $Aapt){$Id=$script:FixtureId;if($script:Wrong){$Id='wrong.app'};return "package: name='$Id' versionCode='1'`nnative-code: 'arm64-v8a'"}
        if($NativeArguments[0] -eq 'devices'){return "List of devices attached`n$($script:FixtureSerial)`tdevice"}
        if($NativeArguments[0] -ne '-s' -or $NativeArguments[1] -ne $script:FixtureSerial){throw 'ADB device command must specify exact serial.'}
        if($NativeArguments -contains 'ro.build.version.sdk'){return '31'}
        if($NativeArguments -contains 'ro.product.cpu.abilist'){return 'arm64-v8a,armeabi-v7a'}
        if($NativeArguments -contains 'ro.product.model'){if($script:BadModel){return 'Other phone'};return 'Pixel 3a'}
        if($NativeArguments -contains 'packages'){return "package:$($script:FixtureId)"}
        if($NativeArguments -contains 'uninstall'){if($script:FailUninstall){return 'Failure [DELETE_FAILED_INTERNAL_ERROR]'};return 'Success'}
        if($NativeArguments -contains 'clear'){return 'Success'}
        if($NativeArguments -contains 'install'){if($script:FailInstall){return 'Failure [INSTALL_FAILED_UPDATE_INCOMPATIBLE]'};if($NativeArguments[-1] -ne $Apk){throw 'Space in APK path lost'};return 'Success'}
        return 'Events injected: 1'
    }
    Invoke-PixelInstall $Apk $Adb '' 'com.mofumachi.merge' $Aapt
    foreach($Id in @('com.mofumachi.merge.qa','com.mofumachi.merge.qa.townlife','com.mofumachi.merge.qa.growthcheck')){
        $script:FixtureId=$Id;Invoke-PixelInstall $Apk $Adb 'Pixel' $Id $Aapt
    }
    $script:FixtureId='com.mofumachi.merge.qa.townlife'
    Expect-Rejection {Invoke-PixelInstall $Apk $Adb 'Pixel' 'com.mofumachi.merge.qa' $Aapt}
    Expect-Rejection {Invoke-PixelInstall $Apk $Adb 'Pixel' 'com.mofumachi.merge.qa.unknown' $Aapt}
    $Validation=(Get-Command (Join-Path $PSScriptRoot '../install-pixel3a.ps1')).Parameters['PackageName'].Attributes | Where-Object {$_ -is [System.Management.Automation.ValidateSetAttribute]}
    if($Validation.ValidValues.Count -ne 4 -or $Validation.ValidValues -notcontains 'com.mofumachi.merge.qa.townlife' -or $Validation.ValidValues -notcontains 'com.mofumachi.merge.qa.growthcheck'){throw 'Installer must allow exactly4 explicit package IDs, including the separate-save growth check.'}
    $script:FixtureId='com.mofumachi.merge'
    if(@($script:Calls | Where-Object {$_ -contains 'install' -and $_ -notcontains '-r'}).Count -gt 0){throw 'Update must retain save using install -r'}
    $script:Wrong=$true;Expect-Rejection {Invoke-PixelInstall $Apk $Adb '' 'com.mofumachi.merge' $Aapt};$script:Wrong=$false
    $script:FailInstall=$true;Expect-Rejection {Invoke-PixelInstall $Apk $Adb '' 'com.mofumachi.merge' $Aapt}
    if(@($script:Calls | Where-Object {$_ -contains 'uninstall' -or $_ -contains 'clear'}).Count -gt 0){throw 'Destructive data command'}
    # Explicit fresh testing is authorized only for this exact QA app/device.
    $script:FailInstall=$false;$script:FixtureId='com.mofumachi.merge.qa.townlife';$script:FixtureSerial='99RAY1BELH';$script:Calls=@()
    $ApkHash=(Get-FileHash -LiteralPath $Apk -Algorithm SHA256).Hash
    Invoke-PixelInstall $Apk $Adb $script:FixtureSerial $script:FixtureId $Aapt -FreshTownQa -ExpectedSha256 $ApkHash
    if(@($script:Calls | Where-Object {$_ -contains 'uninstall'}).Count -ne 1 -or @($script:Calls | Where-Object {$_ -contains 'install' -and $_ -notcontains '-r'}).Count -ne 1 -or @($script:Calls | Where-Object {$_ -contains 'clear' -and $_ -contains 'com.mofumachi.merge.qa.townlife'}).Count -ne 1){throw 'Fresh QA must uninstall/install without -r/clear restored QA data, in that order.'}
    $Mutations=@($script:Calls | Where-Object {$_ -contains 'uninstall' -or $_ -contains 'install' -or $_ -contains 'clear'})
    if($Mutations[0] -notcontains 'uninstall' -or $Mutations[1] -notcontains 'install' -or $Mutations[2] -notcontains 'clear'){throw 'Fresh reset command order is unsafe.'}
    foreach($Id in @('com.mofumachi.merge','com.mofumachi.merge.qa','com.mofumachi.merge.qa.growthcheck')){
        $script:FixtureId=$Id;$script:Calls=@();Expect-Rejection {Invoke-PixelInstall $Apk $Adb $script:FixtureSerial $Id $Aapt -FreshTownQa -ExpectedSha256 $ApkHash}
        if(@($script:Calls | Where-Object {$_ -contains 'uninstall' -or $_ -contains 'clear' -or $_ -contains 'install'}).Count){throw 'Fresh mode touched a different package.'}
    }
    $script:FixtureId='com.mofumachi.merge.qa.townlife';$script:Calls=@()
    Expect-Rejection {Invoke-PixelInstall $Apk $Adb $script:FixtureSerial $script:FixtureId $Aapt -FreshTownQa -ExpectedSha256 ('0'*64)}
    Expect-Rejection {Invoke-PixelInstall $Apk $Adb '' $script:FixtureId $Aapt -FreshTownQa -ExpectedSha256 $ApkHash}
    $script:BadModel=$true;Expect-Rejection {Invoke-PixelInstall $Apk $Adb $script:FixtureSerial $script:FixtureId $Aapt -FreshTownQa -ExpectedSha256 $ApkHash};$script:BadModel=$false
    if(@($script:Calls | Where-Object {$_ -contains 'uninstall' -or $_ -contains 'clear' -or $_ -contains 'install'}).Count){throw 'Fresh mode mutated before identity/hash/device validation.'}
    $script:FailUninstall=$true;$script:Calls=@();Expect-Rejection {Invoke-PixelInstall $Apk $Adb $script:FixtureSerial $script:FixtureId $Aapt -FreshTownQa -ExpectedSha256 $ApkHash}
    if(@($script:Calls | Where-Object {$_ -contains 'install' -or $_ -contains 'clear'}).Count){throw 'Failed uninstall must stop before install/reset.'}
    Write-Host 'Android script gates passed (mock commands, no device tested).'
} finally {Remove-Item -LiteralPath $Temporary -Recurse -Force}
