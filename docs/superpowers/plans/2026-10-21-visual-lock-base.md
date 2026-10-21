# Visual Lock Base Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox syntax.

**Goal:** 中央を基準に街の密度と目的地を戻し、平面Mergeの見やすさを上げ、凍結対象を守る。
**Architecture:** 5地区の既存連続座標と原画を使うPresentation変更。宣言した配置を描画・衝突で共有。ゲーム進行と保存へは書き込まない。
**Tech Stack:** Unity6000.6.4f1 UGUI/C#9、NUnit、PowerShell5.1互換。
**Spec:** docs/superpowers/specs/2026-10-21-visual-lock-base-design.md

## Global Constraints

- 5エリア構成、5×6、正式6名、ロゴ、TOP、図鑑基本構造、日次、セーブ、依頼・納品・報酬、BGMを維持。
- 追加画像・音源・SDKなし。キャラの高度2.5Dは今回行わない。
- Pixel未確認は完成・採用にしない。旧ソースの成功を新版へ引き継がない。

## Review Focus

- 狭い320×536と長い360×808で全30マスとアイテムが見え、入力は48以上。
- 新しい植栽／家具が入口・噴水両側・合流を塞がない。
- NPCとのすれ違い／静止した相手への接近が詰まらない。
- 初期Lvに見えない障害物や、Lv6でだけ通れなくなる場所を作らない。
- 保存進行／日次／図鑑／BGMが変わらず、更新後再起動で復元する。

### Task 1: 凍結と後退判定
**Files:** References/VisualLock/base-2026-10-21.json; scripts/visual-lock.ps1; scripts/tests/visual-lock.tests.ps1; scripts/build-android.ps1
**Interfaces:** Assert-BaseVisualLock(repositoryPath, manifestPath) → 変更した凍結入力を拒否。
- [x] 実ファイルを変更／復元する一時fixtureでhash不一致のREDを確認。
- [x] テキストはLF正規化、画像／音源はbytes、TOPはBuildTitle区間で検査する。
- [x] ビルド開始／終了で同じゲートを実行。旧の検査は緩めない。

### Task 2: 中央から5エリアの街構成
**Files:** TownVillageDressing.cs; TownSceneryLayout.cs; TownVillageDetailGraphic.cs; TownGroundGraphic.cs; TownWorldSurface.cs; Tests/Presentation/VillageCompositionTests.cs
**Interfaces:** 配置Kind/Footprint、既存PavingPolygons/LocalWalksをそのまま使う。
- [x] 噴水脇の座席・接続の植栽、6つの小道の先の家具が足りないREDを確認。
- [x] 共有アトラスの街灯・ベンチを登録し、各目的地周辺の植栽と席を配置。舗装の過大な石を細かくする。
- [x] 旧の幅・道・全入口の接続テストを全成功に保つ。現データを使う内部配置プレビューを確認（Unity描画とは明確に区別）。

### Task 3: Mergeと街の可読性
**Files:** QuestMergeScreens.cs; MergeCellSurfaceGraphic.cs; PerspectiveBoardGraphic.cs; MergePresentation.cs; TownLifeView.cs; TownWorldWalkModel.cs; TownMenuView.cs; related tests
**Interfaces:** 既存30セルindex/矩形と選択・ドラッグ・効果を維持。
- [x] 既存アイテム枠をより大きく安全に使う境界と近い住人のすれ違いをREDで検査。
- [x] カラフルな浅い盤面／装飾枠／控えたLv帯を実装。名札の顔隠れを抑え、メニューのスクロールを示す。
- [x] 純C#全回帰、6asmdef実DLLコンパイル、既存PowerShell全テスト。

### Task 4: 同条件比較と実機の後退ゲート
**Files:** docs/visual-lock-base-2026-10-21.md; native capture/review evidence; Pixel QA helper
**Interfaces:** run/revision/fingerprint → 同じ候補の証拠のみ採用。
- [x] Before102枚のSHAと原本を保持。同名102工程の自動比較HTML・Sept22基準・未採用JSONを用意。
- [ ] 今回ソースのUnity102枚を取得し、中央／MergeをSept22と実画面で比較する。
- [x] Pixelの同じ操作時間の画面／SurfaceView表示時刻／PSSを採る補助と未確認を保留する検査を用意。
- [ ] PixelでBefore／Afterを実測し、操作・保存・負荷の合否を決める。
- [ ] Unity実描画→APK→Pixelを実施可能な環境で行う。阻害される段階と理由を明記。小出しAPKを作らない。

## 現段階

Task1〜3はソース・ローカル回帰の範囲で実装済み。Unity／APK／Pixelの採用は未実施。6asmdefコンパイルはネイティブ実行ではない。今回のAfterがないため、内部CPU配置図を提出用Before／Afterへ混ぜない。
