# ノア単体撮影：動作コンポーネントの無効化を修正

run `noah-reprototype-20261029-122017-2b20bef3`／source `09bef62` を受領。実Unityは0/1、skip0。前回の影の `CanvasRenderer` 欠落は解消し、Home再入場、実際の頂点／陰影検査、最初の静止比較まで進んでいる。今回は撮影テスト側の不備。

テストが `TownLifeView.enabled=false` にしたまま `AdvanceFrame` と公開 `GuidePoint` を呼んでいた。製品の `CanSimulate` は `isActiveAndEnabled` が条件なので、手動更新は進まず、12番目の標本前の歩行指示が正しく拒否された。受領12コマがすべて同じSHA256であることも確認した。歩行面や目的地を変更する問題ではない。

変更は既存撮影メソッドだけ。街コンポーネントを有効なまま保ち、yieldのない同期ループ内で1/30秒を手動更新する。Unityの通常Updateはループへ割り込まない。さらに、開始時に実際の操作可能状態、各stepで実シミュレーション時計の進み、Unityフレーム番号の不変を検査する。製品の入力制限を迂回せず、テスト用の時刻だけを進めて動作したと扱わない。

受領した画像15点（PNG2点・JPEG13点）を全デコード。静止1対の変更画素は再計算で10,131、変更範囲は画像左上基準x308／y745／124×157。ノアの元の顔・服・尾は識別できるが、静止の途中成果だけで2.5Dを合格にはしない。歩行／停止／方向転換／会話の証拠は未受領。完成したJSON／HTMLもない。

実装済み・修正後の実Unityは未確認。関連PlayMode assemblyのAPIコンパイルと固定入力検査を実行。ゲーム本体・原画・他5名・北境界・噴水・Save等は今回無変更。全件テスト、APK、Pixelは実行せず、6名展開も禁止のまま。

GitHub Desktopで `feat/daily-world-pass-20261016` をPullし、Unityを閉じて同じ単体撮影だけ再実行する。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\capture-noah-reprototype.ps1"
```

新しく生成された `noah-reprototype-日時-ID-*.zip` の全パートを共有する。以前のZIPは再添付不要。成功時は新しい比較HTMLが開く。全件QAを先に繰り返さない。
