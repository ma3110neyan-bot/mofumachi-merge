$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../visual-lock.ps1')
. (Join-Path $PSScriptRoot '../west-review.ps1')
. (Join-Path $PSScriptRoot '../east-review.ps1')
. (Join-Path $PSScriptRoot '../north-review.ps1')
. (Join-Path $PSScriptRoot '../south-review.ps1')
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

    # Correct decoding is not permission to bypass the South or APK gates.
    foreach($Checkpoint in @('south_garden','five_area')) {
        $Rejected=$false
        try {Assert-TownCheckpointApproved $Repo $Checkpoint} catch {
            if($_.Exception.Message -notlike '*explicit user image approval missing for south_garden*'){throw}
            $Rejected=$true
        }
        if(-not $Rejected){throw "Unapproved checkpoint opened: $Checkpoint"}
    }
    if((Get-FileHash -LiteralPath $StagePath -Algorithm SHA256).Hash -ne $Before){throw 'Encoding fix rewrote the user approval record.'}
    Write-Host 'Japanese Windows CP932 failure reproduced; actual South prerequisites pass with UTF8 reads; South/integration approval stays closed. No Unity/Pixel execution.'
} finally {
    Remove-Item Function:Get-Content
}
