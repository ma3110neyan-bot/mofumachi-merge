$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../visual-lock.ps1')
function Reject([scriptblock]$Action,[string]$Reason){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw "Unauthorized North/Noah input accepted: $Reason"}}
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-north-noah-lock-'+[guid]::NewGuid())
$ReleaseRelative='References/VisualLock/fountain-release-and-field-freeze-2026-10-27.json'
$RecordRelative='References/VisualLock/north-noah-fixture.json'
$OriginalRelative='References/VisualLock/fountain-final-candidate-2026-10-27.json'
$Authorized=@('Assets/Mofumachi/Presentation/TownNorthMasterLayout.cs','Assets/Mofumachi/Presentation/TownNorthGroundGraphic.cs','Assets/Mofumachi/Presentation/TownActor.cs','Assets/Mofumachi/Presentation/CharacterBodyGraphic.cs')
function FileDigest([string]$Path){Get-VisualLockDigest ([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($Path).Replace("`r`n","`n")))}
try {
    $Record=Get-Content -LiteralPath (Join-Path $Repo $OriginalRelative) -Raw -Encoding UTF8 | ConvertFrom-Json
    $Original=$Record | ConvertTo-Json -Depth 20
    $Freeze=Get-Content -LiteralPath (Join-Path $Repo $ReleaseRelative) -Raw -Encoding UTF8 | ConvertFrom-Json
    $Base=Get-Content -LiteralPath (Join-Path $Repo $Record.base_lock) -Raw -Encoding UTF8 | ConvertFrom-Json
    $Paths=@($Record.base_lock,$OriginalRelative,$ReleaseRelative,$Record.field_freeze,$Record.current_before_record,'References/VisualLock/town-field-final-candidate-2026-10-25.json','References/VisualLock/base-2026-10-21.json')+@($Record.changes.path)+@($Record.new_files.path)+@($Record.capture_inputs.path)+@($Freeze.files.path)+@(Get-CentralInputPaths)+$Authorized
    foreach($Relative in @($Paths | Select-Object -Unique)) {
        $Target=Join-Path $Root $Relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $Target) -Force | Out-Null
        if(Test-Path -LiteralPath (Join-Path $Repo $Relative) -PathType Leaf){Copy-Item -LiteralPath (Join-Path $Repo $Relative) -Destination $Target}
    }
    foreach($Relative in @('Assets/Mofumachi/Presentation/TownNoah25DTrial.cs','Assets/Mofumachi/Presentation/TownNoah25DTrial.cs.meta')) {
        $Target=Join-Path $Root $Relative
        if(-not(Test-Path -LiteralPath $Target -PathType Leaf)){[IO.File]::WriteAllText($Target,"North Noah gate fixture; never native evidence`n")}
        $Record.new_files+=@([pscustomobject]@{path=$Relative;after_sha256=(FileDigest $Target)})
    }
    foreach($Relative in $Authorized) {
        $Target=Join-Path $Root $Relative
        [IO.File]::AppendAllText($Target,"`n// Authorized North/Noah fixture change`n")
        $Entry=@($Base.files | Where-Object {$_.path -eq $Relative})
        $Record.changes+=@([pscustomobject]@{path=$Relative;before_sha256=$Entry[0].sha256;after_sha256=(FileDigest $Target)})
    }
    # Capture is authorized separately. Never include it in the product freeze.
    foreach($InputFile in @($Record.changes)+@($Record.new_files)+@($Record.capture_inputs)){$InputFile.after_sha256=FileDigest (Join-Path $Root $InputFile.path)}
    $Record.scope_id='town_north_noah_trial_2026_10_27'
    $Record | Add-Member -NotePropertyName release_freeze -NotePropertyValue $ReleaseRelative
    $Record | Add-Member -NotePropertyName release_freeze_lf_sha256 -NotePropertyValue (FileDigest (Join-Path $Root $ReleaseRelative))
    @{base_polish=$RecordRelative} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Root 'References/VisualLock/stage-acceptance-2026-10-21.json') -Encoding UTF8
    $RecordPath=Join-Path $Root $RecordRelative
    function SaveRecord {$Record | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $RecordPath -Encoding UTF8}
    SaveRecord
    $Valid=Get-BasePolishPatch $Root
    if($Valid.Before.Count -ne 24 -or $Valid.After.Count -ne 24){throw 'Expected 13 historical changes, ten added files and one capture binding.'}
    Assert-OriginalBaseForCandidate $Root
    $Body='Assets/Mofumachi/Presentation/CharacterBodyGraphic.cs'
    if($Valid.Before[$Body] -ne '1ab679902e3bc8a9631a999c58bfb03d624c7505ee089d8cf076417c4b5c900d'){throw 'Character body Before no longer matches the original 354-input baseline.'}
    $Record.candidate_render_digest=Get-CentralInputFingerprint $Root;SaveRecord
    if((Get-ReviewedCentralInputFingerprint $Root) -ne 'dfb2cbc80e81c0d3967341f13426e2465913887f16f26763f696c36c7efe26fa'){throw 'Original reviewed rendering identity changed.'}
    foreach($Relative in @($Body,'Assets/Mofumachi/Presentation/TownNoah25DTrial.cs','Assets/Mofumachi/Presentation/TownNoah25DTrial.cs.meta')) {
        $Path=Join-Path $Root $Relative;$Saved=[IO.File]::ReadAllBytes($Path);$Digest=Get-CentralInputFingerprint $Root
        [IO.File]::AppendAllText($Path,' fresh dependency mutation')
        if((Get-CentralInputFingerprint $Root) -eq $Digest){throw "Fresh review omitted $Relative"}
        Reject {Get-BasePolishPatch $Root} "unbound dependency $Relative"
        [IO.File]::WriteAllBytes($Path,$Saved)
    }
    foreach($Relative in @('Assets/Mofumachi/Presentation/TownFountainGraphic.cs','Assets/Mofumachi/Presentation/TownFountainWaterLayout.cs','Assets/Mofumachi/Presentation/TownAreaNavigation.cs','Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png')) {
        $Path=Join-Path $Root $Relative;$Saved=[IO.File]::ReadAllBytes($Path)
        $InputFile=@(@($Record.changes)+@($Record.new_files) | Where-Object {$_.path -eq $Relative})
        $SavedHash=if($InputFile.Count){$InputFile[0].after_sha256}else{''}
        [IO.File]::AppendAllText($Path,' rebound but frozen mutation')
        if($InputFile.Count){$InputFile[0].after_sha256=FileDigest $Path}
        $Record.candidate_render_digest=Get-CentralInputFingerprint $Root;SaveRecord
        Reject {Get-BasePolishPatch $Root} "frozen file with refreshed candidate binding $Relative"
        [IO.File]::WriteAllBytes($Path,$Saved)
        if($InputFile.Count){$InputFile[0].after_sha256=$SavedHash}
        $Record.candidate_render_digest=Get-CentralInputFingerprint $Root;SaveRecord
    }
    $ReleasePath=Join-Path $Root $ReleaseRelative;$ReleaseText=[IO.File]::ReadAllText($ReleasePath).Replace("`r`n","`n")
    [IO.File]::WriteAllText($ReleasePath,$ReleaseText.Replace("`n","`r`n"));$null=Get-BasePolishPatch $Root
    [IO.File]::WriteAllText($ReleasePath,$ReleaseText+' ');Reject {Get-BasePolishPatch $Root} 'changed release-freeze content'
    [IO.File]::WriteAllText($ReleasePath,$ReleaseText)
    foreach($Key in @('authorized_by','fountain_adopted','approved_source_revision')) {
        $SavedValue=$Freeze.$Key
        $Freeze.$Key=if($Key -eq 'fountain_adopted'){$false}else{'unapproved'}
        $Freeze | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $ReleasePath -Encoding UTF8
        $Record.release_freeze_lf_sha256=FileDigest $ReleasePath;SaveRecord
        Reject {Get-BasePolishPatch $Root} "rebound release without $Key"
        $Freeze.$Key=$SavedValue
    }
    [IO.File]::WriteAllText($ReleasePath,$ReleaseText);$Record.release_freeze_lf_sha256=FileDigest $ReleasePath;SaveRecord
    # Rehashing both manifests must not replace the adopted release snapshot.
    $FrozenBody='Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png'
    $FrozenPath=Join-Path $Root $FrozenBody;$FrozenBytes=[IO.File]::ReadAllBytes($FrozenPath)
    [IO.File]::AppendAllText($FrozenPath,' forbidden original-art replacement')
    $FrozenEntry=@($Freeze.files | Where-Object {$_.path -eq $FrozenBody})[0]
    $FrozenEntry.sha256=Get-VisualLockDigest ([IO.File]::ReadAllBytes($FrozenPath))
    $Freeze | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $ReleasePath -Encoding UTF8
    $Record.release_freeze_lf_sha256=FileDigest $ReleasePath;$Record.candidate_render_digest=Get-CentralInputFingerprint $Root;SaveRecord
    Reject {Get-BasePolishPatch $Root} 'rehashed adopted freeze plus refreshed candidate'
    [IO.File]::WriteAllBytes($FrozenPath,$FrozenBytes);[IO.File]::WriteAllText($ReleasePath,$ReleaseText)
    $Record.release_freeze_lf_sha256=FileDigest $ReleasePath;SaveRecord
    $Changes=@($Record.changes);$Record.changes=@($Changes | Where-Object {$_.path -ne $Body});SaveRecord
    Reject {Get-BasePolishPatch $Root} 'missing authorized historical body binding'
    $Record.changes=$Changes;SaveRecord
    # The pre-trial scope remains accepted by the bounded-input validator.
    # Its old capture digest is historical; this fixture has changed North/Noah.
    $Record=$Original | ConvertFrom-Json
    foreach($InputFile in @($Record.changes)+@($Record.new_files)+@($Record.capture_inputs)){$InputFile.after_sha256=FileDigest (Join-Path $Root $InputFile.path)}
    $Record.candidate_render_digest=Get-CentralInputFingerprint $Root;SaveRecord
    $Legacy=Get-BasePolishPatch $Root
    if($Legacy.Before.Count -ne 18 -or $Legacy.After.Count -ne 18){throw 'Legacy fountain-final binding compatibility failed.'}
    Reject {Assert-OriginalBaseForCandidate $Root} 'legacy scope cannot authorize body replacement'
    # New Noah-only pass: user accepted North. Restore the fixture to the new
    # current product before exercising its independently anchored freeze.
    $CurrentStage=Get-Content -LiteralPath (Join-Path $Repo 'References/VisualLock/stage-acceptance-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $Record=Get-Content -LiteralPath (Join-Path $Repo $CurrentStage.base_polish) -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach($InputFile in @($Record.changes)+@($Record.new_files)+@($Record.capture_inputs)) {
        Copy-Item -LiteralPath (Join-Path $Repo $InputFile.path) -Destination (Join-Path $Root $InputFile.path)
    }
    $NorthFreezePath=Join-Path $Root $Record.north_boundary_freeze
    Copy-Item -LiteralPath (Join-Path $Repo $Record.north_boundary_freeze) -Destination $NorthFreezePath
    @{base_polish=$RecordRelative;north_boundary_2026_10_28=@{user_approved=$true;frozen=$true;freeze_record=$Record.north_boundary_freeze}} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Root 'References/VisualLock/stage-acceptance-2026-10-21.json') -Encoding UTF8
    SaveRecord;$null=Get-BasePolishPatch $Root;Assert-OriginalBaseForCandidate $Root
    foreach($FrozenPath in @('Assets/Mofumachi/Presentation/TownNorthMasterLayout.cs','Assets/Mofumachi/Presentation/TownNorthGroundGraphic.cs','Assets/Mofumachi/Presentation/TownContinuousWorldView.cs')) {
        $Path=Join-Path $Root $FrozenPath;$Saved=[IO.File]::ReadAllBytes($Path)
        $Binding=@($Record.changes | Where-Object {$_.path -eq $FrozenPath})[0];$SavedHash=$Binding.after_sha256
        [IO.File]::AppendAllText($Path,' forbidden accepted-boundary change')
        $Binding.after_sha256=FileDigest $Path;SaveRecord
        Reject {Get-BasePolishPatch $Root} "accepted boundary rebound in Noah-only pass: $FrozenPath"
        $Record.scope_id='town_north_noah_trial_2026_10_27';SaveRecord
        Reject {Get-BasePolishPatch $Root} "scope downgrade cannot reopen accepted boundary: $FrozenPath"
        $Record.scope_id='town_noah_quality_2026_10_28'
        [IO.File]::WriteAllBytes($Path,$Saved);$Binding.after_sha256=$SavedHash;SaveRecord
    }
    $Saved=[IO.File]::ReadAllBytes($NorthFreezePath)
    [IO.File]::AppendAllText($NorthFreezePath,' ');$Record.north_boundary_freeze_lf_sha256=FileDigest $NorthFreezePath;SaveRecord
    Reject {Get-BasePolishPatch $Root} 'rehashed accepted-North freeze'
    [IO.File]::WriteAllBytes($NorthFreezePath,$Saved);$Record.north_boundary_freeze_lf_sha256=FileDigest $NorthFreezePath;SaveRecord
    $null=Get-BasePolishPatch $Root
    Write-Host 'North/Noah locks: legacy compatibility and new Noah-only pass; accepted North/fountain/art/navigation reject candidate rebinding.'
} finally {if(Test-Path -LiteralPath $Root){Remove-Item -LiteralPath $Root -Recurse -Force}}
