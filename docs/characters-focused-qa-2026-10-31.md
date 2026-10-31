# 5名通過後の全件QA停止：検査コードの限定修正

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
