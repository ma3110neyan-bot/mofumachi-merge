# 第7工程：Eドライブv2ランチャー公開／Windows実行待ち

- branch：`feat/daily-world-pass-20261016`
- 今回のPS1修正commit：`fea87beb5670d28c8dc3c1efa87b3bd4dee98aa0`（通常push済み）
- 固定ゲーム実装：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。ゲーム・テスト内容・件数は無変更。
- 状態：**修正済み・未確認**。**第7工程 complete=false**。
- 目的：v1の未完了attemptを残し、明示許可されたv2を1回実行できる入口とする。

## 既知の未完了状態と証拠範囲

ユーザーはE-v1で「Existing attempt: saved results only; neither Unity phase will restart.」に続き、terminal JSONが見つからない状態を確認した。報告例：

    E:\Mofumachi-Validation\Diagnostics\progress-ui-integration-progress-ui-e-drive-v1-20261107-181802-bbad51c4\progress-ui-integration-result.json

receipt存在・terminal JSON欠損という状態は、保存済みterminal resultではなく **pre-Unity/incomplete attempt** と扱う。今回そのJSONが作られなかった終了／書込原因自体は**未確認**。ファイル欠損だけから以前のUnity起動数を0と断定せず、旧E-v1のnative状態は未確認とする。55件／444件とも合格証拠はない。Windows E原本はクラウドへ未共有であり、ユーザー報告と実ログを混同しない。

旧C容量不足run `progress-ui-integration-20261107-174541-8e831d09` の共有原本5ファイルはSHA不変で保持している。旧E-v1 receipt／Diagnostics、旧Cの途中worktree／receiptを削除・上書きしていない。

## 専用PS1だけの修正

1. 識別子を **`progress-ui-e-drive-v2`** とし、新しいE receipt／run／projectsを使用。旧v1のreceiptを解除・更新しない。
2. 既存receiptの先にJSONがない、壊れている、Preparing／Running等の非terminal、別identityの場合は、保存済み結果のメッセージや共有処理へ進まない。
3. **`E:\Mofumachi-Validation\attempt-stops\incomplete-attempt-<guid>.json`** をCreateNewで別記録し停止。元run／receipt／結果ファイルは修復も変更もしない。記録はnative結果ではなく、native_state=unconfirmed、今回の検出によるUnity起動0、再試行0を明記する。
4. 正式なPassed／Failed／Blocked、製品SHA・識別子・validationフラグが整合するJSONだけを既存の保存結果共有へ渡す。未完了時はGit／checkout／Unity／自動再試行へ進まない。
5. v2新規時は、重いcheckoutとreceipt作成の前に**Preparingの非terminal JSON**を保存する。これを成功結果と扱わない。
6. 旧v1 receiptがあれば、path／SHA／run／結果状態を**読み取りだけ**で観測し、v2 JSONの`previous_e_v1_attempt`へ保存する。早いpreflight失敗時にも履歴を残す。通常のv2結果共有にこの観測情報も含まれる。

変更した実行ファイルは **`scripts/verify-progress-ui-integration-windows.ps1` だけ**。CMD、Core／Presentation／Save／UI、テスト・fixture件数、Unity6000.6.4f1、package、Visual Lock／Guard／TownActor、前工程ランチャーは変更していない。

## 保存先と実行条件

引き続き **`E:\Mofumachi-Validation\`**。direct／integration別のworktreeとLibrary、一時ファイル、子プロセスUPM cache、Diagnostics原本、共有限定worktreeはE配下。元C作業ツリー・index・他の変更を混ぜない方式を維持する。

| フェーズ | 対象 | 必須条件 | v2実結果 |
| --- | --- | --- | --- |
| Phase1 | ProgressUiIntegrationTests | Total55／Passed55、Failed・Skipped・Inconclusive0、exit0、source_unchanged=true | **未実行** |
| Phase2 | 正式9fixture | Total444／Passed444、Failed・Skipped・Inconclusive0、exit0、source_unchanged=true | **未実行** |

Phase1の合格とsource不変が確認できた場合だけPhase2へ進む。Unity合計最大2回、自動再試行0、両方 **`--burst-disable-compilation`**、Unity6000.6.4f1／Android target／EditModeのみ。

9fixtureはSaveServiceTests33、PreferencesTests23、CharacterPreferenceTests7、MofuPointServiceTests68、GenerationCostTests33、TownPointRewardTests69、RecoveryExtraHelpTests71、QuestTownLevelTests85、ProgressUiIntegrationTests55。その件数表・引数生成・XML合格判定は前版から文字単位で不変。

成功・失敗とも通常の小容量証跡を **`References/Review/inbox/latest-progress-ui-integration/`** へ同じ操作で自動共有。旧C証跡はprevious-runsへ保持し、旧E-v1の原本を残した読み取り観測もv2 JSONに含める。未完了の別停止記録を架空のterminal結果へ変換して共有しない。

## 確認結果

一時データでmissing／corrupt／6状態／identity／境界／v1-v2分離と旧バイト保持／履歴観測／別停止記録を確認。実際の既存receipt分岐を別PowerShellプロセスで実行し、missing JSONならGit・共有呼出し前にexit1で停止することを確認した。全て合成データで、実Unity合格証跡にはしていない。

独立読み取りレビュー1回、Critical／Importantなし。軽微な履歴観測タイミングをpreflight前へ移動し、早期停止時の履歴漏れも防いだ。PowerShell構文・UTF8 BOM・5.1対応APIを静的確認。Windows5.1での実行は未確認。

**今回のUnity起動0、実テスト0、自動再試行0、Fetch1回**。全件／PlayMode／APK／Pixel／Visual撮影なし。新しい実runや結果を捏造していない。

## 次の操作と判定

最新版PS1を取得後、既存 **`scripts/verify-progress-ui-integration-windows.cmd` を今回のv2として1回だけダブルクリック**。

今回だけ明示許可された新識別子の実行であり、失敗・未完了時にv3作成やreceipt削除で自動継続しない。

**第7工程 complete=false**。

**第8工程「実機Pixel 3a確認＋リリース候補ビルド」へ進んで安全か：NO**。

主要ブロッカーは**v2のWindows Phase1・Phase2合格結果が未確認**の1点。ここで停止し、第8工程へ進まない。

summary/statusのcommitは今回のPS1作業を示し、固定ゲーム実装・共有文書自身のcommitとは区別する。
