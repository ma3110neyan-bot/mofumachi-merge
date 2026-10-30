# モモ単体：ノア方式の逐次横展開ゲート

ノアはユーザー合格・方式固定。モモの素材／ゲーム接続は実装候補で、実Unity・APK・Pixelは未確認。ルル／ポム／フィオ／ミエルは変更していない。5名完了後の全件・APK・Pixelの順序を守るため、この手順はモモの単体確認だけを行う。

ノアのNoahAuthoredMotionを直接使い、正面は同じく正式atlasを無変形で表示する。左右斜め・後ろは7剛体パーツと閉じ目素材、共有atlas／28頂点。モモにないポーチは追加しない。垂れ耳を含む素材の端ではなく顎と襟を接続する登録を調整し、原本PNGは変更せず保存した。後ろ素材の隣接部は透明な登録ガターで切り分ける。顔や耳・衣装の見た目の最終判定は実画面で行う。

素材確認のブラウザーでは正式正面の完全一致、3方向×4動作／閉じ目、37固定時刻の標本、顎と襟の接続、390px表示、JavaScriptエラーなしを確認。純粋な実移動モデルでは4経路の終端・4方向・横向きでの会話への接近と意図した停止を確認した。これらはUnity・Pixel・FPSの証拠ではない。実Unityを動かせないクラウドでライセンスやAndroid環境の再試行は行わない。

## Windows

Unity Editorを閉じ、以下をまとめて実行する。実行ポリシーを指定した子PowerShell内で最初のhelperから読み込む。既存feature以外では停止する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuGit = Find-SourceGit
    if ((& $mofuGit branch --show-current).Trim() -ne "feat/daily-world-pass-20261016") {
        throw "対象ブランチではありません。この表示を送ってください。"
    }
    & $mofuGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止します。" }
    & ".\scripts\capture-character-authored.ps1" -Character momo
}
```

`Mofumachi.Tests.ExperienceCaptureTests.MomoAuthoredPartsPreserveIdentityInputAndState` だけを実行。表示・実公開GuidePointの歩行／停止／4方向・元表示への復帰・キャラタップ・実TryBeginConversationの新パーツ挨拶／会話・5つの前後表示配置・保存不変・予期しないエラーを確認する。NPCの停止や配置は隔離fixture内だけ。会話の製品ルールは変えない。Sortingの5配置は経路検査とは区別して画像保存する。

失敗しても今回の `Builds\Diagnostics\momo-authored-日時-ID-*.zip` に原本を保存して停止する。画面に表示された今回分のZIPを共有する。単体成功後も実画像を照合し、原画の印象、歩行・足元・方向間のつながりを確認してからルルへ進む。単体結果だけで正式採用・全員完了・Pixel合格にしない。

今回はAPK作成／Pixel install／アンインストール／セーブ初期化／全件テストを行わない。Noah用旧退役リグ手順へ戻らない。
