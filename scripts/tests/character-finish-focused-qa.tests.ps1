$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$Tool=Join-Path $PSScriptRoot '../diagnose-character-finish-qa.ps1'
if(-not(Test-Path -LiteralPath $Tool)){throw 'Character finish focused route is missing.'}
. $Tool
function RejectFinishFocus([scriptblock]$Action){$Caught=$false;try{& $Action}catch{$Caught=$true};if(-not $Caught){throw 'Invalid final-finish focused input accepted.'}}
$Expected=@(
    'Mofumachi.Tests.TownHabitTests.NamesBubblesAndFacilitiesStayInsideSafeArea',
    'Mofumachi.Tests.TownLifeTests.NpcsVisitSeveralStreetStopsWithoutChangingProgress',
    'Mofumachi.Tests.TownMotionPresentationTests.StationaryDragAndLifecycleStopGait',
    'Mofumachi.Tests.TownRegionLifeTests.TwoResidentsMeetAndResumeInVisibleRegion',
    'Mofumachi.Tests.VisualLockPresentationTests.CloseResidentLabelsNeverCoverAnotherCharactersBody',
    'Mofumachi.Tests.TownHabitTests.NearbyResidentsStaySilentUntilAnExplicitTap',
    'Mofumachi.Tests.TownHabitTests.HeroPressReleaseRepeatsPreserveSpeechUntilDragging'
)
$Methods=@(Get-CharacterFinishFocusedMethods)
if($Methods.Count -ne 7 -or @(Compare-Object $Methods $Expected).Count){throw 'Run exactly five failures plus tap/repeat regressions.'}
$Arguments=@(Get-CharacterFinishFocusedArguments 'repo with spaces' 'output with spaces' ('a'*40) ('b'*64))
if('-quit' -in $Arguments -or '-executeMethod' -in $Arguments -or $Arguments[[Array]::IndexOf($Arguments,'-testPlatform')+1] -cne 'PlayMode' -or $Arguments[[Array]::IndexOf($Arguments,'-testFilter')+1] -cne ($Methods -join ';')){throw 'Final-finish filter is not exact.'}
RejectFinishFocus {Get-CharacterFinishFocusedArguments 'repo' 'out' '' ('b'*64)}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-finish-focus-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $Root | Out-Null
try{
    $Path=Join-Path $Root 'test.xml'
    function WriteFinishFocus([string[]]$Names,[string]$Result='Passed',[int]$Failed=0,[int]$Skipped=0){
        $Cases=($Names | ForEach-Object {'<test-case fullname="'+$_+'" result="'+$Result+'" />'}) -join ''
        ('<test-run result="'+$Result+'" total="'+$Names.Count+'" passed="'+($Names.Count-$Failed-$Skipped)+'" failed="'+$Failed+'" skipped="'+$Skipped+'">'+$Cases+'</test-run>') | Set-Content -LiteralPath $Path -Encoding UTF8
    }
    WriteFinishFocus $Methods;Assert-CharacterFinishFocusedResults $Path
    foreach($Bad in @(@{Names=$Methods[0..5]},@{Names=($Methods+@('Unrelated.Test'))},@{Names=($Methods[0..5]+@($Methods[0]))},@{Names=($Methods[0..5]+@('Wrong.Test'))})){
        WriteFinishFocus $Bad.Names;RejectFinishFocus {Assert-CharacterFinishFocusedResults $Path}
    }
    WriteFinishFocus $Methods 'Failed' 1;RejectFinishFocus {Assert-CharacterFinishFocusedResults $Path}
    WriteFinishFocus $Methods 'Skipped' 0 1;RejectFinishFocus {Assert-CharacterFinishFocusedResults $Path}
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Seven exact final-finish methods and incomplete/duplicate/foreign/failure/skip rejection passed. Synthetic XML only.'
