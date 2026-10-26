# 中央噴水の通常水量・短いショー：確認手順

**111414の13ZIPを受領・確認済み。** Edit144/144・Play225/225、failed0／skip0。162新撮影＋旧Before3・185原本の整合性、両APKのsource／fingerprint／成功log／bytesを確認した。拡大・通常水・showは実画像で改善し、新たな重要な景観後退は見当たらない。同runAPKでPixel確認へ進行可、噴水採用は未承認。次は [同じAPKによるPixel受入](fountain-show-pixel3a-2026-10-26.md)。この記録更新だけでPull／再ビルドを繰り返さない。以下の一括QAは既に完了した実行手順の履歴。

今回の候補は噴水だけを変更した。原画を同じ中心から14％拡大して描画し、通常の6本の太い水流・しぶき・3波紋・8反射を強化した。追加の画材・ParticleSystem・Shader・Colliderはない。道・花壇・街灯・ベンチ・他4地区・正式6名・図鑑・BGM・Save・納品報酬・Mergeは維持する。

ショーは中央噴水が画面内で通常操作可能な累積時間180秒ごと。171秒通常→2秒の前兆→6秒の高い3本の噴き上げ→1秒で通常へ戻る。ホーム再構築で待ち時間をリセットせず、メニュー／Pause／別画面／画面外では停止する。アプリ再起動後は新しい周期となり、Saveへ時間を追加しない。14％・180秒・2＋6＋1秒は今回の実装判断であり、実機で見え方を確認して噴水内で調整する。

最大196quad784頂点＋従来波紋672＝1456頂点。新しいアニメーションは30Hz時相、復帰dt.1秒上限。これらはPixel FPS・温度の測定結果ではない。

## 新版の全件QAを一度実行

GitHub Desktopで `feat/daily-world-pass-20261016` をPullし、Unity Editorを閉じて実行する。今回runtimeが変わったため新しいAPKが必要。旧093151APKは弱い版のBeforeとして保持し、新版の証明へ使わない。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
```

期待する宣言はEdit144／Play225。実結果の全ケースPassed・failed0／skip0と、**162枚の新Unity撮影＋旧Before3枚**、両APKと各SHA256が必要。途中で止まった場合はそのrunのXML／logから診断する。ソース検証をUnity成功と呼ばない。

終了画面と同runの`five-plaza-review-*.zip`全パートを提出する。ZIPには次を含める。

- 現在版Before3枚：8aa7a14／20261026-093151の実PNG原本。`34-fountain-*-current-before.png`。原SHAを維持して別にコピーし、新run撮影162枚には数えない。
- 通常After：`33-fountain-{overview,front,behind}-after.png`。既存水を保持し、新レイヤーだけOFF/ONの比較3対と4時相も含む。
- 特別ショーAfter：`34-fountain-{overview,front,behind}-show.png`、前兆／通常復帰の2枚。
- 同一瞬間のショー検証：`34-fountain-*-show-off.png`3枚。これは現在版Beforeでも通常Afterでもなく、ショー中に追加水描画だけを切った検査画像。ONとの比較で、通常水面上限UV.84より高い噴き上げの画素数・投影外0・時計／従来波紋／NPC／カメラ／保存非変化を検査する。
- `fountain-show-before-after.html`／`fountain-show-comparison.json`と既存の原XML・全景・歩行／成長／保存検査・両build-info。

通常→ショーの総差分には通常の波紋の時相も混ざるため、それだけをショー固有の証明にしない。同一瞬間OFF/ONの高い噴き上げと素の実画像を判断材料にする。チャットに画像は貼らない。

## 同じ新版APKをPixelへ更新

新runの原本／実画面／SHAを確認してから、その街QAを`com.mofumachi.merge.qa.townlife`・Pixel`99RAY1BELH`へ既存installerの保存維持install-rで更新する。SHAがまだない段階で旧版の値を転記しない。アンインストール・保存消去・新機能・main統合は行わない。

通常サイズで通常水量が明確か、中央で約3分待つと短いショーが出るか、終了後自然に戻るかを確認する。手前→左右→奥→手前のキャラ／NPC表示、顔・名前が水で完全に隠れないこと、拡大で歩きづらく見えないことも確認する。元の5地区の往復／東端からの自力帰還など持越しPolishも継続記録する。

## FPS・温度・10分の記録

既存の`capture-pixel-visual-check.ps1`を使用する。Unity SurfaceViewの実際の表示タイムスタンプからFPS／95％表示間隔を計算し、PSS、開始・終了のバッテリー温度、thermalservice原文、インストール済みAPKのSHAと実機PNGを記録する。バッテリー温度はCPU温度ではない。取得できない値は未計測とし、Androidウィンドウのgfxinfoや30Hz上限で代用しない。15回の収集ごとにPowerShellへ進捗を出す。

新版を入れる前の旧版が残っていれば同じ条件でBeforeを収集できる。前後比較は同じ秒数・移動経路・充電状態・明るさ・室温で行い、条件の違う温度を単純な負荷増減と扱わない。Beforeがない場合は新版の測定値だけを報告し、「FPS低下なし」とは断定しない。

新版の中央を表示した状態で次を実行し、10分間、中央の通常／複数回のショーを含めて操作する。途中のメニュー／画面外はショーの時計が止まるため、中央を十分表示する。スクリプトはアプリやセーブをリセットしない。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\capture-pixel-visual-check.ps1" -Label After -Seconds 600 -Serial "99RAY1BELH"
```

表示された出力フォルダーの`pixel-evidence.json`／`battery-start.txt`／`battery-end.txt`／`thermal-start.txt`／`thermal.txt`／`pixel.png`と、重さ・発熱・フリーズ／クラッシュ・操作・音・終了再起動保存の体感を提出する。SurfaceFlingerデータが不足する場合は未計測として止め、同じ測定を根拠なく繰り返さない。個別のショー中FPSは原タイムスタンプと実際に見えたショー時刻を照合してから判断し、10分平均をショー瞬間のFPSと呼ばない。

実装／CPU・API・スクリプト検証と、Unity実画面／APK／Pixel受入を分ける。噴水合格まで代表1名2.5D・Merge・以降の工程は開始しない。
