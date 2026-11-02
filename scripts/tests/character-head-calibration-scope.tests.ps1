$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../character-finish-scope.ps1')
function RejectScope([scriptblock]$Action){$Caught=$false;try{& $Action | Out-Null}catch{$Caught=$true};if(-not $Caught){throw 'Out-of-scope calibration accepted.'}}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-calibration-scope-'+[guid]::NewGuid().ToString('N'));$Dir=Join-Path $Root 'References/VisualLock';New-Item -ItemType Directory -Path $Dir -Force | Out-Null
try{
    $Rows=@(foreach($Name in @('NoahAuthoredGraphic.cs','CharacterAuthoredGraphic.cs')){
        $Path='Assets/Mofumachi/Presentation/'+$Name;$File=Join-Path $Root $Path;New-Item -ItemType Directory -Path (Split-Path -Parent $File) -Force | Out-Null;[IO.File]::WriteAllText($File,'original');@{path=$Path;sha256=(Get-FinishTextHash $File);text_normalized=$true}
    })
    $Snapshot=Get-CharactersLockSnapshotHash $Rows
    @{kind='six-character-25d-final';formal_fixed=$true;adopted=$true;characters=@('fio','lulu','miel','momo','noah','pom');accepted_source_revision=('a'*40);misalignments=0;unmet_characters=0;frozen_files=$Rows;snapshot_sha256=$Snapshot} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Dir 'characters-25d-final-2026-11-01.json') -Encoding UTF8
    $Recovery=@(foreach($Row in $Rows){$File=Join-Path $Root $Row.path;[IO.File]::WriteAllText($File,'restored');@{path=$Row.path;before_sha256=$Row.sha256;sha256=(Get-FinishTextHash $File)}})
    $Previous=Join-Path $Dir 'characters-head-recovery-2026-11-01.json'
    @{kind='approved-noah-head-body-restoration';change_authorized=$true;formal_fixed=$false;adopted=$false;reference_revision='7c98804a78f07546fb8176b320904e8af44f6542';final_snapshot_sha256=$Snapshot;overrides=$Recovery} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Previous -Encoding UTF8
    $Overrides=@(foreach($Row in $Recovery){$File=Join-Path $Root $Row.path;[IO.File]::WriteAllText($File,'calibrated');@{path=$Row.path;before_sha256=$Row.sha256;sha256=(Get-FinishTextHash $File)}})
    $Additions=@(foreach($Path in @('Assets/Mofumachi/Presentation/CharacterHeadBodyCalibration.cs','Assets/Mofumachi/Presentation/CharacterHeadBodyCalibration.cs.meta','Tests/Fixtures/character-head-calibration-2026-11-02.json')){$File=Join-Path $Root $Path;New-Item -ItemType Directory -Path (Split-Path -Parent $File) -Force | Out-Null;[IO.File]::WriteAllText($File,'registered');@{path=$Path;sha256=(Get-FinishTextHash $File)}})
    $Path=Join-Path $Dir 'characters-head-calibration-2026-11-02.json'
    $Good=@{kind='individual-head-body-calibration';change_authorized=$true;formal_fixed=$false;adopted=$false;native_verified=$false;pixel_verified=$false;previous_recovery_sha256=(Get-FinishTextHash $Previous);overrides=$Overrides;additions=$Additions} | ConvertTo-Json -Depth 8
    $Good | Set-Content -LiteralPath $Path -Encoding UTF8;Assert-CharactersVisualLock $Root
    foreach($Case in @('art','motion','before','duplicate','fixed','native','add-art','missing-helper','fixture-drift','history')){
        $Bad=$Good | ConvertFrom-Json
        switch($Case){
            art {$Bad.overrides[0].path='References/Characters/original.png'}
            motion {$Bad.overrides[0].path='Assets/Mofumachi/Presentation/NoahAuthoredMotion.cs'}
            before {$Bad.overrides[0].before_sha256=('d'*64)}
            duplicate {$Bad.overrides[1]=$Bad.overrides[0]}
            fixed {$Bad.formal_fixed=$true}
            native {$Bad.native_verified=$true}
            add-art {$Bad.additions[0].path='References/Characters/new.png'}
            missing-helper {$Bad.additions=$Bad.additions[1..2]}
            fixture-drift {$Bad.additions[2].sha256=('e'*64)}
            history {$Bad.previous_recovery_sha256=('d'*64)}
        }
        $Bad | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Path -Encoding UTF8;RejectScope {Assert-CharactersVisualLock $Root}
    }
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Only two head renderer changes plus fixed individual calibration accepted; original art/motion/history remain protected. Synthetic scope checks only.'
