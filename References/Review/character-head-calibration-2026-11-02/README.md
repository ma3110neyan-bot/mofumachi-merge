# 6名 Head / Body位置補正候補（2026-11-02）

現在は **実装済み・Unity実描画／Pixel未確認**。正式完了・固定ではない。

- [6名の正面・左・右・背面](all-six-directions.jpg)
- 各キャラ詳細：[ノア](noah-four-directions.jpg)／[モモ](momo-four-directions.jpg)／[ルル](lulu-four-directions.jpg)／[ポム](pom-four-directions.jpg)／[フィオ](fio-four-directions.jpg)／[ミエル](miel-four-directions.jpg)
- `*-four-directions.jpg`：各キャラの4方向×静止・歩行2位相＋首元拡大。
- [同条件のBefore／After比較](all-six-before-after.jpg)：前版9be23abと候補を比較。

全JPEGは正式原素材を実GraphicのCLR頂点で再現したソフトウェア画像。生成AI、手描き、画像側の位置補正はなし。前版のPNGや合格結果を流用していない。現在のRuntime2Graphicと個別静的補正表から出力した頂点をそのまま使用。原画、UV、サイズ、既存角度、描画順、頭以外のパーツは維持。正面は正式アトラスの一枚絵を維持。静止画像で動的合格を断定しない。

実行対象は頭と胴体の位置だけ。原画、向き切替、歩行速度、影、背景、会話、すれ違い、UI、Save、Merge、BGM等は変更していない。

## 計算検証

各キャラ1774モデルフレーム：指定のFront3秒→Left3秒→Right3秒→Back3秒→Front→停止→再歩行、さらに連続方向切替と各モード。正面は分離のない原一枚絵なので分割頂点検査の対象外。

6名それぞれ：頂点不一致0、非Head頂点／全UV／Headのサイズと角度は不変。各1308分割フレームで、実出力四角形から原素材の襟開口部の被覆と衣装マーカーの非被覆を検査し、不合格0。全頭の同じOffsetは使用していない。判定は **CLR／原素材検査のみ**。Unityインポート、圧縮、フレーム反映やPixelの合格を示さない。

`collar-verification.json` に個別結果・原素材SHA・検査範囲。`preview-source.json` に今回の描画コードSHA、素材SHA、各JPEGのSHAと寸法。`mesh-data/` に実Graphic出力。前版ノアを同じ新基準で検査すると5232頂点不一致→候補0（新静的配置との差を検出するRED／GREEN）。

## 実Unityの個別連続確認

`docs/characters-head-calibration-2026-11-02.md` の頭限定コマンドを使用。ノア→モモ→ルル→ポム→フィオ→ミエルの順、各1054姿勢を別々に確認し、キャラ単体JPEGと連続ビューアを出力。停止中はRootも静止、連続方向切替区間は毎フレーム撮影。全件、街、Merge、Save/BGMの確認、APK、Pixelインストールは実行しない。

6名の現Unity連続画像が個別PASSになるまで、正式完了・ズレ0件・未達0名とは判定しない。全体テストとMerge工程には進めない。
