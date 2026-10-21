$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../visual-lock.ps1')
. (Join-Path $PSScriptRoot '../central-review.ps1')
function Reject([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Invalid central capture was accepted.'}}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-central-review-'+[guid]::NewGuid())
New-Item -ItemType Directory -Path $Root | Out-Null
try {
    $Header=[byte[]]@(137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,2,208,0,0,5,0,0)
    foreach($Level in @(1,3,6)){[IO.File]::WriteAllBytes((Join-Path $Root "04-town-lv$Level-Central.png"),$Header)}
    $Entries=@(Get-CentralCaptureEntries $Root)
    if($Entries.Count -ne 3){throw 'Central review needs all three stages.'}
    Remove-Item -LiteralPath (Join-Path $Root '04-town-lv3-Central.png');Reject {Get-CentralCaptureEntries $Root}
    [IO.File]::WriteAllBytes((Join-Path $Root '04-town-lv3-Central.png'),$Header)
    $Header[20]=1;[IO.File]::WriteAllBytes((Join-Path $Root '04-town-lv3-Central.png'),$Header);Reject {Get-CentralCaptureEntries $Root}
    $Header[20]=0;[IO.File]::WriteAllBytes((Join-Path $Root '04-town-lv3-Central.png'),$Header)
    $Header[0]=0;[IO.File]::WriteAllBytes((Join-Path $Root 'extra.png'),$Header);Reject {Get-CentralCaptureEntries $Root}
    Remove-Item -LiteralPath (Join-Path $Root 'extra.png')
    $Manifest=@{gitRevision=('a'*40);productFingerprint=('c'*64);images=$Entries}
    $Info=@{gitRevision=('b'*40);productFingerprint=('c'*64)}
    Reject {Assert-CentralCaptureIdentity $Info $Manifest $Root}
    $Info.gitRevision=('a'*40);Assert-CentralCaptureIdentity $Info $Manifest $Root
    [IO.File]::WriteAllBytes((Join-Path $Root '04-town-lv6-Central.png'),[byte[]]@(0,1))
    Reject {Assert-CentralCaptureIdentity $Info $Manifest $Root}
    Write-Host 'Central capture gate: Lv1/3/6 dimensions, missing/extra/altered files and source mismatch checked. Synthetic PNG headers only; no native rendering or Pixel test.'
} finally {Remove-Item -LiteralPath $Root -Recurse -Force}
