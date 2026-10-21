# Central Master Visual Lock Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox syntax.

**Goal:** 承認された画面の品質を中央広場へ落とし込み、実描画合格前に他地区へ進めない。
**Architecture:** 既存5地区・連続座標・正式素材を保持し、中央に描き込んだ地表と初期装飾を追加する。道・基礎・成長・奥行きは同じ配置を参照。中央のみのUnityレビューを全体APK工程から分ける。
**Tech Stack:** Unity6000.6.4f1 UGUI/C#9、NUnit、PowerShell5.1。
**Spec:** docs/superpowers/specs/2026-10-21-central-master-visual-lock.md

## Global Constraints

- 提示画像が正式Visual Lock。9月22日案は補助基準。
- 中央合格→西→東→北→南→接続→Merge→UI→基本ループ→Unity全件→QA APK→Pixelの順。
- Core／SaveData／5×6／納品報酬／正式6名／TOP／図鑑／日次／BGMを保持。既存153凍結入力を更新して通さない。
- 同条件の実Unity BeforeとAfterを比較。素材・CPU配置図は実描画ではない。

## Review Focus

- Lv1でも地表や成長予約地が完成した景観になり、Lv3／6でさらに変わる。
- 四方向の広い接続と噴水両側、滞在点の経路が装飾で塞がれない。
- 中央の装飾が他4地区やMergeへ漏れず、ワールド投影と深度が一致する。
- 圧縮した地表にUI／キャラを焼き込まず、見えない障害物を追加しない。
- 中央未合格・画像欠落・基準変更は未採用になり、QA APKへ進めない。

### Task 1: 正式基準と復旧点
**Files:** References/VisualLock/approved-design-2026-10-21.json; scripts/visual-lock.ps1; scripts/tests/visual-lock.tests.ps1; AGENTS.md
**Interfaces:** Assert-ApprovedVisualLock(repositoryPath) → 承認原本のSHAを検査。Assert-CentralStageApproved(repositoryPath) → 中央の明示合格がない場合は拒否。
- [x] 原本差替えと未合格段階を拒否する一時fixtureをREDで確認。
- [x] 中央／Mergeの承認、8判定軸、段階保留、復旧タグを記録。
- [x] 凍結／承認の検査をGREENにし、基準と検査を保存。

### Task 2: 中央広場マスター制作と組み込み
**Files:** new TownCentralGround.png/.meta; TownCentralMasterLayout.cs/.meta; TownCentralGroundGraphic.cs/.meta; TownContinuousWorldView.cs; TownVillageDressing.cs; TownSceneryView.cs; Tests/Presentation/CentralMasterTests.cs; Tests/Presentation/Mofumachi.Visual.Tests.csproj
**Interfaces:** TownCentralMasterLayout.Resource/Bounds/Contains; TownCentralGroundGraphic.Bind(world)/Refresh；中央専用TownCentralMasterLayout.Dressingに初期・成長配置。
- [x] 実データから歩行面・物体のガイドを作り、画像ツールで中央の地表素材を制作。正式原画は変更しない。
- [x] 中央の初期景観・成長・他地区不変・入口／すれ違いを検査するREDを確認。
- [x] 中央地表と奥行き付き装飾を組み込み、既存の歩行幅・衝突・指追従を維持。
- [x] 純C#全回帰と6asmdef参照コンパイル。内部構成プレビューは実Unityと区別し、まだ弱い案は採用しない。

### Task 3: 中央だけのUnityレビュー
**Files:** Assets/Mofumachi/Tests/PlayMode/CentralMasterReviewTests.cs/.meta; scripts/build-android.ps1; scripts/central-review.ps1; scripts/tests/central-review.tests.ps1
**Interfaces:** -CentralReview → 中央Lv1/3/6実PNG・同一source情報・Before／Lock／候補のJPEG・未合格記録。APK生成なし。
- [x] 不一致source／欠落画像／原本改変／中央未合格を拒否する検査をREDで確認。
- [x] 同じ既存撮影条件で中央3段階を撮影する隔離テストを追加し、状態を書き換えない。
- [x] 中央専用レビューと比較JPEGを実装。全体TownQaは中央未合格なら止める。
- [x] ローカル検査とDLLコンパイル後、実Editor起動の可否を確認。Unity実描画できなければ採用せず待機。

### Task 4: 中央の採用判定（後工程の開始条件）
**Files:** 中央レビュー証拠と段階記録
- [ ] Unity実PNGを正式Visual Lock・前版と比較し、見た目・密度・歩行の中央合否を記録。
- [ ] 合格後のみ後工程へ。西→東→北→南→接続→Merge→UI→基本ループ→Unity全件→APK→Pixelを順に実施。
- [ ] 本版のPixel受入まで完成と呼ばない。環境阻害時は中央待機と未着手の工程を明記。


## 実行結果と阻害（2026-10-21）

Task1–3は素材／ソース／ローカル検査／撮影工程の実装まで。Task4の実Unity見た目・歩行合格は未実施で、西以降を開始していない。

- 純C#表示／経路245、Core125が成功。実DLL参照6asmdefコンパイルは警告／エラー0。Unityの実行成功ではない。
- PowerShell5スイート成功。合成PNG／XML／mock ADBによるゲート検査であり、撮影・Windows JPEG出力・Pixel操作の証拠ではない。
- 最終レビューの重要指摘3件に対応：中央入力digestと実画像／Before／承認画像／比較JPEGへ採用を結び付け、中央だけの合格でAPKへ進めず後工程の順序も検査。初回中央撮影前に縦画面を確定し、実座標との一致をネイティブ撮影テストで要求。
- 新規ゲートの欠陥をREDで確認してから修正しGREEN。撮影時の座標一致検査はDLLコンパイルのみで、実Unityでの実行は未了。
- Editor6000.6.4f1の新規起動は終了198、`No valid Unity Editor license found`。Androidモジュール／Pixel USB経路もないため、中央の実PNG・APK・Pixel画面は生成していない。
- 中央のみのWindows工程：`scripts/build-android.ps1 -CentralReview`。3枚と比較JPEGをZIPにし、APKを作らない。
