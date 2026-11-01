$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$Tool=Join-Path $PSScriptRoot '../diagnose-character-stability.ps1'
if(-not(Test-Path -LiteralPath $Tool)){throw 'Character finish focused route is missing.'}
. $Tool
function RejectStabilityFocus([scriptblock]$Action){$Caught=$false;try{& $Action}catch{$Caught=$true};if(-not $Caught){throw 'Invalid final-finish focused input accepted.'}}
$Expected=@(
    'Mofumachi.Tests.CharacterStabilityTests.SixResidentsPassAStationaryNeighbourWithoutJitterOrSpeech',
    'Mofumachi.Tests.TownHabitTests.NearbyResidentsStaySilentUntilAnExplicitTap',
    'Mofumachi.Tests.TownHabitTests.HeroPressReleaseRepeatsPreserveSpeechUntilDragging'
)
$Methods=@(Get-CharacterStabilityFocusedMethods)
if($Methods.Count -ne 3 -or @(Compare-Object $Methods $Expected).Count){throw 'Run exactly six-resident stationary passing plus the two tap regressions.'}
$Arguments=@(Get-CharacterStabilityFocusedArguments 'repo with spaces' 'output with spaces' ('a'*40) ('b'*64))
if('-quit' -in $Arguments -or '-executeMethod' -in $Arguments -or $Arguments[[Array]::IndexOf($Arguments,'-testPlatform')+1] -cne 'PlayMode' -or $Arguments[[Array]::IndexOf($Arguments,'-testFilter')+1] -cne ($Methods -join ';')){throw 'Final-finish filter is not exact.'}
RejectStabilityFocus {Get-CharacterStabilityFocusedArguments 'repo' 'out' '' ('b'*64)}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-stability-focus-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $Root | Out-Null
try{
    $Path=Join-Path $Root 'test.xml'
    function WriteStabilityFocus([string[]]$Names,[string]$Result='Passed',[int]$Failed=0,[int]$Skipped=0){
        $Cases=($Names | ForEach-Object {'<test-case fullname="'+$_+'" result="'+$Result+'" />'}) -join ''
        ('<test-run result="'+$Result+'" total="'+$Names.Count+'" passed="'+($Names.Count-$Failed-$Skipped)+'" failed="'+$Failed+'" skipped="'+$Skipped+'">'+$Cases+'</test-run>') | Set-Content -LiteralPath $Path -Encoding UTF8
    }
    WriteStabilityFocus $Methods;Assert-CharacterStabilityFocusedResults $Path
    foreach($Bad in @(@{Names=$Methods[0..1]},@{Names=($Methods+@('Unrelated.Test'))},@{Names=($Methods[0..1]+@($Methods[0]))},@{Names=($Methods[0..1]+@('Wrong.Test'))})){
        WriteStabilityFocus $Bad.Names;RejectStabilityFocus {Assert-CharacterStabilityFocusedResults $Path}
    }
    WriteStabilityFocus $Methods 'Failed' 1;RejectStabilityFocus {Assert-CharacterStabilityFocusedResults $Path}
    WriteStabilityFocus $Methods 'Skipped' 0 1;RejectStabilityFocus {Assert-CharacterStabilityFocusedResults $Path}
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Three exact stability methods and incomplete/duplicate/foreign/failure/skip rejection passed. Synthetic XML only.'
