# WindowsビルドとPixel 3aの実機確認

今回の画面・音・保存変更を含む**新しいAPK**で確認します。以前の51,201,037 bytesのAPKと40／5テスト成功は旧実装の結果です。クラウドのライセンス／最終APKはユーザー指示で保留し、Windows Unity6000.6.4f1で実行します。

## 1. 正しいフォルダーを更新する

Unityを閉じ、GitHub Desktopで `C:\Users\User\Desktop\Mofumachi\mofumachi-merge` を開いてFetch→Pull。Saved Games内の別プロジェクトは使いません。変更がPullを妨げる場合は対象差分を確認し、作業を保管してください。一括Discard、既存Stashed Changesの破棄は不要です。

Unity HubにAndroid Build Support／SDK & NDK／OpenJDKを入れ、6000.6.4f1でインポートし、Consoleエラーがないことを確認して閉じます。TOPは `Assets/Scenes/TitleScene.unity`。Configure Pixel 3aは設定用で、Test Runnerは「ウィンドウ → 一般 → Test Runner」から開きます。

## 2. 新しい全テスト→APK

プロジェクトフォルダーでPowerShellを開きます。

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-android.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\build-android.ps1 -Qa
```

必要なら `-UnityEditor "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe"` を指定します。Unity Editorは閉じた状態で実行。通常版とQA版は各回の `Builds\Android\日時-識別子\` へ出力します。

現時点の検証ゲートはEditMode最低67件、PlayMode最低22件、必須6／6 suite、全test-case Passed、failed／skipped 0。各回の新XMLとログを検証し、成功時だけAPKを生成します。実際のUnityでの件数・結果はXMLで確定してください。ソース上の予定件数をネイティブ合格と扱いません。

`MOFUMACHI_APK_SUCCESS`、非空の新APK、`build-info.json`のGit revision／Unity／ID／versionCode／SHA／サイズを保存します。Gitがなければrevisionはunknownです。通常版 `com.mofumachi.merge`、QA版 `com.mofumachi.merge.qa`。QA版には独立した保存があり、QAビルドのID／表示名は終了・失敗時に元へ戻します。開発APKはPlay提出物ではありません。

## 3. USB接続と更新

見つかったデータ通信ケーブルでPixel 3aとPCを接続し、Pixelをロック解除。開発者向けオプションのUSBデバッグをON。端末の「USBデバッグを許可しますか？」でこのPCを許可します。ファイル転送モードが必須ではありません。認識しない場合はWindowsのPixel USBドライバー／ポート／ケーブルを確認します。

```powershell
$Adb = "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
& $Adb devices
```

シリアルの右が `device` なら許可済み。空は未接続、unauthorizedは端末側の許可待ち。複数端末の場合は対象Pixelのシリアルを記録します。

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-pixel3a.ps1 -Apk "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Android\今回のフォルダー\vertical-slice.apk" -Serial "Pixelのシリアル"
powershell -ExecutionPolicy Bypass -File .\scripts\install-pixel3a.ps1 -Apk "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Android\今回のQAフォルダー\vertical-slice-qa.apk" -Serial "Pixelのシリアル" -PackageName com.mofumachi.merge.qa
```

スクリプトはUnity同梱aapt2 build-tools36.0.0でAPKの実ID／ARM64を確認し、端末API26以上／ARM64／許可を確認して `install -r`。端末コマンドはすべて `-s` で指定します。署名不一致等は停止し、アンインストールや `pm clear` で保存を消しません。成功後に選んだアプリを起動します。

## 4. 通常版の更新試験（現在の保存を残す）

- 初回「課金について」の全文と「確認しました」を確認。未確認で背後のホームへ入れないこと。確認後は再起動で再表示されないこと。
- 現在のCoins30／街Lv.1／依頼完了、盤面・所持、従来のBGM／SE OFFを保持すること。旧v1からv2へ移行します。
- 完了済み依頼は再納品できず、報酬が増えないこと。
- 設定のBGM／SE個別ON/OFF、音量0とOFFの区別、変更後の再起動復帰。

## 5. QA版の最初から一周試験

QA版を選び、Coins0／街Lv.0から開始します。QAの2回目起動では保存が残るため、既に完了したQAを初期状態と誤認しないでください。

| 手順 | 確認する結果 | 今回の実機記録 |
| --- | --- | --- |
| TOP→初回案内→ホーム | 全文、連打・戻るで迂回不可、六名原画 | 未実行 |
| 依頼を受ける | お茶会の準備、必要Lv.2×1、30 Coins | 未実行 |
| 不足状態 | 納品ボタン無効、無消費／報酬0 | 未実行 |
| Lv.1二つをドラッグでMerge | Lv.2一つ、成功音、連打で二重合成なし | 未実行 |
| 2回タップで空セルへ移動 | セルを正しく移動、盤面外dropは無変更 | 未実行 |
| 納品→報酬→街成長 | +30、街Lv.0→1、完了／チャイム／ファンファーレ | 未実行 |
| ホーム→完了依頼 | Coins30、街Lv.1、再納品不可 | 未実行 |
| 各画面から設定→戻る | 元画面へ、納品エラーが設定へ出ない | 未実行 |
| BGM／SE・各音量 | 各OFF／0／最大、BGM巻き戻り／二重音／音割れなし | 未実行 |
| background／再起動 | ghost／選択／ロックが残らず進捗と音量保持 | 未実行 |
| 全7画面＋案内 | 1080×2220全高、Safe Area、文字切れ／重なりなし | 未実行 |
| 一周＋長めの再生 | クラッシュ、異常な熱・メモリー増加、ループの継ぎ目なし | 未実行 |

端末のdensityは `& $Adb -s "シリアル" shell wm density`、画面は `shell wm size`、APIは `shell getprop ro.build.version.sdk` で記録。ボタンの48UI単位は実機48dpの測定と別です。Pixelのdensity／表示領域で実際の押しやすさを確認してください。

再起動は `& $Adb -s "シリアル" shell am force-stop com.mofumachi.merge.qa` の後、端末で同じQAアプリを開きます。通常版の場合はIDの `.qa` を外します。保存を消すコマンドは使いません。

問題が出たらAPK SHA、ID、日時、端末API、操作順、実スクリーンショット、今回のXML／ログを対応させて保存します。logcatは `& $Adb -s "シリアル" logcat -d -s Unity AndroidRuntime`。ログに個人情報がないか確認して共有してください。テストのために別の機能を追加せず、画面・入力・保存・音の修正を優先します。
