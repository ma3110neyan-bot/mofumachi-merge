# もふまちメルジュ Pixel 3a品質向上 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. 既存の独立したクラウドcheckoutを使い、worktreeは作成しない。

**Goal:** 承認済みの画面・音で、Pixel 3aの縦画面全体を使い、TOP→ホーム→依頼→Merge→納品→報酬→街成長→保存／復帰を一周できるAPKのソースと検証手順を仕上げる。

**Architecture:** 既存UGUI／Input Systemと保存・Merge・納品コアを継続する。画面組み立てをUIFlowControllerから責務別の表示クラスへ分け、背景とSafe Area内の操作を独立させる。設定はv2セーブへ移行し、音は録音素材を使う常駐AudioManagerへ置き換える。

**Tech Stack:** Unity6000.6.4f1、C#9／.NET Standard2.1、UGUI2.6.0、Input System1.20.0、Unity Test Framework1.8.0。manifest／lockと正式6名の原本を維持する。

**Spec:** [2026-10-09承認済み設計](../specs/2026-10-09-pixel3a-quality-design.md)。画面・音・設計の方向と本計画はユーザー承認済み。Codexが順次実装し、最後に独立レビューする。チェック済み工程のクラウド検証はWindows／実機の合格を意味しない。

## Global Constraints

- 公開目標2026-10-31。品質とPixel 3aでの安定動作を優先し、公開日・審査完了を保証しない。
- 正式6名の顔・体型・配色・衣装・シルエットを変更しない。原画SHA-256は`cd423f6599cf41346064405240c72ac071aaa8d3fbd326d2b82661b13b0892c8`。透過全身／歩行素材は未収録で、承認済み原画カードを使う。
- TOP→ホーム→依頼→Merge→納品／報酬→街成長→設定の順で画面を実装する。課金・広告・図鑑・イベント・ショップ・新依頼・ゲーム内リセットは追加しない。
- 最初の依頼は「お茶会の準備」、お茶Lv.2×1、30 Coins、街Lv.0→1。5列×6行、最大Lv.3。合成ロック、納品の原子的保存、二重報酬防止を継続する。
- Canvas Scalerは360×640／Expand。背景は全画面、操作はSafe Area全体。9:16へのFitInParentは横長Editorのみ。セルは正方形、主要タップ領域は48dp相当以上、文字を隠す自動縮小を使わない。
- 初回案内は「課金について」「未成年の方は、課金する前に必ず保護者の方に相談し、許可をもらってください。」、ボタン「確認しました」。保存成功後に進む。実際の購入承認は別経路とし、今回は購入処理を実装しない。
- 保存v2。v1の全進行と音ON/OFFを保持し、新項目のみBGM0.75／SE0.65／未確認で補う。0とOFFを区別し、不正音量・未知形式を拒否する。
- BGM／SEは別AudioSource。承認済みCC0録音素材の旋律・6SEを採用し、仮の実行時合成音を削除する。シーン移動／設定表示でBGMを再起動しない。
- Windowsの正しいプロジェクトは`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`。API36／最低API26／ARM64／IL2CPP／OpenGLES3を継続する。
- クラウドのUnityライセンス・最終APKビルドは保留。Windowsネイティブ試験とPixel実機試験を、クラウドの.NET試験・Unity DLLコンパイルと区別する。
- USBケーブルを使い、通常版`com.mofumachi.merge`の更新ではデータを残す。QA版`com.mofumachi.merge.qa`は別の保存領域。実アプリのアンインストール・データ消去を自動実行しない。
- Play公開・アップロード・署名鍵作成は実施しない。秘密鍵／パスワードをGitへ入れない。Windowsの既存stash／生成ファイルを一括破棄しない。

## Review Focus

1. 新旧セーブ：v1の完了進行とOFFが移行後も保持され、壊れたv2は健全なバックアップへ復帰する（Task1）。
2. 確認失敗／連打／戻る：確認保存に失敗すると未確認のまま止まり、直起動GameSceneや戻る操作でもホームへ迂回しない（Task2）。
3. 入力と中断：ドラッグ末尾のクリック、合成中の切替、納品連打、報酬演出中の停止でアイテム・報酬が重複しない（Task4・5）。
4. 画面形状と日本語：Pixelの長い縦画面、短い縦画面、疑似ノッチ、最大HUD値で本文／操作が収まり、課金案内の全字が出る（Task2・4・6）。
5. 音量変更：0／OFF、ドラッグ中断、書込失敗、シーン切替で見た目・保存値・再生が一致し、音源を重複生成しない（Task3・6）。

---

## ファイルと責務

| 対象 | 責務／所有タスク |
| --- | --- |
| `Core/GameState.cs`、`GameStateManager.cs`、`SaveService.cs` | v2移行・設定・巻き戻し（1） |
| `Presentation/ResponsiveUILayout.cs` | 全面背景・Safe Area・横長Editor・セル寸法（2） |
| `Presentation/UIWidgets.cs`、`RoundedPanelGraphic.cs`、`UIIconGraphic.cs` | 共通ボタン・パネル・原画表示・押下反応（2） |
| `Presentation/ScreenContext.cs`、`UIStrings.cs` | 表示クラスに渡す依存と固定文言（2） |
| `Presentation/TopHomeScreens.cs`、`PurchaseNoticeView.cs` | TOP／ホーム／必須案内（2） |
| `Presentation/UIFlowController.cs` | 保存済み状態・遷移・画面別通知・中断処理（2以降、順次変更） |
| `Presentation/AudioManager.cs`、Resources内の7音源 | 常駐再生・ループ・個別音設定（3） |
| `Presentation/QuestMergeScreens.cs`、`MergeBoardView.cs` | 依頼・盤面・実入力（4） |
| `Presentation/DeliveryPresentation.cs`、`RewardGrowthScreens.cs` | 保存済み納品の表示・短い演出（5） |
| `Presentation/SettingsScreen.cs`、`VolumeControl.cs` | 個別トグル・試聴・操作終了時保存（6） |
| `Editor/AndroidBuild.cs`、Windowsスクリプト・手順 | 通常／QA APK、USB転送、公開準備と報告（7） |

表のCore／Presentationは`Assets/Mofumachi/`内。新規Unityファイル・フォルダーには固有GUIDの`.meta`を付ける。新規Presentationは同じフォルダーに置き、既存asmdefと全ソース互換コンパイルに含める。現行2シーンのbootstrap GUIDは維持する。

## 検証コマンドと証拠

作業ディレクトリは`/workspace/mofumachi-merge`。`DOTNET_CLI_HOME=/workspace/runtime/dotnet`、`NUGET_PACKAGES=/workspace/toolchains/nuget`、`DOTNET_CLI_TELEMETRY_OPTOUT=1`を使用する。

- **Core:** `/workspace/toolchains/unity/6000.6.4f1/Editor/Data/DotNetSdk/dotnet test Tests/Core/Mofumachi.Core.Tests.csproj --no-restore`。対象テスト実行、失敗0／skip0が成功。Task1のRED／GREENはここで実行する。
- **Unity API:** 同じ`dotnet build /workspace/setup/mofumachi/compat-fix-work/Unity.Compatibility.csproj --no-restore`。全Core／Presentation／Editor／EditMode／PlayMode／BuildTimeを実際のUnity DLL・内蔵custom NUnitでコンパイルし、エラー0。`Assert.Multiple`を使わない。
- **Windowsネイティブ:** 対象Editorを閉じ、`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-android.ps1`。新しいXMLの全成功、全予定テストの実行、失敗／skip0を確認後、新しいAPK・成功ログ・SHAを生成する。
- Unityのみで実行可能なテストはクラウドでソースをコンパイルし、Windowsで実行する。コンパイル失敗をネイティブ挙動のREDと呼ばず、未実行欄を残す。更新していない旧APKで新機能を合格にしない。

## Task 1: v1進行を保つv2保存と設定トランザクション

**Files:** Modify `Assets/Mofumachi/Core/GameState.cs`, `Assets/Mofumachi/Core/GameStateManager.cs`, `Assets/Mofumachi/Core/SaveService.cs`, `Assets/Mofumachi/Tests/EditMode/SaveServiceTests.cs`; Create `Assets/Mofumachi/Tests/EditMode/PreferencesTests.cs`。

**Interfaces:**
- Consumes: 既存`IStateStore.Save(GameState state)`、`LoadResult Load()`、`GameState.Clone()`、`CopyFrom(GameState other)`。
- Produces: `GameState.CurrentSaveVersion = 2`、`purchaseNoticeAcknowledged: bool`、`bgmVolume: float = .75f`、`seVolume: float = .65f`。`StateCodec.Decode(string json)`はv1をv2へ明示移行し、`Validate(GameState state)`は移行済みv2を検証する。
- Produces: `bool GameStateManager.AcknowledgePurchaseNotice()`、`bool SetAudioPreferences(bool bgm, bool se, float bgmVolume, float seVolume)`。失敗はfalse＋`LastError`。既存`SetAudio(bool bgm, bool se)`は音量を保持して委譲する。

- [x] RED用テストを追加する。v1 fixtureは新フィールドを持たない旧JSONとし、`V1MigrationPreservesCompletedProgressAndMutedAudio`で`coins==30`, `townGrowthLevel==1`, 完了／受領ID・盤面・inventory・受注状態・既存全項目の一致、`bgmEnabled==false`, `saveVersion==2`, `bgmVolume==.75f`, `seVolume==.65f`, `purchaseNoticeAcknowledged==false`をAssert.Thatで検証する。
- [x] `PreferencesRoundTripAndRollback`で確認済み・音量0・OFFを保存／再読込し、失敗するIStateStoreでは設定・確認・timestamp・既存mergeLocksが操作前と一致することを検証する。`InvalidV2VolumeUsesHealthyBackup`は負数／1超／NaN／Infinity／音量欠落／未知versionを拒否し、旧v1バックアップのCoins30を保持する。
- [x] CoreコマンドでREDを記録する。既存のversion1前提テストは「新形式2」と独立した「旧形式1の移行」に分け、未知形式の文字列置換が実際に成立したこともAssertする。
- [x] 上記シグネチャを実装する。DataContractのフィールド初期化に依存せず、v1だけ新値を補完し、v2の欠落音量は検出する。全新項目をClone／CopyFromに含め、確認済み再確認は保存を重複させない。原子的置換と健全バックアップ保持を継続する。
- [x] Core全件とUnity APIコンパイルを実行する。v1移行→確認保存→v2再起動→再納品拒否まで連結して検証する。
- [x] `feat: migrate saves and persist purchase notice and audio preferences`として当タスクのファイルをコミットする。

## Task 2: 全画面レイアウト・正式原画・TOP／ホーム・初回確認

**Files:** Modify `Assets/Mofumachi/Presentation/UIFlowController.cs`, `Assets/Mofumachi/Resources/Mofumachi/UIFont.otf`と同meta, `Assets/Mofumachi/Tests/PlayMode/VerticalSliceFlowTests.cs`, `Assets/Mofumachi/Tests/BuildTime/Mofumachi.BuildTests.asmdef`; Create `Assets/Mofumachi/Presentation/ResponsiveUILayout.cs`, `Assets/Mofumachi/Presentation/UIWidgets.cs`, `Assets/Mofumachi/Presentation/RoundedPanelGraphic.cs`, `Assets/Mofumachi/Presentation/UIIconGraphic.cs`, `Assets/Mofumachi/Presentation/ScreenContext.cs`, `Assets/Mofumachi/Presentation/UIStrings.cs`, `Assets/Mofumachi/Presentation/TopHomeScreens.cs`, `Assets/Mofumachi/Presentation/PurchaseNoticeView.cs`; Create `Assets/Mofumachi/Resources/Mofumachi/TownBackground.png`, `Assets/Mofumachi/Tests/PlayMode/LayoutNoticeTests.cs`, `Assets/Mofumachi/Tests/BuildTime/PresentationContentTests.cs`。

**Interfaces:**
- Consumes: Task1の確認フラグ／`AcknowledgePurchaseNotice()`。既存`UIFlowController.Game`, `Initialize(IStateStore store)`, `Persist()`, `BeginGame()`, `ShowHome()`, `ShowQuest()`, `ShowMerge()`, `ShowSettings()`を維持する。
- Produces: `ScreenId { Title, Home, Quest, Merge, Result, Growth, Settings }`、`UIFlowController.CurrentScreen: ScreenId`, `bool IsPurchaseNoticeOpen`, `bool ConfirmPurchaseNotice()`, `void GoBack()`, `string FeedbackFor(ScreenId screen)`, `void PlayCue(AudioCue cue)`。
- Produces: `ResponsiveUILayout.Initialize(Canvas canvas)`, `ApplyViewport(Vector2 pixels, Rect safePixels)`, `CreatePage(string name): RectTransform`, `FullScreenRoot/SafeRoot/ContentRoot: RectTransform`、`static float CellSize(Vector2 area, float gap = 6f)`。縦のContentRootはSafeRoot全体、横のみ中央9:16。
- Produces: `ScreenContext(UIFlowController flow, UIWidgets widgets)`、読み取り専用`Flow`, `Game: GameStateManager`, `Widgets`。`UIWidgets(Font font, Texture2D characters)`の`Button(string name, string text, RectTransform parent, Rect anchors, UnityEngine.Events.UnityAction action, bool secondary = false): Button`, `Label(string name, string text, RectTransform parent, Rect anchors, int fontSize): Text`, `Character(string name, int index, RectTransform parent, Rect anchors): RawImage`, `Background(Texture2D texture, RectTransform parent): RawImage`。
- Produces: `void TopHomeScreens.BuildTitle(RectTransform root, ScreenContext context)`／`void BuildHome(RectTransform root, ScreenContext context)`、`void PurchaseNoticeView.Build(RectTransform root, ScreenContext context)`。`UIStrings.All: IReadOnlyList<string>`と上記案内の定数。ScreenIdはScreenContext.csへ定義し、新規表示クラスはMofumachi.Presentation namespaceに置く。ビューは報酬・保存を直接変更しない。

- [x] `PortraitUsesFullSafeHeightAndLandscapeUsesCenteredViewport`を360×640／393×808／1080×1920／1080×2220／1440×2960／2560×1440＋疑似ノッチで検証する。Assertは縦ContentRootとSafeRootの四辺一致、背景とcanvasの四辺一致、横ContentRoot比9/16、6原画の比率保持、主要ボタン高さが基準UI単位48以上。Pixelでは端末densityも確認し48dp相当を検証する。現在の「ホーム街絵が高さ38%以下」等の縮小UI前提は更新する。
- [x] `NoticeBlocksUntilSaveSucceedsAndCannotBeBypassed`は実raycastの「はじめる」クリック→案内1個→背景入力遮断→失敗するstoreで未確認／Title維持→成功でGameScene→再起動後非表示を検証する。連打、Android戻る、未確認のGameScene直起動／ShowHomeも検証する。
- [x] `RequiredJapaneseTextHasGlyphsAndFits`で案内の全文と「可年必方相者許課談護量金」、固定UI／保存エラーの字体を検査する。最大Coins／街Lv.でもラベル領域へ収まり、フォントサイズを自動縮小しないことを検証する。クラウドはUnity APIコンパイル、挙動REDはWindows未実行として記録する。
- [x] 共通部とTOP→案内→ホームの順で実装する。背景は承認済み`town-background-proposal.png`の同一バイトコピー、キャラは既存masterのUV表示。角丸・影・控えめなツヤ・短い押下反応をUGUIのGraphicで描き、アイコンはUI図形として描く。画像のフィット対象とレイアウト枠を分け、画像にクリックを遮らせない。
- [x] UILabel文言とコアの日本語エラーを含め、元の完全版`/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc`のJP faceからOFLフォントを再subsetし、Mofumachi UI名称／ライセンスを保持する。UIFontの現行subsetに欠落字を補おうとせず、完全版から作る。BuildTime asmdefへMofumachi.Presentation参照を追加する。確認は保存成功を遷移条件とし、未確認ならホーム以降の入口を遮断する。画面別の通知を導入し、納品エラーを設定へ持ち越さない。
- [x] Unity APIコンパイル、原画SHA一致、meta/GUID・素材・字形検査を実行する。PlayMode fixtureではcontrollerのcallbackを止めてからsessionを戻す既存順序を保つ。
- [x] `feat: build full-screen title home and first-use notice`としてコミットする。

## Task 3: 録音BGM／6SEと常駐AudioManager

**Files:** Modify `Assets/Mofumachi/Presentation/AudioManager.cs`, `Assets/Mofumachi/Presentation/UIFlowController.cs`, `Assets/Mofumachi/Tests/PlayMode/VerticalSliceFlowTests.cs`, `Assets/Mofumachi/Tests/BuildTime/PresentationContentTests.cs`; Create `Assets/Mofumachi/Resources/Mofumachi/Audio/bgm-town-loop.ogg`, `Assets/Mofumachi/Resources/Mofumachi/Audio/se-confirm.ogg`, `Assets/Mofumachi/Resources/Mofumachi/Audio/se-character.ogg`, `Assets/Mofumachi/Resources/Mofumachi/Audio/se-merge.ogg`, `Assets/Mofumachi/Resources/Mofumachi/Audio/se-delivery.ogg`, `Assets/Mofumachi/Resources/Mofumachi/Audio/se-reward.ogg`, `Assets/Mofumachi/Resources/Mofumachi/Audio/se-growth.ogg`とmeta; Create `Assets/Mofumachi/Tests/PlayMode/AudioManagerTests.cs`, `docs/audio-assets.md`。

**Interfaces:**
- Consumes: GameStateのON/OFF／音量、Task2の`UIFlowController.PlayCue(AudioCue cue)`。
- Produces: `AudioCue { Confirm, Merge, Reward, Character, Delivery, Growth }`（既存4名を維持）、`AudioManager.Instance: AudioManager`、`static AudioManager GetOrCreate()`、`void ApplySettings(bool bgm, bool se, float bgmVolume = .75f, float seVolume = .65f)`、既存`void PlaySe(AudioCue cue)`。
- Produces: Resourcesキー`Mofumachi/Audio/bgm-town-loop`と`se-confirm/character/merge/delivery/reward/growth`。BGMはStreaming、SEはDecompressOnLoad、2D再生、個別AudioSource。

- [x] `SceneAndSettingsChangesKeepOneAudioManagerAndTrackPosition`でInstance1個／音用AudioSource2個／追加AudioListener重複なし、設定表示とScene再読込でも同じBGMclip・再生位置を維持することを検証する。
- [x] `AudioChannelsMuteAndResumeIndependently`でBGM／SE各OFF、音量0／1、中断・復帰を検証し、一方のOFFが他方を止めないことをAssertする。fixtureで常駐managerも停止／破棄してからsession／保存ディレクトリを戻す。クラウドはソースコンパイルのみ、実音再生の成功はWindows／Pixelで記録する。
- [x] 承認済み原曲／録音サンプルを使い、終止付き確認BGMを連続ループ用にレンダリングする。CC0出典・固定revision／SHA・原曲の対応を残す。無音ダミーやオシレーターを用いず、FFmpegデコード、有限サンプル、ピーク≤-3dBFS、長さ、ループ境界差分を検査する。
- [x] 上記AudioManagerを実装し、仮Compose関数と生成clip破棄処理を削除する。シーン所有controllerに音源を付けず、DontDestroyOnLoadの一つだけを使う。BGMの中断はPause／UnPauseで処理し、描画更新による再生開始を避ける。
- [x] UIFlowControllerの音設定適用を4引数へ接続し、押下音は有効な決定時のみ。6SEの再生イベントは以後の画面タスクで接続する。Unity APIコンパイル／音源import設定／出典／ハッシュ検査を実行する。
- [x] `feat: replace provisional synthesis with recorded music and sound effects`としてコミットする。Pixelのループ継ぎ目／スピーカー音質は未確認として残す。

## Task 4: 依頼画面と正方形Merge盤面の実入力

**Files:** Modify `Assets/Mofumachi/Presentation/UIFlowController.cs`, `Assets/Mofumachi/Presentation/MergeBoardView.cs`, `Assets/Mofumachi/Presentation/UIStrings.cs`, `Assets/Mofumachi/Tests/PlayMode/VerticalSliceFlowTests.cs`; Create `Assets/Mofumachi/Presentation/QuestMergeScreens.cs`, `Assets/Mofumachi/Tests/PlayMode/MergeInteractionTests.cs`。

**Interfaces:**
- Consumes: 既存`bool AcceptQuest()`, `bool DropItem(int from, int to)`, `MergeBoard.At(int cell): BoardItem`, `IsLocked(int cell): bool`, `QuestManager.CheckDelivery(): bool`, `RefreshProgress()`。Task2のScreenContext／UIWidgets／CellSizeとTask3のMerge／Character cue。
- Produces: `void QuestMergeScreens.BuildQuest(RectTransform root, ScreenContext context)`、`void BuildMerge(RectTransform root, ScreenContext context)`、既存`MergeBoardView.Initialize(UIFlowController flow, int index)`とEventSystemのdrag handlerを維持する。

- [ ] `PointerDragAndTwoTapsProduceOneMerge`で実raycast→beginDrag／drag／endDragを通し、Lv.1二つ→Lv.2一つ、ドラッグ末尾のclickと合成中の連打は追加変更なし。別テストで2タップ空セル移動、盤面外drop、異なるLv.／最大Lv.／ロックセルの拒否を検証する。
- [ ] `BackgroundingOrLeavingMergeClearsOnlyTransientInput`でドラッグghost／選択／coroutine／ロックが消え、保存済みアイテムが保持されることをAssertする。`ShortPortraitBoardStaysSquareAndOperable`は30セルが正方形・48以上・Safe Area内で、下の生成／納品ボタンに重ならないことを検証する。
- [ ] クラウドではUnity APIコンパイルで追加テストを検査し、Windowsで挙動RED／GREENを確認する。コアの既存意味を変えて表示テストだけを通さない。
- [ ] 依頼→Mergeの順で実装する。依頼は原画カード・必要数／所持数・30 Coins、受注／完了／納品可能を実状態から表示する。盤面は利用可能幅／高さの小さい方から寸法を決め、アイテムを読みやすい茶アイコン＋Lv.で表示する。ドラッグ成功とクリックを重複処理せず、合成中は再入力を拒否する。
- [ ] 短いMerge演出後にReleaseLocks→RefreshProgress→Saveを行い、画面移動・pauseでも解放する。お茶生成は既存AddItemを使い、保存失敗時に成功音／成功表示を出さない。納品ボタンはCheckDeliveryが真の時だけ有効にする。
- [ ] Core全件／Unity APIコンパイル、30セル・文字・metaを検証し、`feat: rebuild quest and merge screens with safe touch input`としてコミットする。

## Task 5: 保存済み納品から報酬・街成長を表示

**Files:** Modify `Assets/Mofumachi/Presentation/UIFlowController.cs`, `Assets/Mofumachi/Presentation/UIStrings.cs`, `Assets/Mofumachi/Tests/PlayMode/VerticalSliceFlowTests.cs`; Create `Assets/Mofumachi/Presentation/DeliveryPresentation.cs`, `Assets/Mofumachi/Presentation/RewardGrowthScreens.cs`, `Assets/Mofumachi/Tests/PlayMode/RewardGrowthTests.cs`。

**Interfaces:**
- Consumes: 既存`DeliveryResult UIFlowController.Deliver()`／`QuestManager.TryDeliver()`、保存済みcoins／townGrowthLevel／完了ID。Task2〜4の表示と音。
- Produces: 不変`DeliveryPresentation(int coinsAwarded, int previousTownLevel, int currentTownLevel)`、読み取り専用`CoinsAwarded/PreviousTownLevel/CurrentTownLevel: int`。`UIFlowController.LastDelivery: DeliveryPresentation`、`void ShowGrowth()`。`void RewardGrowthScreens.BuildResult(RectTransform root, ScreenContext context, DeliveryPresentation delivery)`／`void BuildGrowth(RectTransform root, ScreenContext context, DeliveryPresentation delivery)`。

- [ ] `DeliveryClicksAndPresentationReentryNeverPayTwice`で不足時の完全不変、正常納品のcoins30／街Lv.1／完了ID1個／受領ID1個、二回目拒否、結果再描画／設定往復／街成長再表示で同じ値をAssertする。
- [ ] `PauseOrTerminateDuringRewardResumesCommittedProgress`で報酬表示前・街成長途中に中断し、再ロード後coins30／街Lv.1／完了ID1個、アニメcallbackによる新支払いなしを検証する。確認済みフラグも保持する。
- [ ] クラウドのUnity APIコンパイルとCoreの既存二重報酬／保存失敗テストを実行し、Windowsの新演出テストは未実行として区別する。
- [ ] 納品／報酬→街成長の順で実装する。成功したTryDeliverの前後差だけをDeliveryPresentationに記録し、直ちに納品cue、報酬表示時にReward cue、街成長開始時にGrowth cueをそれぞれ一回再生する。拒否／書込失敗は元画面とその画面の通知へ戻す。
- [ ] ビューは状態を読み取るだけにし、coins付与／街成長保存を再実行しない。cueを既に鳴らしたかはcontrollerの一時状態で管理し、ビュー再生成で再生し直さない。短い表示演出はpause・遷移・destroyで止め、ホームに保存済みレベルを反映する。再起動後は保存済みホームへ戻し、演出の進度を新しいセーブ項目として増やさない。
- [ ] Core全件／Unity APIコンパイルを検証し、`feat: present committed delivery rewards and town growth`としてコミットする。

## Task 6: 音量設定・画面への復帰・エラー分離

**Files:** Modify `Assets/Mofumachi/Presentation/UIFlowController.cs`, `Assets/Mofumachi/Presentation/UIStrings.cs`, `Assets/Mofumachi/Tests/PlayMode/AudioManagerTests.cs`; Create `Assets/Mofumachi/Presentation/SettingsScreen.cs`, `Assets/Mofumachi/Presentation/VolumeControl.cs`, `Assets/Mofumachi/Tests/PlayMode/SettingsScreenTests.cs`。

**Interfaces:**
- Consumes: Task1の`SetAudioPreferences(bool bgm, bool se, float bgmVolume, float seVolume)`、Task3のApplySettings、Task2のFeedbackFor／GoBack。
- Produces: `void SettingsScreen.Build(RectTransform root, ScreenContext context)`、`void VolumeControl.Bind(Slider slider, System.Action<float> preview, System.Action<float> commit)`、`void CancelPending()`。UIFlowControllerに`PreviewAudioVolumes(float bgmVolume, float seVolume): void`、`CommitAudioPreferences(bool bgm, bool se, float bgmVolume, float seVolume): bool`。

- [ ] `SliderGesturePreviewsButCommitsOnlyOnce`でvalue変更中は音源のみ変化／保存0回、pointerUp＋endDragは保存1回、再起動値一致をAssertする。クリック／keyboard submitにも対応する。`VolumeZeroAndOffRemainDistinct`はOFF→ONで同じ保存音量へ戻ることを検証する。
- [ ] `FailedOrInterruptedVolumeEditRestoresSavedAudioAndUi`で書込失敗／ドラッグ中のpause／画面退出を検証し、音源・slider・toggle・GameStateが保存済み値と一致することをAssertする。未完了gestureは保存せず戻す。
- [ ] `SettingsReturnsToOriginWithoutDeliveryError`でTOP／ホーム／依頼／Merge／結果／成長からの設定往復、セーブエラー表示、納品エラー非表示を検証する。短い縦画面でも本文と戻るをスクロールで読め、BGM／SEの48以上の操作領域を確保する。
- [ ] クラウドのUnity APIコンパイルでテストを検査し、設定画面を実装する。BGM／SE個別toggleと0〜100%slider／数値表示、設定を開いた画面へ戻る操作を持たせる。sliderの連続変更で画面を破棄・再生成せず、SEを連打しない。
- [ ] Toggleは保存成功後に適用し、sliderは試聴→gesture終了で原子的保存、失敗はUIも音も戻す。画面別通知と保存復旧の案内を区別する。Core全件／Unity APIコンパイルを検証する。
- [ ] `feat: add separate audio volume controls with reliable persistence`としてコミットする。

## Task 7: Windows検証ゲート・通常／QA APK・USB・Play準備

**Files:** Modify `Assets/Mofumachi/Editor/AndroidBuild.cs`, `Assets/Mofumachi/Tests/BuildTime/AndroidBuildTests.cs`, `Assets/Mofumachi/Tests/BuildTime/PresentationContentTests.cs`, `scripts/build-android.ps1`, `docs/android-smoke-test.md`, `docs/android-preparation-status.md`, `README.md`; Create `scripts/install-pixel3a.ps1`, `scripts/tests/android-tools.tests.ps1`, `docs/google-play-release-readiness.md`, `docs/pixel3a-quality-status.md`, `References/Release/icon-proposal.png`, `References/Release/feature-graphic-proposal.png`, `References/Release/README.md`。

**Interfaces:**
- Consumes: Task1〜6、既存`AndroidBuild.ConfigurePixel3a()`、`ValidateContent()`、`BuildDevelopmentApk()`、WindowsのUnity／SDK。
- Produces: `AndroidBuild.BuildQaApk(): void`（メニュー／batch双方）、`QaApplicationId = "com.mofumachi.merge.qa"`。成功marker`MOFUMACHI_APK_SUCCESS`を維持。通常版は`vertical-slice.apk`、QA版は`vertical-slice-qa.apk`、既存`-mofumachiApkPath`を維持する。
- Produces: `build-android.ps1 [-UnityEditor <path>] [-Qa]`、`install-pixel3a.ps1 -Apk <path> [-Adb <path>] [-Serial <serial>] [-PackageName com.mofumachi.merge|com.mofumachi.merge.qa]`。`-Qa`はBuildQaApkへ接続し、QA build終了時／失敗時に通常のappId／productName設定を戻す。
- Produces: 各実行フォルダーのAPK／XML／ログ／SHA／`build-info.json`（Git revision、Unity、appId、versionCode、APK SHA、サイズ、日時）。Git revisionを取得できなければ不明と記録し、推測値を入れない。転送はUnity同梱SDKのbuild-tools36.0.0のaapt2でAPK実パッケージと指定IDを照合し、端末指定・更新install・起動・保存を消さないforce-stop復帰の手順を提供する。

- [ ] `ContentGateRejectsMissingBackgroundAudioOrJapaneseGlyphs`は背景／7clip／UIFont／bootstrapの欠損を拒否する。`QaBuildIdentityIsIsolatedAndRestoredAfterFailure`は別IDと通常設定復元をAssertする。API36／26／ARM64／IL2CPP／縦向きの既存テストも継続する。
- [ ] PowerShellの実行ゲートを検証する。`RejectsMissingFailedSkippedOrIncompleteTestXml`は今回の予定件数と必須suiteを満たさないXMLを拒否する。`InstallTargetsExactlyOneAuthorizedDeviceAndCorrectPackage`は空／unauthorized／複数端末／wrong APK／install失敗を拒否し、全コマンドの`-s`指定と空白pathを検査する。mock ADB結果の検査を実機成功と呼ばない。
- [ ] ビルドとインストールを実装する。新しい全テストの件数をゲートへ反映し、古い40／5を新しい全件合格と扱わない。ASCII project path、今回のXML、プロセス終了コード、今回のAPK、marker、SHAを検証する。署名不一致時は停止し、uninstall／pm clearを自動実行しない。
- [ ] Windows／Pixel手順を改訂する。USBデバッグ許可→`device`／ARM64／API≥26→通常版`install -r`→初回案内→旧coins30／街Lv.1／完了・音OFFの維持→再起動で案内非表示。別QA版は初期coins0から案内→受注→不足状態の納品不可・無消費→drag／2tap→Merge→正常納品→30 Coins→街Lv.1→再納品拒否→音各設定→中断／再起動を順に記録する。
- [ ] Pixelの全7画面・初回案内を1080×2220実機で確認し、文字切れ・余白・ボタン／ドラッグ・通知・音割れ・ループ継ぎ目・再起動を表へ記録する。新APKのSHA／端末API／日時／スクリーンショット／logcatと対応させる。Windows／実機をこちらで実行できない場合は「手順整備済み、実行待ち」と明記する。
- [ ] Play最小素材を準備する。承認済み街の世界観に沿うキャラなしアイコン512×512・feature graphic1024×500を候補として作り、原画を再描画しない。ストア説明案、名称／appId／現行version1.0・code1と将来の更新規則、OFL／CC0出典、privacy／Data safetyの実データ監査、非Developmentリリース設定、AAB／署名・Console要件の残項目を文書化する。実機完成画面をストア写真に使い、モック画像を実機写真と呼ばない。
- [ ] Core全件、Unity API全ソースコンパイル、PowerShellゲート、素材／font／meta／原画SHA、diff-checkを実行する。major変更の独立コードレビューで具体的な重要問題を修正し、修正に関係する検証を再実行する。
- [ ] `docs/pixel3a-quality-status.md`へ変更ファイル一覧／実装内容／未解決事項／Windowsビルド手順／Pixel確認項目と証拠をまとめ、`build: prepare validated Pixel 3a APK and USB testing workflow`としてコミットする。

## 完成の扱い

ソース実装とクラウド検証が通っても、Windowsの新ネイティブ結果・新APK・Pixelの一周／保存復帰／画面／音が未実行ならVertical Slice安定完成とは報告しない。正式全身／歩行素材、完成実機スクリーンショット、最終音質、公開用署名／AAB・Console／privacyの確認は、実施したものと未実施を分ける。機能を増やして未解決事項を隠さず、問題が出た画面・保存・入力・音を優先して直す。
