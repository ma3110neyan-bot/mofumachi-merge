# East / west POP pass implementation plan

> **For agentic workers:** Use the parallel-agent workflow for isolated Merge, audio and character tasks; the parent owns town/UI/integration. User authorized continuous execution, with one bundled candidate rather than interim APKs.

**Goal:** Implement the full October 18 feedback against the September 22 reference in product code.
**Architecture:** One wide opaque map, authored walkable polygons and existing camera. Independent flat Merge, acoustic POP and atlas-preserving character presentation changes join a compact Home HUD.
**Tech Stack:** Unity 6000.6.4f1 UGUI, C#9, .NET8 pure tests, existing Python audio tooling.
**Spec:** docs/superpowers/specs/2026-10-18-east-west-pop.md

## Global Constraints
No destructive SaveData change; no Core merge/delivery/reward/quest progression change; no official design/logo change; no SDK/payment/main merge. Keep 5×6 and row-major indexes. Native screenshots and Pixel evidence must be actual, not mocked.

## Review Focus
- Restored old save with all quests complete still offers today's goal and preserves board/coins.
- Portrait and short editor viewports show map without blank edges; taps at edges stay on paving.
- Modal open/close cancels dragging and facility walking without accidental entry.
- Every moved facility threshold and NPC stop is on the newly traced walkable surface.
- Old role IDs keep working when a new POP candidate is introduced.

### Task 1: Coherent east/west world and growth (parent)
**Files:** TownWorldMap.cs, TownWorldLayout.cs, TownWalkableArea.cs, TownFacilityView.cs, TownContinuousWorldView.cs, TownGrowthPresentation.cs, TownGrowthView.cs, TownGrowthPropGraphic.cs; TownSeamlessMap.png; pure map tests.
**Interfaces:** Keep Place(string), Point(float,float), RegionAt(TownPoint), TownAreaNavigation, TownFacilityEntry; Width=3, Height=2, Left=-1, Bottom=-.6, west/east by X.
- [ ] Generate and inspect one wide painting; trace roads and buildings from the actual pixels.
- [ ] RED tests: west center east anchor order; all destinations routable; no fountain/building crossings; horizontal travel/camera bounds.
- [ ] Implement coordinates, polygons, facility door UVs and regional copy. Retain stable place IDs.
- [ ] Add level-gated benches, furniture, signs and storefront dressing beside routes; test monotonic additions and clear routes.
- [ ] Run pure presentation tests and Unity reference compile; commit product changes.

### Task 2: Compact town UI (parent)
**Files:** TopHomeScreens.cs, TownRegionControls.cs, new TownMenuView.cs, TownNextGoal.cs if copy needs it; native Home tests.
**Interfaces:** Existing screen Flow and Life; menu opens via HUD and bottom bar, pauses via existing Flow modal mechanism, guides to quest/shop instead of remote transition.
- [ ] Add native tests for default field bounds, expanded actions, pause/cancel and restore.
- [ ] Replace persistent chapter/travel panels with 44-unit HUD, location ribbon and 56-unit bottom bar; preserve names needed by capture fixtures where useful.
- [ ] Connect book/today/discovery/replay/next-goal actions; fast travel in expanded menu only.
- [ ] Compile and update native captures for default and expanded Home.

### Task 3: Flat playable Merge (isolated worker)
**Files:** MergePerspective.cs, PerspectiveBoardGraphic.cs, MergeCellSurfaceGraphic.cs, MergeBoardView.cs, related Merge tests; no town/audio edits.
**Interfaces:** Keep all existing public signatures and 30 index mapping; native capture names may stay stable with added flat-board frame.
- [ ] RED tests for equal widths/heights and constant item scale across six rows; existing hit/roundtrip tests.
- [ ] Implement thin ivory panel, aligned square cells and consistent items. Retain clear selected/compatible/drag/merge animations and >=48 input size.
- [ ] Verify focused pure and native test source expectations; report native execution unavailable; commit only owned files.

### Task 4: Acoustic POP Merge candidate (isolated worker)
**Files:** MusicSelection.cs/catalog location, scripts/render-town-candidates.py or dedicated generator, Audio resources/meta, preview MP3 and manifest; music tests. No town/Merge-board edits.
**Interfaces:** New stable ID merge-pop-promenade; existing role selection/previews/storage unchanged; candidate is Merge context.
- [ ] RED role/catalog assertions for new candidate and legacy preservation.
- [ ] Compose original 32-bar acoustic POP loop at 120 BPM using verified existing samples; render OGG and 8s MP3, review audio metrics and listen with tools if available.
- [ ] Add catalog card, preserve existing bytes/preferences; update hardcoded candidate count tests.
- [ ] Run music checks; commit only owned files and report hash/audio limitations.

### Task 5: Resident presence (isolated worker)
**Files:** TownActor.cs, CharacterBodyGraphic.cs, TownCharacterMotion.cs, related character tests. No map/UI/audio edits.
**Interfaces:** Keep approved atlas bytes and public body/actor APIs, feet are navigation coordinates; modest depth scaling remains independent of camera pan.
- [ ] RED motion tests for finite/bounded idle/stride/turn and depth factor.
- [ ] Add layered contact shadow, modest world-depth size and stable front/back sorting, small idle breathing/hand/head responses, smooth walking and blink; avoid whole-sprite mirroring.
- [ ] Test reset/pause/reaction coexistence and render finite geometry; compile owned source.
- [ ] Commit only owned files and report physical animation review pending.

### Task 6: Integration and one candidate (parent)
**Files:** Native tests/Experience captures, docs handoff and native build tooling only as required.
- [ ] Run Core and Presentation pure suites; compile all six real-Unity-reference asmdefs. Investigate failures rather than skip.
- [ ] Check no changes to Core/save/design masters/town BGM; review whole diff with fresh reviewer and fix material findings.
- [ ] Update native fixtures for east/west walking, UI menu, level differences, character and flat Merge; preserve book/daily/music regression.
- [ ] Commit and push feature branch only; give one exact Windows build command after the full bundle.
- [ ] Review actual native captures, XML and same-run APK identity; then update Pixel with install -r, compare saves, 10-minute play/audio/heat and Before/After. Do not call native or Pixel steps complete without their evidence.
