# WindowsでAPKを作り、Google Pixel 3aで確認する

クラウドは設定・ソースの整備に使い、**最終APKはユーザーのWindows PC上のUnity 6000.6.4f1でビルドする**。Pixel 3aにはUnityライセンスもUnity Editorも不要。クラウド側のライセンス対応は保留にする。

## 1. Windowsの準備

1. GitHub Desktopで`ma3110neyan-bot/mofumachi-merge`を選び、`Fetch origin`→`Current branch: main`→`Pull origin`で今回のコミットを受け取る。未cloneなら`File > Clone repository`から取得する。`Repository > Show in Explorer`で場所を確認し、Unity Hubに`Assets`、`Packages`、`ProjectSettings`があるプロジェクト直下を追加する。
2. Unity Hubで**6000.6.4f1**をインストールし、モジュールの **Android Build Support / Android SDK & NDK Tools / OpenJDK** を追加する。
3. Unity Hubでサインインし、利用条件に合うUnityライセンスを有効にする。
4. プロジェクトを開き、パッケージ復元・インポート・コンパイルの完了を待つ。Consoleに赤いエラーがないことを確認する。
5. `Edit > Preferences > External Tools > Android`で、このUnityに付属するSDK、NDK、JDKを使用する。目安はOpenJDK 17、NDK r27c、SDK Build-tools 36.0.0、API 36のプラットフォーム。別Unityの古いツールを混ぜない。

Androidビルドでは、プロジェクトまでのパス全体を半角英数字などのASCII文字にする。今回のWindows側の場所は`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`。日本語を含む親フォルダーを変更・移動するときはEditorを閉じ、Unity Hubの「追加 → ディスクからプロジェクトを追加」で新しい場所を登録する。GitHub Desktopが旧場所を参照している場合は`Locate`で同じ新しいフォルダーを指定する。Unity Hubでは名前だけでなくパスを確認し、別の`Saved Games\Mofumachi Merge`を開かない。

初回ビルドはUnityのパッケージとGradle/Maven依存を取得するため、PCのネット接続が必要。Pixel 3aはSIMなしで構わない。

`Fetch origin`を押しても`Pull origin`に変わらない場合は、取得する差分がない可能性がある。`History`で今回のコミットがあるか確認する。`No local changes`だけでは最新かどうかは判断できない。

Editorで起動を確認するには、`Project`（プロジェクト）で`Assets > Scenes > TitleScene`をダブルクリックし、上部中央の再生ボタンを押す。`Game`（ゲーム）タブで6名・タイトル・「はじめる」の文字が見えることを確認し、「はじめる」を押してホームへ進む。タイトルは静止表示でBGMが流れる。横長QHDのGameビューでも内容は中央の縦9:16へ収める。縦長で大きく確認したい場合はGameビュー上部の解像度メニューで9:16のAspect Ratioを選ぶ（なければ`+`でWidth=9、Height=16を追加する）。確認後は再生を停止する。

## 2. 確認する設定

`File > Build Profiles`（ファイル → ビルドプロファイル）でAndroidを選び、右下の「プロファイル切り替え」で有効化する。Androidに緑の「有効」が表示されたら、プロジェクトのメニュー`Mofumachi > Android > Configure Pixel 3a`で以下を適用する。

| 設定 | 開発用APKの値 |
| --- | --- |
| Unity | 6000.6.4f1 |
| Package Name | `com.mofumachi.merge` |
| 起動シーン | `TitleScene`、続いて`GameScene` |
| Orientation | Portrait、UGUI基準360×640、安全領域に追従 |
| Minimum API | 26（Android 8.0） |
| Target API | 36（AndroidXのcompileSdk要件35以上に対応。Google Play提出設定は別工程） |
| Scripting Backend / Architecture | IL2CPP / ARM64 |
| API Compatibility / Stripping | .NET Standard / Minimal |
| Graphics API | OpenGLES3 |
| Input | Input System、`InputSystemUIInputModule` |
| Build App Bundle / Export Project | OFF / OFF |
| Development Build | ON、スクリプトデバッガ接続待ちなし |
| 署名 | 開発用debug署名。公開用の鍵は不要 |

正式6名は原本と同一の画像を同梱し、描き直さずUV範囲で表示する。日本語UI用フォントを同梱する。BGM/SEは現時点では試作合成音で、正式音源の収録後に差し替える。

API 36はビルドとTargetの設定であり、実機にAndroid 16を要求する設定ではない。インストール可能な最低バージョンはMinimum API 26で維持する。

## 3. テストしてAPKを生成

推奨は同梱のPowerShellスクリプト。**このプロジェクトを開いているUnity Editorを閉じてから**、プロジェクト直下で実行する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-android.ps1
```

Unityのインストール先を変えている場合:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-android.ps1 -UnityEditor "D:\Unity\6000.6.4f1\Editor\Unity.exe"
```

スクリプトはEditMode、PlayMode、APKを順に実行する。終了コード、今回生成したXML、EditMode最低40件・PlayMode最低5件、失敗・スキップ0を確認し、成功してからAPKを作る。PlayModeにはWindowsの通常のデスクトップ環境を使う。

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
| SDK/NDK/JDK不足 | Hubの3モジュールとExternal Toolsを確認。SDK Managerで`platforms;android-36`を追加する |
| `checkDebugAarMetadata`でAndroidXがcompileSdk 35以上を要求、現在android-34 | mainのSDK修正をPullし、`Configure Pixel 3a`を再実行してからAPKを再ビルドする。設定ファイルとビルドメニューの両方を36へ更新した。旧コードではPlayer Settingsを手動で変更してもビルド時に34へ戻る |
| `Invalid project path` / `non-ASCII characters` | Editorを閉じ、親フォルダーも含めたパスをASCII文字へ変更する。Unity Hubへ新しい場所を追加し、GitHub Desktopも同じ場所へ合わせる。上記Windowsの準備を参照 |
| Safe ModeでCS0117、`Assert`に`Multiple`がない | この互換性修正を受け取る。Editorを閉じ、GitHub Desktopでmainの`Fetch origin`、表示されたら`Pull origin`を押し、同じプロジェクトを開き直す。テストの無効化やNUnitの手動追加は不要 |
| `Unknown version control plugin: Unity Version Control` | GitHub Desktop用のMode=`Visible Meta Files`を今回の修正で適用した。更新後にEditorを開き直して確認する |
| 画像が画面全体を覆う／ボタン文字が見えない／`No cameras rendering` | Editorを閉じて今回のUI表示修正をPullする。修正後は画像ごとの表示枠、縦9:16表示、背景Cameraを生成する。TitleSceneの再生後に6名と「はじめる」を確認し、ホームへの遷移を試す |
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

2026-10-09、ユーザーのWindows Unity 6000.6.4f1で`cf191a0`のEditMode 40件・PlayMode 5件がすべて成功（失敗0、未実行0）した画面を確認した。手動操作でもタイトルから納品まで進み、+30 Coins・街Lv.1を確認した。

その後のAPK作成は、旧API 34設定によるAndroidXのAARメタデータ検査で失敗した。今回のAPI 36修正後は、クラウドで公式AARの必要APIと設定・同梱SDKの整合、全ソースのUnity管理DLL参照コンパイル、コアの.NETテスト39件成功を確認した。SDK設定テストのWindows再実行とAPK再ビルドはこれから行う。IL2CPPを含む最終APKの成功とPixel 3a実機結果はまだ確認できていない。
