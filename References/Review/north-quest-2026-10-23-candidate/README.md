# 北候補：受領版は採用保留

受領ソースfabf00e／実行20261023-094203-35c07a1f。4ZIPの同一ソース・描画入力・元ファイルを照合し、実Unity12件成功、実PNG21枚、固定Before12組、参照／比較JPEG4組を確認した。同じXMLを48件と数えない。元の68ファイルと撮影時のfalse／null採用JSONは変更せず保持する。

[北の比較JPEG](north/north-before-candidate.jpg) はLv1／3／6をBefore／Candidate／中央のVisual Lockデザイン参照で示す。[接続場面のJPEG](north/north-connection-review.jpg) は表示用の形式変換のみ。元の実PNGは `north/screenshots/north-connection.png` に保持し、派生JPEGのSHAは `derived-jpeg.json` に記録する。

独立の実画像レビューでは中央・西・東に新たな重大／重要な後退なし。既存15枚中10枚は同一、変更5枚も比較した。北の景観・密度は基準に沿うが、接続画像にキャラの重なりがありHOLD。

レビュー原文 `reviewer-original.txt` の「誘導中」「顔の大半」は後の確認で言い過ぎと修正された。`reviewer-clarification.txt` と `native-review.json` のroot判定を併読する。静止画だけで制御対象や継続的なNPC衝突を断定しない。

ソース追跡では、背景のカメラ更新後にキャラのRectTransform・名前・奥行きが次の描画フレーム待ちとなる経路を確認。接続画像は画面再構築／カメラ変更直後に撮影していた。カメラ更新とキャラ位置の同期、およびモデルを進めない撮影直前の描画更新を実装した。**修正後の実Unity画像は未取得**。このフォルダーは修正前の証拠であり、新ソースの合格証拠ではない。

北のユーザー承認は未取得。南・APK・Pixelへ進めない。案内とドアの部分的な重なり、舗装の反復は従来形式にある弱点として接続／UI工程へ持ち越す。
