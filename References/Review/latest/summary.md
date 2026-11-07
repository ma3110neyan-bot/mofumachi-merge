# 第4工程：通常会話・通常散策・今日のお花／Windows実行待ち

- branch：`feat/daily-world-pass-20261016`
- 実装commit：`9433202b7dd6f772a2e3c86abd6395b2343b3231`
- 専用ランチャーcommit：`0f0294fd711eb84842781dc4fdcfb96d5e86b69b`
- 状態：**修正済み・未確認**。実装と既存専用ランチャーは通常push済み。GitHub先端 `0f0294fd711eb84842781dc4fdcfb96d5e86b69b` と一致を確認。第4工程complete=false。
- 第3工程は実装 `b9e7f1a2e2384998878edbe97cba82885587e3dd` のWindows実Unity33/33 Passedとして合格済み。今回再実行していない。
- 目的：正式な3行動だけを既存ポイント管理へ接続する。第5工程へは接続しない。

## 実装

| 行動 | 財布への額面 | 1日の上限 | 街行動累計 |
| --- | ---: | --- | ---: |
| 通常会話 | 4P | 異なる正式キャラ3名 | 4×3まで |
| 通常散策 | 6P | 2回、同一ID重複なし | 6×2まで |
| 今日のお花 | 8P | 当日1回 | 8まで |

財布は96Pで止める。94Pから会話すると財布96P・累計+4、96Pでお花でも累計+8。日次の通常累計最大32。累計long overflow時は日付・台帳も含めて付与前に全体を拒否する。

`MofuPointService` の `TryRewardTalk`／`TryRewardNormalWalk`／`TryRewardFlower` が入口。財布計算は既存TryAddと共通の内部計算を使い、新しい残高管理は作らない。日付・対象・上限・重複・overflowを変更前に確認し、既存 `MutateEconomy → Mutate` のClone／CopyFrom／rollbackで財布・累計・台帳を**主セーブ1回**にまとめる。保存中の報酬再入も拒否する。GameStateManagerのrollback実装は変更しない。

同日はリセットしない。過去日は付与もリセットもしない。新しい日だけ既存の日次台帳リセットを共用し、残高／累計／既存進行を維持する。最初の正当な報酬時にrewardsStartDayを一度だけ記録。日次16Pや初期24Pの再配布はない。

### 実際のイベント接続

- `TownDailyService.RecordTalk`：既存会話記録の保存成功後、正式IDと同じ日付で4Pを確定。正式IDは `momo / lulu / pom / fio / miel / noah`。同日再会話は会話・交流記録を継続し、ポイントの主Saveは0回。
- `TownDailyService.RecordWalk`：既存日課のWalks=1は「今日の日課達成」を意味する。これと独立して一意な通常散策の2回目を付与。日課が既に1の場合は社会保存不要で、通常ポイントの2回目を処理できる。初回の社会保存に失敗した場合は付与しない。
- `TownDailyService.Claim`：今日のお花の社会保存後、その当日受領日を主側で消込。主保存失敗後の当日受領は、次の明示的な起動／受取で一度だけ照合可能。画面描画で付与しない。過去日の花の換金・ループ回復はしない。
- UIFlowControllerで各行動の実日付を1回取得して渡す。社会記録が未来日でも、それを現在時刻として使わない。
- 既存主人公の散策開始に、一時WalkId／WalkStartDayを付記。同じ指ドラッグ中はID維持、行き先変更は新ID、取消／復元／FastTravel／完了で旧IDを失効。同日開始・同日完了の既存 `walked=true` 到着だけが対象。NPC・同地区の短距離・FastTravel・日跨ぎは報酬なし。移動経路・速度・描画は変更しない。

交流journalと主セーブは別ファイルである。社会保存成功→主保存失敗では、journalの正当な交流・お花記録は保持し、GameState側の財布・累計・台帳・その他状態を復元する。**2ファイル同時保存や新rollbackは追加していない**。ここでのSave1回／重複時Save0回はポイントの主保存を指し、既存の再会話による交流保存は維持する。ポイント成功の通知は追加していない。

## 変更ファイル

- `Assets/Mofumachi/Core/MofuPointService.cs`：3報酬、事前判定、共通加算・日次リセット、再入防止。
- `Assets/Mofumachi/Presentation/TownDailyProgress.cs`：会話／散策／お花の既存成功処理へ任意のポイント接続。未接続の旧journal利用は維持。
- `Assets/Mofumachi/Presentation/UIFlowController.cs`：ポイント管理の注入、日付スナップショットと散策券の引渡しだけ。
- `Assets/Mofumachi/Presentation/TownWorldWalkModel.cs`：散策の一時ID／開始日・失効処理だけ。
- `Assets/Mofumachi/Presentation/TownContinuousWorldView.cs`：既存主人公の到着から同じ券を渡すだけ。
- `Assets/Mofumachi/Tests/EditMode/TownPointRewardTests.cs` と `.meta`：第4工程の69ケース。
- `Assets/Mofumachi/Tests/EditMode/Mofumachi.EditModeTests.asmdef`：純粋なjournal／移動モデル連携のEditMode検証用に既存Presentation参照を1個追加。GameObject生成・PlayMode実行なし。
- `scripts/verify-town-point-rewards-windows.cmd` と `.ps1`：専用ランチャー。
- `References/Review/latest/summary.md`／`status.json`：今回の公開状態。既存error.logは変更していない。

Merge生成2P、Save version／移行／pre-v4、12依頼、街Lv条件、Story／Chapter、UI配置、TownActor、Visual Lock、フォント、BGM、正式画像は変更なし。既存第1～第3工程の6ランチャーファイルも生バイト不変。日次回復16P・追加散策2P・おたすけ4P・購入／補填／debug付与には接続していない。既存Feedbackを含め+P表示は追加せず、報酬ロジックに限定した。

## 既存確認結果（今回再実行なし）

- 新規fixture：`Mofumachi.Tests.TownPointRewardTests`、**期待69ケース**。実Total／Passed／Failed／Skippedは**未取得（null）**。
- CoreとPresentationの静的C#コンパイル：各1回、エラー0・警告0。
- テストコードの最初の静的コンパイルは `TownPointRewardTests.cs:136,137` でCS1061。新テストが非公開 `GameState.MergeLocks` を参照したことが原因。テストだけを既存の公開 `TryMerge`／`Board.IsLocked` へ1回修正し、修正部分のコンパイルはエラー0・警告0。Coreの可視性を拡張していない。この既知履歴はsummary/statusに保持。既存error.logは別工程の履歴であり今回変更しない。これはUnity実行・NUnit実行の失敗ではない。
- 会話の上限／重複、通常散策2回、当日花、日次非後退、96P時の額面累計、overflow、保存失敗3種×IO／アクセス拒否、journal失敗、主保存との隙間、再入、実移動モデルの到着／日跨ぎをテストへ含めた。**実Unityでの合格は未確認**。
- 独立した読み取りレビュー1回。上記の既知テスト指摘以外に重要指摘なし。
- 軽微な保留1件：移動モデルへ「指追従中Restore→そのままFollowFinger」を直接行うと、release/cancel/new guideまで券が空のまま。現行UIの復元は新しいモデルに行うため現在の画面不具合は確認されていない。将来その導線を追加した場合、その散策が無報酬になる可能性を記録し、今回移動仕様は広げない。
- PowerShell5.1の構文／API／UTF-8 BOMを静的確認。ASTと対象fixture引数の確認成功。一時Git環境で共有3条件（remote先行時の元HEAD／index／staged／未push保護、push拒否1回停止、custom index書込前拒否）を各1回確認。合成資料は実行証跡へ入れていない。
- **クラウドUnity0回、対象EditMode0回、自動再試行0回**。全件・PlayMode・APK・Pixel・Visual撮影・フォント確認なし。第4工程の最初のUnity/testエラーは未実行のため未確認。

## 次に行う操作

**`scripts/verify-town-point-rewards-windows.cmd` を1回ダブルクリック。**

Windows11／Windows PowerShell5.1／Unity6000.6.4f1の既存ライセンスを使用。対象commitは `9433202b7dd6f772a2e3c86abd6395b2343b3231` に固定した隔離worktree。上記fixtureだけをEditModeで実行、Unity最大1回・自動再試行0回。Noah Visual Guardの変更／無効化、Visual Lock変更は行わない。

原本：`Builds/Diagnostics/town-point-rewards-<timestamp>-<id>/`。results.xml／unity.log／source-revision.txt／town-point-rewards-result.json、失敗時first-error.logを保持。

実行終了後は同じ操作内で **`References/Review/inbox/latest-town-point-rewards/`** へ必要な小容量テキストを自動コピー・対象フォルダだけcommit・通常push1回。remote先端を親とする別共有worktreeを使用し、無関係なローカルコミット／staged変更を混ぜない。1ファイル4MiB・合計16MiB、原本SHA／長さと一覧をrun-info.jsonへ記録。大きいunity.logは原本をローカル保持してmanifestへ理由を記録し、XML／JSON／最初のエラー等を共有。push失敗は理由とshare-result.json・ローカル原本を保持して停止。既存の試行receiptがあればUnityを再起動せず、保存済み結果の共有だけを扱い、共有済みならpushもしない。

## 第5工程判定

**第5工程「日次回復・追加散策・おたすけ散歩」へ進んで安全か：NO**。

主要ブロッカー：**第4工程のWindows実Unity69ケースの結果がまだない**。実装済みを工程完了・関連テスト合格とは扱わない。自動共有された同一runのsource／fixture／69件／exit／入力不変／SHAをレビューした後に判定する。今回は第5工程へ進まない。

statusのcommitは実装を指し、ランチャーや共有文書だけのcommitとは区別する。

## 今回の公開確認（2026-11-07）

既存の実装8ファイルとランチャー2ファイルをそのまま使用。開始時作業ツリーはclean、branchはfeature。GitHubの開始先端は10ce38eで第4工程2コミットが未pushだった。Fetch0回、実装・ランチャーの通常push1回が成功し、GitHub先端0f0294fの再読取と、その履歴内の実装commit／CMD／PS1の存在を確認した。今回の追加変更は固定共有summary/statusだけ。再実装・再テスト・第3工程再実行はすべて0回。
