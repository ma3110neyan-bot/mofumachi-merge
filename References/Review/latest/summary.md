# 第6工程：Windows実Unity 85/85 Passed／完了

- branch：`feat/daily-world-pass-20261016`
- 固定ゲーム実装commit：`68f0e5c479716029b31ff3e92da47335da25f957`
- Burst無効化版ランチャーcommit：`cceb6b5676d639a44f2763249715eace30777f49`
- 今回の確認対象・既存結果共有commit：`b39fa214343f3f39202f9a1d9a5850deb0449993`
- 状態：**関連テスト合格**。**第6工程 complete=true**。
- 今回の目的：既に実行された修正版Windows結果だけを照合し、第6工程の合否を確定すること。

## 既存Windows結果

対象run：`quest-town-level-burst-disabled-v1-20261107-134513-1c0ec2fb`。

`References/Review/inbox/latest-quest-town-level/` のJSON／results.xml／source-revision／run-info／unity-arguments／unity.logを照合した。新runのfirst-error.logは存在せず、JSONのfirst_error／runner_errorはnull。

| 確認項目 | 確認結果 |
| --- | --- |
| source commit | `68f0e5c479716029b31ff3e92da47335da25f957` と一致 |
| Unity | Windows実Unity `6000.6.4f1` |
| 実行範囲 | Android target／EditMode／`Mofumachi.Tests.QuestTownLevelTests` のみ |
| 修正版識別子 | `burst-disabled-v1` |
| Burst引数 | 保存済み起動引数に `--burst-disable-compilation` が1個 |
| Total / Passed | **85 / 85** |
| Failed / Skipped / Inconclusive | **0 / 0 / 0** |
| XMLケース | 85件すべて指定fixture、名前重複なし、全件Passed |
| Unity exit code | **0** |
| このWindows runのUnity起動 | **1回** |
| 自動再試行 | **0回** |
| source_unchanged | **true** |
| validation_passed | **true** |
| first_error | **null** |

manifest記載の5ファイルすべての生バイト長・SHA256が一致。source-revision.txtも固定製品commitと一致する。新ログに以前のCrash／Burst起動失敗やCSコンパイルエラー等の重要シグネチャを検出していない。Burst無効化版でこのrunが完走した事実を記録し、以前のgRPC切断そのものが一次原因だったと新たに断定しない。

## 旧失敗証跡の保持

旧run `quest-town-level-20261107-131556-0df6b34b` は **`References/Review/inbox/latest-quest-town-level/previous-runs/quest-town-level-20261107-131556-0df6b34b/`** に保存されている。旧JSON・source・first-error・Unity引数・Unity logの5原本の長さ／SHA256が保存前の記録と一致した。

旧runのexit -2147483645／XML未生成／85件未実行という失敗記録は変更していない。旧runのfirst-errorを新runへ混在させていない。`latest/error.log` はこの旧失敗の履歴として無変更で保持し、その旧source_logの参照先は上記archiveのunity.logとなる。

## 今回の作業と範囲

既存の公開ランチャーと新結果を確認し、共有commitをfast-forwardで取り込み、**summary.md／status.jsonだけ**を更新した。

ゲーム再実装なし、テスト内容・85件の変更なし、ランチャー再変更なし。固定実装commitからのAssets／Packages／ProjectSettings／Visual Lock差分なし。Core／Presentation／TownActor／Guard／フォント／メモリ設定／前工程ランチャー等は変更していない。

**このCodex確認作業ではUnity起動0回、テスト実行0回、自動再試行0回、Fetch1回**。利用したのは既存Windows新runの結果のみ。全件／PlayMode／APK／Pixel／Visual撮影は行っていない。

## 判定と次の作業

**第6工程 complete=true**。

**第7工程「進行UI表示＋全体結合確認」へ進んで安全か：YES**。

理由：指定製品・fixtureのWindows実Unity85ケースが全件Passedで、exit0／skip・inconclusive0／source不変／起動1回・再試行0を満たし、旧証跡も保持されているため。

原本JSONのstage7_safe=false／review_required=trueは、ランチャーがレビュー待ちとして出力する既定値。その原本を変更せず、今回の照合後の判定だけをlatest/status.jsonへYESとして記録した。

この合格は第6工程の直接関連EditMode範囲。ゲーム全体結合・UI表示・APK・Pixel・リリース合格とは扱わない。**第7工程へ自動で進まず、ここで停止する。**

summary/statusのcommitは今回確認した既存Windows結果共有commitを示し、ゲーム実装・ランチャー・共有文書自身のcommitとは区別する。
