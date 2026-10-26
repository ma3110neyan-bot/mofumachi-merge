$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../visual-lock.ps1')
function Reject([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Unbound boundary input was accepted.'}}
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
# The old gate must first reject these newly authorized boundary-only changes.
Assert-BaseStagesReadyForAcceptance $Repo
$Stage=Get-Content -LiteralPath (Join-Path $Repo 'References/VisualLock/stage-acceptance-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$Record=Get-Content -LiteralPath (Join-Path $Repo $Stage.base_polish) -Raw -Encoding UTF8 | ConvertFrom-Json
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-boundary-lock-'+[guid]::NewGuid())
try {
    $Paths=@('References/VisualLock/stage-acceptance-2026-10-21.json',$Stage.base_polish,$Record.base_lock)+@($Record.changes | ForEach-Object {$_.path})+@($Record.new_files | ForEach-Object {$_.path})+@($Record.capture_inputs | ForEach-Object {$_.path})
    if($Record.scope_id -in @('town_fountain_water_2026_10_25','town_fountain_show_2026_10_26','town_fountain_motion_2026_10_26')){$Paths+=@('References/VisualLock/town-field-frozen-2026-10-25.json','References/VisualLock/town-field-final-candidate-2026-10-25.json')}
    if($Record.scope_id -in @('town_fountain_show_2026_10_26','town_fountain_motion_2026_10_26')){$Paths+=$Record.current_before_record}
    foreach($Relative in $Paths){$Target=Join-Path $Root $Relative;New-Item -ItemType Directory -Path (Split-Path -Parent $Target) -Force | Out-Null;Copy-Item -LiteralPath (Join-Path $Repo $Relative) -Destination $Target}
    $RecordPath=Join-Path $Root $Stage.base_polish
    function SaveRecord {$Record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $RecordPath -Encoding UTF8}
    $Valid=Get-BasePolishPatch $Root
    $ExpectedBindings=@($Record.changes).Count+$(if($Record.scope_id -in @('town_fountain_water_2026_10_25','town_fountain_show_2026_10_26','town_fountain_motion_2026_10_26')){9}else{5})
    if($Valid.Before.Count -ne $ExpectedBindings -or $Valid.After.Count -ne $ExpectedBindings){throw 'Original replacements + exact added/capture bindings were lost.'}
    $NewFile=Join-Path $Root $Record.new_files[0].path;$Saved=[IO.File]::ReadAllBytes($NewFile)
    [IO.File]::AppendAllText($NewFile,'unbound change');Reject {Get-BasePolishPatch $Root};[IO.File]::WriteAllBytes($NewFile,$Saved)
    $OriginalScope=$Record.scope_id;$Record.scope_id='unknown';SaveRecord;Reject {Get-BasePolishPatch $Root};$Record.scope_id=$OriginalScope
    if($OriginalScope -in @('town_field_stability_2026_10_25','town_field_completion_2026_10_25','town_fountain_water_2026_10_25','town_fountain_show_2026_10_26','town_fountain_motion_2026_10_26')) {
        $Record.scope_id='town_boundary_final_2026_10_24';SaveRecord;Reject {Get-BasePolishPatch $Root};$Record.scope_id=$OriginalScope;SaveRecord
        $Nav=Join-Path $Root 'Assets/Mofumachi/Presentation/TownAreaNavigation.cs';$NavBytes=[IO.File]::ReadAllBytes($Nav)
        [IO.File]::AppendAllText($Nav,'unbound navigation');Reject {Get-BasePolishPatch $Root};[IO.File]::WriteAllBytes($Nav,$NavBytes)
    }
    if($OriginalScope -in @('town_field_completion_2026_10_25','town_fountain_water_2026_10_25','town_fountain_show_2026_10_26','town_fountain_motion_2026_10_26')) {
        $Record.scope_id='town_field_stability_2026_10_25';SaveRecord;Reject {Get-BasePolishPatch $Root};$Record.scope_id=$OriginalScope;SaveRecord
        foreach($Name in @('TownGrowthPresentation.cs','TownGrowthComparisonView.cs')) {
            $Path=Join-Path $Root ('Assets/Mofumachi/Presentation/'+$Name);$Bytes=[IO.File]::ReadAllBytes($Path)
            [IO.File]::AppendAllText($Path,'unbound growth display');Reject {Get-BasePolishPatch $Root};[IO.File]::WriteAllBytes($Path,$Bytes)
        }
    }
    if($OriginalScope -in @('town_fountain_water_2026_10_25','town_fountain_show_2026_10_26','town_fountain_motion_2026_10_26')) {
        $Record.scope_id='town_field_completion_2026_10_25';SaveRecord;Reject {Get-BasePolishPatch $Root};$Record.scope_id=$OriginalScope;SaveRecord
        foreach($Name in @('TownFountainWaterLayout.cs','TownFountainWaterGraphic.cs')) {
            $Path=Join-Path $Root ('Assets/Mofumachi/Presentation/'+$Name);$Bytes=[IO.File]::ReadAllBytes($Path)
            [IO.File]::AppendAllText($Path,'unbound fountain input');Reject {Get-BasePolishPatch $Root};[IO.File]::WriteAllBytes($Path,$Bytes)
        }
        # Even a refreshed candidate hash cannot authorize a change to the
        # scenery/navigation/growth frozen by the later user instruction.
        foreach($Name in @('TownAreaNavigation.cs','TownGrowthPresentation.cs')) {
            $Relative='Assets/Mofumachi/Presentation/'+$Name;$Path=Join-Path $Root $Relative;$Bytes=[IO.File]::ReadAllBytes($Path)
            $Entry=$Record.changes | Where-Object {$_.path -eq $Relative};$SavedHash=$Entry.after_sha256
            [IO.File]::AppendAllText($Path,'rebound but forbidden field change');$Entry.after_sha256=Get-VisualLockDigest ([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($Path).Replace("`r`n","`n")));SaveRecord
            Reject {Get-BasePolishPatch $Root};[IO.File]::WriteAllBytes($Path,$Bytes);$Entry.after_sha256=$SavedHash;SaveRecord
        }
        foreach($Relative in @('References/VisualLock/town-field-frozen-2026-10-25.json','References/VisualLock/town-field-final-candidate-2026-10-25.json')) {
            $Path=Join-Path $Root $Relative;$Bytes=[IO.File]::ReadAllBytes($Path);[IO.File]::AppendAllText($Path,' ');Reject {Get-BasePolishPatch $Root};[IO.File]::WriteAllBytes($Path,$Bytes)
        }
    }
    if($OriginalScope -in @('town_fountain_show_2026_10_26','town_fountain_motion_2026_10_26')){$Record.scope_id='town_fountain_water_2026_10_25';SaveRecord;Reject {Get-BasePolishPatch $Root};$Record.scope_id=$OriginalScope;SaveRecord}
    $OriginalPath=$Record.new_files[0].path;$Record.new_files[0].path='Assets/Mofumachi/Core/SaveService.cs';SaveRecord;Reject {Get-BasePolishPatch $Root};$Record.new_files[0].path=$OriginalPath
    $CapturePath=$Record.capture_inputs[0].path;$Record.capture_inputs[0].path='Assets/Mofumachi/Presentation/MergeGridLayout.cs';SaveRecord;Reject {Get-BasePolishPatch $Root};$Record.capture_inputs[0].path=$CapturePath
    $Record.new_files=@($Record.new_files[0]);SaveRecord;Reject {Get-BasePolishPatch $Root}
    Write-Host 'Boundary gate: original bindings retained; missing/mutated/new unrelated files and unknown scope rejected.'
} finally {if(Test-Path -LiteralPath $Root){Remove-Item -LiteralPath $Root -Recurse -Force}}
