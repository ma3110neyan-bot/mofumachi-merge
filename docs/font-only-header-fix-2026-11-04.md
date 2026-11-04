# フォント撮影の停止原因と、同じ作業フォルダでの再確認

受領run：`font-only-20261104-143729-65167335`、source `94a954079c40f01fe94db269dd6f824abfeb148a`。

## 確認結果

準備2工程は終了0。フォント関連PlayModeは2件実行、1件成功・1件失敗。`StoryRolesAndNoticeFitPortraitSizes`は成功。失敗は `Current request caption` の必要行高16pxに対し、既存枠12pxで収まらなかったため。XMLのスイート検出件数262は全件実行を意味しない。

撮影helperへ入る前に停止し、親 `capture_verified=false`／画像0件／`source_unchanged=true`。元のFailed XMLや親JSONは書き換えない。受領11ファイルを無加工のZIPとSHA一覧として `References/Review/font-only-failed-20261104-143729-65167335/` に保存した。

## 局所修正

現在のフォント切替はHachi／Zenを同じ文字サイズで試すため、双方の行高が固定枠を超える補助行には不十分だった。対象は `Current request caption` と、同じ12／14px行の `Merge reward coins` だけ。まず従来サイズでHachi→必要時Zenを試し、双方が収まらない場合だけ文字を1段ずつ小さくする。8pxを下限とし、各サイズでHachiを優先。元の文字サイズを保持し、行に余裕が戻れば復元する。

枠・位置・色・盤面・ボタン・素材・キャラ・街・ロジック・Save・BGMは変更しない。他の小さい文字や主要メニューへ、この縮小処理を広げない。テストの収容条件は緩めず、同じ画面内の不適合文字をまとめて報告する。

関連hostテスト42件とAPI参照コンパイルを確認。固定行12px→拡張24px→12pxの収容・元サイズ復元・位置／色不変を確認する実Unity回帰検査を追加した。修正後の実Unity再実行と画像は未取得。完成／Visual合格とは扱わない。

## Windowsで1回だけ再確認

Unity Editorを閉じ、以下をWindows PowerShellへそのまま貼り付ける。今回作成済みの `font-afc3c3d1` とLibraryを再利用する。旧run・元の開発フォルダ・未コミット差分を保持し、fast-forwardできない場合や製品入力に未コミット変更がある場合は停止する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = 'Stop'
    $fontRoot = 'C:\Users\User\Desktop\Mofumachi\font-afc3c3d1'
    $previousRun = Join-Path $fontRoot 'Builds\Diagnostics\font-only-20261104-143729-65167335'
    $previousInfo = Join-Path $previousRun 'font-only-source-info.json'
    if (-not (Test-Path -LiteralPath $previousInfo -PathType Leaf)) { throw 'Previous evidence missing. No capture started.' }
    $previous = Get-Content -LiteralPath $previousInfo -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($previous.gitRevision -ne '94a954079c40f01fe94db269dd6f824abfeb148a' -or $previous.source_unchanged -ne $true) { throw 'Previous source identity differs.' }
    Set-Location -LiteralPath $fontRoot
    . '.\scripts\prepare-font-only-preview.ps1'
    $fontGit = Find-SourceGit
    Assert-CleanProductSource $fontRoot $fontGit
    & $fontGit merge-base --is-ancestor 94a9540 HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Unexpected font worktree revision.' }
    & $fontGit fetch origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw 'Fetch failed. No capture started.' }
    & $fontGit merge --ff-only origin/feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw 'Fast-forward failed. Existing changes preserved.' }
    & '.\scripts\prepare-font-only-preview.ps1'
}
```

成功時は新runの `font-only-review.zip` をExplorerで選択する。実Unity代表PNG10枚＋JSON／HTML／親証跡を含み、ZIP内SHAも再照合する。このZIPを直接添付してVisual確認で停止。全件EditMode／PlayMode・APK・Pixel・インストールへは進まない。
