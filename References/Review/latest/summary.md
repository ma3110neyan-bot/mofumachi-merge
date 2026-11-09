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

---

# 第7工程の確定記録（変更せず保持）

# 第7工程：v5実Unity検証完了・第8工程へ着手可能

- 更新：2026-11-09（日本時間）。状態：**完了（第7工程の指定検証範囲）**。
- branch：`feat/daily-world-pass-20261016`。
- 固定ゲーム実装：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- v5ランチャー：`9ee98de7e58eb75cc624c9f30cc055686afebd2a`。
- 正式証跡commit：**`19ddb34ed35838affc191ae98b18e67900613be7`**。
- validation_id：`progress-ui-e-drive-v5`。
- 対象run：`progress-ui-integration-progress-ui-e-drive-v5-20261109-101604-63c260c0`。

## 結論

**第7工程 complete=true**。

**第8工程「Pixel 3a実機確認＋リリース候補ビルド」へ進んで安全か：YES**。

既存Windows実Unity結果のJSON、両XML、source-check、checkout記録、source revision、共有manifestを照合し、以下の全条件を満たすことを確認した。検証を再実行していない。

| 必須条件 | Phase1 direct | Phase2 integration |
| --- | --- | --- |
| Total | 55 | 444 |
| Passed | **55** | **444** |
| Failed | 0 | 0 |
| Skipped | 0 | 0 |
| Inconclusive | 0 | 0 |
| Unity exit code | 0 | 0 |
| source_unchanged | **true** | **true** |
| validation_passed | true | true |
| Unity起動 | 1回 | 1回 |
| 自動再試行 | 0 | 0 |
| 追跡ファイル／欠損 | 7209／0 | 7209／0 |
| HEAD／clean／登録 | 固定commit一致・正常 | 固定commit一致・正常 |

Root result=Passed、validation_passed=true、source_unchanged=true。Unity起動合計2回、自動再試行0。first_error／runner_error／first_failed_phaseは全てnull。ソース不変判定でv4を止めていた箇所はv5の両Phaseで通過した。

## 実XMLとfixture確認

Phase1は`Mofumachi.Tests.ProgressUiIntegrationTests`のみ55ケース。全fullnameが一意で、全ケースPassed。

Phase2は下記の正式9fixtureだけで444ケース。全fullname一意、追加fixture・重複・欠落なし。

| fixture | Passed |
| --- | --- |
| SaveServiceTests | 33 |
| PreferencesTests | 23 |
| CharacterPreferenceTests | 7 |
| MofuPointServiceTests | 68 |
| GenerationCostTests | 33 |
| TownPointRewardTests | 69 |
| RecoveryExtraHelpTests | 71 |
| QuestTownLevelTests | 85 |
| ProgressUiIntegrationTests | 55 |
| 合計 | **444** |

Phase1とPhase2内の同じ55ケースを、別の新規ケースと数えていない。指定の2段階検証が成功したという判定。

## ソース・実行条件の照合

両Phaseのsource-checkで、固定baselineとHEADが46f65b2に一致し、working diff／index diff／untracked取得の終了コードが全て0。problemsとdisallowed_untrackedは空。

実際のコマンド引数も確認し、全追跡パスを比較し、空白無視や広いパス除外が追加されていないことを確認。許可された未追跡は両Phaseともrootの`.vsconfig`だけ。

両Phaseの実Unity引数はUnity6000.6.4f1、Android target、EditMode、Burst無効化に一致。checkout gateも終了0、固定HEAD、clean、正常登録、tracked7209／missing0。

正式repoはCの`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`、検証・Library・TEMP/TMP・UPM cache・Diagnostics・共有worktreeはEの運用を維持した。今回ゲーム・テスト・ランチャー・Visual Lock・Guardを変更していない。ソース不変比較はVisual Lock原本を含む全追跡パスが対象。

## 証跡の完全性

`run-info.json`に記録された**43ファイル**の長さとSHA256を共有原本と照合し、全て一致。source-revision.txtも固定commitと一致。共有commitの親とmanifestのremote_parentも一致し、commit範囲はinboxだけ。

固定共有先：

`References/Review/inbox/latest-progress-ui-integration/`

- rootのprogress-ui-integration-result.json／source-revision.txt／run-info.json。
- direct/results.xml、integration/results.xml。
- 各source-checkと比較exit／stderr。
- 各checkout stdout／stderr／exit／validation。
- 保存されたUnityログと旧履歴。

原本JSON・XML・ログを編集して成功扱いへ変換していない。旧C／v1／v2／v3／v4とobservationsは保持。以前の合格や一時確認データを今回のXMLへ流用していない。

原本の`review_required=true`、`stage8_safe=false`は、ランチャーが証跡レビューを待つための値。その原本は変更せず、**今回の照合完了後の判定をlatest/status.jsonのnext_stage_safe=trueへ記録**する。

## 今回行った操作

Fetch1回で共有済み証跡を取得。既存inbox証跡commitだけをfast-forward同期し、保存原本を読み取り照合した。今回の変更はsummary/statusだけ。

- 新しいCMD／Unity／テスト実行：0。
- 自動再試行：0。
- ゲーム／テスト／ランチャー変更：なし。
- Windows原本／receipt変更：なし。
- reset／stash／force push：なし。
- APK／Pixel／Visual撮影：未実施。

## 次の工程

第8工程へ**着手可能**とする。Pixel 3aの表示・操作確認、リリース候補ビルド、正式リリース判定を今回済ませたとは扱わない。第8工程は自動開始していない。

Windows CMDの「続行するには何かキーを押してください」は検証・共有成功後のpause。キーを押してこのCMDを終了してよい。検証CMDの再実行は不要。

`commit`は既存ランチャー、`implementation_commit`は固定ゲーム、`evidence_commit`は正式Windows証跡、`review_base_commit`は照合した証跡先端を示す。共有文書自身のSHAと区別する。今回の追加改善候補なし。
