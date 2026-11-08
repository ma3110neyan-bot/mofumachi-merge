# 第7工程：Eドライブv3ランチャー準備済み・Windows実行待ち

- 更新日：2026-11-08（日本時間）。
- 状態：**修正済み・未確認**。準備したのは検証ランチャーだけであり、Windows実Unity合格ではない。
- branch：`feat/daily-world-pass-20261016`。
- v3ランチャーcommit：`c0d2473afc66e43554de4c94224910805c21b5b9`。
- 固定ゲーム実装commit：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。ゲーム再実装なし。
- 検証ID：`progress-ui-e-drive-v3`。
- クラウド／GitHubの開始HEAD：`23a1e43888735809b60f04b7369efbedee6fa7af`。開始時clean、Fetch1回でahead/behind0/0を確認。
- 今回の変更：専用PS1、summary.md、status.jsonの3ファイルのみ。ランチャーcommitと、この共有文書のcommitは別。

## 今回の目的と実施内容

旧v1/v2を再利用・削除・上書きせず、新しいv3の正式検証をWindowsで1回行う入口を準備した。第1〜第6工程は合格済みとして保持し、再実装・再検証していない。

既存`scripts/verify-progress-ui-integration-windows.ps1`だけを最小修正した。CMDは既存バイトのまま。

1. 新しいv3識別子とreceipt名を使用。
2. **Windows実行時に最初に読み取り確認**：正式repoのbranch、HEAD、working tree、worktree list、旧E-v1/v2 receiptと参照先Diagnostics、Diagnostics一覧、該当attempt-stops。
3. 正式repoが指定Cパス・featureブランチ・cleanでない場合、旧worktree登録が残る場合、旧履歴に矛盾がある場合は、**新receipt／worktree／Unityの前で停止**。
4. 旧receipt、存在する結果JSON・source revision・first errorを新Diagnosticsの`history/`へコピーし、長さとSHAで原本と照合。旧stop記録のraw text・SHA、Git状態と旧記録の読み取り結果は`history/preflight.json`へ保存。
5. この小容量履歴も、既存の自動GitHub共有に含める。旧C共有証跡は既存archive処理で保持する。
6. 事前確認で停止した場合も、小容量のBlocked診断・source revision・first/runner errorを保存・共有する。新しいattempt receiptや検証プロジェクト、Unityは作らない。
7. 既存v3 receiptがある場合はUnityを再起動しない。正式terminal結果だけを共有対象とし、Preparing等の未完了状態は別の停止記録として扱う。

**v2中断の根本原因は未確定**。Preparingの値だけから推測修正していない。今回の変更はユーザー指定の別検証IDと安全な再開確認だけ。

## C/E保存場所の正式運用

| 場所 | 用途 |
| --- | --- |
| `C:\Users\User\Desktop\Mofumachi\mofumachi-merge` | 唯一の正式repo。移動・削除・リネームしない |
| `E:\Mofumachi-Validation\projects\...` | v3 direct／integrationの独立worktreeとUnity Library |
| `E:\Mofumachi-Validation\temp\...` | Unity子プロセスだけのTEMP/TMP |
| `E:\Mofumachi-Validation\upm-cache` | Unity子プロセスだけのUPM cache |
| `E:\Mofumachi-Validation\Diagnostics\...` | 原本・XML・Unityログ・履歴snapshot |
| `E:\Mofumachi-Validation\receipts\...` | v3専用CreateNew receipt。旧v1/v2は不変 |
| GitHub／正式repo | 必要な小容量JSON/XML/source revision/logだけ |

C空き約39.4GB、古いC check worktree・旧E-v1/v2 worktree登録とdirect実体の整理完了はユーザー報告。Codexは移動・削除・再測定をしていない。大容量検証コピーをCへ作らず、Unity6000.6.4f1／package／ProjectSettings／ユーザーやマシンの環境設定を変更しない。

## 旧履歴の確認範囲と制約

クラウドにWindows C/Eは接続されていないため、現在のWindows branch/HEAD/working tree/worktree一覧と旧E原本は**未確認**。この制約を隠して「確認済み」とせず、v3ランチャーの最初の読み取り確認へ組み込んだ。クラウドからUnityを起動していない。

| 旧対象 | 現在の情報 |
| --- | --- |
| 旧C容量不足run | GitHub原本5ファイルを保持し、今回もバイト一致を確認。新v3結果へ流用しない |
| E-v1 | 途中状態・Unity合格証拠なしというユーザー報告。原本は未取得 |
| E-v2 | ユーザー確認済み：Preparing、validation=false、Unity0、両Phase NotRun／unity_started=false、first_error／runner_error=null |
| 旧attempt-stops | 原本は未取得。Windows事前確認で該当記録を読み取り保存する |

v2は報告上55件／444件とも未実行であり、製品テスト失敗として扱わない。v3入口ではv2の正式identityと上記Preparing状態を確認する。旧XML／Unityログや起動回数が見つかった場合など、報告と矛盾する記録は無視せず停止する。

v1で結果JSONがない場合、それだけをUnity0の証拠にはしない。不完全履歴として記録し、receipt／Diagnosticsの存在と、記録済み起動・XML／Unityログとの矛盾がないことを確認する。参照するのは各variantのreceiptが指す履歴であり、別runを混ぜない。

既存共有inboxにはまだ旧C `progress-ui-integration-20261107-174541-8e831d09`だけがある。v3の生結果や合格XMLはまだ存在しない。今回の一時確認データをGitHubの生結果へ入れていない。

## 検証条件（既存のまま）

固定製品46f65b2、Unity6000.6.4f1、Android target、EditModeのみ、両Phaseで`--burst-disable-compilation`。

| Phase | 対象 | 合格条件 | Unity起動 |
| --- | --- | --- | --- |
| 1 | `Mofumachi.Tests.ProgressUiIntegrationTests` | Total55／Passed55、Failed/Skipped/Inconclusive0、exit0、source_unchanged=true | 最大1回 |
| 2 | 下記9fixture。Phase1合格かつsource不変の場合のみ | Total444／Passed444、Failed/Skipped/Inconclusive0、exit0、source_unchanged=true | 最大1回 |

正式9fixture：SaveService33、Preferences23、CharacterPreference7、MofuPointService68、GenerationCost33、TownPointReward69、RecoveryExtraHelp71、QuestTownLevel85、ProgressUiIntegration55。

合計Unity最大2回、自動再試行0。失敗はfirst_error、runner_error、failed phase／fixtureを保存し、再起動・全件拡大・推測修正をせず停止。Noah/TownActor Guardは変更・無効化せず、今回のEditMode入口の前提にも追加しない。

## 今回のランチャー確認結果

以下はPowerShell7/Linuxの**一時環境だけ**で行ったランチャー確認であり、Windows実Unity結果ではない。

- 変更前：新しい試行が既存v2 receiptを再利用することを検出する確認が、意図した理由で失敗した。
- 変更後の7確認：v3 receipt分離、旧原本保持とコピー一致、dirty作業ツリー拒否、旧Unity起動記録拒否、旧XML拒否、receipt欠損拒否、旧worktree登録拒否が合格。
- 自動共有1確認：一時ローカルbare remoteへ1回publish。旧C archive・旧v1/v2の履歴コピーを保持し、inbox以外はcommitせず、元repoのHEAD/index/未追跡ファイルと元Diagnosticsを保持。
- 静的確認：UTF8 BOM維持、PowerShell parser正常、Windows5.1互換構文/APIを確認。既存CMD、Unity引数、55/444 XML判定、共有／archive処理本体はバイト単位で不変。想定最長archiveパス241文字。
- 独立した読み取りレビュー：Critical0／Important0。製品コード、既存テスト、Visual Lock、Guard、packageは変更なし。
- **Windows5.1実行とWindows旧E原本照合は未確認**。

## 結果保存と自動GitHub共有

新規結果は：

`E:\Mofumachi-Validation\Diagnostics\progress-ui-integration-progress-ui-e-drive-v3-<timestamp>-<id>\`

新しいreceiptは：

`E:\Mofumachi-Validation\receipts\progress-ui-integration-46f65b24d58531d5934035fbe4cdb4d5b00104a6-progress-ui-e-drive-v3.attempted`

固定共有先：

`References/Review/inbox/latest-progress-ui-integration/`

root結果JSON、source revision、存在するdirect/integration XML・小容量Unity log・first-error、追加のhistory原本コピー／読み取りsnapshot、共有manifestのrun-info.jsonを共有する。1ファイル4MB／合計16MBを維持。大きなUnityログはE原本へ保持し、manifestへ理由を残す。

共有用worktreeもE。inboxだけをcommitし、元repoの未コミット変更・index・無関係commitを含めない。通常push1回、失敗時は再試行せず原本を保持する。旧C失敗runと旧E履歴は保持し、v3と区別する。

## 現在の判定と次の操作

| 必須記録 | 現在値 |
| --- | --- |
| validation_id | `progress-ui-e-drive-v3`（ランチャー準備済み、Windows試行は未作成） |
| 固定ゲーム実装commit | `46f65b24d58531d5934035fbe4cdb4d5b00104a6` |
| Eドライブ使用 | ランチャー設定として確認。Windowsでの実行確認は待ち |
| Phase1 Total / Passed / Failed | 未実行・null / null / null（期待55/55/0） |
| Phase2 Total / Passed / Failed | 未実行・null / null / null（期待444/444/0） |
| 今回Unity起動回数 | 0 |
| 自動再試行回数 | 0 |
| source_unchanged | 未確認（null）。今回の製品ソース変更はなし |
| first_error | null（v3未実行。Native合格の意味ではない） |
| runner_error | null（同上） |
| 第7工程 complete | **false** |

正式repoへGitHubの最新版を反映しcleanにしたうえで、**`scripts/verify-progress-ui-integration-windows.cmd`を1回ダブルクリック**する。PowerShellコマンド入力・PS1の直接実行は不要。旧履歴の事前確認から、条件付きの両Phase、結果共有まで同じ操作で行う。途中で止まったら、同じCMDを再実行せず、その保存済み結果をレビューする。

**第8工程「実機Pixel 3a確認＋リリース候補ビルド」へ進んで安全か：NO**。

理由は1点：**v3の55/55および444/444とsource不変が、Windows実Unityでまだ確認されていないため**。合格した場合も次の独立レビューでYES/NOを判定し、自動で第8工程へ進まない。

改善候補：今回はなし。`commit`はランチャー、`implementation_commit`は固定製品、`review_base_commit`は開始時の先端を表す。
