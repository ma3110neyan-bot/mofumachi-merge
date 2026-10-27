$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../build-android.ps1')
function Reject([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Expected north/Noah evidence rejection.'}}
$Temporary=Join-Path ([IO.Path]::GetTempPath()) ('mofu-north-noah-'+[guid]::NewGuid())
New-Item -ItemType Directory -Path $Temporary | Out-Null
try {
    New-Item -ItemType Directory -Path (Join-Path $Temporary 'noah-sequence') | Out-Null
    # Header-only files exercise gate metadata. They are explicitly synthetic,
    # never a successful native run or image review.
    function Image([string]$Name,[int]$Width=720,[int]$Height=1280,[int]$Salt=0){
        $Bytes=[byte[]]@(137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,0,0,0,0,0,0,$Salt)
        for($i=0;$i -lt 4;$i++){$Bytes[19-$i]=($Width -shr (8*$i)) -band 255;$Bytes[23-$i]=($Height -shr (8*$i)) -band 255}
        $Path=Join-Path $Temporary $Name;[IO.File]::WriteAllBytes($Path,$Bytes)
        return @{file=$Name;width=$Width;height=$Height;bytes=$Bytes.Length;sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
    }
    function Jpeg([string]$Id){$Name="$Id-before-left-after-right.jpg";$Path=Join-Path $Temporary $Name;[IO.File]::WriteAllBytes($Path,[byte[]]@(255,216,255,224,0,16,255,217));@{file=$Name;width=1440;height=1280;bytes=8;sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}}
    function Pair([string]$Id){@{comparison=(Jpeg $Id);state_sha256=('e'*64);world_model_sha256=('f'*64);unity_frame=42;store_writes=1;water_clock=.4;water_phase=.4;original_water_phase=.4;id=$Id;before=(Image "$Id-before-reconstructed.png");after=(Image "$Id-after.png" -Salt 1);changed_pixels=100;outside_allowed_pixels=0;frame_unchanged=$true;actors_unchanged=$true;camera_unchanged=$true;water_clock_unchanged=$true;water_phase_unchanged=$true;state_unchanged=$true;store_writes_unchanged=$true;world_model_unchanged=$true;other_five_unchanged=$true;allowed_x=100;allowed_y=400;allowed_width=450;allowed_height=220}}
    $North=@(foreach($Level in @(1,3,6)){foreach($View in 0..2){$P=Pair "36-north-lv$Level-view$View";$P.level=$Level;$P.camera_x=0;$P.camera_y=@(.85,1,1.15)[$View];$P}})
    $Noah=@(foreach($Mode in @('idle','walk','talk')){$P=Pair "37-noah-$Mode";$P.mode=$Mode;$P})
    $Routes=@(foreach($Id in @('central-to-north','north-to-central','left-out','left-return','right-out','right-return')){@{id=$Id;arrived=$true;all_points_walkable=$true;steps=10;distance=.5;image=(Image "36-route-$Id.png");points=@(@{x=0;y=.95;elapsed_seconds=0},@{x=0;y=1.1;elapsed_seconds=.2});start_x=0;start_y=.95;target_x=0;target_y=1.1}})
    $Frames=@(for($i=0;$i -lt 48;$i++){$Mode=if($i -lt 8){'idle'}elseif($i -lt 16){'start-walk'}elseif($i -lt 32){'walk'}elseif($i -lt 40){'stop'}else{'greet'};@{index=$i;mode=$Mode;elapsed_seconds=($i+1)*.1;world_x=(-.01*$i);world_y=-.25;walking=($i -ge 8 -and $i -lt 32);image=(Image ('noah-sequence/37-noah-frame-{0:D2}.png' -f $i) 320 240 $i)}})
    $HistoricalPath=Join-Path $PSScriptRoot '../../References/Review/fountain-final-2026-10-27-candidate/native/09-connected-path-north.png'
    Copy-Item -LiteralPath $HistoricalPath -Destination (Join-Path $Temporary '36-north-historical-bb3fb82.png')
    $Historical=@{file='36-north-historical-bb3fb82.png';width=720;height=1280;bytes=(Get-Item -LiteralPath $HistoricalPath).Length;sha256=(Get-FileHash -LiteralPath $HistoricalPath -Algorithm SHA256).Hash.ToLowerInvariant()}
    foreach($Route in $Routes){
        switch($Route.id){
            'central-to-north' {$Route.start_y=-.25;$Route.target_y=1.65}
            'north-to-central' {$Route.start_y=1.65;$Route.target_y=-.25}
            default {$Route.start_x=if($Route.id.StartsWith('left')){-.30}else{.30};$Route.target_x=$Route.start_x;$Route.start_y=if($Route.id.EndsWith('out')){.95}else{1.05};$Route.target_y=if($Route.id.EndsWith('out')){1.05}else{.95}}
        };$Route.points=@(@{x=$Route.start_x;y=$Route.start_y;elapsed_seconds=0},@{x=$Route.target_x;y=$Route.target_y;elapsed_seconds=.2})
    }
    $Record=@{historical_native_before=$Historical;kind='same-state-native-north-noah-comparison';source_revision=('a'*40);product_fingerprint=('b'*64);before='reconstructed causal baseline: north join disabled / Noah legacy at the same instant';after='current product code';native_unity=$true;pixel_verified=$false;adopted=$false;north_world_bounds=@{x_min=-.6;x_max=.6;y_min=1;y_max=1.32};north_pairs=$North;noah_pairs=$Noah;routes=$Routes;navigation_changed=$false;teleport_rescue=$false;sequence=@{kind='native-fixed-step-town-AdvanceFrame';manual_advance=$true;step_seconds=(1.0/30);sample_step_count=3;source_width=720;source_height=1280;crop_x=100;crop_y=400;crop_width=320;crop_height=240;state_unchanged=$true;store_writes_unchanged=$true;frames=$Frames}}
    function Save {$Record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $Temporary 'north-noah-comparison.json') -Encoding UTF8}
    $Record.sequence.setup='fresh-isolated-Home-scene-after-completed-route-proof';$Record.sequence.isolated_start_verified=$true
    $Record.sequence.actual_conversation_verified=$true;$Record.sequence.conversation_mode='mutual-noah-momo-greeting';$Record.sequence.greet_trigger='public TryBeginConversation or normal AdvanceFrame auto; verified Noah/Momo greeting'
    $Record.sequence.conversation_speaker_id='noah';$Record.sequence.conversation_partner_id='momo';$Record.sequence.conversation_text=[regex]::Unescape('\u3053\u3093\u306b\u3061\u306f\u3001\u30e2\u30e2\u266a')
    $Record.sequence.noah_reaction_seconds=.65;$Record.sequence.momo_reaction_seconds=.65;$Record.sequence.noah_facing=1;$Record.sequence.momo_facing=-1
    Set-Content -LiteralPath (Join-Path $Temporary 'north-noah-before-after.html') '<!doctype html><title>Synthetic metadata fixture</title>'
    Save;Assert-NorthNoahEvidence $Temporary ('a'*40) ('b'*64)
    $Record.sequence.isolated_start_verified=$false;Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.sequence.isolated_start_verified=$true
    $Record.sequence.Remove('isolated_start_verified');Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.sequence.isolated_start_verified=$true
    $Record.sequence.setup='reused-route-snapshot';Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.sequence.setup='fresh-isolated-Home-scene-after-completed-route-proof'
    Reject {Assert-NorthNoahEvidence $Temporary ('c'*40) ('b'*64)}
    Reject {Assert-NorthNoahEvidence $Temporary ('a'*40) ('d'*64)}
    $Record.sequence.actual_conversation_verified=$false;Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.sequence.actual_conversation_verified=$true
    $Record.sequence.Remove('actual_conversation_verified');Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.sequence.actual_conversation_verified=$true
    $Record.sequence.conversation_mode='ambient-solo-bubble';Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.sequence.conversation_mode='mutual-noah-momo-greeting'
    $Greeting=$Record.sequence.conversation_text;$Record.sequence.conversation_text='ambient line only';Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.sequence.conversation_text=$Greeting
    $Record.sequence.noah_reaction_seconds=0;Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.sequence.noah_reaction_seconds=.65
    $Record.sequence.momo_facing=0;Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.sequence.momo_facing=-1
    $Record.sequence.greet_trigger='ambient solo speech';Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.sequence.greet_trigger='public TryBeginConversation or normal AdvanceFrame auto; verified Noah/Momo greeting'
    $Record.sequence.conversation_speaker_id='lulu';Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.sequence.conversation_speaker_id='noah'
    $Record.sequence.conversation_speaker_id='momo';$Record.sequence.conversation_partner_id='noah';$Record.sequence.conversation_text=[regex]::Unescape('\u3053\u3093\u306b\u3061\u306f\u3001\u30ce\u30a2\u266a');Save;Assert-NorthNoahEvidence $Temporary
    $Record.sequence.conversation_speaker_id='noah';$Record.sequence.conversation_partner_id='momo';$Record.sequence.conversation_text=$Greeting
    $Jpeg=$North[0].comparison;$North[0].comparison=$null;Save;Reject {Assert-NorthNoahEvidence $Temporary};$North[0].comparison=$Jpeg
    $Jpeg.width=720;Save;Reject {Assert-NorthNoahEvidence $Temporary};$Jpeg.width=1440
    $JpegPath=Join-Path $Temporary $Jpeg.file;$JpegBytes=[IO.File]::ReadAllBytes($JpegPath);$JpegHash=$Jpeg.sha256
    [IO.File]::WriteAllBytes($JpegPath,[byte[]]@(0,216,255,224,0,16,255,217));$Jpeg.sha256=(Get-FileHash -LiteralPath $JpegPath -Algorithm SHA256).Hash.ToLowerInvariant();Save;Reject {Assert-NorthNoahEvidence $Temporary};[IO.File]::WriteAllBytes($JpegPath,$JpegBytes);$Jpeg.sha256=$JpegHash
    foreach($Key in @('pixel_verified','adopted','navigation_changed','teleport_rescue')){$Record[$Key]=$true;Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record[$Key]=$false}
    foreach($Key in @('frame_unchanged','actors_unchanged','camera_unchanged','water_clock_unchanged','water_phase_unchanged','state_unchanged','store_writes_unchanged','world_model_unchanged')){$North[0][$Key]=$false;Save;Reject {Assert-NorthNoahEvidence $Temporary};$North[0][$Key]=$true}
    $Noah[0].other_five_unchanged=$false;Save;Reject {Assert-NorthNoahEvidence $Temporary};$Noah[0].other_five_unchanged=$true
    $North[0].outside_allowed_pixels=1;Save;Reject {Assert-NorthNoahEvidence $Temporary};$North[0].outside_allowed_pixels=0
    $North[0].changed_pixels=0;Save;Reject {Assert-NorthNoahEvidence $Temporary};$North[0].changed_pixels=100
    $North[0].camera_y=9;Save;Reject {Assert-NorthNoahEvidence $Temporary};$North[0].camera_y=.85
    $Record.north_pairs=@($North | Select-Object -Skip 1);Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.north_pairs=$North
    $Routes[2].target_x=0;Save;Reject {Assert-NorthNoahEvidence $Temporary};$Routes[2].target_x=-.30
    $Routes[0].arrived=$false;Save;Reject {Assert-NorthNoahEvidence $Temporary};$Routes[0].arrived=$true
    $Frames[20].world_x=$null;Save;Reject {Assert-NorthNoahEvidence $Temporary};$Frames[20].world_x=-.2
    $Frames[20].elapsed_seconds=.1;Save;Reject {Assert-NorthNoahEvidence $Temporary};$Frames[20].elapsed_seconds=2.1
    $Frames[20].image.file='../outside.png';Save;Reject {Assert-NorthNoahEvidence $Temporary};$Frames[20].image.file='noah-sequence/37-noah-frame-20.png'
    $Record.sequence.manual_advance=$false;Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.sequence.manual_advance=$true
    $Record.sequence.frames=@($Frames | Select-Object -Skip 1);Save;Reject {Assert-NorthNoahEvidence $Temporary};$Record.sequence.frames=$Frames
    Save;$Path=Join-Path $Temporary $North[0].before.file;$Bytes=[IO.File]::ReadAllBytes($Path);[IO.File]::WriteAllText($Path,'tampered');Reject {Assert-NorthNoahEvidence $Temporary};[IO.File]::WriteAllBytes($Path,$Bytes)
    $Missing=Join-Path $Temporary $Frames[0].image.file;Move-Item -LiteralPath $Missing -Destination ($Missing+'.held');Reject {Assert-NorthNoahEvidence $Temporary};Move-Item -LiteralPath ($Missing+'.held') -Destination $Missing
    $Pack=Join-Path $Temporary 'pack';$Screens=Join-Path $Pack 'screenshots';New-Item -ItemType Directory -Path $Screens -Force | Out-Null
    foreach($Entry in @(Get-ChildItem -LiteralPath $Temporary | Where-Object {$_.Name -ne 'pack'})){Copy-Item -LiteralPath $Entry.FullName -Destination $Screens -Recurse}
    Write-NativeReviewArchives $Pack
    $Zip=[IO.Compression.ZipFile]::OpenRead((Join-Path $Pack 'five-plaza-review-01.zip'))
    try {
        foreach($Name in @('north-noah-comparison.json','north-noah-before-after.html','36-north-historical-bb3fb82.png')+@($North | ForEach-Object {$_.before.file;$_.after.file;$_.comparison.file})+@($Noah | ForEach-Object {$_.before.file;$_.after.file;$_.comparison.file})+@($Routes | ForEach-Object {$_.image.file})+@($Frames | ForEach-Object {[IO.Path]::GetFileName($_.image.file)})){
            if(@($Zip.Entries | Where-Object {$_.FullName -eq $Name}).Count -ne 1){throw "New native evidence omitted or collided in existing TownQa archive: $Name"}
        }
    } finally {$Zip.Dispose()}
    Remove-Item -LiteralPath (Join-Path $Temporary 'north-noah-before-after.html');Reject {Assert-NorthNoahEvidence $Temporary}
    Write-Host 'North/Noah metadata gate tests passed; synthetic fixtures only, native Unity/Pixel execution pending.'
} finally {Remove-Item -LiteralPath $Temporary -Recurse -Force}
