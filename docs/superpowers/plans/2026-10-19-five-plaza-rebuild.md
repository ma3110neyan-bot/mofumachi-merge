# Five-plaza rebuild implementation plan

> **For agentic workers:** Use executing-plans for parent world integration and dispatching-parallel-agents for the independent Merge presentation task. User explicitly requests bundled work and no staged approval stops.

**Goal:** Replace the rejected zoomed town with five connected playable scenes and restore reference-led, readable Merge.
**Architecture:** Shared textured ground/paving and authored floor/obstacles, transparent landmarks, existing movement/camera. Cardinal presentation districts map to stable legacy Core categories. Merge is an independent presentation change.
**Tech Stack:** Unity6000.6.4f1 UGUI/C#9; existing pure .NET tests, existing asset tools.
**Spec:** docs/superpowers/specs/2026-10-19-five-plaza-rebuild.md

## Global Constraints
No destructive save/Core rule/reward/delivery/official-design/SDK/payment/main change. Preserve5×6 and saved board. Broad user approval applies; no per-file questions. New generated art is not actual game proof.

## Review Focus
North/south walks count daily once despite stable Central activity category. Camera projection/pointer inverse align with real field safe area. All five regions contain open alternative routes and reachable facilities. Foreground/door/furniture sprites have real floor support. Finale Merge never invents items or progression and preserves selected feedback/actions.

### Task1: Five-district topology and movement (parent)
Files: TownWorldMap/Layout, new TownDistrictLayout, TownWalkableArea/AreaNavigation/WorldWalkModel; pure district/world tests.
- [ ] RED tests for cardinal ordering, screen-sized spacing, all five routes, loops, holes and one daily arrival per district crossing.
- [ ] Implement presentation enum/anchors and shared ground polygons without changing Core IDs.
- [ ] GREEN pure route/avoidance/projection checks; preserve finite/clamp/drag behavior.

### Task2: Actual field rendering and facilities (parent)
Files: TownContinuousWorldView, new ground/scenery graphics/catalog, TownFountainGraphic, TownFacilityView, TownGrowthView/Presentation, new transparent landmark atlas.
- [ ] Produce/inspect landmark assets, retaining title/formal six.
- [ ] Render same polygons as visible paving and navigation floor; landmarks outside floor, culling outside camera.
- [ ] Align actor/fountain/doors/growth and camera/pointer conversions to actual field.
- [ ] Add five travel choices/location labels, arrival and daily routing.
- [ ] Native fixture/capture coverage includes all five scenes, four joins and physical entry.

### Task3: Reference-led Merge (isolated worker)
Files: QuestMergeScreens.BuildMerge, PerspectiveBoardGraphic, MergeCellSurfaceGraphic, Merge visuals/selection information; focused pure/native tests.
- [ ] Read rejected Pixel page4 and September22 reference. RED meaningful layout/purpose checks.
- [ ] Restore warm thin frame, clearly readable flat cells and art, selected-item information, actual daily objective after finale; retain all input/manage/delivery behavior.
- [ ] Verify focused suites/reference compilation in isolated outputs; no board/save/core rewrite. Return full limits.

### Task4: Combined quality and delivery (parent)
- [ ] Run Core/Presentation suites and all six reference asmdefs; check constraints and fresh review.
- [ ] Update native geometry/flow/capture gates, preserving every meaningful assertion.
- [ ] Publish one bundled feature candidate; request actual same-source Unity render/APK.
- [ ] Inspect actual After screens against rejected Pixel Before/reference, then update Pixel and check save/control/audio. Label pending evidence explicitly.
