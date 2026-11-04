# Exact typography amendment; historical UX, Merge layout and character locks remain intact.
function Read-MergeFontTypographyOverride([string]$Repository,[string]$Path,[string]$ParentHash){
    $File=Join-Path $Repository 'References/VisualLock/merge-font-visual-2026-11-04.json'
    if(-not(Test-Path -LiteralPath $File -PathType Leaf)){return $ParentHash}
    $C=Get-Content -Raw -Encoding UTF8 -LiteralPath $File|ConvertFrom-Json
    if($C.kind -cne 'merge-font-visual-candidate' -or $C.change_authorized -isnot [bool] -or -not $C.change_authorized -or $C.baseline_revision -cne 'c84e5d9fa1c7d6aaf1305940794250509fa35f8f'){throw 'Invalid Merge font authorization.'}
    foreach($Flag in @('formal_fixed','adopted','native_verified','pixel_verified')){if($C.$Flag -isnot [bool] -or $C.$Flag){throw 'Font candidate cannot claim acceptance.'}}
    if($C.parent_manifest_sha256 -cne (Get-FinishTextHash (Join-Path $Repository 'References/VisualLock/release-ux-2026-11-03.json'))){throw 'Preserve the parent UX manifest.'}
    $Rows=@($C.overrides)
    if($Rows.Count -ne 1 -or $Rows[0].path -cne 'Assets/Mofumachi/Presentation/ReleaseTypography.cs' -or $Rows[0].before_sha256 -cne '195096be22fa1df8aba57a21674cafaed1c320ba1314da39b58deb977f93d941' -or $Rows[0].sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Only the exact typography entry point may change.'}
    $Added=@(
        'Assets/Mofumachi/Presentation/MergeFontProfile.cs'
        'Assets/Mofumachi/Presentation/MergeFontProfile.cs.meta'
        'Assets/Mofumachi/Presentation/MergeTypography.cs'
        'Assets/Mofumachi/Presentation/MergeTypography.cs.meta'
        'Assets/Mofumachi/Tests/PlayMode/MergeFontComparisonCapture.cs'
        'Assets/Mofumachi/Tests/PlayMode/MergeFontComparisonCapture.cs.meta'
    );$Seen=@{}
    if(@($C.additions).Count -ne $Added.Count){throw 'Exact font helper additions required.'}
    foreach($Row in @($C.additions)){
        if($Row.path -cnotin $Added -or $Seen.ContainsKey($Row.path) -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Unrelated font addition refused.'};$Seen[$Row.path]=$true
        $Actual=Join-Path $Repository $Row.path
        if(-not(Test-Path -LiteralPath $Actual -PathType Leaf) -or ((Get-Item -LiteralPath $Actual -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Get-FinishTextHash $Actual) -cne $Row.sha256){throw 'Font helper missing/changed.'}
    }
    $Actual=Join-Path $Repository $Rows[0].path
    if(-not(Test-Path -LiteralPath $Actual -PathType Leaf) -or ((Get-Item -LiteralPath $Actual -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Get-FinishTextHash $Actual) -cne $Rows[0].sha256){throw 'Font entry point changed.'}
    if($Path -ceq $Rows[0].path){if($ParentHash -cne $Rows[0].before_sha256){throw 'Typography amendment parent differs.'};return $Rows[0].sha256}
    return $ParentHash
}
