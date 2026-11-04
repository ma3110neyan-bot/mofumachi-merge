# 狭い図鑑タイトルの文字収容と再撮影

受領run：`font-only-20261104-192108-59fa8b94`、source `1ae54b5ff0a9a3012d1c7ed785774de8e7549b7f`。

## 確認できたこと

準備2工程終了0。関連PlayMode2件中1件成功・1件失敗。前回までのMerge補助文字・設定説明文の回帰検査は通過。全代表画面×3サイズの事前検査で不合格として記録されたのは、320×592の図鑑 `Page title` 1項目だけ。幅142.68px・高さ37.7568pxの固定枠で18サイズの「キャラクター図鑑」が折り返し、必要高さ59pxとなった。`StoryTypographyTests`は成功。

撮影helper前で失敗しているため、画像0件／`capture_verified=false`。受領原本11件は無加工ZIPとSHA一覧を `References/Review/font-only-failed-20261104-192108-59fa8b94/` に保存。Failed XMLを書き換えず、今回の失敗と修正後の結果を分ける。

## 局所修正

既存Hachi TTFの文字送り合計は8em。18サイズでは144px、17では136pxになる。枠142.68pxを動かさず、図鑑のページタイトルだけを元の18から必要時に1段ずつ調整する。下限16、Hachiと軽いBoldを維持し、別フォントには置き換えない。広い画面では18に戻す。他のページタイトル・メニューはこの調整対象にしない。文字列・枠・色・レイアウト・素材・図鑑動作・ゲームロジックは変更しない。

142.68幅→161.7幅→142.68幅で、収容・元サイズ復元・Hachi維持・枠／位置／色／行間不変を確認する実Unity回帰検査を追加した。さらに、実画像640×1072のローカル寸法に当たる320×536も事前検査に加え、4サイズの不適合をまとめて報告する。フォント関連2件のみで、判定条件は緩めない。

修正後の実Unity結果・画像・Visual合格は未取得。全件テスト・APK・Pixelには進まない。

## Windowsで1回だけ再確認

Unity Editorを閉じ、以下をWindows PowerShellへそのまま貼り付ける。既存の `font-afc3c3d1` とLibraryを再利用し、過去のrun・未コミット差分は保持する。fast-forward不可や製品入力の未コミット変更は停止条件のまま。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = 'Stop'
    $fontRoot = 'C:\Users\User\Desktop\Mofumachi\font-afc3c3d1'
    $previousRun = Join-Path $fontRoot 'Builds\Diagnostics\font-only-20261104-192108-59fa8b94'
    $previousInfo = Join-Path $previousRun 'font-only-source-info.json'
    if (-not (Test-Path -LiteralPath $previousInfo -PathType Leaf)) { throw 'Previous evidence missing. No capture started.' }
    $previous = Get-Content -LiteralPath $previousInfo -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($previous.gitRevision -ne '1ae54b5ff0a9a3012d1c7ed785774de8e7549b7f' -or $previous.source_unchanged -ne $true) { throw 'Previous source identity differs.' }
    Set-Location -LiteralPath $fontRoot
    . '.\scripts\prepare-font-only-preview.ps1'
    $fontGit = Find-SourceGit
    Assert-CleanProductSource $fontRoot $fontGit
    & $fontGit merge-base --is-ancestor 1ae54b5 HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Unexpected font worktree revision.' }
    & $fontGit fetch origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw 'Fetch failed. No capture started.' }
    & $fontGit merge --ff-only origin/feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw 'Fast-forward failed. Existing changes preserved.' }
    & '.\scripts\prepare-font-only-preview.ps1'
}
```

成功時は新runの `font-only-review.zip` をExplorerで選択する。街・会話・Merge・依頼・図鑑・設定を720×1480／640×1072で各1枚、計12枚の実Unity PNGと対応JSON／HTML／親記録を含む。実ファイル・解像度・SHA・後処理後の再検証・ZIP内15件のSHAを確認して初めて撮影成功。このZIPを直接添付してVisual確認で停止。
