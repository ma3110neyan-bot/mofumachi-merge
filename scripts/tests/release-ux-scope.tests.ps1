$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../character-finish-scope.ps1')
. (Join-Path $PSScriptRoot '../release-ux-scope.ps1')
. (Join-Path $PSScriptRoot '../prepare-release-ux-preview.ps1')
$Repo=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-ux-fixture-'+[guid]::NewGuid().ToString('N'))
function Reject([scriptblock]$Action){$Rejected=$false;try{& $Action|Out-Null}catch{$Rejected=$true};if(-not $Rejected){throw 'UX accepted an invalid scope.'}}
try{
    $Relative='References/VisualLock/release-ux-2026-11-03.json';$Good=[IO.File]::ReadAllText((Join-Path $Repo $Relative));$C=$Good|ConvertFrom-Json
    foreach($Row in @($C.overrides)+@($C.additions)){
        $Target=Join-Path $Root $Row.path;New-Item -ItemType Directory -Path (Split-Path -Parent $Target) -Force|Out-Null
        Copy-Item -LiteralPath (Join-Path $Repo $Row.path) -Destination $Target
    }
    $Manifest=Join-Path $Root $Relative;New-Item -ItemType Directory -Path (Split-Path -Parent $Manifest) -Force|Out-Null
    [IO.File]::WriteAllText($Manifest,$Good)
    if((Read-ReleaseUxOverrides $Root).Count -ne 6){throw 'Six exact font/receipt/test overrides required.'}
    foreach($Case in @('core','art','character','duplicate','baseline','native','pixel','fixed','missing-font','hash')){
        $Bad=$Good|ConvertFrom-Json
        switch($Case){
            core {$Bad.overrides[0].path='Assets/Mofumachi/Core/GameState.cs'}
            art {$Bad.overrides[0].path='Assets/Mofumachi/Resources/Mofumachi/TitleLogo.png'}
            character {$Bad.overrides[0].path='Assets/Mofumachi/Presentation/TownActor.cs'}
            duplicate {$Bad.overrides[1]=$Bad.overrides[0]}
            baseline {$Bad.overrides[0].before_sha256=('f'*64)}
            native {$Bad.native_verified=$true}
            pixel {$Bad.pixel_verified=$true}
            fixed {$Bad.formal_fixed=$true}
            missing-font {$Bad.additions=@($Bad.additions|Select-Object -Skip 1)}
            hash {$Bad.additions[0].sha256=('f'*64)}
        }
        [IO.File]::WriteAllText($Manifest,($Bad|ConvertTo-Json -Depth 8));Reject {Read-ReleaseUxOverrides $Root}
    }
    $A=@(Get-ReleaseUxArguments $Repo $Root ('a'*40) ('b'*64) ('c'*64));$Text=$A -join ' '
    if($Text -notmatch 'ReleaseUxNativeTests' -or $Text -notmatch 'StoryTypographyTests' -or $Text -notmatch '-mofumachiMergeReward' -or $Text -match '-executeMethod'){throw 'Native focus filter/output was lost.'}
    New-Item -ItemType Directory -Path (Join-Path $Root 'screenshots') -Force|Out-Null
    $Proof=[pscustomobject]@{original_png_saved=$false;images=@([pscustomobject]@{file='fixture-after.jpg'})}
    Write-MergeRewardViewer $Root (Join-Path $Root 'missing-reference.png') $Proof -ReleaseUx
    $Viewer=[IO.File]::ReadAllText((Join-Path $Root 'screenshots/merge-reward.html'))
    if($Viewer -match 'AfterはVisual合格済み' -or $Viewer -notmatch '今回の用途別フォント候補'){throw 'New UX fonts were relabelled as previously approved visuals.'}
    Assert-NoahAuthoredFrozenInputs $Repo
}finally{if(Test-Path -LiteralPath $Root){Remove-Item -LiteralPath $Root -Recurse -Force}}
Write-Host 'UX scope: exact six overrides; Core/art/motion/false acceptance/missing fonts rejected. Related native filter and historical guards passed. Fixture checks, not Unity execution.'
