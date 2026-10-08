# Vertical Slice コア実装記録 — 2026-10-08

最上位仕様は[引き継ぎセット v1.0](../mofumachi_codex_handoff_v1.0.md)。[正式画像・既存プレビュー・音方針](../References/README.md)を参照し、[実装計画](superpowers/plans/2026-10-08-vertical-slice.md)のTask 1/2から実装を開始した。現在はTitleScene/GameSceneと最小ループUI、AudioManager、Windows用APK生成処理を追加した。Unityでの実行は未確認で、Vertical Slice全体は未完成。[Windows / Pixel 3a手順](android-smoke-test.md)を参照。

## 実装した範囲

- `Assets/Mofumachi/Core`: 状態モデル、JSONセーブ、バックアップ復旧、盤面、合成、受注、納品、報酬、街成長、保存失敗時のロールバック。
- 引き継ぎ§8の保存項目を保持。未知バージョン・破損・無効値を拒否し、有効なバックアップまたは初期状態へ戻す。
- 同じID・同じlevelの合成のみ許可。最大level・異種・無効セル・同セル・ロック中の操作を拒否する。
- 必要アイテムを全件確認してから消費し、完了ID・報酬受取ID・街成長を一緒に保存する。重複納品と重複報酬を拒否する。
- 保存失敗時は盤面・所持品・報酬・依頼状態を戻す。合成中のアイテムは納品に使えず、別操作の保存失敗でも既存のアニメーションロックを保持する。
- BGM/SEの個別設定は状態とセーブに保持する。音の再生は追加したAudioManagerへ接続し、現在は試作合成音を使う。正式音源と実機の音質確認は未完了。

初回の仮設定は5列×6行、`tea`系統・最大level 3、level 2を1個納品して30 coins・街成長1段階。初期盤面にlevel 1を2個置く。これらの数値は引き継ぎで確定した仕様ではなく、初回ループを検証するための最小設定。

## 検証結果

| 検証 | 結果 |
| --- | --- |
| Unity同梱.NET SDK 8.0.318で同じNUnitソースを実行 | 39件成功、失敗0、スキップ0 |
| 新規テストプロジェクトの`--locked-mode`復元 | 成功 |
| 製品コードの.NET Standard 2.1 / C# 9ビルド | 成功、警告0、エラー0 |
| 新規Unityアセットのmeta・GUID | 欠落0、重複0 |
| 正式参照先と原本のSHA-256 | 画像・HTMLとも一致 |
| Unityプロジェクトの再インポート | 終了コード198、有効なEditorライセンスなし |
| Unity EditMode / PlayMode | 未実行 |
| APKビルド / Pixel 3a実機 | 未実行 |

NUnitは保存往復、破損復旧、保存不能、誤合成、ロック、数量不足で無消費、必要数だけの消費、重複入力、再読込、16呼出しの同時納品、保存と納品の競合を検証した。独立レビューで見つかった4件を再現テスト付きで修正し、全件を再実行した。[再実行手順](../Tests/Core/README.md)。

このクラウドのテスト結果は`/workspace/artifacts/mofumachi/core-final.trx`。セットアップ再実行ログは`/workspace/setup/mofumachi/setup-rerun-final.log`、Unity再インポートログは`/workspace/setup/mofumachi/vertical-slice-work/unity-import-core.log`に保存する。

## 次の実装に渡す契約

Unityの起動コンポーネントで`SaveService(Application.persistentDataPath)`の`Load()`から状態を取得し、`GameStateManager`へ渡す。UIの盤面変更はManagerを通す。直接`MergeBoard`を呼ぶと自動保存の対象にならない。

合成成功後はロックを保持し、アニメーション終了時に`Board.ReleaseLocks()`→`Quests.RefreshProgress()`→`Save()`を呼ぶ。取消・pause・Scene切替でも解放する。ロックは一時状態でJSONへ保存せず、再起動時には残らない。pause/quitと画面切替は`UIFlowController.Persist()`へ接続済み。実際のAndroidライフサイクルはWindows生成APKで検証する。

依頼は保存中のIDを作成時のカタログから解決する。既定は`QuestDefinition.First`だけ。追加依頼を使う場合は`QuestManager`に受注時と再読込時で同じカタログを渡す。未登録の依頼は保存前に拒否し、保存済みIDが見つからない場合は`LastError`で通知する。

保存や納品に失敗した場合は戻り値と`LastError`／`DeliveryResult.Message`を表示し、成功扱いにしない。Result画面は既に支払われた報酬を表示し、再タップで報酬サービスを呼ばない。

## 未完了

Task 3/4のTitleScene/GameScene、safe area・ドラッグのUI、正式6名の表示、AudioManager、PlayModeテスト、AndroidBuild、Windowsビルドスクリプトを追加した。Unityでのインポートと実行、画面・音のフィードバック、APK、Pixel 3aの1周確認は未完了。正式BGM/SEは未収録だがコア実装の前提にはしない。

最終ビルドはユーザーのWindows PCで行う。クラウドのライセンス対応は保留。WindowsのUnityでインポートとEditMode/PlayMode実行を確認し、Unity/IL2CPPでのJSONシリアライズ、Androidでのファイル置換を検証する。.NETでの成功だけでUnity・Androidの成功を主張しない。
