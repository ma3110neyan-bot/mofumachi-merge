# 第6工程：Burst無効化版ランチャー公開／Windows実行待ち

- branch：`feat/daily-world-pass-20261016`
- 今回の確認対象・公開済みランチャーcommit：`cceb6b5676d639a44f2763249715eace30777f49`
- 固定するゲーム実装commit：`68f0e5c479716029b31ff3e92da47335da25f957`（再実装なし）
- 状態：**修正済み・未確認**。ランチャーの準備は完了、修正版のWindows実Unity確認は未実施。
- **第6工程 complete=false**。

## 目的と今回の変更

変更した実行ファイルは `scripts/verify-quest-town-level-windows.ps1` のみ。Unity引数へ正式な `--burst-disable-compilation` を1個追加し、純粋なEditModeテストで不要なBurstコンパイルを起動させない。

修正版の識別子は **`burst-disabled-v1`**。ゲームcommit＋識別子の新しい `.attempted` をCreateNewで記録し、旧receipt／旧Diagnosticsは変更・削除しない。runと隔離worktree名も識別子で分離。修正版のUnity起動は最大1回、自動再試行0回。新receiptがある場合は保存済み結果の共有だけとなり、Unityを再起動しない。

既存CMD、Core、Presentation、GameState、QuestCatalog、TownGrowth、85テストの内容と件数、前工程ランチャー、Unity版、メモリ設定、packages、Guard／Visual Lockはすべて無変更。UI／フォント／全件／PlayMode／APK／Pixel／Visual撮影なし。

## 使用した既存失敗runだけの確認

対象：`quest-town-level-20261107-131556-0df6b34b`、共有commit `7a4cbeb109083d637c6c01315f38f91f6fe7a934`。同一runのJSON、manifest、source、Unity引数、ログ、first-errorを読み取り。manifestの生バイト長とSHA256は5ファイル一致、原本は無変更。

- source commit一致、Unity起動1回、exit **-2147483645**、自動再試行0。
- results.xml未生成、**85ケース未実行**。Total／Passed／Failed等はnullであり、Failed85やPassed0という実テスト集計に置換しない。
- unity.log **7933行目 `Crash!!!`**、7989行目Windowsのメモリリソース不足メッセージ、8404行目 `Unexpected error in Burst compilation` とgRPC切断を確認。
- **Unity Editor起動中クラッシュで、製品テストのassert失敗ではない**。製品ファイル／行番号は該当なし。
- 自動抽出first-errorは未確定の文面のまま保持。ログ上はCrash→LoadModule／メモリメッセージ→Burst/gRPCの順であり、末尾のgRPC切断自体をクラッシュの一次原因と断定しない。Burst無効化は今回指定された検証入口の修正で、解消したかは次のWindows実行で確認する。
- 旧JSONの `source_unchanged=false` は後段チェック未到達のままの値。製品コードが変わった証拠と扱わない。

最小の実ログ抜粋は同じrun名を明記して `error.log` に更新。旧run原本のJSONやfirst-errorを書き換えてはいない。

## 証跡共有の保持

新runの自動共有先は従来どおり **`References/Review/inbox/latest-quest-town-level/`**。次の共有時、現在の旧run原本を `previous-runs/quest-town-level-20261107-131556-0df6b34b/` へ生バイトで保存・SHA確認してから、新runをlatest直下へ配置する。既存archiveも維持し、古いXML／エラーを新runへ混在させない。

共有はremote先端を親とした別worktreeでこのinboxだけcommit、通常push1回。元作業ツリー・index・未pushコミットを混ぜず、push失敗は再試行せず結果をローカルへ保持。

## 今回の確認範囲

PowerShell構文・UTF8 BOM・5.1で使用できる構文/APIの静的確認、引数1個追加／fixture固定／1回起動箇所／receipt分離を確認。一時Git環境の合成3条件（新Passed時の旧証跡保存、新Failed時の旧XML除外、push拒否1回停止）は**3/3成功**。これはUnityの85ケース実行証跡ではない。

一時確認ハーネスの最初の呼出しだけ、LinuxにC:ドライブがないためJoin-Pathで停止。Unity／共有処理の前であり、ハーネスのパスだけ/tmpへ修正して確認した。ゲームコード・Windowsランチャーの不具合やUnity再試行には数えない。

独立した読み取りレビュー1回、Critical0／Important0。軽微な留保：push成功後のローカル共有成功receiptが失われた場合、同一runの再共有は既存ファイル検出で安全停止する。Unityは再起動されず証跡も保持されるが、receiptの自動復旧はしない。今回この追加改善は行わない。

**今回のUnity起動0回・実EditMode実行0回・自動再試行0回**。Windows環境でのみ修正版を実行する。クラウドのライセンス／描画環境再調査は行っていない。

## Windowsでの次の操作と判定

最新版PS1を取得したうえで、既存 **`scripts/verify-quest-town-level-windows.cmd` を1回ダブルクリック**。

Unity6000.6.4f1、Android target、`Mofumachi.Tests.QuestTownLevelTests` のEditMode85ケースだけ。製品commitは上記の固定値。新runは `Builds/Diagnostics/quest-town-level-burst-disabled-v1-<timestamp>-<id>/`。新receiptは `quest-town-level-68f0e5c479716029b31ff3e92da47335da25f957-burst-disabled-v1.attempted`。旧run／旧receiptを消して実行しない。

合格条件：Total85／Passed85／Failed0／Skipped0／Inconclusive0／exit0／Unity起動1／自動再試行0／source_unchanged=true／validation_passed=true／first_error=null。XMLの各ケースのfixture・重複なしも既存判定のまま。失敗時は最初のエラーと新runを保持して停止する。

**第7工程「進行UI表示＋全体結合確認」へ進んで安全か：NO**。

主要ブロッカーは**Burst無効化版でのWindows実Unity85ケース合格が未確認**の1点。第7工程へ進まない。

summary/statusのcommitは今回のPS1作業commitを表し、固定する製品commitや共有文書自身のcommitとは区別する。
