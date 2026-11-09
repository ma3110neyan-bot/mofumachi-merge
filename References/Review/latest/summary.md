# 第8工程：Git探索最小修正・限定releaseテスト34/34合格

状態：**関連テスト合格**。release入口の静的・限定検証小工程は完了。第8工程全体は未完了。

- branch：`feat/daily-world-pass-20261016`。
- Git探索修正commit：`c86941dc65291867fe6400345c0bb97beb51035e`。
- 修正前HEAD：`b03a9814df64cd26f9feb770bde0a3bbefce3d41`。
- release入口追加commit：`ea503db12f34d0eaec418f4aaa507b2d4512d469`。
- 固定ゲーム：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- 第7工程complete=trueとv5の55/55・444/444を保持。再実行なし。

## 修正範囲

コード変更は`scripts/tests/android-release.tests.ps1`の**1ファイルだけ**。変更前49行目のGit探索を、最初のApplicationを1件選び、null／空パスを明確に拒否する形へ変更した。

```powershell
$GitCommand = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
if ($null -eq $GitCommand -or [string]::IsNullOrWhiteSpace($GitCommand.Source)) { throw 'Git Application was not found or has an empty executable path.' }
$Git = $GitCommand.Source
```

その他のテストロジック・PATH・fallback探索は変更なし。修正前の該当箇所は`validation/git-selection-fix/before-git-discovery.txt`、差分は`git-selection-only.diff`へ保存した。旧ファイルの生バイトからこの1箇所を置換した結果が現ファイルと完全一致することを確認。

コードの変更ファイル数1と、ユーザー指定の共有記録更新ファイル数は分けて扱う。

## 実行結果

`scripts/tests/android-release.tests.ps1`を**今回1回のみ**実行。

| 項目 | 結果 |
| --- | --- |
| exit code | **0** |
| Total | **34** |
| Passed | **34** |
| Failed | **0** |
| 自動再試行 | **0** |
| Git fixture初期化 | 成功 |
| release入口／AAB・APKの入口切替 | 合格 |
| Development OFFの宣言確認 | 合格 |
| 署名preflight | 合格（合成fixture） |
| 秘密値非露出 | 合格（合成値のredaction／引数／JSON確認） |
| source guard | 合格（実一時Git・CRLF・tracked/index/untracked判定） |
| QA経路保護 | 合格（別途、生バイトとSHA256照合） |

34はスクリプト内のCheck assertion数。Unity／NUnitケース数として数えていない。ログ`validation/git-selection-fix/release-tests.log`の末尾に全チェックPASSを確認した。LF→CRLF警告3件は一時fixtureでの改行確認時に出たもので、exit0・全チェック合格。

Gitが4件見つかる同じ環境で、選択件数1・SourceはSystem.String・選択先は/usr/local/bin/gitとなることも読み取り確認。PATHは変更していない。

API-onlyコンパイルはb03a981でPASS済み。C#本体とcompile.rspが不変なので、今回は再コンパイルしていない。

## 不変確認

`AndroidReleaseBuild.cs`と.meta、`build-android-release.ps1`、既存`AndroidBuild.cs`／`build-android.ps1`は修正前とSHA256まで一致。Assets／ProjectSettings／Packages／Visual Lockも不変。

**source_unchanged=true**。承認されたGit探索だけのテスト編集は別記し、既存ゲームやビルダーまで無変更と混同しない。

- Unity起動0、AAB/APKビルド0、Pixel操作0。
- 第7工程テスト再実行0。
- 実署名キーの使用0。Windowsの正式署名preflight自体は未実行。
- 今回のfirst_error／runner_error／failed_phaseは全てnull。新しいfirst-errorを生成していない。

root first-error.log／failure.log／latest/error.logはb03a981までの**旧失敗原本**として変更せず保持した。旧限定テストlogも保持し、今回の成功ログとはパスを分離。直前の結果JSON等を`validation/history-b03a981/`へ保存した。c988164／ba32883と第7工程の履歴も保持。

## 判定

**Git探索修正：YES。コード変更1ファイル。限定releaseテストPASS、exit0、Total34／Passed34／Failed0。**

**次に実AAB/APKビルドへ進んで安全か：YES（次のWindows工程への着手可）**。

Windows正式repo clean・既存のローカル正式署名preflight合格を実行前の必須条件とし、AABを1回、成功時だけAPKを1回とする。今回その操作を開始していない。実ビルド成功や署名・実機確認の合格を今回確定したものとは扱わない。

**第8工程 complete=false**。Pixel／保存復帰／Visual／Google Play提出準備の最終合格は未確認。Google Playへ提出していない。

固定共有：`References/Review/inbox/latest-release-candidate/`。commitは上記コード修正の確認対象SHAを示し、共有文書自身のcommitと区別する。

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
