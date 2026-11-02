# Merge「ごほうびマージ」改修前の記録

ユーザー指定の正式Visual Lockは `References/Merge/merge_visual_lock_reward.png`。2026-11-02、作業ツリー・Git履歴・GitHub最新作業ブランチを確認したが未存在。ユーザー本文のMarkdown部分にも画像データがない。**この画像を確認するまで製品UIの実装を開始しない。** 既存の別Visual Lock画像で代用しない。

## 復元点

- 変更前コミット：`af659256b481aae0697cea7ccfbf5531c6a62c08`
- ローカルチェックポイント：`checkpoint/merge-before-reward-20261102`
- 元UI・保護入力のSHA：`docs/verification/merge-reward-before-2026-11-02.json`
- SaveDataは変更・初期化しない。現行の実Unity Before画像は新規撮影していない。過去PNGを今回のBeforeと偽らない。

ロールバックは、作業完了時に確定したMerge UIの変更パスだけを、このコミットから復元する。全体resetでキャラクター等の変更を戻さない。新規Merge専用ファイルも今回所有物だけを対象にする。

## 実装前の変更候補一覧

- `Assets/Mofumachi/Presentation/QuestMergeScreens.cs`：BuildMergeとMerge専用レイアウト部分のみ。BuildQuestは維持。
- `Assets/Mofumachi/Presentation/MergePresentation.cs`：依頼・進捗・不足数表示とMerge専用レスポンシブ配置。
- `Assets/Mofumachi/Presentation/MergeCellSurfaceGraphic.cs`：セルのCream/Mint背景と薄い装飾。
- `Assets/Mofumachi/Presentation/MergeEffectsView.cs`：成功時だけの軽い達成演出。
- `Assets/Mofumachi/Presentation/MergeSparkleGraphic.cs`：選択・可能Mergeと軽量な花/きらめき。
- 必要な場合のみMerge専用の装飾Graphic/素材を追加。名称と変更範囲は正式画像確認後、実装前に追記する。
- Merge関連UIテストと証拠採取手順だけを追加/調整。Coreのルール検査は既存のものを利用する。

上記は未確定の変更候補。画像確認後に最小の対象へ確定する。まだ製品コードは編集していない。

## 固定事項

5列×6行フラット、正式ItemAtlasのお茶/パン/花、Merge/生成/Lv/依頼/納品/報酬/SaveData/街Lvのロジック、現在のフォントを維持。キャラはfix onlyへ移行し今回触らない。街/噴水/図鑑/BGM等も触らない。Shared UIWidgetsや全体フォントを変えずMergeローカルに装飾を適用する。

画像確認後は新デザイン選択・中間承認を求めず、関連テストとVisual Lock比較を含む実装候補まで進める。全件は候補後だけ。Unityライセンス/Pixel未接続の現在のクラウドで、実画面・全件・APK・Pixel成功を先に宣言しない。
