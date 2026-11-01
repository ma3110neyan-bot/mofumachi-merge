# モモ単体通過後の最終確認

最新追記：113430の最終実行はEdit144/144通過、Play250中245成功・5失敗で停止。旧会話／名前表示テストの整合を修正した。次は [失敗5件＋関連2件の限定確認](characters-final-focused-qa-2026-11-01.md) のみ。以下の最終工程全体は、その結果確認後に戻る。

最新111715-505e7b09の4ZIPを確認した。実Unity1/1成功、failed／skip／inconclusive0、終了0。source34a9748／FP0eed7901はCLI・proof・source-infoで一致。89原本中82画像をデコード・SHA照合し、11組のBefore／Afterと5組の接地影を再計算して範囲外変更0。歩行4方向、背面、停止、まばたき、会話／挨拶、入力、Sorting5配置、保存不変の単体検査を確認した。原本と照合記録は `References/Review/momo-final-capture-passed-2026-11-01-111715/`。

モモの限定確認は通過。6名同時・新全件Unity・新APK・Pixelの成功とは区別する。Sorting専用の配置では、表示中の会話枠が帽子の上部に触れる例があるため、会話枠の位置も最終実機確認に残す。6名の頭接続Before／After12組は次の専用fixtureで取得する。今回の39連続標本は固定刻みで、FPSや発熱の測定ではない。

今回は記録だけで、ゲーム・検査・scripts・原画・保護入力を変更していない。お手元の34a9748には次の実行に必要なコードがすべてあるため、Git更新は不要。

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
