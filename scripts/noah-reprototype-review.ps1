# Read-only proof validator. Header/hash checks are not a visual review or full decode.
function Assert-NoahReprototypeEvidence([string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$AtlasSha) {
    $R=Get-Content -LiteralPath (Join-Path $Directory 'noah-reprototype.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    function Num($V){if($null -eq $V -or $V -isnot [ValueType] -or $V -is [bool] -or [double]::IsNaN([double]$V) -or [double]::IsInfinity([double]$V)){throw 'Missing finite proof number.'};return [double]$V}
    function TrueFlags($V,[string[]]$Keys){foreach($K in $Keys){if($V.$K -isnot [bool] -or -not $V.$K){throw "Missing Noah invariant: $K"}}}
    function Img($I,[int]$W,[int]$H,[string]$Pattern) {
        if($I.file -notmatch $Pattern -or $I.sha256 -notmatch '^[a-f0-9]{64}$' -or (Num $I.width) -ne $W -or (Num $I.height) -ne $H){throw 'Invalid Noah image declaration.'}
        $P=Join-Path $Directory $I.file
        if(-not(Test-Path -LiteralPath $P -PathType Leaf) -or (Get-Item -LiteralPath $P).Length -ne (Num $I.bytes) -or (Get-FileHash -LiteralPath $P -Algorithm SHA256).Hash -ne $I.sha256){throw 'Noah image identity changed.'}
        $B=[IO.File]::ReadAllBytes($P)
        if($I.file.EndsWith('.jpg')){if($B.Length -lt 4 -or $B[0] -ne 255 -or $B[1] -ne 216 -or $B[2] -ne 255){throw 'Not JPEG.'}}
        else{
            if($B.Length -lt 24 -or ($B[0..7] -join ',') -ne '137,80,78,71,13,10,26,10' -or [Text.Encoding]::ASCII.GetString($B,12,4) -ne 'IHDR'){throw 'Not PNG.'}
            $IW=([int64]$B[16]*16777216)+([int64]$B[17]*65536)+([int64]$B[18]*256)+$B[19];$IH=([int64]$B[20]*16777216)+([int64]$B[21]*65536)+([int64]$B[22]*256)+$B[23]
            if($IW -ne $W -or $IH -ne $H){throw 'Wrong PNG dimensions.'}
        }
    }
    if($R.noah_reprototype_version -ne 2 -or $R.kind -ne 'noah-depth-focus-comparison' -or $R.before -ne 'reconstructed current22 Noah renderer at identical clocks; shadow-only pair labeled separately' -or $R.baseline_source_revision -ne '22f2831d834a4be380c69692f8dfbdb2cf912181' -or $R.after -ne 'current product code'){throw 'Wrong current rejected-candidate comparison.'}
    if($Revision -notmatch '^[a-f0-9]{40}$' -or $Fingerprint -notmatch '^[a-f0-9]{64}$' -or $AtlasSha -notmatch '^[a-f0-9]{64}$' -or $R.source_revision -ne $Revision -or $R.product_fingerprint -ne $Fingerprint -or $R.original_atlas_sha256 -ne $AtlasSha){throw 'Noah proof source/atlas mismatch.'}
    TrueFlags $R @('native_unity')
    TrueFlags $R @('capture_clip_verified','current_before_branch_verified')
    if((Num $R.capture_canvas_distance) -ne 30 -or [Math]::Abs((Num $R.capture_near_clip)-.1) -gt .000001 -or (Num $R.capture_far_clip) -ne 100){throw 'Wrong portrait depth clearance for relief.'}
    foreach($K in @('pixel_verified','adopted','navigation_changed','teleport_rescue')){if($R.$K -isnot [bool] -or $R.$K){throw 'Native proof cannot claim device/adoption/navigation changes.'}}
    if(-not(Test-Path -LiteralPath (Join-Path $Directory 'noah-reprototype.html') -PathType Leaf)){throw 'Missing Noah browser review.'}
    $Pairs=@($R.noah_pairs);if($Pairs.Count -ne 9){throw 'Need eight body-depth pairs and one shadow-only pair.'}
    foreach($Mode in @('idle','walk','stop','turn','talk','reaction','forward','back','ground-shadow')){
        $Rows=@($Pairs | Where-Object {$_.mode -eq $Mode});if($Rows.Count -ne 1){throw 'Missing/repeated Noah state.'};$P=$Rows[0]
        if($P.id -ne "38-noah-$Mode" -or (Num $P.changed_pixels) -lt 1 -or (Num $P.outside_allowed_pixels) -ne 0){throw 'Noah pair invisible or outside its unchanged scope.'}
        if($Mode -eq 'ground-shadow'){if($P.comparison_kind -ne 'current22-shadow-only-current-body'){throw 'Wrong shadow-only baseline.'};TrueFlags $P @('body_unchanged_for_shadow')}elseif($P.comparison_kind -ne 'current22-whole-noah'){throw 'Body pairs must compare current22 with focus.'}
        TrueFlags $P @('frame_unchanged','actors_unchanged','camera_unchanged','water_clock_unchanged','water_phase_unchanged','state_unchanged','store_writes_unchanged','world_model_unchanged','other_five_unchanged')
        foreach($K in @('allowed_x','allowed_y','allowed_width','allowed_height')){if((Num $P.$K) -lt 0 -or (Num $P.$K) -ne [Math]::Floor((Num $P.$K))){throw 'Invalid actor pixel bounds.'}}
        if($P.allowed_width -lt 1 -or $P.allowed_height -lt 1 -or $P.allowed_x+$P.allowed_width -gt 720 -or $P.allowed_y+$P.allowed_height -gt 1280){throw 'Actor bounds outside portrait.'}
        Img $P.before 720 1280 ('^38-noah-'+$Mode+'-before-reconstructed\.png$');Img $P.after 720 1280 ('^38-noah-'+$Mode+'-after\.png$');Img $P.comparison 1440 1280 ('^38-noah-'+$Mode+'-before-left-after-right\.jpg$')
        $C=$P.name_control;if($C.mode -ne 'candidate-resident-name-visibility-in-reconstructed-before-only'){throw 'Wrong caption control.'};TrueFlags $C @('before_applied','after_natural','only_name_visibility_changed')
        $Maps=@{};foreach($K in @('natural_before','controlled_before','product_after')){$Map=@{};if(@($C.$K).Count -ne 6){throw 'Need six caption states.'};foreach($S in $C.$K){if($S.character_id -cnotin @('fio','lulu','miel','momo','noah','pom') -or $Map.ContainsKey($S.character_id) -or $S.active -isnot [bool]){throw 'Invalid caption state.'};$Map[$S.character_id]=$S.active};$Maps[$K]=$Map}
        $Changed=$false;foreach($Id in @('fio','lulu','miel','momo','noah','pom')){if($Maps.controlled_before[$Id] -ne $Maps.product_after[$Id]){throw 'After captions must remain natural.'};$Changed=$Changed -or ($Maps.natural_before[$Id] -ne $Maps.product_after[$Id])}
        if($C.natural_visibility_changed -isnot [bool] -or $C.natural_visibility_changed -ne $Changed){throw 'Natural caption change omitted.'}
        if($Changed){Img $C.natural_before_image 720 1280 ('^38-noah-'+$Mode+'-ui-natural-before\.png$')}
        elseif($C.natural_before_image.file -ne $P.before.file -or $C.natural_before_image.sha256 -ne $P.before.sha256 -or $C.natural_before_image.bytes -ne $P.before.bytes){throw 'Natural Before identity changed.'}
    }
    $S=$R.sequence;TrueFlags $S @('isolated_start_verified','manual_advance','state_unchanged','store_writes_unchanged','actual_conversation_verified')
    if($S.kind -ne 'native-continuous-fixed-step-Noah-reprototype' -or $S.setup -ne 'fresh-isolated-Home-scene-Noah-only' -or [Math]::Abs((Num $S.step_seconds)-1.0/30) -gt .000001 -or (Num $S.sample_step_count) -ne 2 -or $S.source_width -ne 720 -or $S.source_height -ne 1280){throw 'Wrong continuous native sequence.'}
    if($S.conversation_mode -ne 'speaker-noah-to-momo' -or $S.conversation_speaker_id -ne 'noah' -or $S.conversation_partner_id -ne 'momo' -or $S.conversation_text -ne ([regex]::Unescape('\u3053\u3093\u306b\u3061\u306f\u3001\u30e2\u30e2')+[char]0x266a)){throw 'Must demonstrate Noah speaking.'}
    if($S.greet_trigger -ne 'public TryBeginConversation(noah,momo); automatic social timer held only in isolated fixture' -or (Num $S.noah_reaction_seconds) -le 0 -or (Num $S.noah_reaction_seconds) -gt 1.001 -or (Num $S.momo_reaction_seconds) -le 0 -or (Num $S.momo_reaction_seconds) -gt .651 -or (Num $S.noah_facing) -ne 1 -or (Num $S.momo_facing) -ne -1){throw 'Actual mutual greeting and fixture control must be disclosed.'}
    foreach($K in @('crop_x','crop_y','crop_width','crop_height')){if((Num $S.$K) -lt 0 -or (Num $S.$K) -ne [Math]::Floor((Num $S.$K))){throw 'Invalid crop.'}}
    if($S.crop_width -lt 64 -or $S.crop_height -lt 64 -or $S.crop_x+$S.crop_width -gt 720 -or $S.crop_y+$S.crop_height -gt 1280){throw 'Crop outside portrait.'}
    $Frames=@($S.frames);if($Frames.Count -ne 222){throw 'Need222continuous frames including actual front/back walking.'};$Last=0.0;$Hashes=@();$WalkPositions=@()
    for($I=0;$I -lt 222;$I++){
        $F=$Frames[$I];$Mode=if($I -lt 12){'idle'}elseif($I -lt 36){'walk'}elseif($I -lt 48){'stop'}elseif($I -lt 90){'turn-and-walk'}elseif($I -lt 96){'settle'}elseif($I -lt 144){'noah-speaking'}elseif($I -lt 174){'forward-away'}elseif($I -lt 210){'back-toward'}else{'depth-stop'}
        $At=Num $F.elapsed_seconds;if($F.index -ne $I -or $F.mode -ne $Mode -or [Math]::Abs($At-$Last-(1.0/15)) -gt .0001 -or $F.walking -isnot [bool]){throw 'A gap, cut or mislabeled continuous state is not allowed.'};$Last=$At
        if([Math]::Abs((Num $F.facing)) -gt 1 -or (Num $F.reaction_seconds) -lt 0 -or (Num $F.reaction_seconds) -gt 1.001){throw 'Invalid pose clock.'}
        if($I -in @(29,65,158,195) -and -not $F.walking){throw 'Need actual walking both ways.'};if($I -eq 44 -and $F.walking){throw 'Need a real stop.'}
        if(($I -lt 12 -or $I -ge 36 -and $I -lt 48 -or $I -ge 95 -and $I -lt 144 -or $I -ge 210) -and $F.walking){throw 'Idle, stopped arrival and conversation must actually stop.'}
        if([Math]::Abs((Num $F.head_facing)) -gt 1 -or [Math]::Abs((Num $F.depth_forward)) -gt 1){throw 'Invalid head or forward-depth direction.'}
        Img $F.image $S.crop_width $S.crop_height ('^38-noah-frame-'+$I.ToString('00')+'\.jpg$');$Hashes+=$F.image.sha256
        $X=Num $F.world_x;$Y=Num $F.world_y;if($F.walking){$WalkPositions+=@("$X,$Y")}
    }
    if((Num $Frames[29].facing) -ge -.25 -or (Num $Frames[65].facing) -le .25 -or @($Hashes | Select-Object -Unique).Count -lt 8 -or @($WalkPositions | Select-Object -Unique).Count -lt 8){throw 'No actual turn or motion.'}
    if((Num $Frames[158].world_y) -le (Num $Frames[144].world_y)+.03 -or (Num $Frames[195].world_y) -ge (Num $Frames[174].world_y)-.05 -or (Num $Frames[158].depth_forward) -le .5 -or (Num $Frames[195].depth_forward) -ge -.5){throw 'Must actually move toward and away with signed depth response.'}
    $HeadLeads=$false;foreach($I in 48..51){$HeadLeads=$HeadLeads -or ((Num $Frames[$I].head_facing) -gt (Num $Frames[$I].facing)+.1)};if(-not $HeadLeads){throw 'Head must lead the body during the right turn.'}
    $Examples=@($R.noah_sorting_examples);if($Examples.Count -ne 2){throw 'Need front/back sorting examples.'}
    foreach($Id in @('fountain-front','fountain-back')){$Rows=@($Examples | Where-Object {$_.id -eq $Id});if($Rows.Count -ne 1){throw 'Missing sorting side.'};$E=$Rows[0];TrueFlags $E @('correct');$Behind=$Id -eq 'fountain-back';if($E.behind -isnot [bool] -or $E.behind -ne $Behind -or ((Num $E.actor_sibling) -lt (Num $E.prop_sibling)) -ne $Behind){throw 'Sorting must change sides with feet Y.'};Img $E.image 720 1280 ('^38-noah-'+$Id+'\.png$')}
    Write-Host 'Noah-only native identities and continuous sequence validated; visual/Pixel/user acceptance remain pending.'
}
