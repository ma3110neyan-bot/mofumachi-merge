# 街フィールド最終確認

run `20261025-210356-e7e1ca30` の11ZIP・160原本を確認済み。原XMLはEdit144/144／Play224/224、failed0／skip0／inconclusive0。144PNGは全SHA・bytes・720×1280・デコード正常。両APKのbuild-infoとcapture-manifestは同じ43a2764／fingerprint21a04dc3…に一致し、各ビルドlogの成功・bytesも一致する。現在の製品／テスト入力はこのrevisionと同じ。APK本体はクラウド未受領なので、バイナリSHAの独立再計算は端末更新前に行う。Pixel合格と正式固定は未了。画像はチャットへ貼らない。

5地区Lv1／3／6の15全景と既存22境界比較画像は前回153810原本とbyte同一。北上端・東西外側端の植栽は新しい31-field画像で確認する。実納品の1→2／2→3／5→6は5地区の実装飾・再読込までUnityで成功。噴水の奥の静止画は名前札だけが噴水と重なり、キャラ本体の遮蔽は証明していない。東の木陰で顔・体・名前が隠れる既存課題とともに、実際のPixel歩行／操作を合格条件に残す。

## 今回生成済みの2APKで更新する

今回の原本確認・記録だけではPull／再ビルド不要。USB接続してPixelを解除し、USBデバッグを許可してから次のブロックを実行する。2本ともSHA照合後、既存installerの明示ID／端末指定・install-rで更新する。別保存の成長確認を先に、既存保存の街QAを最後に開く。保存消去／アンインストール／署名回避はしない。

```powershell
& {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    $MofuRun = ".\Builds\Android\20261025-210356-e7e1ca30"
    $MofuApps = @(
        @{ File="town-growth-check.apk"; Package="com.mofumachi.merge.qa.growthcheck"; Hash="2b5302be61a855557535d11bed1622b4a6a01880f6aa2f169e1242e385fcf01d" },
        @{ File="town-life-qa.apk"; Package="com.mofumachi.merge.qa.townlife"; Hash="0d3426ddfc046c0d8d0709ee49ec2cbeeef3de71ef3f3e242580dc055fd44dbd" }
    )
    foreach ($MofuApp in $MofuApps) {
        $MofuApk = Join-Path $MofuRun $MofuApp.File
        if ((Get-FileHash -LiteralPath $MofuApk -Algorithm SHA256).Hash -ne $MofuApp.Hash) { throw "APKのSHA256が一致しません。" }
        powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\install-pixel3a.ps1" -Apk $MofuApk -PackageName ($MofuApp.Package) -Serial "99RAY1BELH"
        if ($LASTEXITCODE -ne 0) { throw "更新を停止しました。この出力を確認してください。" }
    }
}
```

街QAは118,247,418bytes、成長確認は79,006,293bytes。インストーラの成功表示だけで保存復帰まで合格と扱わず、以下を実際のアプリで確認する。

## 全件QAの実行履歴（今回は再実行しない）

以下の全件QAは今回の160原本で成功を確認した履歴。再実行は不要。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
```

実結果はEdit144／Play224の全ケースPassed・failed0/skip0、144枚の720×1280画像、実納品成長の`town-growth-progression.json`、同じソースの2APKと各SHA256を確認済み。数だけで実機合格にしない。

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

1→2、2→3、5→6では納品後の成長画面で5地区を切り替え、Before／After・短い強調・変更内容を確認する。北の花飾り／南のアーチなど、中心となる新装飾の変化と短い成長表示が実際に伝わるかも見る。静止画やIsHighlightingの内部値だけで演出合格にしない。街へ戻って中央・西・東・北・南の実際の装飾を見てから終了・再起動し、Lv・Coins・装飾・完了依頼が維持されることを確認する。再読込だけで報酬が増えないことも確認する。

北・南の既存の中間装飾はLv3、花アーチはLv6。Lv2で北南まで一律に新装飾を足して成長間隔を変えず、比較画面では「次はLv.3」と示す。Lv3／6は実装済みの追加・置換が現れることを確認する。

## 判定

Pixelの表示／操作・実納品成長／保存復帰・重大クラッシュなしを確認するまで正式ベースは凍結しない。噴水エフェクト→代表1名2.5D→Mergeは合格後の順序で、mainは別承認。

報告はユーザー指定の8項目（移動／引っ掛かり／噴水前後／境界／成長／保存復帰／Pixel／正式固定可否）のみ。未確認を合格と置き換えない。
