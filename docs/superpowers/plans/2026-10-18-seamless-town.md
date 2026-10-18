# Seamless Town Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans inline; one fresh review at the end. Steps use checkbox syntax.

**Goal:** One broad seamless town with physical facilities and four acoustic BGM candidates, delivered as one QA bundle.
**Architecture:** One opaque map, authored floor geometry and stable-ID Presentation anchors. Cancellable threshold entry into existing screens. Auxiliary role music selection.
**Tech Stack:** Unity 6000.6.4f1 UGUI, C#9, .NET tests, offline sampled audio, no SDK.
**Spec:** docs/superpowers/specs/2026-10-18-seamless-town.md

## Global Constraints
Preserve save v3, 5x6 rules, reward/delivery arithmetic and six masters. No payments, SDK, main merge. Town audio unchanged. No partial QA presentation.

## Review Focus
- Tall/short viewports must never expose blank map edges or hide entrances.
- New drag/pause/fast travel during pending entry must never enter remotely.
- Every stable place remains on floor, with connected routes around visible obstacles.
- Old saved music IDs and failed preference writes preserve settings; previews restore context.
- Returning from facilities retains world position, daily progress and current game save.

### Task 1: Unified map and floor
Files: TownWorldMap.cs (new), TownWorldLayout.cs, TownWalkableArea.cs, TownAreaNavigation.cs, TownWorldWalkModel.cs, TownContinuousWorldView.cs, Resources/Mofumachi/TownSeamlessMap.png; tests SeamlessTownTests.cs.
Produces: TownWorldMap.Point(u,v), Place(id), bounds; map-backed routes. Consumes stable Core IDs.
- [x] Add tests for all stable destinations, paths on both sides of fountain, map boundary camera and no teleport.
- [x] Run Presentation tests and observe expected missing-map/floor failures.
- [x] Trace the generated coherent artwork and implement opaque world projection, camera fit, navigation and anchors.
- [x] Run all Presentation tests and real Unity asmdef compile. Commit.

### Task 2: Actual facility entry and destination UI
Files: TownFacilityEntry.cs, TownFacilityView.cs (new), TownLifeView.cs, TownContinuousWorldView.cs, TopHomeScreens.cs; native SeamlessTownTests.
Consumes Task 1 coordinates. Produces cancellable walking/opening/entered facility states and mapped door view.
- [x] Add threshold/cancel/remote-entry tests, observe failures.
- [x] Implement north quest/south shop door views, proximity response, walking then opening then existing screen, destination hints, and return state.
- [x] Verify pure tests, native compile, retain daily/book and fix home/quest clipping. Commit.

### Task 3: Four audio candidates and role playback
Files: MusicSelection.cs, MusicRoleSelection.cs (new), AudioManager.cs, MusicSettingsView.cs, UIFlowController.cs, SettingsScreen.cs, scripts/render-town-candidates.py, four OGGs and preview exports.
Consumes existing MusicPreferenceStore and sample sources. Produces MusicRole, role selection persistence and eight-second preview.
- [x] Add role/legacy/rollback/preview tests, observe failures.
- [x] Compose distinct title A/B and Merge A/B with hash-verified acoustic samples; preserve town asset.
- [x] Implement role routing and candidate cards with game-screen listening. Validate audio decode/duration/peak/loop and tests. Commit.

### Task 4: Bundle validation and Windows handoff
Files: native tests/captures, build scripts, UIStrings/font subsets, README and acceptance report.
Consumes Tasks 1-3. Produces one same-revision native review/APK path.
- [ ] Add native region/door/camera/music regression captures and required gates.
- [ ] Run Core + Presentation + compile + scripts + source/font/resource checks. Resolve failures.
- [ ] Fresh whole-change review, fix important findings, rerun affected suite. Commit feature only.
- [ ] Windows native render/tests/APK and Pixel install/captures; blocked until Windows execution is available, explicitly unconfirmed until evidence.
