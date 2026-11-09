# 第8工程：API参照競合は解消・限定releaseテストで停止

状態：**原因確定**。参照修正とC#コンパイルは成功。本小工程の「限定テストPASS」は未達のため、実AAB/APKビルドはまだ実行しない。

- branch：`feat/daily-world-pass-20261016`。
- 確認対象HEAD：`ba328838d1ca72ec5abc0ba8b5f259ca445070a0`。
- release入口追加commit：`ea503db12f34d0eaec418f4aaa507b2d4512d469`。
- 固定ゲーム：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- 第7工程はcomplete=trueを維持。正式v5の55/55・444/444は再実行していない。

## 参照構成の最小修正

Unity6000.6.4f1の実DLLをCecilで読み、必要なEditor型11種が両DLLで実際に定義されることを確認した。Unityを起動せず、メタデータだけを読んだ。

`Managed/UnityEditor.dll`に全11型があるため、`validation/compile.rsp`から`Managed/UnityEngine/UnityEditor.CoreModule.dll`の参照1行だけを削除。その他の参照・オプション・対象ソースは変更なし。根拠は`validation/editor-type-definitions.json`、厳密な1行差分は`validation/reference-only.diff`。

API-onlyコンパイルは**今回1回、exit0、PASS**。CS0433なし、最初のC#エラーなし。`AndroidReleaseBuild.cs`は変更せず型解決・コンパイルに成功。出力DLLのサイズ・SHAをstatic-validation.jsonへ記録し、DLL自体をGitへ追加していない。

既存のコンパイラオプション警告CS2023（rsp内の/noconfigを無視）は残るが、ソースエラーではなくexit0。参照だけの変更範囲を守り、今回そのオプションを変更していない。

## 限定テストの最初の失敗

コンパイル成功後、既存`scripts/tests/android-release.tests.ps1`を**今回1回**実行し、exit1で停止。

```text
&: /workspace/mofumachi-merge/scripts/tests/android-release.tests.ps1:52
Line |
  52 |  & $Git -C $Repo init --quiet
     |    ~~~~
     | The term '/usr/local/bin/git /usr/bin/git /bin/git
     | /opt/codex/runtimes/codex-primary-runtime/dependencies/bin/fallback/git'
     | is not recognized as a name of a cmdlet, function, script file, or
     | executable program. Check the spelling of the name, or if a path was
     | included, verify that the path is correct and try again.
```

根本箇所は51行目の`$Git = (Get-Command git -CommandType Application).Source`。この環境ではGitが4件見つかり、SourceはSystem.Object[]になる。52行目でその配列を1つの実行ファイルとして起動しようとして失敗した。

読み取り確認で、`/usr/local/bin/git`、`/usr/bin/git`、`/bin/git`、fallback/gitの4件を確認。記録は`validation/git-discovery.json`。Git fixtureの初期化に未到達で、releaseビルダーやゲーム実行の失敗とは判定していない。チェック完走・PASSにはしていない。

今回の変更対象は参照構成だけなので、テスト／PowerShellビルダーを修正せず、PATHを変えた再実行も行わず停止した。自動再試行0。

## ソースと実行範囲

- `AndroidReleaseBuild.cs`と.meta、`build-android-release.ps1`、`android-release.tests.ps1`はba32883から生バイト不変。
- 既存`AndroidBuild.cs`と`build-android.ps1`も不変。
- ゲーム・Packages・ProjectSettings・Visual Lockは不変。`source_unchanged=true`。
- Unity起動0、実AAB/APKビルド0、Pixel操作0、第7工程の再実行0。
- 実署名キー・パスワードは使用していない。限定チェック内の署名情報は合成fixtureのみ。

現在のfirst-error.log／failure.log／latest/error.logはこの限定テストの最初のエラー。前回のCS0433ログ・rsp・結果・first-errorは`validation/history-ba32883/`へ同じバイトで保持。以前のc988164 preflight履歴も保持。

## 最終判定

| 項目 | 結果 |
| --- | --- |
| 参照競合修正 | **YES** |
| API-only compile | **PASS、exit0** |
| release限定テスト | **FAIL、exit1** |
| releaseビルダー本体変更 | NO |
| QA経路変更 | NO |
| ゲーム変更 | NO |
| Visual Lock変更 | NO |
| source_unchanged | **true** |
| 実AAB/APK build | NO |
| 次に実AAB/APKビルドへ進んで安全か | **NO** |
| 第8工程 complete | **false** |

主要ブロッカーは限定テストのGit探索が配列を返すこと。次の最小候補は**テスト補助のGit探索を1実行ファイルへ明示的に限定すること**。今回の参照修正範囲外なので実施していない。ゲームやreleaseビルダーの変更は必要と判断していない。

固定共有：`References/Review/inbox/latest-release-candidate/`。この文書のcommitは共有結果で、上記確認対象HEADや既存release実装commitとは区別する。

Codex／AI側から見た次の改善候補：今回はなし（上記の必須検証ブロッカーのみ）。

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
