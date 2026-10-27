# Central → North join and one Noah 2.5D trial

User instruction/spec: 2026-10-27 「中央→北境界最終修正＋次工程開始」. This explicitly adopts/freezes the current fountain and authorizes these two independent tasks in parallel. Earlier unmeasured Pixel performance/all-level checks remain unverified; they do not revoke this new authorization.

## Global constraints

Freeze ff7215c street artwork, water runtime, other districts, character original PNGs, book, BGM, Core Save/quests/delivery/rewards and Merge. Exactly Noah in town may use the trial; shared portraits and the other five retain legacy rendering. No original art repaint, no full 3D, no main integration. Navigation/collisions/camera/model positions unchanged. No unit/API result is native Unity/Pixel evidence. New candidate cannot be adopted until same-condition native and Pixel review. Product UI exposes no diagnostic controls.

## Design and decisions

- The join problem is an opaque North quad overwriting Central in y=1..1.23. Repair the North-owned lower ground transition only, preserving buildings/dressing and actual/visually readable walking corridors. Reuse original textures; no new large texture or obstacle.
- Noah already has a generic animation path; add restrained per-town-only support-foot registration, bounded walk/talk/settle and original-art blink improvements. Keep face rigid and atlas UVs unmirrored. Preserve world-Y depth sorting. A reversible per-actor switch exists only for isolated evidence/test capture.
- Keep actual bb3fb82 native connected-north PNG as historical Before. A same-state candidate OFF/ON comparison is explicitly a reconstructed causal baseline, never mislabeled a prior runtime capture.

## Task 1 — North ground transition

Own TownNorthMasterLayout.cs and TownNorthGroundGraphic.cs plus dedicated CPU/PlayMode tests. Investigate paint/path mask. Write failing behavior tests, observe failure, implement minimal bounded blend, verify UV/contact/coverage/finite values and legal paths. Keep North parent/court mesh contracts and all scenery placements. No commits until integration.

## Task 2 — Noah-only 2.5D trial (parallel)

Own TownActor.cs, CharacterBodyGraphic.cs, new pure TownNoah25DTrial.cs/.meta and dedicated CPU/PlayMode tests. Exact Noah opt-in in street, legacy defaults everywhere else, unchanged original PNGs. Test post-transform support foot, finite/bounded pose, rigid face, no mirror, start/stop/pause/talk, and other-five equivalence. Parent adds project Compile. No commits until integration.

## Task 3 — Parent integration and proof

Record PDF/SHA and explicit fountain release approval separately from unknown load metrics. Add strict current-source freeze and bounded candidate scope while retaining original baseline/historical digest. Add same-state native pair capture, central↔north walk proof and idle/walk/talk sequences with state/camera/fountain clock invariants, no game save mutation. Extend existing TownQa archive/gate flow and save-preserving install instructions. No new unrelated development.

## Task 4 — Verification and handoff

Review each task for spec/quality and whole change independently. Run focused RED/GREEN, full Presentation/Core real NUnit, six Unity API assembly compiles, PowerShell gate suites and baseline hash check. Licensed native Unity and Pixel are unavailable in this cloud; prepare Windows TownQa capture/build, report implemented/native-pending precisely. Commit/push existing feature branch only. No adoption claim before actual images/device results.
