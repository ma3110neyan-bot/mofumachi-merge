$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../visual-lock.ps1')
function Reject([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Unauthorized base change was accepted.'}}
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
# RED before the patch gate: current camera differs from the immutable image approval.
Assert-BaseStagesReadyForAcceptance $Repo
$Historical=Get-Content -LiteralPath (Join-Path $Repo 'References/VisualLock/stage-acceptance-2026-10-21.json') -Raw -Encoding UTF8 | ConvertFrom-Json
# Historical image approval may remain valid, but is not a fresh capture.
Reject {Assert-CentralReviewEvidence $Repo (Join-Path $Repo $Historical.central.review_directory)}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-camera-lock-'+[guid]::NewGuid())
New-Item -ItemType Directory -Path (Join-Path $Root 'References/VisualLock') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $Root 'Assets/Mofumachi/Presentation') -Force | Out-Null
try {
    $Camera='Assets/Mofumachi/Presentation/TownWorldLayout.cs'
    $View='Assets/Mofumachi/Presentation/TownContinuousWorldView.cs';$ViewFile=Join-Path $Root $View
    $CameraFile=Join-Path $Root $Camera;$Title=Join-Path $Root 'title.cs'
    [IO.File]::WriteAllText($CameraFile,"corrected camera`n");[IO.File]::WriteAllText($Title,"frozen title`n")
    [IO.File]::WriteAllText($ViewFile,"captions removed`n")
    $Old=Get-VisualLockDigest ([Text.Encoding]::UTF8.GetBytes("approved camera`n"))
    $OldView=Get-VisualLockDigest ([Text.Encoding]::UTF8.GetBytes("approved captions`n"));$NewView=Get-VisualLockDigest ([IO.File]::ReadAllBytes($ViewFile))
    $New=Get-VisualLockDigest ([IO.File]::ReadAllBytes($CameraFile))
    $Base='References/VisualLock/base.json';$BaseFile=Join-Path $Root $Base
    @{files=@(@{path=$Camera;mode='lf-text';sha256=$Old},@{path=$View;mode='lf-text';sha256=$OldView},@{path='title.cs';mode='lf-text';sha256=(Get-VisualLockDigest ([IO.File]::ReadAllBytes($Title)))})} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $BaseFile -Encoding UTF8
    $PatchFile=Join-Path $Root 'References/VisualLock/camera-candidate.json'
    $Patch=@{authorized_by='user';authorization_reference='User requested stage1 minimal camera/caption correction';base_lock=$Base;base_lock_sha256=(Get-FileHash -LiteralPath $BaseFile -Algorithm SHA256).Hash;changes=@(@{path=$Camera;before_sha256=$Old;after_sha256=$New},@{path=$View;before_sha256=$OldView;after_sha256=$NewView})}
    $ApprovedChanges=@($Patch.changes)
    @{base_polish='References/VisualLock/camera-candidate.json'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Root 'References/VisualLock/stage-acceptance-2026-10-21.json') -Encoding UTF8
    function SavePatch {$Patch | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $PatchFile -Encoding UTF8}
    SavePatch
    $Valid=Get-BasePolishPatch $Root
    Assert-BaseVisualLock $Root $BaseFile $Valid.After
    [IO.File]::WriteAllText($Title,'regressed title');Reject {Assert-BaseVisualLock $Root $BaseFile $Valid.After}
    [IO.File]::WriteAllText($Title,"frozen title`n")
    $Patch.authorized_by='agent';SavePatch;Reject {Get-BasePolishPatch $Root};$Patch.authorized_by='user'
    $Patch.changes[0].before_sha256=('0'*64);SavePatch;Reject {Get-BasePolishPatch $Root};$Patch.changes[0].before_sha256=$Old
    $Patch.changes[0].after_sha256=('0'*64);SavePatch;Reject {Get-BasePolishPatch $Root};$Patch.changes[0].after_sha256=$New
    $Patch.changes+=@{path='title.cs';before_sha256=$Old;after_sha256=$New};SavePatch;Reject {Get-BasePolishPatch $Root}
    $Patch.changes=$ApprovedChanges;SavePatch
    [IO.File]::WriteAllText($CameraFile,'another unreviewed camera');Reject {Get-BasePolishPatch $Root}
    [IO.File]::WriteAllText($CameraFile,"corrected camera`n")
    [IO.File]::WriteAllText($ViewFile,'another caption implementation');Reject {Get-BasePolishPatch $Root}
    [IO.File]::WriteAllText($ViewFile,"captions removed`n")
    Remove-Item -LiteralPath $PatchFile;Reject {Get-BasePolishPatch $Root}
    Write-Host 'Stage1 camera authorization: exact delta allowed; changed title/camera/old-new hashes/missing approval/extra files rejected.'
} finally {Remove-Item -LiteralPath $Root -Recurse -Force}
