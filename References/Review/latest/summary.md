# 第7工程：Eドライブ版検証ランチャー公開／Windows実行待ち

- branch：`feat/daily-world-pass-20261016`
- 今回の確認対象・公開済みPS1修正commit：`8d8f920d397188c369bc59c434857e23f7678ad1`
- 固定するゲーム実装commit：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`（ゲーム・テストは無変更）
- 状態：**修正済み・未確認**。Eドライブ版のWindows実行待ち。
- **第7工程 complete=false**。
- 目的：Cドライブ容量不足で停止した検証入口を、Eの保存領域で実行できるようにする。

## 既存失敗runと根本原因

`progress-ui-integration-20261107-174541-8e831d09`、共有commit `d981dc0f6ce9f6209150c6257b0cd5afbbe2862f` だけを確認した。ゲームcommit一致。Unity起動**0回**、Phase1／Phase2とも未実行、実XML件数はnull。

GitがC上の一時worktreeへ過去のReferences画像等を展開中、`first-error.log:33` に次の最初の具体的容量不足エラーがある：

    error: unable to create file References/Review/curved-boundary-2026-10-25-candidate/native/21-shop-door-opening.png: No space left on device

これは**Unityテスト開始前のファイルシステム容量不足**。製品テスト失敗・Unity/Burstクラッシュ・PowerShellのthrow行を原因とはしていない。C約460MB／E約925GBはユーザー確認値。クラウドからWindows容量を再測定した結果ではない。

旧run、旧attempt receipt、途中worktreeは削除・上書きしていない。GitHub共有原本5ファイルは長さ・SHA一致で保持。旧JSONのsource_unchanged=falseはチェック未到達であり、ゲームソース変更の証拠とは扱わない。

## 今回の変更：専用PS1だけ

変更した実行ファイルは **`scripts/verify-progress-ui-integration-windows.ps1` のみ**。CMD、Core、Presentation、Save、UI、55テスト内容／件数、限定9fixture、Unity版、パッケージ構成、メモリ設定、前工程ランチャー、Visual Lock／Guard／TownActorは無変更。

新識別子は **`progress-ui-e-drive-v1`**。旧C receiptとは別のE receiptを、worktree展開前にCreateNewで記録する。途中で失敗しても、同じCMDの再操作ではUnity／checkoutを再実行せず保存済みterminal結果の共有だけとなる。自動再試行0。

| 内容 | 配置先 |
| --- | --- |
| 基準 | `E:\Mofumachi-Validation` |
| 直接確認worktree／Unityプロジェクト | `projects/新run名/direct/` |
| 結合確認worktree／Unityプロジェクト | `projects/新run名/integration/` |
| Unity Library | 各Eプロジェクト内の`Library/` |
| Diagnostics原本 | `Diagnostics/新run名/` |
| 新attempt receipt | `receipts/製品SHA＋新識別子.attempted` |
| Unity子プロセスTEMP／TMP | `temp/新run名/phase/` |
| Unity子プロセスUPMキャッシュ | `upm-cache/` |
| 共有限定worktree | `sharing/GUID32/` |

表の全配置先はE基準配下。Unity子プロセスの環境変数だけを設定し、PC全体の環境・packageファイル・ProjectSettingsは変更しない。既存ライセンスとUnity6000.6.4f1を使用する。

元Cリポジトリの作業ツリー・index・他の変更を混ぜない既存共有方式を維持。Gitの共通メタ情報と共有限定の小容量Gitオブジェクトは元リポジトリを使う。C上の既存ファイルを削除して容量確保する処理は追加していない。

## 実行・合格条件

| フェーズ | 対象 | 必須 | E版の実結果 |
| --- | --- | --- | --- |
| Phase1 | Mofumachi.Tests.ProgressUiIntegrationTestsのみ | Total55／Passed55／Failed0／Skipped0／Inconclusive0／exit0／source_unchanged=true | **未実行** |
| Phase2 | 従来の正式9fixtureそのまま | Total444／Passed444／Failed0／Skipped0／Inconclusive0／exit0／source_unchanged=true | **未実行** |

Phase1合格とsource不変を確認した後だけ、結合用worktreeを作りPhase2へ進む。Unity起動は合計最大2回、両方`--burst-disable-compilation`、自動再試行0。失敗時は最初のエラー・phase・fixtureを記録し停止。

9fixture：SaveServiceTests33、PreferencesTests23、CharacterPreferenceTests7、MofuPointServiceTests68、GenerationCostTests33、TownPointRewardTests69、RecoveryExtraHelpTests71、QuestTownLevelTests85、ProgressUiIntegrationTests55。フィルタ生成・XML合格判定・件数表は前ランチャーと文字単位で一致する。

成功・失敗とも同操作で **`References/Review/inbox/latest-progress-ui-integration/`** へ必要な小容量JSON／XML／ログだけを共有。各phaseのsource不変・project／Libraryパス、Eの実空き容量、原本場所をJSONへ記録する。原本E、共有限定worktreeもE。1ファイル4MiB／合計16MiBを維持し、大きいlogはEへ保持して理由をmanifestに記載する。

次の新run共有時、今の旧C失敗証跡を同inboxの`previous-runs/progress-ui-integration-20261107-174541-8e831d09/`へ生バイトで保持してからlatestを新runに更新する。共有範囲だけcommit・通常push1回、push失敗は再試行せずE原本を保持する。

## 今回の確認

PowerShell構文・UTF8 BOM・5.1対応APIを静的確認。一時領域で、2プロジェクト／Libraryの配置、新CreateNew receiptと旧receiptの分離、子プロセス環境だけの変更を確認。一時GitのE相当共有worktree・旧原本保存・古いXML除外・push拒否1回停止・元HEAD/index保護は3条件成功。これは実Windows／Unity結果ではない。

独立読み取りレビュー1回、Critical／Importantなし。将来のarchiveパスが5.1の長さ制限を超えないよう共有一時フォルダ名だけを短縮し、241文字以下となることを静的確認。ゲーム側のコンパイルやテストは行っていない。

**今回のUnity起動0回、実テスト0回、自動再試行0回、Fetch1回**。クラウドでWindows／Eを実行したとは扱わない。新runや画像を捏造していない。Visual変更なしのためvisual-review.mdは無変更。

## 次の操作と判定

最新版PS1を取得後、既存 **`scripts/verify-progress-ui-integration-windows.cmd` を1回ダブルクリック**。

**第7工程 complete=false**。

**第8工程「実機Pixel 3a確認＋リリース候補ビルド」へ進んで安全か：NO**。

主要ブロッカーは**Eドライブ版でのWindows Phase1・Phase2合格結果が未確認**の1点。今回の保存先修正をテスト合格とは扱わず、第8工程へ進まず停止する。

summary/statusのcommitは今回のPS1作業を示し、固定ゲーム実装・共有文書自身のcommitとは区別する。
