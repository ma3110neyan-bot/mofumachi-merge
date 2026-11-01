$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$Tool=Join-Path $PSScriptRoot '../character-finish-scope.ps1'
if(-not(Test-Path -LiteralPath $Tool)){throw 'Scoped final-finish amendment verifier missing.'}
. $Tool
function RejectScope([scriptblock]$Action){$Caught=$false;try{& $Action | Out-Null}catch{$Caught=$true};if(-not $Caught){throw 'Invalid freeze amendment accepted.'}}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-finish-scope-'+[guid]::NewGuid().ToString('N'));$Dir=Join-Path $Root 'References/VisualLock';New-Item -ItemType Directory -Path $Dir -Force | Out-Null
try{
    $Master=Join-Path $Dir 'noah-authored-approved-2026-10-30.json';$Legacy=Join-Path $Dir 'noah-authored-game-candidate-2026-10-30.json'
    @{frozen_files=@(@{path='Assets/Mofumachi/Presentation/NoahAuthoredMotion.cs';sha256=('a'*64);text_normalized=$true},@{path='Assets/Mofumachi/Editor/NoahAuthoredBuild.cs';sha256=('f'*64);text_normalized=$true},@{path='References/Characters/original.png';sha256=('c'*64);text_normalized=$false})} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Master -Encoding UTF8
    $LatestTests=@('TownLifeTests.cs','TownMotionPresentationTests.cs','TownRegionLifeTests.cs','VisualLockPresentationTests.cs')
    $LegacyRows=@(@{path='Assets/Mofumachi/Presentation/TownWorldWalkModel.cs';sha256=('d'*64);text_normalized=$true})
    foreach($Name in $LatestTests){$LegacyRows+=@{path=('Assets/Mofumachi/Tests/PlayMode/'+$Name);sha256=('1'*64);text_normalized=$true}}
    @{protected_files=$LegacyRows} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Legacy -Encoding UTF8
    $Path=Join-Path $Dir 'character-finish-amendment-2026-11-01.json'
    $Good=@{change_authorized=$true;formal_fixed=$false;adopted=$false;historical_manifest_sha256=@{noah=(Get-FinishTextHash $Master);legacy=(Get-FinishTextHash $Legacy)};overrides=@(@{group='noah';path='Assets/Mofumachi/Presentation/NoahAuthoredMotion.cs';before_sha256=('a'*64);sha256=('b'*64)})} | ConvertTo-Json -Depth 8
    $Good | Set-Content -LiteralPath $Path -Encoding UTF8
    $Map=Read-CharacterFinishOverrides $Root 'noah';if($Map['Assets/Mofumachi/Presentation/NoahAuthoredMotion.cs'] -cne ('b'*64)){throw 'Exact authorized source amendment missing.'}
    $TestOnly=$Good | ConvertFrom-Json
    foreach($Name in $LatestTests){$TestOnly.overrides+=@{group='legacy';path=('Assets/Mofumachi/Tests/PlayMode/'+$Name);before_sha256=('1'*64);sha256=('2'*64)}}
    $TestOnly | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Path -Encoding UTF8
    $Map=Read-CharacterFinishOverrides $Root 'legacy'
    foreach($Name in $LatestTests){if($Map['Assets/Mofumachi/Tests/PlayMode/'+$Name] -cne ('2'*64)){throw 'Latest tap/name contract test amendment missing.'}}
    $HeadBuilder=$Good | ConvertFrom-Json
    $HeadBuilder.overrides=@(@{group='noah';path='Assets/Mofumachi/Editor/NoahAuthoredBuild.cs';before_sha256=('f'*64);sha256=('3'*64)})
    $HeadBuilder | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Path -Encoding UTF8
    $Map=Read-CharacterFinishOverrides $Root 'noah';if($Map['Assets/Mofumachi/Editor/NoahAuthoredBuild.cs'] -cne ('3'*64)){throw 'Head-only generated neck registration code amendment missing.'}
    foreach($Case in @('art','product','before','manifest','adopted','fixed','duplicate','path','unauthorised')){
        $Bad=$Good | ConvertFrom-Json
        switch($Case){
            art {$Bad.overrides[0].path='References/Characters/original.png';$Bad.overrides[0].before_sha256=('c'*64)}
            product {$Bad.overrides[0].path='Assets/Mofumachi/Presentation/TownLifeView.cs'}
            before {$Bad.overrides[0].before_sha256=('c'*64)}
            manifest {$Bad.historical_manifest_sha256.noah=('e'*64)}
            adopted {$Bad.adopted=$true}
            fixed {$Bad.formal_fixed=$true}
            duplicate {$Bad.overrides+=@($Bad.overrides[0])}
            path {$Bad.overrides[0].path='../NoahAuthoredMotion.cs'}
            unauthorised {$Bad.change_authorized=$false}
        }
        $Bad | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Path -Encoding UTF8;RejectScope {Read-CharacterFinishOverrides $Root 'noah'}
    }
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Exact authorized-code amendments accepted; art, history tampering, false adoption and unrelated paths rejected. Synthetic scope checks only.'
