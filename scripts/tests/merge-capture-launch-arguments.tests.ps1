$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../prepare-merge-reward-preview.ps1')
function RequireCaptureOutput([object[]]$Arguments,[string]$Expected){
    $Indexes=@(for($I=0;$I -lt $Arguments.Count;$I++){if($Arguments[$I] -ceq '-mofumachiMergeReward'){$I}})
    if($Indexes.Count -ne 1 -or $Indexes[0]+1 -ge $Arguments.Count -or $Arguments[$Indexes[0]+1] -cne $Expected){throw 'The existing launcher must supply one explicit, separate capture directory.'}
}
$Root=Join-Path ([IO.Path]::GetTempPath()) 'merge-capture-argument-test'
$A=@(Get-CharactersFullArguments 'repo' $Root 'PlayMode' ('a'*40) ('b'*64))
RequireCaptureOutput $A (Join-Path $Root 'full-merge-capture')
$Tokens=$null;$Errors=$null;$BuildFile=Join-Path $PSScriptRoot '../build-android.ps1'
$Ast=[Management.Automation.Language.Parser]::ParseFile($BuildFile,[ref]$Tokens,[ref]$Errors)
if($Errors.Count){throw 'Build script parse failed.'}
$Calls=@($Ast.FindAll({param($N) $N -is [Management.Automation.Language.CommandAst] -and $N.GetCommandName() -ceq 'Invoke-Unity' -and $N.Extent.Text.Contains('$PlayXml')},$true))
if($Calls.Count -ne 1){throw 'Expected the one existing full PlayMode invocation.'}
# Evaluate only its literal argument-array expression. No Unity/build command runs.
$ArgumentText=$Calls[0].CommandElements[1].Extent.Text
$A=@(& {param($Text,$Root) $ProjectPath='repo';$PlayXml=Join-Path $Root 'playmode.xml';$OutputDir=$Root;$GitRevision='a'*40;$ProductFingerprint='b'*64; & ([scriptblock]::Create($Text))} $ArgumentText $Root)
RequireCaptureOutput $A (Join-Path $Root 'merge-reward-capture')
Write-Host 'Capture output arguments in existing launchers verified by argument generation only; no test suite, Unity or build executed.'
