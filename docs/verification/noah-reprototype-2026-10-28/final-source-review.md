# Noah reprototype source review — 2026-10-28

Scope: current uncommitted working tree of `/workspace/mofumachi-merge`, feature branch `feat/daily-world-pass-20261016`, base `0fe11b3`; includes new helper, parser, spec, plan and candidate. Read-only review: no repository/index/HEAD changes, no Unity or whole-suite execution. Report created before coordinator fixes below.

**Assessment: fix the three concrete capture issues below before the one minimal native reprototype capture.** No additional runtime blocker identified. This is not aesthetic approval, native execution evidence, Pixel readiness, adoption, other-five rollout or main integration approval.

## Findings

### P1 — New relief/yaw crosses the existing portrait camera near plane

- Location: `Assets/Mofumachi/Tests/PlayMode/ExperienceCaptureTests.cs:810`–`:811`, interacting with `Assets/Mofumachi/Presentation/TownActor.cs:206`–`:207` and new mesh Z.
- The existing portrait camera is orthographic, size320, target720×1280, Canvas scaleFactor2, canvas planeDistance10, nearClipPlane0.1. Its ScreenSpaceCamera canvas has one world unit per local UI unit at these settings. This previously safe planar setup has only9.9 units of depth toward the camera.
- With current real viewport scale1.10, worldY≈−.25 gives actor scale1.1055. Yaw−18° plus relief at grid(u=.303,v=.28) produces relative Z≈−10.22, placing that body vertex behind the near plane; left-facing yaw gives relative Z≈−12.17 at u=1. The alpha at atlas position corresponding to(.303,.28) is253; outer-tail points(.92,.85)/(.95,.85) have alpha252/251. This is not only transparent gutter geometry. Breathing changes these figures slightly, insufficient to remove the crossing.
- Consequence: native evidence can clip the body/outer tail in walking/turning states even though the XY triangle-orientation source tests pass. A native run was not used to claim observed clipping, but the current camera and transform math violate its clip-volume requirement.
- Smallest fix: give this new method a deeper orthographic canvas plane (e.g30 within current far100), preserving projected XY, and assert camera-space depth of every transformed Noah mesh vertex is between near/far before capture. Keep historic camera behavior outside the new method if historical reproducibility is required. Do not reduce the approved art to accommodate the fixture.

### P2 — The parser rejects the fixture's legitimate final arrival sample

- Location: `scripts/noah-reprototype-review.ps1:50`, paired with `ExperienceCaptureTests.cs:277`–`:294`.
- Parser requires every frame index>=30 to have walking=false. The capture does not require arrival until index32, after6.4 simulated seconds. Coordinator's targeted model check confirms index29/6.0s walking=true, index30/6.2s walking=true at X=.13883431, and index31/6.4s walking=false at X=.14. Therefore successful native output from the defined route will fail the post-capture parser at frame30.
- Smallest fix: allow arrival motion at frame30, require stopped from frame31 and throughout actual speech, and retain the exact .2s continuity checks. Add a positive parser fixture with frame30 walking=true, plus rejection if still walking at31. Describe30 as arrival/settle or otherwise keep its label honest.

### P2 — Explicit UnityEditorPath is validated but not launched

- Location: `scripts/capture-noah-reprototype.ps1:27`–`:33`; reused launcher `scripts/build-android.ps1:239`.
- The helper selects and validates `$Editor`, but `Invoke-Unity` launches `$UnityEditor`, which the dot-sourced build script initializes to its default. A valid custom `-UnityEditorPath` is therefore ignored; custom-only installs fail and machines with both paths can launch the wrong editor.
- Smallest fix: assign the validated/resolved selection to `$UnityEditor` before invoking the existing wrapper. Verify the path passed by the real wrapper to a mocked Start-Process with a nondefault path containing spaces; no Unity execution is needed.

## Reviewed protections and evidence

- Independently compared PreviousSample, PreviousVertexOffset and PreviousSupportTranslation with1d11629 using method extraction and name normalization: all three match exactly. The old624-vertex white/Z0 grid, previous root scale/roll and grounded two-shadow values are preserved on reprototype OFF. Historical trial OFF still reaches the untouched legacy2D mesh/math.
- Original atlas bytes have no diff against1d11629. Independently recomputed all candidate `changes`, `new_files`, and `capture_inputs` LF hashes: all match the reviewed working tree.
- New relief/neutral-light paths are behind exact Noah gating. Lighting is same-channel RGB within.78–1, alpha remains graphic alpha, eye registration uses existing atlas offsets. Extra columns include actual paw coordinates. No UV mirroring or artwork edits were introduced.
- Support translation uses yaw then roll in the same order as the actual quaternion product and accounts for breathing scale; the reference paw columns have zero relief. World-foot-Y sorting remains unchanged. Soft shadow is a noninteractive child of the actor root, independent of the bob/lean/yaw visual child; OFF disables it and enables old shadow graphics.
- Source scene uses a real public Noah-first conversation, verifies actual speaker and text, and discloses isolated automatic-social control. Sequence contains48 consecutive six-step samples at1/30s each, with no mid-sequence reset/cut. Sorting examples explicitly restore positions and are described as display-only.
- Helper targets exactly one method and packages only its diagnostic outputs. Source state/adoption flags remain false; historic354/release502/North20 freeze files remain untouched. New method count235 configures future full QA without running it.
- Inspected existing focused29-case NUnit result and successful affected PlayMode API compile log. These are coordinator-produced source/API checks, not my independent native run. Independent review checks were read-only source/diff/hash comparisons and clipping arithmetic plus atlas-alpha inspection. No full QA, APK or device action was performed.

## Limits

Cannot judge visual superiority, perceived depth, shading naturalness, smoothness at real render cadence, silhouette impression, native UGUI clipping/masking/material appearance, or Pixel cost without the resulting real images/device evidence. The proposed camera correction must be covered by the new native depth assertion and eventual one-method run. After these localized fixes and focused checks, the next justified action is only that minimal Noah capture followed by image review; no other-five rollout, full QA or adoption recommendation.
