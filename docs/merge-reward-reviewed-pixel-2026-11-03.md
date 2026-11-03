# Merge QA APK・Pixel3aへ継続

最新：APK生成後のinstall-rで停止したPixel ZIPを受領。現在はこのAPK生成コマンドを繰り返さず、[同じAPKのインストール再確認](merge-reward-install-retry-2026-11-03.md)を使う。以下は生成時の手順として保持する。

受領`merge-reward-repair8-20261103-141127-b59f61a2-review-01.zip`はCRC正常、15ファイルのSHAを確認。実Unityの8件は8/8成功、failed／skip／inconclusive0。CLIとsource-infoのsource9a45507／FP8c11a1d4／packed8922587bが一致し、元の4原本のSHAも一致する。今回のraw XML・ログ・ソースJSONと受領記録は`References/Review/merge-reward-repair-eight-passed-2026-11-03-141127/`に保存。

確認がそろった内容は重点53/53、Edit144/144、元のPlay252/260＋失敗8件の再確認8/8。元のPlay XMLは失敗記録のまま保持する。260/260の全件再実行が成功したとは記録しない。製品入力のGitツリーが元全件時点／修正時点／今回APKで同一であること、nativeテストソースが修正時点と同一であることを検査して引き継ぐ。Merge UI・ロジック・Save・正式素材・キャラ・街の変更はない。

元と修正後のpacked SHAは異なっている。過去の生成物はcleanup済みのため、メタデータだけが差の原因と断定しない。APK用に再生成した素材について**準備入力の6件だけ**をnative検査し、通過した正確なpacked SHAをビルド前後とAPK記録で照合する。全件Edit／Play・キャラ長時間撮影・JPEG再撮影は行わない。生成→6件検査→同じ素材でAPK→Pixelの順で、途中失敗時は停止する。

## Windowsで1回実行

Unityを閉じ、Pixel3aのUSB接続・ロック解除・USBデバッグ許可を済ませて貼り付ける。現在`-Final`や8件診断を再実行する必要はない。

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
    & ".\scripts\prepare-merge-reward-reviewed-pixel.ps1" -RepairDirectory "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Diagnostics\merge-reward-repair8-20261103-141127-b59f61a2" -Install -Serial "99RAY1BELH" -MeasureSeconds 180
}
```

既存の街QAアプリへinstall-rで更新する。アンインストール・保存初期化は行わない。PixelでMerge画面を開き、PC側の案内でEnterを押すと3分計測する。生成3種・選択・ドラッグ・Merge／Lv.UP・依頼／納品を通常の範囲で操作し、納品後はMergeへ戻る。最後もMergeを表示する。満杯／不正Mergeは自然に確認できる状態で確認し、既存進行を初期化して作らない。

## 結果

冒頭の`Exact reviewed Merge run: ...`が新結果フォルダ。その中に`build-review-01.zip`と`pixel-日時-ID.zip`ができる。終了後にExplorerでPixel ZIPを選択する。同じフォルダの**両方のZIP**を共有する。

ビルドZIPは元QA・修正8件・今回素材6件の実XML／ログ・ソース／素材／APK SHAを保持する。子ビルダーはテストを実行しないため、子のtests_run=falseを保持する。親は`qa_coverage_verified=true`と証跡の内訳を記録し、`full_qa_verified`／`full_playmode_rerun`はfalseのまま。古い失敗をPassedへ書き換えない。

Pixel ZIPはFPS／p95間隔・PSS・バッテリー温度差・thermalservice・実画面PNG・crashバッファを保持し、インストール済みAPK SHAとPIDを照合する。温度はバッテリーセンサーでありCPU温度ではない。タップ遅延・体感発熱・表示崩れ・カクつき・クラッシュの合否は実機結果を見て判断し、計測完了だけでpixel_verified／release_acceptedをtrueにしない。

実機接続／計測だけが失敗した場合は、ビルドを繰り返さず、表示された正確な新runを`test-merge-reward-pixel.ps1 -BuildDirectory`で指定して再開できる。元の全件テストと同じAPKを作り直す必要はない。

現状は**QA証跡確認済み・APK／Pixel未実施**。クラウドに有効なUnityライセンス・Android・USB実機がないため、上記Windows実行の結果をもって次の状態へ進める。
