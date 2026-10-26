$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../build-android.ps1')
function Reject([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Expected fountain live evidence rejection.'}}
$Temporary=Join-Path ([IO.Path]::GetTempPath()) ('mofu-fountain-live-'+[guid]::NewGuid())
New-Item -ItemType Directory -Path $Temporary | Out-Null
try {
    $Live=Join-Path $Temporary 'fountain-live';New-Item -ItemType Directory -Path $Live | Out-Null
    $Frames=@(for($n=0;$n -le 120;$n++){
        $At=$n*.5;$Clock=$At%45;$Stage=if($Clock -lt 36){0}elseif($Clock -lt 38){1}elseif($Clock -lt 44){2}else{3}
        # Deliberately synthetic header-only metadata fixture, never native proof.
        $Bytes=[byte[]]@(137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,0,128,0,0,0,200,$n)
        $File=('35-fountain-live-crop-{0:D3}.png' -f $n);$Path=Join-Path $Live $File;[IO.File]::WriteAllBytes($Path,$Bytes)
        @{file=$File;bytes=$Bytes.Length;width=128;height=200;sha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant();realtime_seconds=$At;clock_seconds=$Clock;phase=$At%10;stage=$Stage}
    })
    $Record=@{kind='native-realtime-update-sequence';seconds=60;first_show_seconds=39;cycle_seconds=45;sample_period_seconds=.25;manual_advance=$false;pixel_verified=$false;adopted=$false;state_unchanged=$true;source_width=720;source_height=1280;crop_x=260;crop_y=620;crop_width=128;crop_height=200;normal_mesh_changes=90;show_mesh_changes=10;frames=$Frames}
    $Meta=Join-Path $Live 'fountain-live.json';function Save {$Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Meta -Encoding UTF8}
    Save;Set-Content -LiteralPath (Join-Path $Live 'fountain-live.html') '<!doctype html><title>Metadata fixture</title>'
    Assert-FountainLiveSequence $Temporary
    foreach($Key in @('manual_advance','pixel_verified','adopted')){$Record[$Key]=$true;Save;Reject {Assert-FountainLiveSequence $Temporary};$Record[$Key]=$false}
    $Record.first_show_seconds=61;Save;Reject {Assert-FountainLiveSequence $Temporary};$Record.first_show_seconds=39
    $Record.seconds=59;Save;Reject {Assert-FountainLiveSequence $Temporary};$Record.seconds=60
    $Record.normal_mesh_changes=0;Save;Reject {Assert-FountainLiveSequence $Temporary};$Record.normal_mesh_changes=90
    $Saved=$Record.frames[80].stage;$Record.frames[80].stage=0;Save;Reject {Assert-FountainLiveSequence $Temporary};$Record.frames[80].stage=$Saved
    $Record.frames[20].realtime_seconds=3;Save;Reject {Assert-FountainLiveSequence $Temporary};$Record.frames[20].realtime_seconds=10
    $Record.frames[20].phase=$null;Save;Reject {Assert-FountainLiveSequence $Temporary};$Record.frames[20].phase=0
    $Phases=@($Record.frames | ForEach-Object {$_.phase});foreach($Frame in $Record.frames){$Frame.phase=0};Save;Reject {Assert-FountainLiveSequence $Temporary}
    for($n=0;$n -lt $Phases.Count;$n++){$Record.frames[$n].phase=$Phases[$n]}
    $Record.first_show_seconds=60;Save;Reject {Assert-FountainLiveSequence $Temporary};$Record.first_show_seconds=39
    $Record.frames[0].file='../outside.png';Save;Reject {Assert-FountainLiveSequence $Temporary};$Record.frames[0].file='35-fountain-live-crop-000.png';Save
    $Path=Join-Path $Live $Record.frames[0].file;$SavedBytes=[IO.File]::ReadAllBytes($Path)
    [IO.File]::WriteAllText($Path,'tampered');Reject {Assert-FountainLiveSequence $Temporary};[IO.File]::WriteAllBytes($Path,$SavedBytes)
    Assert-FountainLiveSequence $Temporary
    Write-Host 'Fountain live gate passed: short actual sequence, timing, motion, SHA, dimensions; missing/tampered/forced/frozen evidence rejected.'
} finally {if(Test-Path -LiteralPath $Temporary){Remove-Item -LiteralPath $Temporary -Recurse -Force}}
