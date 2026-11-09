# 第8工程：正式署名作成ランチャー準備済み・Windows入力待ち

状態：**修正済み・未確認（実際のWindows署名作成）**。秘密鍵はまだ作成していない。

- branch：`feat/daily-world-pass-20261016`。
- 署名補助実装commit：`c486b37df3a7c17acfbd852e2b1a4f2d5d95bf77`。
- 固定ゲーム：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- 第7工程complete=true、API compile PASS、release限定34/34 PASSを保持。今回再実行なし。

## 用意した操作

**Windowsで`scripts/prepare-release-signing-windows.cmd`を1回だけダブルクリックする。**

そのPowerShellでpassword2種を各2回、非表示で入力する。値をチャット／コード／コマンド引数へ書かない。成功後はその画面を開いたままにし、次工程で同じセッションを使用する。

現在のクラウド接続からWindowsへ直接操作する手段がなく、passwordもユーザーがローカル入力する仕様のため、Codexがキーを作成したとは扱っていない。

## 実装した保護

- 既存.jks／.keystore／作成receiptがあれば、password入力前に停止。削除・上書き・再生成なし。
- 指定Unity6000.6.4f1 keytoolのみ。RSA2048／JKS／10000日／指定alias、subjectはプロジェクトCNのみ。
- passwordはRead-Host -AsSecureString。keytool argvはenv名参照のみ。平文の履歴・表示・ファイル保存なし。
- 新規一意stagingからFile.Move（上書きなし）。生成1回・alias確認1回、失敗時の自動再試行0。
- ファイル／alias／PrivateKeyEntry／公開certificate SHA256／keystore SHA256を確認。
- 4環境変数はProcess scopeのみ。User／Machineへ永続保存せず、成功セッションをNoExitで維持。
- 秘密鍵の外部コピーはしない。backup_pending=true。
- 正式repoのclean／HEAD維持／キー・secret-likeファイル混入を確認する。
- Gitへ実行結果として共有するのは指定の**7項目だけ**。Eの隔離共有worktreeからJSON1pathを通常push1回。秘密鍵フォルダはコピーしない。

新規ファイルはCMD／PS1／補助限定チェックの3つ。既存release／QAビルダー、ゲーム、ProjectSettings、Packages、Visual Lock、第7工程ファイルは不変。既存process-redaction/Git探索関数を読み込むだけで、既存ビルダーを実行しない。

## 補助検証の結果

新規補助の合成fixtureチェック**26/26 PASS、exit0**。最終構文parseもPASS。keytoolや実キーは使っていない。

検証履歴は`validation/signing-setup-helper/`へ保存。最初のLinux読み込みでEドライブのJoin-Pathが先に評価され停止したため、新規補助の定数宣言1行をWindowsパス文字列へ修正。修正後26チェック成功。その後の自己確認で空白password拒否・共有後repo確認の適用箇所を調整し、最終26チェックも成功。実キー生成／署名コマンドの再試行ではなく、新規補助の合成検証。既存34／55／444は再実行していない。

最終表示文の調整後にAST parseを確認。**Windows PowerShell5.1／実keytoolでの実行は未確認**と明記する。

## 現時点の実結果

| 項目 | 現在 |
| --- | --- |
| release keystore作成 | **NO（Windows未実行）** |
| path／alias | 指定のE保管先／mofumachi-releaseを使う実装。実体未確認 |
| size／keystore SHA／certificate SHA | 未取得 |
| 4環境変数SET | **NO（ローカル入力待ち）** |
| secrets recorded | **NO** |
| keystore Git混入 | Codex作業ではNO。実Windowsツリーは未観測 |
| formal repo clean | 未確認（Windows未接続） |
| backup | 未実施・pending |
| Windows release preflightへ進めるか | **NO（作成・確認・SET完了待ち）** |
| AAB／APK／Pixel／Google Play | 全て未実行 |
| 第8工程 complete | **false** |

実行前なので、`signing-status.json`や実certificate SHAを捏造していない。成功時はWindowsランチャーが、指定7項目の公開JSONだけを固定共有先へ保存する。今回のhelper準備記録と、将来の実署名作成結果は分離する。

操作手順：`windows-signing-preparation.md`。パスワード・キー本体を送らない。次回はGitHubへ共有された公開状態だけを照合する。

固定共有：`References/Review/inbox/latest-release-candidate/`。第7工程・以前のpreflight／エラー原本は保持。今回のfirst_errorはnullで、旧エラーファイルを今回の署名失敗として扱わない。

Codex／AI側から見た次の改善候補：今回はなし。
