$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../character-finish-scope.ps1')
if(-not(Get-Command Read-CharacterHeadRecovery -ErrorAction SilentlyContinue)){throw 'Narrow head-recovery authorization missing; final53 still rejects the user-requested restoration.'}
function RejectRecovery([scriptblock]$Action){$Rejected=$false;try{& $Action | Out-Null}catch{$Rejected=$true};if(-not $Rejected){throw 'Unrelated/false head recovery accepted.'}}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-head-recover-'+[guid]::NewGuid().ToString('N'));$Dir=Join-Path $Root 'References/VisualLock';New-Item -ItemType Directory -Path $Dir -Force | Out-Null
try{
    $Rows=@(foreach($Name in @('NoahAuthoredGraphic.cs','CharacterAuthoredGraphic.cs')){
        $Relative='Assets/Mofumachi/Presentation/'+$Name;$File=Join-Path $Root $Relative;New-Item -ItemType Directory -Path (Split-Path -Parent $File) -Force | Out-Null
        [IO.File]::WriteAllText($File,'before');@{path=$Relative;sha256=(Get-FinishTextHash $File);text_normalized=$true}
    })
    $Snapshot=Get-CharactersLockSnapshotHash $Rows
    @{kind='six-character-25d-final';formal_fixed=$true;adopted=$true;characters=@('fio','lulu','miel','momo','noah','pom');accepted_source_revision=('a'*40);misalignments=0;unmet_characters=0;frozen_files=$Rows;snapshot_sha256=$Snapshot} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Dir 'characters-25d-final-2026-11-01.json') -Encoding UTF8
    $Overrides=@(foreach($Row in $Rows){$File=Join-Path $Root $Row.path;[IO.File]::WriteAllText($File,'restored');@{path=$Row.path;before_sha256=$Row.sha256;sha256=(Get-FinishTextHash $File)}})
    $Path=Join-Path $Dir 'characters-head-recovery-2026-11-01.json'
    $Good=@{kind='approved-noah-head-body-restoration';change_authorized=$true;formal_fixed=$false;adopted=$false;reference_revision='7c98804a78f07546fb8176b320904e8af44f6542';final_snapshot_sha256=$Snapshot;overrides=$Overrides} | ConvertTo-Json -Depth 8
    $Good | Set-Content -LiteralPath $Path -Encoding UTF8;Assert-CharactersVisualLock $Root
    foreach($Case in @('art','motion','snapshot','before','duplicate','adopted','fixed','reference','unauthorized')){
        $Bad=$Good | ConvertFrom-Json
        switch($Case){art {$Bad.overrides[0].path='References/Characters/original.png'};motion {$Bad.overrides[0].path='Assets/Mofumachi/Presentation/NoahAuthoredMotion.cs'};snapshot {$Bad.final_snapshot_sha256=('d'*64)};before {$Bad.overrides[0].before_sha256=('d'*64)};duplicate {$Bad.overrides[1]=$Bad.overrides[0]};adopted {$Bad.adopted=$true};fixed {$Bad.formal_fixed=$true};reference {$Bad.reference_revision=('d'*40)};unauthorized {$Bad.change_authorized=$false}}
        $Bad | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Path -Encoding UTF8;RejectRecovery {Assert-CharactersVisualLock $Root}
    }
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Only the exact two head-renderer restorations accepted; art/motion/history/false adoption rejected. Synthetic scope checks only.'
