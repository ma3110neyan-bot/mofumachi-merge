# 第8工程：リリース候補ビルド前の設定確認で停止

- 更新：2026-11-09。状態：**実装済み・実機未確認**。第8工程の事前確認で不適合を特定した。
- branch：`feat/daily-world-pass-20261016`。作業開始HEAD：`740b5715bfd8bbfa56dacb2097f86cf81a442135`、working treeはclean（クラウド側）。Windows正式repoの現在のclean状態は未確認。
- 固定ゲーム実装：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。HEADの祖先で、Assets／Packages／ProjectSettings／Visual Lockは同commitと差分なし。
- 第7工程は**complete=true**のまま維持。正式証跡`19ddb34ed35838affc191ae98b18e67900613be7`、正式記録`740b5715bfd8bbfa56dacb2097f86cf81a442135`。v5 Phase1 55/55、Phase2 444/444、source_unchanged=true。再実行していない。

## 停止理由

保存済みAndroidプロファイルはDevelopment Build OFFだが、既存カスタムビルダーを使うとONになる。

| 根拠 | 実内容 | RCへの影響 |
| --- | --- | --- |
| Assets/Settings/Build Profiles/Android™.asset:36–40 | Development／Script Debugging／Deep Profiling／Autoconnect Profilerは0 | 保存済みプロファイル自体はOFF |
| Assets/Mofumachi/Editor/AndroidBuild.cs:51,156 | development=true、BuildOptions.Development | ビルダーがOFFを上書きし開発ビルドにする |
| 同:48 | useCustomKeystore=false | 既存経路はdebug署名 |
| 同:49,181 | buildAppBundle=false、出力は.apkのみ | AAB候補を作れない |
| ProjectSettings/ProjectSettings.asset:277–278 | keystore名・alias名は空 | Windowsの正式署名設定は未確認。鍵が存在しないとは断定しない |

既存`scripts/build-android.ps1`は全件EditMode／PlayModeを走らせてから上記Development/QA APKを生成する経路。今回のRC作成には使用していない。別の既存AAB／正式署名用カスタム入口は確認できなかった。

**添付指示Step1の「Development BuildがONなら必要性を明示して停止」と停止ルール「signing不明」に従い、ビルド前で停止した。** Unity／compiler／Gradleの実行エラーではない。設定、コード、署名、Guardを変更して通過させていない。

## 読み取り確認した設定

| 項目 | Git上の確認値 |
| --- | --- |
| Unity | 6000.6.4f1 |
| package | com.mofumachi.merge |
| version／versionCode | 1.0／1 |
| minimum／target API | 26／36 |
| backend／architecture | IL2CPP／ARM64 |
| orientation | Portrait |
| Graphics API | OpenGLES3（自動選択OFF） |
| profileのDevelopment／Script Debugging／Deep Profiling／Autoconnect | 全OFF |
| 既存ビルダー実行時のDevelopment | ONを指定（未実行） |
| Windows実効設定／正式署名 | 未確認 |

この表はGit上の静的設定確認。Unity起動後の実効設定スナップショットやビルド成功の代わりにはしていない。クラウドにはWindows正式repo／Unity既存ライセンス／USB Pixel 3aが接続されていない。既知の環境不足を新たな製品不具合として扱っていない。

## 成果物・実行状態

| 確認 | 今回の結果 |
| --- | --- |
| AAB build | **未実行**（PASS／FAILを判定できるビルドなし） |
| APK build | **未実行** |
| Pixel 3a install | **未実行** |
| Pixel 3a functional check | **未実行** |
| Save/Resume | **未実行** |
| Visual regression | **未実行・未確認** |
| source_unchanged | **true（静的ソース比較のみ）**。実ビルド後の値は未確認 |
| Visual Lock変更 | なし |
| Unity／ビルド／テスト起動 | 全0回 |
| 自動再試行 | 0回 |

新AAB／APK／実機画像／Unityビルドログは存在しないため捏造せず、JSONではnull／NotRun。過去版の成果物を流用しない。build-settings.json、build-info.json、source-revision.txt、checksums.sha256、preflight.log、failure.log、空のscreenshot-manifest.json、未実行のPixelチェック表を固定共有先へ保存した。failure.logは設定不適合の根拠であり、架空のUnityエラーではない。checksumsは記録文書と読み取った入力ファイルのSHAで、AAB/APKのSHAではない。

正式repo `C:\Users\User\Desktop\Mofumachi\mofumachi-merge`を移動・削除・変更していない。以後のビルド／Library／TEMP／cache／Diagnosticsは`E:\Mofumachi-Validation`を使用する前提で、今回は作成・実行していない。

## 必要な最小対応（未実施）

1. Windows上で、リリース用keystore／alias／証明書の識別を確認する。鍵やパスワードをGit／共有ログへ入れない。
2. 既存の開発用ビルダーをRC用に使用しない。Development／debugging／profilersをOFFにし、AABと実機APKを選べるrelease専用の入口・設定を用意する必要がある。現`AndroidBuild.cs`の48–51、156行を通る経路は不適合。既存QA経路を全体変更する案は採用していない。
3. 実行前にWindows正式repo clean、固定製品との関係、変更するビルド用ファイル・差分、署名を再確認する。今回その設定変更や新しいビルダーは作成していない。

固定共有先：`References/Review/inbox/latest-release-candidate/`。

**第8工程 complete=false**。

**Google Play提出準備へ進んで安全か：NO**。リリース用ビルド設定・署名の事前条件が未充足で、実ビルドと実機確認の証跡がない。Google Playへ提出していない。

Codex／AI側から見た次の改善候補：今回はなし（上記の必須対応のみ）。
