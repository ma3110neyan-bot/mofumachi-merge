# Windows正式署名の準備（未実行の次工程案）

現在、ユーザー回答で次の2点を確認した。

- 署名用4環境変数は未設定。
- リリース用keystore／alias／passwordもまだ用意していない。

このため、**preflightも実AAB/APKビルドも開始していない**。この文書はキー作成・ビルドの実行結果ではない。

## 次に必要な作業

Windowsローカルで新しい正式署名用キーを準備する。キーはGitリポジトリや検証用キャッシュの外へ置く。keystore・passwordをチャット、Git、Diagnostics、summary/statusへ送らない。

準備案（未採用・未実行）：

| 項目 | 候補 |
| --- | --- |
| キー保管先 | `E:\Mofumachi-Signing\mofumachi-release.jks` |
| alias | `mofumachi-release` |
| 作成ツール | Windowsに既存のUnity6000.6.4f1 Android JDK/keytool |
| キー形式・方式 | JKS／RSA2048／有効期間10000日を候補とする |
| password入力 | Windowsの対話入力のみ。コマンド引数・コードへ値を直書きしない |
| 保管 | keystoreとpasswordの復旧手段をユーザー管理で確保。Gitへ入れない |

今回はキーを生成していない。パス・alias・キー運用は次工程で確定する。既存ファイルが見つかった場合は上書き・作り直しをしない。Google PlayのApp Signing／upload keyの役割も未確認であり、今回採用済みにしない。

## キー準備後の環境変数

| 環境変数 | 設定する内容 |
| --- | --- |
| MOFUMACHI_KEYSTORE_PATH | 実在するkeystoreファイルの絶対パス |
| MOFUMACHI_KEYSTORE_PASSWORD | keystoreのpassword |
| MOFUMACHI_KEY_ALIAS | 実際に作成したalias |
| MOFUMACHI_KEY_ALIAS_PASSWORD | そのaliasのpassword |

Windowsの同じPowerShellプロセス内で設定してから、既存releaseスクリプトを子プロセスとして呼ぶ方式を推奨する。passwordはマスク付き対話入力を使い、値や環境変数一覧を表示しない。具体的な設定コマンドは、キー準備後に秘密値を含めず渡す。

## その後の実行順（今回は未実行）

1. 正式repoのbranch／HEAD／untrackedを含むcleanを確認。ゲーム・QA・Visual Lockの差分なしを確認。
2. 既存`scripts/build-android-release.ps1`をビルド指定なしで1回実行。`PreflightPassed`／Unity0／sourceと正式repo不変／公開certificate SHA256を確認。
3. PASS時だけ既存スクリプトの`-Build -Format Both`へ進む。AAB1回、完全PASS時だけAPK1回。実ビルド呼出し内部の署名安全確認も維持し、省略しない。
4. 保存済み結果だけを共有し、失敗時の再実行・キー作り直し・アンインストールは行わない。

正式repoは`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`のまま。検証／Library／TEMP／cache／Diagnostics／成果物は`E:\Mofumachi-Validation`。大容量AAB/APKや秘密鍵をGitへ追加しない。

クラウドからWindowsを直接操作する接続はないため、Windowsでの実操作が必要。今回、既知の環境確認やUnity起動を繰り返していない。
