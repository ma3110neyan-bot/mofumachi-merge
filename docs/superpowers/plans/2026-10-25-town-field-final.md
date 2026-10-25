# Town Field Final Completion Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans inline. User has authorized bounded field completion and opted out of per-file approvals. Keep the existing feature checkout for Windows continuity. One fresh whole-change review at the end.

**Goal:** Returnable five-area field and verifiable real-delivery growth without lowering approved scenery.
**Architecture:** Retain custom area navigation/Y sorting and existing art. Complete only exposed outer planting and expand the existing delivery comparison to five districts; validate the unchanged production/save path.
**Tech Stack:** Unity6000.6.4f1 UGUI, C#9/.NET8 tests, existing PS5.1 TownQa.
**Spec:** docs/superpowers/specs/2026-10-25-town-field-final-design.md

## Global Constraints

- Original354 approval inputs/15 approved JPEGs immutable; exact new bounded candidate hashes only.
- No landscape removal, floor/obstacle shrinkage, save or reward calculation changes, new systems, later stages or main.
- No player-save resets, synthetic player rewards or replaying completed quests in the user save.
- No chat image embedding. Native execution/APK/Pixel status must remain distinct.

## Review Focus

- North/South have no new Lv2 decoration: preserve authored pacing and show the actual next level.
- Near-NPC/obstacle escape must work through real motion, never teleport rescue.
- Foreground tree pixel concealment is not answered by raycast success; Pixel acceptance remains required.
- Plant rectangles must remain outside all five floors and below a bounded static vertex budget.
- Scene reload must recover the saved level/rewards and live decoration without applying a test-only level.

### Task1: Real progression and five-district growth comparison

Files: TownGrowthPresentation.cs, TownGrowthComparisonView.cs; new CPU FinalTownGrowthTests.cs and native TownGrowthProgressionTests.cs; existing QualityPassStoryGrowthTests.cs.
Interfaces: ForDistrict(TownDistrictId,int), FocusForDistrict(TownDistrictId,int), ChangeSummary(TownDistrictId,int,int) derive existing authored placements. No serialized schema.

- [x] Write CPU tests for actual six-quest progression with disk reload and district-level growth/focus/summary. Record existing three-choice gap before changing UI.
- [x] Add native sequential generation/Merge/accept/delivery/compare/reload test covering1→2,2→3,5→6 and all five live districts; no direct growth-level/quest completion assignment.
- [x] Extend only comparison selection/focus and short change labels to five districts. Keep existing reward/growth presentation and authored unlocks.
- [x] Run focused CPU RED→GREEN and full CPU; API compile/native test declaration check, not native success.

### Task2: Final outer boundary and movement regressions

Files: TownBoundaryGardenLayout.cs; Tests/Presentation/FinalFieldRecoveryTests.cs and TownBoundaryGardenTests.cs/CurvedBoundaryTests.cs; existing native capture fixture.
Interfaces: Existing Dressing/GetVertex/Vertices; immutable original placements remain, new pieces have no footprint.

- [x] Reproduce uncovered North cap and West outer strip with spatial coverage tests, then add bounded outside-floor planting/curves using shared source art.
- [x] Exercise5-area perimeter/obstacle and overlapping-NPC return journeys at Lv1/3/6. Diagnose and fix only reproduced stops.
- [x] Improve native fountain far-side overlap and extreme-edge evidence without always-front sorting or art removal.
- [x] Run focused/full CPU and six Unity API assembly builds; preserve strict native hit/return checks.

### Task3: Exact lock, full-QA handoff and device acceptance

Files: new town-field-final candidate, exact visual-lock scope and rejection tests, capture metadata/packaging, verification receipt and Pixel guide.
Interfaces: Same full-TownQa pipeline, failed0/skip0, matching build/capture identity, updated required images/metadata.

- [x] Bind exact display deltas with original/historical approval unchanged; invalidate current native/APK/Pixel/adoption flags for the new runtime.
- [x] Run relevant PS tests, historical/current digest and Windows CRLF preflight; fresh whole-change review and one verified fix pass if needed.
- [x] Commit/push feature only. Provide one full -TownQa command for actual Windows native/QA APK evidence; do not claim cloud execution.
- [ ] Inspect new native evidence then same-APK Pixel acceptance including real growth/save/restart and600seconds. Formal freeze only after actual user/device pass.

## Ledger

Start9b24fc7, existing feature checkout clean. Previous143/223/138PNG/APK success belongs to871808c; cannot transfer it to a changed runtime. Pixel not received. Inline execution, no per-file approval. Native game-progression test starts from isolated initial state; real user Lv6 remains untouched.

Ruling: reuse ExperienceCaptureTests for the new real-progression UnityTest instead of adding another fixture/camera implementation. One extra case,6extraPNG and1metadata record; full144Edit/224Play/144PNG required. Legacy district review filters select only the original capture method, preserving their exact scope/count.

Ruling: retain authored North/South Lv3/6 additions instead of changing growth scheduling to manufacture a Lv2 difference. Truthful next-Lv label plus all-five current-state/reload checks. Separate-save normal-game QA package makes real initial Pixel progression possible without resetting the existing Lv6 player save.

Evidence: baselineCPU325/325 andCore125/125. Focused missing district helpers/cap coverage RED3→GREEN;6real-delivery/disk reload case passed without Core changes. Perimeter/apron and overlapping-resident cases7/7 passed (7min), no new nav/obstacle/floor correction required. FullCPU334/334 passed before final mirror-preservation review fix; post-review full suite remains separately recorded.

Final review Critical0/Important1/Minor1: newly inserted cap entries changed sorted ordinal, which flipped all200 original exterior sprites. Fixed by stable per-piece UV orientation assigned before additions; preservation regression RED→GREEN, focused13/13. Treat the Home-only duplicate-delivery assertion as an important evidence gap: now attempts from Quest and verifies unchanged coins/reward IDs. Native execution remains pending. No second review dispatched.

All14 PS fixtures passed before the final mirror/capture hash refresh. Original354/JPEG15 remain immutable. Current candidate bindings must be refreshed and relevant gates rerun after that fix. Pixel/visual quality/real APK content are declined-to-judge without actual execution/images and remain mandatory acceptance gates.

Final verification: post-review fullCPU335/335 (8m52s), Core125/125 unchanged-source baseline, six API DLLs0warnings/errors, native declarations144/224 (not execution), relevant2PS gates rerun successfully, current/historical exact gates pass in411-file Windows CRLF fixture. Fountain front→left→.441far rim→right→front actual-model probes passed atLv1/3/6 with live NPCs. No graphics/Pixel verdict inferred. New native144PNG/twoAPK/SHA and actual Pixel acceptance remain outstanding; old153810 evidence is historical.
