# Windows APK準備の検証記録 — 更新 2026-10-09

ユーザーの指示により、最終ビルドはWindows PCのUnity 6000.6.4f1で実行する。クラウドのライセンス対応と最終APK生成は保留。手順は[Windows / Pixel 3a実機テスト](android-smoke-test.md)。

## 整備したもの

- `TitleScene` / `GameScene`をビルド対象へ登録し、runtime UGUIでTOP、ホーム、受注、Merge、納品、Result、設定を構成。
- Input Systemでドラッグと2タップ移動、360×640基準・safe area、合成ロックと画面切替・pause/quitの保存を接続。
- 正式6名の画像を同一バイト列でResourcesへ同梱し、UV表示。仮名や主人公選択仕様は追加していない。日本語フォントを同梱。
- AudioManagerに試作音とBGM/SE個別設定を接続。正式素材・音質・操作との相性は引き続き確認対象。
- API 26以上、Target API 36、ARM64、IL2CPP、OpenGLES3、Minimal stripping、debug署名の開発用APK設定を用意。
- `AndroidBuild`に内容確認、失敗時の例外、新しいAPKの検証、出力先指定を追加。
- Windows PowerShellにUnityテスト→APKの実行順と、終了コード・新規XML・実行件数・失敗/スキップ・APK成功ログの確認を追加。実行ごとに出力フォルダを分ける。

元の`defaultScreenOrientation: 0`はPortraitだった。変更したのは画面寸法、回転許可、Target API、描画API、stripping、シーン一覧。既存Unityのmanifest/lockは変更していない。

## 検証記録

| 検証 | 結果 |
| --- | --- |
| コアNUnit（.NET 8、実際の製品コード） | 39成功、失敗0、スキップ0 |
| コア・Presentation・Editor・全テストソースを実際のUnity 6000.6.4f1管理DLLと内蔵カスタムNUnitでコンパイル | 下記互換性修正後に成功、警告0、エラー0 |
| meta、GUID、シーンbootstrap、asmdef参照、日本語フォントの文字対応 | 静的確認成功 |
| 正式画像とゲーム内画像のSHA-256 | 一致 |
| PowerShell | 7.4.13で構文確認、空白を含むパスの受け渡し、正常XML受理、0件/失敗/スキップ/欠損XML拒否を確認 |
| Windowsでのインポート・手動操作 | Safe Mode解除、画像表示、BGM再生を確認。下記UI修正後、タイトル→ホーム→受注→Merge→納品→+30 Coins・街Lv.1の画面を確認 |
| Unity EditMode / PlayMode | Windowsの`cf191a0`で40件 / 5件すべて成功。失敗0・未実行0のユーザー画面を確認。今回変更したSDK設定テストはWindowsで再実行予定 |
| AndroidX AARのSDK要件と設定 | 公式core/core-ktx 1.15.0のminCompileSdk=35。旧設定34で検査失敗、修正36で整合を確認。同梱SDKにAPI 36あり、最低API 26を維持 |
| IL2CPP / APK | WindowsでAPKを試行し、下記AAR検査で失敗。SDK修正後の再ビルドと最終APK成功は未確認 |
| Pixel 3a | 実機試験は未実行 |

DLL参照のコンパイル確認はUnityによるインポート・実行の代わりではない。PowerShellの検証もWindows上のUnityプロセス実行の代わりではない。

## Windows初回インポートで判明した互換性修正

WindowsのSafe Modeで、`SaveServiceTests.cs`と`DeliveryTests.cs`の`Assert.Multiple`に対するCS0117が報告された。前回の独立コンパイルでは通常のNUnit 3.14を参照し、EditModeテストを含めていなかったため、Unity側の互換性を確認できていなかった。

Unity 6000.6.4f1に内蔵される`com.unity.ext.nunit` 2.1.0はNUnit 3.5ベースのカスタム版で、`Assert.Multiple`を持たない。実際の内蔵DLLを参照し、全テストソースを含む独立コンパイルで同じ2件のCS0117を再現した。検査する値・条件をすべて維持して、通常の`Assert.That`を順に呼ぶ形へ変更した。同じコンパイルが警告0・エラー0で成功し、.NET 8のコアテスト39件も成功した。その後、Windowsでの通常インポートとEditMode 40件の成功を確認した。

併せて、GitHub Desktopで管理するプロジェクトのVersion Control Modeを`Unity Version Control`から`Visible Meta Files`へ変更した。Unity 6000.6.4f1の同梱テンプレートと同じ設定を用い、未認識のUnity Version Controlプラグインを要求しないようにした。Unity Version Controlパッケージは削除していない。Windows Consoleでの解消確認は更新後の再起動で行う。

## Windows初回再生で判明したUI表示修正 — 2026-10-09

ユーザーのQHD（2560×1440）Gameビューで、1名の画像が画面を覆い、ボタンの文字が欠け、中央に`No cameras rendering`が出た。画像・フォント・BGMの読み込みは確認できたが、操作可能なタイトル画面にはなっていなかった。

Unity同梱UGUIの`AspectRatioFitter.UpdateRect`を確認した。`FitInParent`は対象自身のアンカーを0〜1へ書き換えるため、ページ直下に置いた6名の画像が各表示枠を失い、全体に重なっていた。ホームの街画像にも同じ使い方があった。画像ごとに表示枠のRectTransformを設け、内側のRawImageだけをその枠へフィットさせるよう修正した。原本とUV範囲は変更していない。

幅基準のCanvasScalerは、横長QHDで基準幅360に対して高さが202.5になり、ボタンの文字が収まらなかった。Expandスケーリングとsafe area内の9:16表示枠を組み合わせ、横長EditorとPixel 3aの画面でも縦レイアウトを維持する。描画対象を持たず背景だけをクリアするCameraもbootstrapに追加した。

タイトルの9:16比率、6名が別々の小さな枠に収まること、開始ボタンの文字・クリック判定・ホーム遷移・街画像の範囲を検証するPlayMode回帰テストを追加した。PowerShellのPlayMode最低件数を5へ更新した。全ソースの実際のUnity管理DLL・内蔵NUnit参照コンパイルは警告0・エラー0、コアの.NETテスト39件は成功。クラウドのUnityライセンス対応は保留を維持した。その後WindowsでPlayMode 5件の成功と、開始→ホーム→納品結果の手動操作を確認した。修正前のネイティブ回帰テストを実行できていないため、ネイティブのRED/GREEN確認とは扱わない。

## Windows APKビルドで判明したSDK修正 — 2026-10-09

WindowsのGradle `:launcher:checkDebugAarMetadata`が失敗した。依存する`androidx.core:core`と`core-ktx` 1.15.0はcompileSdk 35以上を要求するが、こちらで用意した設定は34だった。Google Mavenの公式AARメタデータでも`minCompileSdk=35`を確認した。

Unity 6000.6.4f1の同梱SDKにAPI 36があることを確認し、`ConfigurePixel3a`、追跡するPlayer Settings、BuildTimeテストを36へ揃えた。Unity標準GradleテンプレートのcompileSdkに使うAPIを更新するための設定で、最低端末APIは26のまま。旧ビルドメニューは毎回34を設定するため、Player Settingsだけを手動変更する方法では解消しない。

公式AARの要件と実際の設定・ビルドメニュー・同梱SDKを比較するクラウドの検査は、修正前34で失敗、修正後36で成功した。全ソースを実際のUnity管理DLL・内蔵NUnitでコンパイルし、警告0・エラー0。コアの.NETテスト39件は失敗0・スキップ0で成功した。これはWindowsのGradleタスクやAPKの成功を意味しない。最終APKをクラウドで作らず、Windowsで再ビルドする。

Androidビルド用にWindowsのプロジェクトパスを`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`へ変更したこともユーザーのUnity Hub画面で確認した。スタックトレースに残る旧日本語パスだけで、開いている場所が旧フォルダーだとは判断しない。

## レビューと判断

独立レビューでは静的なWindowsビルド阻害を指摘されなかった。テスト終了時に合成コールバックが残り、後続テストで例外が出る問題を修正した。fixtureはテスト用セッションを先に設定し、コルーチン停止・controller破棄→元セッション復帰→一時セーブ削除の順で終了する。テスト専用のproductionメソッドは削除し、終了処理の回帰テストを追加した。

一時停止時の合成色の残留は、ドラッグ表示も残る可能性を含めて対処した。pause/resumeで画面を再構成し、ロック表示とドラッグghostを更新する。これも回帰テストを追加した。終了処理とpauseの回帰テストを含むPlayMode 5件は、その後Windowsで成功した。

今回の判断は、既存計画の最小ループをruntime UGUIで接続すること、正式画像を改変せずUV表示すること、現在のUI文字に必要なフォントを同梱すること、AndroidXの要件に合わせTarget APIを36へ更新すること。画面・音の品質、AndroidのJSON/AOTとファイル置換、依存復元はWindowsとPixel 3aで確認し、実測に問題があれば修正する。正式音源の未収録をコア／ビルド設定整備の前提にはしない。
