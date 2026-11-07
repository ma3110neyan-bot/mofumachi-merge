# 第5工程：日次回復・追加散策・おたすけ散歩／Windows確認待ち

- branch：`feat/daily-world-pass-20261016`
- 実装commit：`0c22f30fae41c18471fe771f67153e4828716629`
- 専用ランチャーcommit：`d83118f93af788c0028f6fe747cd7195193b0f0b`
- 公開先端：`150ebebae9e46b42ba536b95a41fc5a07f58cf26`。実装・ランチャーは通常push済み。
- 状態：**修正済み・未確認**。第5工程complete=false、Windows実行待ち。
- 第4工程：ユーザーの最新指示どおり実装 `9433202b7dd6f772a2e3c86abd6395b2343b3231` のWindows実Unity **69/69 Passed・合格済み**として扱う。再実装・再実行・再検証していない。GitHubに追加された既存共有commit `83d11f57127dfc137a58425286da2c60760685b3` は原本のまま履歴へ取り込んだ。
- 目的：指定の3報酬だけを接続し、無限回復を防ぐ。12依頼・街Lv条件へは進まない。

## 報酬と台帳

| 報酬 | 財布への額面 | 制限 | 街行動累計 | 同時に保存する台帳 |
| --- | ---: | --- | --- | --- |
| 日次回復 | 16P | 当日1回 | 加算なし | lastRecoveryDay |
| 追加散策 | 2P | 当日2回、通常枠と独立 | 加算なし | additionalWalkRewards＋Additionalのwalk receipt |
| おたすけ散歩 | 4P | 当日1回、全前提必須 | 加算なし | helpWalkClaimed＋Helpのwalk receipt |

全て既存 `GameStateManager.MofuPoints / MofuPointService` を使用。財布は96Pで止める。既存の通常報酬と同じ方針で、満タンの回復・追加散策も正当な取得なら台帳を消費する。おたすけは残高2P以上で対象外のため、満タン時には台帳を消費しない。

おたすけは **残高0／1P AND 当日会話3名 AND 通常散策2回 AND 当日花取得 AND 当日回復取得 AND おたすけ未取得** が全て必要。追加散策を使い切る条件は追加していない。取得後に生成で0／1Pへ戻ってもhelpWalkClaimedが残るため再取得不可。日次1回・追加2回も台帳で止める。

通常会話4P・通常散策6P・花8Pは従来どおり額面を累計へ加算。今回の3報酬は共通Reward処理へ `addTownAction:false` を渡し、累計がlong.MaxValueでも増減せず受領できる。所持上限と累計を混同しない。初期24Pは変更せず、当日の16Pは別取引。初回の当日回復が成功すれば24→40Pとなる。

## 実際の入口と保存

- 回復：起動、実際のバックグラウンドからの復帰、「今日」メニュー、明示的な通常行動の後に既存日付と照合する。描画・EnsureDay・コンストラクタ・タイマーでは付与しない。未起動日数分の一括付与なし。
- 起動直後にUnityが通知するOnApplicationPause(false)は、前のappPausedがfalseなら復帰扱いにしない。失敗した起動回復の自動再試行を防ぐ。
- 直前の主保存またはjournal保存が失敗した場合、後続の回復保存を行わない。次の明示的なユーザー操作から未取得を改めて処理する入口は残す。無料会話・散策をロックしない。
- 散策開始時に既存の一時ID／開始日と一緒に種別を固定。**通常枠が残る→通常、条件成立→おたすけ、追加枠が残る→追加、それ以外は無報酬**。同じドラッグ中に再選択しない。追加API自体の回数は通常枠と独立。
- 完了時にも全条件を再確認。おたすけの残高等が変わって不成立になっても追加へ自動で付け替えない。同一IDは全種別で共通消込し、通常と追加などへの二重利用を拒否。receiptは通常2＋追加2＋おたすけ1の最大5件／日。
- 既存の主人公の地区越境完了だけが対象。開始日と完了日が違う券・取消・復元・FastTravel・NPCは従来の扱いを維持。移動経路・速度・描画は不変。
- 取得資格・日付・上限・重複を全て変更前に判定。成功時だけ財布・適用日・台帳を既存MutateEconomy／Clone／CopyFromで**主Save1回**へまとめる。保存失敗はその他GameStateと既存Mergeロックも復元。新rollbackなし。
- 同日ではリセットしない。過去日では付与も日付後退もしない。新しい日だけ既存の日次台帳リセットを共用し、残高・累計・既存進行を維持する。

## 変更ファイル

- `Assets/Mofumachi/Core/MofuPointService.cs`：3API、累計非加算、共通散策消込、種別選択。既存Credit／日次リセット／再入防止を再利用。
- `Assets/Mofumachi/Presentation/TownDailyProgress.cs`：明示的な回復入口、実際の復帰・保存状態のガード、固定種別の散策取次ぎ。
- `Assets/Mofumachi/Presentation/UIFlowController.cs`：既存起動／復帰／行動への接続と同じ実日付スナップショットだけ。
- `Assets/Mofumachi/Presentation/TownWorldWalkModel.cs`：一時券へnullableの既存enum種別を付記・失効。
- `Assets/Mofumachi/Presentation/TownContinuousWorldView.cs`：選択関数注入と既存到着での券の引渡しだけ。
- `Assets/Mofumachi/Tests/EditMode/RecoveryExtraHelpTests.cs` と `.meta`：第5工程専用71ケース。
- `scripts/verify-recovery-extra-help-windows.cmd` と `.ps1`：専用Windows検証と自動結果共有。
- `References/Review/latest/summary.md` と `status.json`：今回の状態。

GameState／SaveService／Save version4／移行／pre-v4、GameStateManagerのrollback、通常4／6／8P、生成2P、12依頼、街Lv、Story／Chapter、UIレイアウト、TownActor、Visual Lock、フォント、BGM、正式画像は変更なし。前4工程のランチャー8ファイルも生バイト不変。課金・広告・購入分・補償・debug付与への接続なし。追加の+P通知は作らずロジックへ限定した。

## 確認結果

- 専用fixture：`Mofumachi.Tests.RecoveryExtraHelpTests`、**期待71ケース**。実Total／Passed／Failed／Skippedは **未取得（null）**。
- テストは製品コードより先に作成。回復の重複／翌日／過去日／上限、追加2回と跨種別ID、全おたすけ条件、0／1／2P境界、使い直し無限ループ防止、5receipt、累計long.MaxValue維持、IO／アクセス拒否rollback、保存中再入、固定券の実モデル到着・条件変化を含む。
- Core／Presentation／初期テストコードの静的C#コンパイルはエラー0・警告0。
- 独立した読み取りレビューを1回実施。初期false復帰通知による再試行と、journal失敗後の後続保存を1回の最小修正で対策。回帰ケース6件を先に加えて最終71件とし、変更したPresentation／テストソースの静的コンパイルもエラー0・警告0。再レビュー・全件テストへ拡大していない。
- PowerShell5.1の構文／API／UTF-8 BOMを静的確認。AST／EditMode限定引数の確認成功。実Windows動作確認の代わりにはしない。
- 一時Git環境で、自動共有成功、push拒否1回停止、custom index書込前拒否の3条件を各1回確認。元HEAD／index／staged／未追跡／未pushコミット・remote先行変更を保護し、旧inbox除去・生バイト一致・共有済み二重push防止も確認。合成資料をGitHubの実行証跡へ入れていない。
- **Unity起動0回、対象EditMode実行0回、自動再試行0回**。PlayMode・全件・APK・Pixel・Visual撮影・フォント確認なし。今回のUnity/test失敗は未実行のため未確認。
- 既存error.logは旧工程の履歴。第5工程の失敗として扱わず、今回更新しない。

## Windowsで次に行う1操作

**`scripts/verify-recovery-extra-help-windows.cmd` を1回ダブルクリック。**

Windows11／Windows PowerShell5.1／Unity6000.6.4f1の既存ライセンス、Android target。実装commit `0c22f30fae41c18471fe771f67153e4828716629` の隔離worktreeを固定し、上記fixtureだけをEditMode実行。Unity最大1回、自動再試行0回。元作業ツリー・staged変更・他のローカルコミットを変更しない。Guard変更・無効化なし、Visual Lock変更なし。

原本は `Builds/Diagnostics/recovery-extra-help-<timestamp>-<id>/` にresults.xml／unity.log／source-revision.txt／recovery-extra-help-result.json、失敗時first-error.logを保持する。71/71、Failed／Skipped／Inconclusive0、exit0、source入力不変を必須とする。

終了後は同じ操作内で **`References/Review/inbox/latest-recovery-extra-help/`** へ必要な小容量テキストだけ自動コピー・対象だけcommit・通常push1回。共有専用worktreeはremote先端を親にし、無関係変更を混ぜない。原本SHAと長さをrun-info.jsonへ記録。1ファイル4MiB／合計16MiBまで、大きいunity.logは原本保持と理由を記録しXML／JSON／最初のエラー等を共有する。

push失敗時は理由・ローカル原本・share-result.jsonを保持して停止。既存試行receiptがある場合はUnityを再起動せず、保存済み結果の共有だけを扱う。共有済みなら再pushもしない。

## 第6工程判定

**第6工程「12依頼拡張＋街Lv条件」へ進んで安全か：NO**。

主要ブロッカーは1つ：**第5工程のWindows実Unity71ケースの結果がまだない**。実装済みを関連テスト合格・工程完了とは扱わない。自動共有された同一runを次のレビューで照合する。今回は第6工程へ進まない。

statusのcommitは確認対象実装、launcher_commitは専用ランチャー、共有文書自身のcommitとは区別する。
