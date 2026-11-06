# 第2工程：Windows専用検証ランチャー準備完了・実行待ち

更新日：2026-11-06（日本時間）。状態：**修正済み・未確認**。

- 現在branch：`feat/daily-world-pass-20261016`
- 第2工程の実装commit：`97be3fafe5d73fda055ae3eba34c4ff8c45f85b7`。既存commitをそのまま使用した。
- GitHubへの実装公開：**成功・反映確認済み**。公開先端`1d30cb18050a14f126522ad4708adfe37caa9b8f`に実装commitが含まれ、変更5ファイルのblobも存在する。
- 今回は再実装・ゲームコード変更・再テスト・Unity起動を行っていない。
- 第2工程の直接関連EditMode：`Mofumachi.Tests.MofuPointServiceTests`、期待68ケース、**実Unity未実行（NotRun）**。
- 第1工程：対象`2b81174`のWindows63/63 Passedを合格済みとして保持。共有済みXML／source JSONも一致し、テストは再実行していない。

## 今回：既存Windowsランチャーを公開

**stage2 Windows launcher ready = true**。既存ファイルをそのままcommitする。内容の再実装・変更、テスト再実行、第1工程再実行は行っていない。通常push1回でランチャー2ファイルと本共有記録をまとめて公開するため、必要な状態項目を同じcommitへ含める。

- CMD：`scripts/verify-mofu-point-foundation-windows.cmd`
- PS1：`scripts/verify-mofu-point-foundation-windows.ps1`
- target implementation commit：`97be3fafe5d73fda055ae3eba34c4ff8c45f85b7`
- 対象：`Mofumachi.Tests.MofuPointServiceTests` のみ、期待 **68** ケース
- Windows 11／Windows PowerShell 5.1／Unity **6000.6.4f1**／EditMode
- Unity起動上限：**1回**。automatic retries：**0**
- Windows execution pending：**true**。この準備・公開をテスト合格として扱わない。
- 隔離worktreeで固定commitを使用。Noah/TownActor Guard、Visual Lock、第1工程ランチャーは変更せず、全件・PlayMode・APK・Pixel・Visual・フォント工程には進まない。
- 保存先：`Builds/Diagnostics/mofu-points-<timestamp>-<id>/`。results.xml、unity.log、source-revision.txt、mofu-points-result.json、失敗時first-error.logを保存。

ユーザーが次にする操作：`scripts/verify-mofu-point-foundation-windows.cmd`を1回ダブルクリック。

## 第2工程の変更ファイル（既存実装commit内）

1. `Assets/Mofumachi/Core/GameStateManager.cs`
2. `Assets/Mofumachi/Core/MofuPointService.cs`
3. `Assets/Mofumachi/Core/MofuPointService.cs.meta`
4. `Assets/Mofumachi/Tests/EditMode/MofuPointServiceTests.cs`
5. `Assets/Mofumachi/Tests/EditMode/MofuPointServiceTests.cs.meta`

## 実装内容

単一の`MofuPointService`を`GameStateManager.MofuPoints`から利用する内部APIとして追加済み。残高24P・0〜96P、実加算量の返却、消費可能判定・不足／負数拒否、財布とは独立した非減少の街行動累計を扱う。累計は既存Int64を使用し、long.MaxValueを超える加算は拒否する。0Pを未初期化扱いしたり、自動補充したりしない。加算／消費0は保存を通す有効なno-opとする。

日次適用日は正規ISO日付の後退を拒否し、後の日付に進んだ場合だけ当日会話ID・通常／追加散策回数・おたすけ済み・散策消込情報をリセット。残高・累計・初期化済み・導入日・回復／お花の履歴日・既存進行は維持する。会話は正式CharacterCatalog（`noah`）・重複不可・最大3名。散策は通常2／追加2／おたすけ1、合計5件のIDと回数を同じ保存で確定する。各取得APIは明示日付が現在適用日と一致する場合だけ台帳を更新し、遅延イベントの誤登録を拒否する。

既存MutateのClone／CopyFrom／MergeLocks復元を再利用し、経済操作では依頼進捗を再計算しない。新しいrollback実装や新サービスへの分割はしていない。

ゲーム中の生成・合成・会話・散策・お花・UI・依頼・街Lvには接続していない。Save version4・SaveService移行・pre-v4保護・正式素材・Guard・Visual Lock・TownActorは変更していない。

## 保存済み確認結果

- 直接関連テストソース：68ケース。残高・累計・日次・ID重複／上限・保存失敗時の全体復元／ロック保持・V4実保存の往復を対象に追加済み。
- 前工程中の限定APIコンパイル：Coreと新fixtureだけ、Unity SDK／Unity-custom NUnit API参照で各exit0。**NUnit／Unity実行合格の代用にはしない。**
- 前工程中の独立した読み取りレビュー：Coreの阻害事項なし。Windows runnerの例外情報保持を補足済み。
- 第2工程Native EditMode：未実行。Passed／Failed件数は未確認。今回のテスト再実行・自動再試行は0回。

## 前回の実装公開：Git同期と反映確認

既存実装を公開した前回の作業では、fetch first拒否を受けてFetchを**1回だけ**実施した。今回のランチャー公開でFetchは行っていない。リモート追加分`d4f8147d3c717b74cfaeb4e3fb3a6af097c9bae4`は第1工程の共有結果だけだった。既存実装commitとリモート結果の両方を通常mergeで保持し、Assetsが実装commitと完全一致することを確認した。

実装の通常pushは**1回成功**。GitHubの`ls-remote`で先端`1d30cb18050a14f126522ad4708adfe37caa9b8f`を読み戻し、同commitに含まれる実装commitと5ファイルのblobを確認した。reset／force push／stash削除／既存変更破棄は行っていない。共有記録は実装公開後の別commitで更新する。

前回まで未追跡だった専用CMD／PS1を、今回の明示指示により内容を変更せず追加する。既存の実装commit97be3faはそのまま維持する。

## 次工程

**第3工程「Merge生成時2P消費」へ進んで安全か：NO。** 第2工程のWindows実Unityによる68ケース合格が未確認。ランチャー公開を第2工程の合格・完了と扱わず、Windowsでの1回実行と結果レビューを待つ。
