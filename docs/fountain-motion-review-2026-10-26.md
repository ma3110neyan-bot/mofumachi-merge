# 噴水の動き・45秒QA：新版の確認手順

最新受領：192546-249dc5ea／98596dafの原370件Passed0failed0skip、165全景＋240連続＋Before3を確認済み。今回の受領記録だけで再ビルドしない。次は生成済み同run街QAでPixel確認する：[同APKの更新・計測手順](fountain-motion-pixel3a-2026-10-26.md)。以下は実装・失敗診断を含む履歴であり、新規のPull／再ビルド要求ではない。

受領追記：run `20261026-185535-c31c8bfb` はEdit144/144（画面）、原PlayMode209/226・failed17・skip0。17件すべてが追加した`MOFUMACHI_FOUNTAIN`の通常ログを既存`LogAssert.NoUnexpectedReceived`が拒否した失敗。診断ログだけを削除し、検査を維持した修正版で再実行する。60秒ケースの状態ログは36/38/44/45秒の遷移を記録したが、Failedを成功に置き換えない。原本／診断は`References/Review/fountain-motion-qa-failure-2026-10-26/`。

状態は**ソース修正済み・新版Unity実画面／APK／Pixel未確認**。前版111414の369件成功・静止画は新版の合格証拠にしない。ユーザーの「水が動いて見えず、3分以上待ってもショーを確認できない」を未達として記録し、噴水採用は保留する。

## 今回の変更

製品runtimeはTownFountainWaterLayout／Graphicの2ファイルだけ。固定帯の明滅中心から、白い流水が実際に下へ移動する表現へ変更。青い水の縁、しぶき、広がる波紋、反射を強め、ショーの3本も曲線で動かす。原画の拡大率・噴水親のY深度・広場の道／花壇／ベンチ／街灯・他4地区・歩行／保存／図鑑／BGM／キャラ／Mergeには変更を加えない。

既存196quad／784頂点、旧水を含む上限1500頂点を維持し、新しいParticle／ライト／製品RenderTextureは追加しない。これはPixel負荷合格の証拠ではない。実機性能が成立しない場合は、別の軽量案を先に提示してユーザー承認を得る。

## 発生条件と間隔

- **街QA（Development APK）とEditor：45秒周期**。通常36秒→前兆2秒→ショー6秒→復帰1秒。
- 新しく街を開いた初回は**38秒でショー開始、約39秒で最大水量、45秒で通常へ復帰**。
- 製品の非Development版は180秒周期を維持。今回の検証には街QAを使用する。
- Home内のカメラ移動・噴水が画面外にある間も待ち時間を進め、戻ってから見えない時間の再待機を要求しない。画面外ではメッシュを再構築しない。
- メニュー／別画面／アプリ休止では停止。Homeの再構築では時計を保持し、休止時間を再開時に加算しない。低FPSの活動フレームを0.1秒へ切り捨てない。
- したがって、メニューを閉じて中央を連続表示した街QAでは、周期の途中からでも45秒以内に次のショーを確認する設定。これは実機確認前の仕様であり、未確認の実機成功とは表記しない。

## 新版を一度QAする

GitHub Desktopで`feat/daily-world-pass-20261016`の最新をPullし、Unity Editorを閉じて実行する。以前の111414 APKを再利用しない。セーブ消去・アンインストールは不要。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
```

想定棚卸しはEditMode144／PlayMode226（計370）。実際の全件成功・failed0／skip0が揃い、下記連続証拠が有効である場合だけ同runの街QA APKを生成する。

新しい`FountainShowRunsFromRealUpdatesWithinSixtySeconds`は、**実時間60秒をUnity Updateで進める**。時刻の手動加算・強制ステージ・早送りを使わない。最初に横長レイアウトを経由し、縦画面の実描画が確定した後で切出位置を決める。各サンプルで実メッシュの全頂点が切出範囲内にあることを検査する。撮影は負荷測定ではない。

## 提出される証拠

- 通常／ショー／復帰の実720×1280全景：`35-fountain-live-normal.png`、`35-fountain-live-show.png`、`35-fountain-live-recovered.png`。
- `screenshots/fountain-live/`：約4fpsの実描画PNG切出し、`fountain-live.json`、`fountain-live.html`。HTMLを開くと連続再生できる。フレームの実時刻に従って再生し、動画やPixel画面とは呼ばない。
- JSONは各フレームの経過実秒、アニメーション位相、ショーステージ、実クロック、切出矩形、寸法、bytes、SHA256を保持。実際のメッシュ移動も検査し、NPCや旧水による画素差だけで新水の動きを合格にしない。
- 既存33／34の同一瞬間OFF／ONは別の因果比較。手動進行の静止画で、上記60秒実Update確認の代わりにはしない。
- Beforeは前版5edfa5db／111414の実PNG3枚を原本SHAのままコピー。古い8aa Beforeや承認原本を改変しない。

終了画面と、新しいrunに出力される`five-plaza-review-*.zip`一式を提出する。ZIPには連続原本・JSON・HTMLも含まれる。HTMLとPNGは全ZIPを同じフォルダーへ展開して閲覧できる。チャットへの画像貼付はしない。

## Pixelと合格条件

新Unity原本の連続再生を確認した後、同runの新SHAを照合して既存街QA ID`com.mofumachi.merge.qa.townlife`へ保存維持更新する。通常でも流水が一目で分かること、45秒以内のショー、増量／しぶき／反射、手前・奥のキャラ視認性をユーザーが実際に確認する。続いて既存600秒計測でFPS／温度／安定性を記録し、終了・再起動後の保存を確認する。温度／FPSが取得不能なら未測定とする。

ユーザーが新版で確認できるまで噴水を合格にしない。街の固定は維持し、2.5D／Merge／以降／mainは未着手のまま。
