# 大規模仕上げ候補の比較

このフォルダーのPNGは、正式素材と今回の実装方針から作成した**素材・配置プレビュー**です。**Unity／Pixel 3aの実画面ではありません。** Beforeも前候補の配置を再構成した説明図で、旧APKの画面キャプチャではありません。可読性、操作、音、負荷、成長の達成感はWindows／Pixelで受け入れます。

- [全体比較PNG](comparison.png) — 約2 MB、1520×2824。PDFは使用していません。
- [街移動](walking.png)
- [Merge Before／After](merge.png)
- [キャラ動作の説明図](characters.png)
- [3曲選択](music.png)
- [3地域のLv.1／Lv.6](growth.png)
- [依頼と次目標](goals.png)

`comparison.html` は同じ素材からの開発用レイアウトです。生成は `python3 scripts/render-experience-previews.py`（開発PCのPlaywright＋Chromiumが必要、ゲームの依存追加はなし）。素材の加工・正式背景の描き換えはありません。新しい `MergeBoardFrame.png` は今回生成した盤面用装飾です。

Windowsの `scripts/build-android.ps1 -TownQa` はPlayMode検証中に、新規 `ExperienceCaptureTests` を通して `Builds/Android/<今回の日時>/screenshots/` へ**実際のUnity UGUI描画**を出力します。隔離したテスト用保存状態から720×1280のRenderTextureで取得し、プレイヤーの保存や選曲設定へ書きません。これらもPixel実機キャプチャではなく、GPU上の候補UI確認用です。この取得処理は現在クラウドでコンパイル確認済み、Windowsでは未実施です。

出力対象は初期／9種のMerge、選択状態、3地域それぞれのLv.1とLv.6、街歩き選択、歩行8コマ、依頼＋目標、選曲画面です。見た目の最終判断には、同じセーブからのPixel 3aスクリーンショットと10分の操作確認を合わせてください。
