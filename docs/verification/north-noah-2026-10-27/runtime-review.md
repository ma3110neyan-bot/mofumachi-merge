# Independent runtime review — North join and Noah trial

Reviewed the exact runtime-review.patch and current corresponding implementation, plan, targeted tests, North report, and relevant existing lifecycle/render code. No suites rerun. Conclusions below distinguish source behavior from unexecuted Unity and Pixel evidence.

## Task 1: North ground

**Spec: no actionable Critical/Important discrepancy found in the reviewed source. Quality: no actionable Critical/Important discrepancy found; native and visual verdict pending.**

The overlay is bounded to North's lower ground and uses the existing Central texture. The original North and court quads remain; the diff does not change scenery placements, navigation, or world connections. The static tests check actual triangle-interpolated alpha at the reported side-lane and bank samples as well as the analytic opacity. At the contact all mesh vertices are opaque and use Central's corresponding UV; the mesh edges follow the road boundary at the tested bank heights. The prepared fixture projects actual mesh vertices after pan and restore and tests both directions at each review level. These are sampled checks, not proof of every interpolated pixel, perceived lane width, or visual blending. Native execution and same-condition imagery, actual artwork evaluation, and Pixel traversal/performance remain pending.

## Task 2: Noah town trial

**Spec: no remaining actionable Critical/Important discrepancy found in the reviewed source; native and visual verdict pending. Quality: the two Important findings below were resolved in the scoped source recheck, subject to native execution.** The town actor opts in only for the exact Noah id; other bodies default to legacy. Trial mesh uses the unchanged normal atlas crop and unmapped eyes outside two separately registered UV islands, with rigid translation above the head threshold and unchanged tail vertices. The support calculation accounts for scale and rotation of the two nominal paw points, while world position/depth sorting stays in the existing path. The CPU support test evaluates the mathematical function rather than rendered paw pixels; actual contact, silhouette and blink joins require native images. The OFF/ON fixture compares reconstructed legacy mesh and state at the same clocks, but has not run in Unity and is not a historical frame.

### Resolved Important — native talk/pause fixture reset the pose before checking it

`Assets/Mofumachi/Tests/PlayMode/Noah25DTrialPresentationTests.cs:79` disables `life` immediately after `RenderNow`. `TownLifeView.OnDisable` calls `CancelInteraction` (`Assets/Mofumachi/Presentation/TownLifeView.cs:242`), which calls each actor's `ResetVisual` (line 240), setting `VisualRoot.anchoredPosition` to zero. Consequently the next assertion at line 80 requires a value greater than .5 after it has been reset to zero. The licensed native suite cannot pass this test as written; API compilation does not execute the lifecycle. Check the talk pose while the component is enabled, then trigger the intended pause and assert reset without first disabling it.

### Resolved Important — start/stop motion had a discrete pose jump

`Assets/Mofumachi/Presentation/TownNoah25DTrial.cs:26` changes the settle term directly on the boolean `moving`; the breath term at line 28 does the same. When `walking=.5`, facing=1 and all other inputs are identical, switching moving true to false changes lean by .9 degrees and breath by .0033 instantly. `TownActor.Render` passes the discrete `moving` flag while `amplitude` is still smoothed (`Assets/Mofumachi/Presentation/TownActor.cs:166,182`), so a stop/restart while amplitude is intermediate produces that step rather than a continuous settle. The CPU test `Noah25DTrialTests.cs:65` explicitly expects the discontinuity and does not test continuity. Blend the start/stop state over time or use a continuous signal and verify adjacent samples across the transition; whether the existing jump is visibly objectionable needs native review.

## Scoped recheck after fixes

The updated fixture leaves `life` enabled through the talk assertion and then opens the menu before checking the reset. The specific premature `OnDisable` reset is removed; licensed Unity execution remains pending. The updated pose calculation uses the continuously blended walking amplitude for both settle terms and no longer branches on `moving`. The revised CPU test compares identical inputs with the flag flipped and samples blended stopping and restarting; recorded focused results report 20/20 passing. These resolve the two reported causes in source; no native execution or visual quality is inferred.

## Evidence limits

No licensed Unity, APK or Pixel run was available for this review. CPU/API checks do not establish actual visual quality, alpha rasterization, blink appearance, device performance, or user adoption. Findings are source-derived; no actual Unity execution is claimed.
