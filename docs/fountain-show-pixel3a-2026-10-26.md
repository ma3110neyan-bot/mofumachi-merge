# 噴水ショー：原本確認後のPixel 3a受入

run `20261026-111414-78cde31d`、ソース `5edfa5db37f1386b811713b43c4833b4cea451ab` の13ZIP・185原本を確認した。Edit144/144・Play225/225、全369ケースPassed、failed0／skip0／inconclusive0。新162PNGと旧Before3PNGは全SHA256・bytes・720×1280・デコード正常。既存の全件／境界／歩行／実納品成長／通常水／ショー／旧Before／固定景観のゲートも成功。両APKとcaptureのfingerprintは `1fa608c9c6242e8e8be28efcfa55bdc5b67d014ba380a5ebfd8e191dd4a1219e` で一致し、成功logのbytesも一致した。APK本体は未受領なので、以下でローカルの実ファイルをSHA照合する。

## 判定

**同じrunの街QA APKで実機確認へ進行可。噴水の最終採用は未承認。** 本体の拡大、太い通常水、明確に高い3本のショーを実画面で確認した。周辺の花・道・ベンチ・街灯とUIに新たな重要な後退は見当たらない。西東北南Lv1/3/6の12全景は前版とbyte同一。手前キャラは水の前で顔が読め、奥ではショーの水が体の前へ出る。通常時の固体噴水とのライブ前後切替は静止画だけでは合格にしない。

通常の追加水OFF/ON差分は923／963／1741画素、投影外0。ショー同一瞬間OFF/ONの全差分は5061／5056／6387画素。通常水面より高い噴き上げは専用Native検査で2344／2361／2345画素、投影外0、時計・既存波紋位相・状態非変化を確認した。通常→ショーの総差分には表情・名前・通常波紋など別時相の変化があり、全てをショーの効果とは呼ばない。旧Before3は8aa7a14／093151の実原本で、新版撮影へ付け替えない。

独立実画像レビューはCritical0、新たなImportant景観後退0。持越しImportant2は東の木陰からの視認／自力帰還と、通常・ショー中のライブ前後切替。Minor2は新ショーの直線的な水流と既存北外周の反復。奥側の水は口・胴に細く重なるが、示された静止画では両目は読める。実機で糸／棒のように見えたり表情を邪魔したりする場合は、噴水内の調整として扱う。今ここで推測の再改修や街の再設計はしない。

## 今回の街QAだけを更新する

今回の原本受領・記録だけを理由にPull／再ビルドを繰り返す必要はない。PixelをUSB接続してロック解除・USBデバッグを許可し、次を実行する。既存IDへのinstall-rで更新し、保存消去／アンインストールは行わない。

```powershell
& {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    $MofuApk = ".\Builds\Android\20261026-111414-78cde31d\town-life-qa.apk"
    if ((Get-FileHash -LiteralPath $MofuApk -Algorithm SHA256).Hash -ne "14594390f993d40512e108e4d1487c6e107c94e9fe57c392e6b285ccb11b899a") { throw "APKのSHA256が一致しません。" }
    powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\install-pixel3a.ps1" -Apk $MofuApk -PackageName "com.mofumachi.merge.qa.townlife" -Serial "99RAY1BELH"
    if ($LASTEXITCODE -ne 0) { throw "更新が停止しました。この出力を確認してください。" }
}
```

街QAは79,027,285bytes。成長確認APKは79,027,325bytes、SHA256 `3934fdabe1af63742b36122e2bad6d013d5da62dbe2a40e74fa33ffdaf87368c`、ID `com.mofumachi.merge.qa.growthcheck` と記録した。今回の噴水確認は既存保存の街QAを使用し、初期成長のやり直しのために保存を消さない。

## 中央を表示して10分の確認・測定

更新前のLv・Coins・選択キャラ・盤面・図鑑が維持されていることを確認し、中央へ行く。約3分は中央が画面内かつ操作可能な累積時間で、メニュー・Pause・別画面・画面外ではショーの時計が止まる。通常水量、前兆2秒→ショー6秒→復帰1秒、手前→左右→奥→手前の視認性を実際に見る。奥の口・胴へ重なる細い水が自然か、目・名前が追えるかも確認する。5地区の短い往復と東の木陰からの帰還は、既存の持越しとして確認する。

街QAを前面にし、中央を十分に表示した状態で実行する。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\capture-pixel-visual-check.ps1" -Label After -Seconds 600 -Serial "99RAY1BELH"
```

実際のUnity SurfaceView表示時刻からFPS／p95表示間隔を求め、メモリ、バッテリー温度の開始／終了、thermal原文、インストール済みAPK SHA、実機PNGを保存する。15回の収集ごとに進捗を表示する。バッテリー温度はCPU温度ではなく、10分平均はショー瞬間のFPSでもない。今回比較可能なBefore測定がない場合、「FPS低下なし」「発熱増加なし」とは断定せず、新版の測定値と体感を報告する。室温・充電・明るさ等の条件も添える。取得不能はnull／未計測のまま扱い、30Hz上限やEditor頂点数で代用しない。

最後にアプリを閉じて再起動し、保存状態を確認する。インストール終了画面、収集先フォルダーの`pixel-evidence.json`・`pixel.png`・battery／thermal原文、通常／ショーの見え方、前後、重さ・発熱・音・フリーズ／クラッシュ、保存復帰の結果を提出する。測定が止まったら終了画面と収集済み原文を残し、原因不明のまま繰り返さない。

実機受入・ユーザー承認まで噴水採用と2.5D／Merge／以降／mainは保留。街の正式ベース候補固定は維持する。画像はチャットへ貼らない。
