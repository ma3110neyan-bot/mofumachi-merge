# 限定7件通過後の最終確認

最新123101-bda3c51fの原XMLは、限定PlayMode7/7成功、failed／skip／inconclusive0、Unity終了0。失敗していた5件と、タップ専用／実ポインター連打の回帰2件をすべて確認した。source7931c06／FPa8b5349eは原CLI・source-infoと一致し、実行中の変更なし。原本3記録とSHAは `References/Review/characters-finish-focused-passed-2026-11-01-123101/`。

前段階のモモ111715は実Unity1/1と82画像／11比較対／接地影5対を確認済み。113430はEdit144/144通過、Play245/250で停止し、その5件を今回の限定7件で修正後確認した。これらを現在版の全件成功へ合算しない。同じ一つの版での最終受入工程へ戻る。

6名同時・新全件Unity・新APK・Pixelは未確認。モモのSorting専用配置では、会話枠が帽子の上部に触れる例があるため、枠の位置も最終実機確認に残す。6名の頭接続Before／After12組は次の専用fixtureで取得する。単体の連続標本は固定刻みで、FPSや発熱の測定ではない。

今回は記録だけで、ゲーム・検査・scripts・原画・保護入力を変更していない。お手元の7931c06には次の実行に必要なコードがすべてあるため、Git更新は不要。

Unityを閉じ、Pixelを接続・画面解除し、USBデバッグを許可して、PowerShellで次の1行を実行する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\scripts\prepare-characters-final-pass.ps1" -Install -Serial "99RAY1BELH" -MeasureSeconds 600
```

同じ一つのsourceでノア1件→5名を順番に単体確認→全件EditMode／PlayMode各1回→6名の比較・すれ違い・タップ→QA APK→Pixelへ進む。単体の修正候補が通った後の最終受入検査であり、失敗ごとの全件反復はしない。失敗したらその新runの結果で止まる。

Pixelへは今回の6名2.5D版を最初に入れる。街が表示されたらPCでEnterを押し、10分間確認する。

- 頭／胴／足のつながり、サイズ・接地影
- 歩行／停止／前後左右、上移動で背面が出ること
- すれ違い時のブレ、背景との前後関係
- 自動会話なし、タップ会話、連打で重複しないこと、会話枠がキャラを隠さないこと
- カクつき、タップ遅延、発熱、メモリ、クラッシュ、再起動後の保存

6名同時を実機で見られなければunknownと回答する。Unityの同時表示で実機結果を代用しない。最後にExplorerで選択される今回のfinal-review ZIPを全パート添付する。途中停止なら新しいrunのエラーとZIPを共有し、全体を自動で再実行しない。

クラウド純粋C#全体の長時間経路ケースは前回中断の保留が残る。今回それを成功へ変更したり、全件を再実行したりしていない。新Unity／Pixelの結果とこの残課題が揃うまで、formal_fixed／adoptedはfalse。
