# 街フィールド安定化：新APKの確認手順

今回は噴水の前後描画、東の建物角で近接NPCを避けられない経路、端部のカメラ見切れだけを修正。原画・配置・装飾・歩行面・保存・依頼／報酬・BGM・図鑑・Mergeは維持。旧084947APKには今回の修正は入っていない。

## 今回確認できた範囲

run `20261025-153810-fa9b2be9` の11ZIPを受領し、原XMLのEdit143/143／Play223/223、failed0・skip0、138PNGのSHA・720×1280・デコード、build／captureの同じ871808c由来を確認した。5地区Lv1/3/6の15全景は前版とbyte同一。原本と判定は `References/Review/field-stability-2026-10-25-candidate/native-review.json`。

**同じAPKで条件付きの実機検証へ進行可。街ベース合格は保留。** 東端は木に顔・体・名前がほぼ隠れる画面がある。保持した木の自然な前後関係と整合し、新しい停止／描画退行と確定していないが、見つけて操作できるか未解決。北端・西端には外周地面も見える。噴水の奥側静止画はキャラと噴水が重なっておらず、歩行中の実際の前後切替は実機で確認する。

今回の受領記録ではゲーム・テスト・QAツールは変更していない。**Pull／再ビルドは不要。** 下の既存APKを使用する。APKバイナリはクラウド未受領なので、端末へ入れる直前にローカルSHAを照合する。

## 全件QAの履歴

以下は今回成功した全件QAのコマンド。今回の記録更新だけを理由に再実行しない。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
```

現在の宣言数はEditMode143／PlayMode223。失敗0・skip0、従来124＋安定化14＝138枚の同版実画面、QA APK／SHA256までが正常終了条件。件数は実機合格の代用にしない。エラー時は再試行せず、表示された同runのXML/logを調べる。

撮影には `31-field-fountain-front/behind`、`31-field-east-corner-out/return`、5地区の `31-field-地区名-edge/return` が含まれる。全て孤立した試験用セーブでのUnity実描画・実歩行で、Pixel画面ではない。`field-stability.json` とcapture-manifest/build-infoで同版を照合する。30-boundaryのBeforeは「今回のランタイムで曲線帯だけOFF」の境界限定比較へ変更している。旧APK全体の再現と扱わない。

## Pixel 3a

USB接続・USBデバッグ許可済みのPixelで、以下をそのまま実行する。更新install-rを使用し、保存を消さない。インストーラの成功表示だけでは実際の進行復帰成功と扱わず、ゲーム内で確認する。

```powershell
$ErrorActionPreference = "Stop"
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
$MofuApk = ".\Builds\Android\20261025-153810-fa9b2be9\town-life-qa.apk"
if ((Get-FileHash -LiteralPath $MofuApk -Algorithm SHA256).Hash -ne "6f1d163c6b8a14cf7025399e350e7f88e526f53856dd382f20d08b498beed124") {
    throw "APKのSHA256が一致しません。"
}
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\install-pixel3a.ps1" -Apk $MofuApk -PackageName "com.mofumachi.merge.qa.townlife" -Serial "99RAY1BELH"
```

対象118,242,007bytes／街QA ID `com.mofumachi.merge.qa.townlife`。インストールでエラーになったら既存アプリを削除せず、その出力を確認する。

- 中央：噴水の手前→左右→奥→手前。手前ではキャラが前、奥では噴水が前になること。NPCにも同じ前後関係があること。
- 東：右端／右上端へタップ・ドラッグで行き、内側へ両操作で戻る。花屋の右側から下の入口へ戻り、NPCが近い場合も角で進行不能にならないこと。木の陰でもキャラを見つけて戻せるか重点確認。Unityの最前面タッチ検査は、木に隠れた画素の見やすさまでは保証しない。隠れて位置不明／操作困難なら不合格のまま記録する。
- 西／北／南と中央上端：端まで行って戻る。キャラの姿と操作対象が画面外へ消えないこと。北上端・西左端で見える外周地面の目立ち方も記録する。
- 新発見通知が出た場合：北端のキャラを通知表示中もドラッグでき、通知自体をタップすると図鑑が開くこと。消えた後のカメラ／景観も確認。既存セーブを初期化して通知を作らず、出なかった場合は未確認と記録する。
- 5地区を連続往復し、キャラ同士の重大な詰まり、カメラ飛び、文字切れ、景観後退がないこと。
- 既存セーブで可能な依頼→Merge→納品→報酬→街成長→図鑑の範囲、終了・再起動保存復帰を確認。完了済み物語や盤面を初期化して試験しない。
- 10分以上の連続操作で重さ・熱・停止・クラッシュ・音を記録する。

Pixel確認が終わるまで正式ベース固定／噴水エフェクト／2.5D／Merge Quality Pass／main統合へ進まない。今回版のUnity実画面・APK・Pixel結果は、旧版の成功とは分けて記録する。

報告は「インストール／噴水前後／東の木の陰と端部帰還／他4端部と5地区連続移動／保存復帰／10分の重さ・熱・音・停止」をテキストでよい。確認していない項目は未確認とする。現在セーブで再実行できない依頼などのために進行を初期化しない。
