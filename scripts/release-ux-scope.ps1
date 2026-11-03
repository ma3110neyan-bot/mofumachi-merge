# Additive, exact UX permission. Original character/Merge manifests remain immutable.
function Read-ReleaseUxOverrides([string]$Repository){
    $Result=@{};$File=Join-Path $Repository 'References/VisualLock/release-ux-2026-11-03.json'
    if(-not(Test-Path -LiteralPath $File -PathType Leaf)){return $Result}
    $C=Get-Content -Raw -Encoding UTF8 -LiteralPath $File | ConvertFrom-Json
    if($C.kind -cne 'release-ux-font-and-flower-only' -or $C.change_authorized -isnot [bool] -or -not $C.change_authorized -or $C.baseline_revision -cne '035944bca6c007f14061d9326cb3e2cfba4f4ad6'){throw 'Invalid release UX authorization.'}
    foreach($Flag in @('formal_fixed','adopted','native_verified','pixel_verified')){if($C.$Flag -isnot [bool] -or $C.$Flag){throw 'UX candidate cannot claim native/device acceptance.'}}
    $Allowed=@{
        'Assets/Mofumachi/Presentation/UIFlowController.cs'='f9e24228a7ce20dadb800d4380820694442e4bf6539026a232e7ed49bae3a106'
        'Assets/Mofumachi/Presentation/UIWidgets.cs'='0edd18d32869f76ef94e3c4260ffd4e724c378db53baadce28b66cfe2d39f676'
        'Assets/Mofumachi/Presentation/TownTodayView.cs'='918324afbd608cb06f74e5f4af6b4d25a88efef470d0695ba101a9892c1580c3'
        'Assets/Mofumachi/Tests/PlayMode/StoryTypographyTests.cs'='4f7f8f51936fde3906dca031bc0db7c87ba02ee7aa5ad3e48ce6dc539cc396fc'
        'Assets/Mofumachi/Tests/PlayMode/QualityPassMergeTests.cs'='78c1f2f655c6ba705117deeced077d44bd1c0f6cd0fdbc5adb2a1683a4eb1b5a'
        'Assets/Mofumachi/Tests/PlayMode/LayoutNoticeTests.cs'='622b54b5af6c964684cd99c1252ac75f3d6d5e41471e8d961c16b5d80dc62616'
    }
    if(@($C.overrides).Count -ne $Allowed.Count){throw 'UX only permits the six named UI/font tests.'}
    foreach($Row in @($C.overrides)){
        if(-not $Allowed.ContainsKey($Row.path) -or $Result.ContainsKey($Row.path) -or $Row.before_sha256 -cne $Allowed[$Row.path] -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'UX cannot unlock Core, art, motion or unrelated files.'}
        $Path=Join-Path $Repository $Row.path
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf) -or ((Get-Item -LiteralPath $Path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Get-FinishTextHash $Path) -cne $Row.sha256){throw "UX source changed: $($Row.path)"}
        $Result[$Row.path]=$Row.sha256
    }
    $Added=@(
        'Assets/Mofumachi/Presentation/ReleaseFontPolicy.cs'
        'Assets/Mofumachi/Presentation/ReleaseFontPolicy.cs.meta'
        'Assets/Mofumachi/Presentation/ReleaseTypography.cs'
        'Assets/Mofumachi/Presentation/ReleaseTypography.cs.meta'
        'Assets/Mofumachi/Presentation/DailyFlowerPresentation.cs'
        'Assets/Mofumachi/Presentation/DailyFlowerPresentation.cs.meta'
        'Assets/Mofumachi/Resources/Mofumachi/UIFontReading.ttf'
        'Assets/Mofumachi/Resources/Mofumachi/UIFontReading.ttf.meta'
        'Assets/Mofumachi/Resources/Mofumachi/UIFontReadingBold.ttf'
        'Assets/Mofumachi/Resources/Mofumachi/UIFontReadingBold.ttf.meta'
        'Assets/Mofumachi/Resources/Mofumachi/UIFontAccent.ttf'
        'Assets/Mofumachi/Resources/Mofumachi/UIFontAccent.ttf.meta'
    );$Seen=@{}
    if(@($C.additions).Count -ne $Added.Count){throw 'UX role font/helper additions differ.'}
    foreach($Row in @($C.additions)){
        if($Row.path -cnotin $Added -or $Seen.ContainsKey($Row.path) -or $Row.text_normalized -isnot [bool] -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$' -or $Row.text_normalized -ne (-not $Row.path.EndsWith('.ttf'))){throw 'Unrelated UX addition refused.'};$Seen[$Row.path]=$true
        $Path=Join-Path $Repository $Row.path
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf) -or ((Get-Item -LiteralPath $Path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'UX addition missing or linked.'}
        $Hash=if($Row.text_normalized){Get-FinishTextHash $Path}else{(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
        if($Hash -cne $Row.sha256){throw "UX addition changed: $($Row.path)"}
    }
    return $Result
}
