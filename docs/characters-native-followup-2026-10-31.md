# 最新ポム通過・モモ／ルルの前回原本確認

最新ポム121731-30b7e04d、sourcecdf36e0の実Unityは1/1成功。修正した後ろ向きの瞬きについても、毎回実描画する形状／UV検査と厳格ログ検査を通過した。11画像対の範囲外0、70画像のハッシュ・bytes・寸法・デコード、素材入力digestを再確認した。左右／後ろ／正式正面、歩行・停止、挨拶・会話の確認範囲に新たな破綻は見当たらない。追加修正なしで次のQA工程へ進行可。ただし正式採用・実機負荷の合格ではない。

同梱のモモ114346-a1a2d711とルル114536-68ef34c0も、原XMLで各1/1成功を確認した。各70画像・11因果対を照合し、当時のsource4fa3a9eと同じvalidatorで証拠を検証した。両者は後ろ向き修正前の歴史的結果で、新しい必須rear_blink_verifiedは存在しない。新しい版の成功へ流用しない。ルルは11比較対・40標本・Sorting5枚等を主担当が確認。モモは既存の全画像確認記録を保持し、今回の画像は抜き取り視認と全数数値照合を行った。

ノア185815-7308feb2の2ZIPは承認済み方式の過去資料として保存し、当時の1/1成功、98画像・8対を照合した。原本のadopted:falseや未完成項目を改変せず、後にユーザーが明示した正式基準の承認はそのまま維持する。これを今回の追加5名やPixel負荷の合格証拠にはしない。

統合run104645と114313のreview ZIPは停止時のsource-infoのみ。どちらも全件QA・6名同時・APK生成へ未到達で、完成runとして再利用しない。フィオ／ミエルの実Unity原本と、最新版の5名一式・6名同時・APK・Pixelの結果はまだ未受領。原本と版ごとの判定は `References/Review/characters-native-followup-2026-10-31/`。今回調べた13ZIP以外の24個の過去名ファイルは再展開せず、既存の過去記録を保持した。

今回は受領・記録更新のみで、ゲーム／撮影／素材／固定ノア／街・北境界・噴水等は無変更。40標本は固定シミュレーションで実FPSではなく、静的Sorting5枚も全ての遮蔽経路の証明ではない。接地感、頭と襟のつなぎ目、向き切替、既存会話パネルの部分重なり、同時表示負荷はPixelで確認する。

次は既存の以下の手順で、5名単体を順番に確認→全員成功後に全EditMode／PlayMode各1回→6名同時→比較2APK→保存維持のPixel更新・600秒測定。既存の通過者も同一最新版として検査する。失敗すればその場で停止し、対象だけを調査する。個別画像承認待ちや新しい素材方式の検討へ戻らない。

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
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗しました。" }
    try {
        & ".\scripts\prepare-characters-pixel-preview.ps1" -Install -Mode authored -Serial "99RAY1BELH" -MeasureSeconds 600
    }
    finally {
        Invoke-Item -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Diagnostics"
    }
}
```

APK工程を通過したらPixelで街を開き、PowerShellの案内でEnterを押して測定する。終了または停止後の結果画面を確認する。旧ZIPを手探りで選ばず、必要な場合はその画面に出た正確なrunのファイルを共有する。アンインストール・Save初期化・main統合はしない。性能の増減判定は同じrunのbaselineとの同条件比較も必要で、未測定を合格にしない。
