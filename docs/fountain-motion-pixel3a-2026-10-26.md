# 噴水：連続原本確認後のPixel 3a受入

**同runの生成済み街QAで実機確認へ進行可。噴水の最終採用は未承認。** 今回は受領記録だけで、ゲーム／テスト／QAツールは変更していない。記録更新のためのPull／再ビルドは不要。

## 原本で確認したこと

run `20261026-192546-249dc5ea`、実ビルドsource `98596daf798bcea54d39f6a41b58a565070e1370`。15ZIP・430原本、165全景＋240連続切出し＋旧Before3枚の全SHA／bytes／寸法／デコード正常。原XMLはEdit144/144・Play226/226、合計370成功、failed0／skip0／inconclusive0。前回の17件の診断ログ失敗も解消。capture／街APK／成長APKのsourceとfingerprint `ca87897bd86b67c7a02bfc5d14c90f2c4c8d77ee30dbef68766d9b5fedc3b0ab` が一致し、両ビルド成功logとAPKbytesも一致した。APKbinary自体は未受領なので、下の実ファイルSHA照合を省かない。

実Update60.114秒、240枚、最大記録間隔0.272秒。前兆35.958秒、show状態37.971秒、明確な高い噴き上げ38.472秒、最大水量38.723秒、復帰43.997秒、通常45.003秒を確認。高い噴流は44.500秒のサンプルで消え、60.114秒まで残留なし。`first_show_seconds`の38.723は、最大水量条件を初めて満たした記録時刻であり、show状態の開始時刻とは別。

遮られていない通常水では流水の白い筋・着水・波紋・反射が変化し、全景でもshowの3本の高い曲線噴流を判別できる。西東北南Lv1/3/6の12全景と実成長6枚は前版とbyte同一。90同一／72変更／3新規の全差分を水の効果とは呼ばない。NPC・表情・名前・別時相も含まれる。Before3は5edfa/111414の実原本で、撮り直した画像ではない。

独立画像レビューはCritical0／新たなImportant景観後退0。同じAPKのPixel確認へ進行可。保留項目は、既存会話パネルによる通常水の隠れ、既存東の木陰からの視認／帰還、ライブでの噴水の自然な前後関係。会話パネルは今回48/240サンプルに重なったが、show／前兆／復帰は覆わなかった。旧版との同じ連続記録がないため発生頻度不変とは断定しない。新showの線が規則的／リボン状に見える点も、実機の自然な動きでユーザーが判断する。街やUIを推測で再設計しない。

## 街QAを保存維持更新

PixelをUSB接続してロック解除・USBデバッグを許可し、次を実行する。既存の街QA IDへの`install-r`で、アンインストール／保存消去はしない。成長確認APKの再インストールは今回不要。

```powershell
& {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    $MofuApk = ".\Builds\Android\20261026-192546-249dc5ea\town-life-qa.apk"
    if ((Get-FileHash -LiteralPath $MofuApk -Algorithm SHA256).Hash -ne "90c225caceeab25e44764cabd146ea0dbc2e82b557c1dabe5cd093a2151d5586") {
        throw "APKのSHA256が一致しません。"
    }
    powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\install-pixel3a.ps1" -Apk $MofuApk -PackageName "com.mofumachi.merge.qa.townlife" -Serial "99RAY1BELH"
    if ($LASTEXITCODE -ne 0) { throw "更新が停止しました。この出力をお送りください。" }
}
```

街QAは79,030,001bytes。成長QAは79,030,049bytes、別ID`com.mofumachi.merge.qa.growthcheck`、SHA256 `0b6c747ca3f72ccb0d5069c0c303dc5b02ae073b92920bda67b3b0ae61a017fb`と記録した。実Nativeのモモ／0Coinsは隔離fixtureであり、端末のノア／進行／330Coins等を初期化する指示ではない。

## 中央を開いて10分確認

更新後、現在のLv／Coins／選択キャラ／盤面／図鑑が維持されていることを確認する。TOPから中央広場へ入り、街QAが前面にある状態で次を実行する。インストール直後のTOPのまま計測を始めない。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\capture-pixel-visual-check.ps1" -Label After -Seconds 600 -Serial "99RAY1BELH"
```

通常でも水が流れていると一目で分かるか、メニューを閉じた中央で60秒以内にshowを確認できるかを見る。街QAは45秒周期、初回は約38秒で開始し、6秒のshow後に1秒で通常へ戻る設定。Homeのカメラ移動は時計を止めず、メニュー／別画面／アプリ休止では止まる。高い噴き上げ・水しぶき・反射が見えるか、線やリボンのように不自然ではないかを判断する。

噴水の手前→左右→奥→手前を歩き、顔・名前を追えること、自然な前後関係を確認する。会話パネルが出た時の見え方も確認する。既存の持越しとして、東の木陰／端からタップ・ドラッグで戻れるかも短く確認する。景観や他地区を今回作り直す指示ではない。

10分の重さ・タップ反応・発熱・音・停止／クラッシュを確認し、最後にアプリを閉じて再起動し、保存復帰を確かめる。計測はUnity SurfaceViewの表示時刻、PSS、開始／終了のバッテリー温度、thermal原文、実機PNG、インストール済みAPK SHAを保存する。バッテリー温度はCPU温度ではない。取得不能はnull／未計測とし、比較可能なBefore測定がなければ「FPS低下なし」「発熱増加なし」と断定しない。Native4fpsの切出しや30Hzの描画上限をPixelのFPS測定と呼ばない。

更新終了画面と、計測終了時に表示される結果フォルダーをZIPで提出する。`pixel-evidence.json`・`pixel.png`・surface／battery／thermal原文を残し、通常／showの自然さ、キャラの前後、重さ・熱・音、保存復帰の体感を添える。計測が停止したら出力と取得済み原文を残し、原因不明の再試行を繰り返さない。

ユーザーが同版Pixelで確認するまで噴水採用・2.5D／Merge／以降／mainは保留。街の正式ベース候補固定は維持する。チャットへ画像を貼らない。
