# 最新レビュー：Windows用フォント確認ランチャー準備完了／実Unityは未実行

- 現在ブランチ：`feat/daily-world-pass-20261016`
- 最新の確認対象作業commit SHA：`bee1265411b237463ba7a65f699c66e6bef92616`（共有記録だけの後続commitとは区別）
- 製品commit：`75cd7c0ef234934a17aa623860937d894a1d875c`。既存のheightAllowance最小修正を維持。
- 今回の目的：Windows 11／Windows PowerShell 5.1から、指定の実Unity2テストだけをダブルクリック1回で確認できるようにする。
- 実施内容：専用CMD、PS1、両テスト成功後だけ動く撮影用callbackを追加。CMDのCRLF属性を追加。作業commitはpush済み。
- 成功／失敗：ランチャーの作成・限定的な静的／補助検証は完了。Windows実行・実Unityテスト・画像取得は**未実施**。`success=false`／`tests_passed=false`は製品の実確認が未完了であることを示す。ランチャー準備完了は`launcher_preparation_success=true`で区別。
- 根本原因：既知の修正前run `font-only-20261104-204729-31fcbc90`では、高さ14に必要高14.5を+0.5許容で受け入れ、文字頂点0となった。製品修正は75cd7c0で適用済み。今回、新たなUnity側エラーは観測していない。
- 修正内容：確認ツールだけ。製品、既存テスト、フォント素材、Visual Lock安全記録、UI、Merge、SaveDataには今回の差分なし。
- 次にやること：Windowsで `scripts/verify-font-fix-windows.cmd` をダブルクリック。クラウドからUnityは起動せず、この準備作業で停止する。

## ランチャーの動作

既存作業フォルダ・未コミット差分を残し、製品75cd7c0に固定した別worktreeを作る。Windows既存ライセンスを使い、Unity Hub標準配置の `6000.6.4f1` をAndroid targetで1回だけ起動する。旧preview／素材準備／全件テストの処理は呼ばない。

指定対象は以下の2件だけ。

- `Mofumachi.Tests.ReleaseUxNativeTests.ScopedFontsFitTitleTownMergeBookSettingsAndTodayAtPhoneSizes`
- `Mofumachi.Tests.StoryTypographyTests.StoryRolesAndNoticeFitPortraitSizes`

撮影callbackは新しいテストを登録せず、別worktreeの既存PlayModeテストassemblyへ補助ファイルとして配置する。両テストがPassedした後だけ、同じUnity起動内で実UIを描画する。Mergeと会話を720×1480、640×1072の各サイズで撮影する計4 PNG。撮影用状態はメモリ内に限定し、既存ユーザーSaveを使用・変更しない。画像、寸法、SHA256、sourceの一致まで確認した場合だけ`capture_verified=true`になる。

結果は元リポジトリの専用 `Builds/Diagnostics/font-only-<日時>-<識別子>/` へ保存する。最低限 `capture.xml`、`capture.log`、`source-revision.txt`、`font-only-source-info.json`。成功時は `images/` と `capture-native.json` を追加。失敗時は最初のテスト失敗メッセージ／stackまたはUnity側エラーを `first-error.log` へ保存し、再試行しない。元worktreeと診断資料は保持する。APK・全件テスト・Pixel・インストールへ進む処理はない。

## 今回実施した確認と限界

- PowerShellの構文解析エラー0件。ASCIIのみのPS1、Windows PowerShell用CMD、CMDのCRLFを確認。Windows PowerShell 5.1実機実行は未実施。
- 補助関数だけの合成入力確認：指定2件／Android引数、XMLの件数・名前・Passed判定、不正結果4種類の拒否、最初のNUnit／compilerエラー保持を確認。Unityは起動していない。
- 撮影の成功条件を判定する純粋関数9ケースを確認。失敗・skip・重複・不足・追加・別テスト・run失敗時は撮影不可。
- 追加callbackだけをUnity 6000.6.4f1のAPI参照でコンパイルし、警告0／エラー0。Unity実行や実描画の検証ではない。
- 製品commitとの `Assets/`・`ProjectSettings/`・`Packages/` 差分なし。全件テスト・APK・Pixel・新規Diagnostics共有なし。

実Unity起動0回、Nativeテスト呼び出し0回、新しい実画像0枚。フォント修正の実表示、両テスト成功、Pixelでの可読性、Visual採用は未確認のまま。

`error.log`は修正前193ab6eの受領run204729のassert原文を保持している。今回の新規失敗や75cd7c0の結果ではない。`visual-review.md`・`images/`・受領原本は変更していない。

- [機械判定用status.json](status.json)
