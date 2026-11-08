# 第7工程：v4 checkout安全確認付きランチャー準備済み・Windows実行待ち

- 更新：2026-11-08（日本時間）。状態：**修正済み・未確認**。
- branch：`feat/daily-world-pass-20261016`。
- 固定ゲーム実装：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- v4ランチャーcommit：`ee9c8cd6cc4d5ff7d4ba410716aaefea04396f0b`。
- validation_id：`progress-ui-e-drive-v4`。
- 開始HEAD／Fetch1回で確認したGitHub先端：`0b1f50565348313d26f98d63106617903032fb9f`。開始時clean。
- 変更は専用PS1とsummary/statusのみ。既存CMD、ゲーム、テスト件数・内容、Visual Lock、Guard、package、Unityバージョンは変更なし。

## v3の確定状態と扱い

ユーザーの調査報告により、v3 `progress-ui-integration-progress-ui-e-drive-v3-20261108-131915-7ed6e789` は以下と確定：

- Preparing保存、receipt作成、direct登録は成功。
- direct HEADは指定46f65b2。
- statusはD7209／未追跡3。Assetsあり、Packagesなし。
- Unity0、Phase1/2はNotRun。
- **direct worktree checkout不完全。ゲーム本体やテストの失敗ではない。**
- 旧Gitのstdout/stderr/exit codeは未取得なので、低レベルの失敗原因を推測で埋めない。

壊れたdirectだけをユーザーが登録解除・実体削除済み。Test-Path=Falseとの報告を前提とする。旧v3 Diagnostics／receipt／attempt-stopsは保持。Codexによる削除・整理・v3再実行はしていない。

旧パス：

`E:\Mofumachi-Validation\projects\progress-ui-integration-progress-ui-e-drive-v3-20261108-131915-7ed6e789\direct`

## v4の変更

`scripts/verify-progress-ui-integration-windows.ps1`を限定修正。CMDは不変。

1. v4専用receipt・Diagnostics・projectsを使用。旧v1/v2/v3のreceiptを再利用しない。
2. 既存履歴3件を読み取り保存。旧登録や旧v3 project実体が残っていれば停止し、自動削除しない。
3. `git worktree add`を独立したGit子プロセスとして1回実行。stdout／stderrは別ファイルへ逐次コピーし、終了コード・PID・timeout情報をJSONへ保存。
4. PowerShellコールバックを使わず、.NETの非同期ストリームコピーで出力を排出。Git出力を巨大なPowerShell配列へ溜めない。
5. Git待機は最大900秒。タイムアウト／処理例外では、その起動で作ったGitツリーだけを停止・確認し、再試行しない。WindowsはPID／開始時刻を確認した対象へtaskkill /Tを使い、別検証やUnityを名前指定で停止しない。
6. Git長パス設定は`-c core.longpaths=true`を各コマンドにだけ付与。正式repo／グローバル設定／OS設定は書き換えない。
7. receipt作成直後とworktree作成前後のcheckpointを結果JSONへ保存し、receiptフラグの古いsnapshotを減らす。
8. 通常の作成・検証失敗はcatch/finallyでterminal `Blocked`として保存・共有。後処理へ到達しても状態が未完了なら、first_error／runner_error付きのterminal結果へ確定する。未完了receiptの自動再利用はしない。

## Unity起動前の必須gate（両Phase）

| 確認 | 条件 |
| --- | --- |
| Git終了 | worktree addの実終了コードが0、処理例外・timeoutなし |
| HEAD | 指定46f65b24d58531d5934035fbe4cdb4d5b00104a6 |
| status | porcelainが空。変更・未追跡なし |
| ディレクトリ | Assets／Packages／ProjectSettings／scriptsが全て存在 |
| 追跡ファイル | 固定commitのGit treeを列挙し、**全追跡パス**の実在を確認。欠損0 |
| Git登録 | 対象パスが1件、指定HEADのdetached登録。locked／prunableなし |

文字化けで欠損判定を誤らないよう、GitのNUL区切りパスをraw UTF8として読む。ファイル数、欠損数と先頭サンプル、登録情報、実際のGitエラーをvalidation JSONへ保存する。

1つでも不合格ならUnityを起動せず、そのPhaseをBlockedとして後続へ進まない。ファイル欠損の自動修復・checkout再試行はしない。

## 保存場所

正式repoは唯一のC版を維持：

`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`

以下は全て`E:\Mofumachi-Validation`配下：

- `projects\progress-ui-integration-progress-ui-e-drive-v4-<timestamp>-<id>\direct`／`integration`と各Library。
- `temp\...`のUnity子プロセスTEMP/TMP。
- `upm-cache`。
- `Diagnostics\progress-ui-integration-progress-ui-e-drive-v4-<timestamp>-<id>`。
- v4専用receipts、共有用sharing worktree。

Cへ新しい大容量check worktreeを作らず、C正式repoの移動・削除・リネームはしない。

## ログ・共有

各Phaseの小容量証跡：

- `worktree-add.stdout.log`／`worktree-add.stderr.log`。
- `worktree-add.exit.json`。
- `worktree-validation.json`。
- `tracked-tree.stderr.log`／`tracked-tree.exit.json`。
- 既存のresults.xml／unity.log／unity-arguments.txt（実行された場合）。

rootには結果JSON、source revision、失敗時first-error.log。Gitの最初のfatal/errorと終了メタデータを共有結果にも含め、ラッパーの一般メッセージだけにしない。

固定inboxは`References/Review/inbox/latest-progress-ui-integration/`。成功・失敗とも既存の同一CMD内で自動commit・通常pushする。共有用worktreeもE、commit対象はinboxだけ。元repoのHEAD/index/無関係変更を混ぜない。旧C・旧v1/v2/v3の履歴と受領済みobservationsは保持する。

1ファイル4MB／合計16MBを維持。大きなUnity／GitログはE原本に残し、共有manifestへ理由を記録する。first errorとvalidation／exit JSONは小容量で共有。GitHub push失敗は再試行せず、ローカル結果を保持する。

## 今回の確認範囲

**PowerShell7/Linuxの一時環境でランチャーだけを確認。Windows実Unityの55件／444件の証跡ではない。**

- v4 receiptが旧v3と分離することを変更前に検出し、変更後は分離を確認。
- 実Gitの正常・Unicode名を含むworktreeで全gate通過。
- 終了17の作成失敗を拒否し、stdout／stderr／実exit codeを保存。
- HEAD違い、Packages欠損、追跡ファイル欠損、dirty、locked登録を拒否。
- 実際のmain finallyを一時状態で動かし、未完了Preparingからterminal Blockedへ確定することを確認。
- 独立レビューでImportant2件を検出：Git子孫の残存、追跡tree取得時の根本エラー欠落。両方修正。
- 修正後、Git子孫がtimeout後に書き込みを続けないこと、tree取得のfatal本文とexit17が共有JSONへ残ることを確認。
- 一時bare remoteへの共有で、3世代履歴と新ログが含まれ、旧archive／observationsが保持され、inboxだけがcommitされることを確認。
- 最終parserエラー0、UTF8 BOM維持。CMD、Unity引数、55／444のXML合格判定は不変。

Windows PowerShell5.1/.NET4.8のAPI互換性は静的確認。WindowsのCIM／taskkillと実checkout、Unity起動は**未確認**。クラウドのUnityライセンス／描画環境を再調査・起動していない。

## 実Unity条件（不変）

固定製品46f65b2、Unity6000.6.4f1、Android target、EditMode、両Phaseで`--burst-disable-compilation`。

Phase1：`Mofumachi.Tests.ProgressUiIntegrationTests`のみ55/55、Failed/Skipped/Inconclusive0、Unity exit0、source_unchanged=true。

Phase2：Phase1合格かつsource不変の場合だけ、正式9fixtureの444/444：
SaveService33、Preferences23、CharacterPreference7、MofuPointService68、GenerationCost33、TownPointReward69、RecoveryExtraHelp71、QuestTownLevel85、ProgressUiIntegration55。

各Phase最大1起動、合計最大2、自動再試行0。全件・APK・Pixel・Visual撮影・フォント工程へ拡大しない。第1〜第6工程は再実装・再検証しない。

## 現在の結果と次の操作

| 必須記録 | 現在値 |
| --- | --- |
| validation_id | progress-ui-e-drive-v4 |
| 固定ゲーム実装commit | 46f65b24d58531d5934035fbe4cdb4d5b00104a6 |
| E使用 | ランチャー設定として確認、Windows実行待ち |
| Phase1 Total / Passed / Failed | 未実行、null / null / null |
| Phase2 Total / Passed / Failed | 未実行、null / null / null |
| 今回Unity／CMD起動 | ともに0 |
| 自動再試行 | 0 |
| source_unchanged | 未確認（null） |
| first_error／runner_error | v4未実行のためnull。v3の低レベルGitエラーは未記録 |
| 第7工程complete | **false** |

Windows正式repoへ最新版を反映してcleanにしたうえで、**`scripts/verify-progress-ui-integration-windows.cmd`を1回ダブルクリック**する。PS1の検証IDがv4となる。v3を再実行せず、旧receipt・Diagnosticsへ触れない。途中で止まったら同じCMDを繰り返さず、保存済みv4結果だけをレビューする。

**第8工程「Pixel 3a実機確認＋リリース候補ビルド」へ進んで安全か：NO**。

理由は**v4で55/55、444/444、source_unchanged=trueのWindows結果がまだないこと**。合格後も独立レビューで判定し、自動で第8工程へ進まない。

`commit`はランチャー、`implementation_commit`は固定ゲーム実装、`review_base_commit`は開始時の先端。共有文書のcommitと区別する。今回は追加改善候補なし。
