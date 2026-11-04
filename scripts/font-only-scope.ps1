# Exact font-only amendment. Historical records and non-font product inputs stay protected.
function Read-FontOnlyAmendment([string]$Repository){
    $File=Join-Path $Repository 'References/VisualLock/font-only-2026-11-04.json'
    if(-not(Test-Path -LiteralPath $File -PathType Leaf)){return $null}
    $C=Get-Content -LiteralPath $File -Raw -Encoding UTF8|ConvertFrom-Json
    if($C.kind -cne 'hachi-font-only-candidate' -or $C.change_authorized -isnot [bool] -or -not $C.change_authorized -or $C.baseline_revision -cne '7d95057c852ce63720ccd43cf382408c7a6dcc41'){throw 'Invalid font-only authorization.'}
    foreach($Flag in @('formal_fixed','adopted','native_verified','pixel_verified')){if($C.$Flag -isnot [bool] -or $C.$Flag){throw 'Font-only candidate cannot claim acceptance.'}}
    $Parents=@{
        'References/VisualLock/release-ux-2026-11-03.json'='117fdd6aed6b7d32a5e221fdcee0d6fb08085312eeebdbf7ced0b98c6efd7772'
        'References/VisualLock/merge-font-visual-2026-11-04.json'='4b99dbc16a38d4e8b5258668763494c0125d46df0c24140363ab8f20e80d3577'
    }
    if(@($C.parents).Count -ne 2){throw 'Preserve both historical typography manifests.'};$Seen=@{}
    foreach($Row in @($C.parents)){if(-not $Parents.ContainsKey($Row.path) -or $Seen.ContainsKey($Row.path) -or $Row.sha256 -cne $Parents[$Row.path] -or (Get-FinishTextHash (Join-Path $Repository $Row.path)) -cne $Row.sha256){throw 'Historical font manifest changed.'};$Seen[$Row.path]=$true}
    $Allowed=@{
        'Assets/Mofumachi/Presentation/ReleaseFontPolicy.cs'='71329b1e6d0e58960da2ffc8702c35470c8ff42ea2a2a26a95dadbcbbb64d208'
        'Assets/Mofumachi/Presentation/ReleaseTypography.cs'='8d0592ba172938676ecf01da2403b91360b50a7f49da9f9afb731ce1bbc7d156'
        'Assets/Mofumachi/Presentation/MergeTypography.cs'='1fd979f6c12662abe2eb9f7a2f2094eecbaaf23c315410db824d54b34a4b0ce3'
        'Assets/Mofumachi/Presentation/UIWidgets.cs'='c0461745cf47c3b0c32d5ebfbc99191ab63d7a06de8bbd435ec3f583919d4753'
        'Assets/Mofumachi/Tests/PlayMode/ReleaseUxNativeTests.cs'='604e46af0b3c6a1501b002aaa253fbff427e56042d3579a02b379a5e07756e21'
        'Assets/Mofumachi/Tests/PlayMode/StoryTypographyTests.cs'='66665ad6abac3dca90f49f16b23b910ec0f2e34374aa2fcf37c8d1ff354327b2'
        'Assets/Mofumachi/Tests/PlayMode/QualityPassMergeTests.cs'='5892c9bb7084923c1e393d2f160ca0a3148096284441fe9a307bbb4e241d477f'
        'Assets/Mofumachi/Tests/PlayMode/QualityPassStoryGrowthTests.cs'='df3d8af1a12a91b2c3060d96d68db596a478c37db72367f3586cc1188bee49c3'
        'Assets/Mofumachi/Tests/PlayMode/LayoutNoticeTests.cs'='c4f84a9a8b154eec6756ee1768dae217cf2fc3fe9c7adc7c6be99f343a19257e'
    };$Seen=@{}
    if(@($C.overrides).Count -ne $Allowed.Count){throw 'Exact font-only sources/tests required.'}
    foreach($Row in @($C.overrides)){
        if(-not $Allowed.ContainsKey($Row.path) -or $Seen.ContainsKey($Row.path) -or $Row.before_sha256 -cne $Allowed[$Row.path] -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Font-only cannot unlock art, logic or unrelated code.'};$Seen[$Row.path]=$true
        $Actual=Join-Path $Repository $Row.path
        if(-not(Test-Path -LiteralPath $Actual -PathType Leaf) -or ((Get-Item -LiteralPath $Actual -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Get-FinishTextHash $Actual) -cne $Row.sha256){throw 'Font-only source missing/changed.'}
    }
    $Added=@('Assets/Mofumachi/Tests/PlayMode/HachiFontCapture.cs','Assets/Mofumachi/Tests/PlayMode/HachiFontCapture.cs.meta');$Seen=@{}
    if(@($C.additions).Count -ne 2){throw 'Exact native font helper/meta required.'}
    foreach($Row in @($C.additions)){
        if($Row.path -cnotin $Added -or $Seen.ContainsKey($Row.path) -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Unrelated font-only addition refused.'};$Seen[$Row.path]=$true
        $Actual=Join-Path $Repository $Row.path
        if(-not(Test-Path -LiteralPath $Actual -PathType Leaf) -or ((Get-Item -LiteralPath $Actual -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Get-FinishTextHash $Actual) -cne $Row.sha256){throw 'Native font helper missing/changed.'}
    }
    return $C
}
function Read-FontOnlyExpected([string]$Repository,[string]$Path,[string]$ParentHash){
    $C=Read-FontOnlyAmendment $Repository;if($null -eq $C){return $ParentHash}
    $Row=@($C.overrides|Where-Object {$_.path -ceq $Path})
    if($Row.Count -eq 0){return $ParentHash}
    if($Row[0].before_sha256 -cne $ParentHash){throw 'Font-only amendment parent differs.'}
    return $Row[0].sha256
}
