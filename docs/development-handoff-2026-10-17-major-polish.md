# 大幅Polish Pass — 実装候補・実機未確認

対象は `feat/daily-world-pass-20261016`。下記を同じ候補へまとめた。**本版のUnity実描画・QA APK・Pixel受入は未完了。main未統合。**

- 街の左右の透明化を廃止し、正式背景が横幅を覆う倍率へ変更。北／中央／南の接続だけを狭い範囲でなじませる。
- 噴水・花壇を避ける歩行面と複数経路。住民のすれ違い・目的地予約・停滞時の迂回。通常歩行は連続座標と追従カメラ、上部3ボタンはファストトラベル。
- キャラを押したまま指を動かすと床の範囲で追従し、離すと短く減速して停止。停止後に施設の入口案内を再判定。
- Mergeの長方形下地をなくし、上端76%幅の台形・楕円の受け皿・前後のサイズ差を強調。ドラッグ中は元アイテムを隠し、影と持ち上がったアイテムを表示。
- 紙・淡いリボン・話者の名前札・文字色と行間を会話／図鑑／今日／依頼／お礼へ適用。数字と操作の可読性は維持。
- 図鑑の次の発見をスクロール外へ配置。会話と散歩で発見し、NEWと任意で開ける案内を表示。「今日」に進捗・あと1つ・受取を表示。

Main SaveData、5×6、合成・納品・報酬計算、正式6名の原画、ロゴ、背景原画、BGM3曲、SDKは変更なし。散歩・既読数は既存の補助日誌へ省略可能な項目として追加。旧日誌の読み込み・保存失敗時の巻き戻しを検証した。

クラウド検証：Core **125/125**、純粋な表示・移動・日誌 **81/81**。Unity実DLL参照の6アセンブリを警告0・エラー0でコンパイル。300経路は全到着し、全位置が床の内側。5,000線分の交差判定も照合した。**Unityネイティブ試験・PixelのFPS測定ではない。**

ネイティブ宣言構成は **EditMode142、PlayMode167**（DailyWorld10、MajorPolish6を含む）。実件数と合否はWindows XMLを正とする。MajorPolishは指追従・施設入口・図鑑NEW・実際に高さを変えた画面・30セルの中心と隅の先頭レイキャストを検査する。

一括実行は未コミットの製品入力を拒否し、各工程間の入力SHA256を照合する。PATHにGitがない場合はGitHub Desktop同梱Gitを探す。Unity生成のローカル設定差分は破棄せずbuild-infoに記録する。QA設定画面下端にも短縮SHAを表示する。

## Windowsで同じ候補を生成

Unity Editorを閉じ、対象ブランチをPullしてから次の1行を実行する。生成済みのローカルファイルをまとめてコミットする必要はない。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"; powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
```

同じフォルダの両XML、53PNG、texture-memory、capture-manifest、build-info、QA APK、SHA256を確認。失敗・skip・画像不足ならPixelへ進めない。PNGヘッダー検証だけでは見た目の合格にならない。

## 比較・実機の残り

Unityの比較元は受領済み **19253e2 / 20261017-160232-c0731ca6** の36枚。bccb869のPixel PDFは別の旧版であり、19253e2のBeforeに混ぜない。

|場面|比較・追加画像|
|---|---|
|中央公園・左右・文字|04-town-lv6-Central、15-wide-park-left/right|
|北／中央／南と継ぎ目|09-connected-path、18-continuous-walkの5地点|
|Merge|02-merge-all-items、03-merge-selection|
|歩行・すれ違い・指追従|06-walking、16-residents-passing、17-finger-follow/released|
|図鑑・NEW|10-book各6名、19-book-new-in-town／discovery-first-page|
|今日・次目標・成長|11〜14、04-town-lv1/3/6、07-quest-story-goal|

追加場面の旧版画像がない場合、同条件比較が成立したとは報告しない。Pixel更新前に現在版のセーブ・画面を記録し、`install -r`で更新後、同じ状態を撮る。アンインストール・データ削除はしない。

PixelではCoins／街Lv／お気に入り／現在依頼／盤面／BGMの保持を最初に確認。保存異常なら停止する。通常歩行・指追従で北↔中央↔南、施設入口、すれ違い、Merge、図鑑・今日、3曲、再起動を確認し、10分以上の試遊で負荷・発熱・音・入力を評価。**実画面Before／AfterとPixel評価が揃うまで、合格・反映済み・main統合可とは報告しない。**
