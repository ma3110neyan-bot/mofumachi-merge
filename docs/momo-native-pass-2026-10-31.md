# モモ単体：実Unity検査通過／次は6名QA

run `momo-authored-20261031-112329-63c14f20` の3ZIPを受領した。原XMLはモモ1件成功・失敗0／skip0／inconclusive0。ソースは `9ca07905da3b16835a9495fcd3176c74010fa2d7`。前回の名札差分と撮影後の正面mesh消失を越え、正面meshの完全vertex一致、4方向の実歩行完了、画面raycast／遮断／タップ、会話接近・挨拶・発話、5か所の深度順、Save非変更まで通過した。

原本77点／画像70枚を保存し、全画像のSHA・bytes・720×1280・デコードを確認した。11組のBefore／Afterの差分を再計算し、対象の投影範囲外は全組0。自然Beforeを別保存した2組も含め、全6名の名札状態と自然Afterを既存validatorで検証した。素材入力digestは、登録JSONのWindows CRLFと3方向のPNG SHAから再現した。原本・判定は `References/Review/momo-authored-2026-10-31-112329/`。

主担当による画像確認では、静止、左右斜め／後ろ／正式正面、歩行・停止、まばたき、挨拶・会話に新しい破綻は見当たらない。**モモは次のQA工程へ進行可。正式採用やPixel合格ではない。** 40標本は固定シミュレーション刻みで、実時間動画／FPSではない。5か所のSortingも静的配置で、全ての木・噴水を回り込む経路の証明ではない。会話パネルが帽子へ重なる既存状態、接地影の見え方、方向切替の滑らかさはPixelで確認する。今回ゲーム／撮影コードの追加修正・全件再実行は不要。

次は既存の `prepare-characters-pixel-preview.ps1 -Install -Mode authored -Serial 99RAY1BELH -MeasureSeconds 600`。同一runの5名単体ゲートを順番に実行し、全員通過後に全EditMode／PlayModeを各1回、6名同時、比較2APK、Pixel更新・測定へ進む。今回通ったモモも、統合runと同じソースとして単体検査される。失敗時は停止し、該当runだけを調査する。個別プレビュー承認待ちは復活させない。

以下をまとめてWindows PowerShellへ貼り付ける。検査・測定が終了または停止した後、Diagnosticsのエクスプローラーを開く。Pixelで街が表示されたら、PowerShellの案内に従ってEnterを押す。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuGit = Find-SourceGit
    if ((& $mofuGit branch --show-current).Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチが異なるため停止しました。"
    }
    & $mofuGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗しました。" }
    try {
        & ".\scripts\prepare-characters-pixel-preview.ps1" -Install -Mode authored -Serial "99RAY1BELH" -MeasureSeconds 600
    }
    finally {
        Invoke-Item -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Diagnostics"
    }
}
```

インストールは保存維持のinstall-r。アンインストール・Save初期化・main統合は行わない。ノア基準、街・北境界・噴水・Core等は固定。Pixelの負荷増加なしと判断するには、同じrunのbaseline側と同条件の比較も必要で、未測定値を合格としない。既存完成runを再測定する場合の手順は `docs/characters-authored-rollout-2026-10-30.md`。
