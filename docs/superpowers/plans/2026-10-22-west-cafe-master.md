# West Cafe Master Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox syntax.

**Goal:** 承認された西カフェ案を既存の5地区・原画・保存を保って歩ける街へ落とし込む。
**Architecture:** 西専用の固定ワールド地表＋既存素材の深度付き配置。新規基礎を同じ配置から歩行判定へ接続。中央を含む非APKレビューで後退を検査する。
**Tech Stack:** Unity6000.6.4f1 UGUI/C#9、NUnit/.NET8、PowerShell5.1。
**Spec:** docs/superpowers/specs/2026-10-22-west-cafe-master.md

## Global Constraints

5地区、正式原画、5×6、Core・保存・報酬・納品・日次・図鑑・BGMは不変。街Lv間隔と成長説明ページは後工程。旧中央合格を共有コード変更後に流用しない。西以外の新規仕上げ・APKは行わない。

## Review Focus

- 初期庭席と既存Lv2庭席の重複、Lv6による入口や名札の隠れ。
- 左右の回遊と中央接続が新しい家具の基礎で塞がれないこと。
- 地表がカメラに貼り付かず、指入力を捕まえないこと。
- 共有描画変更後も中央の原画／装飾が同条件で描けること。
- 一致しないソース・不足画像のレビューや、西だけで全体APKを許す誤判定。

### Task 1: 西の配置・地表と経路
**Files:** TownWestMasterLayout.cs/.meta; TownWestGroundGraphic.cs/.meta; TownWestGround.png/.meta; TownSceneryView.cs; TownWalkableArea.cs; TownContinuousWorldView.cs; TownGrowthComparisonView.cs; Tests/Presentation/WestMasterTests.cs; visual csproj.
**Interfaces:** TownWestMasterLayout.Resource/TexturePoint/Dressing/VisibleAtLevel; 地表Bind/BindPreview/Refreshは中央と同じ投影。
- [x] 初期庭席はLv2で消える、全新規基礎がLv0から可視、入口・左右・中央が到達可能、西だけの固定UVを要求するテストをREDで確認。
- [x] ガイド付き地表を画像ツールで制作。原画やキャラ・家具を焼き込まず、西の前庭と左右の道を描く。
- [x] 西専用配置と共有描画・歩行を接続。低い花壇と庭席、ベンチ、灯りを道の外へ。
- [x] 純C#全回帰GREENとDLL参照コンパイル。技術用CPU配置図は非Unityと明示。

### Task 2: 中央＋西の実描画確認を準備
**Files:** WestMasterReviewTests.cs/.meta; ExperienceCaptureTests.cs; scripts/build-android.ps1; scripts/west-review.ps1; scripts/central-review.ps1; scripts/tests/west-review.tests.ps1.
**Interfaces:** -WestReview → 中央Lv1/3/6＋西Lv1/3/6＋接続／徒歩／入口、同ソース識別とZIP。APKなし。
- [x] source不一致／画像不足／改変／未知画像を拒否するPowerShellテストをREDで確認。
- [x] 西地表・コントローラ回遊・庭席置換・非保存をネイティブテストに追加。中央3画像の既存検査は維持。
- [x] 中央＋西の9PNG、固定Beforeの静止6組、正式中央Lockと西の承認配置案、JPEGとZIPを出力する。
- [x] PowerShell全件・参照コンパイルをGREENにし、Unity起動可否を確認。未実行は未確認と記録。

### Task 3: 確認・引き渡し
**Files:** evidence/docs/AGENTS/stage record。
- [x] 承認と後工程FBを記録し、共有入力が変わった中央段階を再確認待ちへ。西を未採用として保持。
- [x] 一度の独立レビュー、重大／重要のみ一度のRED→GREEN修正。
- [x] 凍結SHAと全関連検査を確認しfeatureへコミット。Windows -WestReviewを一つのコマンドで渡す。
- [x] 実画像受領後に中央／西を比較して合否判定。01fa982の中央は再合格、西の静止景観は合格、到着の可読性は保留。APK／Pixel／ベース完成は未了。

### Task 4: 実画像で検出した到着可読性の限定修正
**Files:** TownLifeView.cs; TownFacilityEntry.cs; TownFacilityView.cs; TownFacilityEntryTests.cs; WestMasterReviewTests.cs; ExperienceCaptureTests.cs; scripts/build-android.ps1; scripts/central-review.ps1; scripts/tests/android-tools.tests.ps1; review/stage/docs。
**Contract:** 自動会話は主人公の徒歩／施設の入店待ちを遮らず、意図的なNPC会話を維持。カフェ看板は文脈表示、ドア操作は維持。景観・成長速度・保存・報酬は不変。
- [x] 受領原本・同ソース／SHA照合を保存し、独立画像レビューと担当側の可読性保留判断を分けて記録。
- [x] 自動会話／看板の規則2RED→GREEN。Lv2以降のカフェ到着会話も競合しないよう接続。
- [x] Lv1／Lv3のクリック経路と二度目のタップ・入店、撮影時の会話なしをネイティブ検査へ。必須件数を6／West3／全件212へ引き上げ。
- [x] 純C#全260、DLL6asmdef、PS6、凍結／旧証拠拒否を確認。中央／西の新ソースゲートを再確認待ちへ。
- [ ] 修正版の中央3＋西6画像とネイティブ6検査を受領し、入口可読性と中央後退を判定してから東へ。APK／Pixelは後工程。

## 初回ソース引き渡し結果（履歴）

Task1–2とTask3のソース引き渡しまで。実装済み・実機未確認。新規配置4RED→GREEN、旧植栽の隠れ1RED→GREEN、既存の広い床／小道2件を配置修正で復旧、最終258／258。PS6スイート、実際のZIP／移動／Before照合（Windows JPEG描画のみ代替）、実DLL6asmdef警告／エラー0。独立ソースレビューの重大／重要／軽微指摘なし。Editor起動198でネイティブ検査は未実行。中央も含む-WestReview待ち。

## 実画像受領後の限定修正結果

01fa982の実Unity9枚・5検査を照合し、中央を再合格、西の静止景観を合格とした。到着中の自動会話の重なりは保留とし、一度の修正で自動会話／文脈看板を調整。全純C#260、DLL6、PS6が成功。修正版のネイティブ6検査・9画像は未確認、Pixel／APKなし。次の実画像判定後に東へ進む。
