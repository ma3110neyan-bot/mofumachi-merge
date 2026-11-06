# 第3工程：ユーザー生成だけ2P消費／Windows確認待ち

- branch：`feat/daily-world-pass-20261016`
- 状態：**修正済み・未確認**。実装・専用ランチャーは通常push済み。
- 確認対象実装commit：`b9e7f1a2e2384998878edbe97cba82885587e3dd`
- ランチャーcommit：`b4b93e8c58abc8377857c23a316f195654dffbef`
- 第2工程：ユーザーの最新指示により、`97be3fafe5d73fda055ae3eba34c4ff8c45f85b7` のWindows実Unity **68/68 Passed・合格済み**として扱う。再実装・再実行していない。今回Codexが旧XMLを新たに照合したという意味ではない。
- 目的：Tea／Bread／Flower Lv1のユーザー生成だけ2Pを消費し、追加と消費を同じ保存・rollback単位にする。

## 実装と変更ファイル

- `Assets/Mofumachi/Core/GameStateManager.cs`：`TryGenerateLv1` にユーザー生成を集約。種別・配置済み生成枠・残高を確認し、既存 `MutateEconomy → Mutate` のClone／CopyFrom／rollbackを使用。成功時Saveは1回。汎用 `AddItem` は初期配置・復元用として無料。
- `Assets/Mofumachi/Core/MofuPointService.cs`：既存消費APIと生成操作は同じ内部消費プリミティブを使用。残高の減算はここだけ。盤面追加に成功した後2P消費し、別Saveはしない。
- `Assets/Mofumachi/Presentation/UIFlowController.cs`：既存生成入口を上記APIへ接続し `Time.frameCount` を渡す。既存Feedbackで不足通知。表示枠・配置は変更しない。
- `Assets/Mofumachi/Tests/EditMode/GenerationCostTests.cs` と `.meta`：第3工程専用33ケース。
- `scripts/verify-generation-cost-windows.cmd` と `.ps1`：専用Windows検証と自動結果共有。第1・第2工程ランチャーは変更なし。
- `References/Review/latest/summary.md` と `status.json`：今回の状態。

同一フレームの重複入力と保存中の再入を防止。異なるフレームの操作、フレームを省略した独立Core呼び出しは各2P。同じフレームの保存失敗後も重複通知で自動再試行しない。入力記録は一時状態でSaveDataへ追加しない。

24→22、3回18、2→0をテスト対象に含めた。0／1P、盤面満杯、空きがMergeでロック中、生成枠未配置、無効種別では追加・消費・Saveを行わない。初期Tea Lv1×2と24P、汎用配置／復元、Merge、移動、納品、生成枠整理、Save／Load／Resetは無料のまま。

保存失敗時は盤面・残高・その他GameState・既存Mergeロックを復元。IO／アクセス拒否×3種別をテストへ追加。**実Unityでのrollback合格は未確認**。

**UI表示は次工程以降**。残高ラベル・盤面・ボタン・レイアウト変更なし。会話／散策／お花／日次等の報酬接続、依頼、街Lv、Save version 4／移行、Visual Lock、TownActor、フォント、BGM、正式画像は変更なし。

## 確認結果

- Core APIと新規テストコードの静的C#コンパイル：各1回、エラー0・警告0。Unityテスト合格の代わりにはしない。
- 対象：`Mofumachi.Tests.GenerationCostTests`、期待 **33ケース**。実Total／Passed／Failed／Skippedは**未取得（null）**。
- クラウドUnity0回、EditMode／PlayMode実行0回、自動再試行0回。全件、APK、Pixel、Visual撮影、フォント確認は未実施。
- 独立した読み取りレビュー1回。共有失敗後・共有専用経路でcustom `GIT_*` が元indexへ作用し得る指摘1件を、すべてのGit呼び出し直前で拒否する1回の修正で対策。その他重要指摘なし。
- 一時Git環境の共有確認3件合格：remote先行でも元HEAD／index／staged／未追跡／未pushコミットを保護、push拒否は1回で停止、custom index指定は書込前に拒否。旧inbox削除・証跡生バイト一致・共有済みreceiptで再pushしないことも確認。合成資料をGitHub実行証跡へ入れていない。
- Windows PowerShell 5.1互換性は構文／API／UTF-8 BOMを静的確認。PowerShell 7の共有確認はWindows実機確認の代わりにはしない。最初の引数確認はLinuxにC:ドライブがないため停止し、同じ確認を再実行していない。製品不具合とは扱わない。
- 最初のUnity／testエラー：**未実行のため未確認**。過去の `error.log` は過去runの履歴で、第3工程の失敗証拠ではない。

## Windowsの次の1操作

`scripts/verify-generation-cost-windows.cmd` を **1回ダブルクリック**。

- Windows 11／Windows PowerShell 5.1／Unity **6000.6.4f1**、既存ライセンス、Android target、EditModeの上記fixtureだけ。
- 実装commit `b9e7f1a2e2384998878edbe97cba82885587e3dd` の隔離worktreeを使用。元作業ツリー・ProjectSettings・Build Profileは変更しない。
- Unity最大1回、自動再試行0回。原本：`Builds/Diagnostics/generation-cost-<timestamp>-<id>/`。
- `results.xml`、`unity.log`、`source-revision.txt`、`generation-cost-result.json`、失敗時 `first-error.log`。33/33・Failed0・Skipped0・Inconclusive0・fixture/source一致・入力不変を必須とする。
- 同じ操作内で **`References/Review/inbox/latest-generation-cost/` へ自動commit・通常pushを1回**。remote先端を親にする共有専用worktree／indexで固定フォルダだけpublishし、無関係なローカルコミットは混ぜない。SHA・一覧を `run-info.json` へ記録。1ファイル4MiB／合計16MiBまで。大きすぎるunity.logはローカル保持し、XML／JSON／最初のエラー等を共有。
- 共有失敗時は理由・ローカル原本・`share-result.json` を保持して終了。reset／force push／stash操作なし。仮にCMDを再度開いても既存Unity試行は再実行せず、保存済み結果の共有だけを扱い、共有済みなら再pushしない。
- Noah/TownActor Visual Guardを今回のテスト前提にせず、Guard改変・無効化なし、Visual Lock原本変更なし。

## 次工程判定

**第4工程「街でポイントを獲得する処理」へ進んで安全か：NO**。

主要ブロッカーは1つ：**第3工程のWindows実Unity 33ケースの結果がまだない**。実装済みを関連テスト合格・工程完了とは扱わない。1回実行後に自動共有された原本をレビューする。第4工程へは進まない。

`status.json` のcommitは確認対象の実装を指し、共有文書だけのcommit自身とは区別する。
