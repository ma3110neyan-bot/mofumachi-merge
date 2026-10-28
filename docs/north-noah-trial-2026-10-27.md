> 2026-10-28最新：ユーザーが北境界を合格・固定と明示。以下の北差戻しは過去のCodex評価で、追加修正しない。現在はノア1名の品質候補のみ。最新手順は [ノア品質受入](noah-quality-review-2026-10-28.md)。旧runの成功は新候補へ流用しない。

# 中央→北の接続／ノア1名2.5D試作 — 2026-10-27

最新原本審査：Unity全378件／画像原本は成功。ただし北接続の見た目に局所後退2件があり、統合候補のPixel採用は保留。ノア単体は重要な崩れなし・実機未確認。[10月28日の審査結果](north-noah-native-review-2026-10-28.md)。以下のコンソール記録／インストール例は履歴・将来の再合格後の手順で、今回runのインストール指示ではない。

10月28日のrun `20261028-101255-ecac0943` はUnity144＋234件、250画像、北／ノアメタデータ検証、街QA／成長QA APK生成まで正常終了したと画面で確認。Single誤判定は画面上で解消。原XML／build-info／画像／APK bytesは未受領で、実画像採用・Pixelは未確認。次はこのrunの `five-plaza-review-*.zip` 全部を提出する。今回の受領記録だけではPull／再ビルド不要。[画面成功記録](verification/north-noah-qa-build-screen-report-2026-10-28.json)。

以下は過去の失敗と修正の履歴。

最新run220456は前回の階層修正を通過し、ノア撮影の初期位置引継ぎで停止。撮影用シーンの修正記録は [こちら](north-noah-capture-scene-repair-2026-10-27.md)。修正版の実Unity全件／APK／Pixelは未確認。

前回run214011は実PlayMode233/234、比較撮影1件の階層探索エラー。撮影テストだけを修正した再確認手順は [撮影修正記録](north-noah-capture-repair-2026-10-27.md)。修正後の実Unity／APK／Pixelは未確認。

## 今回の範囲と状態

ユーザーが現在の噴水をリリース版として採用したため、水流・ショー・大きさ・前後描画の実装を固定する。噴水の追加Polishはリリース後の提案だけに残す。今回の変更対象は中央→北の地面接続と、街で表示するノア1名だけ。図鑑・原画・他5名・BGM・Save・依頼・納品・報酬・Merge・mainは変更しない。

設計・実装済み。内部NUnitはPresentation365/365＋Core125/125、failed0/skip0/inconclusive0。PowerShell17/17、Unity APIに対する6assemblyコンパイルは警告0・エラー0。採用版の固定502ファイルと24候補bindingを照合済み。これはライセンス付きUnityの描画実行ではない。新しい同一runのUnity成功／APK生成はコンソール報告済み。実XML／画像／APK同一性の原本照合とPixel 3a負荷は未確認。前版370件成功や既存Pixel画像を、新版の合格として転用しない。

## 確認すること

境界は中央の石畳・縁石を北へ段階的につなぐ。北側地面が中央を直線状に覆っていた箇所だけを対象にし、建物・花壇・小道・歩行判定は維持する。±0.30の横道を含む往復が成立し、植栽で歩ける場所を隠していないか、実画像で判定する。見た目が悪化した候補は採用しない。

ノアは既存の正式アトラスを使用し、顔・衣装・配色・体型を描き換えない。足元と影の位置、歩行・停止・まばたき・小さな会話反応を試す。他5名の試作展開は行わない。試作ON/OFFは隔離撮影用APIのみで、製品UIや保存項目を増やさない。

## Windowsで同一版を実Unity撮影／QAビルド

既存featureブランチを更新後、Unity Editorのプロジェクトを閉じて実行する。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
```

正常終了した**今回の**出力フォルダーの比較HTMLと `five-plaza-review-*.zip` 全部を確認する。失敗したら、そのrunの `playmode.xml` / `playmode.log` を確認する。前回フォルダーの画像やAPKへ取り違えない。

比較HTMLの各ペアから、実PNGを並べた `.jpg`（1440×1280、左Before／右After）を開ける。画像はUnity描画から作り、元PNGも保持する。

比較のBeforeは2種類を明記する。①前版bb3fb82で取得済みの北接続画像は歴史的な実Before。②新しい隔離シーンで接続レイヤー／ノア試作を同一時刻にOFF/ONするものは「現行実装を再現した比較用Before」。後者を旧実行の原本画像と呼ばない。静止画とは別に、固定刻みで進行したUnity描画列を残し、実時間FPSと混同しない。会話は単独の自動吹き出しではなく、ノアとモモの相互の挨拶・双方の反応・向きが揃った場面を記録する。

## Pixel 3aはセーブを維持して更新

今回は再アンインストール／初期化しない。正常終了runのフォルダーを指定し、build-infoとAPKのSHAを照合して更新する。

```powershell
$TrialRun = "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Android\今回の正常終了run"
$TrialInfo = Get-Content -LiteralPath (Join-Path $TrialRun "build-info.json") -Raw -Encoding UTF8 | ConvertFrom-Json
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\install-pixel3a.ps1" -Apk (Join-Path $TrialRun "town-life-qa.apk") -PackageName "com.mofumachi.merge.qa.townlife" -Serial "99RAY1BELH" -ExpectedSha256 $TrialInfo.sha256
```

中央→北→中央を往復し、中央道／左右の逃げ道、画面端、噴水前後の表示を確認する。主セーブ（GameState）と今日のお出かけ記録（Daily）は別保存。実際の歩行では既存Daily記録が正常に更新される。同一瞬間の描画切替ではどちらのゲーム処理も進めず、実歩行検査では主セーブを維持しつつ通常のDaily更新を抑止しない。

ノアを選択して静止／左右歩行／開始停止／会話を確認し、顔が変わって見えないこと、足が滑らないこと、影が接地することを確認する。図鑑・他5名・噴水の既存品質も比較する。

負荷比較は、同じ端末・明るさ・場所・条件で旧版のBeforeと新版のAfterを各10分測定する。旧版の計測がなければAfterの測定値だけを報告し、負荷増減を断定しない。街を前面表示して以下を実行する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\capture-pixel-visual-check.ps1" -Label After -Seconds 600 -Serial "99RAY1BELH"
```

バッテリー温度はCPU温度ではない。取得できない指標を0や合格に置き換えない。Pixel確認後もノアの承認前に6名へ展開しない。

## 提出範囲

中央→北Before／After、Unity実画面、往復歩行結果。ノアBefore／Afterの静止・歩行・会話、Pixel 3a負荷結果のみ。新規候補の採用は実画像／実機確認待ち。

Codex／AI側から見た次の改善候補：今回はなし。境界とノア1名の実画像・実機判定を優先する。
