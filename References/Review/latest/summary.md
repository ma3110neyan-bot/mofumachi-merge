# 第7工程：v4の55/55証跡保持・v5ソース不変判定修正済み

- 更新：2026-11-09（日本時間）。状態：**修正済み・未確認**。
- branch：`feat/daily-world-pass-20261016`。
- 固定ゲーム実装：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- v5ランチャーcommit：`9ee98de7e58eb75cc624c9f30cc055686afebd2a`。
- 検証ID：`progress-ui-e-drive-v5`。
- 今回Fetchは1回。共有済み証跡commit `777b3567c064d8e7793afb5ef1c4aa6cafa5bded`だけをfast-forward取得。
- 変更は専用PS1のsource_unchanged判定・v5識別子・必要な診断配線と、summary/statusだけ。ゲーム・テスト内容／件数・Visual Lock・CMD・checkout gate・Unity引数は不変。

## 保持したv4の正式証跡

対象：

`progress-ui-integration-progress-ui-e-drive-v4-20261108-162156-229f6fd8`

GitHubの実XMLを読み、55ケース全てが`Mofumachi.Tests.ProgressUiIntegrationTests`でPassedであることを確認した。

| 項目 | v4の保存値 |
| --- | --- |
| 固定実装 | 46f65b24d58531d5934035fbe4cdb4d5b00104a6 |
| worktree | tracked7209／missing0 |
| Unity起動／終了 | 1回／exit0 |
| Phase1 | **Total55／Passed55／Failed0／Skipped0／Inconclusive0** |
| Phase2 | 未実行 |
| 自動再試行 | 0 |
| source_unchanged | false |
| runner_error | Pinned product inputs changed; do not start another phase. |

**v4のPhase1 55/55は有効な合格証跡として保持する。** rootのFailedは後続のソース不変判定が通らなかったためで、55件のテスト失敗とは扱わない。既存v4のXML・Diagnostics・receiptを再実行・削除・上書きしていない。XMLの生バイトSHAはstatus.jsonへ記録。

## 原因と今回の方針

旧PS1はUnity後の`git status --porcelain`が空でないだけで、製品ソース変更と判定していた。

Windowsで5追跡済み設定の内容diffは空・LF/CRLF警告のみ、未追跡はrootの.vsconfigだけだったことはユーザー確認情報。Windowsの生diff自体は未取得なので、5ファイルの内容を独立検証済みとは記載しない。確認できたコード上の問題は**statusのdirty表示と内容差分を区別しない判定**。

v5は追跡済みの実内容差分、staged差分、許可外の未追跡、比較エラーを個別に判定する。広いignore、空白差分無視、設定書換え、productの自動restoreは追加しない。

## v5のsource_unchanged条件

対象は隔離project内の**全追跡ファイル**。旧判定の一部パスへの限定を外し、実内容変更をより広く検出する。

1. HEADが固定製品commitと一致すること。
2. 作業ツリー対indexを`git diff --quiet --no-ext-diff --no-textconv --`で確認。
3. index対固定製品を`git diff --cached --quiet --no-ext-diff --no-textconv <fixed-commit> --`で確認。
4. 各比較のexit0は内容diffなし、exit1は変更あり、その他の終了コード／処理エラーは確認失敗としてfalse。
5. stderrにLF/CRLF警告があっても、内容diffなしのexit0ならそれだけでfalseにしない。
6. 未追跡は`git ls-files --others --exclude-standard -z`で取得。従来statusと同じ既存ignoreルールを維持し、追加allowlistは**rootの通常ファイル`.vsconfig`だけ**。
7. 入れ子の`Assets/.vsconfig`、別名、その他の未追跡は拒否。追跡済み.vsconfigを変更した場合は通常の追跡差分として拒否する。
8. .vsconfigのサイズ・SHA、比較結果・終了コード・stderr・許可／拒否パスを診断記録へ保存。監査記録を保存できない場合も不変とは判定しない。

`.gitignore`、グローバル／repo Git設定は変更していない。Git自身が隔離worktreeのindex statキャッシュを更新することはあるが、追跡内容とstagedエントリーの書換え・自動復元は行わない。

## Diagnosticsと共有

各Phaseへ次を追加：

- `source-check.json`：固定baseline、HEAD、tracked比較結果、理由、明示allowlist、許可／拒否未追跡、許可ファイルSHA。
- `source-working.exit.json`／`source-working.stderr.log`。
- `source-index.exit.json`／`source-index.stderr.log`。
- `source-untracked.exit.json`／`source-untracked.stderr.log`。

ソース判定が失敗した場合は、first_errorへ実際の判定理由を記録する。55/55成功後の判定失敗を「Unity root error unconfirmed」という一般的なUnityエラーだけにしない。

固定共有先は`References/Review/inbox/latest-progress-ui-integration/`。既存の同一CMD内自動共有・inbox限定commit・通常push1回・再試行0を維持。新しい判定記録もコピー対象へ追加した。v4を含む旧結果は次回の共有でもarchiveへ生バイト保持し、受領済みobservationsも残す。

## 今回の確認（実Unityとは区別）

一時環境のPowerShell7.4.6／実Gitで、次の9ケースを確認した。

- clean、root.vsconfig単独、同一内容のCRLF変換は通過。
- 設定の実内容変更、stagedゲーム変更、許可外未追跡、入れ子.vsconfig、追跡ファイル削除は拒否。
- HEAD不一致／比較コマンド異常は確認失敗として拒否。

最初の補助確認ではindexファイルの生バイト一致を厳格に要求し、Gitのstatキャッシュ更新を検出した。追跡内容・stagedエントリーの同一性を確認し、`/tmp`の補助assertだけを修正した。ゲームのテストソースや製品設定は変更していない。

一時bare remoteへの共有確認では、新しい判定ログが含まれ、v4の55件XMLと全履歴が保持され、inboxだけがcommitされることを確認した。一時データを実ゲームの証跡としてGitHubへ追加していない。

parserエラー0、UTF8 BOM維持。checkout処理・Unity引数・55/444 XML合格判定・既存publisher/CMD・ゲーム／Visual Lock／ignoreファイルは不変。

別担当レビューは利用上限により未完了。再試行せず、担当側で差分確認と上記の限定確認を完了。Windows PowerShell5.1の構文/APIは静的確認、**Windows実行・v5実Unityは未確認**。

## Windows実行条件と保存場所（不変）

正式repoは`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`。

v5 worktree／Library／TEMP/TMP／UPM cache／Diagnostics／receipt／共有worktreeは`E:\Mofumachi-Validation`配下。v4 receiptを再利用しない。旧v1/v2/v3/v4の証跡を削除・上書きしない。

Unity6000.6.4f1、Android target、EditMode、両Phaseで`--burst-disable-compilation`。

- Phase1：ProgressUiIntegrationTests 55/55＋source_unchanged=true。
- Phase2：Phase1合格時だけ正式9fixture 444/444＋source_unchanged=true。
- 正式件数：SaveService33、Preferences23、CharacterPreference7、MofuPointService68、GenerationCost33、TownPointReward69、RecoveryExtraHelp71、QuestTownLevel85、ProgressUiIntegration55。
- 各Phase Unity最大1回、合計最大2回、自動再試行0。
- v4再実行、全件拡大、APK、Pixel、Visual撮影は行わない。

## 現在の結果・最終判定

| 項目 | 現在値 |
| --- | --- |
| validation_id | progress-ui-e-drive-v5 |
| 固定実装 | 46f65b24d58531d5934035fbe4cdb4d5b00104a6 |
| v5 Phase1 | 未実行、Total/Passed/Failedはnull |
| v5 Phase2 | 未実行、Total/Passed/Failedはnull |
| 今回Unity／CMD起動 | ともに0 |
| 自動再試行 | 0 |
| v5 source_unchanged | 未確認（null） |
| v4 Phase1 | 55/55合格のまま保持 |
| 第7工程complete | **false** |

Windows正式repoへ最新版を反映してcleanにした後、**`scripts/verify-progress-ui-integration-windows.cmd`を1回ダブルクリック**する。最新版のIDはv5。v4は実行しない。停止・失敗したら同じCMDを繰り返さず、保存済み結果を確認する。

**第8工程「Pixel 3a実機確認＋リリース候補ビルド」へ進んで安全か：NO**。

理由は、**v5で55/55、444/444、source_unchanged=trueがまだ確認されていないこと**。YESになっても自動で第8工程へ進まない。

`commit`はv5ランチャー、`implementation_commit`は固定ゲーム、`review_base_commit`は保持したv4証跡の先端。共有文書commitと区別する。今回は追加改善候補なし。
