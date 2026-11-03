$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../character-finish-scope.ps1')
. (Join-Path $PSScriptRoot '../merge-reward-scope.ps1')
function Reject([scriptblock]$Action){$Caught=$false;try{& $Action | Out-Null}catch{$Caught=$true};if(-not $Caught){throw 'Merge scope admitted an invalid change.'}}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-merge-scope-'+[guid]::NewGuid().ToString('N'))
function Put([string]$Path,[string]$Text){$File=Join-Path $Root $Path;New-Item -ItemType Directory -Path (Split-Path -Parent $File) -Force | Out-Null;[IO.File]::WriteAllText($File,$Text);return Get-FinishTextHash $File}
try{
    $Paths=@('QuestMergeScreens.cs','MergePerspective.cs','MergeCellSurfaceGraphic.cs','MergeSparkleGraphic.cs')|ForEach-Object {'Assets/Mofumachi/Presentation/'+$_}
    $Paths+=@('Assets/Mofumachi/Tests/PlayMode/MergeInteractionTests.cs','Assets/Mofumachi/Tests/PlayMode/ExperienceMergeTests.cs','Assets/Mofumachi/Tests/PlayMode/MergePolishTests.cs','Assets/Mofumachi/Tests/PlayMode/DailyWorldTests.cs','Assets/Mofumachi/Tests/PlayMode/MajorPolishTests.cs')
    $Rows=@(foreach($Path in $Paths){@{path=$Path;sha256=(Put $Path 'old');text_normalized=$true}})
    $History=Put 'References/VisualLock/noah-authored-game-candidate-2026-10-30.json' (@{protected_files=$Rows}|ConvertTo-Json -Depth 6)
    $Protected=@(@{path='Assets/Mofumachi/Core/MergeBoard.cs';sha256=(Put 'Assets/Mofumachi/Core/MergeBoard.cs' 'unchanged');text_normalized=$true})
    $Before=Put 'docs/verification/merge-reward-before-2026-11-02.json' (@{protected_core_geometry_input_and_formal_item_files=$Protected}|ConvertTo-Json -Depth 6)
    $Overrides=@(foreach($Row in $Rows){@{path=$Row.path;before_sha256=$Row.sha256;sha256=(Put $Row.path 'new')}})
    $Added=@(foreach($Name in @('MergeRewardPresentation.cs','MergeRewardPresentation.cs.meta','MergeRewardView.cs','MergeRewardView.cs.meta')){$Path='Assets/Mofumachi/Presentation/'+$Name;@{path=$Path;sha256=(Put $Path 'new')}})
    $Path=Join-Path $Root 'References/VisualLock/merge-reward-ui-2026-11-02.json'
    $Reference=Put 'References/Merge/merge_visual_lock_reward.png' 'synthetic-original'
    $Good=@{kind='merge-reward-ui-candidate';change_authorized=$true;formal_fixed=$false;adopted=$false;native_verified=$false;pixel_verified=$false;original_png_saved=$true;reference_sha256=$Reference;historical_manifest_sha256=$History;before_snapshot_sha256=$Before;overrides=$Overrides;additions=$Added}|ConvertTo-Json -Depth 8
    [IO.File]::WriteAllText($Path,$Good);$Result=Read-MergeRewardOverrides $Root; if($Result.Count -ne 9){throw 'Exactly four UI files and five related tests expected.'}
    foreach($Case in @('core','art','character','unrelated-test','before','duplicate','native','fixed','history','baseline','missing','helper','reference','reference-claim','protected')){
        $Bad=$Good|ConvertFrom-Json
        switch($Case){
            core {$Bad.overrides[0].path='Assets/Mofumachi/Core/MergeBoard.cs'}
            art {$Bad.overrides[0].path='Assets/Mofumachi/Resources/Mofumachi/ItemAtlas.png'}
            character {$Bad.overrides[0].path='Assets/Mofumachi/Presentation/NoahAuthoredGraphic.cs'}
            unrelated-test {$Bad.overrides[0].path='Assets/Mofumachi/Tests/PlayMode/QuestUiTests.cs'}
            before {$Bad.overrides[0].before_sha256=('d'*64)}
            duplicate {$Bad.overrides[1]=$Bad.overrides[0]}
            native {$Bad.native_verified=$true}
            fixed {$Bad.formal_fixed=$true}
            history {$Bad.historical_manifest_sha256=('d'*64)}
            baseline {$Bad.before_snapshot_sha256=('d'*64)}
            missing {$Bad.additions=$Bad.additions[1..3]}
            helper {$Bad.additions[0].sha256=('d'*64)}
            reference {$Bad.reference_sha256=('d'*64)}
            reference-claim {$Bad.original_png_saved=$false}
            protected {$null=Put 'Assets/Mofumachi/Core/MergeBoard.cs' 'changed'}
        }
        [IO.File]::WriteAllText($Path,($Bad|ConvertTo-Json -Depth 8));Reject {Read-MergeRewardOverrides $Root}
    }
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Merge scope: four UI overrides plus five related tests only; art/Core/characters/history/pending acceptance protected. Synthetic checks.'
