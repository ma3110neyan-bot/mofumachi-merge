$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../character-finish-scope.ps1')
if(-not(Get-Command Assert-CharactersVisualLock -ErrorAction SilentlyContinue)){throw 'Final six-character Visual Lock verifier missing.'}
function RejectLock([scriptblock]$Action){$Caught=$false;try{& $Action}catch{$Caught=$true};if(-not $Caught){throw 'Changed frozen character input accepted.'}}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-char-lock-'+[guid]::NewGuid().ToString('N'));$Folder=Join-Path $Root 'References/VisualLock';New-Item -ItemType Directory -Path $Folder -Force | Out-Null
try{
    $File=Join-Path $Root 'Assets/Mofumachi/Presentation/CharacterNeckAttachment.cs';New-Item -ItemType Directory -Path (Split-Path -Parent $File) -Force | Out-Null;[IO.File]::WriteAllText($File,"original`nsource`n")
    $Rows=@(@{path='Assets/Mofumachi/Presentation/CharacterNeckAttachment.cs';sha256=(Get-FinishTextHash $File);text_normalized=$true})
    $Path=Join-Path $Folder 'characters-25d-final-2026-11-01.json'
    $Record=@{kind='six-character-25d-final';formal_fixed=$true;adopted=$true;characters=@('fio','lulu','miel','momo','noah','pom');misalignments=0;unmet_characters=0;accepted_source_revision=('a'*40);frozen_files=$Rows;snapshot_sha256=(Get-CharactersLockSnapshotHash $Rows)}
    $Good=$Record | ConvertTo-Json -Depth 6;$Good | Set-Content -LiteralPath $Path -Encoding UTF8
    Assert-CharactersVisualLock $Root
    [IO.File]::WriteAllText($File,"original`r`nsource`r`n");Assert-CharactersVisualLock $Root
    [IO.File]::WriteAllText($File,"changed`nsource`n");RejectLock {Assert-CharactersVisualLock $Root}
    [IO.File]::WriteAllText($File,"original`nsource`n")
    foreach($Case in @('empty','duplicate','path','snapshot','flag','character')){
        $P=$Good | ConvertFrom-Json
        switch($Case){empty {$P.frozen_files=@()};duplicate {$P.frozen_files+=@($P.frozen_files[0])};path {$P.frozen_files[0].path='../outside'};snapshot {$P.snapshot_sha256=('d'*64)};flag {$P.formal_fixed=$false};character {$P.characters[0]='other'}}
        if($Case -in @('empty','duplicate','path')){$P.snapshot_sha256=Get-CharactersLockSnapshotHash @($P.frozen_files)}
        $P | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Path -Encoding UTF8;RejectLock {Assert-CharactersVisualLock $Root}
    }
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Final character snapshot: unchanged/LF/CRLF accepted; content/set/path/false-freeze mutations rejected. Synthetic guard tests only.'
