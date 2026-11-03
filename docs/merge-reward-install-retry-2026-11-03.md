# 同じAPKでPixelのインストールだけ再確認

受領`pixel-20261103-152656-75832264.zip`はCRC正常・原本2件。APK SHAは`39c4f014e3bea8c17dd454655d247a80e5c6432e94df209528d90ee1f851e9ca`、sourceはa8795c2。停止点は`adb install -r`のストリーミング転送で、Android側の詳細理由は出ていない。PID未取得、アプリ起動／FPS計測は未到達。crash-bufferは`- waiting for device -`だけで、有効なクラッシュログではない。元のcrash_log_captured=trueは書き換えず、受領receiptで未確認と区別した。

根本原因はまだ確定していない。接続状態を確認し、転送方式だけを`--no-streaming`へ変える1回の診断を行う。APK／SHA・パッケージ・既存QAデータを保持する。アプリ削除、pm clear、downgrade許可、全件テスト、素材6件検査、APK再生成は行わない。

PixelをUSBでつなぎ直し、画面ロックを解除する。USBデバッグ許可が出た場合は許可し、接続したまま次を実行する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mergeGit = Find-SourceGit
    if ((& $mergeGit branch --show-current).Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチが異なります。"
    }
    & $mergeGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗しました。" }
    & ".\scripts\test-merge-reward-pixel.ps1" -BuildDirectory "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Diagnostics\merge-reward-reviewed-pixel-20261103-152132-db7ec19a" -Serial "99RAY1BELH" -MeasureSeconds 180 -NonStreaming
}
```

原QA／素材6件／APK SHAを読み直してから同じAPKをinstall-r --no-streamingで1回だけ更新する。自動再試行なし。今回はADB版、端末状態、/data空き容量、現在のQAパッケージ情報も保存する。切断／待機をクラッシュログ成功にしない。

成功後、PixelでMergeを開き、PCの案内でEnterを押して3分操作する。納品後はMergeへ戻り、最後もMergeを表示する。失敗した場合は繰り返さず、新しいPixel ZIPを共有する。

最後に選択される新しい`pixel-日時-ID.zip`と、同じフォルダの既存`build-review-01.zip`を添付する。後者はまだ受領していないため、素材6件・APK生成原ログの独立確認が残っている。Pixel合格／クラッシュなし／性能良好はまだ判定しない。
