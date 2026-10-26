# Independent whole-change review — fountain visible motion

Review base: `6bc570e241f6083375ce70c01669dcaa10539785`. Review head: working tree, including untracked runtime-support scripts, plan, candidate/Before records and received-device receipt. This is one read-only review; no repository file, index or HEAD was modified. Handoff documentation still being written was deliberately excluded as a defect. No subagents were spawned.

## Findings

Critical: 0. Important: 1. Minor: 1.

### Important I1 — settle portrait layout before freezing the crop rectangle

`Assets/Mofumachi/Tests/PlayMode/ExperienceCaptureTests.cs:234-245` (primary location: line 239).

The new test calls `EnsurePortraitCamera()`, immediately projects the fixed crop rectangle, then performs its first `Capture`. On its first invocation, `EnsurePortraitCamera` changes the canvas render mode, camera, plane distance, scale factor and viewport anchors. It does not force canvas layout or refresh the world camera. Those operations happen later, inside `Portrait()` (lines 308-319), after the rectangle has already been stored. Unity canvas layout is deferred, and this project specifically supports batch Editors whose initial Game view is landscape. Consequently the rectangle can be computed using the pre-portrait field bounds/camera fit and then applied to the final portrait for the entire minute. The mesh assertions still observe the correct water mesh, while the saved crop may clip/miss that mesh or show another changing part of the town; the gate checks only rectangle bounds and image diversity. This threatens the required reviewable continuous fountain proof or can make the expensive full QA fail on crop dimensions.

Specific fix: complete the initial portrait render/layout refresh before calculating and storing `low/high/x/y/width/height`. For example move the initial normal `Capture` before the projection and initialize the elapsed observation baseline afterwards, or explicitly force the canvas and refresh the world, then force again before projection. Assert the settled crop encloses the projected water bounds and is not merely clamped to a thin edge. Validate in actual Unity with an initially landscape Game view. This is a code-order finding; exact native pixel displacement cannot be measured here.

### Minor M1 — the new live gate accepts contradictory phase and first-show metadata

`scripts/fountain-review.ps1:23-29` (primary location: line 24), with summary validation at lines 15-16.

The gate validates `phase` only as a finite number in [0,10). Its elapsed-time consistency check covers `clock_seconds`, but never the independently recorded animation phase. It also accepts any `first_show_seconds` in [30,60] without matching it to the first full-strength show in the frames. I reproduced acceptance using the existing synthetic metadata fixture with every frame's phase changed to 0 and `first_show_seconds=60`, although its timeline enters stage 2 at 38 seconds. Output was `Real Update sequence: 121 crops / 60 seconds; first show 60 seconds`. The fixture remains synthetic; this was a gate probe, not native evidence. The implementation's actual mesh-change assertions are useful, so this gap does not by itself establish a frozen runtime fountain, but the gate does not yet reject all internally inconsistent/frozen timeline evidence it claims to reject.

Specific fix: validate animation phase progression modulo 10 against elapsed time using the initial phase and a reasonable sampling tolerance; derive or cross-check the first full-strength show timestamp from the stage/clock frame timeline. Add negative fixtures for frozen phase and inconsistent first-show time. Retain the mesh-change check, since image SHA diversity alone could come from NPCs or older water.

## Reviewed strengths / no additional defect found

- Runtime edits are confined to the two fountain water files. Core/save, navigation, scenery, fountain parent hierarchy and depth sorting are unchanged relative to this base.
- Editor/development chooses 45 seconds; nondevelopment chooses 180. The schedule remains normal until cycle−9, prelude for 2 seconds, show for 6 seconds and recovery for 1 second. Town QA builds are Development builds.
- Clock accumulation retains fractional time and removes the old .1-second active-frame cap. It handles slow active frames without repeated mesh rebuilds for catch-up; phase and show cycle are tracked separately.
- The CWT retains a clock per UIFlow through Home rebuilds. `CanAnimateWater` uses the existing Home/modal/app-pause predicate. Disabled graphics reset their realtime baseline upon enabling; app pause/resume resets baselines, and UIFlow rebuilds paused/unpaused Home. Offscreen advancement dirties no mesh; viewport refresh handles the returning visible mesh.
- Normal highlights change position down each stream; show control points animate. The mesh remains 196 quads / 784 vertices, under the existing combined 1500-vertex assertion. No product ParticleSystem, light or RenderTexture was introduced.
- The new coroutine yields real Unity frames for at least 60 seconds and calls no manual clock advance or forced stage. Existing manual OFF/ON captures remain separately labeled causal stills.
- Live water mesh changes are counted independently of the crop hashes; NPC changes are not the only asserted animation evidence. Human review is still necessary to establish perceptual motion.
- Archive inclusion covers the live JSON/HTML/crops plus the three full frames. Old Before originals remain separately identified and hash-preserved. New native/Pixel/adoption flags are false.
- Candidate changed/new/capture file SHA values matched LF-normalized current files in this review. Device feedback PDF SHA matched its receipt. `git diff --check` passed.

## Verification and explicitly declined judgments

- Read the whole tracked diff, new live gate and tests, candidate/Before records, plan and relevant unchanged runtime/capture/build dependencies. Ran only read-only repository commands and an external `/tmp` synthetic PowerShell gate probe. Did not rerun the implementer's full CPU/API/PS suites; their reported totals are not independent executions by this reviewer.
- Declined to judge actual Unity native test success, real crop positions/colors/playback, whether the water is immediately obvious at normal display size, show aesthetics or smoothness: no new native run is available and Unity licensing is unavailable here.
- Declined to judge Pixel FPS, allocations on device, thermal change, load, audio, 600-second stability, save preservation across APK update/restart, or perceived front/behind character readability: no same-version Pixel execution is available. Unchanged source and vertex budget are not device performance proof.
- Declined to diagnose the earlier user's missing-show incident from still PDF feedback; the receipt correctly limits that evidence.
- Declined to judge old native evidence as proof of this new candidate. Historical successes and user acceptance cannot be transferred to changed code.
- Declined to certify APK identity or release-build execution; none was built or received during this review. Source selection of the nondevelopment 180-second branch was reviewed only statically.
- Declined to judge focus-only OS behavior and notification overlays without native/device execution. Baseline-reset code is present, but UIFlow's paused predicate is driven by app-pause callbacks, not a focus flag.
- Declined to judge final handoff docs, not yet complete when review began, and declined final product/fountain adoption. Both require subsequent actual evidence and user acceptance.

Assessment: fix I1 before sending the full native QA run. M1 is a bounded evidence-validation improvement. No other critical/important source regression found in this one review. No rereview performed.
