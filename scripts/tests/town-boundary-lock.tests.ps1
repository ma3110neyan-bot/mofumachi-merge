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
    foreach($Relative in $Paths){$Target=Join-Path $Root $Relative;New-Item -ItemType Directory -Path (Split-Path -Parent $Target) -Force | Out-Null;Copy-Item -LiteralPath (Join-Path $Repo $Relative) -Destination $Target}
    $RecordPath=Join-Path $Root $Stage.base_polish
    function SaveRecord {$Record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $RecordPath -Encoding UTF8}
    $Valid=Get-BasePolishPatch $Root
    if($Valid.Before.Count -ne 10 -or $Valid.After.Count -ne 10){throw 'Original5 + added4 + capture1 bindings were lost.'}
    $NewFile=Join-Path $Root $Record.new_files[0].path;$Saved=[IO.File]::ReadAllBytes($NewFile)
    [IO.File]::AppendAllText($NewFile,'unbound change');Reject {Get-BasePolishPatch $Root};[IO.File]::WriteAllBytes($NewFile,$Saved)
    $Record.scope_id='unknown';SaveRecord;Reject {Get-BasePolishPatch $Root};$Record.scope_id='town_boundary_final_2026_10_24'
    $OriginalPath=$Record.new_files[0].path;$Record.new_files[0].path='Assets/Mofumachi/Core/SaveService.cs';SaveRecord;Reject {Get-BasePolishPatch $Root};$Record.new_files[0].path=$OriginalPath
    $CapturePath=$Record.capture_inputs[0].path;$Record.capture_inputs[0].path='Assets/Mofumachi/Presentation/MergeGridLayout.cs';SaveRecord;Reject {Get-BasePolishPatch $Root};$Record.capture_inputs[0].path=$CapturePath
    $Record.new_files=@($Record.new_files[0]);SaveRecord;Reject {Get-BasePolishPatch $Root}
    Write-Host 'Boundary gate: original bindings retained; missing/mutated/new unrelated files and unknown scope rejected.'
} finally {if(Test-Path -LiteralPath $Root){Remove-Item -LiteralPath $Root -Recurse -Force}}
