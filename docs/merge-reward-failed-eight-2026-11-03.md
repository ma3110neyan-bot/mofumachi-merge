# Merge全件結果から失敗8件だけを確認

実Unity受領結果（run `merge-reward-20261103-112005-85e3d93b`、source `c2297d0`）は重点53/53、EditMode144/144、PlayMode252/260、失敗8・skip0。前回修正した演出テストも合格している。原本3XML・親JSONと受領15ファイルのSHAは`References/Review/merge-reward-full-failed-2026-11-03-112005/`に保存。元の失敗結果を書き換えない。

今回修正したのは製品を変更しないテスト条件だけ。

- DailyWorld：旧「正方形」条件を、現行5×6長方形・48以上・正確な位置／寸法・30個の非重複判定へ訂正。
- MajorPolish：無効化済みの旧フレームの空メッシュではなく、実表示中の`Reward board paper`を検査。4画面高・30セル・中心／四隅の実Raycast検査は維持。
- キャラ準備6件：明示的なMerge準備QAでは実入力SHA・素材の存在・正式ID／設定の読込みを確認。それ以外では従来の素材混入拒否を維持。プレビュー専用の長時間撮影は起動しない。キャラ本体・原画・設定値は変更していない。

**現在は`-Final`を繰り返さない。以下は失敗8件だけを実行する。** 既存素材準備2工程は必要だが、全件テスト・JPEG撮影・APK生成・インストールは実行しない。元の重点53件／Edit144件はXMLを読むだけで再実行しない。

Unityを閉じ、Windows PowerShellへまとめて貼り付ける。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mergeGit = Find-SourceGit
    if ((& $mergeGit branch --show-current).Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチが異なります。"
    }
    & $mergeGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗しました。" }
    & ".\scripts\diagnose-merge-reward-failed-eight.ps1" -OriginalDirectory "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Diagnostics\merge-reward-20261103-112005-85e3d93b"
}
```

終了すると新しい`merge-reward-repair8-日時-ID-review-01.zip`を選択したExplorerを開く。ZIPには今回の8件のXML・ログ・ソース／素材SHA、元の3XML／親JSONを残す。原本のSHA照合と、元の全件確認時点から製品入力に差分がないことを確認してから実行する。別のrunの結果で代用しない。

今回の修正はAPIコンパイル／関連PowerShell契約・実ソースガードで確認済み。**修正後の実Unity8件・APK・Pixelは未確認。** 8件の新結果を受けてリリース候補確認の残りへ進む。元のPlayMode全件XMLは252/260のまま保持し、重点結果を全件再実行の成功に見せない。

11:34:31の追加画像は別run `merge-reward-20261103-113431-4927c40c`の終了2だけを示す。ログ／XMLが未受領のため、その失敗内訳を同一と断定しない。今回の診断には内訳確認済みの11:20:05の原本を使う。
