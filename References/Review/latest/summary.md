# 第3工程：既存Windows実行結果確認／33件合格

- branch：`feat/daily-world-pass-20261016`
- 確認対象実装commit：`b9e7f1a2e2384998878edbe97cba82885587e3dd`
- 既存Windows証跡のGitHub commit：`3eb8f848bee29bac66062a1c74a02645adadded7`
- 対象run：`generation-cost-20261106-223046-7a38c6f6` の1件だけ。
- 状態：**関連テスト合格**。**第3工程 complete=true**。
- 今回の目的：保存済みのWindows実行結果を読み取り、第3工程と第4工程への判定を確定する。
- 実施：共有済みJSON／XML／source-revision／run-info／unity.log／起動引数を照合。再実装・Unity再起動・テスト再実行・新規run生成はすべて0回。
- 成功／失敗：**成功（第3工程の直接関連EditMode検証）**。
- 根本原因／修正内容：対象テストの失敗なし。ゲームコード・ランチャー・元証跡の修正なし。今回の変更はこのsummaryとstatusだけ。

## 同一runの原本照合

固定共有先：`References/Review/inbox/latest-generation-cost/`。

`generation-cost-result.json`、`results.xml`、`source-revision.txt`、`unity.log`、`unity-arguments.txt` の5ファイルについて、`run-info.json` 記載の**SHA256と生バイト長がすべて一致**。共有原本は編集していない。過去runや合成データは混ぜていない。

- 実装SHA：JSON／source-revision／manifestの3者とも `b9e7f1a2e2384998878edbe97cba82885587e3dd`。
- fixture：`Mofumachi.Tests.GenerationCostTests`。実XMLの33件すべてがこのfixture、重複0、各caseがPassed。
- 実結果：**Total33／Passed33／Failed0／Skipped0／Inconclusive0**。XML親の発見可能テスト数267等は全件実行数として扱わず、実case33件と実行totalを照合した。
- Windows 11／Unity **6000.6.4f1**、Android target、**EditMode**。起動引数・Editorログとも一致。
- Unity起動1回、終了コード **0**、自動再試行 **0**。
- `validation_passed=true`、`source_unchanged=true`、`first_error=null`、`runner_error=null`。
- `first-error.log` は存在しない。ログに `Test run completed. Exiting with code 0 (Ok). Run completed.` を確認。CSコンパイルエラーなし。
- XMLの実行時刻：2026-11-06 13:37:15 UTC。共有日時：2026-11-06 13:37:24 UTC。

## テストケースから確認した仕様

| 確認仕様 | 実XML対応テスト | ケース数 | 結果 |
| --- | --- | ---: | --- |
| Tea／Bread／Flower Lv1生成で24→22、追加1個・Save1回／3回生成で18P | UserGenerationAddsExactlyOneLv1AndChargesTwoOnce、ThreeIndependentOperationsChargeTwoPerItem | 4 | Passed |
| 2P→0P、1P・0Pでは盤面／残高不変・Saveなし | BoundaryBalanceNeverOverdrawsOrSavesInsufficientRequest | 3 | Passed |
| 満杯・ロックされた空きでは生成／消費／Saveなし | FullBoardDoesNotConsumeOrSave、LockedVacancyIsNotAUsableSpaceAndNeverCostsPoints | 2 | Passed |
| 不正種別・生成枠未配置で無変更 | UnknownGenerationKindIsRejectedWithoutSave、PackedFrameCannotGenerateOrCharge | 6 | Passed |
| 初期Tea Lv1×2は無料、開始24P | InitialTwoTeaAreFreeAndNewBalanceRemains24 | 1 | Passed |
| 0Pでも移動／Merge／整理／納品が無料、汎用配置／復元も無料 | ZeroBalanceStillAllowsMoveMergeAndFrameManagement、DeliveryAndQuestCompletionAtZeroRemainFree、GenericSetupAndRestoreItemAdditionRemainsFree | 5 | Passed |
| 保存失敗で追加アイテム・2P・その他状態・既存ロックを復元（IO／アクセス拒否×3種別） | SaveFailureRestoresItemWalletWholeStateAndPriorLocks | 6 | Passed |
| 同一フレーム／保存中再入の二重消費防止、異なる操作は各2P、失敗の重複通知で再試行しない | DuplicateFrameOperationAddsAndChargesOnlyOnce、DistinctFramesAreDistinctUserOperations、ReentrantGenerationDuringSaveCannotChargeAgain、DuplicateFrameDoesNotAutomaticallyRetryFailedSave | 4 | Passed |
| Save／Load／新規Resetで生成料金なし | SaveLoadAndNewGameResetDoNotChargeGenerationFees | 1 | Passed |
| 既存ポイント消費APIは共通プリミティブを使い、不正消費で保存しない | ExistingSpendApiUsesTheSamePrimitiveAndSavesOnlyValidSpending | 1 | Passed |
| **合計** | **第3工程fixtureのみ** | **33** | **Passed** |

UI表示は次工程以降。今回UI・画像・フォント・BGM・Save version／移行・報酬接続・依頼・街Lv条件・Visual Lock・TownActorを変更していない。全件・PlayMode・APK・Pixel・Visual撮影は実施していない。

## Editorログの付記事項

`first_error=null` はランチャー／対象テストの結果。Editorログには以下の非停止メッセージがあるため、ログ全体が無エラーだったとは扱わない。

- `unity.log:96`：`[Licensing::Module] Error: Access token is unavailable; failed to update`。続く97～103行でentitlement解決・ライセンス更新成功、その後テスト完走。
- `unity.log:7913`：`Library/PackageCache/com.unity.2d.tooling@4e93af4036ee/.../SpriteAtlasTextureSpaceUsedIssueSettings.uss` のDirectoryNotFoundException。後に関連uxmlのimportメッセージもある。ゲームコード起因とは確定していない。対象33ケースは合格しexit0。今回この周辺の調査拡大・修正・再実行はしない。

対象テスト失敗はないため `error.log` は更新しない。現存する同ファイルは**旧SaveData第1工程のGuard不一致履歴**であり、今回の失敗として扱わない。

## 第4工程判定

**第4工程「街でポイントを獲得する処理」へ進んで安全か：YES**。

理由：指定実装・fixture一致、33/33、失敗／skip／inconclusive0、exit0、入力不変、原本SHA一致、再試行0をすべて確認。生成2P消費・無料操作・不足／満杯時不変・保存失敗rollback・二重消費防止の対象ケースが合格した。

原本JSONの `stage4_safe=false`／`review_required=true` は結果レビュー前の初期値として保持し、**今回レビュー後のYESをsummary/statusへ記録**。原本JSONは書き換えない。

合格範囲は第3工程の内部生成処理と直接関連EditModeテスト。全ゲーム／実機リリース／Visualの合格とは扱わない。次は第4工程の明示指示を待つ。**今回、第4工程の実装へは進まない。再実行も不要。**

`status.json` のcommitは確認対象実装、evidence_commitはWindows証跡の共有commitを示し、今回の文書更新commit自身とは区別する。
