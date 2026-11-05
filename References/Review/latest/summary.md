# 最新レビュー：SaveData v4第1工程／ガード停止・直接テスト未実行

- 現在ブランチ：`feat/daily-world-pass-20261016`
- 実装候補commit SHA：`2b81174496a5b4703bad0516645829893d4297b9`（push済み。共有記録だけの後続commitと区別）
- 作業開始HEAD：`0a448d2d3a084c7c581122536ebe1d7533a98b2d`。開始時の作業ツリーはclean。
- 今回の目的：v1/v2/v3→v4の保存移行、原本保護、読込停止のみ。ポイント経済のゲーム処理は実装しない。
- 状態：**修正済み・未確認**。実装候補はあるが、直接関連確認が未完了なので第1工程の完了条件は未達。
- 成功／失敗：既存Safety Guardを1回確認して失敗。指示どおり実装変更とテストを停止し、ガード再実行・解除・弱体化をしていない。Unity・テスト・APIコンパイルは未実行。
- 根本原因：ガードが旧ノア候補のTownActor.csに対し、現行ファイルと違うSHAを要求して停止。これは作業開始HEADでも同じ不一致。今回TownActor.csは変更していない。**SaveData差分を検証する段階には未到達で、SaveDataコードが原因の失敗と断定しない。**
- 次にやること：現行承認済み候補に適したガード入口と限定検証経路を読み取りで整理する。正式原本、TownActor、ガードを勝手に直さない。整合後、ライセンスのある環境で保存関連対象だけを1回確認する。今回はこの共有で停止。

## 実装候補の変更内容

saveVersion=4、保存専用EconomyState、初期24P・累計0・初期化済み・未取得履歴・新旧依頼経路を追加。v1/v2の欠損補完は元版と項目存在を区別し、存在する音量・パートナー等を保持する。v4の0Pは再初期化しない。Clone/CopyFrom/Validate/シリアライズへ新状態を含める。未知の既存データもExtensionDataで保持する構成。

正常な旧主セーブだけからsave.pre-v4.jsonを排他作成し、生バイトの長さ・SHAを照合。SHAをv4にも保存し、後日の原本不整合も検出する構成。退避・移行・検証・未対応版・必須経済情報欠損・途中状態は明示LoadBlocked/MigrationBlockedへ進め、初期GameStateで代用しない。停止したインスタンスの保存を拒否する。

UIFlowControllerへの変更は、主読込を社会記録初期化より先に行い、停止時は通常セッションを開始せず既存Feedbackを使う読込ゲートだけ。正常時の画面配置・色・盤面・ボタン・素材・フォント・BGMを変更していない。この停止時表示の実Unity確認は未実行。

保護済みpre-v4がある状態のResetSaveは停止し、保護データを新規開始へ黙って置換しない。生原本の自動削除・上書きはしない。これは保護のための実装判断で、通常の移行なし新規セーブの既存reset対象3ファイルは維持。

## 変更ファイル（実装候補8ファイル）

- `Assets/Mofumachi/Core/GameState.cs`
- `Assets/Mofumachi/Core/IStateStore.cs`
- `Assets/Mofumachi/Core/SaveService.cs`
- `Assets/Mofumachi/Presentation/UIFlowController.cs`
- `Assets/Mofumachi/Tests/EditMode/CharacterPreferenceTests.cs`
- `Assets/Mofumachi/Tests/EditMode/PreferencesTests.cs`
- `Assets/Mofumachi/Tests/EditMode/SaveServiceTests.cs`
- `Assets/Mofumachi/Tests/PlayMode/MergeProgressionUiTests.cs`

製品4ファイル＋直接関連テスト4ファイル。PlayMode側は既存のsaveVersion期待値1か所を3→4としただけ。QuestCatalog、TownGrowthService、MergeBoard、GameStateManagerの生成・消費、日次報酬、正式素材、Visual Lock原本、スクリプトは変更していない。ゲーム設計の変更はないためdocs/game-design/latest.mdも変更していない。

## 最初のガード失敗

```text
Display differs from the authorized correction candidate.
```

- ガード：`Assert-OriginalBaseForCandidate` → `Get-BasePolishPatch`
- 拒否位置：`scripts/visual-lock.ps1:89`
- 参照記録：`References/VisualLock/noah-final-rig-candidate-2026-10-29.json`
- 最初の不一致：`Assets/Mofumachi/Presentation/TownActor.cs`
- 記録の期待SHA：`10dc0c0a8f041bc511ef13c0d89bb773ef6e8a0fc564f39325e20a8a0c68c57e`
- 今回候補SHA：`1591e855b186f5e232600f24cc7ab72c70523eee6975b2b62d0002632e8905b2`
- 作業開始時SHA：`1591e855b186f5e232600f24cc7ab72c70523eee6975b2b62d0002632e8905b2`（今回候補と同一）

SHA照合は保存済みエラーの原因特定のための読み取りで、ガードを再実行していない。既存の承認候補／入口との整合を先に確認することが最小対応。旧記録のSHAを現在値へ書き換えて回避する案、TownActorを戻す案は採らない。詳細はerror.log／status.jsonへ記録。

## テストと環境

SaveServiceTestsへ新規v4、旧3版、全旧項目、生バイト退避、再読込0P、CopyFrom/失敗rollback、既存退避、退避失敗、commit途中、不正v4、未対応version、街Lv異常、退避改変、未知項目保持の対象を追加。PreferencesTests/CharacterPreferenceTestsはv4期待と明示停止の仕様に対応させた。これらは**ソースを用意しただけで未実行**。

| 確認 | 結果 |
| --- | --- |
| 静的レビュー | 1回。欠損/不正値区別と停止時Feedbackの2指摘をガード確認前に対応。実行合格の代用にしない |
| 文書・ソース差分確認 | 実施。今回対象外のロジック・素材・原本への差分なし |
| 既存Source Safety Guard | 1回失敗。再試行0回 |
| SaveData直接関連EditMode | 未実行。ガード停止後は開始しない |
| 保存再開の既存PlayMode期待・読込停止画面 | 未実行 |
| APIコンパイル | 未実行 |
| Unity起動 | 0回 |
| 全件・APK・Pixel・capture・フォント工程 | 未実行 |

描画DISPLAY/WAYLAND設定なし、Xvfbなし。Unityライセンス関連環境変数は未設定。標準保存先 `/root/.local/share/unity3d/Unity` の参照は権限拒否のためライセンスの有無を確定できず、同じ参照を再試行していない。この環境不足を新しいゲーム不具合・Unity失敗ログとして扱わない。新しいUnityエラー／test failureは観測していない。

**第2工程「もふポイント管理基盤」へ進んで安全か：NO。** ガード整合と第1工程の直接関連確認が未完了。実装候補のpushを完了・採用・実機合格と扱わない。第2工程へ自動で進まない。
