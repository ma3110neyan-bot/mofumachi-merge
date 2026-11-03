# Windows：今回候補の重点確認 → 最終QA

今回のクラウド検証は関連C#24件成功、3フォント572文字の字形／SHA検査成功、Presentation・PlayModeコードのUnity APIコンパイル0警告0エラー、PowerShellの範囲・証跡テスト成功。**Unity実行・全件・APK・Pixelは今回候補では未取得**。

ロゴは色の比較見本のみ。製品の原本は変えない。街Lvの設定も据え置き。花は既存の保存先を「今日」の画面で確認できるようにしただけ。

## 一括実行

Unity Editorを閉じ、Pixel 3aを接続・ロック解除しUSBデバッグを許可して実行する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $uxGit = Find-SourceGit
    if ((& $uxGit branch --show-current).Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチが異なるため停止しました。"
    }
    & $uxGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止します。" }
    & ".\scripts\prepare-release-ux-preview.ps1" -Final -Install -Serial "99RAY1BELH" -MeasureSeconds 180
}
```

今回関連のネイティブ重点8件（フォント・今日・既存枠・Merge実描画）→既存Merge重点→全EditMode1回→全PlayMode1回→同一候補のQA APK1個→保存を保持する更新インストール→既存Pixel計測の順。どこか失敗したら停止して原本を保持し、自動で全件・ビルドを繰り返さない。

全件前の重点だけを先に行う場合は、上記の最後の行を以下にする。

```powershell
& ".\scripts\prepare-release-ux-preview.ps1"
```

こちらは全件・APK・インストールを行わない。`Builds/Diagnostics/release-ux-focus-*`に`capture.xml`、`capture.log`、`release-ux-source-info.json`、11枚の既存Merge描画JPEG／JSON／HTMLを保存する。撮影成功は実ファイルとSHAの最終確認が前提。フォントの全対象画面はネイティブ検査で枠内収容を確認するが、11枚の画像はMergeの各状態であり、全画面のBefore/After撮影ではない。フォント変更前後は提出した素材標本と区別する。

`-Final`の結果フォルダは既存`merge-reward-*`。XML3本、同じ候補のJPEG・APK、`release-ux-parent.json`で重点runとの対応を記録する。過去版のXML/APK/Pixel結果を流用しない。HTMLの新フォントは「今回候補・未承認」と表示する。

## Pixelで見る項目

- タイトル・ボタン・キャラ会話は世界観を維持し、小さい本文・Coins・Lv・進捗は読みやすいか。欠け・潰れ・切れ・はみ出しがないか。
- 「今日」で受領前→受領→受取日／累計が見えるか。再度開いた時、再起動時、翌日の保持。日付を巻き戻したりSaveを編集して試験しない。
- 以前の街・キャラ・Merge・BGMを維持しているか。今回街Lv値は変えていないため、初期化せず実プレイ時間と操作感だけ記録する。
- カクつき・タップ遅延・発熱・メモリ・クラッシュ。計測完了だけを目視／操作合格と扱わない。

Pixelだけ失敗した場合は全件やAPKを再作成せず、正確な今回`merge-reward-*`runを`test-merge-reward-pixel.ps1 -BuildDirectory`へ渡して続行する。`uninstall`、`pm clear`、Save初期化は行わない。
