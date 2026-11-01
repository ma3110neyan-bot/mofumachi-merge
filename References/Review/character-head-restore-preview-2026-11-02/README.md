# 6名の前後左右・JPEG確認用

**素材・コード再現プレビューです。Unity／Pixel実画面ではありません。**

現在のゲーム描画コード（復旧コミット`0f92ab2`）を最小のCLR描画APIで実行して頂点を取り出し、正式素材の切り抜き・元の登録マスクをその座標へ描いたJPEGです。画像側で頭や胴体を移動して隙間を閉じる補正はしていません。原画・ゲームコードは今回変更していません。

- `all-six-directions.jpg`：6名の正面・左・右・背面の一覧。
- `noah-four-directions.jpg`：ノア。
- `momo-four-directions.jpg`：モモ。
- `lulu-four-directions.jpg`：ルル。
- `pom-four-directions.jpg`：ポム。
- `fio-four-directions.jpg`：フィオ。
- `miel-four-directions.jpg`：ミエル。

個別JPEGは1980×2020、静止・歩行2位相・首元拡大を掲載。正面は元の正式一枚絵のままです。街・接地影・Sorting・Unityのアトラス圧縮は再現していません。方向転換の時間差は静止画では判定できません。

**確認した残問題：ルルの左右は頭と襟の間に隙間が見えます。現状は未合格です。** コード上の配置一致を、見た目の接続合格へ読み替えません。他キャラの首元も拡大欄で確認できる状態にしています。Pixelへは更新していません。

素材SHA、描画コードSHA、JPEGの実形式・寸法・SHAは`preview-source.json`、元の描画頂点は`mesh-data/`に保存しています。各JPEGにも実画面ではないことを明記しました。
