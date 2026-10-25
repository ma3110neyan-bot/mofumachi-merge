# Read-only gate. A matching hash protects frozen inputs; visual/input/performance
# acceptance still requires same-scene Unity and Pixel comparisons.
. (Join-Path $PSScriptRoot 'central-review.ps1')
function Get-VisualLockDigest([byte[]]$Bytes) {
    $Hasher=[Security.Cryptography.SHA256]::Create()
    try {return ([BitConverter]::ToString($Hasher.ComputeHash($Bytes))).Replace('-','').ToLowerInvariant()} finally {$Hasher.Dispose()}
}
function Assert-BaseVisualLock([string]$RepositoryPath,[string]$ManifestPath,[hashtable]$ReplacementHashes=@{}) {
    if(-not(Test-Path -LiteralPath $ManifestPath -PathType Leaf)){throw 'Missing Visual Lock baseline.'}
    $Root=[IO.Path]::GetFullPath($RepositoryPath).TrimEnd('/','\')
    $Lock=Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if(@($Lock.files).Count -eq 0){throw 'Empty Visual Lock baseline.'}
    foreach($Entry in $Lock.files) {
        $Relative=[string]$Entry.path
        if([IO.Path]::IsPathRooted($Relative) -or $Relative -match '(^|[/\\])\.\.([/\\]|$)' -or $Relative.Contains(':')){throw 'Unsafe frozen input path.'}
        $Path=[IO.Path]::GetFullPath((Join-Path $Root $Relative))
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "Missing frozen input: $Relative"}
        if($Entry.mode -eq 'binary'){$Bytes=[IO.File]::ReadAllBytes($Path)}
        else {
            $Value=[IO.File]::ReadAllText($Path).Replace("`r`n","`n")
            if($Entry.mode -eq 'section') {
                $Start=$Value.IndexOf([string]$Entry.start,[StringComparison]::Ordinal)
                if($Start -lt 0){throw "Missing frozen section: $Relative"}
                $End=$Value.IndexOf([string]$Entry.end,$Start+([string]$Entry.start).Length,[StringComparison]::Ordinal)
                if($End -le $Start){throw "Missing frozen section end: $Relative"}
                $Value=$Value.Substring($Start,$End-$Start)
            } elseif($Entry.mode -ne 'lf-text'){throw "Unknown Visual Lock mode: $($Entry.mode)"}
            $Bytes=[Text.Encoding]::UTF8.GetBytes($Value)
        }
        $Expected=if($ReplacementHashes.ContainsKey($Relative)){$ReplacementHashes[$Relative]}else{$Entry.sha256}
        if((Get-VisualLockDigest $Bytes) -ne $Expected){throw "Visual Lock changed: $Relative. Restore the frozen input; do not replace the baseline to bypass this gate."}
    }
}
function Get-BasePolishPatch([string]$RepositoryPath) {
    $StageFile=Join-Path $RepositoryPath 'References/VisualLock/stage-acceptance-2026-10-21.json'
    if(-not(Test-Path -LiteralPath $StageFile -PathType Leaf)){return $null}
    $Stages=Get-Content -LiteralPath $StageFile -Raw -Encoding UTF8 | ConvertFrom-Json
    $Property=$Stages.PSObject.Properties['base_polish'];if(-not $Property){return $null}
    $Relative=[string]$Property.Value
    if(-not $Relative -or [IO.Path]::IsPathRooted($Relative) -or $Relative -match '(^|[/\\])\.\.([/\\]|$)' -or $Relative.Contains(':')){throw 'Unsafe base polish record path.'}
    $Record=Get-Content -LiteralPath (Join-Path $RepositoryPath $Relative) -Raw -Encoding UTF8 | ConvertFrom-Json
    if($Record.authorized_by -ne 'user' -or [string]::IsNullOrWhiteSpace([string]$Record.authorization_reference)){throw 'Base polish requires the user bounded correction instruction.'}
    $BaseRelative=[string]$Record.base_lock
    if(-not $BaseRelative -or [IO.Path]::IsPathRooted($BaseRelative) -or $BaseRelative -match '(^|[/\\])\.\.([/\\]|$)' -or $BaseRelative.Contains(':')){throw 'Unsafe base polish baseline path.'}
    $BasePath=Join-Path $RepositoryPath $BaseRelative
    $ModeProperty=$Record.PSObject.Properties['base_lock_hash_mode']
    $BaseMode=if($ModeProperty){[string]$ModeProperty.Value}else{'binary'}
    # Match the existing lf-text input locks across Git's Windows CRLF checkout.
    # The original approval file and its recorded content hash stay unchanged.
    if($BaseMode -eq 'lf-text'){$BaseBytes=[Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($BasePath).Replace("`r`n","`n"))}
    elseif($BaseMode -eq 'binary'){$BaseBytes=[IO.File]::ReadAllBytes($BasePath)}
    else {throw 'Unknown base approval hash mode.'}
    if($Record.base_lock_sha256 -notmatch '^[a-fA-F0-9]{64}$' -or (Get-VisualLockDigest $BaseBytes) -ne $Record.base_lock_sha256){throw 'Original base approval content was changed.'}
    $Base=Get-Content -LiteralPath $BasePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $Changes=@($Record.changes)
    $Allowed=@('Assets/Mofumachi/Presentation/TownWorldLayout.cs','Assets/Mofumachi/Presentation/TownContinuousWorldView.cs')
    $ScopeProperty=$Record.PSObject.Properties['scope_id']
    $Scope=if($ScopeProperty){[string]$ScopeProperty.Value}else{''}
    if($Scope -and $Scope -notin @('town_boundary_final_2026_10_24','town_field_stability_2026_10_25','town_field_completion_2026_10_25','town_fountain_water_2026_10_25')){throw 'Unknown bounded display correction scope.'}
    $Fountain=$Scope -eq 'town_fountain_water_2026_10_25'
    $FieldFinal=$Scope -eq 'town_field_completion_2026_10_25' -or $Fountain
    $FieldStability=$Scope -eq 'town_field_stability_2026_10_25' -or $FieldFinal
    $Boundary=$Scope -eq 'town_boundary_final_2026_10_24' -or $FieldStability
    if($Boundary){$Allowed+=@('Assets/Mofumachi/Presentation/TownSouthGroundGraphic.cs','Assets/Mofumachi/Presentation/TownSouthEntryBlendLayout.cs','Assets/Mofumachi/Presentation/TownSouthEntryBlendGraphic.cs')}
    if($FieldStability){$Allowed+='Assets/Mofumachi/Presentation/TownAreaNavigation.cs'}
    if($FieldFinal){$Allowed+=@('Assets/Mofumachi/Presentation/TownGrowthPresentation.cs','Assets/Mofumachi/Presentation/TownGrowthComparisonView.cs')}
    if($Changes.Count -lt 1 -or $Changes.Count -gt $Allowed.Count){throw 'Display changes exceed the explicitly authorized correction scope.'}
    $Before=@{};$After=@{}
    foreach($Change in $Changes) {
        $Path=[string]$Change.path
        if($Path -notin $Allowed -or $Before.ContainsKey($Path)){throw 'Unrelated or duplicate base polish input.'}
        $Entry=@($Base.files | Where-Object {$_.path -eq $Path})
        if($Entry.Count -ne 1 -or $Entry[0].mode -ne 'lf-text' -or $Change.before_sha256 -ne $Entry[0].sha256 -or $Change.after_sha256 -notmatch '^[a-fA-F0-9]{64}$'){throw 'Display correction does not match its original base input.'}
        $Bytes=[Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText((Join-Path $RepositoryPath $Path)).Replace("`r`n","`n"))
        if((Get-VisualLockDigest $Bytes) -ne $Change.after_sha256){throw 'Display differs from the authorized correction candidate.'}
        $Before[$Path]=$Change.before_sha256;$After[$Path]=$Change.after_sha256
    }
    $AddedProperty=$Record.PSObject.Properties['new_files'];$CaptureProperty=$Record.PSObject.Properties['capture_inputs']
    if(-not $Boundary) {
        if($AddedProperty -or $CaptureProperty){throw 'Stage1 cannot add rendering or capture inputs.'}
    } else {
        if(-not $AddedProperty -or -not $CaptureProperty){throw 'Boundary correction must bind its added and capture inputs.'}
        $Added=@($AddedProperty.Value);$Captures=@($CaptureProperty.Value)
        $AllowedAdded=@('Assets/Mofumachi/Presentation/TownBoundaryGardenLayout.cs','Assets/Mofumachi/Presentation/TownBoundaryGardenLayout.cs.meta','Assets/Mofumachi/Presentation/TownBoundaryGardenGraphic.cs','Assets/Mofumachi/Presentation/TownBoundaryGardenGraphic.cs.meta')
        if($Fountain){$AllowedAdded+=@('Assets/Mofumachi/Presentation/TownFountainWaterLayout.cs','Assets/Mofumachi/Presentation/TownFountainWaterLayout.cs.meta','Assets/Mofumachi/Presentation/TownFountainWaterGraphic.cs','Assets/Mofumachi/Presentation/TownFountainWaterGraphic.cs.meta')}
        if($Added.Count -ne $AllowedAdded.Count -or $Captures.Count -ne 1){throw 'Boundary rendering/capture input list is incomplete.'}
        foreach($InputFile in $Added) {
            $Path=[string]$InputFile.path
            if($Path -notin $AllowedAdded -or $Before.ContainsKey($Path) -or $InputFile.after_sha256 -notmatch '^[a-fA-F0-9]{64}$'){throw 'Unrelated or duplicate added rendering input.'}
            $Bytes=[Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText((Join-Path $RepositoryPath $Path)).Replace("`r`n","`n"))
            if((Get-VisualLockDigest $Bytes) -ne $InputFile.after_sha256){throw 'Added rendering input differs from the authorized candidate.'}
            # A null historical hash omits a newly added path from the old
            # approval digest only. Fresh capture identity always uses the file.
            $Before[$Path]=$null;$After[$Path]=$InputFile.after_sha256
        }
        $Capture=$Captures[0];$Path=[string]$Capture.path
        if($Path -ne 'Assets/Mofumachi/Tests/PlayMode/ExperienceCaptureTests.cs' -or $Capture.before_sha256 -notmatch '^[a-fA-F0-9]{64}$' -or $Capture.after_sha256 -notmatch '^[a-fA-F0-9]{64}$'){throw 'Unrelated or incomplete boundary capture input.'}
        $Bytes=[Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText((Join-Path $RepositoryPath $Path)).Replace("`r`n","`n"))
        if((Get-VisualLockDigest $Bytes) -ne $Capture.after_sha256){throw 'Boundary capture differs from the authorized candidate.'}
        $Before[$Path]=$Capture.before_sha256;$After[$Path]=$Capture.after_sha256
    }
    if($Fountain) {
        $FreezePath='References/VisualLock/town-field-frozen-2026-10-25.json'
        if($Record.field_freeze -ne $FreezePath -or $Record.field_freeze_hash_mode -ne 'lf-text'){throw 'Fountain water requires the exact user-frozen field reference.'}
        $FreezeText=[IO.File]::ReadAllText((Join-Path $RepositoryPath $FreezePath)).Replace("`r`n","`n")
        if($Record.field_freeze_sha256 -notmatch '^[a-fA-F0-9]{64}$' -or (Get-VisualLockDigest ([Text.Encoding]::UTF8.GetBytes($FreezeText))) -ne $Record.field_freeze_sha256){throw 'User field-freeze record changed.'}
        $Freeze=$FreezeText | ConvertFrom-Json
        if($Freeze.authorized_by -ne 'user' -or $Freeze.five_area_graphics_frozen -isnot [bool] -or -not $Freeze.five_area_graphics_frozen -or $Freeze.next_authorized_stage -ne 'fountain_water_only' -or $Record.before_runtime_source -ne $Freeze.approved_source_revision -or $Record.before_render_digest -ne $Freeze.approved_render_digest){throw 'Fountain candidate is not tied to the user-approved field.'}
        $Accepted='References/VisualLock/town-field-final-candidate-2026-10-25.json'
        $AcceptedText=[IO.File]::ReadAllText((Join-Path $RepositoryPath $Accepted)).Replace("`r`n","`n")
        if($Freeze.accepted_candidate_record -ne $Accepted -or (Get-VisualLockDigest ([Text.Encoding]::UTF8.GetBytes($AcceptedText))) -ne $Freeze.accepted_candidate_record_lf_sha256){throw 'Original accepted field candidate changed.'}
        $AcceptedRecord=$AcceptedText | ConvertFrom-Json
        if($AcceptedRecord.candidate_render_digest -ne $Freeze.approved_render_digest -or @($Freeze.fixed_render_inputs).Count -ne 12){throw 'Frozen field render bindings are incomplete.'}
        $FrozenPaths=@{}
        foreach($Frozen in $Freeze.fixed_render_inputs) {
            if($FrozenPaths.ContainsKey($Frozen.path) -or -not $After.ContainsKey($Frozen.path)){throw 'Duplicate or missing frozen field input.'};$FrozenPaths[$Frozen.path]=$true
            if($Frozen.path -ne 'Assets/Mofumachi/Presentation/TownContinuousWorldView.cs' -and $After[$Frozen.path] -ne $Frozen.sha256){throw 'Fountain pass cannot alter the frozen field scenery/navigation/growth.'}
        }
    }
    return @{Record=$Record;Before=$Before;After=$After}
}
function Get-ReviewedCentralInputFingerprint([string]$RepositoryPath) {
    $Current=Get-CentralInputFingerprint $RepositoryPath;$Patch=Get-BasePolishPatch $RepositoryPath
    if(-not $Patch){return $Current}
    if($Patch.Record.candidate_render_digest -ne $Current){throw 'Base polish candidate rendering inputs changed.'}
    $Historical=$Patch.Before.Clone()
    # The original approved central digest predated this explicit navigation
    # dependency. Omit it only when reconstructing that historical identity;
    # its original file remains locked and fresh captures bind its current hash.
    $Historical['Assets/Mofumachi/Presentation/TownAreaNavigation.cs']=$null
    $Reviewed=Get-CentralInputFingerprint $RepositoryPath $Historical
    if($Patch.Record.reviewed_render_digest -ne $Reviewed){throw 'Base polish is not bound to the original reviewed rendering inputs.'}
    return $Reviewed
}
function Write-VisualLockComparison([string]$RepositoryPath,[string]$RunDirectory,[string]$BeforeDirectory='',[string]$OutputDirectory='') {
    $Baseline=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/baseline-native-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $AfterBuild=Get-Content -LiteralPath (Join-Path $RunDirectory 'build-info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $After=Get-Content -LiteralPath (Join-Path $RunDirectory 'capture-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if($After.gitRevision -ne $AfterBuild.gitRevision -or $After.productFingerprint -ne $AfterBuild.productFingerprint){throw 'Visual comparison has mismatched capture/APK source.'}
    if(-not $OutputDirectory){$OutputDirectory=$RunDirectory}
    if(-not $BeforeDirectory){$BeforeDirectory=Join-Path $RepositoryPath ('Builds/Android/'+$Baseline.run)}
    $Record=[ordered]@{before_run=$Baseline.run;before_revision=$Baseline.build.gitRevision;candidate_revision=$AfterBuild.gitRevision;candidate_apk_sha256=$AfterBuild.sha256;native_pairs_verified=0;visual=$null;input=$null;information=$null;world=$null;town_density=$null;merge_fun=$null;fps=$null;pixel_load=$null;pixel_verified=$false;adopted=$false;reason='Native images require review; Pixel input/performance/save verification pending.'}
    if(-not(Test-Path -LiteralPath $BeforeDirectory -PathType Container)) {
        $Record.reason='Before images missing. APK generation is not visual acceptance; restore the recorded baseline images before adoption.'
        $Record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'visual-adoption.json') -Encoding UTF8
        Write-Host 'Before/After pending: baseline run directory is missing. Adoption remains blocked.'
        return
    }
    function Image-Directory([string]$Directory){$Images=Join-Path $Directory 'screenshots';if(Test-Path -LiteralPath $Images -PathType Container){return $Images};return $Directory}
    function Html([string]$Value){return [Net.WebUtility]::HtmlEncode($Value)}
    function File-Uri([string]$Path){return ([Uri]([IO.Path]::GetFullPath($Path))).AbsoluteUri}
    $BeforeImages=Image-Directory $BeforeDirectory;$AfterImages=Image-Directory $RunDirectory
    $Rows=New-Object 'System.Collections.Generic.List[string]'
    foreach($Image in $Baseline.capture.images) {
        if($Image.file -notmatch '^[A-Za-z0-9_.-]+\.png$'){throw 'Unsafe baseline image name.'}
        $BeforePath=Join-Path $BeforeImages $Image.file;$AfterPath=Join-Path $AfterImages $Image.file
        $New=@($After.images | Where-Object {$_.file -eq $Image.file})
        if($New.Count -ne 1 -or -not(Test-Path -LiteralPath $BeforePath) -or -not(Test-Path -LiteralPath $AfterPath)){throw "Missing comparison image: $($Image.file)"}
        if((Get-FileHash -LiteralPath $BeforePath -Algorithm SHA256).Hash -ne $Image.sha256 -or (Get-FileHash -LiteralPath $AfterPath -Algorithm SHA256).Hash -ne $New[0].sha256){throw "Comparison image hash mismatch: $($Image.file)"}
        $Same=if($Image.sha256 -eq $New[0].sha256){' / byte-identical; retained'}else{''}
        $Rows.Add('<section><h2>'+ (Html ($Image.file+$Same)) +'</h2><div class="pair"><figure><figcaption>Before</figcaption><img loading="lazy" src="'+(Html (File-Uri $BeforePath))+'"></figure><figure><figcaption>After candidate / not accepted</figcaption><img loading="lazy" src="'+(Html (File-Uri $AfterPath))+'"></figure></div></section>')
    }
    $Record.native_pairs_verified=$Rows.Count
    $Reference=File-Uri (Join-Path $RepositoryPath 'References/Preview/design-first-2026-10-21/design-proposal.png')
    $Header='<!doctype html><html lang="ja"><meta charset="utf-8"><title>もふまち Visual Lock 比較</title><style>body{font-family:sans-serif;background:#fff6ef;color:#51383c;max-width:1100px;margin:24px auto}h1{color:#c33366}.pending{background:#ffe4db;padding:16px}.pair{display:flex;gap:16px}figure{margin:0;flex:1}img{width:100%;height:auto}section{margin:36px 0}figcaption{padding:8px;background:#d9f1e5}.reference{max-height:400px;object-fit:contain}</style><h1>Visual Lock / 実Unity Before・After候補</h1><p class="pending">未採用。静止画だけでは操作・FPS・Pixel負荷を合格にしません。全8軸の確認とPixel確認が揃うまで採用を保留します。</p><p>同名の隔離撮影工程・地区・Lv・720×1280。撮影日による今日の相手、NPC位置／表情は異なる場合があります。元絵を描き換えた比較ではありません。</p><p>Before: '+(Html $Baseline.build.gitRevision)+' / After candidate: '+(Html $AfterBuild.gitRevision)+'</p><h2>正式Visual Lock（ユーザー承認デザイン）</h2><img class="reference" src="'+(Html $Reference)+'">'
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'before-after.html'),$Header+($Rows -join "`n")+'</html>')
    $Record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'visual-adoption.json') -Encoding UTF8
    Write-Host "Native comparison: $OutputDirectory\before-after.html. Pixel/visual adoption remains pending."
}
function Assert-BaseAdoption([string]$RecordPath) {
    $Record=Get-Content -LiteralPath $RecordPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach($Axis in @('visual','input','information','world','town_density','merge_fun','fps','pixel_load','pixel_verified')) {
        if($Record.$Axis -isnot [bool] -or $Record.$Axis -ne $true){throw "Base adoption blocked: $Axis has not passed."}
    }
    if([int]$Record.native_pairs_verified -lt 102){throw 'Base adoption blocked: same-scene comparison is incomplete.'}
}
function Assert-ApprovedVisualLock([string]$RepositoryPath) {
    Assert-BaseVisualLock $RepositoryPath (Join-Path $RepositoryPath 'References/VisualLock/approved-design-2026-10-21.json')
}
function Assert-CentralStageApproved([string]$RepositoryPath) {
    Assert-ApprovedVisualLock $RepositoryPath
    $StagePath=Join-Path $RepositoryPath 'References/VisualLock/stage-acceptance-2026-10-21.json'
    if(-not(Test-Path -LiteralPath $StagePath -PathType Leaf)){throw 'Central master has no reviewed native checkpoint. Run -CentralReview before advancing.'}
    $Stage=(Get-Content -LiteralPath $StagePath -Raw -Encoding UTF8 | ConvertFrom-Json).central
    foreach($Axis in @('native_unity','visual','town_density','walking')) {
        if($Stage.$Axis -isnot [bool] -or $Stage.$Axis -ne $true){throw "Central master not accepted: $Axis. Review actual Unity images before the next district or QA APK."}
    }
    if($Stage.reviewed_candidate -notmatch '^[a-fA-F0-9]{40}$' -or [int]$Stage.native_image_count -ne 3){throw 'Central checkpoint has no identified Lv1/3/6 native image review.'}
    $Relative=[string]$Stage.review_directory
    if(-not $Relative -or [IO.Path]::IsPathRooted($Relative) -or $Relative -match '(^|[/\\])\.\.([/\\]|$)' -or $Relative.Contains(':')){throw 'Central review must identify an existing repository-relative evidence directory.'}
    $Info=Assert-CentralReviewEvidence $RepositoryPath (Join-Path $RepositoryPath $Relative) -HistoricalApproval
    if($Stage.reviewed_candidate -ne $Info.gitRevision -or $Stage.central_input_fingerprint -ne $Info.centralInputFingerprint){throw 'Central acceptance is for a different candidate or input digest.'}
}
function Assert-TownCheckpointApproved([string]$RepositoryPath,[string]$Checkpoint) {
    $Order=@('north_quest','south_garden','five_area')
    if($Checkpoint -notin $Order){throw 'Unknown town image approval checkpoint.'}
    Assert-ApprovedVisualLock $RepositoryPath
    $Stages=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/stage-acceptance-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $Checkpoints=$Stages.PSObject.Properties['user_review_checkpoints']
    if(-not $Checkpoints){throw 'Town progress blocked: north, south and integrated town require explicit user image approval.'}
    $Lock=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/approved-design-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $ReferenceHash=(Get-FileHash -LiteralPath (Join-Path $RepositoryPath $Lock.reference) -Algorithm SHA256).Hash.ToLowerInvariant()
    foreach($Name in $Order) {
        $Property=$Checkpoints.Value.PSObject.Properties[$Name]
        if(-not $Property){throw "Missing user image checkpoint: $Name"}
        $Approval=$Property.Value
        if($Approval.user_approved -isnot [bool] -or $Approval.user_approved -ne $true -or $Approval.approved_by -ne 'user' -or [string]::IsNullOrWhiteSpace([string]$Approval.approval_reference)){throw "Town progress blocked: explicit user image approval missing for $Name."}
        if($Approval.native_unity -isnot [bool] -or $Approval.native_unity -ne $true -or $Approval.reviewed_candidate -notmatch '^[a-fA-F0-9]{40}$' -or $Approval.input_fingerprint -notmatch '^[a-fA-F0-9]{64}$' -or $Approval.approved_visual_lock_sha256 -ne $ReferenceHash){throw "Town checkpoint has no source-bound native/Visual Lock review: $Name"}
        foreach($Axis in @('density','cuteness','walkability','ui_readability','level_difference','world','characters')) {
            if($Approval.quality.$Axis -isnot [bool] -or $Approval.quality.$Axis -ne $true){throw "Town quality regressed or unreviewed: $Name/$Axis"}
        }
        [string[]]$Districts=if($Name -eq 'north_quest'){@('North')}elseif($Name -eq 'south_garden'){@('South')}else{@('Central','West','East','North','South')}
        if(@($Approval.images).Count -ne $Districts.Count*3){throw "Town checkpoint needs Lv1/3/6 images for every district: $Name"}
        foreach($District in $Districts){foreach($Level in @(1,3,6)) {
            $Images=@($Approval.images | Where-Object {$_.district -eq $District -and $_.level -eq $Level})
            if($Images.Count -ne 1){throw "Missing or duplicate town image: $Name/$District/Lv$Level"}
            $Image=$Images[0];$Relative=[string]$Image.file
            if(-not $Relative -or [IO.Path]::IsPathRooted($Relative) -or $Relative -match '(^|[/\\])\.\.([/\\]|$)' -or $Relative.Contains(':') -or $Relative -notmatch '(?i)\.jpg$'){throw 'Town approval must reference repository-relative JPEG files.'}
            $Path=Join-Path $RepositoryPath $Relative
            if(-not(Test-Path -LiteralPath $Path -PathType Leaf) -or $Image.sha256 -notmatch '^[a-fA-F0-9]{64}$' -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Image.sha256){throw "Reviewed town JPEG missing or changed: $Relative"}
            $Bytes=[IO.File]::ReadAllBytes($Path)
            if($Bytes.Length -lt 4 -or $Bytes[0] -ne 255 -or $Bytes[1] -ne 216 -or $Bytes[-2] -ne 255 -or $Bytes[-1] -ne 217){throw "Reviewed town file is not a JPEG: $Relative"}
        }}
        # Earlier approvals prove sequence, while the integrated approval must
        # match current inputs. Neither metadata nor JPEG signatures replaces
        # actual native image review and the user's explicit approval.
        if($Name -eq 'five_area' -and $Approval.input_fingerprint -ne (Get-ReviewedCentralInputFingerprint $RepositoryPath)){throw 'Integrated town approval is for stale rendering inputs.'}
        if($Name -eq $Checkpoint){return}
    }
}
function Assert-BaseStagesReadyForAcceptance([string]$RepositoryPath) {
    Assert-CentralStageApproved $RepositoryPath
    $Stages=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/stage-acceptance-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $BaseLock=$Stages.PSObject.Properties['approved_base_lock']
    $Patch=Get-BasePolishPatch $RepositoryPath
    if($Patch -and -not $BaseLock){throw 'Base polish requires its original approved base lock.'}
    if($BaseLock) {
        $Relative=[string]$BaseLock.Value
        if(-not $Relative -or [IO.Path]::IsPathRooted($Relative) -or $Relative -match '(^|[/\\])\.\.([/\\]|$)' -or $Relative.Contains(':')){throw 'Approved base lock must use a repository-relative path.'}
        if($Patch) {
            if($Patch.Record.base_lock -ne $Relative){throw 'Base polish references a different approval snapshot.'}
            Assert-BaseVisualLock $RepositoryPath (Join-Path $RepositoryPath $Relative) $Patch.After
        } else {Assert-BaseVisualLock $RepositoryPath (Join-Path $RepositoryPath $Relative)}
    }
    $Required=@('west_cafe','east_florist_shop','north_quest','south_garden','connections')
    $Completed=@($Stages.completed_after_central)
    if($Completed.Count -lt $Required.Count){throw 'QA APK blocked: finish west/east/north/south/connections in order, with user image approvals.'}
    for($Index=0;$Index -lt $Required.Count;$Index++) {
        if($Completed[$Index] -ne $Required[$Index]){throw "QA APK blocked: expected stage $($Required[$Index]) at position $Index."}
    }
    Assert-TownCheckpointApproved $RepositoryPath 'five_area'
    # The build then runs the full native suite before producing this same APK.
}
