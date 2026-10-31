$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../diagnose-characters-qa.ps1')
function RejectFocused([scriptblock]$Action){$Caught=$false;try{& $Action}catch{$Caught=$true};if(-not $Caught){throw 'Invalid focused QA input accepted.'}}
$Methods=@(Get-CharactersFocusedMethods)
$A=@(Get-CharactersFocusedArguments 'repo with spaces' 'output with spaces' ('a'*40) ('b'*64))
if('-quit' -in $A -or '-executeMethod' -in $A -or '-mofumachiCharacterAuthoredPreview' -in $A -or $A[[Array]::IndexOf($A,'-testPlatform')+1] -cne 'PlayMode' -or $A[[Array]::IndexOf($A,'-testFilter')+1] -cne ($Methods -join ';')){throw 'Focused route widened or lost exact filter.'}
RejectFocused {Get-CharactersFocusedArguments 'repo' 'out' '' ('b'*64)}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-focused-qa-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $Root | Out-Null
try{
    $Path=Join-Path $Root 'test.xml'
    function WriteFocused([string[]]$Names,[string]$Result='Passed',[int]$Failed=0,[int]$Skipped=0){
        $Cases=($Names | ForEach-Object {'<test-case fullname="'+$_+'" result="'+$Result+'" />'}) -join ''
        ('<test-run result="'+$Result+'" total="'+$Names.Count+'" passed="'+($Names.Count-$Failed-$Skipped)+'" failed="'+$Failed+'" skipped="'+$Skipped+'">'+$Cases+'</test-run>') | Set-Content -LiteralPath $Path -Encoding UTF8
    }
    WriteFocused $Methods;Assert-CharactersFocusedResults $Path
    WriteFocused $Methods[0..4];RejectFocused {Assert-CharactersFocusedResults $Path}
    WriteFocused ($Methods+@('Unrelated.Test'));RejectFocused {Assert-CharactersFocusedResults $Path}
    WriteFocused ($Methods[0..4]+@($Methods[0]));RejectFocused {Assert-CharactersFocusedResults $Path}
    WriteFocused ($Methods[0..4]+@('Wrong.Test'));RejectFocused {Assert-CharactersFocusedResults $Path}
    WriteFocused $Methods 'Failed' 1;RejectFocused {Assert-CharactersFocusedResults $Path}
    WriteFocused $Methods 'Skipped' 0 1;RejectFocused {Assert-CharactersFocusedResults $Path}
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Focused QA filter/count/duplicate/foreign/failure/skip guards passed. Synthetic XML, no native Unity or Pixel.'
