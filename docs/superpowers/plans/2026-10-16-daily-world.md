# Daily World Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Inline implementation, then one independent whole-branch review.

**Goal:** 6依頼完了後も会話・散歩・合成・図鑑が楽しい、北中央南の連続した街を実装する。
**Architecture:** Coreを保持し、世界投影/連続歩行、斜め盤面、補助交流記録をPresentation内に分離。
**Tech Stack:** Unity6000.6.4f1、UGUI、C#9、既存.NET8検証。SDK追加なし。
**Spec:** docs/superpowers/specs/2026-10-16-daily-world-design.md

## Global Constraints
- 正式6名・ロゴ・背景原画は変更しない。接続素材は別ファイル。
- 保存v3、5×6、同種同Lv.、既存納品/報酬計算を保持。
- 新しいCoins報酬、課金、SDK、main統合は行わない。
- 未実施のUnity/Pixel/FPS確認を成功扱いしない。

## Review Focus
- 園路の途中の目的地変更/設定/pause/再開で位置が飛ばず入力が戻る。
- 複数指/連打/短いSafe Areaで30セルindexと操作範囲が一致する。
- 6依頼完了後もデイリーを達成でき、架空依頼/報酬を作らない。
- 日付逆行/壊れた補助保存/書込失敗で二重解放や主セーブ変更をしない。
- 画面外の背景/装飾/NPCを抑制し、キャラを増殖させない。

### Task 1: Continuous world model and view
Files: new Presentation/TownWorldLayout.cs, TownWorldWalkModel.cs, TownContinuousWorldView.cs; TownActor/TownLifeView/TownRegionSession/TownViewportView/TownRegionView/TopHomeScreens/TownStreetWalkView/TownRegionControls/TownGrowthView.
- [x] Test global coordinate round trips, N-C-S routes, bounded no-teleport steps, cancel/retarget, culling and invalid input in Tests/Presentation/TownWorldTests.cs. Run RED.
- [x] Implement pure map/model with existing TownNavigation, then UGUI projection and actor bridge; stable place IDs, no lateral exit controls, explicit fast travel.
- [x] Verify pure suite and6assemblies; commit.

### Task 2: Perspective Merge
Files: new Presentation/MergePerspective.cs, PerspectiveBoardGraphic.cs; QuestMergeScreens/MergeCellSurfaceGraphic/MergeBoardView/MergeEffectsView; tests.
- [x] Test30 indexes, top narrower than bottom, upper art scale,48-unit targets/short viewport, non-overlap and invalid input. Run RED.
- [x] Apply row projection/tray/ellipse/shadows, preserving Core and animation/save boundary.
- [x] Verify timing/interaction contracts and compile; commit.

### Task 3: Daily social model and persistence
Files: new Presentation/TownDailyProgress.cs, TownDailyStore.cs, TownCharacterBook.cs; pure tests.
- [x] Test date rollover/rollback, finale-compatible goals, no repeated reward, write rollback, malformed backup and completion-derived book fields. Run RED.
- [x] Implement atomic versioned auxiliary file; cosmetic/dialogue rewards only, separate from savev3/currency. No production test hooks.
- [x] Verify Core unchanged and pure suite; commit.

### Task 4: Book, Today UI and goals
Files: CharacterSelectionScreen, TopHomeScreens, UIFlowController, TownLifeView, daily/book views and glyph text catalog.
- [ ] Execute native UI routes/Back/scroll/names and successful merge/talk/walk events. Regression cases are written and compile; Windows runtime is pending.
- [x] Implement six profiles, favorite flow, daily checklist/claim, post-finale next goal, story/UI font role split.
- [x] Verify compile and protected hashes; commit.

### Task 5: Living characters, growth and review
Files: TownCharacterMotion/CharacterBodyGraphic/TownGrowthView/ExperienceCaptureTests; reliable script SHA fallback.
- [x] Add bounded pose and native input/lifecycle/capture coverage for seams/book/daily/perspective/mid-walk interruptions.
- [x] Improve poses/arrival/cosmetic growth and inhabitable paths.
- [x] Run available suites/6assemblies/script gates/inventory; independent review and material fixes.
- [x] Record actual vs unrun Windows/Pixel results, commit/push candidate, hand off same-run XML/PNG/APK workflow.

Native execution remains pending: Windows Unity141/161 declarations,36actual PNGs,QA APK and Pixel. Checked items above mean implementation and available cloud validation only; they do not mark native/device acceptance passed.
