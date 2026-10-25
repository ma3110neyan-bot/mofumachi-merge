# 街フィールド安定化：新APKの確認手順

今回は噴水の前後描画、東の建物角で近接NPCを避けられない経路、端部のカメラ見切れだけを修正。原画・配置・装飾・歩行面・保存・依頼／報酬・BGM・図鑑・Mergeは維持。旧084947APKには今回の修正は入っていない。

## Windowsで全件QA

GitHub Desktopで `feat/daily-world-pass-20261016` をPull後、Unity Editorを閉じて実行する。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
```

現在の宣言数はEditMode143／PlayMode223。失敗0・skip0、従来124＋安定化14＝138枚の同版実画面、QA APK／SHA256までが正常終了条件。件数は実機合格の代用にしない。エラー時は再試行せず、表示された同runのXML/logを調べる。

撮影には `31-field-fountain-front/behind`、`31-field-east-corner-out/return`、5地区の `31-field-地区名-edge/return` が含まれる。全て孤立した試験用セーブでのUnity実描画・実歩行で、Pixel画面ではない。`field-stability.json` とcapture-manifest/build-infoで同版を照合する。30-boundaryのBeforeは「今回のランタイムで曲線帯だけOFF」の境界限定比較へ変更している。旧APK全体の再現と扱わない。

## Pixel 3a

新しい同runの `town-life-qa.apk` とbuild-infoのSHA／街QA IDを照合し、既存 `install-pixel3a.ps1` を `-Apk`、`-PackageName "com.mofumachi.merge.qa.townlife"`、`-Serial "99RAY1BELH"` で実行する。更新install-rを使用し、アンインストール／保存消去／署名回避はしない。

- 中央：噴水の手前→左右→奥→手前。手前ではキャラが前、奥では噴水が前になること。NPCにも同じ前後関係があること。
- 東：右端／右上端へタップ・ドラッグで行き、内側へ両操作で戻る。花屋の右側から下の入口へ戻り、NPCが近い場合も角で進行不能にならないこと。
- 西／北／南と中央上端：端まで行って戻る。キャラの姿と操作対象が画面外へ消えないこと。
- 新発見通知が出た場合：北端のキャラを通知表示中もドラッグでき、通知自体をタップすると図鑑が開くこと。消えた後のカメラ／景観も確認。既存セーブを初期化して通知を作らず、出なかった場合は未確認と記録する。
- 5地区を連続往復し、キャラ同士の重大な詰まり、カメラ飛び、文字切れ、景観後退がないこと。
- 既存セーブで可能な依頼→Merge→納品→報酬→街成長→図鑑の範囲、終了・再起動保存復帰を確認。完了済み物語や盤面を初期化して試験しない。
- 10分以上の連続操作で重さ・熱・停止・クラッシュ・音を記録する。

Pixel確認が終わるまで正式ベース固定／噴水エフェクト／2.5D／Merge Quality Pass／main統合へ進まない。今回版のUnity実画面・APK・Pixel結果は、旧版の成功とは分けて記録する。
