# Senior Code Review — Noah depth-focus source candidate

Reviewed BASE/HEAD `431c29b` plus the uncommitted whole change and the named new files. Read the design, plan, ledger (including every Ruling), handoff, candidate/stage records, evidence record, runtime integration, native fixture, CPU tests, validator and synthetic fixture. This is a source-readiness review for one native capture, not visual, user, Pixel, release or merge acceptance.

## Critical

None found.

## Important

1. **The lower face is outside the rigid head region and deforms relative to the eyes.** `Assets/Mofumachi/Presentation/TownNoah25DTrial.cs:205` applies full head weight only at `v >= .53`; `:211` blends the lower region against the independently posed torso/source plane. On the unchanged formal Noah crop, `v=.53` is atlas Y `165.81`, through the mouth, while the lower mouth/chin continue below it. Consequently the mouth/chin receive different rotation, translation and depth weights from the eyes. This is a concrete exception to the required rigid face/eyes, not a judgment that the new rendered face looks worse.

   The relevant original-art region is approximately mouth `u=.35–.45, v=.49–.57`, with the chin continuing to about `v=.46`. At `(u=.42,v=.49)` (atlas `1369.68,178.73`, an actual grid vertex), the head weight is only `0.699474`; at `v=.50` it is `0.817431`, versus `1` at the eye row. With a neutral pose, those lower-face depths are approximately `-7.727719` and `-8.536983`, versus the rigid head plane at `-10`. Opposite turns change the 3D distance between the same-column `.49` and `.60` points from approximately `8.91040` to `8.84773` UI units; a single rigid transform cannot do that. The larger chin transition also creates relative parallax on top of this distance change. Source atlas inspection confirms this transition lies within the face rather than entirely below the jaw.

   **Fix before handing off capture:** move/shape the head-to-body transition below the actual mouth/chin, keeping the lower muzzle in the same rigid transform as the eyes while retaining the existing hand and outer-tail protection. Extend `Tests/Presentation/NoahDepthFocusTests.cs:28` beyond its current `.60/.80` samples: include `u=.37` and `.42`, `v=.49` (actual mouth-adjacent grid row), `.53`, `.60`, plus a chin point near `(u=.39,v=.46)`. Compare pairwise 3D distances under neutral/opposite head/body turns, nod and front/back poses to the source-plane distances. Also retain the existing grid-fold and outer-tail checks. No new art or full suite is needed.

## Minor

None found that merit expanding this pass.

## Other review conclusions

- The original `TownNoah25DTrial` class remains byte-equivalent to source `22f2831` after LF normalization. Focus OFF selects the original body, root/support and shadow values; the historical reprototype/2D switches remain distinct. The new pose fields do not feed those branches or other characters.
- The prepared-pose fallback is present in both vertex generation and projection. Root quaternion order and analytical projection both apply scale, Ry, Rx, Rz; support uses the two real paw vertices. The shadow remains a navigation-root sibling and does not inherit body lift.
- The native fixture retains enabled public navigation and actual Noah conversation, advances the real simulation clock synchronously, checks the Unity frame guard, and adds actual public front/back targets. The DTO modes, sample count, comparison kinds and validator agree on 222 samples / eight body pairs / one shadow-only pair.
- The shadow-only override runs after the normal presentation refresh in `Portrait`, and is reapplied to both Before renders. The natural/controlled Before comparison therefore does not accidentally compare the new shadow against the old one. After restores normal focus shadow, and body vertices/UV/colors are checked unchanged.
- The extended crop and each-frame bounds assertion cover the added vertical itinerary at source level; the camera also checks transformed body depth before rendering. No definite crop or clip failure was found. Actual native screen framing remains unexecuted.
- Independently checked all eight evidence source hashes and all 24 candidate after-file hashes against the current worktree. Product changes are confined to the three authorized runtime files. Existing protected assets/frozen records are not modified by this diff. Candidate/stage/handoff consistently keep new native, visual, APK, Pixel, adoption, full-QA and rollout acceptance false.

## Verification used

Read-only source inspection, original atlas inspection, LF-normalized baseline/file-hash comparisons, and a small independent arithmetic check of the face-mask weights and rigid-distance invariant. The recorded 24/24 focused CPU checks, two successful API compiles and synthetic PowerShell checks were reviewed as existing evidence, not rerun or relabeled as native success. No full suites, Unity startup, permissions retry, product edits, index/HEAD changes or delegated reviews were performed.

## Declined to judge

- Perceived improvement, face likeness and shadow readability in the new Unity images; no new native images exist yet.
- Actual one-method native execution, screen clipping/crop appearance, rasterized overlap and final outside-scope pixel result.
- Pixel FPS, thermal behavior, memory, input latency, APK quality and save retention on device.
- User adoption, six-character rollout, full-QA readiness, release or merge approval.

## Readiness for minimal native capture

**Not yet ready for handoff: address the one Important lower-face rigidity issue, refresh only its focused checks and affected evidence bindings, then proceed with the existing single-method native capture.** No other source blocker was identified. A successful capture will still require review of its actual images and will not itself establish visual/user/Pixel acceptance.
