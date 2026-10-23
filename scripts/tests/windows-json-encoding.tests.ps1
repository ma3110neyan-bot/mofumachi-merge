$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# Load the production entry's exact strict mode and helper dependencies.
. (Join-Path $PSScriptRoot '../build-android.ps1')
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$StagePath=Join-Path $Repo 'References/VisualLock/stage-acceptance-2026-10-21.json'
$Before=(Get-FileHash -LiteralPath $StagePath -Algorithm SHA256).Hash

# Exercise the real prerequisite under Japanese Windows PowerShell's legacy
# default. Only the unavailable host default is simulated: disk bytes, BOM
# detection, JSON parser, hashes and all production approval gates are real.
# Removing the explicit UTF8 read must reproduce the reported parse failure.
$LegacyEncoding=[Text.Encoding]::GetEncoding(932)
function Get-Content {
    [CmdletBinding()]
    param([string]$LiteralPath,[switch]$Raw,[string]$Encoding)
    if($PSBoundParameters.ContainsKey('Encoding')) {
        Microsoft.PowerShell.Management\Get-Content @PSBoundParameters
        return
    }
    $Reader=[IO.StreamReader]::new($LiteralPath,$LegacyEncoding,$true)
    try {$Value=$Reader.ReadToEnd()} finally {$Reader.Dispose()}
    if($Raw){$Value}else{$Value -split '\r?\n'}
}
try {
    $Rejected=$false
    try {$null=(Get-Content -LiteralPath $StagePath -Raw) | ConvertFrom-Json -ErrorAction Stop} catch {$Rejected=$true}
    if(-not $Rejected){throw 'Fixture no longer reproduces the Japanese legacy-decoder JSON failure.'}

    # The current user-authorized South review must run through the actual
    # prerequisite without changing/escaping the approved Japanese record.
    Assert-SouthReviewPrerequisites $Repo
    Assert-FiveAreaReviewPrerequisites $Repo

    # Correct decoding is not permission to bypass the South or APK gates.
    foreach($Checkpoint in @('south_garden','five_area')) {
        $Rejected=$false
        try {Assert-TownCheckpointApproved $Repo $Checkpoint} catch {
            if($_.Exception.Message -notlike '*Town quality regressed or unreviewed: south_garden/density*'){throw}
            $Rejected=$true
        }
        if(-not $Rejected){throw "Unapproved checkpoint opened: $Checkpoint"}
    }
    if((Get-FileHash -LiteralPath $StagePath -Algorithm SHA256).Hash -ne $Before){throw 'Encoding fix rewrote the user approval record.'}
    # PS5.1 also decodes dot-sourced scripts with the system ANSI code page.
    # Test the actual reader/parser, not only the JSON readers: an UTF8 script
    # without BOM previously corrupted quoted Japanese text and failed parsing.
    $ScriptPath=Join-Path $Repo 'scripts/five-area-review.ps1'
    $Reader=[IO.StreamReader]::new($ScriptPath,$LegacyEncoding,$true)
    try{$Decoded=$Reader.ReadToEnd()}finally{$Reader.Dispose()}
    if($Decoded -ne [IO.File]::ReadAllText($ScriptPath,[Text.Encoding]::UTF8)){throw 'Legacy host corrupted review script strings.'}
    $Tokens=$null;$ParseErrors=$null;$null=[Management.Automation.Language.Parser]::ParseInput($Decoded,[ref]$Tokens,[ref]$ParseErrors)
    if(@($ParseErrors).Count -gt 0){throw 'Legacy host cannot parse the actual review helper.'}
    Write-Host 'Japanese Windows CP932 JSON failure reproduced; actual South/five-area review prerequisites and script decoding/parser pass; final quality/integration acceptance stays closed. No Unity/Pixel execution.'
} finally {
    Remove-Item Function:Get-Content
}
