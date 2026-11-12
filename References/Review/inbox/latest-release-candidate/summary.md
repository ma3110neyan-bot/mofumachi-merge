# 第8工程：CreateWorktree exit128の原因確定・停止

対象branch：`feat/daily-world-pass-20261016`。source HEAD：`ddbb93f3e6b5a45795be87561f61de5d4194f65c`。
run：`create-worktree-audit-20261112-085636-7bfbd421b1e14b878a04027074133e48`。Git実行は11月12日 **17:56:37〜18:01:34 JST**。
このSHAはWindows正式repoと作成対象のsourceであり、この共有commit自身のSHAではない。

**原因確定：深いReview履歴パスのディレクトリ作成が `Filename too long` で失敗。**
`core.longpaths=unset`だけから推測せず、今回新たに許可された1回の実行のstderr全文から確認した。
実行後の設定変更・修復・再試行は行わない。

最初のfatal全文：

```text
fatal: cannot create directory at 'References/Review/inbox/latest-progress-ui-integration/previous-runs/progress-ui-integration-progress-ui-e-drive-v3-20261108-122257-63a8a5be/history': Filename too long
```

失敗した正確なコマンド（記録。実行指示ではない）：

```text
"C:\Users\User\AppData\Local\GitHubDesktop\app-3.6.10\resources\app\git\cmd\git.exe" "worktree" "add" "--detach" "E:\Mofumachi-Validation\projects\serialization-probe-20261112-085636-7bfbd421b1e14b878a04027074133e48" "ddbb93f3e6b5a45795be87561f61de5d4194f65c"
```

working directory：`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`。
stdoutは0 bytes。**stderr全文8,494 bytes**は[原本](worktree-audit/20261112-085636/received/git-step-01.stderr.log)に保存。
JSON内のstderr文字列とも一致し、受領manifest35件のサイズ/SHA256をすべて照合した。

| 最終報告項目 | 結果 |
| --- | --- |
| source HEAD | `ddbb93f3e6b5a45795be87561f61de5d4194f65c` |
| 診断コード変更ファイル | `scripts/probes/invoke-create-worktree-audit.ps1`のみ |
| formal repo unchanged | YES（前後HEAD一致、working tree clean、staged empty） |
| limited logging test | PASS、1回。意図的read-only失敗はtestディレクトリに分離 |
| CreateWorktree invocation count | 1 |
| failed/succeeded target step | target/1失敗。target/2、target/3は未実行 |
| exact Git command | 上記と`received/git-step-01.json` |
| exit code | 128 |
| first fatal line | 上記全文と`first-fatal.txt` |
| stderr取得 | YES、全文保持 |
| worktree directory | 終了時NO。展開進捗41%は記録されており、最初から一度も作られなかったとは扱わない |
| created worktree HEAD / clean | 未取得 / 未確認（作成失敗後に追加確認を実行せず停止） |
| core.longpaths / git config変更 | NO / NO |
| Unity / AAB / APK / Pixel | 0 / 0 / 0 / 0 |
| serialization P0-P6 / signing verification | 未実行 / 0 |
| automatic retries | 0 |
| 根本原因を特定できたか | YES（今回再採取したexit128のパス長制約） |
| 次の推奨アクション | 別指示で、短い一意なE隔離先名を検討。今回は適用しない |
| 第8工程 complete | false |

隔離先101文字＋区切り1文字＋問題の相対ディレクトリ148文字＝**250文字**。
Gitの実エラーがパス長による作成拒否を示している。閾値を260と決めつけず、
今回は失敗したディレクトリと実メッセージを証拠として扱う。

**自動fatal抽出の注意：** 原本`first_fatal_line`はnull。fatalの直前がGit進捗の単独CRだったため、
v1の行頭正規表現が拾えなかった。stderr本文は欠落していない。
このレビューでCR/LFを分割して実際のfatalを導出した。原本JSON・ログは書き換えず、
診断コードも実行したv1のまま共有する。抽出器の追加修正・再テストは本作業で実施しない。

前run `serialization-probe-20261112-053538-2d6c6d55b1964b569b7ef5d6cb11bff4`の
未保存stderrを復元したとは主張しない。今回の保存済みstderrで、今回の失敗原因を確定した。
Android Graphics末尾iのwriter/妥当性は依然未確定。S1-S7判定は未実施。
既存AAB・source guard・ProjectSettings・ゲーム・Visual Lock・signing経路・versionは変更しない。

前回の集約は`previous-latest/`へ原本保存。過去のAAB生成PASS／全体FAILやStage7の合格を
今回のprobe実行結果に流用しない。共有後に停止し、Unity/probe/AAB/APKへ進まない。

root直下の既存build-info / artifact-metadata / aab-review等は過去AABの履歴として維持する。今回の一次資料は本auditのreceived/であり、既存AABの生成・署名結果を更新したものではない。
