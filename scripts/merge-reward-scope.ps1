# Additive permission for the user's Merge UI task. Historical locks stay intact.
function Read-MergeRewardOverrides([string]$Repository){
    $Manifest=Join-Path $Repository 'References/VisualLock/merge-reward-ui-2026-11-02.json';$Result=@{}
    if(-not(Test-Path -LiteralPath $Manifest -PathType Leaf)){return $Result}
    $Change=Get-Content -Raw -Encoding UTF8 -LiteralPath $Manifest | ConvertFrom-Json
    if($Change.kind -cne 'merge-reward-ui-candidate' -or $Change.change_authorized -isnot [bool] -or -not $Change.change_authorized){throw 'Merge UI authorization missing.'}
    foreach($Flag in @('formal_fixed','adopted','native_verified','pixel_verified')){if($Change.$Flag -isnot [bool] -or $Change.$Flag){throw 'Pending Merge UI record cannot claim formal/native/device acceptance.'}}
    $Reference=Join-Path $Repository 'References/Merge/merge_visual_lock_reward.png'
    $Saved=$Change.PSObject.Properties['original_png_saved']
    if((Test-Path -LiteralPath $Reference) -or ($null -ne $Saved -and $Saved.Value)){
        if($null -eq $Saved -or $Saved.Value -isnot [bool] -or -not $Saved.Value -or $Change.reference_sha256 -cnotmatch '^[a-f0-9]{64}$' -or -not(Test-Path -LiteralPath $Reference -PathType Leaf) -or ((Get-Item -LiteralPath $Reference -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Get-FileHash -LiteralPath $Reference -Algorithm SHA256).Hash -ine $Change.reference_sha256){throw 'Formal Merge reference missing, changed or incorrectly recorded.'}
    }
    $Historical=Join-Path $Repository 'References/VisualLock/noah-authored-game-candidate-2026-10-30.json'
    $Before=Join-Path $Repository 'docs/verification/merge-reward-before-2026-11-02.json'
    if((Get-FinishTextHash $Historical) -cne $Change.historical_manifest_sha256 -or (Get-FinishTextHash $Before) -cne $Change.before_snapshot_sha256){throw 'Preserve the historical lock and Merge preflight snapshot.'}
    $Old=Get-Content -Raw -Encoding UTF8 -LiteralPath $Historical | ConvertFrom-Json
    $Allowed=@('QuestMergeScreens.cs','MergePerspective.cs','MergeCellSurfaceGraphic.cs','MergeSparkleGraphic.cs') | ForEach-Object {'Assets/Mofumachi/Presentation/'+$_}
    $Allowed+=@('Assets/Mofumachi/Tests/PlayMode/MergeInteractionTests.cs','Assets/Mofumachi/Tests/PlayMode/ExperienceMergeTests.cs')
    if(@($Change.overrides).Count -ne 6){throw 'Only four named Merge UI files and their two existing presentation tests may change.'}
    foreach($Row in @($Change.overrides)){
        $Prior=@($Old.protected_files | Where-Object {$_.path -ceq $Row.path})
        if($Row.path -cnotin $Allowed -or $Result.ContainsKey($Row.path) -or $Prior.Count -ne 1 -or $Row.before_sha256 -cne $Prior[0].sha256 -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Merge amendment cannot unlock Core, art, characters or unrelated UI.'}
        $Result[$Row.path]=$Row.sha256
    }
    $Added=@('MergeRewardPresentation.cs','MergeRewardPresentation.cs.meta','MergeRewardView.cs','MergeRewardView.cs.meta') | ForEach-Object {'Assets/Mofumachi/Presentation/'+$_}
    $Seen=@{};if(@($Change.additions).Count -ne 4){throw 'Exact Merge UI helper/view additions required.'}
    foreach($Row in @($Change.additions)){
        if($Row.path -cnotin $Added -or $Seen.ContainsKey($Row.path) -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Unrelated Merge addition refused.'};$Seen[$Row.path]=$true
        $Result[$Row.path]=$Row.sha256
    }
    foreach($Path in @($Result.Keys)){
        $File=Join-Path $Repository $Path
        if(-not(Test-Path -LiteralPath $File -PathType Leaf) -or ((Get-Item -LiteralPath $File -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Get-FinishTextHash $File) -cne $Result[$Path]){throw "Merge candidate source drift: $Path"}
    }
    $Snapshot=Get-Content -Raw -Encoding UTF8 -LiteralPath $Before | ConvertFrom-Json
    foreach($Row in @($Snapshot.protected_core_geometry_input_and_formal_item_files)){
        # D's cell aspect is presentation-only; other original protected inputs stay fixed.
        if($Row.path -ceq 'Assets/Mofumachi/Presentation/MergePerspective.cs'){continue}
        if($Row.path -cnotmatch '^Assets/Mofumachi/' -or $Row.path -match '(^|/)\.\.?(/|$)|[:\\]' -or $Row.sha256 -cnotmatch '^[a-f0-9]{64}$' -or $Row.text_normalized -isnot [bool]){throw 'Invalid original protected Merge input.'}
        $File=Join-Path $Repository $Row.path
        if(-not(Test-Path -LiteralPath $File -PathType Leaf) -or ((Get-Item -LiteralPath $File -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Protected Merge input missing/linked.'}
        $Hash=if($Row.text_normalized){Get-FinishTextHash $File}else{(Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant()}
        if($Hash -cne $Row.sha256){throw "Protected Merge logic/item/input changed: $($Row.path)"}
    }
    foreach($Path in $Added){$Result.Remove($Path)}
    return $Result
}
