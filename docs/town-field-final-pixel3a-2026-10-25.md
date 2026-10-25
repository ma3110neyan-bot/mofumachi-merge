# 街フィールド最終確認

今回の修正版は実装済み。新しい実Unity全件結果・新APK・Pixel合格は未確認。旧153810の143/223・138画像・APK成功は今回へ流用しない。画像はチャットへ貼らない。

## 一度の全件QA

GitHub Desktopで`feat/daily-world-pass-20261016`をPullし、Unity Editorを閉じて実行する。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
```

現在の宣言数はEdit144／Play224。全件Passed・failed0/skip0、144枚の720×1280画像、実納品成長の`town-growth-progression.json`、同じソースの2APKと各SHA256までが成功条件。数だけで画面／実機合格にしない。エラー時は同じrunの原XML/logを診断し、推測で再実行しない。

出力フォルダ：

- `town-life-qa.apk`／`build-info.json`：従来の街QA。既存Lv6保存を更新インストールで維持する。
- `town-growth-check.apk`／`growth-build-info.json`：アプリ名「もふまちメルジュ 成長確認」、ID`com.mofumachi.merge.qa.growthcheck`。通常の初期ゲームから実進行を検証する別保存。レベル・盤面・報酬の注入機能はない。
- `five-plaza-review-*.zip`：全件原XML、撮影原本、成長／再読込の検証、両APKの識別情報。APK本体はZIPへ入れない。

## Pixelへ入れる前

同runの原本と実画面をレビューし、それぞれのAPKを対応するbuild-infoのSHA256・appId・sourceへ照合する。新APKのSHAを旧版から転記しない。街QAは`com.mofumachi.merge.qa.townlife`、成長確認は`com.mofumachi.merge.qa.growthcheck`を既存`install-pixel3a.ps1`の`-PackageName`へ明示し、`-Serial "99RAY1BELH"`で更新install-r。両アプリとも保存消去・アンインストール・署名回避はしない。実機接続はユーザーのWindows側が必要。

## 街QA：既存保存の維持とフィールド

- 更新前後でLv6／Coins／依頼・図鑑・盤面が保持されていること。
- 中央↔西／東／北／南を連続往復。全端部、建物・ベンチ・花壇の脇からタップとドラッグで自力帰還する。リセット／ファストトラベル救済で合格にしない。
- 東の木に隠れてもキャラを見つけて操作できるか。描画順として自然でも、通常操作が困難なら不合格。
- 噴水手前→左右→奥→手前を歩く。手前はキャラ、奥は噴水が前。NPCも自然な前後関係で、常時キャラ最前面でないこと。
- 北上端・東西外側端・その他接続で、直線切断／緑の未完成面／貼り合わせ感が残らず、既存の密度・歩行範囲が維持されていること。
- 10分以上の連続操作、重さ・発熱・音・停止・クラッシュ、終了と再起動後の保存復帰を記録する。

## 成長確認アプリ：実際の納品と再起動

従来の街QAセーブを初期化して試験しない。別アプリの通常の生成・Merge・受注・納品を進める。

| 順 | 納品 | 街Lv | 累計Coins |
|---|---|---|---|
| 1 | お茶Lv2×1 | 0→1 | 30 |
| 2 | パンLv2×1 | 1→2 | 70 |
| 3 | 花Lv2×1 | 2→3 | 110 |
| 4 | お茶Lv3×1＋パンLv2×1 | 3→4 | 180 |
| 5 | 花Lv3×1＋お茶Lv2×1 | 4→5 | 250 |
| 6 | パンLv3×1＋花Lv2×1 | 5→6 | 330 |

1→2、2→3、5→6では納品後の成長画面で5地区を切り替え、Before／After・短い強調・変更内容を確認する。街へ戻って中央・西・東・北・南の実際の装飾を見てから終了・再起動し、Lv・Coins・装飾・完了依頼が維持されることを確認する。再読込だけで報酬が増えないことも確認する。

北・南の既存の中間装飾はLv3、花アーチはLv6。Lv2で北南まで一律に新装飾を足して成長間隔を変えず、比較画面では「次はLv.3」と示す。Lv3／6は実装済みの追加・置換が現れることを確認する。

## 判定

Pixelの表示／操作・実納品成長／保存復帰・重大クラッシュなしを確認するまで正式ベースは凍結しない。噴水エフェクト→代表1名2.5D→Mergeは合格後の順序で、mainは別承認。

報告はユーザー指定の8項目（移動／引っ掛かり／噴水前後／境界／成長／保存復帰／Pixel／正式固定可否）のみ。未確認を合格と置き換えない。
