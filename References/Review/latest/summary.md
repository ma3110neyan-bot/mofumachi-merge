# 第8工程：正式署名未準備のため実ビルド未着手

状態：**未着手（Windows release preflight／実AAB・APK）**。ビルド失敗とは判定していない。

- branch：`feat/daily-world-pass-20261016`。
- 確認対象／予定ビルドHEAD：`4a0c471fb21313ef31be83f1aeaac9476ecf8819`。実成果物のsource commitではない。
- 固定ゲーム：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- 第7工程complete=true、API-only compile PASS、限定release34/34 PASSを保持。今回再実行なし。

## 確定した主要ブロッカー

ユーザー回答で、**署名用4環境変数は未設定、release用keystore／alias／passwordも未準備**と確認した。秘密値は要求・記録していない。

現在の接続はクラウドで、Windows正式repo／Eドライブを直接操作できる接続はない。Windows実行や署名preflightを行ったと扱わず、既知の環境調査・ライセンス起動も繰り返していない。

入力条件が未充足なので、preflight専用実行／AAB／APKへ進まず停止。署名キーをクラウドで生成したり、debug署名で代用したりしていない。ゲーム・releaseビルダー・QA経路・設定・Visual Lockを変更していない。

## 今回の結果

| 項目 | 結果 |
| --- | --- |
| Windows release preflight | **未実行** |
| AAB build／exit／size／SHA256／signer SHA256 | **未実行／全て未生成・未確認** |
| APK build／exit／size／SHA256／signer SHA256 | **未実行／全て未生成・未確認** |
| AAB/APK signer一致 | 未確認 |
| Development OFF | release実装ではOFF。実ビルド設定は未確認 |
| source_unchanged | **true（クラウドの既存ソース確認）** |
| formal repo unchanged | **未確認（Windows現在状態を未観測）** |
| Visual Lock変更 | **NO** |
| Unity／実ビルド／テスト再実行 | **全0回** |
| 自動再試行 | **0回** |
| blocking issue | **正式署名未準備** |
| Pixel 3a実機確認へ進んで安全か | **NO** |
| 第8工程 complete | **false** |

実ログ・成果物がないため、架空のsigning-preflight.log／build.log／first-errorを生成していない。first_error／runner_errorはnull。rootの過去first-error.log・failure.log・latest/error.logは旧失敗原本として不変で、今回のエラーではない。

署名前提のユーザー回答は`windows-prerequisites.json`へ出典付きで保存。予定ソースと実ビルドのsourceを区別し、build-info.jsonのactual sourceはnull。新規AAB/APK／キー／実機画像をGitへ追加していない。

## 次に必要な作業

**Windowsローカルで正式署名用キーを準備する工程が必要。** `windows-signing-preparation.md`へ保管先・alias等の未実行案と、その後の既存release経路の実行順を記録した。

キー作成・Play App Signing／upload keyの採用・秘密値の設定は今回行っていない。keystoreやpasswordをチャット／Git／Diagnosticsへ渡さない。キー準備後、4環境変数をWindowsローカルに設定し、既存`build-android-release.ps1`のpreflight専用を1回。PASS時だけAAB1回、成功時のみAPK1回へ進む。

正式repoは`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`のまま。検証／Library／TEMP／cache／Diagnostics／成果物はE運用のまま。今回は実ディレクトリを操作していない。

静的検証・限定34チェックの完了記録は`validation/static-validation.json`と`validation/history-4a0c471/`へ保持。第7工程の記録も保持。

固定共有：`References/Review/inbox/latest-release-candidate/`。現在のcommit欄は確認対象HEADで、共有文書自身のcommitと区別する。

Codex／AI側から見た次の改善候補：今回はなし（必須の署名準備のみ）。

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
