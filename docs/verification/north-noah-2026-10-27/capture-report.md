# North / Noah native evidence preparation — 2026-10-27

Implemented in `/workspace/mofumachi-merge`; no commit, production save, fountain runtime, navigation, manifest, stage JSON or visual-lock edits by this agent. Licensed Unity rendering, APK generation and Pixel review have not run in this cloud.

## Native capture fixture

Added one normal-suite UnityTest: `ExperienceCaptureTests.NorthNoahBeforeAfterAndFixedStepEvidencePreserveSavedState`.

- North: Lv1/3/6 × camera `(0,.85)`, `(0,1)`, `(0,1.15)`; actual same-instant renderer OFF/ON pairs. Before is explicitly a **reconstructed causal baseline**, never an old runtime capture. Pixel changes must be nonzero and remain within projected world x[-.6,.6], y[1,1.32], with a three-pixel raster guard.
- Each pair asserts unchanged Unity frame, actor roots, world resident state, camera center/follow mode, fountain show clock, both water phases, complete encoded GameState and main TestStore writes. North also keeps all actor visual transforms/meshes; Noah keeps other-five body vertices, UVs, colors and visual transforms unchanged.
- Actual `GuidePoint`/`TownLifeView.AdvanceFrame` walking reaches Central→North→Central and x±.30 broad seam lanes y.95→1.05→.95. Every sampled position and small travel segment is walkable; no Restore/FastTravel rescue or navigation edits. JSON records start/target, steps, distance and chronological model positions.
- Noah: actual idle/walk/greet states, six full PNG originals, same-instant legacy/current switches; 48 cropped native frames show idle→start walk→walk→stop→greet. Manual fixed steps are explicitly labeled 1/30s, sampled every three steps. A real walk to the original Momo resident precedes the normal `TryBeginConversation` greeting; its elapsed gap remains recorded. This is not real-time/FPS/Pixel evidence.
- Twelve browser comparison `.jpg` files derive directly from each exact native PNG pair: Before left / After right, 1440×1280, JPEG quality90. Both original PNGs remain the lossless pixel proof. HTML links and filenames label the sides.
- One actual historical North PNG is copied unchanged from `References/Review/fountain-final-2026-10-27-candidate/native/09-connected-path-north.png`, SHA256 `b001ee0b7bb6a1719876b16c951a9bdcf5c58df3d7817dc797b379cf0c863324`. Its different bb3fb82/run102109 runtime conditions are labeled as historical context only.

Expected new output: 30 candidate full PNGs, one historical North PNG, 12 comparison JPEGs and 48 cropped PNGs. Existing219 full PNGs therefore become minimum250 in TownQa; the old fountain prior originals and real-time crops remain separately required. No new debug UI or user command is introduced.

## Save semantics checked after parent review

`state_unchanged` and `store_writes_unchanged` refer to the complete main **GameState** and fixture **TestStore**. Normal walking callbacks continue to update the separate isolated Daily journal. They are not suppressed or pre-recorded.

Evidence: `UIFlowController.RecordCharacterOuting`/`RecordWorldWalk` call only `Daily.RecordOuting`/`RecordWalk`; `TownDailyService` has no GameState/economic/main-save dependency and saves only its `ITownDailyStore`. `SliceUiTestFixture` injects a separate `MemoryTownDailyStore` at line28 and a main `TestStore(SaveService)` at line30. Existing `DailyWorldTests.ActualWalkingHasNoFadeAndDailyCountsOnlyItsCompletedArrival` independently asserts actual arrival updates Daily.Walks while GameState/main-write count stay equal. Thus unchanged main-save assertions during these walks are deliberate and correct; they do not claim the Daily journal is frozen across walking.

## TownQa integration

`build-android.ps1` passes source SHA/product fingerprint into the Unity fixture; the emitted `north-noah-comparison.json` is checked against the same test/build identity. Existing boundary/field/growth/fountain/show/live gates remain. PlayMode minimum234 adds North3, Noah4 and capture1 to the previous226; the two dedicated native suite counts are enforced exactly.

Both real original-base checks now use the gate agent's `Assert-OriginalBaseForCandidate`, preserving the original354 policy with only the approved body exception after full candidate/freeze validation.

The existing ZIP writer includes JSON/HTML, all12 JPGs and all48 crops. A synthetic packaging test confirms no omitted or collided files under its existing flattened archive convention; the HTML player supports both run-folder and extracted flat crop paths.

## Verification

- Explicit gate RED observed before implementation: missing native comparison validator threw its unimplemented error.
- JPEG requirement RED observed: deleting comparison metadata was incorrectly accepted, causing `Expected north/Noah evidence rejection`; preserved in `capture-jpeg-red.log`.
- Final evidence/packaging gate GREEN, exit0: `capture-gate-green.log`. Synthetic PNG/JPEG headers test metadata/parser behavior only; the copied historical PNG is an actual unchanged prior original. Negative cases cover source mismatch, missing pairs/views/modes, invariants, out-of-scope pixels, seam endpoints, sequence chronology/frozen model movement, unsafe/missing/tampered files, JPEG encoding magic/dimensions and false Pixel/adoption/navigation/rescue claims.
- Six Unity API assemblies compiled with0 warnings/errors in `capture-api.log`; this caught capture integration against actual Unity APIs but is not licensed rendering. Final post-JPEG PlayMode API compile is0 warnings/errors in `capture-api-playmode-final.log`. Parent owns the final all-source pass after Noah's later helper adjustments.
- Existing fountain-live metadata gate GREEN (`capture-fountain-live-green.log`); Android mock-command suite GREEN before the minimum adjustment (`capture-android-tools-green.log`). Gate agent separately confirmed the updated234 Android fixture GREEN and owns the final complete PowerShell rerun after parent candidate rebind.
- `git diff --check` passed.

Next execution remains the existing Windows `scripts/build-android.ps1 -TownQa`; actual images/JSON/HTML/JPEGs and same-source APKs must be reviewed before any new North/Noah candidate adoption. Current fountain approval is preserved; earlier unmeasured device FPS/heat/load remain unverified.

## Final parent-review correction: actual mutual greeting

The original sequence incorrectly requested `Pause(id,300)` even though the existing model caps a pause at four seconds; its generic `IsSpeaking` check also could accept a solo ambient line. Both gaps were corrected only in the capture fixture and its metadata validator, with no runtime/save/navigation changes.

Every fixed sequence step now renews the real `Pause(id,4)` for each NPC and asserts all their original positions remain unchanged. After actual walking arrival, `StartInteraction` clears prior bubbles. The fixture attempts the public `TryBeginConversation(noah,momo)` and steps for a bounded twelve seconds if the normal automatic social path wins its cooldown. Success requires a Noah/Momo speaker and partner, the exact normal `こんにちは、…♪` greeting, positive reaction timers on **both** actors, and mutual model facing. The talk pair rechecks that signature after its first three steps. A generic `IsSpeaking` result alone cannot pass.

JSON now records `actual_conversation_verified`, the explicit mutual-greeting mode/trigger, actual speaker/partner/text, both reaction timers and both facing values. The PowerShell gate rejects missing/false verification, ambient-only mode/text/trigger, a third speaker, absent reactions and incorrect facing, and accepts either legitimate Noah/Momo speaker order.

Observed RED: the old gate accepted a false verification flag (`capture-conversation-red.log`). Final focused gate/packaging GREEN exited0 (`capture-conversation-green.log`); final PlayMode API compile exited0 with0 warnings/errors (`capture-api-conversation.log`). `git diff --check` passed. Native rendering and the final actual greeting images remain pending licensed Unity execution.
