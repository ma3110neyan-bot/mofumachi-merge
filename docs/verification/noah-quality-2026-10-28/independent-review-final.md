# Independent review — remediation verification

Review base: d54812f. Reviewer made no repository edits and ran no broad/full suite.

## Disposition

Critical0; Important0 remaining; Minor0 unaddressed. The three initial Important findings are resolved in the reviewed current code:

1. Combined gait/greeting: Hand is bounded to[-1,1] and local vertical paw wave is3.1. The exact original failed triangle now has positive signed area8.200123 instead of−7.125749.
2. Formal face/tail: local head translation is removed. All tail mesh corners, including the coarse cells covering the opaque upper curl, retain zero local offset. Direction is expressed through the complete original illustration rotating around a planted paw, with positive yaw scaling and no mirroring. Idle left/right direction is distinct; intermediate smoothed facing values move continuously in the checked root transform.
3. Accepted North: current accepted-stage checkpoint rejects a legacy candidate scope before mutable path/hash bindings can reopen the boundary. Focused checkpoint fixture returned the expected exception; historical fixtures without this checkpoint remain supported.

The Minor reconstructed-Before limitation is explicitly documented in native HTML: same remaining clock, historical0.65s versus candidate1s; no equal-elapsed historical behavior claim.

## Focused independent evidence

Command:
/workspace/toolchains/powershell/7.4.6/pwsh -NoProfile -File /workspace/runtime/noah-quality-oct28/independent-review-final-numeric.ps1

This compiled the actual current math sources and checked7686 independent phase/reaction/walk/facing states using the actual mesh divisions. Actual output is preserved in independent-review-final-numeric.log. Minimum triangle signed area1.3548951; maximum planted-foot Y error2.546726E-07; maximum tail mesh-corner offset0. Checked idle yaw scales remained positive and the actual face-point X progressed from−8.2077505 at facing−1 to−5.5677642 at facing+1 through intermediate inputs.

Scope command:
/workspace/toolchains/powershell/7.4.6/pwsh -NoProfile -File /workspace/runtime/noah-quality-oct28/independent-review-fixed-scope-check.ps1

Observed exit0 and exact output:
REVIEW PASS: Accepted North checkpoint requires the Noah-only scope; previous boundary scope cannot reopen it.

Implementer related25/25 green log was inspected, not counted as a reviewer native run.

## Limits

Code and numerical review only. Native Unity rendering, candidate APK identity, Pixel visual superiority, input/performance, heat, memory and save-preserving device acceptance remain pending. No native/Pixel pass or other-five rollout approval is inferred.
