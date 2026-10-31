$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../capture-character-authored.ps1')
if(-not(Get-Command Assert-CharacterFinalGrounding -ErrorAction SilentlyContinue)){throw 'Final contact-shadow evidence gate is missing.'}
function RejectFinal([scriptblock]$Action){$Caught=$false;try{& $Action}catch{$Caught=$true};if(-not $Caught){throw 'Invalid contact-shadow proof accepted.'}}
$Pairs=@(foreach($Mode in @('Left','Front','Back','Left','Right')){
    $P=@{mode=$Mode;comparison_kind='accepted-2.5D-shadow-vs-final-shadow-same-instant';changed_pixels=7;outside_allowed_pixels=0}
    foreach($Flag in @('frame_unchanged','actors_unchanged','other_five_unchanged','camera_unchanged','water_clock_unchanged','water_phase_unchanged','state_unchanged','store_writes_unchanged','world_model_unchanged')){$P[$Flag]=$true};$P
})
$Good=@{grounding_verified=$true;grounding_pairs=$Pairs} | ConvertTo-Json -Depth 8
Assert-CharacterFinalGrounding ($Good | ConvertFrom-Json) 'momo'
Assert-CharacterFinalGrounding ($Good | ConvertFrom-Json) 'miel'
foreach($Kind in @('missing','unverified','wrong-kind','missing-direction','outside','no-change','changed-frame','changed-model','missing-pair')){
    $P=$Good | ConvertFrom-Json
    switch($Kind){
        missing {$P.PSObject.Properties.Remove('grounding_pairs')}
        unverified {$P.grounding_verified=$false}
        wrong-kind {$P.grounding_pairs[0].comparison_kind='2D-vs-25D'}
        missing-direction {$P.grounding_pairs[4].mode='Left'}
        outside {$P.grounding_pairs[0].outside_allowed_pixels=1}
        no-change {$P.grounding_pairs[0].changed_pixels=0}
        changed-frame {$P.grounding_pairs[0].frame_unchanged=$false}
        changed-model {$P.grounding_pairs[0].world_model_unchanged=$false}
        missing-pair {$P.grounding_pairs=$P.grounding_pairs[0..3]}
    }
    RejectFinal {Assert-CharacterFinalGrounding $P 'momo'}
}
Write-Host 'Final grounding proof gates passed (synthetic metadata; no native rendering).'
