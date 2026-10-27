# Noah one-character town trial — implementation handoff

Status: implemented; CPU tests and Unity API compilation checked. Licensed Unity drawing, actual native PlayMode execution, APK and Pixel 3a review remain unverified. No commits were created.

## Owned files

- `Assets/Mofumachi/Presentation/TownActor.cs`
- `Assets/Mofumachi/Presentation/CharacterBodyGraphic.cs`
- `Assets/Mofumachi/Presentation/TownNoah25DTrial.cs` and `.meta`
- `Tests/Presentation/Noah25DTrialTests.cs`
- `Assets/Mofumachi/Tests/PlayMode/Noah25DTrialPresentationTests.cs` and `.meta`

Parent owns the shared CPU project Compile entry, all captures, gate/archive changes, freeze metadata and final integration.

## Behavior

`TownActor.NoahTrialEnabled` is true by default only for the exact formal ID `noah` in town. `SetNoahTrialEnabled(bool)` immediately changes the presentation at zero elapsed time, preserving breath, stride, movement blend, reaction timer and all world/save state. No product UI/debug overlay or persistence exists. Other five characters and non-town Noah body graphics keep the legacy path. `TownCharacterMotion` and `BodyReaction` are untouched.

The trial registers Noah's actual two paws at normalized U .303/.535 to midpoint .419; the generic right-foot location .75 was on the original asymmetric tail. The resulting normal body is approximately 5 local pixels farther right relative to the actor rectangle, bringing the real paw midpoint over the existing foot shadow; the actor/navigation/hit rectangle and name remain unchanged. Original source PNGs, proportions, colors, clothing, asymmetry and atlas remain unchanged. Pure mesh offsets articulate the original paws/very small arm islands. Head/face vertices translate rigidly by at most .55 local pixels; UVs never mirror. Parent reviewed the centering tradeoff before implementation.

Walking has at most 1.8-pixel body bob and 3.2-pixel lifted-paw offset, plus smooth existing start/stop movement blending. The small settle uses only the continuous smoothed walking envelope; a discrete moving-flag transition cannot jump the pose. Talk uses the existing reaction timer with a <=3.2-pixel body bob and small original-arm motion. Rotation is bounded to 2 degrees; breathing is below 0.5%. The support translation is solved after rotation, breathing and unchanged .94–1.06 world-Y depth, placing one actual paw at floor Y=0 without letting the other sink. The tail base receives no paw lift/travel/cancellation; it follows the rigid body's bob. This is presentation contact correction, not a claim of measured perceptual anti-skating on device.

The source contains only three approved full-body expressions per character (normal, smile, surprise), with no side/back/independent limb frames. The trial keeps the original normal Noah body UV across expression requests, avoiding full-body pose jumps during talk. Blink samples only two original approved smiling-eye islands, registering normal centers (1330,144)/(1405,148) to smile centers (1330,480)/(1403,487). No new eyes or face artwork are painted. The island registration and the naturalness of the final appearance still require actual native image/clip review.

Noah uses 624 vertices / 312 triangles, versus the unchanged legacy 224 / 112. This adds 400 vertices for exactly one body. One shared original atlas remains; no new texture, material, light, RT, particle or per-frame collection is introduced. Counts do not prove Pixel performance.

## Evidence

- `noah-gate-red.xml`: 1 test, 1 expected failure — missing one-character gate.
- `noah-gate-green.xml`: first gate implementation passed.
- `noah-motion-red.xml`: 6 tests, 4 expected failures — missing walking articulation, rigid head gaze, talk/start-stop pose and registered smile eyes.
- `noah-motion-green.xml`: 6/6 passed after implementation.
- `noah-tail-red.xml`: 6 tests, 1 expected failure after expanding tail assertions to the bottom rows; exposed -0.59433 local X tail paw travel.
- `noah-continuity-red.xml`: 6 tests, 1 expected failure; exposed a .9-degree discontinuity when the moving flag flipped at half amplitude. Removed boolean-gated settle and tested identical-pose continuity plus adjacent blended stop/restart frames. The native talk fixture also now asserts the talk pose before triggering `OnDisable`/pause cleanup.
- `noah-and-legacy-green.xml`: fresh final focused run of Noah plus existing resident-presence/street tests, 20/20, failed/skipped/inconclusive 0.
- Direct Unity 6000.6.4f1 API assembly compilation: Presentation and PlayModeTests, 0 warnings / 0 errors. An initial PlayMode compile referenced the old Presentation DLL and reported missing new APIs; rebuilding Presentation first resolved all of those dependency errors. This is API compilation, not Unity engine execution.
- `git diff --check` clean for the worktree at handoff.
- Independent runtime reviewer rechecked the two reported Important causes after fixes and marked both resolved in source; licensed native execution remains pending (`runtime-review.md`).

The four dedicated native tests cover exact Noah-only eligibility and OFF/ON/OFF frame restoration with clocks/state unchanged; all six shared portrait defaults and other-five town frame equivalence; original normal-body UV retention and eye-only blink source deltas; and talk/pause/contact shadow behavior. They compile but have not run in licensed Unity.

Original hashes rechecked:

- `References/Characters/mofumachi_6characters_master.png` and `Resources/Mofumachi/CharacterMaster.png`: `cd423f6599cf41346064405240c72ac071aaa8d3fbd326d2b82661b13b0892c8`
- `Resources/Mofumachi/CharacterBodyAtlas.png`: `23d14025f146c429da01b6e3a74ef803254755f8d149f9eb53516aacaa30163d`

## Review caveats

Require same-condition native idle/walk/start-stop/talk clips and OFF/ON pairs with unchanged clocks/world state, original face/clothing readability and existing fountain occlusion. Inspect the approximately 5-pixel centering shift relative to the name/touch area, blink-island seams, paw contact during turns and actual movement, and tail silhouette. Actual native/Pixel review must decide whether the result is better. No adoption or performance claim is made here.

Codex / AI next improvement candidates: none beyond reviewing this authorized trial.
