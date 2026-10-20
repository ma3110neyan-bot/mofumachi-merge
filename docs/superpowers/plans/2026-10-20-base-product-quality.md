# ベース版統合品質 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. 独立した既存領域を並行実装し、rootで統合・全体レビューする。

**Goal:** 街・仲間・依頼・Merge・納品・街成長・図鑑／今日がつながるベース版。
**Architecture:** 既存5エリアとCoreは固定。表示配置の宣言データと既存画面の接続を改善する。
**Tech Stack:** Unity6000.6.4f1 / uGUI / C# / NUnit / Android ARM64。
**Spec:** `docs/superpowers/specs/2026-10-20-base-product-quality.md`

**2026-10-20実画像レビュー後：追加修正は実装済み・実機未確認。** 受領180369dは実Edit143／Play203成功、102枚・APK記録を確認。画像から街の接合／配置・図鑑初画面・今日の受取表示に不足を発見し、同じ工程内で修正した。新しい実Unity／修正版APK／Pixelは未実施。[段階を分けた報告](../../base-product-quality-2026-10-20.md)。完了チェックはその範囲だけを示す。

## Global Constraints

- Core／既存セーブ／5×6／納品報酬／正式6名／ロゴを変更しない。
- 5エリア・自由な歩行面・通常徒歩の連続カメラ、48以上の操作領域を守る。
- APK提示は最後の統合版のみ。完成判定はPixelの体験と同条件比較に基づく。

## Review Focus

- 配置した建物・家具にキャラが入る、施設前の経路が塞がる、NPCが同じ座席を使う。
- Today／図鑑から遠隔会話して発見数だけ増える、動く相手の古い目的地へ案内する。
- 扉への移動を中断しても自動入場する、別の施設を誤って開く。
- 320幅／短画面／Pixel縦長で必要数・下段セル・ボタンの文字が隠れる。
- 設定／一時停止／再起動でMerge演出や会話が保存済み進行を再実行する。

## Task 1: 街の景観・広い経路・段階成長

Files: `TownSceneryLayout/View`, `TownWorldSurface`, `TownGroundGraphic`, `TownWalkableArea`, `TownGrowthPresentation/View/PropGraphic`, new `TownVillageDressing` / graphic and transparent scenery atlas.
Interface: `TownSceneryView.SetTownLevel(int)`; same declarative dressing provides drawing and solid footprints. All stable `TownWorldMap.Place` anchors remain reachable.
- [x] Focused geometry tests first: every entrance / anchor walkable or intentional approach; four joins connected; multiple landmark routes; solids match rendering.
- [x] Build composed destination groups and quieter continuous materials; preserve dimensions and locations of the 5 areas.
- [ ] Verify Lv1/3/6 composition, culling and local growth highlight in native captures.

## Task 2: Flat Merge readability and tactile manipulation

Files: `Merge*.cs`, `QuestMergeScreens.cs`, focused pure/native Merge tests.
Interface: same30-cell mapping, UIFlow actions, sparse saved inventory and node names. Frame management is secondary; requirements remain visible.
- [x] Write and run failing geometry/drag feedback tests before changing behavior.
- [x] Implement clear cells / selected pairs / lift / merge attraction / sparkle / landing and concise tool shelf.
- [x] Pure geometry verifies all30targets and requirement hit height at320wide/small and tall sizes.
- [ ] Native verification: failed/invalid drag, busy delivery, pause/resume and saved-cell invariance; visual legibility and actual48+target accessibility.

## Task 3: Original-art character life

Files: `TownActor`, `TownCharacterMotion`, `CharacterBodyGraphic`, `BodyReaction`, focused motion tests.
Interface: retain resident positions, original atlas UVs, depth and pointer/facility locks.
- [x] Pin finite/bounded motion, footfall and turn/rest/arrival semantics in focused tests.
- [x] Improve weight transfer, walk/readiness/reaction without redesigning faces/clothes.
- [ ] Check frozen input, live walking, resting and six art silhouettes.

## Task 4: Real destinations and daily/book integration

Files: `TownFacilityEntry/View`, `TownTodayView`, `TownNextGoal`, `TownGoalCard`, `TownMenuView`, `CharacterSelectionScreen`, `TownCharacterBook`, `TownDiscoveryNotice`; root owns `UIFlowController`, `TopHomeScreens`, world wiring.
Interfaces: `DestinationFor(string id)`, `TapDoor`, `VisitTownCharacter(string id)`, `TownCharacterDestination(string id)`, `OpenCafe`, flower atelier in `OpenShopPreview`.
- [x] Replace auto-entry tests with arrival-waits/explicit-near-door/no-remote-entry assertions.
- [x] Route Today/book to actual resident/neighbor destination; count talk only through real meeting.
- [x] Implement 48+targets, scrolling, errors, NEW/next-unlock/claim invariants and simplified street HUD; native verification remains pending.

## Task 5: Integrated validation and real-screen acceptance

- [x] Run Core/pure Presentation suites, actual Unity-reference6assemblies and glyph/resource checks; inspect failures rather than weakening assertions.
- [x] Fresh whole-change review; fix important issues with reproduction tests and full green suite.
- [x] Commit the combined candidate on the current feature branch; no main integration.
- [x] Receive180369d: native143/203 pass,102images and same-fingerprint APK records; inspect Before/received images.
- [x] Reject unfinished paving/book/daily composition and implement bounded refinements.
- [ ] New Windows-native run for the refinements:205 declared PlayMode cases,102 captures, same-fingerprint APK; compare the actual new After.
- [ ] Pixel walks/drag/merge/doors/daily/book/BGM/save restart and user experience acceptance. Explicitly leave unavailable stages pending; never infer them from compilation.
