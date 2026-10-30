$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../prepare-noah-pixel-preview.ps1')
$Repository=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$ManifestRelative='References/VisualLock/noah-authored-game-candidate-2026-10-30.json'
$Record=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Repository $ManifestRelative) | ConvertFrom-Json
# Exercise the actual frozen-input gate and stored hashes. A CRLF-only Git
# checkout must pass; changing either linker rule or an image must still fail.
$XmlFiles=@($Record.protected_files | Where-Object {$_.path -like '*/link.xml'})
if($XmlFiles.Count -ne 2){throw 'Expected both protected linker inputs.'}
$Files=@($Record.protected_files | Where-Object {$_.text_normalized -and $_.path -notlike '*/link.xml'} | Select-Object -First 100)+$XmlFiles+@($Record.protected_files | Where-Object {$_.path -like '*.png'} | Select-Object -First 1)
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-noah-frozen-newlines-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $Root | Out-Null
function Assert-FrozenChangeRejected([string]$ExpectedPath){
    $Message='';try{Assert-NoahAuthoredFrozenInputs $Root}catch{$Message=$_.Exception.Message}
    if($Message -ne ('Frozen input changed: '+$ExpectedPath)){throw "Changed input not rejected at its path: $ExpectedPath ($Message)"}
}
try{
    foreach($File in $Files){
        $Target=Join-Path $Root $File.path
        New-Item -ItemType Directory -Path (Split-Path -Parent $Target) -Force | Out-Null
        [IO.File]::Copy((Join-Path $Repository $File.path),$Target)
    }
    $Record.protected_files=$Files
    $Manifest=Join-Path $Root $ManifestRelative
    New-Item -ItemType Directory -Path (Split-Path -Parent $Manifest) -Force | Out-Null
    $Record | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $Manifest -Encoding UTF8
    Assert-NoahAuthoredFrozenInputs $Root
    foreach($File in $Files){
        if($File.text_normalized -or $File.path -like '*/link.xml'){
            $Target=Join-Path $Root $File.path
            $Text=[IO.File]::ReadAllText($Target).Replace("`r`n","`n").Replace("`n","`r`n")
            [IO.File]::WriteAllText($Target,$Text,(New-Object Text.UTF8Encoding($false)))
        }
    }
    Assert-NoahAuthoredFrozenInputs $Root
    foreach($File in $XmlFiles){
        $Target=Join-Path $Root $File.path;$Original=[IO.File]::ReadAllBytes($Target)
        if(-not [IO.File]::ReadAllText($Target).Contains("`r`n")){throw 'CRLF fixture not created.'}
        [IO.File]::AppendAllText($Target,'<assembly fullname="Unexpected.Change" preserve="all" />')
        Assert-FrozenChangeRejected $File.path
        [IO.File]::WriteAllBytes($Target,$Original)
    }
    $Image=$Files | Where-Object {$_.path -like '*.png'} | Select-Object -First 1
    $ImagePath=Join-Path $Root $Image.path;$Bytes=[IO.File]::ReadAllBytes($ImagePath)
    $Bytes[0]=$Bytes[0] -bxor 1;[IO.File]::WriteAllBytes($ImagePath,$Bytes)
    Assert-FrozenChangeRejected $Image.path
    Write-Host 'Frozen-input newline checks passed: LF and CRLF accepted; both changed linker rules and changed PNG rejected. No Unity/APK/device execution.'
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
