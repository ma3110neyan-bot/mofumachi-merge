# Head / Body位置補正候補：6名個別連続確認

状態：**実装済み・Unity実描画／Pixel未確認**。正式完了ではありません。

原因は、原素材ごとに異なる襟・顎に対する静的な登録不足と、5名のHead登録Offsetを首振り角度側で回していたことです。元から単一Graphic内の共有Rootで、別Animator/Tweenの追従遅延はありませんでした。

同じ現在フレームのBody座標系でHead位置を解決し、キャラ×左／右／背面18種類の静的な登録値を適用します。Headの既存角度とサイズ、胴体／手足、正式原画、向き、影、速度、背面、会話、すれ違い、街／UI／Save等は無変更です。正面は原一枚絵を維持します。添付見本は配置の基準として扱い、元画像や正式原画を加工していません。

JPEGは `References/Review/character-head-calibration-2026-11-02/`。すべて素材・コード再現であり、実Unity／Pixel画像ではありません。

## Windowsで頭だけを確認

以下をWindows PowerShellにまとめて貼り付けます。RemoteSignedは子PowerShellだけです。既存の作業ブランチを通常更新し、頭限定の新1メソッドだけ実行します。Unity Hub操作は不要ですがUnity本体での実描画確認は必要です。Unity Editorは閉じて実行してください。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuGit = Find-SourceGit
    if ((& $mofuGit branch --show-current).Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチが異なるため停止しました。"
    }
    & $mofuGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止しました。" }
    & ".\scripts\capture-character-head-calibration.ps1"
}
```

ノア→モモ→ルル→ポム→フィオ→ミエル、各1054姿勢を60Hzモデルで測定します。正面3秒→左3秒→右3秒→背面3秒→正面→停止→再歩行＋連続方向切替。停止区間はRootも固定、方向転換は全パーツを同じ姿勢で更新します。キャラ単体480×600のJPEGを通常5fps＋切替境界、連続方向切替区間は毎フレーム撮影します。実時間の街移動／FPS評価とは別です。

終了時、今回の `characters-head-calibration-...-review-01.zip` が選択されたエクスプローラーが開きます。複数ZIPなら同じ実行名の全番号が必要です。中の `screenshots/head-calibration/head-calibration-review.html` でキャラごとに連続確認できます。JPEGはUnity自身で全デコード、Windows検査でもGDI+で全デコード・SHA照合します。

この手順は全件テスト、街全景、Merge/Save/BGM確認、APK生成、ADB、インストールを実行しません。保存データを削除しません。過去の検査結果は今回の合格へ代用しません。6名それぞれの技術結果と目視判定を分けて記録し、現Unity画像が全員PASSになるまでは正式固定・全体テスト・Merge改修へ進みません。

## 関連検証のみ（クラウド）

- 実Graphic CLR検査：各1774モデルフレーム、非Head不変・全UV不変・頭の角度／サイズ不変、頂点不一致0。
- 原画の襟被覆／衣装非被覆：各1308分割フレーム、6名個別不合格0。
- Presentation／Editor／PlayModeTestsのAPIコンパイル：ネイティブUnity成功を意味しません。
- 個別証拠の欠落・ズレ・停止中移動・旧証拠・偽合格と画像破損の拒否、原画／既存固定入力の保護：関連PowerShell検査のみ。

現在、実Unity連続目視の個別判定は6名とも保留です。完了条件を下げてPASSへ置換しません。
