# Mergeの小さなブランド名の文字欠けと再撮影

受領run：`font-only-20261104-195143-c2ae77d1`、source `0614cfe89c393d54921b1db73fbd6ab465bfeb7d`。

準備2工程は終了0。関連PlayModeは2件中1件成功・1件失敗、skip 0。4サイズ×各代表画面の事前収まり検査は通過し、その後の実撮影で `Merge brand: actual glyph mesh` が0となって停止した。`StoryTypographyTests`は成功。`source_unchanged=true`、`capture_verified=false`、検証済み画像数0。添付の会話画像は確認できるが、出力フォルダ全体は未受領なので実在する途中PNGの総数は未確認。検証済み0枚と、実ファイル0枚は区別する。

小さな「もふまちメルジュ」欄は既存の高さ48×0.94×0.28＝12.6336。Hachiの9サイズの行高はフォントメトリクスで13.032、8サイズでは11.584。以前は必要高さ13が枠高さ＋0.5以内として事前検査を通り、UGUIのTruncateで1行全体を描けない状態を見落としていた。描画頂点0を拒否する撮影判定は正しく動作しており、これを外さない。

## 局所修正

- `Merge brand`だけ高さの丸め許容を0とし、既存の狭いMerge文字用フォント調整へ追加。Hachiと軽いBoldを維持して、必要なときだけ9から8へ調整する。広い欄では元の9へ戻す。
- 既存ネイティブ回帰検査で、実寸12.6336→24→12.6336に対する文字生成・元サイズ復元・Hachi維持・枠／位置／色不変を確認する。
- 全代表画面の既存事前検査に、実TextGenerationSettingsと固定枠での文字頂点生成を加え、不具合を画面・サイズごとにまとめて検出する。実撮影時のCanvasRenderer頂点検査も維持し、失敗メッセージへ画面・サイズ・フォント・文字サイズ・枠寸法を加える。
- 製品変更は `ReleaseFontPolicy.cs`／`ReleaseTypography.cs` のフォント適用のみ。背景・枠・色・レイアウト・盤面・正式アイテム・ゲームロジック・SaveData・キャラ・街・BGM・TTFは変更しない。候補manifestは該当ソースのSHAのみ更新し、旧Visual Lock・安全ガード・撮影成功条件は維持する。

受領11原本は `References/Review/font-only-failed-20261104-195143-c2ae77d1/received-originals.zip` に無加工で保存し、CRCと各SHAを確認。元Failed XMLや以前の撮影結果は書き換えない。

クラウドでフォント関連host 54件成功、Presentation／PlayModeテストのUnity API参照コンパイル0警告0エラー、関連PowerShell契約と保護ガード、572字形・元TTF SHAを確認した。これは修正後の実Unity／Visual／Pixel合格ではない。実Unity再撮影は下記1回のみで、全件テスト・APK・Pixelへ進まない。

## Windowsで1回だけ再確認

Unity Editorを閉じ、以下をWindows PowerShellへそのまま貼り付ける。既存の `font-afc3c3d1` とLibraryを再利用し、過去のrun・未コミット差分は保持する。fast-forward不可や製品入力の未コミット変更は停止条件のまま。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = 'Stop'
    $fontRoot = 'C:\Users\User\Desktop\Mofumachi\font-afc3c3d1'
    $previousRun = Join-Path $fontRoot 'Builds\Diagnostics\font-only-20261104-195143-c2ae77d1'
    $previousInfo = Join-Path $previousRun 'font-only-source-info.json'
    if (-not (Test-Path -LiteralPath $previousInfo -PathType Leaf)) { throw 'Previous evidence missing. No capture started.' }
    $previous = Get-Content -LiteralPath $previousInfo -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($previous.gitRevision -ne '0614cfe89c393d54921b1db73fbd6ab465bfeb7d' -or $previous.source_unchanged -ne $true) { throw 'Previous source identity differs.' }
    Set-Location -LiteralPath $fontRoot
    . '.\scripts\prepare-font-only-preview.ps1'
    $fontGit = Find-SourceGit
    Assert-CleanProductSource $fontRoot $fontGit
    & $fontGit merge-base --is-ancestor 0614cfe HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Unexpected font worktree revision.' }
    & $fontGit fetch origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw 'Fetch failed. No capture started.' }
    & $fontGit merge --ff-only origin/feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw 'Fast-forward failed. Existing changes preserved.' }
    & '.\scripts\prepare-font-only-preview.ps1'
}
```

成功時は新runの `font-only-review.zip` をExplorerで選択する。街・会話・Merge・依頼・図鑑・設定を720×1480／640×1072で各1枚、計12枚の実Unity PNGと対応JSON／HTML／親記録を含む。実ファイル・解像度・SHA・後処理後の再検証・ZIP内15件のSHAを確認して初めて撮影成功。このZIPを直接添付してVisual確認で停止する。失敗時は繰り返さず、新runのcapture.log／capture.xml／親JSONを保持する。
