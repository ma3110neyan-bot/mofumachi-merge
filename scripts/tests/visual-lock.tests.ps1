$ErrorActionPreference='Stop'
$Gate=Join-Path $PSScriptRoot '../visual-lock.ps1'
if(-not(Test-Path -LiteralPath $Gate)){throw 'Visual Lock gate is missing.'}
. $Gate
function Reject([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Frozen input change was accepted.'}}
$Fixture=Join-Path ([IO.Path]::GetTempPath()) ('mofu-visual-lock-'+[guid]::NewGuid())
New-Item -ItemType Directory -Path $Fixture | Out-Null
try {
    $File=Join-Path $Fixture 'title.cs';$Manifest=Join-Path $Fixture 'lock.json'
    $Before="TITLE`nkept`nHOME`nchangeable`n"
    [IO.File]::WriteAllText($File,$Before)
    $Entries=@(@{path='title.cs';mode='section';start='TITLE';end='HOME';sha256=(Get-VisualLockDigest ([Text.Encoding]::UTF8.GetBytes("TITLE`nkept`n")))})
    @{files=$Entries} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Manifest -Encoding UTF8
    Assert-BaseVisualLock $Fixture $Manifest
    [IO.File]::WriteAllText($File,$Before.Replace('changeable','new town').Replace("`n","`r`n"))
    Assert-BaseVisualLock $Fixture $Manifest
    [IO.File]::WriteAllText($File,$Before.Replace('kept','regressed'))
    Reject {Assert-BaseVisualLock $Fixture $Manifest}
    [IO.File]::WriteAllText($File,$Before.Replace('HOME','removed'))
    Reject {Assert-BaseVisualLock $Fixture $Manifest}
    [IO.File]::WriteAllBytes($File,[byte[]]@(0,1,255,128))
    $Entries=@(@{path='title.cs';mode='binary';sha256=(Get-VisualLockDigest ([IO.File]::ReadAllBytes($File)))})
    @{files=$Entries} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Manifest -Encoding UTF8
    Assert-BaseVisualLock $Fixture $Manifest
    [IO.File]::WriteAllBytes($File,[byte[]]@(0,2,255,128))
    Reject {Assert-BaseVisualLock $Fixture $Manifest}
    $Entries[0].path='../outside';@{files=$Entries} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Manifest -Encoding UTF8
    Reject {Assert-BaseVisualLock $Fixture $Manifest}
    Remove-Item -LiteralPath $File
    $Entries[0].path='title.cs';@{files=$Entries} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Manifest -Encoding UTF8
    Reject {Assert-BaseVisualLock $Fixture $Manifest}
    Assert-BaseVisualLock (Join-Path $PSScriptRoot '../..') (Join-Path $PSScriptRoot '../../References/VisualLock/base-2026-10-21.json')
    $Before=Join-Path $Fixture 'before';$After=Join-Path $Fixture 'after';$Reports=Join-Path $Fixture 'reports'
    foreach($Directory in @($Before,$After,$Reports,(Join-Path $Fixture 'References/VisualLock'))){New-Item -ItemType Directory -Path $Directory -Force | Out-Null}
    $Entries=@(foreach($Name in @('04-town-lv6-Central.png','02-merge-all-items.png')) {
        $Bytes=[Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a1ZkAAAAASUVORK5CYII=')
        [IO.File]::WriteAllBytes((Join-Path $Before $Name),$Bytes);[IO.File]::WriteAllBytes((Join-Path $After $Name),$Bytes)
        @{file=$Name;sha256=(Get-VisualLockDigest $Bytes)}
    })
    @{run='before-run';build=@{gitRevision='before'};capture=@{images=$Entries}} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $Fixture 'References/VisualLock/baseline-native-2026-10-21.json')
    @{gitRevision='after';productFingerprint='same-source';sha256='apk-hash'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $After 'build-info.json')
    @{gitRevision='after';productFingerprint='same-source';images=$Entries} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $After 'capture-manifest.json')
    Write-VisualLockComparison $Fixture $After $Before $Reports
    $Pending=Get-Content -LiteralPath (Join-Path $Reports 'visual-adoption.json') -Raw | ConvertFrom-Json
    if($Pending.adopted -or $Pending.pixel_verified -or $Pending.native_pairs_verified -ne 2){throw 'Comparison alone was treated as adoption.'}
    Reject {Assert-BaseAdoption (Join-Path $Reports 'visual-adoption.json')}
    foreach($Axis in @('visual','input','information','world','fps','pixel_load','pixel_verified')){$Pending.$Axis=$true}
    $Pending | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Reports 'visual-adoption.json')
    Reject {Assert-BaseAdoption (Join-Path $Reports 'visual-adoption.json')}
    Set-Content -LiteralPath (Join-Path $After '02-merge-all-items.png') 'changed image'
    Reject {Write-VisualLockComparison $Fixture $After $Before $Reports}
    Write-Host 'Visual Lock: frozen title/bytes/missing/unsafe paths rejected; Home-only changes and CRLF accepted; real baseline intact.'
} finally {Remove-Item -LiteralPath $Fixture -Recurse -Force}
