# モモ比較撮影：停止時の名札差分修正

受領した2ZIPは `momo-authored-20261031-104717-828afa35`、source `0efef45aacddd055f67cbe0b7ee4ade7d08b950e`。原XMLはモモ専用1件の失敗、成功0・skip0。停止画像 `stop-0241` の `outside_allowed_pixels` が期待0に対し5362で停止し、全件テスト／APK／Pixelへ進んでいない。

自然Beforeでは右下のノア名札が表示され、自然Afterでは既存の名札回避により非表示となっていた。追加キャラ撮影には、既存ノア撮影で使う名札の比較条件統一が抜けていた。モモの身体範囲外にある名札の差が、身体以外の不正な変更として検出された。受領原本は変更せず `References/Review/momo-authored-2026-10-31-104717/` に保存した。全32画像は720×1280で正常にデコードできた。

## 修正範囲

追加5名の撮影helperを既存の固定ノア検査と同じ処理に揃えた。自然な候補Afterの名札状態を取得し、比較Beforeだけその状態へ揃える。自然Beforeが異なる場合は、元画像を `*-ui-natural-before.png` として別保存する。全6名の自然Before／比較Before／自然Afterの状態、Beforeで名札以外を変更していないこと、Afterが自然な状態であることを検証・記録する。

身体の実際のBefore／After投影範囲の和集合を用い、範囲外差分は引き続き0を要求する。時刻、他5名、カメラ、水、世界状態、Saveの不変検査も維持する。失敗時は画像に加え、差分座標とGraphic状態のJSONを保存する。ライブの名札回避ルール、ノア本体、キャラ素材、街、北境界、噴水、Coreは変更していない。

PowerShell側も比較条件と自然Before画像のSHA／bytes／寸法を検証する。自然Beforeを必要としない比較では、Unityが空の画像オブジェクトを出力しても正常に扱う。

## 確認状態

- 受領した実Unity：失敗1件。元の結果は変更しない。
- 修正後：関連するPowerShell単体／連携ガードとPlayMode APIコンパイル成功。名札制御の不正証拠を旧validatorが受理するREDと、新validatorが拒否するGREENを確認した。空画像オブジェクトの正常ケースもRED→GREEN。合成データの検査であり、Unity描画の成功ではない。
- 固定ノアと552保護入力：ハッシュ不変を確認済み。
- 修正後のUnity実画面、APK、Pixel負荷：未確認。全件スイートは再実行していない。

## Windowsでモモだけ再確認

修正が反映されたfeatureを取得してから、下記をまとめて実行する。子PowerShell全体にRemoteSignedを指定する。ブランチ不一致やpull失敗時は停止し、ローカル変更をresetしない。

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
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止しました。" }
    & ".\scripts\capture-character-authored.ps1" -Character momo
}
```

モモの新しいPlayMode1メソッドだけを実行し、画像／XML／ログのZIPを出力する。全件EditMode／PlayMode、APK、Pixel更新、アンインストール、Save初期化は行わない。出力された今回のZIP全パートを確認し、通過後に残りの受入工程へ戻る。
