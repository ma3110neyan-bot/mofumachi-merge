# 5名通過後の全件QA停止：検査コードの限定修正

## 最新：限定6件の実Unity原本を確認、次は最終QA・Pixel

`characters-qa-focus-20261031-133851-5be71820-review-01.zip` の原本3点を受領。ZIPのCRC／SHA、XMLの正確な6メソッド・6成功／失敗0／skip0／inconclusive0、ログのUnity6000.6.4f1・終了コード0を照合した。約99.26秒。source `94ca8c6f95fa33d14dad184e4ace20f6dd65af54`、fingerprint `778de54aa9d485027edee9154f721e9558b7d8bd116e839573f701e7d5350c34` はUnity引数とsource-infoで一致。元の `Archive:` エラー記録は保持し、今回の回復ZIPが正常に作られたことを別途確認した。

原本と判定は `References/Review/characters-focused-2026-10-31-133851/`。旧失敗5件＋新しい可視性回帰がすべて通過。画面内の誤cull／空meshを拒否する検査も実Unityで通った。噴水の実Updateによる30〜60秒内ショーの検査は成功したが、今回の小さいZIPには画像／fountain-live.jsonがないため、正確な初回時刻や新しい見た目を原本から独立再評価したことにはしない。

94ca8c6以降はZIP用DLL修正と記録のみで、ネイティブ対象コード／全素材は同一。今回も製品・テスト・ノア・街を変更しない。限定6件の成功を全件／6名同時／APK／Pixel合格に置換しない。次は既存の最終経路を一度実行する。同じソース・素材に結び付く受入資料を揃えるため5単体を順番に行い、全員通過後に全件各1回→6名fixture→比較2APK→保存維持install-r→Pixel600秒へ進む。停止した旧124623runや限定runを、完成APKのBuildDirectoryとして再利用しない。

Pixel3aを接続・画面解除し、Unityを閉じて下記を一括実行する。街が表示されたらPowerShellでEnter、案内に従い10分試遊してカクつき／タップ／熱を回答する。実機では正面・左右／前後・歩行停止・会話挨拶・背景との前後関係を確認する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuGit = Find-SourceGit
    if ((& $mofuGit branch --show-current).Trim() -ne "feat/daily-world-pass-20261016") { throw "作業ブランチが異なります。" }
    & $mofuGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗しました。" }
    $mofuFinalStarted = Get-Date
    try {
        & ".\scripts\prepare-characters-pixel-preview.ps1" -Install -Mode authored -Serial "99RAY1BELH" -MeasureSeconds 600
    } finally {
        $mofuReviewZip = Get-ChildItem -LiteralPath ".\Builds\Diagnostics" -Filter "characters-authored-*-review-01.zip" -File |
            Where-Object { $_.LastWriteTime -ge $mofuFinalStarted } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($mofuReviewZip) { Start-Process explorer.exe -ArgumentList ('/select,"' + $mofuReviewZip.FullName + '"') }
        else { Invoke-Item -LiteralPath ".\Builds\Diagnostics" }
    }
}
```

個別プレビュー承認待ちは復活させない。ノア基準・街／北境界／噴水・Save等は固定。baseline APKもノアは有効で、他5名が従来表示。Pixelで負荷増加なしと結論するには同じ完成runのbaselineを同条件で測定する必要がある。今回はまずauthoredで試遊し、実測なしに合格と呼ばない。統合review ZIPはPixel測定前に作られ、実機結果は別の `pixel-authored-*/characters-pixel.json` と収集フォルダーに保存される。途中停止時は新しいrunの画面と該当ZIPだけを共有し、無条件に全体を反復しない。

以下の圧縮回復手順は完了済みの履歴。

## 追記：限定6件は画面上で通過、ZIP作成だけ停止

ユーザーのWindows画面に `characters-qa-focus-20261031-133851-5be71820`、`Tests passed: 6/6` を確認。その後 `Write-CharactersReviewArchives` で `[IO.Compression.ZipArchiveMode]` が見つからず停止。今回の再添付は旧124623のZIPとSHA256 `2d02798e9febc35f00f1f3f93fc27556e5c50a31b053fc78f0a3f2c0f2d8a2c8` が同一で、今回の6件のXML原本はまだ未受領。画面での通過とXMLの独立確認を区別する。

原因はWindows PowerShellの依存DLLロード。`System.IO.Compression.FileSystem` だけでは `ZipArchiveMode` を含む `System.IO.Compression` が型解決時にロード済みとは限らない。元の全体経路では先行単体の `Compress-Archive` が両DLLをロードしていたが、限定検査はそこを通らない。Microsoft.PowerShell.Archiveの実装、既存 `five-area-review.ps1` も両DLLを明示ロードする。共有writerに不足した `Add-Type -AssemblyName System.IO.Compression` だけを追加した。製品／テスト／描画・合格ノアは変更しない。

既存の限定runのXML／ログ／source-infoを保持し、圧縮だけを回復する。source-info内の過去の `Archive:` エラーは書き換えない。下記はXMLの6件を再照合してZIPを作るだけで、Unity／全件／単体／APK／Pixelを再実行しない。冒頭でDLLを直接ロードするため、現在の94ca8c6 checkoutでも回復できる。共有writer修正は次の通常pullでも取得する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    . ".\scripts\diagnose-characters-qa.ps1"
    $mofuRun = ".\Builds\Diagnostics\characters-qa-focus-20261031-133851-5be71820"
    Assert-CharactersFocusedResults (Join-Path $mofuRun "focused-playmode.xml")
    $mofuZips = @(Write-CharactersReviewArchives (Resolve-Path -LiteralPath $mofuRun).Path)
    Write-Host ("添付ファイル: " + $mofuZips[0])
    Start-Process explorer.exe -ArgumentList ('/select,"' + $mofuZips[0] + '"')
}
```

既存のPSアーカイブ検査はPowerShell7環境で元コードも成功していたため、Windows5の再現結果として扱わない。修正後も実ZIPの階層／APK除外を含む同じ関連検査を確認する。Windows5での回復成功と新XMLの独立確認は、このコマンド後の添付待ち。以下の全6件実行手順は履歴であり、今回繰り返さない。

受領原本 `characters-authored-20261031-124623-4875bad5-review-01.zip` は source `237b12a62075ab23a2751a94802ef3eae94bb5de`、fingerprint `7afd33903a9c24d22383957c923adf7418039c63c6e8d710b8e0a29be0ec3be2`。5単体通過の親記録、EditMode144/144、PlayMode248件中243成功・5失敗。skip／inconclusive0。6名fixture・APK・Pixelは未到達。原本5点とSHA／XML判定は `References/Review/characters-native-2026-10-31-124623/` に保存した。最新5単体の画像ZIPは今回未受領であり、フィオ／ミエルの見た目確認を主張しない。

## 切り分けと変更

- LayoutNotice／PopPresentation：無効化済みの旧影にもネイティブ矩形clipを要求していた。UGUIでは無効Graphicがマスクから登録解除される。全Graphicのmaskable／親マスク確認を保持し、有効Graphicはclipあり、無効Graphicはclipなしを検査する。有効Graphic数も正に限定し、全スキップでの成功を防ぐ。既存の配置・全身・UI重なり検査は維持。
- Noah25DTrialPresentation：CanvasRendererから借りたMeshを切替／再構築後に再読取りしていた。切替前に取得済みの頂点／UV／色の配列を使う。元の全数値・原画UV・状態／時計・Sorting検査を維持する。退役試作方式を再採用する変更ではない。
- ExperienceCapture2件：失敗はPortraitの非null Mesh検査。`RegionVisible`は左右64／上下90単位の先読みも含み、実際の画面内とは異なる。UGUIは完全にmask外のGraphicの再構築を省略し得る。撮影検査を、UGUI公開APIで取得した複合mask・paddingとroot canvas内の実Graphic矩形から可視性を求める方式へ修正した。実際のcullフラグだけでスキップしない。画面内はcull=false・clip有効・非null／非空mesh・全頂点near/far内を引き続き必須とする。原本には位置診断がないため、当該2件が本当に完全mask外だったかはWindows再実行まで未確定。新しい失敗文には矩形・clip・cullを残す。

新しい実Unity回帰1件は、隔離fixture内だけでノアを既存の中央検査地点へ配置し、カメラを動かして実際の先読み／完全mask外・空meshを確認。その後画面内へ戻し、意図的な誤cullとmesh消去の両方が検査で拒否されることを確認する。これは描画検査用の配置であり、歩行・実FPSの証拠ではない。保存書込みは増えないことを検査する。

製品側のPresentation／Editor／Core・全キャラ素材・合格済みNoah master26固定ファイルは変更なし。未採用の旧Noah game-candidate manifestに含まれる旧テスト3ファイルの期待SHAだけを更新し、前SHA・理由・基点commitを `qa_test_maintenance_2026_10_31` へ保存。他549保護入力と552パス構成は不変。承認済みmaster manifestと過去の街／噴水baselineは変更しない。旧TownQaの歴史的baseline検査を今回の6名配布経路として使わない。

## クラウドでの確認

- 変更を含む全PlayModeソースのUnity6000.6.4f1 APIコンパイル：成功、警告0／エラー0。
- `scripts/tests/characters-focused-qa.tests.ps1`：正しい6件の受理、欠落／追加／重複／異なるメソッド／失敗／skipの拒否、PlayModeのみの正確なfilterを確認。合成XMLのガード検査であり、実Unity成功ではない。
- 承認済みNoah masterと現552入力の検査：成功。前552との差は旧テスト3点だけ、他549は元SHAと一致。
- `scripts/tests/noah-authored-frozen-newlines.tests.ps1`：LF／CRLF許容、実内容を変えたlink.xml2点／PNGを拒否。

実Unityの修正後6件、全件QA、6名同時、APK、Pixelは未確認。クラウドには実Unity用ライセンス／接続されたPixelがないため、APIコンパイルを実描画成功へ置き換えない。

## 次にWindowsで実行するもの

Unity Editorを閉じ、以下をPowerShellへ一括貼付する。既存featureへfast-forwardするだけで、main統合・保存初期化は行わない。途中失敗時は今回のZIPだけを添付し、全件QAや全5名の単体を繰り返さない。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuGit = Find-SourceGit
    if ((& $mofuGit branch --show-current).Trim() -ne "feat/daily-world-pass-20261016") { throw "作業ブランチが異なります。" }
    & $mofuGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止します。" }
    $mofuFocusStarted = Get-Date
    try {
        & ".\scripts\diagnose-characters-qa.ps1"
    } finally {
        $mofuFocusZip = Get-ChildItem -LiteralPath ".\Builds\Diagnostics" -Filter "characters-qa-focus-*-review-01.zip" -File |
            Where-Object { $_.LastWriteTime -ge $mofuFocusStarted } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($mofuFocusZip) { Start-Process explorer.exe -ArgumentList ('/select,"' + $mofuFocusZip.FullName + '"') }
        else { Invoke-Item -LiteralPath ".\Builds\Diagnostics" }
    }
}
```

実行するのは失敗5件＋新しい回帰1件のPlayModeだけ。画像原本は新runの `full-screenshots/`、小さい添付用ZIPはログ／XML／source-infoだけを含む。終了時に今回のZIPを選択したExplorerを開く。この6件の成功後に、最終の全件／6名／同ソースAPK／Pixelへ進める。まだPixel向けAPKは生成していない。
