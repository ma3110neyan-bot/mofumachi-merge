# 設定説明文の局所修正と再撮影

受領run：`font-only-20261104-184959-07a44cbc`、source `eaa257f30676362eb7ebb8a001c0b47dd46d0412`。

## 今回の実結果

準備2工程終了0。関連PlayMode2件中1件成功・1件失敗。前回のMerge補助文字の収容／元サイズ復元検査と、最初の狭い画面でのMerge確認は通過した。その次の設定説明文で、必要高さ46pxが固定枠40pxを超過して停止した。`StoryTypographyTests`は成功。画像生成前なので `capture_verified=false`／画像0件、ソース不変。Failed XMLを成功へ書き換えない。受領原本11件は無加工ZIPとSHA一覧を `References/Review/font-only-failed-20261104-184959-07a44cbc/` に保存。

## 今回の修正範囲

`Settings description`だけ、HachiとZenを元の14で試しても収まらない場合に文字サイズを1段ずつ調整する。下限12、上限は元サイズ14。各サイズでHachiを優先し、枠に余裕が戻れば14に復元する。他の長文・ボタン・数値へこの縮小条件を広げない。設定画面の枠40px・位置・色・文面・BGM処理は変更しない。

テストは元と同じフォント関連2件。確認対象の全viewport・全代表画面の失敗をまとめて報告し、何か1つでも不合格なら撮影成功扱いにしない。判定基準は緩めない。説明文256×40→296×40→256×40の収容・元サイズ復元・枠／位置／色／行間不変の実Unity回帰検査も追加。

修正後の実Unity実行・画像・Visual判定は未取得。クラウドの合格と実Unityの合格を区別する。

## Windowsで1回だけ再確認

Unity Editorを閉じ、以下をWindows PowerShellへそのまま貼り付ける。今回も `font-afc3c3d1` と既存Libraryを再利用する。前回と今回の失敗runは保持。製品入力に未コミット差分がある場合やfast-forwardできない場合は停止し、差分を破棄しない。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = 'Stop'
    $fontRoot = 'C:\Users\User\Desktop\Mofumachi\font-afc3c3d1'
    $previousRun = Join-Path $fontRoot 'Builds\Diagnostics\font-only-20261104-184959-07a44cbc'
    $previousInfo = Join-Path $previousRun 'font-only-source-info.json'
    if (-not (Test-Path -LiteralPath $previousInfo -PathType Leaf)) { throw 'Previous evidence missing. No capture started.' }
    $previous = Get-Content -LiteralPath $previousInfo -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($previous.gitRevision -ne 'eaa257f30676362eb7ebb8a001c0b47dd46d0412' -or $previous.source_unchanged -ne $true) { throw 'Previous source identity differs.' }
    Set-Location -LiteralPath $fontRoot
    . '.\scripts\prepare-font-only-preview.ps1'
    $fontGit = Find-SourceGit
    Assert-CleanProductSource $fontRoot $fontGit
    & $fontGit merge-base --is-ancestor eaa257f HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Unexpected font worktree revision.' }
    & $fontGit fetch origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw 'Fetch failed. No capture started.' }
    & $fontGit merge --ff-only origin/feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw 'Fast-forward failed. Existing changes preserved.' }
    & '.\scripts\prepare-font-only-preview.ps1'
}
```

## 生成する代表画像

街・会話・Merge・依頼・図鑑・設定を720×1480／640×1072で各1枚、計12枚の実Unity PNG。設定画面を含め、実際の文字が読めるかをVisual確認する。フォント素材標本やA/B/Cは増やさない。

成功時は新runの `font-only-review.zip` をExplorerで選択する。PNG12枚・対応JSON・HTML・親記録の計15ファイルを梱包し、実ファイルとZIP内SHAを照合。1枚でも不足、後処理失敗、証跡不一致なら `capture_verified=false`。このZIPを直接添付してVisual確認で停止。全件EditMode／PlayMode・APK・Pixel・インストールへは進まない。
