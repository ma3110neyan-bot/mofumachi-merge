# Identity/invariant gate for actual native outputs. Synthetic fixtures validate
# this parser only; successful metadata validation is never an image review.
function Assert-NorthNoahEvidence([string]$Directory,[string]$ExpectedRevision='',[string]$ExpectedFingerprint='',[switch]$RequireQuality) {
    $Record=Get-Content -LiteralPath (Join-Path $Directory 'north-noah-comparison.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if(-not(Test-Path -LiteralPath (Join-Path $Directory 'north-noah-before-after.html') -PathType Leaf)){throw 'Missing north/Noah native comparison HTML.'}
    function Number($Value){if($null -eq $Value -or $Value -isnot [ValueType] -or $Value -is [bool]){throw 'Missing native numeric field.'};$N=[double]$Value;if([double]::IsNaN($N) -or [double]::IsInfinity($N)){throw 'Nonfinite native field.'};return $N}
    function Flags($Value,[string[]]$Keys,[bool]$Expected){foreach($Key in $Keys){if($Value.$Key -isnot [bool] -or $Value.$Key -ne $Expected){throw "Wrong north/Noah invariant: $Key"}}}
    function Image($Entry,[int]$Width,[int]$Height,[string]$Pattern){
        if($Entry.file -notmatch $Pattern){throw 'Unsafe native evidence path.'}
        $Path=Join-Path $Directory $Entry.file
        if($Entry.sha256 -notmatch '^[a-f0-9]{64}$' -or -not(Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-Item -LiteralPath $Path).Length -ne (Number $Entry.bytes) -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Entry.sha256){throw 'Native image identity changed.'}
        $Stream=[IO.File]::OpenRead($Path);try{$Header=New-Object byte[] 24;$Read=$Stream.Read($Header,0,24)}finally{$Stream.Dispose()}
        if($Read -ne 24 -or (Get-Item -LiteralPath $Path).Length -le 24 -or ($Header[0..7] -join ',') -ne '137,80,78,71,13,10,26,10' -or [Text.Encoding]::ASCII.GetString($Header,12,4) -ne 'IHDR'){throw 'Invalid native PNG.'}
        $W=([int64]$Header[16]*16777216)+([int64]$Header[17]*65536)+([int64]$Header[18]*256)+$Header[19];$H=([int64]$Header[20]*16777216)+([int64]$Header[21]*65536)+([int64]$Header[22]*256)+$Header[23]
        if($W -ne $Width -or $H -ne $Height -or (Number $Entry.width) -ne $Width -or (Number $Entry.height) -ne $Height){throw 'Native image dimensions changed.'}
    }
    function ComparisonJpeg($Entry,[string]$Id){
        if($null -eq $Entry -or $Entry.file -notmatch ('^'+[regex]::Escape($Id)+'-before-left-after-right\.jpg$') -or (Number $Entry.width) -ne 1440 -or (Number $Entry.height) -ne 1280){throw 'Missing browser comparison JPEG with Before left / After right.'}
        $Path=Join-Path $Directory $Entry.file
        if($Entry.sha256 -notmatch '^[a-f0-9]{64}$' -or -not(Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-Item -LiteralPath $Path).Length -ne (Number $Entry.bytes) -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Entry.sha256){throw 'Comparison JPEG identity changed.'}
        $Stream=[IO.File]::OpenRead($Path);try{$A=$Stream.ReadByte();$B=$Stream.ReadByte();$C=$Stream.ReadByte()}finally{$Stream.Dispose()}
        # This is an identity/magic check, not a full JPEG decoder. Actual native
        # review must decode the entire JPEG and both original PNGs.
        if($A -ne 255 -or $B -ne 216 -or $C -ne 255){throw 'Comparison .jpg is not JPEG encoded.'}
    }
    if($Record.kind -ne 'same-state-native-north-noah-comparison' -or $Record.before -ne 'reconstructed causal baseline: north join disabled / Noah legacy at the same instant' -or $Record.after -ne 'current product code'){throw 'Wrong native causal comparison mode.'}
    Flags $Record @('native_unity') $true;Flags $Record @('pixel_verified','adopted','navigation_changed','teleport_rescue') $false
    if($Record.source_revision -notmatch '^[a-f0-9]{40}$' -or $Record.product_fingerprint -notmatch '^[a-f0-9]{64}$' -or ($ExpectedRevision -and $Record.source_revision -ne $ExpectedRevision) -or ($ExpectedFingerprint -and $Record.product_fingerprint -ne $ExpectedFingerprint)){throw 'North/Noah source does not match this capture/build.'}
    $B=$Record.north_world_bounds
    # The producer declares these coordinates as C# float. JsonUtility may
    # write .6f as .6000000238418579; compare exact native Single values rather
    # than mixing Single-derived JSON with Double literals or a wider epsilon.
    if([single](Number $B.x_min) -ne [single](-.6) -or [single](Number $B.x_max) -ne [single](.6) -or [single](Number $B.y_min) -ne [single](1) -or [single](Number $B.y_max) -ne [single](1.32)){throw 'North changed its bounded scope.'}
    Image $Record.historical_native_before 720 1280 '^36-north-historical-bb3fb82\.png$'
    if($Record.historical_native_before.sha256 -ne 'b001ee0b7bb6a1719876b16c951a9bdcf5c58df3d7817dc797b379cf0c863324'){throw 'Historical north native original changed.'}
    $North=@($Record.north_pairs);$Noah=@($Record.noah_pairs)
    $Version=if($Record.PSObject.Properties['noah_quality_version']){Number $Record.noah_quality_version}else{1}
    if($Version -notin @(1,2) -or ($RequireQuality -and $Version -ne 2)){throw 'Current Noah quality evidence version is required.'}
    $Quality=$Version -eq 2
    $Modes=if($Quality){@('idle','walk','talk','turn','stop','reaction')}else{@('idle','walk','talk')}
    if($North.Count -ne 9 -or $Noah.Count -ne $Modes.Count){throw 'Missing north or complete Noah same-state pose pairs.'}
    foreach($Level in @(1,3,6)){foreach($View in 0..2){$Y=@(.85,1,1.15)[$View];$Rows=@($North | Where-Object {$_.level -eq $Level -and [Math]::Abs([double]$_.camera_y-$Y) -lt .00001 -and $_.camera_x -eq 0});if($Rows.Count -ne 1 -or $Rows[0].id -ne "36-north-lv$Level-view$View"){throw 'Missing or repeated north level/view pair.'}}}
    foreach($Mode in $Modes){if(@($Noah | Where-Object {$_.mode -eq $Mode -and $_.id -eq "37-noah-$Mode"}).Count -ne 1){throw 'Missing Noah mode pair.'}}
    foreach($Pair in @($North)+@($Noah)){
        Flags $Pair @('frame_unchanged','actors_unchanged','camera_unchanged','water_clock_unchanged','water_phase_unchanged','state_unchanged','store_writes_unchanged','world_model_unchanged') $true
        if($Pair.state_sha256 -notmatch '^[a-f0-9]{64}$' -or $Pair.world_model_sha256 -notmatch '^[a-f0-9]{64}$'){throw 'Missing saved-state/model audit digest.'}
        foreach($Key in @('unity_frame','store_writes','water_clock','water_phase','original_water_phase')){if((Number $Pair.$Key) -lt 0){throw 'Invalid same-instant audit value.'}}
        if((Number $Pair.changed_pixels) -lt 1 -or (Number $Pair.outside_allowed_pixels) -ne 0){throw 'Causal pair is invisible or changed pixels outside its projection.'}
        foreach($Key in @('allowed_x','allowed_y','allowed_width','allowed_height')){$N=Number $Pair.$Key;if($N -lt 0 -or $N -ne [Math]::Floor($N)){throw 'Invalid causal pixel rectangle.'}}
        if($Pair.allowed_width -lt 1 -or $Pair.allowed_height -lt 1 -or $Pair.allowed_x+$Pair.allowed_width -gt 720 -or $Pair.allowed_y+$Pair.allowed_height -gt 1280){throw 'Causal rectangle lies outside portrait.'}
        $Before='^'+[regex]::Escape($Pair.id)+'-before-reconstructed\.png$';$After='^'+[regex]::Escape($Pair.id)+'-after\.png$'
        Image $Pair.before 720 1280 $Before;Image $Pair.after 720 1280 $After
        ComparisonJpeg $Pair.comparison $Pair.id
    }
    foreach($Pair in $Noah){Flags $Pair @('other_five_unchanged') $true}
    $Routes=@($Record.routes);if($Routes.Count -ne 6){throw 'Missing actual central/north crossing and return paths.'}
    foreach($Id in @('central-to-north','north-to-central','left-out','left-return','right-out','right-return')){
        $Rows=@($Routes | Where-Object {$_.id -eq $Id});if($Rows.Count -ne 1){throw 'Missing/repeated actual walking path.'};$Route=$Rows[0]
        Flags $Route @('arrived','all_points_walkable') $true
        if((Number $Route.steps) -le 0 -or (Number $Route.distance) -le 0 -or @($Route.points).Count -lt 2){throw 'Walking proof needs actual steps and sampled model positions.'}
        $Last=-1.0;foreach($Point in $Route.points){$X=Number $Point.x;$Y=Number $Point.y;$At=Number $Point.elapsed_seconds;if($At -le $Last){throw 'Invalid walking chronology.'};$Last=$At}
        $First=$Route.points[0];$End=$Route.points[-1]
        if([Math]::Abs($First.x-(Number $Route.start_x)) -gt .002 -or [Math]::Abs($First.y-(Number $Route.start_y)) -gt .002 -or [Math]::Abs($End.x-(Number $Route.target_x)) -gt .002 -or [Math]::Abs($End.y-(Number $Route.target_y)) -gt .002){throw 'Walking proof endpoints do not match actual model trace.'}
        if($Id -eq 'central-to-north' -and ($Route.start_y -gt 1 -or $Route.target_y -lt 1.32)){throw 'Outward path must actually reach North from Central.'}
        if($Id -eq 'north-to-central' -and ($Route.start_y -lt 1.32 -or $Route.target_y -gt 1)){throw 'Return path must actually reach Central from North.'}
        if($Id -match '^(left|right)-(out|return)$'){
            $X=if($Matches[1] -eq 'left'){-.30}else{.30};$From=if($Matches[2] -eq 'out'){.95}else{1.05};$To=if($Matches[2] -eq 'out'){1.05}else{.95}
            if([Math]::Abs($Route.start_x-$X) -gt .002 -or [Math]::Abs($Route.target_x-$X) -gt .002 -or [Math]::Abs($Route.start_y-$From) -gt .002 -or [Math]::Abs($Route.target_y-$To) -gt .002){throw 'Broad seam crossings must walk both x+/-0.30 lanes in both directions.'}
        }
        Image $Route.image 720 1280 ('^36-route-'+$Id+'\.png$')
    }
    $Sequence=$Record.sequence
    if($Sequence.kind -ne 'native-fixed-step-town-AdvanceFrame' -or [Math]::Abs((Number $Sequence.step_seconds)-(1.0/30)) -gt .000001 -or $Sequence.sample_step_count -ne 3 -or $Sequence.source_width -ne 720 -or $Sequence.source_height -ne 1280){throw 'Sequence must label its actual fixed-step mode.'}
    if($Sequence.setup -ne 'fresh-isolated-Home-scene-after-completed-route-proof'){throw 'Actor evidence must start in a separate fresh fixture episode after the completed route proof.'}
    Flags $Sequence @('isolated_start_verified') $true
    Flags $Sequence @('manual_advance','state_unchanged','store_writes_unchanged') $true
    Flags $Sequence @('actual_conversation_verified') $true
    if($Sequence.conversation_mode -ne 'mutual-noah-momo-greeting' -or $Sequence.greet_trigger -ne 'public TryBeginConversation or normal AdvanceFrame auto; verified Noah/Momo greeting'){throw 'Ambient speech is not Noah/Momo mutual-greeting evidence.'}
    if(($Sequence.conversation_speaker_id -eq 'noah' -and $Sequence.conversation_partner_id -eq 'momo')){$Partner=[regex]::Unescape('\u30e2\u30e2')}
    elseif($Sequence.conversation_speaker_id -eq 'momo' -and $Sequence.conversation_partner_id -eq 'noah'){$Partner=[regex]::Unescape('\u30ce\u30a2')}
    else{throw 'Greeting must identify actual Noah/Momo speaker and partner.'}
    $NoahReactionLimit=if($Quality){1.001}else{.651}
    if($Sequence.conversation_text -ne ([regex]::Unescape('\u3053\u3093\u306b\u3061\u306f\u3001')+$Partner+[char]0x266a) -or (Number $Sequence.noah_reaction_seconds) -le 0 -or (Number $Sequence.momo_reaction_seconds) -le 0 -or (Number $Sequence.noah_reaction_seconds) -gt $NoahReactionLimit -or (Number $Sequence.momo_reaction_seconds) -gt .651 -or (Number $Sequence.noah_facing) -ne 1 -or (Number $Sequence.momo_facing) -ne -1){throw 'Greeting text, both positive reactions and mutual facing must be verified together.'}
    foreach($Key in @('crop_x','crop_y','crop_width','crop_height')){$N=Number $Sequence.$Key;if($N -lt 0 -or $N -ne [Math]::Floor($N)){throw 'Invalid actor crop.'}}
    if($Sequence.crop_width -lt 64 -or $Sequence.crop_height -lt 64 -or $Sequence.crop_x+$Sequence.crop_width -gt 720 -or $Sequence.crop_y+$Sequence.crop_height -gt 1280){throw 'Actor crop outside portrait.'}
    $Frames=@($Sequence.frames);if($Frames.Count -ne 48){throw 'Need exactly48 bounded native actor crops.'}
    $Last=-1.0;$Hashes=@();$WalkPositions=@();for($i=0;$i -lt 48;$i++){$Frame=$Frames[$i];$Mode=if($i -lt 8){'idle'}elseif($i -lt 16){'start-walk'}elseif($i -lt 32){'walk'}elseif($i -lt 40){'stop'}else{'greet'};$At=Number $Frame.elapsed_seconds
        $X=Number $Frame.world_x;$Y=Number $Frame.world_y;if($i -ge 8 -and $i -lt 32){$WalkPositions+=@("$X,$Y")}
        if($Frame.index -ne $i -or $Frame.mode -ne $Mode -or $At -le $Last -or $Frame.walking -isnot [bool] -or $Frame.walking -ne ($i -ge 8 -and $i -lt 32) -or ($Last -ge 0 -and $i -ne 40 -and [Math]::Abs($At-$Last-.1) -gt .0001)){throw 'Missing, mislabeled or nonchronological actor sequence.'};$Last=$At
        Image $Frame.image $Sequence.crop_width $Sequence.crop_height ('^noah-sequence/37-noah-frame-'+$i.ToString('00')+'\.png$');$Hashes+=@($Frame.image.sha256)
    }
    if(@($Hashes | Select-Object -Unique).Count -lt 8 -or @($WalkPositions | Select-Object -Unique).Count -lt 8){throw 'Actor sequence or actual model walking is frozen.'}
    if($Quality) {
        foreach($Frame in $Frames){$Facing=Number $Frame.facing;$Reaction=Number $Frame.reaction_seconds;if($Facing -lt -1 -or $Facing -gt 1 -or $Reaction -lt 0 -or $Reaction -gt 1.001){throw 'Invalid actual Noah facing/reaction sample.'}}
        if((Number $Frames[20].facing) -gt -.25 -or (Number $Frames[26].facing) -lt .25){throw 'Noah must actually reverse direction within the contiguous walking episode.'}
    }
    Write-Host 'North/Noah native evidence metadata validated; visual/Pixel review and adoption pending.'
}
