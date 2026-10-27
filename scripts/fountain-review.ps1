# Native rendered crops and timestamps; this gate does not claim Pixel acceptance.
function Assert-FountainLiveSequence([string]$Directory) {
    $Live=Join-Path $Directory 'fountain-live'
    if(-not(Test-Path -LiteralPath (Join-Path $Live 'fountain-live.html') -PathType Leaf)){throw 'Missing real-time fountain player.'}
    $Record=Get-Content -LiteralPath (Join-Path $Live 'fountain-live.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    function Read-LiveNumber($Value,[string]$Label) {
        if($null -eq $Value -or $Value -isnot [ValueType] -or $Value -is [bool]){throw "Missing numeric live field: $Label"}
        $Number=[double]$Value
        if([double]::IsNaN($Number) -or [double]::IsInfinity($Number)){throw "Nonfinite live field: $Label"}
        return $Number
    }
    if((Read-LiveNumber $Record.town_level 'town_level') -ne 0){throw 'The live show must also run in the actual initial town, not a seeded Lv6 fixture.'}
    if($Record.kind -ne 'native-realtime-update-sequence' -or $Record.cycle_seconds -ne 45 -or $Record.sample_period_seconds -ne .25 -or $Record.source_width -ne 720 -or $Record.source_height -ne 1280){throw 'Wrong fountain real-time evidence mode.'}
    foreach($Key in @('manual_advance','pixel_verified','adopted')){if($Record.$Key -isnot [bool] -or $Record.$Key){throw 'Real-time Unity evidence must not force time or claim Pixel/adoption.'}}
    if($Record.state_unchanged -isnot [bool] -or -not $Record.state_unchanged -or $Record.normal_mesh_changes -le 8 -or $Record.show_mesh_changes -le 3){throw 'Live water must actually animate its mesh and preserve saved state.'}
    $Duration=Read-LiveNumber $Record.seconds 'seconds';$FirstShow=Read-LiveNumber $Record.first_show_seconds 'first_show_seconds'
    if($Duration -lt 60 -or $Duration -gt 90 -or $FirstShow -lt 30 -or $FirstShow -gt 60){throw 'QA needs a real60-second sequence with a show inside30-60seconds.'}
    foreach($Key in @('crop_x','crop_y','crop_width','crop_height')){$Number=Read-LiveNumber $Record.$Key $Key;if($Number -ne [Math]::Floor($Number) -or $Number -lt 0){throw 'Invalid crop rectangle.'}}
    if($Record.crop_width -lt 64 -or $Record.crop_height -lt 64 -or $Record.crop_x+$Record.crop_width -gt 720 -or $Record.crop_y+$Record.crop_height -gt 1280){throw 'Fountain crop lies outside the actual portrait.'}
    $Frames=@($Record.frames);if($Frames.Count -le 60 -or $Frames.Count -gt 245){throw 'Missing continuous fountain samples.'}
    $Names=@{};$LastTime=-1.0;$StartClock=0.0;$StartPhase=0.0;$StartTime=0.0;$FirstPeak=$null;$SeenShow=$false;$Recovered=$false;$Stages=@();$NormalHashes=@();$ShowHashes=@()
    foreach($Frame in $Frames) {
        if($Frame.file -notmatch '^35-fountain-live-crop-\d{3}\.png$' -or $Names.ContainsKey([string]$Frame.file)){throw 'Unsafe or repeated live frame name.'};$Names[[string]$Frame.file]=$true
        $At=Read-LiveNumber $Frame.realtime_seconds 'realtime_seconds';$Clock=Read-LiveNumber $Frame.clock_seconds 'clock_seconds';$Phase=Read-LiveNumber $Frame.phase 'phase';$Stage=Read-LiveNumber $Frame.stage 'stage'
        if($At -lt 0 -or $At -le $LastTime -or ($LastTime -ge 0 -and $At-$LastTime -gt 2) -or $Clock -lt 0 -or $Clock -ge 45 -or $Phase -lt 0 -or $Phase -ge 10){throw 'Live timeline is frozen, discontinuous or invalid.'}
        if($LastTime -lt 0){if($At -gt 2 -or $Clock -gt 2){throw 'Real-time capture must begin before the first show.'};$StartClock=$Clock;$StartPhase=$Phase;$StartTime=$At}
        $ExpectedClock=($StartClock+$At-$StartTime)%45;$Drift=[Math]::Abs($Clock-$ExpectedClock)
        if([Math]::Min($Drift,45-$Drift) -gt .5){throw 'Live show clock lost real elapsed time.'}
        $ExpectedPhase=($StartPhase+$At-$StartTime)%10;$PhaseDrift=[Math]::Abs($Phase-$ExpectedPhase)
        if([Math]::Min($PhaseDrift,10-$PhaseDrift) -gt .5){throw 'Water animation phase is frozen or lost real time.'}
        $ExpectedStage=if($Clock -lt 36){0}elseif($Clock -lt 38){1}elseif($Clock -lt 44){2}else{3}
        if($Stage -ne $ExpectedStage){throw 'Live stage does not match the actual short-cycle clock.'}
        if($Stage -eq 2){
            $SeenShow=$true;$ShowHashes+=@($Frame.sha256)
            $Ramp=[Math]::Min(1.0,[Math]::Max(0.0,($Clock-38)/.75))
            if($null -eq $FirstPeak -and $Ramp*$Ramp*(3-2*$Ramp) -gt .99){$FirstPeak=$At}
        }
        if($Stage -eq 0){$NormalHashes+=@($Frame.sha256);if($SeenShow){$Recovered=$true}}
        $Stages+=@($Stage);$LastTime=$At
        $Path=Join-Path $Live $Frame.file
        if($Frame.sha256 -notmatch '^[a-f0-9]{64}$' -or -not(Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-Item -LiteralPath $Path).Length -ne $Frame.bytes -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Frame.sha256){throw 'Live rendered frame identity changed.'}
        $Stream=[IO.File]::OpenRead($Path)
        try{$Header=New-Object byte[] 24;$Read=$Stream.Read($Header,0,24)}finally{$Stream.Dispose()}
        if($Read -ne 24 -or ($Header[0..7] -join ',') -ne '137,80,78,71,13,10,26,10' -or [Text.Encoding]::ASCII.GetString($Header,12,4) -ne 'IHDR'){throw 'Invalid live PNG.'}
        $Width=([int64]$Header[16]*16777216)+([int64]$Header[17]*65536)+([int64]$Header[18]*256)+$Header[19]
        $Height=([int64]$Header[20]*16777216)+([int64]$Header[21]*65536)+([int64]$Header[22]*256)+$Header[23]
        if($Width -ne $Record.crop_width -or $Height -ne $Record.crop_height -or $Frame.width -ne $Width -or $Frame.height -ne $Height){throw 'Live crop dimensions changed.'}
    }
    if(-not $Recovered -or $null -eq $FirstPeak -or [Math]::Abs($FirstShow-$FirstPeak) -gt .01 -or @($Stages | Select-Object -Unique).Count -ne 4 -or [Math]::Abs($LastTime-$Duration) -gt .01 -or @($NormalHashes | Select-Object -Unique).Count -le 4 -or @($ShowHashes | Select-Object -Unique).Count -le 3){throw "Incomplete live sequence: recovered=$Recovered, reportedShow=$FirstShow, observedPeak=$FirstPeak, last=$LastTime, duration=$Duration."}
    Write-Host "Real Update sequence: $($Frames.Count) crops / $Duration seconds; first show $FirstShow seconds. Pixel/user acceptance pending."
}
