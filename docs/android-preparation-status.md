# Windows APK準備の検証記録 — 2026-10-08

ユーザーの指示により、最終ビルドはWindows PCのUnity 6000.6.4f1で実行する。クラウドのライセンス対応と最終APK生成は保留。手順は[Windows / Pixel 3a実機テスト](android-smoke-test.md)。

## 整備したもの

- `TitleScene` / `GameScene`をビルド対象へ登録し、runtime UGUIでTOP、ホーム、受注、Merge、納品、Result、設定を構成。
- Input Systemでドラッグと2タップ移動、360×640基準・safe area、合成ロックと画面切替・pause/quitの保存を接続。
- 正式6名の画像を同一バイト列でResourcesへ同梱し、UV表示。仮名や主人公選択仕様は追加していない。日本語フォントを同梱。
- AudioManagerに試作音とBGM/SE個別設定を接続。正式素材・音質・操作との相性は引き続き確認対象。
- API 26以上、Target API 34、ARM64、IL2CPP、OpenGLES3、Minimal stripping、debug署名の開発用APK設定を用意。
- `AndroidBuild`に内容確認、失敗時の例外、新しいAPKの検証、出力先指定を追加。
- Windows PowerShellにUnityテスト→APKの実行順と、終了コード・新規XML・実行件数・失敗/スキップ・APK成功ログの確認を追加。実行ごとに出力フォルダを分ける。

元の`defaultScreenOrientation: 0`はPortraitだった。変更したのは画面寸法、回転許可、Target API、描画API、stripping、シーン一覧。既存Unityのmanifest/lockは変更していない。

## このクラウドで確認できたこと

| 検証 | 結果 |
| --- | --- |
| コアNUnit（.NET 8、実際の製品コード） | 39成功、失敗0、スキップ0 |
| コア・Presentation・Editor・全テストソースを実際のUnity 6000.6.4f1管理DLLと内蔵カスタムNUnitでコンパイル | 下記互換性修正後に成功、警告0、エラー0 |
| meta、GUID、シーンbootstrap、asmdef参照、日本語フォントの文字対応 | 静的確認成功 |
| 正式画像とゲーム内画像のSHA-256 | 一致 |
| PowerShell | 7.4.13で構文確認、空白を含むパスの受け渡し、正常XML受理、0件/失敗/スキップ/欠損XML拒否を確認 |
| Unity EditMode / PlayMode | 未実行。Windowsで最低40件 / 4件の成功を確認する |
| IL2CPP / APK / Pixel 3a | 未実行 |

DLL参照のコンパイル確認はUnityによるインポート・実行の代わりではない。PowerShellの検証もWindows上のUnityプロセス実行の代わりではない。

## Windows初回インポートで判明した互換性修正

WindowsのSafe Modeで、`SaveServiceTests.cs`と`DeliveryTests.cs`の`Assert.Multiple`に対するCS0117が報告された。前回の独立コンパイルでは通常のNUnit 3.14を参照し、EditModeテストを含めていなかったため、Unity側の互換性を確認できていなかった。

Unity 6000.6.4f1に内蔵される`com.unity.ext.nunit` 2.1.0はNUnit 3.5ベースのカスタム版で、`Assert.Multiple`を持たない。実際の内蔵DLLを参照し、全テストソースを含む独立コンパイルで同じ2件のCS0117を再現した。検査する値・条件をすべて維持して、通常の`Assert.That`を順に呼ぶ形へ変更した。同じコンパイルが警告0・エラー0で成功し、.NET 8のコアテスト39件も成功した。Unityによるインポートとテスト実行は引き続きWindowsで確認する。

併せて、GitHub Desktopで管理するプロジェクトのVersion Control Modeを`Unity Version Control`から`Visible Meta Files`へ変更した。Unity 6000.6.4f1の同梱テンプレートと同じ設定を用い、未認識のUnity Version Controlプラグインを要求しないようにした。Unity Version Controlパッケージは削除していない。Windows Consoleでの解消確認は更新後の再起動で行う。

## レビューと判断

独立レビューでは静的なWindowsビルド阻害を指摘されなかった。テスト終了時に合成コールバックが残り、後続テストで例外が出る問題を修正した。fixtureはテスト用セッションを先に設定し、コルーチン停止・controller破棄→元セッション復帰→一時セーブ削除の順で終了する。テスト専用のproductionメソッドは削除し、終了処理の回帰テストを追加した。

一時停止時の合成色の残留は、ドラッグ表示も残る可能性を含めて対処した。pause/resumeで画面を再構成し、ロック表示とドラッグghostを更新する。これも回帰テストを追加した。両テストはソースコンパイル確認までで、実行結果はWindowsで確認する。

今回の判断は、既存計画の最小ループをruntime UGUIで接続すること、正式画像を改変せずUV表示すること、現在のUI文字に必要なフォントを同梱すること、Target API 34を実機用に固定すること。画面・音の品質、AndroidのJSON/AOTとファイル置換、依存復元はWindowsとPixel 3aで確認し、実測に問題があれば修正する。正式音源の未収録をコア／ビルド設定整備の前提にはしない。
