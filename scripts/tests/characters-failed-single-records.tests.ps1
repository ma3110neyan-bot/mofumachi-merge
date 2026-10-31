$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../prepare-characters-pixel-preview.ps1')
if(-not(Get-Command Copy-CharactersFailedSingleRecords -ErrorAction SilentlyContinue)){throw 'Failed-single diagnostics packaging is absent.'}
function RejectFailedRecords([scriptblock]$Action){$Caught=$false;try{& $Action | Out-Null}catch{$Caught=$true};if(-not $Caught){throw 'Unsafe failed-single diagnostic input accepted.'}}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-failed-records-'+[guid]::NewGuid().ToString('N'))
$Diag=Join-Path $Root 'Builds/Diagnostics';$Single=Join-Path $Diag 'momo-authored-20261031-212944-eaacbdb6';$Parent=Join-Path $Diag 'characters-authored-20261031-212911-c48ee7f9'
New-Item -ItemType Directory -Path $Single,$Parent -Force | Out-Null
try{
    $Source=@{gitRevision=('a'*40);productFingerprint=('b'*64);native_test_passed=$false;error='synthetic failed native assertion'}
    $SourceFile=Join-Path $Single 'source-info.json';$Source | ConvertTo-Json | Set-Content -LiteralPath $SourceFile -Encoding UTF8
    [IO.File]::WriteAllText((Join-Path $Single 'playmode.xml'),'<test-run result="Failed"><test-case result="Failed" /></test-run>')
    [IO.File]::WriteAllText((Join-Path $Single 'playmode.log'),'actual diagnostic log bytes')
    [IO.File]::WriteAllText((Join-Path $Single 'prepare.log'),'prepare log')
    [IO.File]::WriteAllText((Join-Path $Single 'prepared.json'),'{}')
    [IO.File]::WriteAllText((Join-Path $Single 'not-a-diagnostic.txt'),'do not copy unrelated input')
    RejectFailedRecords {Copy-CharactersFailedSingleRecords $Root $Parent $Single ('c'*40) ('b'*64)}
    RejectFailedRecords {Copy-CharactersFailedSingleRecords $Root $Parent $Single ('a'*40) ('c'*64)}
    $Foreign=Join-Path $Root 'momo-authored-20261031-212944-eaacbdb6';New-Item -ItemType Directory -Path $Foreign | Out-Null
    RejectFailedRecords {Copy-CharactersFailedSingleRecords $Root $Parent $Foreign ('a'*40) ('b'*64)}
    $Link=Join-Path $Diag 'miel-authored-20261031-212944-eaacbdb6';New-Item -ItemType SymbolicLink -Path $Link -Target $Single | Out-Null
    RejectFailedRecords {Copy-CharactersFailedSingleRecords $Root $Parent $Link ('a'*40) ('b'*64)}
    $Target=Copy-CharactersFailedSingleRecords $Root $Parent $Single ('a'*40) ('b'*64)
    $Manifest=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Target 'failure-context.json') | ConvertFrom-Json
    if($Manifest.original_single_run -cne $Single -or $Manifest.files.Count -ne 5 -or $Manifest.passed -or $Manifest.adopted){throw 'Failed evidence was lost or mislabelled.'}
    foreach($Row in $Manifest.files){
        if((Get-FileHash -LiteralPath (Join-Path $Target $Row.file) -Algorithm SHA256).Hash -ine $Row.sha256 -or (Get-FileHash -LiteralPath (Join-Path $Single $Row.file) -Algorithm SHA256).Hash -ine $Row.sha256){throw 'Diagnostic bytes changed.'}
    }
    if(Test-Path -LiteralPath (Join-Path $Target 'not-a-diagnostic.txt')){throw 'Unrelated inputs copied.'}
    RejectFailedRecords {Copy-CharactersFailedSingleRecords $Root $Parent $Single ('a'*40) ('b'*64)}
    $Zip=@(Write-CharactersReviewArchives $Parent)[0]
    $Archive=[IO.Compression.ZipFile]::OpenRead($Zip)
    try{foreach($Name in @('failed-single/playmode.xml','failed-single/playmode.log','failed-single/source-info.json','failed-single/failure-context.json')){if($Name -cnotin @($Archive.Entries.FullName)){throw 'Parent review ZIP still lacks the actual single-character diagnostics.'}}}finally{$Archive.Dispose()}
    Remove-Item -LiteralPath $Zip
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Failed-single parent ZIP, unchanged originals, source/path/link/collision guards passed. Synthetic evidence only; no native rerun.'
