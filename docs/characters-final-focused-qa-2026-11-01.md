# 最終PlayModeの5件を限定確認

最新追記：123101-bda3c51fの原XMLは限定7/7成功、failed／skip／inconclusive0、終了0。厳密7メソッド集合とCLI・source-infoのsource7931c06／FPa8b5349eを照合した。修正後の実Unity確認は通過。次は [現在版の最終確認](characters-final-followup-2026-11-01.md)。以下は113430失敗時の診断・限定実行の履歴。

run `characters-authored-20261101-113430-8e907640` のreview-01を確認。source4c1384d／fingerprint0eed7901は全件ログとノアログで一致。原XMLはEditMode144/144成功、PlayMode250件中245成功・5失敗、ノア単体1/1成功。いずれもskip／inconclusive0。APK／Pixelへ進む前に停止した。

今回part01には全件XML／logとNoahの準備記録等8点、画像17枚の一部がある。CRC、全25entry SHA、XML件数と終了コードを照合。原本は `References/Review/characters-full-failed-2026-11-01-113430/`。親source-infoと5単体の原本は今回partにないので、それらを独立確認済みとはしない。失敗5件の調査に必要な原XMLは揃っている。

|失敗した検査|原因と更新|
|---|---|
|TownHabitTests.NamesBubblesAndFacilitiesStayInsideSafeArea|画面サイズ変更は会話を消すが0.4秒の連打制御は維持される。3画面を即時に連続タップする旧fixtureを、各画面でシミュレーションを0.5秒進めてから明示タップする形へ更新。待機中のモデル位置を固定し、会話表示・文字サイズ・施設ボタン寸法の検査は保持|
|TownLifeTests.NpcsVisitSeveralStreetStopsWithoutChangingProgress|到着時の自動挨拶を期待していた。到着時は無言、明示タップ後は本人の名前で会話することを検査。経路・到着位置・お出かけ・保存の検査は保持|
|TownMotionPresentationTests.StationaryDragAndLifecycleStopGait|ドラッグで名前枠が非表示になり、通常の子Text検索がnullになる。非表示を含む参照を保持し、位置が安定し、実際に非表示になることを検査。歩行停止・入力所有権・ライフサイクルの検査は保持|
|TownRegionLifeTests.TwoResidentsMeetAndResumeInVisibleRegion|すれ違い自動会話を待っていた。2名を同じfixtureで画面内へ収め、近接だけでは無言、実タップで相手への挨拶・2名の一時停止・再開になることを検査。タップ前のPauseが0であることも必須|
|VisualLockPresentationTests.CloseResidentLabelsNeverCoverAnotherCharactersBody|NPC名の常時表示を期待していた。静止して邪魔しない本人名は表示、NPC名とドラッグ中の名札は非表示、表示名札と他キャラbodyは交差しないことを検査|

修正対象はこの5テストと限定実行スクリプト・保護検査・記録のみ。ゲームPresentation／Core／原画／街／承認元manifest2点は4c1384dから変更なし。既存5点のコード例外に旧テスト4点だけを正確な元SHA→新SHAで追加し、今回の合計は9点。原画や任意のゲームコードへの例外追加は拒否する。元のVisual Lockファイル自体は書き換えない。

修正版のPlayMode APIコンパイルは0警告／0エラー。限定filterの7件一致と欠落・余分・重複・別名・失敗・skip拒否、今回250件XMLの拒否、限定テストSHA許可と原画／任意ゲームコード拒否、Noah26／旧552の照合が通過した。これらは合成XML／API検査であり、新しい実Unity7件の成功ではない。

## 次は7件だけ

失敗5件に、通過済みのタップ専用会話・実ポインター連打の回帰2件を加える。全件、5単体撮影、APK、Pixelは再実行しない。Unityを閉じ、以下をPowerShellへまとめて貼り付ける。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuFocusGit = Find-SourceGit
    $mofuFocusBranch = & $mofuFocusGit branch --show-current
    if ($LASTEXITCODE -ne 0 -or $mofuFocusBranch.Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチを確認してください。"
    }
    & $mofuFocusGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止します。" }
    & ".\scripts\diagnose-character-finish-qa.ps1"
}
```

最後に新しい `characters-finish-focus-…-review-01.zip` を選択したExplorerが開く。その新ZIPを添付する。実Unity7件が通過した後に、同じ新版の最終全件・6名・APK・Pixelへ戻る。今回の旧全件を、新版の全件成功として再利用しない。クラウド純粋C#長時間経路の保留も維持。formal_fixed／adoptedはfalse。
