# 第8工程：Windows release preflight実行待ち

署名準備は完了済み。同じPowerShellセッション内で、既存releaseスクリプトをBuild指定なしで1回だけ実行する指示を受領した。CodexからそのWindowsセッションを直接操作できないため、まだpreflightを実行したとは扱わない。

実行ブロック：`References/Review/inbox/latest-release-candidate/windows-release-preflight-only.md`。

コマンドは構文確認だけ実施。4環境変数の有無とkeystore SHAを確認し、既存スクリプトを1回呼ぶ。終了後にcertificate等を照合し、公開ファイルだけのZIPをEへ作成する。ユーザーはそのZIPだけを共有する。秘密鍵／passwordは含めない。

現在preflight結果・exit codeは未取得。Unity／実ビルド／テスト再実行／キー再生成は0。AAB実ビルドへ進める判定はNO（preflight結果待ち）。第8工程complete=false。成功・失敗とも、保存結果を受領してから固定共有先へ更新する。

---

# 第8工程：Windows正式署名準備完了・preflight未実行

状態：**完了（署名準備の小工程のみ）**。第8工程全体は未完了。

- branch：`feat/daily-world-pass-20261016`。
- Windows公開署名状態commit：`8c8579026b43359f240ade43fd3eaf863f354d9c`。
- 署名補助実装：`c486b37df3a7c17acfbd852e2b1a4f2d5d95bf77`。
- 固定ゲーム：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- 第7工程complete=true、release API compile PASS／34チェックPASS、署名補助26チェックPASSの以前の証拠は保持。再実行なし。

## 公開7項目の照合

GitHub共有の`signing-status.json`を取得し、以下の7項目だけであること、2つのSHAが提示されたWindows成功画面と完全一致することを確認した。原本JSONは変更していない。

| 許可された記録項目 | 共有結果 |
| --- | --- |
| keystore_prepared | **true** |
| alias_configured | **true** |
| env_vars_configured | **true** |
| certificate_sha256 | `96bb1402b8085570305a0486ae3f7fa3fafe2c60a3b8cd062c2ee8e0f81b8e09` |
| keystore_sha256 | `faac2959a5e9ae6e5e531c110955a2085502d13da7cc96148d91066ad666b4cb` |
| backup_pending | **true** |
| secrets_recorded | **false** |

共有commitの親は2f29b07。変更pathはこの公開JSON1個のみ。秘密鍵本体・password・source変更を含めていない。Codexはprivate keystoreを取得・開封していない。

実作成のpath／alias名／sizeはユーザー画面で確認し、指定どおり実署名記録としてGitへ追加するのは上記7項目に限定した。パスワードのマスク入力数や長さも記録していない。

## 次へ進める範囲

**Windows release preflightへ進んで安全か：YES（同じPowerShellセッションでのpreflight専用確認まで）**。

4環境変数は成功時点のProcess scopeだけ。画面を開いたまま保持し、キー作成CMDを再実行しない。閉じた場合は新しいキーを作らず、既存キーに対する環境変数の再設定を別途行う。

バックアップは未実施のまま。外部媒体／クラウドへのキーコピーは行っていない。backup_pending=trueを保持する。

今回は公開結果の照合だけで、release preflight／AAB／APK／Pixelは未実行。次の実ビルドsource HEAD、ビルド後のsource不変、正式署名による成果物、実機合格はまだ確定しない。公開署名状態のcommitと、将来の実ビルドsource commitを混同しない。

**第8工程 complete=false**。preflight以降へ自動で進んでいない。

## 今回のレビュー操作

Fetch1回で共有commitを取得。公開JSON1pathだけのincoming差分を確認後、証跡だけをfast-forward同期した。キー作成・キー検証・password入力・テスト・Unity・ビルドは全て0回。自動再試行0。ゲーム／既存release・QAビルダー／設定／Visual Lockは変更なし。

更新前の準備待ち記録は`validation/history-2f29b07/`へ保存。第7工程・34チェック・以前のfailure原本も保持。今回のfirst_error／runner_errorはnull。古いfailure.logやlatest/error.logは今回の署名失敗を示さない。

固定共有：`References/Review/inbox/latest-release-candidate/`。commit欄は今回レビューした公開証跡SHAを指し、レビュー文書自身のSHAとは区別する。

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
