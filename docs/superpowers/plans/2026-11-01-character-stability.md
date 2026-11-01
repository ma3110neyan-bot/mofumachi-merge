# Character Stability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Preserve the six approved-looking characters and remove actual close-contact movement reversals before final acceptance.

**Architecture:** Keep the existing directional rigid parts, grounding and tap-only dialogue. Repair only local steering continuity; retain obstacle checks, foot clearance, existing waiting/replanning and all art. Native and Pixel evidence remain separate from pure model checks.

**Tech Stack:** Unity 6000.6.4f1, C#, NUnit, PowerShell.

**Spec:** User's Nov1 “2.5Dキャラ最終安定化・正式固定指示”; two PDFs recorded in `References/Review/character-stability-feedback-2026-11-01/`.

## Global Constraints

- No new method, art, size, town, north boundary, fountain, Merge, Save, book, BGM, font or general UI changes.
- Normal conversation only by tap. Automatic greetings remain disabled, satisfying the maximum once/day rule without Save changes.
- Related tests only during implementation; final native full suites/APK/Pixel once after related stability.
- No intermediate approval: user explicitly authorized bounded execution and feature-branch push.
- Formal Visual Lock requires actual full native and Pixel acceptance; keep `formal_fixed/adopted=false` meanwhile.

## Review Focus

- Stationary neighbour near a reachable destination: no rapid alternating sidesteps and still arrive.
- Occupied destination or obstacle apron: wait/replan without jitter or clipping.
- Explicit new/reversed target and new route waypoint: continuity memory must not prevent a legitimate turn.
- NPC and hero speed differences: maintain clearance and original passing behavior for all six identities.
- Rendering/input: no new art/pose changes, rear for north, head attachment and tap-only behavior retained.

### Task 1: Steering continuity

**Files:** `Assets/Mofumachi/Presentation/TownWorldWalkModel.cs`, new `Tests/Presentation/CharacterContactStabilityTests.cs`, exact SHA amendment `References/VisualLock/character-finish-amendment-2026-11-01.json`.

**Interfaces:** Existing Guide/Step/Restore/Pause APIs; no public API or Save schema change. Per-resident transient steering direction, reset for a new route/waypoint.

- [x] Add failing real-model regressions for the reproduced stationary-neighbour cases: half-second reversal bursts <=1, legal floor/clearance, eventual arrival. Include all six hero IDs, NPC motion and legitimate new target reversal.
- [x] Run focused NUnit runner, retain RED XML showing the repeated displacement reversals.
- [x] Reject a steering candidate that reverses the last accepted movement on the same waypoint; if no candidate is legal, keep position and let existing waiting/replanning act. Clear transient memory on new route/waypoint and restore/cancel.
- [x] Run related model/pose/route tests and the original sweep; verify no bursts and no new arrival failures. Update only this path's exact amendment digest.
- [ ] Commit tested fix and bounded evidence.

### Task 2: Native acceptance evidence and handoff

**Files:** existing `ExperienceCaptureTests.CharacterFinish.cs`, focused stability native test/runner as needed, `docs/characters-stability-2026-11-01.md`, feedback receipt and `AGENTS.md`.

**Interfaces:** Current source identity/fingerprint, existing final-pass pipeline, actual model displacements and real input events.

- [x] Add a focused native regression for the reproduced movement and preserve existing real tap/no-auto tests. Make native final crossing evidence assert reversal bursts as well as arrival/clearance.
- [x] Compile normal/Android/Editor/native API, run scoped PowerShell guards and exact frozen-source checks. No cloud Unity/Pixel success claim.
- [x] Fresh review of the complete bounded diff; fix important findings with regression tests.
- [ ] Push the authorized feature branch and verify remote SHA. Document one focused native gate then existing final-pass command; failure shares only that run, never automatic full reruns.
- [ ] Report local results and pending native full/Pixel criteria without declaring formal completion.

## Execution ledger

Base `10dbbf95d75885d1531c4164870c833728369b53`, clean feature checkout. Continue in the established project checkout (all existing harness/API paths use it); no new worktree or approval round. User's explicit continuation and branch authorization take precedence over optional workflow prompts.

Investigation: ordinary head-on sweep144/144 arrived, no reversal. Contact sweep249 valid fixtures (40s bounded) reproduced rapid reversals in131; worst458 reversals/600 ticks. Case9: hero(-.20,-.22), stationary peer(0,-.30), goal(.16,-.22),70 reversals then arrival. Trace shows alternating lateral candidate choices at foot-clearance boundary, no source/Sorting/target changes. This is a world-position defect, not a rendering defect.

Ruling: use focused tests while implementing and preserve the previously recorded full-suite timeout; do not repeat that unrelated long run. Full native suites and Pixel must be supplied by Windows hardware after the focused gate.

Task1 local implementation complete: RED8/9 failed for actual repeated reversals, GREEN9/9, related68/68. First continuity-only attempt retained7 failures (NPC abandon and double soft avoidance on resident-aware paths); traced and resolved both without weakening assertions.249 selected contact cases:131 periodic bursts→0,249 arrivals.144 head-on cases:144 arrivals,0 reversals. No full-suite repeat.

Task2 local implementation/guards/API complete; physical acceptance is pending. Strict3-method native script and final-pass handoff are prepared; native execution has NOT run here. Fresh review: no Critical/Minor, one Important native fixture iteration issue fixed by snapshotting the actor list and checking all six identities exactly once. PlayMode API recompiled after fix; real native success awaits Windows.

Final review rulings: native rendering/head attachment and Pixel are unjudged by cloud review; keep formal flags false and use the explicit Windows workflow. Latest traces68/78/99 explain the two-turn windows: resume after blocked recovery, then a new waypoint7–10 ticks later, no soft-avoidance force and no periodic loop. Keep these records and require Pixel naturalness; if harsh, only local movement polish within the same locked appearance. Human documentation was self-checked after the review; no second review dispatch.

Ruling: retain approved feature checkout and established harness paths, no new worktree; user authorized direct continuation and feature push. Cost if wrong: changes share this checkout; clean starting state and bounded explicit commit allow rollback.
Ruling: do not call pure/API/synthetic checks native or full success. Cloud has no usable Unity license or Pixel USB. Cost: final visual freeze remains pending genuine hardware evidence rather than being declared prematurely.
