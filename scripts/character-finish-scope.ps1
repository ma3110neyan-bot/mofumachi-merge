function Get-FinishTextHash([string]$Path){
    $Hasher=[Security.Cryptography.SHA256]::Create()
    try{return ([BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($Path).Replace("`r`n","`n"))))).Replace('-','').ToLowerInvariant()}finally{$Hasher.Dispose()}
}
function Get-CharactersLockSnapshotHash($Rows){
    [string[]]$Lines=@(foreach($Row in @($Rows)){$Row.path+':'+$Row.sha256+':'+$(if($Row.text_normalized){'text'}else{'binary'})})
    [Array]::Sort($Lines,[StringComparer]::Ordinal);$Hasher=[Security.Cryptography.SHA256]::Create()
    try{return ([BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes(($Lines -join "`n"))))).Replace('-','').ToLowerInvariant()}finally{$Hasher.Dispose()}
}
function Assert-CharactersVisualLock([string]$Repository){
    $Path=Join-Path $Repository 'References/VisualLock/characters-25d-final-2026-11-01.json'
    # Older snapshots and isolated legacy guard fixtures predate formal adoption.
    if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){return}
    $Lock=Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
    if($Lock.kind -cne 'six-character-25d-final' -or $Lock.formal_fixed -isnot [bool] -or -not $Lock.formal_fixed -or $Lock.adopted -isnot [bool] -or -not $Lock.adopted -or $Lock.accepted_source_revision -cnotmatch '^[a-f0-9]{40}$' -or $Lock.misalignments -ne 0 -or $Lock.unmet_characters -ne 0){throw 'Invalid final character Visual Lock.'}
    $Ids=@('fio','lulu','miel','momo','noah','pom')
    if(@($Lock.characters).Count -ne 6 -or @($Lock.characters | Select-Object -Unique).Count -ne 6 -or @(Compare-Object $Ids @($Lock.characters)).Count){throw 'Final lock requires all six identities.'}
    $Rows=@($Lock.frozen_files)
    if(-not $Rows.Count -or $Lock.snapshot_sha256 -cnotmatch '^[a-f0-9]{64}$' -or (Get-CharactersLockSnapshotHash $Rows) -cne $Lock.snapshot_sha256){throw 'Final character snapshot list changed.'}
    $Seen=@{}
    foreach($Row in $Rows){
        if($Row.path -cnotmatch '^(Assets/Mofumachi/(Presentation|Editor|Resources)/|References/Characters/)' -or $Row.path -match '(^|/)\.\.?(/|$)|[:\\]' -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$' -or $Row.text_normalized -isnot [bool] -or $Seen.ContainsKey($Row.path)){throw 'Invalid final character snapshot path or digest.'};$Seen[$Row.path]=$true
        $File=Join-Path $Repository $Row.path
        if(-not(Test-Path -LiteralPath $File -PathType Leaf) -or ((Get-Item -LiteralPath $File -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw "Frozen character file missing or linked: $($Row.path)"}
        $Hash=if($Row.text_normalized){Get-FinishTextHash $File}else{(Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant()}
        if($Hash -cne $Row.sha256){throw "Final character Visual Lock changed: $($Row.path)"}
    }
}
function Read-CharacterFinishOverrides([string]$Repository,[ValidateSet('noah','legacy')][string]$Group){
    Assert-CharactersVisualLock $Repository
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
    $Allowed=@('Assets/Mofumachi/Editor/NoahAuthoredBuild.cs','Assets/Mofumachi/Presentation/NoahAuthoredMotion.cs','Assets/Mofumachi/Presentation/NoahAuthoredGraphic.cs','Assets/Mofumachi/Tests/PlayMode/ExperienceCaptureTests.NoahAuthored.cs','Assets/Mofumachi/Presentation/TownWorldWalkModel.cs','Assets/Mofumachi/Tests/PlayMode/TownHabitTests.cs',
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
