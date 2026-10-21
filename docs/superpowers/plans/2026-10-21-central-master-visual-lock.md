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
- [x] Unity実PNGを正式Visual Lock・前版と比較し、見た目・密度・歩行の中央合否を記録。b25658eは未達で不採用、歩行／Pixel未確認。
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


### Task 4a: 実描画で判明した中央の不足をまとめて修正

承認方向の内部調整。5地区構造・原画・Core・成長計算・保存を変更しない。実画像の比較報告は `References/Review/central-master-2026-10-21/native-review.json`。中央の採用ゲートは閉じたまま。

- [x] 噴水の画面占有率・拡大した基礎・両側の経路、見える成長区画、中央NPCと5地区の居場所を検査してREDを確認。
- [x] 噴水の描画と基礎を共通定数へ揃える。既存の広い歩行面を保ち、接近可能な足場を再検査。
- [x] Lv1／3／6の置換花壇を下部UIより上へ追加し、街灯を正式原画のまま見える大きさへ。既存の下端の仕上げは維持。
- [x] 重複する西の初期NPC1名を中央ベンチへ。残り4地区にも1名ずつ残し、記録・会話は自動加算しない。
- [x] 純C#全回帰・参照コンパイル・既存ネイティブ期待値の材質／基礎整合を確認。
- [ ] 新しい中央実PNGで再判定するまでは未採用。他地区・Mergeへ進めない。


Task4aローカル結果：新規4検査はRED4→GREEN8。旧通路検査が全件で1件失敗し、不要な前面四角形の除去で旧検査と中央9件GREEN、最終全249／249。実DLL6asmdefは警告／エラー0。中央の実歩行コントローラ経路検査をネイティブ3件の中へ追加したが、この新候補では未実行。最終の独立レビューに重大／重要／軽微の指摘なし。実描画・Pixelの合格を意味しない。


### Task 4b: 中央の構図・成長・住人の統合（実描画2fcab4aの不採用対応）

実画像＋独立レビューで中央は再度不採用。噴水の拡大と地表の改善は維持し、悪化した花壇の積層は戻して再構成。構造変更ではなく正式Visual Lockへ落とし込む中央の限定仕上げ。

- [x] 上側の目的地／灯り、ベンチを隠さない置換花壇、画面内の祝祭飾り、名札の見通しをREDで確認。
- [x] 既存の掲示板・灯り・小花壇で上側の道に奥行きと目当てを作る。四方向の道と噴水ループを保持。
- [x] 下端の見えない成長を初期の低花壇へ戻し、見える花壇は低く置換。旧中央Lv3／6の同位置の花壇も積まず置換。Lv6は既存バンティングを見える位置へ。
- [x] 中央の初期友達はベンチから既存「広場」活動の石畳へ。正式原画・アニメ・タッチ範囲は維持し、会話・保存の加算なし。
- [x] 固定基礎・全経路・名札の配置を検査し、純C#全件と参照コンパイル。内部CPU図は実Unityと区別する。
- [ ] 実Unity再撮影で判定するまで中央未合格。他地区・Merge・APKへ進めない。


Task4b結果：実画像の独立レビューの重要3点に対し一度の構図修正。構図4件RED→GREEN、成長入力改変RED→GREEN。内部CPU投影で初期友達が草花上にいる点を検出し、実際の石畳帯を検査するRED→位置修正。最終全253／253、参照6asmdefは警告／エラー0。ゲートとAndroid／撮影スクリプト検査成功。CPU図は非Unity、源泉PNG・ゲーム原画は未改変。新しい実画面で再判定するまでは中央未合格で、後工程を止める。
