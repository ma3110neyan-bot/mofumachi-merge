# 既存6名APK：検査・ビルド成功／完了記録だけ復旧してPixelへ

受領 `characters-resume-check-20261031-185823-dcff0179.zip` はSHA256 `dd194a2913f013405f6b229d7d1069251f6e37924295b8525c61f0624d271c4c`。Windowsの逆スラッシュZIP名を正規化してから重複／絶対／親参照を検査し、CRC・全46原本のSHAを保存した。原本と判定は `References/Review/characters-final-2026-10-31-135626/`。

検査・APKのsourceは `7af024864fc353a36ad886c034f1a2b22dda7648`、fingerprintは `2f2c786b2672cade0f4279d7fb3e1bb6053de1adcf56c89c6fbcae4706a9236d`。報告PCの現在値とも一致。モモ／ルル／ポム／フィオ／ミエル各1/1、全Edit144/144、全Play249/249、6名同時1/1、全てfailed／skip／inconclusive0。各5名の正式method名・source／素材digest・4方向・rear blink・Save／tap／会話／挨拶・55比較対の記録を照合。6名のnative／全員有効／Save非変更／tapフラグと6素材入力がsealに一致。3つの全体ログもsource・fingerprint・Unity終了0を確認した。

2APKは同じseal `78cd652e55904723a01dc9e33d380c570e031bf490cf01c354827020b375ff74`。Windowsで取得した物理ファイルSHAと各ビルドJSONが一致する。

| APK | SHA256 |
| --- | --- |
| baseline：固定ノア＋従来5名 | `de07a9197e998d61d457b6333db0524afcbfb26fb1d0973505c47d1b07e79d77` |
| authored：固定ノア＋2.5D候補5名 | `d0b7ddc6858bf731cff25ab593f0ec3c9ac62fb08e47574174f872150a0d46c9` |

`build.log` は2回のPlayerBuildInfo success:trueと最後の終了コード0を記録。一方、親処理のsource-info.jsonは未作成。報告18:58時点のプロセスCSVにUnity／java／dotnet／adbはなく、PowerShellも18:25以降のもののみで13:56の起動処理は残っていない。外側の完了処理が未完の理由までは確定しない。Start-Processの子プロセス待ちが原因と断定せず、待機実装の変更やプロセスkillは今回行わない。

**再検査・再ビルド不要。** 今回は製品／nativeテスト／素材／Noah master／街を変更しない。既存の合格結果から欠けた完了レコードだけを復旧する。APKバイナリ／最新画像本体は未受領であり、Windowsのhash報告とnative原XMLの確認に限定する。コマンド実行時にPC上で実APK・確認済みの8記録ファイルをSHA再照合し、5名の元の画像も既存validatorで照合してから記録を作る。Pixel／画質の正式採用は未判定。

## 一度だけの復旧・実機試験

Pixel3a接続・画面解除、Unityを閉じて以下を一括実行する。Git更新を必要としない、現在の既存helperを使う手順。source-infoが既にあれば上書きせず既存のRead-CharactersPairで検証する。欠けている場合はCreateNewで競合／上書きを防ぎ、事実に基づく合格フラグと復旧証拠ZIP SHAを保存。pixel_verified／adoptedはfalseのまま。JSONはliteral／元contextの単純な値だけで、32KB上限も設ける。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\prepare-characters-pixel-preview.ps1"
    $mofuRoot = (Get-Location).Path
    $mofuRun = Join-Path $mofuRoot "Builds\Diagnostics\characters-authored-20261031-135626-6f4151d8"
    $mofuRevision = "7af024864fc353a36ad886c034f1a2b22dda7648"
    $mofuFingerprint = "2f2c786b2672cade0f4279d7fb3e1bb6053de1adcf56c89c6fbcae4706a9236d"
    if (@(Get-Process -Name Unity -ErrorAction SilentlyContinue).Count) { throw "Close Unity before this recovery." }
    Assert-NoahAuthoredMasterFixed $mofuRoot
    Assert-NoahAuthoredFrozenInputs $mofuRoot
    if ((Get-CharacterAuthoredSourceFingerprint $mofuRoot) -cne $mofuFingerprint) { throw "Source changed. Preserve this run and report the error." }
    $mofuPins = @{
        "context.json" = "c6bc4a1188ba643bcf7c22570774c253f1b62889110ae43e217a9f0b4895d92f"
        "full-editmode.xml" = "61b6444166adedc585f4e2c896b9454dc446e07fb1dc58a6eb413b244c145e6a"
        "full-playmode.xml" = "e13c6c0ce18304f68f0ef176ad634237656fd6eb2aa295806d77e9fe0f8b7b53"
        "six-playmode.xml" = "09db4db4c8663a90bfc30e74592c9ee9da5f62b9df0adfa9798e6de5019eac3c"
        "prepared-six.json" = "3adeb7871bb5d1ed83dd007a164fb95274494872b0b3c6bca55d6bc4e3ba6ddc"
        "screenshots\six-authored\six-proof.json" = "94285f73288ad80019b7117f817532b720545cada7e4c6da912fd7b8bdd76c52"
        "characters-baseline-build.json" = "97165e9f6e4e2cbf5cd539948f9277a625a1b598d73da65c77fb203cf3560e38"
        "characters-authored-build.json" = "38278ba9042387725a8f0ff8ddc5be7fecaf67a64ec0019ac4d20490b0328ce7"
        "characters-baseline.apk" = "de07a9197e998d61d457b6333db0524afcbfb26fb1d0973505c47d1b07e79d77"
        "characters-authored.apk" = "d0b7ddc6858bf731cff25ab593f0ec3c9ac62fb08e47574174f872150a0d46c9"
    }
    foreach ($mofuName in $mofuPins.Keys) {
        if ((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $mofuRun $mofuName)).Hash -ine $mofuPins[$mofuName]) { throw "Verified file changed: $mofuName" }
    }
    $mofuContext = [IO.File]::ReadAllText((Join-Path $mofuRun "context.json")) | ConvertFrom-Json
    $mofuIds = @(Get-CharacterAuthoredIds)
    for ($mofuI = 0; $mofuI -lt 5; $mofuI++) {
        Assert-CharacterAuthoredProof $mofuContext.singleRuns[$mofuI] $mofuRevision $mofuFingerprint $mofuIds[$mofuI]
    }
    $mofuInfoPath = Join-Path $mofuRun "source-info.json"
    if (-not (Test-Path -LiteralPath $mofuInfoPath)) {
        $mofuRecord = [ordered]@{
            gitRevision=$mofuRevision; productFingerprint=$mofuFingerprint
            full_qa_verified=$true; single_native_verified=$true; six_native_verified=$true
            builds_generated=$true; source_unchanged=$true; pixel_verified=$false; adopted=$false
            singleRuns=@($mofuContext.singleRuns); error=""
            recovery="Completion record restored from independently verified native XML, build identities and APK hashes. Original outputs preserved."
            recovery_evidence_sha256="dd194a2913f013405f6b229d7d1069251f6e37924295b8525c61f0624d271c4c"
        }
        $mofuBytes = [Text.Encoding]::UTF8.GetBytes(($mofuRecord | ConvertTo-Json -Depth 4))
        if ($mofuBytes.Length -gt 32768) { throw "Unexpected completion record size." }
        $mofuStream = [IO.File]::Open($mofuInfoPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try { $mofuStream.Write($mofuBytes, 0, $mofuBytes.Length) } finally { $mofuStream.Dispose() }
    }
    Read-CharactersPair $mofuRun | Out-Null
    & ".\scripts\prepare-characters-pixel-preview.ps1" -BuildDirectory $mofuRun -Install -Mode authored -Serial "99RAY1BELH" -MeasureSeconds 600
}
```

最後は既存 `-BuildDirectory` 分岐なので全件・単体・BuildPlayerは呼ばない。Read-CharactersPairと通常install helperで改めてAPKのsource／package／SHAを確認し、install-rで保存を維持する。生成Resourcesや元XML／ログ／APKは削除・コミットしない。街が表示されたらPCでEnter、10分の試遊と手動回答。実機出力は元runのpixel-authored-*配下。途中エラー時は全体を再実行せず、その表示を共有する。

## 検証範囲

復旧コマンドのPowerShell AST構文を確認。レコード生成部分を同じASTから取り出し、実際のRead-CharactersPairと2つの異なる小さい合成ファイルでschema受理を確認した。errorあり・source_unchanged=false・ファイル内容改変の3条件は拒否。これはmetadata interfaceの確認であり、合成ファイルをAPKや実機成功とは呼ばない。確認済み8原本・2物理SHAのpinも受領証拠と照合。実Unityは受領済み原XMLの結果であり、新たにクラウド実行していない。

先の216MBの確認JSONはGet-ContentのWindows PowerShell ETS／PSDrive情報まで深くJSON化したことが原因だった。原本ZIPはruntimeへ保持し、小さい要約とSHAだけを今回の記録へ保存。以後の採取はrawファイルcopyとscalar CSVにして同じ問題を避けた。
