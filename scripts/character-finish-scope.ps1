function Get-FinishTextHash([string]$Path){
    $Hasher=[Security.Cryptography.SHA256]::Create()
    try{return ([BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($Path).Replace("`r`n","`n"))))).Replace('-','').ToLowerInvariant()}finally{$Hasher.Dispose()}
}
function Read-CharacterFinishOverrides([string]$Repository,[ValidateSet('noah','legacy')][string]$Group){
    $Folder=Join-Path $Repository 'References/VisualLock';$Path=Join-Path $Folder 'character-finish-amendment-2026-11-01.json'
    $Result=@{};if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){return $Result}
    $Change=Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
    if($Change.change_authorized -isnot [bool] -or -not $Change.change_authorized -or $Change.formal_fixed -isnot [bool] -or $Change.formal_fixed -or $Change.adopted -isnot [bool] -or $Change.adopted){throw 'Final-finish authorization must not claim formal acceptance.'}
    $Names=@{noah='noah-authored-approved-2026-10-30.json';legacy='noah-authored-game-candidate-2026-10-30.json'};$Records=@{}
    foreach($Key in @('noah','legacy')){
        $Historical=Join-Path $Folder $Names[$Key]
        if((Get-FinishTextHash $Historical) -cne $Change.historical_manifest_sha256.$Key){throw 'Historical lock manifest changed; amendment refused.'}
        $Record=Get-Content -Raw -Encoding UTF8 -LiteralPath $Historical | ConvertFrom-Json
        $Records[$Key]=if($Key -eq 'noah'){@($Record.frozen_files)}else{@($Record.protected_files)}
    }
    # Latest user request authorises implementation corrections, never source art.
    $Allowed=@('Assets/Mofumachi/Presentation/NoahAuthoredMotion.cs','Assets/Mofumachi/Presentation/NoahAuthoredGraphic.cs','Assets/Mofumachi/Tests/PlayMode/ExperienceCaptureTests.NoahAuthored.cs','Assets/Mofumachi/Presentation/TownWorldWalkModel.cs','Assets/Mofumachi/Tests/PlayMode/TownHabitTests.cs',
        'Assets/Mofumachi/Tests/PlayMode/TownLifeTests.cs','Assets/Mofumachi/Tests/PlayMode/TownMotionPresentationTests.cs','Assets/Mofumachi/Tests/PlayMode/TownRegionLifeTests.cs','Assets/Mofumachi/Tests/PlayMode/VisualLockPresentationTests.cs')
    $Seen=@{}
    foreach($Row in @($Change.overrides)){
        if($Row.group -cnotin @('noah','legacy') -or $Row.path -cnotin $Allowed -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Art or unrelated path cannot be amended.'}
        $Identity=$Row.group+':'+$Row.path;if($Seen.ContainsKey($Identity)){throw 'Duplicate final-finish amendment.'};$Seen[$Identity]=$true
        $Old=@($Records[$Row.group] | Where-Object {$_.path -ceq $Row.path})
        if($Old.Count -ne 1 -or $Old[0].sha256 -cne $Row.before_sha256){throw 'Amendment does not name the exact historical source hash.'}
        if($Row.group -ceq $Group){$Result[$Row.path]=$Row.sha256}
    }
    return $Result
}
