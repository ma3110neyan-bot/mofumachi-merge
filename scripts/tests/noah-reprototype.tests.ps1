$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../capture-noah-reprototype.ps1')
function Reject([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Invalid Noah proof accepted.'}}
$Temporary=Join-Path ([IO.Path]::GetTempPath()) ('mofu-noah-reprototype-'+[guid]::NewGuid())
try {
    $NativeArgs=@(Get-NoahReprototypeArguments 'repo with spaces' 'output with spaces' ('a'*40) ('b'*64))
    if($NativeArgs[[Array]::IndexOf($NativeArgs,'-testFilter')+1] -ne 'Mofumachi.Tests.ExperienceCaptureTests.NoahReprototypeContinuousSequencePreservesArtAndSavedState'){throw 'Must run exactly the Noah-only method, not North or full suites.'}
    if('-quit' -in $NativeArgs -or $NativeArgs[[Array]::IndexOf($NativeArgs,'-buildTarget')+1] -ne 'Android'){throw 'Wrong native job configuration.'}
    Reject {Get-NoahReprototypeArguments 'repo' 'out' 'short' ('b'*64)}
    $SavedEditor=$UnityEditor;$script:LaunchedPath=''
    function Start-Process([string]$FilePath,[string]$ArgumentList,[switch]$Wait,[switch]$PassThru){
        if(-not $Wait -or -not $PassThru){throw 'Native process must complete before validation.'}
        $script:LaunchedPath=$FilePath;$script:LaunchedArguments=$ArgumentList
        [pscustomobject]@{ExitCode=0}
    }
    try{
        $UnityEditor='default fixture editor'
        Invoke-Unity -UnityArguments $NativeArgs -EditorPath 'custom Unity with spaces/Unity.exe'
        if($script:LaunchedPath -ne 'custom Unity with spaces/Unity.exe'){throw 'Selected editor was validated but ignored by the real launcher.'}
        Invoke-Unity -UnityArguments $NativeArgs
        if($script:LaunchedPath -ne 'default fixture editor'){throw 'Default editor compatibility changed.'}
    }finally{$UnityEditor=$SavedEditor;Remove-Item Function:Start-Process}
    $Dir=Join-Path $Temporary 'screenshots/noah-reprototype';New-Item -ItemType Directory -Path $Dir -Force | Out-Null
    $Bytes=[byte[]]@(0,255,13,10,9,42)
    [IO.File]::WriteAllBytes((Join-Path $Dir '38-noah-frame-00.png'),$Bytes)
    [IO.File]::WriteAllText((Join-Path $Dir 'noah-reprototype.json'),'{"synthetic":true}')
    [IO.File]::WriteAllText((Join-Path $Dir 'noah-reprototype.html'),'synthetic HTML')
    [IO.File]::WriteAllText((Join-Path $Temporary 'playmode.xml'),'<synthetic/>')
    [IO.File]::WriteAllText((Join-Path $Temporary 'screenshots/36-north-unrelated.png'),'unrelated')
    [IO.File]::WriteAllText((Join-Path $Temporary 'unrelated.apk'),'must not ship')
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $Parts=@(Write-NoahReprototypeArchives $Temporary -MaxPartBytes 30)
    if($Parts.Count -lt 2){throw 'No archive split exercised.'}
    $Names=@();foreach($Part in $Parts){$Zip=[IO.Compression.ZipFile]::OpenRead($Part);try{
        $Names+=@($Zip.Entries.FullName)
        if(($Zip.Entries | Measure-Object Length -Sum).Sum -gt 30){throw 'Raw upload budget exceeded.'}
        $Frame=$Zip.GetEntry('38-noah-frame-00.png');if($Frame){$Stream=$Frame.Open();$Memory=New-Object IO.MemoryStream;try{$Stream.CopyTo($Memory);if(($Memory.ToArray() -join ',') -ne ($Bytes -join ',')){throw 'Original evidence bytes changed.'}}finally{$Memory.Dispose();$Stream.Dispose()}}
    }finally{$Zip.Dispose()}}
    foreach($Name in @('38-noah-frame-00.png','noah-reprototype.json','noah-reprototype.html','playmode.xml')){if(@($Names | Where-Object {$_ -eq $Name}).Count -ne 1){throw 'Original missing/duplicated.'}}
    if('36-north-unrelated.png' -in $Names -or 'unrelated.apk' -in $Names){throw 'Included unrelated work.'}
    Reject {Assert-NoahReprototypeEvidence $Dir ('a'*40) ('b'*64) ('c'*64)}
    function ProofImage([string]$Name,[int]$Width,[int]$Height,[bool]$Jpeg=$false){
        if($Jpeg){$Data=New-Object byte[] 32;$Data[0]=255;$Data[1]=216;$Data[2]=255}else{
            $Data=New-Object byte[] 32;([byte[]]@(137,80,78,71,13,10,26,10)).CopyTo($Data,0);[Text.Encoding]::ASCII.GetBytes('IHDR').CopyTo($Data,12)
            foreach($N in 0..3){$Data[16+$N]=[byte](($Width -shr (24-8*$N)) -band 255);$Data[20+$N]=[byte](($Height -shr (24-8*$N)) -band 255)}
        }
        $Path=Join-Path $Dir $Name;[IO.File]::WriteAllBytes($Path,$Data)
        return [ordered]@{file=$Name;bytes=$Data.Length;width=$Width;height=$Height;sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
    }
    $States=@(foreach($Id in @('fio','lulu','miel','momo','noah','pom')){@{character_id=$Id;active=$true}})
    $Pairs=@(foreach($Mode in @('idle','walk','stop','turn','talk','reaction')){
        $Before=ProofImage "38-noah-$Mode-before-reconstructed.png" 720 1280
        [ordered]@{id="38-noah-$Mode";mode=$Mode;changed_pixels=1;outside_allowed_pixels=0;allowed_x=0;allowed_y=0;allowed_width=50;allowed_height=100;
            frame_unchanged=$true;actors_unchanged=$true;camera_unchanged=$true;water_clock_unchanged=$true;water_phase_unchanged=$true;state_unchanged=$true;store_writes_unchanged=$true;world_model_unchanged=$true;other_five_unchanged=$true;
            before=$Before;after=(ProofImage "38-noah-$Mode-after.png" 720 1280);comparison=(ProofImage "38-noah-$Mode-before-left-after-right.jpg" 1440 1280 $true);
            name_control=@{mode='candidate-resident-name-visibility-in-reconstructed-before-only';before_applied=$true;after_natural=$true;only_name_visibility_changed=$true;natural_visibility_changed=$false;natural_before=$States;controlled_before=$States;product_after=$States;natural_before_image=$Before}}
    })
    $Frames=@(foreach($I in 0..143){$Mode=if($I -lt 12){'idle'}elseif($I -lt 36){'walk'}elseif($I -lt 48){'stop'}elseif($I -lt 90){'turn-and-walk'}elseif($I -lt 96){'settle'}else{'noah-speaking'}
        @{index=$I;mode=$Mode;elapsed_seconds=($I+1)*(1.0/15);world_x=$I*.01;world_y=-.25;facing=$(if($I -lt 48){-1}else{1});reaction_seconds=0;walking=($I -ge 12 -and $I -lt 36 -or $I -ge 48 -and $I -le 94);image=(ProofImage ('38-noah-frame-{0:D2}.jpg' -f $I) 200 200 $true)}
    })
    # Different harmless payload bytes exercise the parser's non-frozen-hash
    # requirement. These are header fixtures, not decodable native images.
    foreach($I in 0..143){$P=Join-Path $Dir $Frames[$I].image.file;$Data=[IO.File]::ReadAllBytes($P);$Data[31]=[byte]$I;[IO.File]::WriteAllBytes($P,$Data);$Frames[$I].image.sha256=(Get-FileHash -LiteralPath $P -Algorithm SHA256).Hash.ToLowerInvariant()}
    $Examples=@(foreach($Id in @('fountain-front','fountain-back')){$Behind=$Id -eq 'fountain-back';@{id=$Id;kind='display-only world-foot-Y sorting, not route proof';world_x=.26;world_y=$(if($Behind){.46}else{.14});actor_sibling=$(if($Behind){1}else{3});prop_sibling=2;behind=$Behind;correct=$true;image=(ProofImage "38-noah-$Id.png" 720 1280)}})
    $Valid=[ordered]@{capture_clip_verified=$true;capture_canvas_distance=30;capture_near_clip=.1;capture_far_clip=100;noah_reprototype_version=1;kind='noah-only-reprototype-comparison';before='reconstructed rejected Noah renderer 1d11629 at identical clocks; candidate name visibility on Before only';baseline_source_revision='1d11629ae8bab0ec398c6e53bd0bfd5f3030c92c';after='current product code';source_revision=('a'*40);product_fingerprint=('b'*64);original_atlas_sha256=('c'*64);native_unity=$true;pixel_verified=$false;adopted=$false;navigation_changed=$false;teleport_rescue=$false;noah_pairs=$Pairs;noah_sorting_examples=$Examples;
        sequence=@{greet_trigger='public TryBeginConversation(noah,momo); automatic social timer held only in isolated fixture';noah_reaction_seconds=1;momo_reaction_seconds=.65;noah_facing=1;momo_facing=-1;kind='native-continuous-fixed-step-Noah-reprototype';setup='fresh-isolated-Home-scene-Noah-only';step_seconds=(1.0/30);sample_step_count=2;source_width=720;source_height=1280;crop_x=0;crop_y=0;crop_width=200;crop_height=200;isolated_start_verified=$true;manual_advance=$true;state_unchanged=$true;store_writes_unchanged=$true;actual_conversation_verified=$true;conversation_mode='speaker-noah-to-momo';conversation_speaker_id='noah';conversation_partner_id='momo';conversation_text=([regex]::Unescape('\u3053\u3093\u306b\u3061\u306f\u3001\u30e2\u30e2')+[char]0x266a);frames=$Frames}}
    function SaveProof($Value){$Value | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $Dir 'noah-reprototype.json') -Encoding UTF8}
    SaveProof $Valid;Assert-NoahReprototypeEvidence $Dir ('a'*40) ('b'*64) ('c'*64)
    $Original=$Valid | ConvertTo-Json -Depth 20
    foreach($Mutate in @(
        {$script:Bad.sequence.conversation_speaker_id='momo'},
        {$script:Bad.sequence.frames[120].elapsed_seconds+=1},
        {$script:Bad.noah_pairs[0].outside_allowed_pixels=1},
        {$script:Bad.noah_pairs[0].other_five_unchanged=$false},
        {$script:Bad.noah_pairs[0].name_control.after_natural=$false},
        {$script:Bad.sequence.frames[29].facing=1},
        {$script:Bad.original_atlas_sha256=('d'*64)},
        {$script:Bad.adopted=$true},
        {$script:Bad.sequence.frames[95].walking=$true},
        {$script:Bad.capture_clip_verified=$false},
        {$script:Bad.capture_canvas_distance=10},
        {$script:Bad.noah_sorting_examples[0].actor_sibling=1}
    )){$script:Bad=$Original | ConvertFrom-Json;& $Mutate;SaveProof $script:Bad;Reject {Assert-NoahReprototypeEvidence $Dir ('a'*40) ('b'*64) ('c'*64)}}
    Write-Host 'Noah reprototype helper: exact single method, bounded raw archive, unrelated work excluded; synthetic fixtures only.'
} finally {
    if(Test-Path -LiteralPath $Temporary){Remove-Item -LiteralPath $Temporary -Recurse -Force}
    foreach($Part in @(Get-ChildItem -Path ($Temporary+'-*.zip') -ErrorAction SilentlyContinue)){Remove-Item -LiteralPath $Part.FullName -Force}
}
