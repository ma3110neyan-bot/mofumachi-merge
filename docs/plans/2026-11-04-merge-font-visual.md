# Mergeフォント比較候補

基点c84e5d9。ユーザーの2026-11-04指示を仕様とする。旧Visual Lock、配色、位置、サイズ、5×6、アイテム、報酬・タスク・Save・街・キャラは不変。途中確認不要の既存指示に従い、比較候補まで進めてVisual確認で停止する。

## 設計

Hachi Maru Popは公式Google FontsメタデータでもRegular400の1種類。現在はUGUI Textなので、TMP移行やフォント資産の作り直しはしない。A＝Hachi＋軽い擬似太字、B＝A＋薄い濃ピンク輪郭0.35参照px、C＝既存Kiwi Maru Medium500。候補の既定値はB、正式採用は未確定。Merge内のタイトル・戻る・生成・整理・依頼／納品・下部ナビだけを対象にする。メニューは小サイズでもHachiのBold、数値・情報は既存Zen Maru Gothicのまま。既存の強い文字影は対象文字だけ無効にする。Rectや色を変えず、Bの輪郭色だけ追加する。

細いピンク帯／中央コインはCapturePinkBaselineのBefore専用と確認済み。Afterの実装を維持し、フォント候補の撮影は必ずAfterで行う。

## 作業とファイル

1. `MergeFontProfile.cs`に純粋な対象名・A/B/Cの役割定義、`MergeTypography.cs`にUGUI適用を追加。`ReleaseTypography.cs`へMerge内だけの分岐1行。関連hostテストを先に追加してRED→GREEN。既存の他画面ポリシーを維持する。
2. 旧UX manifestを編集せず、新`merge-font-visual-2026-11-04.json`と限定scope読取でReleaseTypographyの正確な旧／新SHAだけ許可する。元のガード・Core・表示6ソースは維持する。
3. 既存の撮影テストに任意のフォント比較を追加。新helperで同一fixtureのA/B/Cを720×1480と640×1072の6枚にし、JSON・SHA・HTML・終了後の実在を検証する。通常CaptureOnlyの11枚契約は維持。フルテスト設定は変更しない。
4. 実TTFをFreeTypeで描く素材標本6枚と一覧を作る。太字・輪郭は近似で、Unity描画とは明示的に区別する。クラウドではUnity実行不可なので、実Unity比較用CaptureOnlyだけの経路も準備する。今回全件・APK・Pixelには進まない。
5. 関連host／PS fixture・APIコンパイル・元資産不変を確認し、1回の独立レビュー後に作業ブランチへ通常push。Visual確認で停止する。

## 確認重点

- 小さいメニューが全体ポリシーのZenへ戻らないこと。
- Merge以外に字体・太さ・縁取りが波及しないこと。
- 同一Textへの繰返し適用でOutlineが増殖しないこと。
- 比較用A/B/Cを変えてもRect・盤面・State・Save回数が変わらないこと。
- 画像・JSON・HTML欠損、SHA不一致、掃除失敗は比較成功にしないこと。
