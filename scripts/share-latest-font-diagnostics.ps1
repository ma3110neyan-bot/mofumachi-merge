#requires -Version 5.1
[CmdletBinding()]
param()

# Copy existing diagnostics only. No Unity, capture, archive or Git invocation.
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$MaxFileBytes = 4MB
$MaxTotalBytes = 16MB
$MaxFiles = 64

function Assert-UnlinkedPath([string]$Path) {
    $At = [IO.Path]::GetFullPath($Path)
    while ($At) {
        if (Test-Path -LiteralPath $At) {
            if ((Get-Item -LiteralPath $At -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Linked path is not supported; nothing will be written there: $At"
            }
        }
        $Parent = [IO.Directory]::GetParent($At)
        if ($null -eq $Parent) { break }
        $At = $Parent.FullName
    }
}

function Assert-UnlinkedTree([string]$Path) {
    Assert-UnlinkedPath $Path
    $Pending = New-Object 'System.Collections.Generic.Stack[string]'
    $Pending.Push($Path)
    while ($Pending.Count) {
        foreach ($Item in Get-ChildItem -LiteralPath $Pending.Pop() -Force) {
            if ($Item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Existing inbox contains a link; preserving it: $($Item.FullName)"
            }
            if ($Item.PSIsContainer) { $Pending.Push($Item.FullName) }
        }
    }
}

function Get-RunTextFiles([string]$Path, [Collections.Generic.List[string]]$Skipped) {
    $Pending = New-Object 'System.Collections.Generic.Stack[string]'
    $Pending.Push($Path)
    while ($Pending.Count) {
        foreach ($Item in Get-ChildItem -LiteralPath $Pending.Pop() -Force) {
            if ($Item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                $Skipped.Add(($Item.FullName.Substring($Path.Length + 1) + ' | linked path'))
                continue
            }
            if ($Item.PSIsContainer) {
                if ($Item.Name -notin @('Library','Temp','Build','Builds','bin','obj','.git','.utmp','screenshots','images')) {
                    $Pending.Push($Item.FullName)
                }
            } elseif ($Item.Extension.ToLowerInvariant() -in @('.log','.xml','.json','.txt','.stdout','.stderr','.out','.err') -or $Item.Name -in @('stdout','stderr')) {
                $Item
            }
        }
    }
}

function Test-TextBytes([string]$Path) {
    $Bytes = [IO.File]::ReadAllBytes($Path)
    try {
        # Preserve UTF-16/32 console logs, UTF-8 and legacy Japanese bytes as-is.
        if ($Bytes.Length -ge 4 -and $Bytes[0] -eq 255 -and $Bytes[1] -eq 254 -and $Bytes[2] -eq 0 -and $Bytes[3] -eq 0) {
            $Text = [Text.UTF32Encoding]::new($false,$true,$true).GetString($Bytes,4,$Bytes.Length-4)
        } elseif ($Bytes.Length -ge 4 -and $Bytes[0] -eq 0 -and $Bytes[1] -eq 0 -and $Bytes[2] -eq 254 -and $Bytes[3] -eq 255) {
            $Text = [Text.UTF32Encoding]::new($true,$true,$true).GetString($Bytes,4,$Bytes.Length-4)
        } elseif ($Bytes.Length -ge 2 -and $Bytes[0] -eq 255 -and $Bytes[1] -eq 254) {
            $Text = [Text.UnicodeEncoding]::new($false,$true,$true).GetString($Bytes,2,$Bytes.Length-2)
        } elseif ($Bytes.Length -ge 2 -and $Bytes[0] -eq 254 -and $Bytes[1] -eq 255) {
            $Text = [Text.UnicodeEncoding]::new($true,$true,$true).GetString($Bytes,2,$Bytes.Length-2)
        } else {
            $Text = [Text.Encoding]::GetEncoding(28591).GetString($Bytes)
        }
        return -not [regex]::IsMatch($Text, '[\x00-\x08\x0B\x0E-\x1A\x1C-\x1F]')
    } catch { return $false }
}

$Repo = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$Diagnostics = Join-Path $Repo 'Builds/Diagnostics'
Assert-UnlinkedPath $Diagnostics
if (-not (Test-Path -LiteralPath $Diagnostics -PathType Container)) {
    throw "Diagnostics directory not found: $Diagnostics. No capture will be started."
}
$Runs = @(Get-ChildItem -LiteralPath $Diagnostics -Directory -Filter 'font-only-*' -Force |
    Sort-Object -Property @{Expression='LastWriteTimeUtc';Descending=$true},@{Expression='Name';Descending=$true})
if ($Runs.Count -eq 0) { throw "No font-only-* run found in $Diagnostics. Existing inbox is unchanged." }
$Run = $Runs[0]
Assert-UnlinkedPath $Run.FullName
$Skipped = New-Object 'System.Collections.Generic.List[string]'
$Files = @(Get-RunTextFiles $Run.FullName $Skipped | Sort-Object -Property @{Expression={
    if ($_.Name -in @('capture.log','capture.xml','font-only-source-info.json')) { 0 }
    elseif ($_.Name -match 'editor|stdout|stderr|compil|batchmode') { 1 }
    else { 2 }
}},FullName)
if ($Files.Count -eq 0) { throw 'Latest run has no eligible diagnostic text. Existing inbox is unchanged.' }

$Inbox = Join-Path $Repo 'References/Review/inbox'
$Latest = Join-Path $Inbox 'latest'
Assert-UnlinkedPath $Latest
if (Test-Path -LiteralPath $Latest) {
    if (-not (Test-Path -LiteralPath $Latest -PathType Container)) { throw 'Inbox latest is not a directory.' }
    Assert-UnlinkedTree $Latest
}
$null = [IO.Directory]::CreateDirectory($Inbox)
$Stage = Join-Path $Inbox ('.font-diagnostics-stage-' + [guid]::NewGuid().ToString('N'))
$Backup = Join-Path $Inbox ('.font-diagnostics-previous-' + [guid]::NewGuid().ToString('N'))
$Copied = New-Object 'System.Collections.Generic.List[object]'
$TotalBytes = 0L
try {
    $null = [IO.Directory]::CreateDirectory($Stage)
    foreach ($File in $Files) {
        $Relative = $File.FullName.Substring($Run.FullName.Length + 1)
        $Reason = $null
        if ($Relative -ieq 'run-info.txt') { $Reason = 'reserved provenance filename' }
        elseif ($File.Length -gt $MaxFileBytes) { $Reason = 'over per-file size limit' }
        elseif ($Copied.Count -ge $MaxFiles) { $Reason = 'over file-count limit' }
        elseif ($TotalBytes + $File.Length -gt $MaxTotalBytes) { $Reason = 'over total size limit' }
        elseif (-not (Test-TextBytes $File.FullName)) { $Reason = 'binary or invalid Unicode text' }
        if ($Reason) { $Skipped.Add(($Relative + ' | ' + $Reason)); continue }
        Assert-UnlinkedPath $File.FullName
        $SourceHash = (Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $Target = Join-Path $Stage $Relative
        $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $Target))
        [IO.File]::Copy($File.FullName,$Target,$false)
        $Bytes = (Get-Item -LiteralPath $Target -Force).Length
        if ($Bytes -ne $File.Length -or (Get-FileHash -LiteralPath $Target -Algorithm SHA256).Hash -ine $SourceHash) {
            throw "Source changed while copying; existing inbox preserved: $Relative"
        }
        $Copied.Add([pscustomobject]@{Relative=$Relative;Source=$File.FullName;Bytes=$Bytes;Sha256=$SourceHash})
        $TotalBytes += $Bytes
    }
    if ($Copied.Count -eq 0) { throw 'No small diagnostic text was copied. Existing inbox is unchanged.' }
    foreach ($File in $Copied) {
        Assert-UnlinkedPath $File.Source
        if ((Get-FileHash -LiteralPath $File.Source -Algorithm SHA256).Hash -ine $File.Sha256) {
            throw "Source changed before publication; existing inbox preserved: $($File.Relative)"
        }
    }
    $Info = New-Object 'System.Collections.Generic.List[string]'
    $Info.Add('Source run folder: ' + $Run.Name)
    $Info.Add('Source Windows path: ' + $Run.FullName)
    $Info.Add('Source folder modified UTC: ' + $Run.LastWriteTimeUtc.ToString('o'))
    $Info.Add('Copied UTC: ' + [DateTime]::UtcNow.ToString('o'))
    $Info.Add('Limits: 4 MiB per file; 16 MiB total; 64 files. No truncation.')
    $Info.Add('Copied files (relative path | bytes | SHA256):')
    foreach ($File in $Copied) { $Info.Add(('{0} | {1} | {2}' -f $File.Relative,$File.Bytes,$File.Sha256)) }
    $Info.Add('Skipped eligible paths (if any):')
    foreach ($Line in $Skipped) { $Info.Add($Line) }
    $Info.Add('Local copy only. Commit and push this inbox separately to publish it to GitHub.')
    [IO.File]::WriteAllLines((Join-Path $Stage 'run-info.txt'),$Info.ToArray(),[Text.UTF8Encoding]::new($true))
    Assert-UnlinkedPath $Latest
    if (Test-Path -LiteralPath $Latest) {
        Assert-UnlinkedTree $Latest
        [IO.Directory]::Move($Latest,$Backup)
    }
    try { [IO.Directory]::Move($Stage,$Latest) }
    catch {
        if ((Test-Path -LiteralPath $Backup) -and -not (Test-Path -LiteralPath $Latest)) { [IO.Directory]::Move($Backup,$Latest) }
        throw
    }
    if (Test-Path -LiteralPath $Backup) {
        try { [IO.Directory]::Delete($Backup,$true) }
        catch { Write-Warning "New inbox is ready; old inbox retained at $Backup" }
    }
} finally {
    if (Test-Path -LiteralPath $Stage) { [IO.Directory]::Delete($Stage,$true) }
}
Write-Host ("Copied {0} diagnostic text files ({1} bytes) from {2} to {3}" -f $Copied.Count,$TotalBytes,$Run.Name,$Latest)
if ($Skipped.Count) { Write-Warning 'Some paths were skipped; see run-info.txt for reasons.' }
Write-Host 'Copy only. Commit and push References/Review/inbox/latest separately to publish.'
