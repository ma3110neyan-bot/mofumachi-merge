$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../diagnose-noah-capture.ps1')
function Reject([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Invalid diagnostic arguments accepted.'}}
$Temporary=Join-Path ([IO.Path]::GetTempPath()) ('mofu-noah-diagnostic-'+[guid]::NewGuid())
try {
    $RepositoryWithSpaces=Join-Path $Temporary 'project with spaces'
    $DirectoryWithSpaces=Join-Path $Temporary 'diagnostic with spaces'
    $Arguments=@(Get-NoahDiagnosticArguments $RepositoryWithSpaces $DirectoryWithSpaces ('a'*40) ('b'*64))
    $Filter=[Array]::IndexOf($Arguments,'-testFilter')
    if($Filter -lt 0 -or $Arguments[$Filter+1] -ne 'Mofumachi.Tests.ExperienceCaptureTests.NorthNoahBeforeAfterAndFixedStepEvidencePreserveSavedState'){throw 'The diagnostic would rerun unrelated native tests.'}
    if(@($Arguments | Where-Object {$_ -eq '-runTests'}).Count -ne 1 -or '-quit' -in $Arguments){throw 'Diagnostic must complete the one asynchronous Unity test job.'}
    if($Arguments[[Array]::IndexOf($Arguments,'-buildTarget')+1] -ne 'Android'){throw 'Diagnostic changed the original Android test configuration.'}
    if($Arguments[[Array]::IndexOf($Arguments,'-projectPath')+1] -ne $RepositoryWithSpaces){throw 'Argument-array path boundary lost.'}
    Reject {Get-NoahDiagnosticArguments 'repo' 'output' 'short' ('b'*64)}
    Reject {Get-NoahDiagnosticArguments 'repo' 'output' ('a'*40) 'missing'}
    $Failed=Join-Path $Temporary 'screenshots/failed-pairs'
    New-Item -ItemType Directory -Path $Failed -Force | Out-Null
    $Bytes=[byte[]]@(0,255,13,10,9,42)
    [IO.File]::WriteAllBytes((Join-Path $Failed '37-noah-turn-FAILED-before.png'),$Bytes)
    [IO.File]::WriteAllText((Join-Path $Failed '37-noah-turn-FAILED.json'),'{"synthetic":true,"passed":false}')
    [IO.File]::WriteAllText((Join-Path $Temporary 'source-info.json'),'{"synthetic":true,"scope":"diagnostic-only"}')
    [IO.File]::WriteAllText((Join-Path $Temporary 'playmode.xml'),'<synthetic/>')
    [IO.File]::WriteAllText((Join-Path $Temporary 'playmode.log'),'synthetic log')
    [IO.File]::WriteAllText((Join-Path $Temporary 'screenshots/36-north-unrelated.png'),'must not include unrelated review bulk')
    $Zip=Write-NoahDiagnosticArchive $Temporary
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $Archive=[IO.Compression.ZipFile]::OpenRead($Zip)
    try {
        $Names=@($Archive.Entries.FullName)
        foreach($Name in @('source-info.json','playmode.xml','playmode.log','37-noah-turn-FAILED-before.png','37-noah-turn-FAILED.json')){if($Name -notin $Names){throw "Lost diagnostic evidence: $Name"}}
        if('36-north-unrelated.png' -in $Names){throw 'Diagnostic archive included unrelated North captures.'}
        $Entry=$Archive.GetEntry('37-noah-turn-FAILED-before.png');$Stream=$Entry.Open();$Memory=New-Object IO.MemoryStream
        try{$Stream.CopyTo($Memory);$Actual=$Memory.ToArray();if(($Actual -join ',') -ne ($Bytes -join ',')){throw 'Raw rejected image bytes changed.'}}finally{$Memory.Dispose();$Stream.Dispose()}
    } finally {$Archive.Dispose()}
    # Retain complete Noah originals/sequence on success too, in bounded parts.
    $Frames=Join-Path $Temporary 'screenshots/noah-sequence';New-Item -ItemType Directory -Path $Frames -Force | Out-Null
    $Evidence=@('screenshots/37-noah-talk-ui-natural-before.png','screenshots/37-noah-talk-after.png','screenshots/noah-before-after.html','screenshots/noah-sequence/37-noah-frame-00.png')
    foreach($Name in $Evidence){[IO.File]::WriteAllBytes((Join-Path $Temporary $Name),(New-Object byte[] 800))}
    $Parts=@(Write-NoahDiagnosticArchive $Temporary -MaxPartBytes 1500)
    if($Parts.Count -lt 2){throw 'Bounded diagnostic parts were not produced.'}
    $AllNames=@()
    foreach($Part in $Parts){$ZipPart=[IO.Compression.ZipFile]::OpenRead($Part);try{$AllNames+=@($ZipPart.Entries.FullName);if(($ZipPart.Entries | Measure-Object -Property Length -Sum).Sum -gt 1500){throw 'Uncompressed part budget exceeded.'}}finally{$ZipPart.Dispose()}}
    foreach($Name in $Evidence){if(@($AllNames | Where-Object {$_ -eq [IO.Path]::GetFileName($Name)}).Count -ne 1){throw "Noah original/sequence missing or duplicated: $Name"}}
    Write-Host 'Noah diagnostic: one exact native method, safe argument boundaries, original rejected bytes retained, unrelated captures excluded. Synthetic fixture only.'
} finally {
    if(Test-Path -LiteralPath $Temporary){Remove-Item -LiteralPath $Temporary -Recurse -Force}
    if(Test-Path -LiteralPath ($Temporary+'.zip')){Remove-Item -LiteralPath ($Temporary+'.zip') -Force}
    foreach($Part in @(Get-ChildItem -Path ($Temporary+'-*.zip') -ErrorAction SilentlyContinue)){Remove-Item -LiteralPath $Part.FullName -Force}
}
