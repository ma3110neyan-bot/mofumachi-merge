param([string]$Launcher=(Join-Path $PSScriptRoot '../probes/invoke-skip-p6-serialization-probe.ps1'))
$ErrorActionPreference='Stop'
if(-not(Test-Path -LiteralPath $Launcher)){throw 'FAIL: skip-P6 control classifier is not implemented.'}
$Tokens=$null;$Errors=$null
$Ast=[Management.Automation.Language.Parser]::ParseFile((Resolve-Path -LiteralPath $Launcher),[ref]$Tokens,[ref]$Errors)
if($Errors.Count){throw 'FAIL: launcher does not parse.'}
$Function=$Ast.FindAll({param($Node) $Node -is [Management.Automation.Language.FunctionDefinitionAst] -and $Node.Name -eq 'Classify-P6Control'},$true)
if($Function.Count -ne 1){throw 'FAIL: skip-P6 control classifier is not implemented.'}
. ([scriptblock]::Create($Function[0].Extent.Text))
$BaselineHash='d334127167318fe7278575eb17fe2621ab2c0e39c3ab2c53b7c1f39cc555e7fc'
$MutatedHash='266f17baf9d7101401580553aab9d0ede0390f6c859e137e31f6274d4713f07a'
function Stage([string]$Id){return [pscustomobject]@{stage=$Id;sha256=$BaselineHash;byte_size=24343;m_APIs='0b000000';api_raw_hex='30 62 30 30 30 30 30 30';android_keystore_name='<empty>';metadata_stable=$true;automatic='0'}}
function Fixture {
    $Stages=@('P0','P1','P2','P3a','P3b','P3c','P4a','P4b','P4c','P4d','P4e','P5a','P5b','P5c','P5d','P5e','P5') | ForEach-Object {Stage $_}
    return [pscustomobject]@{
        Report=[pscustomobject]@{baseline=(Stage 'B0');stages=@($Stages);completed_stages=$true;original_signing_restored=$true;unity_invocations=1;save_assets_calls=1;graphics_setters=0;graphics_restore_setters=0;non_graphics_setters=3;signing_setting_setters=5;signing_restore_setters=5;build_player_calls=0;aab_count=0;apk_count=0;automatic_retries=0}
        E0=(Stage 'E0');Skip=[pscustomobject]@{p6_skipped=$true;after_stage='P5'};Exit=0
    }
}
$Total=0
function Check([string]$Name,$Fixture,[string]$Expected){
    $Actual=Classify-P6Control $Fixture.Report $Fixture.E0 $Fixture.Skip $Fixture.Exit
    if($Actual -cne $Expected){throw ('FAIL '+$Name+': expected '+$Expected+', got '+$Actual)}
    $script:Total++;Write-Host ('PASS '+$Name)
}
$F=Fixture;Check 'A: complete control retains exact baseline at exit' $F 'Case A'
$F=Fixture;$F.E0.sha256=$MutatedHash;$F.E0.byte_size=23513;$F.E0.m_APIs='0b000000i';$F.E0.api_raw_hex+=' 69';$F.E0.android_keystore_name="'{inproject}: '";Check 'B: suffix i first appears at process exit' $F 'Case B'
$F=Fixture;$F.Report.stages[4].sha256=$MutatedHash;$F.Report.completed_stages=$false;$F.Exit=1;Check 'C: pre-P5 mutation wins over aborted protocol' $F 'Case C'
$F=Fixture;$F.E0.sha256=$MutatedHash;$F.E0.android_keystore_name="'{inproject}: '";Check 'Unclassified: key-only exit mutation is not baseline-preserving A' $F 'Inconclusive'
$F=Fixture;$F.Report.stages=@($F.Report.stages | Where-Object {$_.stage -ne 'P4c'});Check 'Unclassified: missing stage cannot pass' $F 'Inconclusive'
$F=Fixture;$F.Skip=$null;Check 'Unclassified: missing explicit skip evidence cannot pass' $F 'Inconclusive'
$F=Fixture;$F.Exit=1;Check 'Unclassified: failed Unity exit cannot pass' $F 'Inconclusive'
$F=Fixture;$F.Report.save_assets_calls=2;Check 'Unclassified: extra SaveAssets invalidates control' $F 'Inconclusive'
$F=Fixture;$F.Report.stages+=Stage 'P6';Check 'Unclassified: any P6 record invalidates control' $F 'Inconclusive'
$F=Fixture;$F.E0.metadata_stable=$false;Check 'Unclassified: unstable byte read cannot pass' $F 'Inconclusive'
$F=Fixture;$F.E0.sha256=$MutatedHash;$F.E0.byte_size=23513;Check 'Unclassified: same APIs and empty key with different bytes is not A' $F 'Inconclusive'
$F=Fixture;$F.E0.api_raw_hex+=' 69';Check 'Unclassified: raw bytes must agree with API value' $F 'Inconclusive'
Write-Host ('TOTAL='+$Total+' PASS='+$Total+' FAIL=0; data-only; no Git or Unity execution.')
