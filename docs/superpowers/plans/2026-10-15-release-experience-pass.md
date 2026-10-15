# Release Experience Pass Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans. Implement inline, then obtain one independent whole-branch review.

**Goal:** Make walking, characters, Merge, music and visible goals feel substantially different in the next Pixel QA build.

**Architecture:** Extend the existing Presentation components and transient regional session. Keep save v3, reward/delivery transactions, formal artwork and 5×6 rules unchanged. Store only the new music selection in application-local music preferences, independently of game saves.

**Tech Stack:** Unity 6000.6.4f1, UGUI meshes, existing recorded audio samples, no added SDK/packages.

**Spec:** The user's 2026-10-15 「リリース前・大規模仕上げ指示」 is the binding specification, summarized below. The user explicitly authorizes the complete change range and asks not to stop for per-file approvals. Approval is still required for game-save schema/rules/reward/formal-design changes, SDKs, purchases, destructive changes and main integration.

## Global constraints and visible design

- Work from `4f3a3e8` on `feat/release-experience-pass-20261015`; retain the previous Quality Pass branch as the accepted baseline. Do not merge main.
- Preserve モモ／ルル／ポム／フィオ／ミエル／ノア, formal logo and all three approved background paintings.
- Character tap arms street walking; road/place/exit taps send the same hero through the existing navigation graph. Keep destination menu and region shortcuts available. B transfers remain about 0.30s; West↔East still passes through Central.
- Use existing character atlas with bounded mesh/transform animation; no redesigned faces/clothes or extra residents. Walk, stop, turn, blink and activity reactions must visibly differ.
- Merge keeps 30 hit targets and transactional rules, but changes the whole presentation into a warm gold/porcelain board with rounded dishes, large tactile art, a clear needs shelf and a distinct production strip.
- Three actual distinguishable loop arrangements; selected track persists separately from v3. Preview is reversible and never changes the saved track until selection. Existing volume/OFF behavior and scene continuity remain.
- Derive next-goal and unlock previews from current growth level/completed quest IDs. No invented rewards or progression writes for presentation.
- The cloud's native Unity license/Android module/device limitations remain. Never label generated design previews as actual Unity/Pixel captures; add a Windows-native capture path for actual comparison artifacts.

## Review focus

- Character selection, road panning and tapping must not compete for the same pointer or leave ghost travel after Settings/Back/pause.
- Exit taps in another viewed region must not teleport the hero; camera crop, destination region and actual arrival must agree.
- Music preview/OFF/volume interruption and failed preference writes must restore the saved track without changing core saves.
- Short portrait layouts must retain 48-unit targets, readable Japanese and all 30 Merge cells without overlays stealing drops.
- Growth/goal presentation must reflect existing completion state, including all-six-complete and unknown quest IDs, without extra payment.

## Task 1: Walking and living characters

**Files:** `TownViewportView.cs`, `TownLifeView.cs`, `TownRegionSession.cs`, `TownActor.cs`, new road marker/intent and character motion presentation components, related PlayMode tests and pure Presentation tests.

- [ ] Add tests for selecting a hero, tapping reachable roads/exits, and interruption without saving.
- [ ] Add direct road-point guidance and stable world-anchored exit signs/footsteps using the existing graph and B transition. Keep prior menu interfaces usable.
- [ ] Add bounded acceleration, lean, foot/body mesh motion and contextual arrival/rest/flower/social reactions. Keep all six formal designs.
- [ ] Run pure behavior tests, compile Unity assemblies, record native tests as unrun until Windows.
- [ ] Commit the independently reviewable walking/character result.

## Task 2: Whole Merge presentation

**Files:** `QuestMergeScreens.cs`, cell/board/effect graphics, `ItemGraphic.cs`, new decorative board asset if needed, layout and interaction tests.

- [ ] Preserve tests for real Input System drag, tap, removal, packing, delivery, save failure and lock duration; add visual structure checks.
- [ ] Rework board frame/dishes, item composition, matching glow and drag shadow; retain 5×6 targets and hit rectangles.
- [ ] Refine meeting/light/landing/bounce and make requirements/production visually separate and immediately legible.
- [ ] Compile and run applicable pure/layout checks; retain native/render/performance checks for Windows/Pixel.
- [ ] Commit the complete Merge presentation change.

## Task 3: Three-track music

**Files:** `AudioManager.cs`, `SettingsScreen.cs`, music catalog/preference store/presentation, 2 additional recorded loop arrangements, relevant audio and settings tests.

- [ ] Add pure tests for track fallback, selection, preview cancellation and failed preference writes; no game-state schema edits.
- [ ] Render three distinguishable loop arrangements from existing licensed samples and retain replaceable resource keys.
- [ ] Add 3 rows with titles, preview, choose/current state; keep volume and ON/OFF in the existing scrollable Settings.
- [ ] Ensure preview cancellation on leave/pause/teardown and persisted selection on restart; keep gameplay writes unchanged.
- [ ] Decode/measure audio, run tests/compile, commit. Listening and Pixel audio remain explicit checks.

## Task 4: Growth and next goals

**Files:** growth/quest/story presentation and Home goal UI; pure data and PlayMode tests.

- [ ] Test next unlock at levels 0–6, completed story and unknown quest states without mutation.
- [ ] Show a concise next-goal card, remaining deliveries and concrete unlock imagery; integrate chapter tease and reunion reactions.
- [ ] Strengthen region-specific decoration clusters and activities at saved growth stages without repainting base art or spawning duplicate formal residents.
- [ ] Run pure tests and compile, commit.

## Task 5: Visible evidence and acceptance

**Files:** editor/native comparison capture helper, comparison PNG/contact sheet/short animation previews, handoff, current test gate requirements if suite counts change.

- [ ] Prepare Before/After from the same approved source assets with explicit labeling where native capture is unavailable; include walking, character motion, Merge, music, levels and goals.
- [ ] Provide an isolated Windows Unity capture path that does not write the user's actual save; preserve actual screenshots separately from design previews.
- [ ] Run Core/Presentation suites, real Unity DLL compilation, glyph/audio/content checks and protected-source hashes.
- [ ] Obtain independent review, address material findings, push this candidate branch only.
- [ ] Report exact tested/unrun status; Windows all-test XMLs → new QA APK/build-info → Pixel update and 10-minute acceptance. Do not reuse the previous APK or claim native results from compilation.
