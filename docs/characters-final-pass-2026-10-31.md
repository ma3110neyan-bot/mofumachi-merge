# キャラ2.5D 最終確認候補（2026-10-31）

実装済み・実機未確認。正式固定は、新版の最終Unity／APK／Pixel結果を確認した後に行う。

製品変更はモモ・ミエルの接地影だけ。足元の中心を約2 UI単位下げ、楕円を少し絞り、濃度.38→.43。97頂点の既存影を使い、追加の毎フレーム処理・素材・原画・動作・歩行速度・サイズ・Sorting・ノア方式・街・保存は変更しない。旧影へ戻す経路も保持する。添付PDF20画面はReferences/Review/characters-final-feedback-2026-10-31/に原本保存。

## Windowsでの最終実行

Unity Editorを閉じ、Pixel 3aのロックを解除し、USBデバッグを許可する。下記全体をPowerShellへ貼る。途中で失敗したらそのrunの記録を保持し、全件を繰り返さない。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuFinalGit = Find-SourceGit
    $mofuFinalBranch = & $mofuFinalGit branch --show-current
    if ($LASTEXITCODE -ne 0 -or $mofuFinalBranch.Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチを確認してください。"
    }
    & $mofuFinalGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止します。" }
    & ".\scripts\prepare-characters-final-pass.ps1" -Install -Serial "99RAY1BELH" -MeasureSeconds 600
}
```

この1回で、所有者・完了APK・素材ハッシュが一致する旧生成素材だけをBuilds/Diagnostics/characters-preserved-*へ移動保存する。追跡済み・未知・改変・所有者不一致・リンクは動かさず停止。削除／git clean／Save初期化はしない。途中移動エラーもpreserved-inputs.jsonに移動済みパスとSHAを残す。復元はUnity終了後、退避先と元パスの両方を確認し、元が空いている項目だけ戻す。上書きしない。

次に5名単体を順番に確認→全件EditMode／PlayMode各1回→6名の正面・左右・背面／足元・サイズfixture→同ソース比較2APK。モモ・ミエルは旧2.5D影と今回の影の5比較対を同一時刻・影の範囲内だけで撮る。従来の2D対2.5D比較とは区別する。実Unityで失敗した場合、該当する検査へ戻る。

## Pixel操作

比較用baseline APKを保存維持で更新し、街が見えたらPCでEnterを押して10分試遊する。続いてauthored APKで同じ10分試遊。合計20分で、比較用baselineの追加5名は原2D表示、ノアは承認方式。最後に今回の6名2.5Dが残る。通常の保存維持更新であり、アンインストールはしない。

両方で同じ経路・同程度の操作を行う。モモ・ミエルの横歩きと停止、全6名の前後左右・挨拶・会話、木／ベンチ／噴水／建物／花壇の前後を確認する。タップ反応・カクつき・発熱の質問へ回答する。FPSは実Unity SurfaceView、メモリ・熱・クラッシュ記録を採取する。

最後の追加質問は実機で見た内容を記録する。6名同時表示は、実際のPixelの街画面に6名が同時に見えた場合だけyes。個別に見たことやUnity fixtureで代用しない。確認できなければunknownで進み、正式固定を保留する。保存確認は同じアプリを終了・再起動し、進行が残ることを見る。

## 提出と判定

終了時に開くエクスプローラーで、今回の`characters-authored-日時-ID-final-review-review-*.zip`をすべて添付する。最終全件XML・6名4方向・モモ／ミエルの影比較・baseline／authoredの実機記録が入る。他単体の画像全体は元run／元ZIPに保持し、最終ZIPの単体proofは画像全体の同梱を意味しない。失敗時は表示された元runのreview ZIPとエラー画面を使う。

クラウドで確認済み：接地・既存動作の純粋検査14/14、通常／Android／PlayMode／Editor APIコンパイル、証拠ゲートと退避の合成検査。新Unity実描画・全件・APK・Pixel成功へ置き換えない。

実画像・負荷・保存・6名同時・全件が揃うまで`formal_fixed/pixel_verified/adopted`はfalse。今回の候補を旧7af0248の全件成功やPixel画像で固定しない。新しい会話仕様は別工程のまま。
