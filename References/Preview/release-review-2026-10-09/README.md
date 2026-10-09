# Pixel 3a向けの画面と音のレビュー — 2026-10-09

**実装前のレビュー資料。新しいUnity UIやAPKではありません。** 現在のAPKにこの画面・音・初回ダイアログはまだ含まれていません。

- [設計案](../../../docs/superpowers/specs/2026-10-09-pixel3a-quality-design.md)
- [8画面の一覧画像](screen-proposal-board.png)
- [画面切り替えHTML](preview.html)
- [BGMと6種類のSEの再生ページ](audio-preview.html)
- [BGMの短いMP3](audio/bgm-short-review.mp3)
- [6種類のSEをまとめた音源](audio/se-six-cues-review.ogg)

## 確認方法

PCでリポジトリを取得した後、このフォルダーの`preview.html`をダブルクリックする。ボタンを押すと対応する画面案へ移動する。`audio-preview.html`を開くと音を再生できる。両方のHTMLをフォルダーと素材ごと保存する必要がある。GitHubのHTMLソース表示だけでは音声プレイヤーは動かない。

Pixel 3aで画像を見る場合は`top.png`、`home.png`等、または一覧画像を開く。この資料は実際のゲーム操作を確認するAPKではない。Unityの実装後は新しいAPKをWindowsで作り、端末に入れて確認する。

## 収録内容と変えていない素材

一覧画像は7画面と初回課金案内の8枚。個別画像は393×808で、Pixel 3aの1080×2220と同じ比率を使用した。スクリーンショットの角丸は資料の表示枠。Unityの背景は端末画面全体まで描く。

| 個別画像 | 内容 |
| --- | --- |
| `top.png` | TOP |
| `notice.png` | 初回の課金案内 |
| `home.png` | ホーム／もふまち |
| `quest.png` | キャラ依頼 |
| `merge.png` | Merge盤面 |
| `result.png` | 納品／報酬 |
| `growth.png` | 街成長 |
| `settings.png` | 設定 |

正式6名は[元画像](../../Characters/mofumachi_6characters_master.png)をHTMLから参照し、原本と同じ表示範囲を切り出す。顔・体型・配色・衣装を再生成していない。正式原本、既存v0.3プレビュー、Unity内のCharacterMasterは変更しない。

`town-background-proposal.png`は、既存の街の世界観を参考に作った新しい背景の提案。画像生成により作成したが、キャラクター、文字、UIは含めない。背景とHUD／操作部分を分け、参考絵に焼き込まれたCoinsやメニューは使わない。

現時点で正式な透過全身立ち絵や歩行モーションはない。画面案では原本のキャラカードを使っている。正式デザインを変えずに全身キャラを歩かせる仕上げには、その正式素材が必要になる。

## 音源と出典

新しい音はこの作業で構成した旋律・編曲を、CC0実録サンプルでレンダリングしたもの。以前のオシレーターによる仮音と、第三者の既存デモ曲は流用していない。正式採用・ミックス・ループ調整・Pixelスピーカー確認はまだ行っていない。

- VCSL: https://github.com/sgossner/VCSL — Versilian Studios LLC、CC0 1.0。ピアノ、マリンバ、グロッケン、シェイカー、ボンゴ。
- VSCO-2-CE: https://github.com/sgossner/VSCO-2-CE — 録音Sam Gossner／Simon Dalzell、編集Elan Hickler／Soundemote、CC0 1.0。柔らかいヴァイオリン・セクション。
- [各サンプルの出典・固定リビジョン・SHA-256](audio/sources/sources.json)
- [VCSLライセンス全文](audio/sources/review-audio-vcsl-LICENSE.txt)
- [VSCO-2-CEライセンス全文](audio/sources/review-audio-vsco-LICENSE.txt)
- [レンダリング済み音源のSHA-256](audio/rendered-files.json)

BGM約40秒、決定0.38秒、キャラ反応0.36秒、Merge1秒、納品1.18秒、報酬1.85秒、街成長2.85秒。音声ファイルは44.1kHzステレオOGG、BGM短縮版は20秒のMP3。BGMは方向性を聴くための終止付きサンプルで、連続ループの完成品ではない。

## 確認したこと／まだ確認していないこと

画面案をChromiumで描画し、8画面で素材が読み込めることとPixel比率の配置を確認した。正式画像とUnity内画像のSHA-256一致を確認した。音声10ファイルはFFmpegでデコードに成功し、長さ、44.1kHzステレオ、クリッピングしていないことを検査した。これらは音質の聴感評価ではない。現在のUnityフォントには初回案内等の12文字がないため、Unity実装時に文字を追加する。

これらはUnityの画面、ドラッグ、セーブ、実機の音質、Google Play公開品質の検証ではない。設計確認後にUnity実装とWindowsのネイティブテスト、新しいAPKでのPixel検証を行う。Windows6000.6.4f1で成功した既存APKの結果は維持し、クラウドの最終APK生成は保留する。

## WindowsとPixelでの次の工程

このレビューだけを反映してもUnityの画面は変わらないので、現時点ではこの案を試すための再ビルドは不要。Unity実装後は[既存Windows手順](../../../docs/android-smoke-test.md)の正しいプロジェクトを使う。

`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`

Unity6000.6.4f1のAndroid Build Support／SDK・NDK／OpenJDKを使用し、`Mofumachi → Android → Configure Pixel 3a`でAPI36を設定する。Editorを閉じて`scripts/build-android.ps1`を実行する方法では、テストと新しいAPK・SHA-256を確認する。実装に追加したテストの必要件数も更新する。

USB転送が使えない場合は、新しいAPKをPCから本人のGoogle Driveへアップロードし、Pixel側で同じファイルをダウンロードしてインストールする。古いAPKを新しい実装の確認に使わない。

Pixelでは起動、初回確認と二回目以降の非表示、縦画面全体、全画面の文字とボタン、ドラッグ／Merge、不足時の無消費、正常納品、報酬・街成長、二重納品防止、BGM／SE／各音量、アプリ中断、セーブ／再起動復帰を確認する。
