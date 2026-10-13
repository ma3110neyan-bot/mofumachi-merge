# もふまち：街の広がり・立体Merge・文章UI — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. ユーザーが以前指定した「Codexが順に実装し、最後に独立レビュー」を維持する。

**Goal:** 9月22日の見やすさと世界観に現在の演出を統合し、成長で歩く場所が増える街、目的が分かる立体Merge、絵本調の文章UIを完成させる。

**Architecture:** 進行データを変更せず、保存済み街Lv.を読む地点カタログと、一時的な背景UVカメラを追加する。水・キャラ・入口・装飾・指入力は同じ背景座標に従う。Mergeは既存QuestManagerの所持数と既存の生成・納品・整理処理を使い、表示と案内だけを組み替える。

**Tech Stack:** Unity 6000.6.4f1、UGUI、既存Input System 1.20.0、C# 9、NUnit、Kiwi Maru Medium／Zen Maru Gothic Medium・Bold（SIL OFL1.1）。追加パッケージなし。

**Spec:** [最新指定と2点の調整を反映した設計](../specs/2026-10-13-integration-baseline-review.md)。実装時は必ず両方を読む。

**状態:** ユーザーの再承認も同じ7工程の継続として反映。工程1〜6のソース／設計、独立レビューと2テストの修正を`3405c26`まで反映済み。Core110／演出計算10／6アセンブリコンパイル／字形／モック検査は成功。最新のユーザー提供画像でWindows PlayMode99件成功・失敗0・skip0を確認し、Town QA APK生成完了の報告を受領。EditMode121件の実結果・結果XML・APK生成時刻／SHA・Pixelの試遊と比較は未確認。第7工程を継続し、完成済みのソースを作り直さない。[実結果と引き継ぎ](../../development-handoff-2026-10-13-integrated-quality.md)。

## Global Constraints

- 順番は①街→②Merge→③フォント→④2.5D設計。7工程はこの順番を具体化したもの。
- ビジュアルの最上位は9月22日の基準画像。復元ではなく、承認済み改善と統合する。
- 正式6名はモモ／ルル／ポム／フィオ／ミエル／ノア。顔・配色・衣装・体型、正式ロゴ、現在の街背景・アイテム原画を変更しない。
- 5×6、3系統×Lv.1〜3、同種同Lv.合成、既存保存v3／納品／報酬を維持。
- GameState／GameStateManager／MergeBoard／SaveService／QuestManager／RewardService／TownGrowthService／ItemCatalog／QuestCatalog、Packages／ProjectSettingsは変更しない。
- 生成枠一括片づけと再配置を保持。盤面アイテムの単品片づけと区別し、既存の確認と保存失敗時の全復元を使う。
- カメラ・散歩・会話・不足カードによる系統選択は一時状態。これら自体で保存や報酬を発生させない。
- 街Lv.0〜1／2〜3／4〜5／6以上で散歩場所を拡張。既存の依頼部屋とショップ入口は初期から利用可能。
- カメラは同じ背景内、初期ズーム候補1.10・上限1.15、更新最大30Hz。元画像の外を映さない。
- 合成の操作ロック0.22秒、光0.5秒、選択1.06倍を維持。演出で入力復帰を遅らせない。
- 操作面積とセルは最小48 Canvas単位、名前12・本文14を基本とし、日本語をBestFitで縮小しない。小さなLv.・数値の例外は可読性を別途検査。
- 基準の件数はEditMode105（Core94＋Build11）／PlayMode85。既存テストを削除・Ignoreせず、UIの名前変更では探索先のみ更新して検査目的を残す。
- 2.5Dは設計・試作範囲の提案まで。BGM選曲、Coins／Gemsショップ、新施設の購入保存、新背景はこの計画へ追加しない。

## Review Focus

- HomeのカメラUVがTOP・Merge・結果・設定へ漏れないこと。工程2の離脱／復帰テストで固定する。
- 主人公と空いた街面の二つの指、リサイズ、pause時に入力所有権が残らないこと。工程2の入力排他テストで固定する。
- 高Lv.の既存セーブ・旧セーブでも元の入口と依頼を失わず、カメラ／会話に保存書込みがないこと。工程1・3で固定する。
- 未受注、合成ロック中、生成枠を全部片づけた状態でも不足案内が進行を誤認しないこと。工程4で固定する。
- 最短の縦画面と最大Coins、長い説明・保存失敗文でも文字を欠かず、操作面積を保つこと。工程4・5で固定する。

## ファイルの責務と変更範囲

| 対象（Assets/Mofumachi配下） | 責務 |
|---|---|
| 新規Core/TownPlaceCatalog.cs＋.meta | TownPlace／TownStreetMap、街Lv.別の読み取り専用地点・道・活動・案内。保存へ依存しない |
| 新規Core/TownViewport.cs＋.meta | TownViewportRect／TownViewport、背景UV範囲・移動・画面内へ収める計算。Unityへ依存しない |
| 新規Presentation/TownViewportView.cs＋.meta | Home専用のUV所有権、空いた街面のドラッグ、主人公の案内追従、入力解放と背景復元 |
| Presentation/TopHomeScreens.cs／TownLifeView.cs／TownActor.cs | コンパクトHUD、場所別お散歩、休憩・交流、名前・吹き出し・施設案内、カメラ接続 |
| Presentation/TownAmbientGraphic.cs／TownGrowthView.cs | 水・成長装飾を同じUVへ追従させる。既存軽量メッシュ・保存済み装飾を保持 |
| Presentation/QuestMergeScreens.cs／UIFlowController.cs | 不足カード、単一の生成操作行、合成・整理・納品への案内。既存の処理を呼び出す |
| Presentation/UIWidgets.cs／UIStrings.cs | 背景の通常表示への復元、書体の役割、コンパクトUIと新しい案内文 |
| Presentation/RewardGrowthScreens.cs／SettingsScreen.cs／MergeActionMenuView.cs／DiscardConfirmationView.cs | 説明と補足の書体・行数。保存や支払い処理は変えない |
| Resources/Mofumachi/UIFont*.ttf | 必要な新文字がある場合だけ3書体を補完。元TTFのSHA／OFL／GUIDを保持 |
| 新規Tests/EditMode/TownPlaceCatalogTests.cs／TownViewportTests.cs＋各.meta | 地点とカメラの純粋計算の回帰 |
| 新規Tests/PlayMode/TownViewportPresentationTests.cs／TownHabitTests.cs＋各.meta | 共通座標・入力・生活感・保存非変更の実UI回帰 |
| 既存Tests/PlayModeのTownLifeTests／TownMotionPresentationTests／LayoutNoticeTests／MergeProgressionUiTests／MergePolishTests／MergeInteractionTests／StoryTypographyTests | レイアウト変更に対応し、既存の操作・中断・保存失敗検査を維持 |

上の表と各工程のCore／Presentation／Tests／Resourcesの相対パスは、すべて`Assets/Mofumachi/`を先頭に付ける。工程内でファイル名だけを記す場合も、この表にある同名の対象を指す。

追加する資料：`docs/town-walk-map-2026-10-13.md`、`docs/2point5d-staged-prototype-2026-10-13.md`、`docs/integrated-quality-pixel3a-checklist.md`、終了時引き継ぎ。README、Referencesの基準説明、`scripts/build-android.ps1`と`scripts/tests/android-tools.tests.ps1`は実際の検査構成に合わせて更新する。インストーラーと依存パッケージは変更しない。

## 検証コマンドと実施の区別

クラウドでは既存の隔離された`/workspace/mofumachi-merge`を使い、追加worktree・依存の更新は行わない。環境確認スキルによる既存ツールを再利用する。

```bash
DOTNET_CLI_HOME=/workspace/runtime/dotnet NUGET_PACKAGES=/workspace/toolchains/nuget /workspace/toolchains/unity/6000.6.4f1/Editor/Data/DotNetSdk/dotnet test Tests/Core/Mofumachi.Core.Tests.csproj --no-restore --nologo
DOTNET_CLI_HOME=/workspace/runtime/dotnet NUGET_PACKAGES=/workspace/toolchains/nuget /workspace/toolchains/unity/6000.6.4f1/Editor/Data/DotNetSdk/dotnet test Tests/Presentation/Mofumachi.Visual.Tests.csproj --no-restore --nologo
python3 /workspace/setup/mofumachi/verify_asmdefs.py
python3 scripts/subset-ui-fonts.py --check --source-dir /workspace/setup/mofumachi/chapter-font-sources
```

必要なテストだけを先に絞る場合は`dotnet test`へ`--filter FullyQualifiedName~TownPlaceCatalogTests`または`~TownViewportTests`を付ける。WindowsではUnity Test Runnerで該当suiteを選びRun Selected、最後はEditMode／PlayModeのRun AllとExport Results。純粋計算の成功・実DLLコンパイル・テスト属性の件数確認は、Unityネイティブの成功とは別記する。ネイティブを実行できない環境では赤／緑の実行を未実施と記録し、Windows全件検証をAPK生成の前提にする。

---

### 工程1：街Lv.で増える道と居場所

**Files:** 新規Core/TownPlaceCatalog.cs＋.meta、新規Tests/EditMode/TownPlaceCatalogTests.cs＋.meta、Presentation/TownLifeView.cs、`docs/town-walk-map-2026-10-13.md`。

**Interfaces:**
- 定義：`TownActivity`はSquare／Bench／Flowers／HomeFront／ShopFront／Fountain／Celebration／Entranceのenum。`TownPlace`はId／Name／Dialogue（string）、Position（TownPoint）、MinLevel（int）、Activity（TownActivity）を持つ読み取り専用値。`TownStreetMap`はNodes（IReadOnlyList<TownPoint>）／Edges（IReadOnlyList<TownEdge>）／Places（IReadOnlyList<TownPlace>）を持つ。
- Produces: `TownPlaceCatalog.ForLevel(int level): TownStreetMap`、`TryFind(TownStreetMap map, string id, out TownPlace place): bool`、既存座標と同じRequestEntrance／ShopEntrance。
- Consumes: 既存`TownNavigation(IReadOnlyList<TownPoint>, IReadOnlyList<TownEdge>, float)`と保存済みtownGrowthLevel。半幅.028を維持。

- [x] **通行図を作る。** 元のTownBackgroundを参照し、既存8地点・8辺を保持して、`bench-left`／`bench-right`／`home-front`（Lv.0）、`flower-walk`（Lv.2）、`front-promenade`（Lv.4）、`celebration`（Lv.6）への枝を定義。`quest`／`shop`は常時利用可能。足元・通路幅・噴水／水路／建物／植込みの除外範囲を資料に記録する。背景PNGは編集しない。
- [x] **失敗する8テストを書く。** `ExistingEntrancesRemainAvailableAtLevelZero`、`EveryStageHasConnectedRoutes`、`HigherLevelsRetainEarlierStopsAndEdges`、`StreetSegmentsAvoidSurveyedObstacles`、`NewStopsHaveExpectedUnlockLevels`、`InvalidLevelsUseSafeDisplayStages`、`UnknownPlacesAreRejected`、`CatalogCannotMutateGameState`。経路を独立した通行図の除外範囲で検査し、カタログ自身の数値を期待値に写すだけの検査にしない。
- [x] **Coreの対象suiteを実行して失敗を確認する。** 現在存在しないカタログへの参照または解放条件の検査で失敗すること。別の環境問題での失敗と区別。
- [x] **最小のカタログを実装してTownLifeViewへ接続する。** Lv.を0〜6へ正規化し、既存の入口・経路を閉じない。既存の花を見る場所は`flowers`、噴水は`fountain`として初期から残し、Lv.2の`flower-walk`は別の追加訪問先にする。新建物・購入状態・保存フィールドを追加しない。
- [x] **Coreと全6asmdefを検証する。** 8件と既存94件の成功を確認。通行図はUnity／Pixelで見た目も検証する対象として残す。
- [x] **独立した成果をコミットする。** `feat: expand town walks from existing growth levels`。道が増えることと実画面の未確認を別記。

### 工程2：同じ背景内を巡るカメラ

**Files:** 新規Core/TownViewport.cs／Presentation/TownViewportView.cs＋各.meta、新規Tests/EditMode/TownViewportTests.cs／Tests/PlayMode/TownViewportPresentationTests.cs＋各.meta、TopHomeScreens.cs、TownLifeView.cs、TownActor.cs、TownAmbientGraphic.cs、TownGrowthView.cs、UIWidgets.cs、UIFlowController.cs（中断時の新ビュー停止のみ）。

**Interfaces:**
- 定義：`TownViewportRect`はX／Y／Width／Height（float）のreadonly struct。`TownViewport(float sourceAspect)`は純粋計算モデル。
- Produces: `TownViewport.UvRect`、`Configure(float displayAspect, float zoom): bool`、`TryPan(float normalizedDeltaX, float normalizedDeltaY): bool`、`TryFrame(TownPoint point, TownViewportRect visibleFrame): bool`。無効な数値・ゼロサイズはfalseで状態を保持。
- View: `TownViewportView.Bind(ScreenContext c, RawImage background, TownLifeView life)`、`Focus(TownPoint point)`、`Advance(float elapsedSeconds)`、`CancelGesture()`（各void）。`OwnedPointerId: int?`／`IsDragging: bool`は読み取り専用。ビューはUGUIのPointerDown／BeginDrag／Drag／EndDrag／PointerUpを受ける。
- 連携：`TownLifeView.Background: RawImage`、`RefreshViewport()`、`TownActor.SetViewScale(float scale)`、`TownAmbientGraphic.NotifyViewportChanged()`、`BackgroundCover.FitNow()`。既存Local／Sourceは同じRawImageのUVを参照し続ける。

- [x] **純粋計算8件・実UI4件の失敗するテストを書く。** 純粋計算は`CoverPreservesSourceAspect`、`PanNeverLeavesOriginalImage`、`PanAndProjectionRoundTrip`、`FrameKeepsHeroInVisibleWorld`、`ImpossibleFrameClampsWithoutTeleport`、`ResizePreservesAndClampsCenter`、`InvalidInputsLeaveViewUnchanged`、`ZoomCannotExceedApprovedLimit`。実UIは`CameraKeepsWaterActorsMarkersAndGrowthAligned`、`ActorAndCameraGesturesHaveOneOwner`、`PauseModalAndResizeReleaseCameraInput`、`LeavingHomeRestoresBackgroundAndDoesNotSaveCamera`。
- [x] **保存の期待値を分ける。** カメラ操作そのものはWrites増加0・進行状態不変。Home離脱・設定への移動・pauseは既存Persistの保存回数を期待し、cameraによる追加保存がないことを検査する。既存保存によるタイムスタンプ更新を「カメラが進行を変更した」と判定しない。
- [ ] **対象のCore suiteを実行して赤を確認する。** 実UI4件はUnityで実行し、実施できない場合は未実施と記録。
- [x] **カメラ計算を実装する。** 基本UVは画面と元画像のアスペクトからCover計算し、幅・高さをzoomで割る。中心は画像内に制限。移動量は表示面で正規化したdelta×UVサイズ。TryFrameのvisibleFrameはHUD・下部UI・名前・体の余白を除いた実領域を背景面へ変換したもの。
- [x] **Home専用のビューを接続する。** 初期zoom候補1.10、上限1.15。Homeの間だけBackgroundCoverの書込みを停止し、離脱時は有効化＋FitNowで通常UVへ復元。カメラの入力面はキャラ・施設・HUDより背面の空いた街面に限定する。
- [x] **一時入力と共通位置を接続する。** 1pointerだけを所有。主人公の手動ドラッグを優先してカメラの新規ドラッグを拒否。手動操作中は自動追従を停止し、施設案内中とリリース後は主人公を可視領域へ収める。NPCは追従対象にしない。Advanceは街の移動更新後・キャラ描画前に呼び、最大30Hz。RefreshViewportは札・吹き出し等を再配置し、移動状態や補間位置をリセットしない。水のメッシュを更新通知し、成長ビューはlife.BackgroundのUV変化を追跡して再配置。サイズの倍率は基準サイズから計算し、繰り返し乗算しない。
- [ ] **Core／asmdef／実UIを検証してコミットする。** 全て同じUV・指の往復変換、画面離脱、カメラによる追加保存0を確認。実機未実施は残し、`feat: add bounded town camera with shared coordinates`。

### 工程3：街の生活感・成長案内・交流

**Files:** TopHomeScreens.cs、TownLifeView.cs、TownActor.cs、TownGrowthView.cs、UIStrings.cs、新規Tests/PlayMode/TownHabitTests.cs＋.meta、既存TownLifeTests.cs／TownMotionPresentationTests.cs／LayoutNoticeTests.cs。

**Interfaces:**
- Consumes: 工程1のTownStreetMap／TownPlace、工程2のTownViewportView。
- Produces: `TownLifeView.GuideToPlace(string id): bool`、`TryBeginConversation(TownActor first, TownActor second): bool`。既存GuideToFacilityはquest／shopへ、GuideToRestStopはbench→bench-right／flowers→flowers／fountain→fountainの互換入口として残す。
- Runtime only: TownLifeViewが地点の予約と会話cooldownを所有。TownActorは目的地・待機・表情の一時状態を持つ。

- [x] **4つの失敗する実UIテストを追加する。** `NpcsUseDifferentStopsAndResumeAfterWaiting`、`NearbyConversationBelongsToItsActorsAndDoesNotSave`、`GrowthRestoresExpandedStopsFromSavedLevel`、`NamesBubblesAndFacilitiesStayInsideSafeArea`。Store.WritesとStateCodec.Encodeを散歩・会話の前後で比較する。pauseに伴う既存保存は別の期待値にする。
- [ ] **Unityの該当suiteで赤を確認する。** 既存の指操作・移動補間・施設到着テストを先に残して確認。
- [x] **NPCの居場所を分散する。** 未予約の訪問先を選び、地点別に待機と3表情を使う。近接時は主人公を優先、NPC同士は正式並び順で優先を決める。双方が永久に待たないよう待機期限を設け、通路外への押出しはしない。
- [x] **近い2名の短い交流を接続する。** 一度に1組、会話cooldown8秒、停止は最大1秒。吹き出しを話すキャラの近くへ配置して画面内へ制限。主人公の手動入力・modal・pause時は交流を開始しない。
- [x] **HUD・名前・成長案内を整える。** 街Lv.とCoinsを小さい横並びへ、依頼は下の1カードへ。名前は主人公・会話・滞在中を優先。大きな常設施設案内行はタップ時の行き先と入口へまとめ、操作面積48を維持する。Lv.2／4／6の新しい散歩場所と既存の花・リボン・アーチ・ガーランドを案内する。
- [ ] **既存回帰と新4件を確認してコミットする。** 店先へ歩く導線と販売実装を混同せず、真の座りポーズ・手足歩行は未制作と明記。`feat: add town routines conversations and visible growth`。

### 工程4：立体Mergeと画像付き不足案内の統合

**Files:** QuestMergeScreens.cs、UIFlowController.cs、UIWidgets.cs、UIStrings.cs、MergeProgressionUiTests.cs、MergePolishTests.cs、MergeInteractionTests.cs、LayoutNoticeTests.cs。既存ItemGraphic／MergeEffectsView／MergeSparkleGraphic／MergeVisualTimingは再利用し、動作定数・原画を変更しない。

**Interfaces:**
- Consumes: `QuestManager.GetProgress(QuestDefinition)`／CheckDelivery、既存GenerateFromFrame／ChooseProductionItem／一括整理／単品片づけ／Deliver。
- Produces: `UIFlowController.FocusRequirement(string itemId): bool`。受注中の要求品だけを案内し、未受注は依頼画面へ。合成中は案内変更を拒否し、勝手な生成・再配置・納品をしない。

- [x] **4つの失敗する実UIテストを追加する。** `RequiredArtCountsAndShortageAgreeWithInventory`、`RequirementGuidesBreadWithoutGeneratingOrSaving`、`PackedFramesAndUnacceptedQuestHaveExplicitNextActions`、`ShortPortraitKeepsOneProductionRowAndReadableControls`。ロック中のLv.2は所持数へ数えず、最大2品、未受注と保存失敗も含める。
- [ ] **Unityの対象suiteで赤を確認する。** 既存満杯→整理→パンLv.2→納品→70Coins→Lv.2→次依頼→再起動の実ボタン回帰を保持する。
- [x] **不足表示と操作を接続する。** 上の2行文字を不足カードへ置換。品物画像・名前・Lv.・所持／必要数・不足を示す。カードは種類選択へ案内し、片づけ済みなら再配置メニューへ。下はお茶・パン・花・生成枠整理・納品の1操作行へまとめる。全枠片づけでも再配置入口を残す。
- [x] **盤面の質感を統合する。** クリーム・ミント・ピンク・金色を使い、盤面全体の縁・浅いセル・接地影・控えめな光沢を揃える。種類とLv.を見失わず、候補光、1.06倍の選択、浮遊→集合→弾み→星8粒を再利用する。縦の領域を追加せず置換し、592／640／808とSafe Areaで最小セル48を保つ。
- [x] **既存テストの探索名を更新する。** 生成は`Generate frame tea/bread/flower`の単一入口へ。旧`Create tea`や重複した生成行を前提にした探索だけを修正し、操作結果・保存失敗・ロック・入力の検査を削らない。
- [ ] **全UI回帰と演出計算を確認してコミットする。** `feat: unify tactile Merge board and requirement guidance`。保存失敗・拒否合成に成功演出を出さない。

### 工程5：文章の役割に合わせた書体と余白

**Files:** UIWidgets.cs／UIStrings.cs／QuestMergeScreens.cs／TopHomeScreens.cs／RewardGrowthScreens.cs／SettingsScreen.cs／MergeActionMenuView.cs／DiscardConfirmationView.cs、必要時のみUIFont*.ttf、既存StoryTypographyTests.cs／LayoutNoticeTests.cs。

**Interfaces:** 既存StoryLabel／StoryHeading／Heading／Labelを役割ごとに使用。名称・会話・依頼・説明はUIFontStory、操作はUIFontBold、数値はUIFont／UIFontBold。新フォント依存を追加しない。

- [x] **既存の書体回帰へ失敗する検査を追加する。** 新しい不足品名、街の成長案内、確認画面の説明もUIFontStory。ボタン・数値・重要な保存失敗文は読みやすい書体。名前12／本文14、BestFitなし、最大Coinsと長文のpreferredHeightを実領域と比較する。
- [ ] **Unityで変更前の失敗を記録する。** 最短縦592、640、808、上24／下32のSafe Area例を同じ条件で検査する。
- [x] **役割ごとに書体と余白を適用する。** 文章と所持数を別Textへ分け、小さな領域は説明を詳細表示へ分ける。タイトルロゴと課金注意文の内容は保持する。
- [x] **必要字形だけ補う。** 検証済み元TTFとOFLからサブセットを作り、既存字形・幅・GUIDを保持。欠字検査、6asmdef、ネイティブFontEngineを区別して報告する。
- [ ] **回帰を確認してコミットする。** `feat: unify storybook text roles across town and Merge`。

### 工程6：2.5Dの段階導入案

**Files:** 新規`docs/2point5d-staged-prototype-2026-10-13.md`、References/Preview/integration-baseline-2026-09-22/README.md（設計リンクのみ）。製品素材・SDK・ゲーム本体を変更しない。

**Interfaces:** 既存6名の共有アトラス・3表情・工程2のカメラ／前後関係を制作条件として読む。新リグや素材を実装へ渡す工程ではない。

- [x] **4方式を比較する。** 現行2D強化／軽い2.5D／Live2D・ボーン／完全3Dを、必要素材、顔の一致、6名への展開、Pixel負荷傾向、概算人日で整理。未実測のFPSやSDKの商用条件を確定扱いにしない。
- [x] **モモ1名の試作範囲を提案する。** 街での待機・笑顔・小さな奥行き反応から開始。歩く手足・手振りはパーツ分割と隠れた部分の補完を別工程にする。顔・耳・帽子・衣装の比較、Pixel10分試遊の30FPS／メモリ／発熱／入力遅延を採否条件にする。
- [x] **設計と正式素材の固定範囲を照合してコミットする。** `docs: propose staged 2.5D character trial`。今回の成果は提案で、試作完成とは報告しない。

### 工程7：独立レビュー・全件検証・Pixel比較

**Files:** scripts/build-android.ps1／scripts/tests/android-tools.tests.ps1、README、最新引き継ぎ、新規`docs/integrated-quality-pixel3a-checklist.md`。検証中に追加の製品変更が必要なら、既承認範囲との違いを先に提示する。

**Interfaces:** 前6工程の成果、実行結果XML、製品commit SHA、APKのbuild-infoとSHA256。

- [x] **Core／演出計算／6asmdef／字形／正式素材SHAを検証する。** Coreは工程1・2の計16件を加えた110件が計画値、演出計算10件、警告0／エラー0、正式画像8点不変。実際の属性件数を確認する。
- [x] **テストゲートを実構成へ合わせる。** 最終属性棚卸しはEditMode121／PlayMode99（追加14件。レビューで入口と章再閲覧を追加）。実装中に件数が変われば最終棚卸しの数で更新し、既存suiteを全て残して新しい4suiteも必須にする。PowerShellのモック検査を実行し、0件・不足・失敗・skipのXMLがAPK生成を止めることを確認。
- [x] **保存／入力／演出／画面境界の独立レビューを実施する。** 指定どおりCodexが実装し、最後に1名が全変更を読む。Critical／Importantは解消して関係する検査を再実行する。全差分の独立レビュー、指摘修正、追加の2テスト修正を実施。最新PlayMode99件の成功をユーザー画像で確認済み。実機の表示・性能の採否は下のPixel工程へ残す。
- [ ] **Windows UnityでEditMode／PlayModeを全件実行する。** Export Resultsの2XMLをcommitと対応づける。Codex側で実行していなければ未実施と報告し、ユーザーの検証へ引き継ぐ。全件合格後だけBuild Town QA APK (separate save)を生成する。
- [ ] **同じTown QA packageへ更新する。** 保存データを消さず、`com.mofumachi.merge.qa.townlife`、`scripts/install-pixel3a.ps1`、端末`99RAY1BELH`を使う。古いAPK・違う保存領域を取り違えない。
- [ ] **Pixelで操作と前後比較を残す。** ①街Lv.0／2／4／6で広がり、②カメラを動かして水・入口・飾り・指の一致、③NPC分散・交流・主人公の施設到着、④生成枠複数片づけ→全枠再配置、⑤パンLv.1×2→Lv.2→納品→報酬→街成長→次依頼、⑥失敗条件と二重納品、⑦再起動復帰、⑧10分試遊の重さ・停止・クラッシュを確認。街・Merge・文章の同じ進行状態のスクショと10〜20秒の動きを保存する。
- [x] **完了報告を作ってコミットする。** 変更内容、基準との統合箇所、実行済み／未実施、残る制作、GitHub最新commit、Windows／Pixelの次の操作を明記。ネイティブ・実機が未確認なら全体完成にしない。

## 計画の自己確認

- 街の可動域・カメラ・成長・自然な交流は工程1〜3。保存仕様を追加しないことと元の入口を維持することを回帰に含めた。
- Mergeは工程4で基準の見やすさと現在の立体演出を統合。色の復元・アイテムの自動消去・ルール変更は含まない。
- フォントは工程5、2.5Dは工程6の提案まで。BGM・ショップは保持された別の設計へ残した。
- 工程1〜3のインターフェース名はこの計画内で統一。新ファイルは上の一覧に記載し、保存系ファイル・Packages・正式画像は除外した。
- Review Focusの5条件は対応する工程の検査へ割り当てた。Pixelの見た目・入力・性能は工程7で実操作し、ソース検証から合格を推測しない。

実装方法の再選択は不要。計画の確認後、以前の指定を維持してCodexが順に実装し、最後に独立レビューする。

最新の再承認で順番・対象・固定仕様を変更しない。収益化・運営の追加指定は[将来設計](../../monetization-and-operations-direction-2026-10-13.md)へ記録し、現行の7工程へ機能実装を追加しない。

### 実行時の補足

実装時点の実UI回帰の赤／緑はUnityネイティブ未実施だった。その後、2テスト修正を経てユーザー提供画像でPlayMode99件の成功を確認した。純粋計算16件では追加前の失敗→成功を確認した。レビュー修正は事前の実寸・字体・表示域の反例、回帰の追加、コンパイルと計算を確認したが、実画面の成功とは区別する。実寸360×592へSafe Areaを適用する検査へ修正し、短い画面は48単位を保つコンパクトUI＋非遮断メッセージとした。MultiQuestPresentationTests.csの盤面画像探索1行はユーザーの追加承認後に変更。実行時判断と保留した軽微な指摘は引き継ぎ第8項に記録。収益化・テナント・分析は追加SDK／保存変更をせず設計資料のみ。
