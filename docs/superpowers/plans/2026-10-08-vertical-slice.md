# もふまちメルジュ Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Execution uses the existing isolated cloud checkout; do not create a worktree.

**Goal:** AndroidでTOP→ホーム→依頼受注→Merge→納品→報酬→街成長→保存→再起動復帰を1周できる。

**Architecture:** Unityから独立した状態モデルを単一のGameStateManagerが所有する。盤面・依頼・報酬・街成長・保存のサービスを分離し、TitleSceneとGameSceneのUIが同じ状態を参照する。

**Tech Stack:** Unity 6000.6.4f1、C#、URP 2D、Input System 1.20.0、UGUI 2.6.0、Unity Test Framework 1.8.0。既存manifest/lockを維持する。

**Spec:** [最上位: 引き継ぎセット v1.0](../../../mofumachi_codex_handoff_v1.0.md)、[設計・プレビュー確認案](../specs/2026-10-08-vertical-slice-design.md)。正式画像・既存プレビュー・音の補足を受領した。これを現在の設計入力としてTask 1/2のコア実装を進める。画面と音の確認・修正はTask 3で継続する。

## Global Constraints

- 『もふまちメルジュ』、Unity 2D / 縦画面 / ローカルセーブ、Android / Google Play。
- 「2026-10-06採用の6名キャラクターデザインを正式基準とする」「キャラ名は未確定。命名は後工程」。新キャラ・旧3名版を代用しない。
- 「同じ itemId ＋ 同じ level の2個だけ合成可能」「最大level同士は合成不可」。合成中セルをロックする。
- 「1個でも不足していればアイテム消費禁止」「二重タップ・二重報酬禁止」。報酬付与後は即保存対象。
- 「初回5分は強い課金導線を出さない」。ショップ/広告/IAP/イベントは対象外。
- 引き継ぎ§8の全保存項目・全保存タイミングを満たす。「破損データをそのまま使用しない」。
- 「実装前にFB可能なプレビューを提示する」「画面・操作・BGM/SE確認 → FB → 修正 → 本実装」。無音の画面確認で音の確認を代替しない。
- Pixel 3aの起動・操作・保存復帰を実機で確認する。APKの存在・ビルド成功・実機成功は別々に報告する。

## Review Focus

- 連続・同時の受注/納品入力で二重消費・報酬が起こらない（Task 2）。
- 保存失敗時に成功表示や再起動後の重複報酬が起こらない（Task 1/2）。
- 両セーブ破損・未知saveVersionで安全な初期状態へ戻る（Task 1）。
- ドラッグ中の画面切替・一時停止でロックやアイテムが残らない（Task 3）。
- Pixel 3aのsafe area・タッチ・画面比率で全操作が使える（Task 4）。

## Task 1: 状態と安全なセーブ

**Files:** Create `Assets/Mofumachi/Core/GameState.cs`, `GameStateManager.cs`, `SaveService.cs`, `IStateStore.cs`, `Mofumachi.Core.asmdef`; Create `Assets/Mofumachi/Tests/EditMode/SaveServiceTests.cs`, `Mofumachi.EditModeTests.asmdef`。新規フォルダ・ファイルのmetaも作成する。

**Interfaces:** `GameState`は引き継ぎ§8全項目と`BoardItem { itemId, level, cellIndex }`、`InventoryEntry { itemId, level, count }`を持つ。`IStateStore.Save(GameState state)`は失敗時に例外、`LoadResult Load()`は検証済状態と復旧理由を返す。`SaveService(string directory)`が実装し、`GameStateManager.Save()`を提供する。起動・pause・quitとの接続はTask 3のUnityコンポーネントで行う。

- [x] 全保存項目の往復一致、主ファイル破損→バックアップ復旧、両ファイル破損/未知バージョン→初期状態、無効セル/負数→拒否、書き込み不能→例外のEditModeテストを書く。
- [x] Unity同梱.NET SDKで、同じNUnitソースの未実装エラーを確認する。Unity EditMode実行とは区別する。
- [x] 一時ファイル→検証→主ファイル置換と前回有効データのバックアップを実装する。復旧理由はLoadResultへ保持する。
- [x] .NET上で保存テストを実行し、独立レビューで見つかった保存・納品の同時実行問題を修正する。
- [x] WindowsのUnity EditModeでコア39件の成功を確認した。Androidの保存APIはPixel 3aで確認する。

## Task 2: Merge・依頼・報酬・街成長

**Files:** Create `Assets/Mofumachi/Core/MergeBoard.cs`, `QuestManager.cs`, `QuestDefinition.cs`, `RewardService.cs`, `TownGrowthService.cs`; Create `Assets/Mofumachi/Tests/EditMode/MergeBoardTests.cs`, `DeliveryTests.cs`。

**Interfaces:** `MergeBoard(GameState state)`（寸法と最大levelは現在`GameState`の定数）, `bool AddItem(string itemId,int level)`, `bool TryMove(int from,int to)`, `bool TryMerge(int from,int to)`, `bool IsLocked(int index)`, `void ReleaseLocks()`。`QuestManager(GameState state,IStateStore store,RewardService reward,TownGrowthService town,IEnumerable<QuestDefinition> questCatalog = null)`, `bool AcceptQuest(QuestDefinition quest)`, `bool CheckDelivery()`, `DeliveryResult TryDeliver()`。`RewardService.GrantReward(GameState state,string rewardId,int coins)`と`TownGrowthService.EvaluateTownGrowth(GameState state)`を使う。DeliveryResultは成功・拒否・保存失敗を区別する。依頼は同じIDのカタログから再開する。既定カタログは初回依頼のみで、追加依頼を受注・再開する場合は同じカタログを渡す。未登録の依頼は保存前に拒否する。

- [x] 同ID/同levelのみ合成可、最大level・異種・同セル・無効セル・ロック拒否、空セル移動、誤移動で不変をテストする。
- [x] 未受注/不足/完了済/報酬受取済で数量と所持金が不変、正常納品で必要数のみ減る、連続二重入力とLoad後も報酬1回のテストを書く。
- [x] .NET上で未実装エラーを確認し、引き継ぎ§7の順で処理する。同じID/levelを要求する複数行は合算して検証する。
- [x] 報酬・完了ID・街成長を同じスナップショットとして保存する。保存失敗時は作業前状態に戻し、再試行でも報酬が重複しないことをテストする。
- [x] Task 1/2のNUnit計39件が.NET上で成功。独立レビューの4件を再現テスト付きで修正する。
- [x] WindowsのUnity EditModeで、BuildTime 1件を含めた40件の成功を確認した（SDK修正前の`cf191a0`）。

## Task 3: 正式素材で1周の画面と音

**Files:** Create `Assets/Mofumachi/Presentation/UIFlowController.cs`, `MergeBoardView.cs`, `AudioManager.cs`; Create `Assets/Scenes/TitleScene.unity`, `GameScene.unity`; Create `Assets/Mofumachi/Tests/PlayMode/VerticalSliceFlowTests.cs`, `Mofumachi.PlayModeTests.asmdef`; Modify `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ProjectSettings.asset`。正式画像を同一バイト列で`Assets/Mofumachi/Resources/Mofumachi/CharacterMaster.png`へ追加し、日本語フォントを同梱する。正式音源は未収録で、現在はAudioManagerの試作合成音を使う。

**Interfaces:** `UIFlowController.ShowHome()`, `ShowQuest()`, `ShowMerge()`, `ShowResult(DeliveryResult result)`, `ShowSettings()`。`MergeBoardView`がInput System入力をモデルへ渡し、アニメーション終了時に`Board.ReleaseLocks()`→`Quests.RefreshProgress()`→`Save()`を実行する。ロックは保存しない一時状態で、納品の条件確認と消費から除外する。取消・pause・Scene切替時にも解放する。`AudioManager.SetBgmEnabled(bool enabled)`, `SetSeEnabled(bool enabled)`, `PlaySe(AudioCue cue)`。Resultの表示は報酬支払いを行わない。

- [x] Title→ホーム→受注→Merge→納品→Result→ホームと、保存後Scene再読込のPlayModeテストを追加した。ドラッグ取消・Scene切替・pauseでロック解放と数量維持を検証する。
- [x] 9:16 CanvasScaler、safe area、縦画面、Title/Gameの起動bootstrap、Input Systemのドラッグと2タップ移動を追加した。未実装クラスによるコンパイル失敗を確認後、Unity DLL参照でコンパイルした。
- [x] WindowsのUnity PlayModeで5件の成功を確認した。クラウドのコンパイル確認を実行結果とは扱わない。
- [ ] 正式6名素材の顔・外形・配色を変えずに使用する。ユーザーのBGM/SE方針に沿う仮音の確認導線・個別音設定を実装し、正式素材受領後に差し替える。正式ファイルは現在未収録。無音・素材欠損を完成としない。
- [ ] 全PlayMode成功と件数を確認する。画面・操作・音を承認済プレビューと比較する。
- [x] Scene・設定・素材・UI・テストを含む差分をレビューしてコミットした。SDK修正の追加差分は別途確認する。

## Task 4: Android APK と Pixel 3a

**Files:** Create `Assets/Mofumachi/Editor/AndroidBuild.cs`, `docs/android-smoke-test.md`。

**Interfaces:** `Mofumachi.Editor.AndroidBuild.BuildDevelopmentApk()`はTitleScene/GameSceneをAndroid ARM64開発用APKにビルドする。`BuildReport`がSucceeded以外なら例外で終了。既定成果物はWindowsの`Builds/Android/vertical-slice.apk`。`-mofumachiApkPath`で指定でき、PowerShellスクリプトは実行ごとに新しいフォルダへ出力する。

- [ ] EditMode/PlayMode成功後にAndroidビルドコマンドを実行する。新規APK・今回の成功ログ・終了コード・サイズを確認する。
- [ ] Pixel 3aで引き継ぎ§12の9項目を実行し、端末・APK・日時・各結果を記録する。画面比率・safe area・ドラッグ・誤合成・不足納品・正常納品・二重納品・保存再起動が必須。
- [ ] 未実行は未実行と報告し、実機成功前にSlice完成と呼ばない。署名鍵やPlay Console公開は本タスク外。
- [ ] ビルド導線とテスト記録をレビューしてコミットする。

## Windowsでの検証とビルド

ユーザーは最終APKをWindows PC上のUnity 6000.6.4f1で作り、Pixel 3aで実機確認する。クラウドのUnityライセンスは保留とし、クラウドで最終APKビルドを行わない。

TitleScene/GameScene、Presentation、AudioManager、AndroidBuild、BuildTimeテスト1件、PlayModeテスト5件を追加した。Windowsの`cf191a0`でEditMode 40件・PlayMode 5件がすべて成功した。手動でも納品結果の+30 Coins・街Lv.1を確認した。APKの最初の試行は旧API 34によるAAR検査で失敗し、SDK修正後の再ビルド・実機試験はこれから行う。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-android.ps1
```

手順・SDK設定・インストール・9項目の確認は[Windows / Pixel 3a手順](../../android-smoke-test.md)。PowerShellは今回のXMLを確認し、EditMode最低40件、PlayMode最低5件、失敗・スキップ0を要求してからAPKを作る。

### 2026-10-09 Windows初回再生の追補

Safe Mode解除とTitleSceneの画像・BGM読み込みがユーザー画面で確認できた。QHDでは画像のFitInParentが表示枠を失って重なり、幅基準のスケーリングで文字が収まらず、Camera不在の案内も表示された。画像枠とフィット対象を分離し、safe area内の9:16表示とExpandスケーリング、背景Cameraを追加した。開始ボタンの文字・クリック判定・ホーム遷移を含むPlayMode回帰テストを追加し、計5件とした。その後Windowsで全5件の成功と、開始から納品までの手動操作を確認した。

### 2026-10-09 Android SDKの追補

日本語を含む親パスによるAndroidビルドエラーの後、Windows側のプロジェクトを`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`へ移動した。Gradleはその後、AndroidX core/core-ktx 1.15.0のcompileSdk 35以上という要件を満たさないAPI 34設定で失敗した。公式AARで要件を確認し、Unity 6000.6.4f1に同梱されたAPI 36へビルドメニュー・Player Settings・BuildTimeテストを揃える。最低APIは26を維持する。

- [x] 公式AARメタデータと設定を比較し、旧34で失敗、修正36で成功する検査を確認した。
- [x] 修正後の全ソースを実際のUnity DLL・内蔵NUnitでコンパイルし、コアの.NETテスト39件が成功した。
- [ ] Windowsで変更したSDK設定テストを再実行し、新しいAPKと今回のビルド成功ログを確認する。
- [ ] Pixel 3aで起動・保存復帰を含む実機試験を行う。

## 現時点の検証記録

[2026-10-08 コア実装と検証](../../vertical-slice-core-status.md)と[ライセンス不要の検証手順](../../../Tests/Core/README.md)を参照。Task 1/2のコアは実装済みで、WindowsのUnityテスト40件・5件はSDK修正前に成功した。SDK修正後のWindows確認、最終APK、Pixel 3a実機は未完了で、Vertical Slice全体の完成はまだ主張しない。
