# 6名の頭・胴体同期だけの修正

原因は、頭の矩形下端を接続基準にし、各方向素材の実際の顎・襟を同じ座標基準で結んでいなかったことです。別Animatorの追従遅延ではなく、既存の同一メッシュ内の接続登録の問題でした。

原素材の不透明な首の画素中心を準備時に登録し、そのフレームの胴体の首へ頭を接続します。個別キャラの手修正値、追従Tween、前フレームの頭座標は使いません。頭の角度・サイズ・UV、元のアニメーション値と頭以外の頂点は維持します。左右・背面・閉じ目素材も同じ方式です。

ローカルでは6名の左右・背面（計18ビュー）と閉じ目を含む46パーツ、関連16検査が成功しました。これは純粋C#の計算・素材検査です。新しい実Unityの描画はまだ未確認のため、「頭胴体ずれ0件／未達0名」の正式完了・固定は行っていません。

前回の受領ZIPは近接・タップの実Unity3/3成功です。今回の頭同期の合格へ流用しません。通常・Android・Editor・PlayMode等のAPIコンパイルは0警告/0エラー。原画、動作サンプル、Actor、歩行速度、影、向き方式、会話・すれ違い・街・Save等は変更していません。

## Windowsで頭同期1件だけを撮影

Unityを閉じ、PowerShellへ以下全体を貼り付けます。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuHeadGit = Find-SourceGit
    $mofuHeadBranch = & $mofuHeadGit branch --show-current
    if ($LASTEXITCODE -ne 0 -or $mofuHeadBranch.Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチが異なるため停止しました。"
    }
    & $mofuHeadGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗しました。" }
    & ".\scripts\capture-character-head-sync.ps1"
}
```

このコマンドは、6名の同じ原素材を一時準備し、頭同期のPlayMode **1メソッドだけ**を実行します。9状態×8刻み×6名の432メッシュ標本と、同じ素材・同じ瞬間のBefore/After36画像を保存します。正面は正式な一枚絵、左右・背面は顎と襟の接続、歩行開始・途中・停止前後・向き切替・閉じ目を確認します。頭以外の頂点、全UV、頭の辺ベクトルも比較します。

全件テスト・APK作成・Pixel更新・Saveリセットは実施しません。古い生成資源は既存の所有者検査に通るものだけ可逆退避し、今回の一時資源だけを終了時に片付けます。失敗時も原本とZIPを残します。

終了時にエクスプローラーで選択される、新しい **`characters-head-sync-...-review-*.zip`全パート**を添付してください。各runの`screenshots\head-sync\head-sync-review.html`でも比較を開けます。前の全件・Pixel用コマンドを今回の限定撮影へ代用しないでください。

6名全員の新しい描画結果がそろい、頭胴体ずれ0件・未達0名を確認後に正式固定します。Merge改修は別指示を待ちます。
