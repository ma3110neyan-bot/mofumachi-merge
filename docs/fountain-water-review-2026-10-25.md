# 噴水エフェクト候補の確認手順

**2026-10-26更新：実Unity全件・両APK・154実画像を受領して確認済み。** run `20261026-093151-b7d557b1`、ソース8aa7a142。Edit144/144・Play225/225、failed0／skip0／inconclusive0、12ZIP・172原本の整合性を確認した。新たな景観後退は見当たらず、同じAPKでPixel確認へ進行可。水は静止画では非常に控えめで、噴水の採用は未承認。次の実行手順は [Pixel 3a確認](fountain-water-pixel3a-2026-10-26.md)。今回の原本受領・記録だけを理由にPull／再ビルドを繰り返さない。以下の「未確認」「一度の全件QA」は実装時の履歴。

街5地区はユーザーの「街フィールド正式固定 → 次工程開始」により、43a2764の正式ベース候補グラフィックとして固定した。原画・道・装飾・歩行面・正式6名・図鑑・BGM・保存・依頼／納品／報酬・Mergeは維持。残る5項目の街Polishと、未計測の実機性能等は別記し、全条件確認済みとは扱わない。

今回の変更は噴水だけ。既存原画・波紋の上に、子Graphic1つで細い水流・しぶき・波紋・小反射を足した。追加320頂点、アニメーション30Hz、カメラ移動時は投影だけ追従。画面外・無効・アプリPauseでは新レイヤーを休止し、同じ噴水の前後順へ含める。新画像・ParticleSystem・Shader・Colliderなし。

ソース確認は全CPU340/340・API6DLL0警告エラー・関連PS14本成功、独立コードレビュー0Critical/Important/Minor。**今回版のUnity実画面・新APK・Pixelは未確認。** 基準原本は書き換えず、噴水限定の新scopeとuserfreezeへ結んだ。

## 一度の全件QA

今回は実描画を追加したため、GitHub Desktopで既存feature`feat/daily-world-pass-20261016`をPullし、Unity Editorを閉じて実行する。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
```

宣言数はEdit144／Play225、全ケースPassed・failed0／skip0が必要。154枚の720×1280画像は既存144枚＋噴水10枚。全件成功と新しい街QA／別保存の成長確認APK2本・各SHAまで生成する。停止したらそのrunのXML／logから診断し、推測で同版を再実行しない。

終了画面と同じrunの`five-plaza-review-*.zip`全パートを提出する。原XML・画像・metadata・両APKのsource／SHAを照合してから、生成した街QAを保存維持install-rでPixelへ更新する。旧210356APKには今回のエフェクトは入っていない。新SHAを旧版から転記しない。

## Before／After

- `33-fountain-overview-before/after.png`：全景。
- `33-fountain-front-before/after.png`：キャラが手前の状態。
- `33-fountain-behind-before/after.png`：奥の状態。
- `33-fountain-phase-00..03.png`：同じ位置の異なる4時相。
- `fountain-water-before-after.html`／`fountain-water-comparison.json`：比較と検査記録。

Beforeは同じ状態で新レイヤーだけOFF。従来の噴水・既存波紋・街・NPC・カメラ・保存状態は保持する。旧APK全体を再現した画像ではない。新Native検査は3対の可視変化と、噴水投影外の差分0を必須にする。静止画だけでライブの前後切替や端末負荷を合格にしない。チャットに画像を貼らない。

## Pixelでの噴水受入

同じ新街QAで中央へ行き、水流・しぶき・波紋・反射が控えめに動き、キャラの顔・名前が見づらくなっていないか確認する。手前→左右→奥→手前をキャラとNPCが歩いたときの自然な順序、端部からの帰還や既存の街品質も維持する。メニュー／別エリア／終了・再起動で異常がなく、10分以上の重さ・発熱・フリーズ・クラッシュ・音を確認する。SamplerのGC0や頂点数は端末のFPS／熱の測定値ではない。

この噴水工程が実画面・Pixelで合格するまで、代表1名2.5D・Merge以降へ進めない。main統合は未承認のまま。
