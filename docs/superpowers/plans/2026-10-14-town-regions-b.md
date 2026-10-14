# もふまち：B案の3領域・移動と滞在 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. 前回指定の「Codexが順に実装し、最後に独立レビュー」を維持する。

**Goal:** 中央の正式景観を保持し、西のカフェ・ベンチ、東の花の小道・工房へ、入口まで歩いて短く遷移し、6名が分散して過ごせる街を作る。

**Architecture:** 地域内の道は既存TownNavigationを使い、地域間の入口接続は別の一時状態として扱う。地域ID・ローカル座標・将来の全体座標・施設ID・背景表示・カメラを分離する。3領域の生活を更新し、通常は1背景、切替中も最大2背景を描画する。

**Tech Stack:** Unity 6000.6.4f1、UGUI、既存Input System 1.20.0、C# 9、NUnit、既存の文字・画像・音の描画。新パッケージ・SDKなし。

**Spec:** [確認済みの接続・個別素材・導線設計](../specs/2026-10-14-town-side-connections-design.md)。[7ページの画像比較](../../../References/Preview/town-presentation-2026-10-14/town-expansion-review.pdf)と、2026-10-14のユーザー決定「Bを先行採用、将来Aへ発展」「まず3領域の移動・滞在を安定させる」を合わせて読む。

**状態:** B案と左右素材はユーザー確認済み。この計画はレビュー前で、ゲームへ未適用。現行main `588b910` のWindows PlayMode99件成功は受領済み。EditMode121件とPixelの一周の結果は未受領。本文Klee One試験 `c9e29d7` は別候補として保持し、この計画の基準と混ぜない。

## Global Constraints

- 正式6名はモモ／ルル／ポム／フィオ／ミエル／ノア。顔・配色・衣装・体型・既存アトラス・正式ロゴ・中央PNGのバイトを維持。
- 中央PNG SHA256は `971461342858e74c8db5f669f28c3e5c0c1a0f9d364e645ec55b9ea2834e7b51`。Bでは中央の花壇を覆う園路を追加しない。
- 西・東は確認済みの875×1798画像をそのまま追加。引き伸ばし・境界の描き替え・Aの連続背景化は行わない。
- GameState／GameStateManager／StateCodec／SaveService／QuestManager／RewardService／TownGrowthService／MergeBoard／ItemCatalog／QuestCatalogを変更しない。保存v3・5×6・同種同Lv.・消費・報酬1回・保存失敗時の復元を保持。
- Packages／ProjectSettings／asmdefを変更しない。BGM選曲・購入・Gems・建設所有・親密度・2.5Dはこの計画の範囲外。
- 3領域の閲覧・散歩・会話・遷移は保存を呼ばない。既存の画面移動・pause・終了のPersistはそのまま。
- 再起動は中央と保存済みのお気に入り・進行から復帰。Home内の一時位置はディスクへ書かない。
- 現行移動速度（主人公`.12`／秒、NPC`.028`／秒）・30Hzの移動更新・描画補間を保持。長い移動を納品の必須条件にしない。
- 地域内カメラは初期倍率1.10、最大1.15、画像外を映さない。3領域を1画面へ縮小しない。
- 既存テストを削除・Ignoreしない。多領域化に伴う探索範囲だけは地域を明示して更新し、検査の目的を維持する。
- 作業は既存の隔離クラウドチェックアウトを使う。計画確認後に `origin/main` の基準コミットからB専用ブランチ `feat/town-regions-b-20261014` を作り、本文試験ブランチ・mainは保持。mainが進んでいたら差分を確認して基準を記録し、古いコミットへ戻さない。
- 先行する候補実装・Core検査と、本線への適用・Android QAの生成を分ける。本線適用前には現行EditMode／Pixelの結果、候補のWindows全件結果が必要。未実施を合格扱いしない。

## Review Focus

- fadeの中点でpause・設定・Back・画面破棄が起きても、旧／新入口の一方へ確定し、2背景・入力ロック・古い到着処理を残さない。工程2・4・6で検査。
- 西／東を見た後、TOP・Merge・結果・設定へ背景・UV・地域の水や装飾が漏れない。工程4・6で検査。
- NPCが別領域にいるとき、同じローカル座標を持つ別キャラと会話・衝突・名前札を誤判定しない。工程3・5で検査。
- 西から東、東から依頼部屋、連打や2本指でも、中央経由の順序と主人公の位置を保ち、途中の入口で勝手に納品・購入へ進まない。工程2・3・6で検査。
- 高Lv.保存・全生成枠を片づけた盤面・保存失敗でも、領域の一時状態を進行へ混ぜず、旧コアループを維持する。工程1・6で検査。

## 変更するファイルと責務

以下のCore／Presentation／Resources／Tests／Editorはすべて `Assets/Mofumachi/` 配下。新規Assetsファイルとフォルダには一意なGUIDの`.meta`を付ける。

| 対象 | 役割 |
|---|---|
| 新規Core/TownRegionTypes.cs、TownRegionCatalog.cs | 地域ID、地域付き座標、背景キー、全体座標への原点、地域内地図、入口接続。既存中央カタログは読み取り利用 |
| 新規Core/TownRegionJourney.cs | 入口へ歩く→短い遷移→次の入口から歩く、の一時的な段階とキャンセル。地域間を偽の歩行辺にしない |
| 新規Presentation/TownRegionSession.cs | 6名の地域、経路の割当、地域移動の調整、地域の閲覧、一時スナップショット。保存へ依存しない |
| 新規Presentation/TownRegionView.cs、TownRegionControls.cs | 背景の最大2枚切替、軽いfadeと花の光、地域案内・見る／歩く・主人公へ戻る操作 |
| Presentation/TownLifeView.cs、TownActor.cs | 地域を持つ散歩・描画・足元・施設到着・会話・札。既存の主人公ドラッグと6体の共有アトラスを保持 |
| Presentation/TownViewportView.cs、TopHomeScreens.cs | 地域ごとのカメラ、Home組立、コンパクトな地域案内と既存ナビ |
| Presentation/TownAmbientGraphic.cs、TownGrowthView.cs | 地域ごとの表示。中央の噴水・成長飾りを左右へ流用しない |
| Presentation/UIFlowController.cs | Home再構築前の一時状態の捕捉・破棄のみ。Initializeでリセット。既存保存・納品・報酬メソッドは変更しない |
| Presentation/UIStrings.cs | 地域名・操作・新しい短い生活会話を必要字形検査へ登録 |
| 新規Resources/Mofumachi/TownWestBackground.png、TownEastBackground.png | 承認済み左右素材の同一PNG。中央原画は保持 |
| Editor/AndroidBuild.cs | 追加背景の存在・インポート設定・字形をQAビルド前に検査。QAの別セーブID・ビルド方法を保持 |
| 新規Tests/EditMode/TownRegionCatalogTests.cs、TownRegionJourneyTests.cs | 経路・入口・接続順・不正入力・段階／中断の純粋検査 |
| 新規Tests/PlayMode/TownRegionFlowTests.cs、TownRegionLifeTests.cs | 実UIの見る／歩く／到着・地域別生活・背景復元・保存非変更 |
| 新規Tests/BuildTime/TownRegionContentTests.cs | 背景・既存原画・共通描画サイズ・日本語文字の検査 |
| 既存Tests/PlayMode/TownLifeTests.cs、TownHabitTests.cs、TownMotionPresentationTests.cs、TownViewportPresentationTests.cs、PopPresentationTests.cs | 「全員中央」という探索の前提を見直す。6名・移動・反応・会話・保存・入力の検査を残す |
| Resources/Mofumachi/UIFont.ttf、UIFontBold.ttf、UIFontStory.ttf（不足字形がある場合のみ） | 同じ元書体から新しいUI文字だけ補完。書体の変更・GUID変更・ライセンス変更はしない |

資料追加は `docs/town-regions-b-pixel3a-checklist.md`、終了時の引き継ぎ、経路を元絵で検査するPreview。READMEは候補の状態・実結果だけ追記する。依存・生成コード・保護対象への追加変更が必要なら、その対象を先に提示する。

## 検証コマンド

クラウドの純粋計算と実Unity DLLのコンパイルは、Unity Test Runner・Android実機の成功とは別に記録する。

```bash
DOTNET_CLI_HOME=/workspace/runtime/dotnet NUGET_PACKAGES=/workspace/toolchains/nuget /workspace/toolchains/unity/6000.6.4f1/Editor/Data/DotNetSdk/dotnet test Tests/Core/Mofumachi.Core.Tests.csproj --no-restore --nologo
DOTNET_CLI_HOME=/workspace/runtime/dotnet NUGET_PACKAGES=/workspace/toolchains/nuget /workspace/toolchains/unity/6000.6.4f1/Editor/Data/DotNetSdk/dotnet test Tests/Presentation/Mofumachi.Visual.Tests.csproj --no-restore --nologo
python3 /workspace/setup/mofumachi/verify_asmdefs.py
python3 scripts/subset-ui-fonts.py --check --source-dir /workspace/setup/mofumachi/chapter-font-sources
```

対象を絞るCore検査は最初のコマンドへ `--filter FullyQualifiedName~TownRegionCatalogTests` または `~TownRegionJourneyTests` を付ける。実UIはWindows UnityのPlayModeで対象suiteをRun Selected、最後にEditMode／PlayModeをRun All→Export Results。件数は実際のXMLのtotal／passed／failed／skippedとコミットを記録する。ネイティブ未実施なら赤／緑を実行済みと書かない。

---

### 工程1：地域・入口・居場所の読み取りカタログ

**Files:** 新規Core/TownRegionTypes.cs、TownRegionCatalog.cs、Tests/EditMode/TownRegionCatalogTests.cs、左右PNGとmeta、元絵上の足元・障害物確認図。

**Interfaces:** `TownRegionId { West, Central, East }`。`TownRegionLocation(TownRegionId region, TownPoint local)`はRegion／Local、`TownRegionDefinition`はId／Name／WorldOrigin／BackgroundResource／Mapを持つ。`TownRegionPortal`はFrom／To（両方TownRegionLocation）。`TownRegionCatalog.ForLevel(TownRegionId region,int level): TownRegionDefinition`、`TryFindPlace(string id,int level,out TownRegionLocation location,out TownPlace place): bool`、`TryGetPortal(TownRegionId from,TownRegionId to,out TownRegionPortal portal): bool`、`ToWorld(TownRegionLocation location): TownPoint`。

- [ ] **足元と家具・花壇の幅を元絵で検査する。** 既存route-proposals.jsonを元絵へ重ね、正式キャラの足元幅と曲がりを確認。道へ必要な曲がり点を加え、建物・椅子・テーブル・花壇・施設用芝生を除外する。判定用の独立した除外図をテスト資料として残す。
- [ ] **失敗するカタログ検査を書く。** `ThreeRegionsKeepOriginalSizeAndSeparateWorldOrigins`（西−1／中央0／東1のX原点、Yは0、中央の旧地図が一致）、`PortalsAreAdjacentAndNeverStreetEdges`、`OriginalFacilitiesKeepTheirIdsAndCoordinates`、`SideStopsStayOnConnectedSurveyedPaths`、`LevelsRetainEarlierSideStops`、`UnknownRegionsAndPlacesAreRejected`、`CatalogCannotChangeProgressOrSave`。

```csharp
// 新規APIがまだない最初の赤は、型をReflectionで確認し、欠如で失敗させる。
Assert.That(typeof(GameState).Assembly.GetType("Mofumachi.Core.TownRegionCatalog"), Is.Not.Null);
// API追加後は同じ検査へ以下の固定値と経路・障害物検査を接続する。
Assert.That(TownRegionCatalog.ForLevel(TownRegionId.Central,0).Map, Is.SameAs(TownPlaceCatalog.ForLevel(0)));
Assert.That(TownRegionCatalog.TryGetPortal(TownRegionId.West,TownRegionId.East,out _), Is.False);
```

- [ ] **Coreの対象suiteで赤を確認する。** 上記型の欠如が理由の失敗。環境エラーを赤と数えない。
- [ ] **カタログを実装する。** 中央のquest／shopと全Lv.別地図はそのまま。中央↔西は(.15,.52)↔(.94,.55)、中央↔東は(.68,.515)↔(.09,.50)。西↔東の直結なし。西・東の場所IDは`west.cafe-front`／`west.bench-front`／`west.flowers`／`west.meeting`、`east.workshop-front`／`east.bench-front`／`east.flowers`／`east.meeting`。入口・ベンチ・花・待合は初期から、カフェ訪問はLv.2、工房訪問はLv.4。歩いて通る道と訪問先の解放を区別する。将来の庭の余地は今回歩行辺にしない。
- [ ] **対象suiteと全Core・互換コンパイルを確認する。** 全地点の接続と独立除外図、旧保存非変更を確認。
- [ ] **コミットする。** `feat: define separate town regions and safe entrance links`。

### 工程2：歩行と0.30秒の地域遷移の一時モデル

**Files:** 新規Core/TownRegionJourney.cs、Tests/EditMode/TownRegionJourneyTests.cs。

**Interfaces:** `TownRegionTravelPhase { WalkingOut, Fading, WalkingIn, Complete, Cancelled }`。`TownRegionJourney(TownRegionLocation start,TownRegionLocation destination,int level)`、`Tick(TownPoint currentLocalPosition,float seconds): void`、`TryTakeTransfer(out TownRegionLocation entry): bool`、`Cancel(): void`。読み取り専用Region／Target／Phase／FadeOpacity、定数FadeSeconds=.30f。モデルはRegionId付きの行程だけを管理し、Unity・人物画像・GameStateに依存しない。

- [ ] **失敗するモデル検査を書く。** `EntryMustBeReachedBeforeFade`（距離.001未満）、`FadeTransfersExactlyOnceAtMidpoint`、`WestToEastVisitsCentralBeforeEast`、`CancelledBeforeAndAfterMidpointSettlesOneRegion`、`InvalidDeltaDoesNotAdvanceOrUnlock`、`ARepeatedRequestCannotReplayArrival`。負の時間・NaN・Infinity・大きいdt、入口と出発点が同じ場合も含める。

```csharp
Assert.That(TownRegionJourney.FadeSeconds, Is.EqualTo(.30f));
// 門に到着前はPhase=WalkingOut、TryTakeTransfer=false。
// .15秒到達時のTryTakeTransferは1回だけtrueでTo入口を返し、2回目はfalse。
// Cancel後はFadeOpacity=0、確定したRegionを保持し、追加transferを返さない。
```

- [ ] **対象suiteで赤を確認する。** 欠けているモデルまたは段階の検査による失敗を記録。
- [ ] **最小モデルを実装する。** 地域内歩行はTownNavigationへ委ね、fade中点で移動先入口へ1回確定する。最大.1秒ずつ時間を受け、確認済みのtransferを受け取るまで次の歩行へ進まない。transferの描画フレームはFadeOpacity=1を保ち、次フレームから薄くする。実際の長さは1フレームの丸めを含め0.25〜0.4秒へ収める。中点前の取消は元地域、中点後は移動先入口へ確定。2ホップも同じ順番を使い、座標を地域間で線形補間しない。
- [ ] **対象suiteと全Coreを確認する。** GameStateがモデルへ参照されていないことも確認。
- [ ] **コミットする。** `feat: add cancellable entrance walking and short region transitions`。

### 工程3：6名と地域内歩行・施設への復帰をつなぐ

**Files:** 新規Presentation/TownRegionSession.cs、TownLifeView.cs、TownActor.cs、新規Tests/PlayMode/TownRegionFlowTests.cs、既存TownLifeTests.csと関連する探索検査。

**Interfaces:** `TownRegionSession.Bind(ScreenContext c,TownLifeView life,IReadOnlyList<TownActor> actors): void`、`GuideToRegion(TownRegionId region): bool`、`GuideToPlace(string id): bool`、`LookAtRegion(TownRegionId region): bool`、`ReturnToHero(): void`、`Step(float seconds): void`、`CancelJourneys(): void`。HeroRegion／ViewingRegion／IsHeroTransitioningは読み取り専用。`TownActor.RegionId`、`TransferToRegion(TownRegionDefinition region,TownPoint entry): void`、`SetRegionVisible(bool visible): void`を追加。TownLifeView.ActorsはBindで1度作る読み取り専用コレクション。

- [ ] **失敗する実UI検査を書く。** `SixOfficialCharactersHaveOneControllerAndThreeRegions`、`HeroWalksToGateThenStartsAtNeighbourEntry`、`WestToEastKeepsCentralHopOrder`、`SideToQuestReturnsToOriginalEntranceAndRequiresEnter`、`ViewingOnlyDoesNotMoveHeroOrWriteSave`。実際の行き先ボタンとフレーム更新で検査し、施設へ瞬間移動するテスト用入口は作らない。
- [ ] **Windows対象suiteで赤を確認する。** 未実行なら未実施と記録し、クラウドの実DLLコンパイルだけで実UI合格と書かない。
- [ ] **6体を地域へ割り当てる。** お気に入りは中央の既存初期位置。他の5名は正式並び順で西2／中央1／東2、各地域の別の居場所へ。NPCのMovementは引き続きnull。正式6体と共有アトラスは1組だけ生成し、地域ごとに複製しない。
- [ ] **地域付き歩行を接続する。** 既存GuideToFacility／GuideToPlace／GuideToRestStopの呼出しを保持し、旧施設は中央までの行程へ解決。手動ドラッグは表示中の主人公だけ、地域内の道へ射影。ユーザーの新しい入力は旧行程を取り消すが、自動案内の内部GuideToが自分の行程を取り消す再入を防ぐ。出入口まで歩いてからtransferし、補間のprevious位置・ポインタ・古い目的地を同じフレームでリセット。画面外の人物はCanvasGroupの表示／raycastを止め、描画更新を省く。6個の人物オブジェクトと生活の更新は保持する。
- [ ] **実DLL・旧指入力回帰・対象suiteを確認する。** 6名の数、中央施設、明示入室、追加保存0を維持。地域付きの場所IDを使い、別地域の同じ座標を到着と誤認しない。
- [ ] **コミットする。** `feat: connect six town actors to regional walks and existing facilities`。

### 工程4：地域の背景・カメラ・Home案内・軽い遷移

**Files:** 新規Presentation/TownRegionView.cs、TownRegionControls.cs、TopHomeScreens.cs、TownViewportView.cs、UIFlowController.cs、TownAmbientGraphic.cs、TownGrowthView.cs、TownRegionFlowTests.cs。

**Interfaces:** `TownRegionView.Bind(ScreenContext c,RawImage background,TownLifeView life,TownRegionSession session): void`、`ShowRegion(TownRegionId region): void`、`SetTransition(TownRegionId from,TownRegionId to,float opacity): void`、`RestoreCentral(): void`。`TownViewportView.ChangeRegion(RawImage background): void`は地域内カメラを再設定。`TownRegionSession.CaptureSettledSnapshot(): TownRegionSnapshot`、`RestoreSnapshot(TownRegionSnapshot snapshot): void`。`TownRegionTypes.cs`へ追加するTownRegionSnapshotはFavoriteId（string）、ViewingRegion、読み取り専用Actors（CharacterId／Location／PlaceId）、地域別CameraCenters（TownPoint）を持つ。行程・入力・報酬・コールバックを含めず、ディスクのDTOへ登録しない。

- [ ] **失敗する実UI検査を追加する。** `TransitionUsesAtMostTwoBackgroundsAndNeverInterceptsHud`、`HeroRegionAndViewingRegionRemainSeparate`、`CentralWaterAndGrowthDoNotAppearInSideTown`、`LeavingHomeRestoresCentralTextureAndCover`、`PauseSettingsBackAndResizeReleaseAllRegionalInput`、`RebuildUsesOnlySettledTransientSnapshot`。fade開始前／中点前後／終了時の各状態を実UIで観察する。
- [ ] **Windows対象suiteで赤を確認する。** ソースだけで未描画の背景枚数を合格扱いにしない。
- [ ] **最大2背景の切替を実装する。** 通常は1枚、切替中は旧／新の2枚まで。0.30秒の穏やかな明るいfade＋既存の花アイコンを最大6個の固定メッシュで短く流す。1個の再利用レイヤー、常時パーティクル・新RenderTexture・キャラの複製なし。NPCだけの地域移動ではカメラを切り替えない。中央の水・成長飾りは中央だけへ表示し、左右へ中央の噴水を重ねない。
- [ ] **Homeへ見る／歩くの案内を接続する。** 現在地と西・中央・東の「眺める」操作、表示中の地区へ「おさんぽする」、別地域の主人公へ「主人公へ」を、48以上の操作面積で表示。見るだけでは人物を移さず、歩く選択では出発地域へ戻って案内追従する。空いた街面の既存ドラッグは各地域内のカメラ操作を維持。主人公タップには表示地域の居場所と中央の依頼／ショップへの案内を用意。依頼概要・既存下部ナビ・戻る／設定は保持する。
- [ ] **Home再構築時の一時状態を接続する。** UIFlowController.Renderで旧Homeを閉じる前に行程を確定・入力解放してスナップショットを捕捉。Homeへ戻るときのみ復元し、Initialize・新しいFlow・お気に入り不一致では破棄して中央から始める。ディスクへ書かない。Disposeで中央TextureとBackgroundCoverを確実に復元してから次画面を描く。
- [ ] **実DLL・対象suite・Safe Areaを確認してコミットする。** 360×592／640／808とSafe Area、文字と48操作面、UV・水・札・入口の共通位置を検査。`feat: present short fairy-tale region transitions and exploration controls`。

### 工程5：NPCの地域間散歩・滞在・交流と街Lv.の表示

**Files:** TownRegionSession.cs、TownLifeView.cs、TownActor.cs、TownGrowthView.cs、UIStrings.cs、新規Tests/PlayMode/TownRegionLifeTests.cs、既存TownHabitTests.cs／TownLifeTests.cs／TownMotionPresentationTests.cs。

**Interfaces:** 工程3のStep・GuideToPlace、工程1の地域別PlacesとMinLevelを使う。`TownRegionSession.TryScheduleNpcVisit(string characterId,TownRegionId region): bool`は入口への行程を作り、人物の位置を即座に移さない。NPCの到着・予約はSessionだけが管理する。

- [ ] **失敗する実UI検査を書く。** `OffscreenNpcsKeepLivingWithoutDuplicatedActors`、`NpcChangesRegionThroughReservedEntrances`、`DifferentRegionsNeverConverseOrBlockEachOther`、`TwoResidentsMeetAndResumeInVisibleRegion`、`SideVisitsNeverMutateCoinsQuestsOrBoard`、`LevelsAddVisitsWithoutClosingExistingEntrances`。実際の歩行で訪問を観察し、人物を道外へ置いて会話を通さない。
- [ ] **Windows対象suiteで赤を確認する。** 既存NPC寄り道・会話検査は地域を明示して探索し、反応は表示中のNPCで検査する。テストの削除・緩和で通さない。
- [ ] **地域ごとの散歩と予約を実装する。** まず地域内の2〜4居場所を巡る。滞在後、最短20秒の地域訪問間隔を空け、隣の空いている地域へ時々移動する。NPCの地域定員は2名、行程中の到着予約も含める。狭い入口は1名ずつ、予約が取れなければ元地域の別の居場所へ。主人公はNPC定員で移動を拒否しない。町外へ足元を押し出さない。
- [ ] **短い生活反応を接続する。** ベンチ前で休む、花の縁を見る、カフェ／工房前で待機、広い待合で2名が向き合う。既存Normal／Smile／Surpriseと軽い反応を使い、座り・独立した手足素材の完成と扱わない。会話は同じ地域の近い2名だけ、1組・cooldown8秒・停止最大1秒。名前札は主人公・話者を優先し、隣の地域の人物を出さない。
- [ ] **既存Lv.に従う訪問と表示を確認する。** 初期から左右のベンチ・花・待合へ行ける。Lv.2のカフェ前、Lv.4の工房前の訪問会話を追加。中央のLv.2／4／6の旧枝・飾りは保持。将来の新建物・道・飾りは地域カタログへ追加できる責務分離までとし、今回購入状態を保存へ足さない。
- [ ] **地域別行動・保存非変更・既存回帰を確認してコミットする。** `feat: add regional resident strolls rests and encounters`。

### 工程6：QAコンテンツ・日本語・中断・旧コアループの回帰

**Files:** Editor/AndroidBuild.cs、UIStrings.cs、Tests/BuildTime/TownRegionContentTests.cs、TownRegionFlowTests.cs、既存の街関連検査、必要字形が不足した場合だけ既存3書体TTF、READMEと実機チェックリスト。

- [ ] **失敗する検査を追加する。** `RegionAssetsKeepApprovedSourceAndImportSettings`、`RegionUiHasSourceGlyphs`、`InterruptedTransitionLeavesNoInputOrCallbacks`、`RegionVisitThenBreadDeliveryAndReloadPreservesProgress`。新しい日本語、7往復、SaveService失敗、旧保存、高Lv.、生成枠全片づけから再配置を含む。行程だけでStore.Writes増加0、通常の画面移動は既存Persistの回数を期待し、lastSaveTimeと進行を区別する。
- [ ] **対象suiteで赤を確認する。** 新検査が足りない実装を理由に失敗し、環境の未実施を誤認していないこと。
- [ ] **追加背景の検査と必要字形を接続する。** 左右のTextureImporterは中央と同じ縦画面用上限2048、mipmapなし・isReadable false、Android側も明記。共有キャラ／UIアトラスの1024上限は変更しない。元PNGのSHAと中央の不変を検査し、字形不足は同じOFL元書体からだけ補完する。QAのapplicationId・独立セーブ・ビルドメニューはそのまま。
- [ ] **クラウドの全Core・演出計算・実6アセンブリ・字形・素材差分を検証する。** 各結果とコマンド、保護対象ファイルのSHA、不変の保存スキーマを記録。Unityネイティブ・Pixelは別欄。
- [ ] **Windows構成を棚卸しする。** 既存EditMode121／PlayMode99の検査目的と新規検査を一覧化。候補の正確なテスト件数は実際のNUnit XMLに基づいて報告し、旧99成功を候補の成功に転用しない。
- [ ] **コミットする。** `test: guard region lifecycle content and saved core loop`。

### 工程7：独立レビュー・Windows全件・Pixel 3aでの比較

**Files:** 変更一覧・検証記録・実機チェックリスト・終了時引き継ぎのみ。レビューで必要となるコード修正は指摘の再現検査→修正→全回帰の順で行う。

- [ ] **変更全体の独立レビューを行う。** 設計、7工程、変更差分、実結果を渡す。重点は保存非変更、背景の復元、地域間／地域内の座標、重複する到着、pause・連打・2本指、NPC予約の詰まり、実機未検証箇所。重要指摘は再現する赤→緑と全回帰で解決。
- [ ] **本線への適用条件を確認する。** 現行mainのEditMode121／Pixel一周の結果を受領して不具合を切り分け、候補BのWindows EditMode／PlayMode全件とExport Resultsを確認。結果不明のままmainへ統合しない。必要なら本線への具体的差分と統合先を提示する。
- [ ] **全件合格した候補からTown QA APKを生成する。** `Mofumachi → Android → Build Town QA APK (separate save)`、ビルドcommit／日時／SHAを記録。実機更新は別セーブの同じパッケージを上書きし、旧セーブを消すアンインストールはしない。
- [ ] **Pixelで地域の実操作を確認する。** 中央→西→中央→東→西、見るだけ→主人公へ、店先／ベンチ／花／会話、地域移動中のpause・設定・Back、10分の散歩、BGM継続、文字切れ・Safe Area・応答・フリーズ・クラッシュ・メモリ／フレーム時間を確認。30FPSは目安で、測定前に保証しない。
- [ ] **Pixelでパンの一周と再起動を確認する。** 依頼→パンLv.1を2個生成→ドラッグ／タップ合成→Lv.2納品→消費・Coins・街成長→次依頼→再起動。報酬二重なし、盤面と生成枠とお気に入り保持、再起動の街は中央。実機未実施は完了と扱わない。
- [ ] **比較画像と引き継ぎを報告する。** 同じ解像度・同じ保存状態で中央／西／東、遷移、滞在、既存Merge／納品を撮影。実装済み／未実装、確認済み／未実施、正確な件数、最新コミット・main反映・Windowsの次操作を記載。A案は別の境界素材と通行幅確認を伴う次期候補として残す。

## 計画の自己点検

- 中央保持・左右の遊び場・入口歩行・0.30秒の遷移・6名分散・別領域への散歩・滞在・交流・将来Aへの座標分離を工程1〜5へ割り当てた。
- 保存・納品・報酬・正式素材・旧施設の保護、画面離脱と中断、文字と負荷を工程4・6・7へ割り当てた。
- カメラで見る地域と人物の地域を別に持ち、NPCのoffscreen更新と現在地の描画を分離した。入口リンクと歩行辺、将来の全体座標と現在のローカル座標も混ぜていない。
- 将来Aの連続スクロール・新素材仕上げ・正式な座り／手足・店内購入・収益化は今回の成果へ数えない。
- 本文候補と現行本線の検証を混ぜず、クラウド検査・Unity・Pixelの結果を独立して報告する。
