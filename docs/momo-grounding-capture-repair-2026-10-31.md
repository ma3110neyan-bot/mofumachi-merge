# モモ接地影の比較撮影だけ修正（2026-10-31）

最新4ZIP（momo-authored-20261031-212944-eaacbdb6）をCRC/SHA照合し、91原本／85画像を確認。実Unityは1件中0成功・1失敗・skip0。source f30cab4、fingerprint c0be19eaで親記録と一致。原XML・ログ・素材準備記録・source-infoはReferences/Review/momo-grounding-failed-2026-10-31-212944/に保存、全画像のSHAと4ZIPのSHAも記録した。古い時刻のMomo ZIPを今回版の成功として扱わない。

失敗は最後のgrounding_pairsの4方向集合照合。旧撮影は各経路13コマ目に固定され、北へ進む経路の途中の横向きを撮っていた。実際の歩行で4方向が観測される検査は先に通過したが、影比較5対の方向はLeft/Right/Frontだけだった。ゲームの背面表示を変更して解決しない。

修正は検査側のみ。各経路の狙い方向を開始位置と目的地から計算し、12フレーム以上経過後、実際にその向きで歩行している最初の時点を1回撮る。向きを強制せず、5対・4方向・影範囲外0・同一瞬間・キャラmesh/UV/色の不変条件を維持する。

純粋な実モデル経路検査はMomo/Mielとも4方向を収集。関連17/17成功。旧frame13方式へ戻すと14成功／3失敗を再現し、戻した修正版では17成功。PlayMode APIコンパイル0警告／0エラー、純粋テストprojectの登録もビルド確認。全件テストは実行していない。Noah26／既存552固定入力検査成功。Presentation／Core／原画／街はf30cab4から変更なし。修正版の実Unity／新APK／Pixelは未確認、正式固定は保留。

停止時の親ZIPは、以前はsource-infoしか入らなかった。現在は正確な新規停止子runを特定し、同じsource/fingerprintのXML・ログ・準備／source記録をコピーして既存親writerへ渡す。元ファイルは保持。リンク・範囲外・別source・出力上書きを拒否。合成データで拒否と親ZIPの中身を検査した。

## 次はモモ1件だけ

Unityを閉じて、以下をPowerShellへまとめて貼る。全件QA／APK／Pixel／残り4名の再実行には進まない。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuCaptureGit = Find-SourceGit
    $mofuCaptureBranch = & $mofuCaptureGit branch --show-current
    if ($LASTEXITCODE -ne 0 -or $mofuCaptureBranch.Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチを確認してください。"
    }
    & $mofuCaptureGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止します。" }
    & ".\scripts\capture-character-authored.ps1" -Character momo -OpenReview
}
```

終了時に今回作成した最初のモモZIPを選択したエクスプローラーを開く。画面に表示された同じ新日時・IDのモモZIPを全パート添付する。古い104717／111028等や今回の失敗済み212944を再添付しない。新モモの成功後に、ミエル等の同じ修正を含む最終確認へ進む。
