# Windows正式署名キー作成：1回だけ実行する手順

**Windowsでの作成・alias確認・4環境変数SETは、公開7項目の共有commit 8c8579026b43359f240ade43fd3eaf863f354d9cで確認済み。キー作成CMDは再実行しない。** 成功したPowerShell画面を開いたまま保持する。passwordをチャットへ送らない。backup_pending=true。

## 作成時のユーザー操作（履歴・再実行禁止）

1. GitHub Desktop等でfeature branchをFetch／Pullし、新しい2ファイルがscripts内にあることを確認する。
2. `scripts/prepare-release-signing-windows.cmd`を**1回だけダブルクリック**する。
3. 開いたPowerShellでkeystore passwordとalias passwordを、それぞれ2回ずつ入力する。入力は非表示。JKS/keytoolが要求する6文字以上で一致が必要。値をコマンドやチャットへ貼らない。
4. 成功後は**そのPowerShell画面を開いたままにする**。4環境変数はこのプロセス内だけに存在し、別CMDや別PowerShellには引き継がれない。

CMDは新しいPowerShellをNoProfile／NoExitで開く。実行ポリシー指定はその子プロセスだけで、Windowsの永続設定やproject guardを変更しない。既存のrelease preflight／Unity／AAB／APKは実行しない。

## 既存ファイルと停止

- `E:\Mofumachi-Signing`配下に既存.jks／.keystoreがあれば、存在だけを確認して停止する。キー内容を開かず、password入力にも進まない。
- 以前の作成attempt receiptがある場合も停止。削除・再試行・再生成なし。
- 指定Unity JDKのkeytoolがない、repo dirty、repo内にkeystore／secret-likeファイルがある場合も停止。
- 失敗・中断後にこのCMDを再実行しない。できたファイルやreceiptを残したまま原因確認へ進む。

## 作成されるキー（成功時だけ）

| 項目 | 設定 |
| --- | --- |
| 保存先 | `E:\Mofumachi-Signing\mofumachi-release.jks` |
| alias | `mofumachi-release` |
| 方式 | JKS／RSA2048／10000日 |
| 証明書subject | 技術用途のCN=Mofumachi Releaseのみ。個人情報を追加しない |
| keytool | 指定のUnity6000.6.4f1 Android JDKのみ |
| 上書き | 禁止。新規一意stagingから非上書きMove |
| backup | 未実施、backup_pending=true。外部媒体／クラウドへ秘密鍵をコピーしない |

作成後、alias／PrivateKeyEntry／certificate SHA256／keystore SHA256を確認してから、4環境変数をProcess scopeに設定する。password値・長さは表示せず、各変数名とSETだけを表示する。主キー／passwordをGitへ追加しない。

## 結果共有

Gitへ自動共有する実行結果は**次の7項目だけ**。

- keystore_prepared
- alias_configured
- env_vars_configured
- certificate_sha256
- keystore_sha256
- backup_pending
- secrets_recorded

公開状態JSONの固定共有先：`References/Review/inbox/latest-release-candidate/signing-status.json`。

公開情報だけをE上の隔離共有worktreeから通常push1回。正式repoのHEAD／working treeはそのまま保持する。keystoreフォルダをGitへコピーせず、追加pathもJSON1ファイルに限定する。Git共有だけ失敗した場合は再試行せず、E側の公開JSONを保持する。キーを作り直さない。

同じ7項目の公開JSONを`E:\Mofumachi-Validation\Diagnostics\signing-setup\signing-status-<id>.json`にも保存する。秘密鍵本体やpasswordファイルは共有しない。

成功画面ではファイルpath／alias／size／公開SHA2種／SET表示／formal repo cleanを確認できる。環境変数がSETという記録は、その成功時点の当該PowerShellセッションに限る。閉じると失われるため、キー作成CMDでの復旧を試みず、既存キーへの環境再設定を別途行う。

**成功してもrelease preflight／AAB／APK／Pixelへ自動で進まない。** 次の指示まで画面を開いたまま保持する。
