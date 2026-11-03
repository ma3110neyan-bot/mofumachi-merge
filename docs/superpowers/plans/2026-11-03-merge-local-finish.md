# Merge Local Finish Implementation Plan

> Execute inline with superpowers:executing-plans. User already chose local finishing and waived intermediate design questions. One final bounded review.

**Goal:** Raise the current reward Merge by one visual step using only the user's eight specified local adjustments.

**Architecture:** Retain a18e598's board geometry, assets and callbacks. Only the Merge builder header, MergeRewardView styling/read-only placement and empty-cell motif opacity change. Reuse existing floral frame/icon atlas and fonts. No animation or new textures.

**Tech Stack:** Existing Unity6000.6.4f1 UGUI, current fonts and atlases.

**Spec:** User's2026-11-03「Merge最終仕上げ指示」in this conversation; fixed original References/Merge/merge_visual_lock_reward.png. User confirms reviewing previous eleven Unity pictures; those actual JPEGs have not been uploaded here, so do not claim the author inspected them.

## Global Constraints

5×6, official ItemAtlas, Merge rules, generation, Save, Quest conditions, delivery/reward calculation, Core, characters, town and BGM are unchanged. No redesign, shared widget edit, new particles or extra motion. Only Merge-related minimal checks. Stop for Visual Gate; no full suites/APK/Pixel before acceptance.

## Review Focus

- Pink reward heading must be the product default; legacy blue reconstruction remains historical Before only.
- New decorative header/pills stay behind text, never intercept input or cover requirements.
- Multi-requirement art, names and counts fit320×536/360×640/360×740, names remain12+, requirement buttons48+.
- Review/delivery callbacks and busy locks stay exactly as before; secondary control remains easy to tap despite quieter styling.
- Source guards retain the exact Core/assets/geometry hashes and original PNG; updated UI hashes must not imply native/visual/device acceptance.

## Task1: local appearance

Files: QuestMergeScreens.cs (BuildMerge header call only), MergeRewardView.cs, MergeCellSurfaceGraphic.cs. Related existing native tests: MergeRewardUiTests.cs.

- [x] Extend native checks for pink title, primary/secondary styling and multi-requirement non-overlap/no save writes.
- [x] Build the product header directly as pink「ごほうびMerge」, keeping old blue solely in existing Editor Before reconstruction.
- [x] Warm the current-request card; add a soft pink caption band and existing Coin icon; modest pink/gold reward capsule and progress accent. No height increase.
- [x] Keep request/delivery dominant through color, frame/gloss and existing large action area. Reduce frame/gloss of generation-frame management, retain its48-unit target.
- [x] Reduce empty motif opacity .27→.16 without changing success sparkles or paper/geometry.
- [x] Multi-requirement cards get4 extra units of separation;12-point names; icons above the count row, which remains12-point. Single requirement and all board geometry stay fixed.

## Task2: verify and handoff

Files: pending Merge UI SHA record, this plan and latest AGENTS entry only.

- [x] Run only MergeRewardPresentationTests (read-only reward/count invariants), Presentation/PlayMode API compilation, and Merge scope checks. Do not fake Unity execution; no current cloud license.
- [x] Compare protected source/art bytes with94c7f6e/a18e598; preserve BuildQuest and every BuildMerge callback/board statement.
- [x] Update only the three authorized candidate file hashes, retaining the previous pending-manifest hash and exact base. Original26/552/53 records unchanged.
- [x] Bounded final review: no Important/Critical issues.
- [ ] Normal feature commit/push and CaptureOnly1-command handoff for native Visual Gate.

## Ledger

Ruling: these are reversible local styling adjustments; don't invent constant-mirroring tests. Existing pure Merge tests protect real data semantics, meaningful native layout assertions protect overlap/touch geometry, and API compilation checks the changed view. Native assertions cannot be run in this cloud and remain pending, not PASS. No repeated license probe/full tests. Original reference visually inspected again before implementation.

Results: MergeRewardPresentationTests14/14 passed, scope synthetic check and actual original/Core/art/geometry/character guards passed; Presentation API compiled without warnings/errors. Native assertions are prepared, not run; new local Visual Gate remains pending. Read-only review found no Important/Critical issue. Exact diff check proves QuestMergeScreens has one header substitution only; callbacks/board/layout/BuildQuest are byte-identical.

PlayModeTests API compilation also passed with zero warnings/errors. Protected-source check passed for all board mapping/input/FX/timing/services/assets/history; actual new native rendering and Visual Gate are still pending.
