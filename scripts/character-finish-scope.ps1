function Get-FinishTextHash([string]$Path){
    $Hasher=[Security.Cryptography.SHA256]::Create()
    try{return ([BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($Path).Replace("`r`n","`n"))))).Replace('-','').ToLowerInvariant()}finally{$Hasher.Dispose()}
}
function Get-CharactersLockSnapshotHash($Rows){
    [string[]]$Lines=@(foreach($Row in @($Rows)){$Row.path+':'+$Row.sha256+':'+$(if($Row.text_normalized){'text'}else{'binary'})})
    [Array]::Sort($Lines,[StringComparer]::Ordinal);$Hasher=[Security.Cryptography.SHA256]::Create()
    try{return ([BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes(($Lines -join "`n"))))).Replace('-','').ToLowerInvariant()}finally{$Hasher.Dispose()}
}
function Read-CharacterHeadCalibration([string]$Repository,$Previous){
    $Folder=Join-Path $Repository 'References/VisualLock';$Path=Join-Path $Folder 'characters-head-calibration-2026-11-02.json'
    if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){return $Previous}
    $C=Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
    if($C.kind -cne 'individual-head-body-calibration' -or $C.change_authorized -isnot [bool] -or -not $C.change_authorized){throw 'Invalid individual head calibration authorization.'}
    foreach($Flag in @('formal_fixed','adopted','native_verified','pixel_verified')){if($C.$Flag -isnot [bool] -or $C.$Flag){throw 'Pending calibration cannot claim formal/native acceptance.'}}
    if((Get-FinishTextHash (Join-Path $Folder 'characters-head-recovery-2026-11-01.json')) -cne $C.previous_recovery_sha256){throw 'Preserve the previous restoration record.'}
    $Allowed=@('Assets/Mofumachi/Presentation/NoahAuthoredGraphic.cs','Assets/Mofumachi/Presentation/CharacterAuthoredGraphic.cs');$Seen=@{}
    if(@($C.overrides).Count -ne 2){throw 'Head calibration only amends the two head renderers.'}
    foreach($Row in @($C.overrides)){
        if($Row.path -cnotin $Allowed -or $Seen.ContainsKey($Row.path) -or -not $Previous.ContainsKey($Row.path) -or $Row.before_sha256 -cne $Previous[$Row.path] -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Head calibration cannot unlock art, motion or unrelated paths.'}
        $Seen[$Row.path]=$true;$Previous[$Row.path]=$Row.sha256
    }
    $Added=@('Assets/Mofumachi/Presentation/CharacterHeadBodyCalibration.cs','Assets/Mofumachi/Presentation/CharacterHeadBodyCalibration.cs.meta','Tests/Fixtures/character-head-calibration-2026-11-02.json');$Seen=@{}
    if(@($C.additions).Count -ne 3){throw 'Exact common resolver, meta and individual reference required.'}
    foreach($Row in @($C.additions)){
        if($Row.path -cnotin $Added -or $Seen.ContainsKey($Row.path) -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Invalid head calibration addition.'};$Seen[$Row.path]=$true
        $File=Join-Path $Repository $Row.path
        if(-not(Test-Path -LiteralPath $File -PathType Leaf) -or ((Get-Item -LiteralPath $File -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Get-FinishTextHash $File) -cne $Row.sha256){throw "Individual head calibration changed: $($Row.path)"}
    }
    return $Previous
}
function Read-CharacterHeadRecovery([string]$Repository){
    $Folder=Join-Path $Repository 'References/VisualLock';$Path=Join-Path $Folder 'characters-head-recovery-2026-11-01.json';$Result=@{}
    if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){if(Test-Path -LiteralPath (Join-Path $Folder 'characters-head-calibration-2026-11-02.json')){throw 'Head calibration requires the previous recovery record.'};return $Result}
    $Change=Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
    if($Change.kind -cne 'approved-noah-head-body-restoration' -or $Change.reference_revision -cne '7c98804a78f07546fb8176b320904e8af44f6542' -or $Change.change_authorized -isnot [bool] -or -not $Change.change_authorized -or $Change.formal_fixed -isnot [bool] -or $Change.formal_fixed -or $Change.adopted -isnot [bool] -or $Change.adopted){throw 'Head restoration is authorized correction, not new formal acceptance.'}
    $Lock=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Folder 'characters-25d-final-2026-11-01.json') | ConvertFrom-Json
    if($Change.final_snapshot_sha256 -cne $Lock.snapshot_sha256 -or (Get-CharactersLockSnapshotHash @($Lock.frozen_files)) -cne $Lock.snapshot_sha256){throw 'Head recovery must retain the exact former53 snapshot.'}
    $Allowed=@('Assets/Mofumachi/Presentation/NoahAuthoredGraphic.cs','Assets/Mofumachi/Presentation/CharacterAuthoredGraphic.cs')
    if(@($Change.overrides).Count -ne 2){throw 'Only the two head renderers may be restored.'}
    foreach($Row in @($Change.overrides)){
        if($Row.path -cnotin $Allowed -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$' -or $Result.ContainsKey($Row.path)){throw 'Art, motion or unrelated code cannot be changed by head recovery.'}
        $Before=@($Lock.frozen_files | Where-Object {$_.path -ceq $Row.path})
        if($Before.Count -ne 1 -or $Row.before_sha256 -cne $Before[0].sha256 -or $Before[0].text_normalized -isnot [bool] -or -not $Before[0].text_normalized){throw 'Head recovery does not name the exact former renderer hash.'}
        $Result[$Row.path]=$Row.sha256
    }
    return Read-CharacterHeadCalibration $Repository $Result
}
function Assert-CharactersVisualLock([string]$Repository){
    $Path=Join-Path $Repository 'References/VisualLock/characters-25d-final-2026-11-01.json'
    # Older snapshots and isolated legacy guard fixtures predate formal adoption.
    if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){if(Test-Path -LiteralPath (Join-Path $Repository 'References/VisualLock/characters-head-recovery-2026-11-01.json')){throw 'Preserve the former character snapshot during head recovery.'};return}
    $Lock=Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
    if($Lock.kind -cne 'six-character-25d-final' -or $Lock.formal_fixed -isnot [bool] -or -not $Lock.formal_fixed -or $Lock.adopted -isnot [bool] -or -not $Lock.adopted -or $Lock.accepted_source_revision -cnotmatch '^[a-f0-9]{40}$' -or $Lock.misalignments -ne 0 -or $Lock.unmet_characters -ne 0){throw 'Invalid final character Visual Lock.'}
    $Ids=@('fio','lulu','miel','momo','noah','pom')
    if(@($Lock.characters).Count -ne 6 -or @($Lock.characters | Select-Object -Unique).Count -ne 6 -or @(Compare-Object $Ids @($Lock.characters)).Count){throw 'Final lock requires all six identities.'}
    $Rows=@($Lock.frozen_files)
    if(-not $Rows.Count -or $Lock.snapshot_sha256 -cnotmatch '^[a-f0-9]{64}$' -or (Get-CharactersLockSnapshotHash $Rows) -cne $Lock.snapshot_sha256){throw 'Final character snapshot list changed.'}
    $Recovery=Read-CharacterHeadRecovery $Repository;$Seen=@{}
    foreach($Row in $Rows){
        if($Row.path -cnotmatch '^(Assets/Mofumachi/(Presentation|Editor|Resources)/|References/Characters/)' -or $Row.path -match '(^|/)\.\.?(/|$)|[:\\]' -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$' -or $Row.text_normalized -isnot [bool] -or $Seen.ContainsKey($Row.path)){throw 'Invalid final character snapshot path or digest.'};$Seen[$Row.path]=$true
        $File=Join-Path $Repository $Row.path
        if(-not(Test-Path -LiteralPath $File -PathType Leaf) -or ((Get-Item -LiteralPath $File -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw "Frozen character file missing or linked: $($Row.path)"}
        $Hash=if($Row.text_normalized){Get-FinishTextHash $File}else{(Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant()}
        $Expected=if($Recovery.ContainsKey($Row.path)){$Recovery[$Row.path]}else{$Row.sha256}
        if($Hash -cne $Expected){throw "Final character Visual Lock changed: $($Row.path)"}
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
    $Recovery=Read-CharacterHeadRecovery $Repository
    foreach($Path in $Recovery.Keys){if(@($Records[$Group] | Where-Object {$_.path -ceq $Path}).Count -eq 1){$Result[$Path]=$Recovery[$Path]}}
    . (Join-Path $PSScriptRoot 'merge-reward-scope.ps1')
    $Merge=Read-MergeRewardOverrides $Repository
    if($Group -ceq 'legacy'){foreach($Path in $Merge.Keys){$Result[$Path]=$Merge[$Path]}}
    . (Join-Path $PSScriptRoot 'release-ux-scope.ps1')
    $Ux=Read-ReleaseUxOverrides $Repository
    if($Group -ceq 'legacy'){foreach($Path in $Ux.Keys){$Result[$Path]=$Ux[$Path]}}
    return $Result
}
