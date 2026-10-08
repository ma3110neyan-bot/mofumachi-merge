# WindowsでAPKを作り、Google Pixel 3aで確認する

クラウドは設定・ソースの整備に使い、**最終APKはユーザーのWindows PC上のUnity 6000.6.4f1でビルドする**。Pixel 3aにはUnityライセンスもUnity Editorも不要。クラウド側のライセンス対応は保留にする。

## 1. Windowsの準備

1. GitHub Desktopで`ma3110neyan-bot/mofumachi-merge`を選び、`Fetch origin`→`Current branch: main`→`Pull origin`で今回のコミットを受け取る。未cloneなら`File > Clone repository`から取得する。`Repository > Show in Explorer`で場所を確認し、Unity Hubに`Assets`、`Packages`、`ProjectSettings`があるプロジェクト直下を追加する。
2. Unity Hubで**6000.6.4f1**をインストールし、モジュールの **Android Build Support / Android SDK & NDK Tools / OpenJDK** を追加する。
3. Unity Hubでサインインし、利用条件に合うUnityライセンスを有効にする。
4. プロジェクトを開き、パッケージ復元・インポート・コンパイルの完了を待つ。Consoleに赤いエラーがないことを確認する。
5. `Edit > Preferences > External Tools > Android`で、このUnityに付属するSDK、NDK、JDKを使用する。目安はOpenJDK 17、NDK r27c、SDK Build-tools 36.0.0。Target API 34のプラットフォームも必要。別Unityの古いツールを混ぜない。

初回ビルドはUnityのパッケージとGradle/Maven依存を取得するため、PCのネット接続が必要。Pixel 3aはSIMなしで構わない。

## 2. 確認する設定

`File > Build Profiles`でAndroidを選び、`Switch Platform`または`Activate`で有効化する。プロジェクトのメニュー`Mofumachi > Android > Configure Pixel 3a`で以下を適用する。

| 設定 | 開発用APKの値 |
| --- | --- |
| Unity | 6000.6.4f1 |
| Package Name | `com.mofumachi.merge` |
| 起動シーン | `TitleScene`、続いて`GameScene` |
| Orientation | Portrait、UGUI基準360×640、安全領域に追従 |
| Minimum API | 26（Android 8.0） |
| Target API | 34（この実機確認用。Google Play提出設定は別工程） |
| Scripting Backend / Architecture | IL2CPP / ARM64 |
| API Compatibility / Stripping | .NET Standard / Minimal |
| Graphics API | OpenGLES3 |
| Input | Input System、`InputSystemUIInputModule` |
| Build App Bundle / Export Project | OFF / OFF |
| Development Build | ON、スクリプトデバッガ接続待ちなし |
| 署名 | 開発用debug署名。公開用の鍵は不要 |

正式6名は原本と同一の画像を同梱し、描き直さずUV範囲で表示する。日本語UI用フォントを同梱する。BGM/SEは現時点では試作合成音で、正式音源の収録後に差し替える。

## 3. テストしてAPKを生成

推奨は同梱のPowerShellスクリプト。**このプロジェクトを開いているUnity Editorを閉じてから**、プロジェクト直下で実行する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-android.ps1
```

Unityのインストール先を変えている場合:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-android.ps1 -UnityEditor "D:\Unity\6000.6.4f1\Editor\Unity.exe"
```

スクリプトはEditMode、PlayMode、APKを順に実行する。終了コード、今回生成したXML、非ゼロ件数、失敗・スキップ0を確認し、成功してからAPKを作る。PlayModeにはWindowsの通常のデスクトップ環境を使う。

出力は`Builds\Android\日時-識別子\`。`editmode.xml`、`playmode.xml`、各ログ、`vertical-slice.apk`と表示されたSHA-256を保管する。古いAPKと取り違えないよう、実行ごとに新しいフォルダを作る。

GUIで行う場合は`Window > General > Test Runner`でEditModeとPlayModeを実行し、すべて成功した後に`Mofumachi > Android > Build Development APK`を選ぶ。APKは`Builds\Android\vertical-slice.apk`。Consoleの`MOFUMACHI_APK_SUCCESS`と今回の更新時刻を確認する。失敗時に前回APKが残っていても今回の成功とは扱わない。

## 4. Pixel 3aへインストール

1. Pixel 3aの`設定 > デバイス情報 > ビルド番号`を7回タップし、開発者向けオプションのUSBデバッグを有効にする。
2. データ通信できるUSBケーブルでPCへ接続し、Pixel側でこのPCのUSBデバッグを許可する。
3. PowerShellで、実際のAPKパスを指定する。

```powershell
$Adb = "$env:ProgramFiles\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$Apk = "C:\Projects\mofumachi-merge\Builds\Android\今回のフォルダ\vertical-slice.apk"
& $Adb devices -l
& $Adb shell getprop ro.product.cpu.abilist
& $Adb shell getprop ro.build.version.sdk
& $Adb install -r $Apk
& $Adb shell monkey -p com.mofumachi.merge -c android.intent.category.LAUNCHER 1
```

`devices`が`device`、ABIに`arm64-v8a`、APIが26以上、インストール結果が`Success`であることを確認する。Unityを別ドライブへ入れた場合は`$Adb`も変更する。複数端末がある場合はすべてのadbコマンドに`-s 端末シリアル`を付ける。

## 5. 初回Smoke Test

初期盤面には`お茶 Lv.1`が2個ある。既存セーブがある場合は復帰試験を先に行う。初期状態を作るためのアプリデータ消去やアンインストールは、セーブが消えるので必要な場合だけ行う。

| # | 操作 | 期待結果 | 実機結果 |
| --- | --- | --- | --- |
| 1 | 起動→はじめる | TOPからホームへ進み、日本語が読める | 未実行 |
| 2 | 縦持ち・横持ち、画面端を確認 | 縦表示を維持し、ボタンがシステム領域に重ならない | 未実行 |
| 3 | お茶を空きマスへドラッグ／2タップ移動、盤面外にドラッグ | 空きマス移動は保存。盤面外は数量・位置不変 | 未実行 |
| 4 | Lv.1をもう1個のLv.1へドラッグ | 2個がLv.2の1個になり、演出中の再操作を拒否 | 未実行 |
| 5 | 「お茶 Lv.1 を作る」→Lv.2へ重ねる | 異なるlevelなので合成しない。数量・位置不変 | 未実行 |
| 6 | Lv.2を作る前に依頼を受け、「納品する」 | 不足を表示し、アイテム・Coinsを消費しない | 未実行 |
| 7 | 受注後、Lv.2×1を用意して納品 | Lv.2だけを消費、+30 Coins、街Lv.1、Result表示 | 未実行 |
| 8 | 納品連打・Result再表示・依頼へ戻る | Coins・完了ID・報酬が増えない | 未実行 |
| 9 | ホームへ戻る→アプリを停止→起動し直す | Coins、街、盤面、完了済み依頼、音設定が復帰 | 未実行 |

表は試験項目の一覧。#6は#4より先に実行する。追加で、合成直後のホーム切替・画面OFF・バックグラウンド復帰、BGM/SEの個別ON/OFFと再起動復帰、6名の表示・タップ音を確認する。音は「明るいPOP × 上品 × ファンタジー」の方向性についてフィードバックを記録する。

アプリのプロセスを止めて再起動する例（セーブは消さない）:

```powershell
& $Adb shell am force-stop com.mofumachi.merge
& $Adb shell monkey -p com.mofumachi.merge -c android.intent.category.LAUNCHER 1
```

## 6. 問題が出た場合

| 症状 | 確認・対応 |
| --- | --- |
| SDK/NDK/JDK不足 | Hubの3モジュールとExternal Toolsを確認。SDK Managerで`platforms;android-34`を追加する |
| Scene/Font/Character importエラー | 今回追加したAssetsとmetaが揃っているか、Consoleの最初の赤いエラーを確認 |
| Project is already open | 対象プロジェクトのEditorを閉じてスクリプトを再実行 |
| Unityライセンスエラー | WindowsのUnity Hubでライセンスを有効化する。Pixel側の設定では解決しない |
| `unauthorized` / 端末なし | Pixelの許可ダイアログ、ケーブル、WindowsのGoogle USBドライバーを確認 |
| `INSTALL_FAILED_UPDATE_INCOMPATIBLE` | 以前のAPKと署名が違う。同じdebug鍵でビルドするか、セーブ消去を了承したうえで旧アプリをアンインストールする |
| 起動直後に終了 / セーブできない | 下記ログを採取。特にIL2CPPのJSONシリアライズとAndroidファイル置換を確認する |

```powershell
& $Adb logcat -v time 'Unity:I' 'AndroidRuntime:E' '*:S' | Tee-Object -FilePath .\pixel3a-logcat.txt
```

問題を報告するときは、Unityバージョン、APKのSHA-256、端末のAndroid/API、該当する試験番号、最初のエラー、ビルドログまたはlogcatを添える。

## 現在の検証範囲

クラウドではコアの.NETテスト39件が成功し、追加ソースはUnity 6000.6.4f1の管理DLL参照でコンパイル確認した。これはUnityプロジェクトのインポートやEditMode/PlayMode実行、IL2CPP、APKビルドの成功を意味しない。Windows上のUnityテスト・最終APK・Pixel 3a実機は未実行で、この表に実測結果を追記する。
