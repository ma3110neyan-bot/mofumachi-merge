# もふまち・3章の物語と滑らかな街の暮らし Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 正式6名と既存6依頼を3章へつなぎ、メルヘン調の文字・自然な街の動き・景色に残る成長を整え、Pixel 3aで最初の物語と保存復帰を確かめる。

**Architecture:** 章は既存の完了IDから算出する読み取り専用モデルとし、Save v3／納品処理を維持する。移動は既存の街の経路と指の所有を使い、固定ステップの計算と毎フレームの描画を分離する。章末と街装飾は保存済み進行を見せるPresentationとして追加し、既存のシーン・UGUIを拡張する。

**Tech Stack:** Unity 6000.6.4f1、C# 9／netstandard2.1、UGUI／Input System、NUnit、.NET 8コア検証、Android API26以上／target36／ARM64／IL2CPP。

**Spec:** [承認済み設計](../specs/2026-10-11-chapter-game-design.md)。2026-10-11の「設計を承認して実装計画へ進める」を受領。本計画はレビュー用で、以下の製品変更はまだ実装していない。

**実装方法:** ユーザーが選んだ「Codexが順に実装し、最後に独立レビュー」を継続する。工程ごとの実装担当・レビュー担当の追加派遣は行わない。既存の隔離チェックアウトを使い、追加worktreeを作らない。

## Global Constraints

- 正式6名はモモ・ルル・ポム・フィオ・ミエル・ノア。ID順は `momo,lulu,pom,fio,miel,noah`。採用済みの顔・配色・衣装・シルエット・全身・通常／笑顔／驚きを維持する。鏡像、背面、独立した脚のコマ、新衣装は追加しない。
- 5×6盤面、茶器／パン／花の3系統×Lv.1〜3、同時受注1件。既存の要求数量・順序・6報酬（30,40,40,70,70,80 Coins）を維持する。
- 新規から全6件なら330 Coins／街Lv.6。章1は70／Lv.2、章2は180／Lv.4。ただし画面には保存済みの実値を表示し、基準値で上書きしない。
- 章は `completedQuestIds` から算出し、Save v3を維持する。章ボーナス、既読フラグ、新しい建築保存データを増やさない。未知ID、旧進行、音設定、主人公、盤面、報酬IDを保持する。
- 初回案内「課金について」「未成年の方は、課金する前に必ず保護者の方に相談し、許可をもらってください。」と「確認しました」を維持する。
- 主人公は指で自由移動。道・噴水回避・指を離したら停止・到着後の明示入室を維持する。移動先タップの操作へ変えない。
- 移動計算は1/30秒、1フレーム最大3ステップ。描画は毎フレーム。歩行振幅の開始／停止は約0.12秒。6名、同時吹き出し1つ。ホーム離脱／中断／モーダルで停止する。
- 名前は最低12、会話・説明は14、章名・物語見出しは18〜22 Canvas単位、行間約1.2。Kiwi Maru Mediumを物語・キャラへ、Zen Maru Gothic Medium／Boldを操作・数量・注意文へ使う。自動縮小で文字を押し込まない。
- 街背景・道・入口のUV位置、Lv.1〜3装飾を維持する。Lv.4歓迎リボン、Lv.5花アーチ、Lv.6ガーランドを固定位置へ追加。6以上は6の外観を使い、保存Lv.は変更しない。
- 画像は共有アトラス、Default／Android有効上限1024、ミップなし、Readable無効。正式音源は別途採用が必要で、今回の音源候補を正式版と報告しない。
- ショップ販売、課金、広告、イベント、図鑑、新通貨、自由建築、スタミナ制限は追加しない。
- 2026年10月の公開は目標。品質で必要なら設計と日程を見直す。「誰も真似できない」は、6名との関係・お届け・街に残る成果を結ぶ独自性の目標であり、競合の不存在を保証する表現ではない。

## Review Focus

1. 旧1件完了、全6件完了、順不同完了、未知ID／未知受注を持つセーブ：進行を消さず、案内だけ算出する（工程4 `LegacyAndOutOfOrderProgressRemainReadOnly`）。
2. 納品保存失敗・章末連打・設定往復・途中終了：章や報酬を先取りせず、再開／再表示で再払いしない（工程5 `ChapterReentryAndSaveFailureNeverPayOrAdvance`）。
3. 不規則フレーム、長い停止、角の補間、静止ドラッグ、別指：道を外れず、停止中に歩かず、復帰で飛ばない（工程2／3 `ClockDropsStallDebtAndResetClearsIt`、`InterpolationFollowsTheCorner`、`StationaryDragAndLifecycleStopGait`）。
4. 長い章名・台詞、int.MaxValueの実HUD、短い縦画面／Pixel Safe Area：文字と操作を装飾で隠さず、縮小で合格にしない（工程1／5／6 `StoryRolesAndNoticeFitPortraitSizes`、`ChapterPagesFitShortPortraitAndActualValues`）。
5. 新素材／字形の欠落と、旧結果XMLの使い回し：実FontEngine・有効画像設定・新試験の実行を確認し、欠落時にAPKを成功扱いしない（工程1／6／7 `StoryFontHasRequiredGlyphs`、`TownDecorationsUseEffective1024Limit`、`ChapterBuildGateRejectsMissingSuites`）。

## 工程の境界と検証環境

工程1〜7を順に進める。`Assets/`内の新ファイルにはmetaを追加し、既存GUIDは保持する。CoreへUnity参照、UIへ章の支払い処理、新しい外部UI／物理ライブラリを持ち込まない。

コアのコマンドはチェックアウトのルートで実行する。クラウドでは既存環境を使う：

```bash
export PATH="/workspace/toolchains/unity/6000.6.4f1/Editor/Data/DotNetSdk:$PATH"
export DOTNET_CLI_HOME=/workspace/runtime/dotnet
export NUGET_PACKAGES=/workspace/toolchains/nuget
export DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet test Tests/Core/Mofumachi.Core.Tests.csproj --no-restore
```

現状のコア81件は過去の検証記録。新しい試験数は実行結果から記録し、予定数で合格を報告しない。対象テストは下記の `--filter`、Unity専用はWindowsのTest Runnerでクラス名を指定して実行する。クラウドの補助確認は `python /workspace/setup/mofumachi/verify_asmdefs.py`（6アセンブリ、実Unity DLL、直接参照のみ、0警告／0エラー）。Unityライセンス未利用／クラウドAPK保留は既知の制約で、FontEngine・描画・入力・IL2CPPのネイティブ合格はWindowsで確認する。実行できない試験は「未実行」と残し、コンパイルをその代用としない。

---

### 工程1：物語の書体、文字の役割、読み切れるレイアウト

**Files:**
- 作成：`scripts/subset-ui-fonts.py`、`Assets/Mofumachi/Resources/Mofumachi/UIFontStory.ttf`、`Assets/Mofumachi/Resources/Mofumachi/UIFontStory-LICENSE.txt`、`Assets/Mofumachi/Tests/PlayMode/StoryTypographyTests.cs`。
- 変更：`Assets/Mofumachi/Presentation/UIWidgets.cs`、`UIStrings.cs`、`CharacterSelectionScreen.cs`、`TownLifeView.cs`、`QuestMergeScreens.cs`、`RewardGrowthScreens.cs`、`PurchaseNoticeView.cs`、`Assets/Mofumachi/Editor/AndroidBuild.cs`、`Assets/Mofumachi/Tests/BuildTime/PresentationContentTests.cs`、既存 `UIFont.ttf`／`UIFontBold.ttf`、`docs/ui-pop-assets.md`。
- 上記のファイル名のみのPresentationパスは `Assets/Mofumachi/Presentation/`。TTFは `Assets/Mofumachi/Resources/Mofumachi/`。既存 `UIFont-LICENSE.txt` を保持し、StoryのOFLと出典を追加する。

**Interfaces:** 既存 `UIWidgets.Label/Heading/Ribbon` の引数を保つ。追加 `StoryLabel(string name,string text,RectTransform parent,Rect anchors,int fontSize): Text`、同じ引数の `StoryHeading`、`StoryRibbon`。3メソッドだけが `Resources.Load<Font>("Mofumachi/UIFontStory")` を選ぶ。操作ラベル／数値は既存APIを継続。生成CLIは `python scripts/subset-ui-fonts.py --source-dir <キャッシュの親> --check`（検査のみ）、`--check`なしは3TTFを生成する。

- [ ] **1. 失敗する字形・役割テストを書く。** BuildTime `StoryFontHasRequiredGlyphs`：実FontEngineで `UIStrings.All` を3書体に検査、Story欠落とU+FFFF／U+0378／U+0416に `BuildFailedException`。PlayMode `StoryRolesAndNoticeFitPortraitSizes`：名前／紹介／会話がStory、操作・数値・案内がZen、最低サイズ、行間、bestFit=false、preferredHeight/Widthが枠内。360×592／640／808、1080×2220とSafe Areaを使い、案内の「保護者」が分断されないことも確認する。

```csharp
var story = Resources.Load<Font>("Mofumachi/UIFontStory");
Assert.That(story, Is.Not.Null);
foreach (var text in UIStrings.All) Assert.DoesNotThrow(() => AndroidBuild.ValidateGlyphs(story, text));
Assert.Throws<BuildFailedException>(() => AndroidBuild.ValidateGlyphs(story, "\uffff"));
// StoryTypographyTestsでは実ページを構築して名前・紹介・会話と操作ラベルを検査する。
```

- [ ] **2. 対象の赤を確認する。** Windows EditMode `PresentationContentTests`、PlayMode `StoryTypographyTests` を実行。まずStory資源／APIの欠落で失敗することを確認。クラウドのみの場合は不足APIのコンパイル失敗とネイティブ未実行を区別する。
- [ ] **3. 生成と役割APIを実装する。** Google Fontsの実体SHAを固定：Zen Medium `3cfdb98a13571ede17fcc769f5093a97c38b80a7b9b2ab754a26b4d822092b3b`、Bold `fe24426b9c8b5523a0146a8235c8674eccf0493af354a53ec895c3596d9eb745`、Kiwi Medium `b2659f300a7d48c3f29eb273ffc5e1b26cc416ac8c37ff6bb2f3e43c2f4d235a`。`--source-dir`配下の `ZenMaruGothic-Medium.ttf`、`ZenMaruGothic-Bold.ttf`、`KiwiMaru-Medium.ttf`を検証してから、Core／Presentationの日本語・ASCII・現行cmapの和集合を3書体へサブセット化。全出力を検査後に置換、GUID維持、Story派生名は `Mofumachi Story UI`、OFL同梱。元TTFは製品へ追加しない。役割別に適用し、案内は正式文言を保つ改行と高さで調整する。ビルドの `ValidateContent()` は3書体を必須にする。
- [ ] **4. 検証する。** 生成CLIの `--check` は欠字・SHA不一致時非0、正常時0。上記Windows対象は全件成功・skip0、実DLLコンパイルは0警告／0エラー。フォント見本のPillow描画だけをネイティブ合格としない。
- [ ] **5. Commit。** `feat: add storybook typography with verified font subsets`。

### 工程2：時間の蓄積と道に沿う補間をCoreで検証

**Files:** 作成 `Assets/Mofumachi/Core/TownFrameClock.cs`、`Assets/Mofumachi/Tests/EditMode/TownFrameClockTests.cs`。変更 `Assets/Mofumachi/Core/TownNavigation.cs`、`Assets/Mofumachi/Tests/EditMode/TownMovementTests.cs`。既存 `TownMovement.Tick(float)` の1ステップ上限を維持する。

**Interfaces:** `TownFrameClock.StepSeconds = 1f/30`、`MaxStepsPerFrame = 3`、`AddElapsed(float elapsedSeconds): int`、`Alpha: float`、`Reset(): void`。`TownNavigation.DistanceAlongPath(TownPoint from,TownPoint to): float`（到達不能は0）、`InterpolateAlongPath(TownPoint from,TownPoint to,float alpha): TownPoint`（到達不能／非有限alphaはfrom）。既存 `Move` と同じ接続経路・幅・キャッシュを使う。

- [ ] **1. 失敗するテストを書く。** `FramePartitionsKeepTheSameTravelBudget`：合計1秒の30Hz／60Hz／不規則刻みで30ステップ、距離上限.12＋誤差.0001。`ClockDropsStallDebtAndResetClearsIt`：大きな遅延でも3以下、次フレームへ整数ステップの借金を残さず0≤Alpha<1、Reset後0、NaN／Infinity／負値／0を無視。`InterpolationFollowsTheCorner`：L字と切断経路、alpha 0／.5／1／範囲外を検査する。

```csharp
var nav = new TownNavigation(new[]{new TownPoint(0,0),new TownPoint(1,0),new TownPoint(1,1)},
    new[]{new TownEdge(0,1),new TownEdge(1,2)}, .028f);
var a = new TownPoint(.9f,0); var b = new TownPoint(1,.1f);
Assert.That(nav.DistanceAlongPath(a,b), Is.EqualTo(.2f).Within(.0001f));
var middle = nav.InterpolateAlongPath(a,b,.5f);
Assert.That(middle.X, Is.EqualTo(1).Within(.0001f));
Assert.That(middle.Y, Is.EqualTo(0).Within(.0001f));
```

- [ ] **2. 赤を確認する。** `dotnet test Tests/Core/Mofumachi.Core.Tests.csproj --no-restore --filter 'FullyQualifiedName~TownFrameClockTests|FullyQualifiedName~TownMovementTests'`。新API未実装による失敗を確認する。
- [ ] **3. 時間と経路APIを実装する。** 蓄積は内部double、誤差で1ステップを失わない。返す計算回数は3まで、余剰の整数ステップを破棄し小数余りを保持。補間は経路全長×clamp(alpha,0,1)を同じ道に沿って進め、直線Lerpで角を切らない。経路探索は既存キャッシュを共有し、描画ごとの配列／LINQ生成を避ける。
- [ ] **4. 対象と全コアを実行する。** 上記対象と `dotnet test Tests/Core/Mofumachi.Core.Tests.csproj --no-restore` が全件成功・skip0。既存の2本指拒否・噴水回避・Tick(10)でも1ステップの試験を弱めない。
- [ ] **5. Commit。** `fix: preserve town motion timing and interpolate along streets`。

### 工程3：街の描画・歩行・反応と水の流れを滑らかにする

**Files:** 変更 `Assets/Mofumachi/Presentation/TownLifeView.cs`、`TownActor.cs`、`BodyReaction.cs`、`TownAmbientGraphic.cs`、`UIFlowController.cs`、`Assets/Mofumachi/Tests/PlayMode/TownLifeTests.cs`、`PopPresentationTests.cs`。作成 `Assets/Mofumachi/Tests/PlayMode/TownMotionPresentationTests.cs`。ファイル名のみのコードはPresentation、試験はPlayMode配下。

**Interfaces:** 工程2のClock／経路APIを使う。`TownLifeView.AdvanceFrame(float elapsedSeconds): void` をUpdateから呼ぶ。`TownActor.Position` は計算値のまま、追加 `RenderPosition: TownPoint`、`VisualRoot: RectTransform`。内部 `Advance(float dt)` は計算だけ、`Render(float alpha,float elapsedSeconds): void` は補間と見た目、`ResetVisual(): void` はprev=currentと歩行位相／振幅をリセット。`TownAmbientGraphic.AdvanceWater(float elapsedSeconds): void` と読み取り専用 `Phase: float` で経過時間を積む。

- [ ] **1. 失敗するUnity試験を書く。** `StationaryDragAndLifecycleStopGait`：指を保持して同じ位置なら歩行振幅0、移動で子だけ弾む、名前／足元は揺れない、release／別指／設定／ショップモーダル／中断／寸法変更で停止とClock破棄。`RenderedCornerAndArrivalUseTheSameStreet`：前工程の角をUIへ投影、足元・奥行き・入室判定が同じ道、指を離すまで入室なし。`WaterFallsAndRipplesAnimateWithoutRaycast`：異なるPhaseでMeshの位置／alphaが変わる、水筋が下へ進み波紋が広がる、raycast=false、総頂点1500以下、吹き出し1つ以下。既存ドラッグRaycast試験を子Graphicでの実depth＋GetComponentInParent<TownActor>へ対応させる。

```csharp
yield return EnterHome();
var life = Flow.GetComponentInChildren<TownLifeView>();
var hero = Flow.GetComponentsInChildren<TownActor>().Single(a => a.Controlled);
hero.Movement.TryBeginDrag(1); // targetは現在位置のまま
var name = hero.GetComponentInChildren<Text>().rectTransform.localPosition;
for (int i=0;i<30;i++) life.AdvanceFrame(1f/60);
Assert.That(hero.VisualRoot.anchoredPosition.y, Is.EqualTo(0).Within(.01f));
Assert.That(hero.GetComponentInChildren<Text>().rectTransform.localPosition, Is.EqualTo(name));
```

- [ ] **2. 赤を確認する。** Windows PlayMode `TownMotionPresentationTests` と対象 `TownLifeTests` を実行。停止中の揺れ／時間取りこぼしと新API欠落の失敗を分けて記録する。
- [ ] **3. 計算と見た目を接続する。** Clockの回数だけ6名をAdvanceし、毎フレームRender。足元親にTownActor／名前、子VisualRootにBodyGraphicを置く。位相は実経路距離、子の上下動は最大2 Canvas単位、振幅を.12秒で開始／停止。release時にprev=current、移動を止める。NPC待機・タップ反応.65秒・小さな呼吸は局所経過時間を使い、無効時の時刻ジャンプを避ける。呼吸は子のscaleを1±.005以内とし、静止時の上下動は出さない。ソートは描画足元、release時に計算足元と同期してから施設判定する。Pause中は新入力／反応を拒否する。水は経過時間で更新し、水筋・波紋の元UV／24筋／4輪／5小光を維持して濃さと下向きの流れを調整する。
- [ ] **4. 対象を検証する。** Windowsの上記対象、`PopPresentationTests`、実DLLコンパイルで成功。静止ドラッグ／曲がり角／停止直後を録画してPixelで後から照合する。脚を独立して動かすアニメーションができたと報告しない。
- [ ] **5. Commit。** `fix: smooth town actors and make fountain flow visible`。

### 工程4：3章のカタログと保存を変えない進捗モデル

**Files:** 作成 `Assets/Mofumachi/Core/ChapterCatalog.cs`、`ChapterProgress.cs`、`Assets/Mofumachi/Tests/EditMode/ChapterProgressTests.cs`。変更 `Assets/Mofumachi/Presentation/UIStrings.cs`、3TTF。`GameState.cs`／QuestManager／報酬サービスへ章の状態を追加しない。

**Interfaces:** `ChapterDefinition` は読み取り専用string `Id,Title,Objective,ClosingLine`、`QuestIds: IReadOnlyList<string>`。`ChapterCatalog.All: IReadOnlyList<ChapterDefinition>`、`Find(string id): ChapterDefinition`、`ForQuest(string questId): ChapterDefinition`（未知はnull）。`ChapterProgress.For(GameState state): ChapterProgress` は不変スナップショット；`CompletedCount: int`、`CurrentChapter: ChapterDefinition`（全完了はnull）、`CompletedInCurrent: int`（現在章なしなら0）、`AllCompleted: bool`、`LastCompletedChapter: ChapterDefinition`（なければnull）、`IsComplete(string chapterId): bool`（未知はfalse）。

- [ ] **1. 失敗するテストを書く。** `SixDeliveriesProduceThreeChaptersAnd330Coins` は既存受注／生成／合成／納品APIで6件を順に確定し、2／4／6件の章完了と70／180／330、Lv.2／4／6を検査。`LegacyAndOutOfOrderProgressRemainReadOnly`：tea-01のみ、順不同、未知ID、未知受注、全完了、後章の既知未完了受注を検査。算出前後でStateCodecの文字列が同じ、未知値を消さない。コレクションを書き換えてカタログの2IDや過去のスナップショットを変更できないことも検査する。

```csharp
var state = GameState.CreateInitial(); state.coins=30; state.townGrowthLevel=1;
state.completedQuestIds.Add("tea-01"); state.completedQuestIds.Add("legacy-extra");
var before = StateCodec.Encode(state); var progress = ChapterProgress.For(state);
Assert.That(progress.CurrentChapter.Id, Is.EqualTo("chapter-01"));
Assert.That(progress.CompletedInCurrent, Is.EqualTo(1));
Assert.That(progress.CompletedCount, Is.EqualTo(1));
Assert.That(StateCodec.Encode(state), Is.EqualTo(before));
```

- [ ] **2. 赤を確認する。** `dotnet test Tests/Core/Mofumachi.Core.Tests.csproj --no-restore --filter FullyQualifiedName~ChapterProgressTests`。章API未実装の失敗を確認。
- [ ] **3. 読み取り専用モデルを実装する。** `chapter-01`「はじめてのお茶会」／tea-01,bread-01、`chapter-02`「花の香る広場」／flower-01,tea-02、`chapter-03`「もふまちのお祝い」／flower-02,bread-02。Objectiveは設計表の正式文言。ClosingLineは順に「仲間と囲む、最初のお茶会ができました。」「花の香る広場で、みんながひと休み。」「みんなのお茶会がひらけました！」。現在章は既知の未完了受注の章を優先、それ以外は最初の未完了章。最後の完了章は完全な2件を持つ章の最大順。未知受注の表示はQuestCatalog既存エラーを保持する。UIStrings.Allへ章の全表示文を登録し、工程1CLIで3書体を再生成する。
- [ ] **4. 対象と全コアを実行する。** 対象／全コア成功・skip0、Fontチェック0。`saveVersion=3`、納品保存失敗／連打／旧1件完了データの既存試験を維持。
- [ ] **5. Commit。** `feat: derive three story chapters from existing quest progress`。

### 工程5：目的カード、章末、最初の物語完了と安全な復帰

**Files:** 作成 `Assets/Mofumachi/Presentation/ChapterPresentation.cs`、`ChapterSummaryScreen.cs`、`Assets/Mofumachi/Tests/PlayMode/ChapterFlowTests.cs`。変更 `Assets/Mofumachi/Presentation/ScreenContext.cs`、`UIFlowController.cs`、`TopHomeScreens.cs`、`QuestMergeScreens.cs`、`RewardGrowthScreens.cs`、`UIStrings.cs`、3TTF、`Assets/Mofumachi/Tests/PlayMode/LayoutNoticeTests.cs`、`SettingsScreenTests.cs`。ScreenIdの実ファイルはScreenContext.cs。

**Interfaces:** `ChapterPresentation.Create(GameState state,string chapterId): ChapterPresentation`（未完了／未知章はArgumentException）。読み取り専用 `ChapterId,Title,Objective,ClosingLine,FavoriteCharacterId,NextChapterTitle: string`（NextChapterTitleは算出した現在章のTitle、なければ空文字）、`CompletedCount,Coins,TownLevel: int`、`IsFinale: bool`。`IsFinale` は全6件完了かつchapter-03。UIFlowへ `LastChapter: ChapterPresentation`、`ContinueAfterGrowth(): void`、`OpenChapterSummary(string chapterId): bool`、`ReviewLastChapter(): bool` を追加。enum `ScreenId.ChapterSummary` を追加し、`ChapterSummaryScreen.Build(RectTransform root,ScreenContext c,ChapterPresentation chapter): void` で章末／finaleを表示する。

- [ ] **1. 失敗するUnity試験を書く。** `EveryChapterHasExplicitClosureAndFinale`：実納品→報酬→成長→明示ボタンで2／4／6件目だけ章末、1件目はホーム、finaleは笑顔6名／6/6／実CoinsLv.／戻る2ボタン、追加依頼なし。`ChapterReentryAndSaveFailureNeverPayOrAdvance`：2件目の納品失敗で完了章なし、成功後連打／設定／再選択／まとめ再表示で報酬と消費が不変。章末を開く保存に失敗したら元画面、TOP保存失敗でも元画面。章末途中の再起動はホーム、任意のまとめから再払いなし。`UnknownActiveQuestDoesNotPretendStoryComplete`：未知受注の値を保持し、全完了ではないのに終了文や新受注を提示しない。`ChapterPagesFitShortPortraitAndActualValues`：全3章とfinale、int.MaxValue HUD、各サイズで文字／ボタンの重なり0、名12／説明14／見出し18以上、bestFit=false。

```csharp
// ChapterFlowTests：2件の実納品を終えたGrowth画面で、実進行値を記録する。
var coins = Flow.Game.State.coins; var level = Flow.Game.State.townGrowthLevel;
var rewards = Flow.Game.State.claimedRewardIds.ToArray();
Flow.ContinueAfterGrowth(); Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.ChapterSummary));
Flow.ShowSettings(); Flow.GoBack();
Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.ChapterSummary));
Assert.That(Flow.Game.State.coins, Is.EqualTo(coins));
Assert.That(Flow.Game.State.townGrowthLevel, Is.EqualTo(level));
Assert.That(Flow.Game.State.claimedRewardIds, Is.EqualTo(rewards));
```

- [ ] **2. 赤を確認する。** Windows PlayMode `ChapterFlowTests` と章表示を足した `LayoutNoticeTests`。追加API／画面の不足で失敗することを確認。
- [ ] **3. 目的と章末の導線を実装する。** Home目的カードの章名18、完了数は別の操作数値、実依頼者／次の目的14。初回だけ「仲間とお茶会をひらこう」、受注中はその章、完了後は「最初の物語をふりかえる」。Homeカード高さ92、下端136、街可動域下端240；最後の章のまとめ48高は下端78のFeedback帯と排他的に置く。各短い縦画面で実計測し、必要ならパネルを広げる。依頼／Mergeの既存要求品・数量・受注は変えず章名を補足する。QuestCatalogがnullでもAllCompleted=falseなら「保存された依頼を確認できません。」と安全な戻るを表示し、受注や完了を装わない。成長の明示ボタンは `ContinueAfterGrowth`、成功したLastDeliveryの章が完了ならSummary、それ以外Home。SummaryはHome／Growthからのみ開き、Pause／モーダル／遷移中は拒否。共通のPersistを成功させてからLastChapterを作成・確定し、Render("chapterSummary")で描画する（Navigateによる先行描画／二重保存を避ける）。設定復帰とAndroid戻るを接続する。画面再表示は受注／納品／付与を呼ばない（通常の画面遷移保存は維持）。
- [ ] **4. 画面と復帰を検証する。** 上記、`SettingsScreenTests`、`CharacterSelectionTests`、`MultiQuestPresentationTests` が成功。finale文は設計の2文と「街へ戻る」「保存してTOPへ」、章1／2は「次の章へ」でHomeへ。長い強制待ちなし、再起動で自動章末なし。Font `--check` と実DLLコンパイルも成功。
- [ ] **5. Commit。** `feat: connect chapter goals closures and a safe first-story finale`。

### 工程6：Lv.4〜6の街に残る装飾と成長時の注目点

**Files:** 作成 `Assets/Mofumachi/Resources/Mofumachi/TownDecorationAtlas.png`、`Assets/Mofumachi/Presentation/TownDecorationGraphic.cs`。変更 `Assets/Mofumachi/Presentation/TownGrowthView.cs`、`RewardGrowthScreens.cs`、`TownLifeView.cs`、`UIFlowController.cs`、`UIStrings.cs`、3TTF、`Assets/Mofumachi/Editor/AndroidBuild.cs`、`Assets/Mofumachi/Tests/BuildTime/PresentationContentTests.cs`、`Assets/Mofumachi/Tests/PlayMode/TownLifeTests.cs`、`TownMotionPresentationTests.cs`、`ChapterFlowTests.cs`、`docs/town-growth-direction.md`、`docs/ui-pop-assets.md`。

**Interfaces:** `TownDecorationKind {WelcomeRibbon,FlowerArch,CelebrationGarland}`。`TownDecorationGraphic.Bind(TownDecorationKind kind): void` は共有atlasの固定3領域を描くMaskableGraphic、RequireComponent(CanvasRenderer)、raycast=false。既存 `TownGrowthView.Apply(int townLevel): void` を0〜6外観へ拡張。`Bind(ScreenContext c,RawImage background): void` を追加し、既存 `Bind(ScreenContext c,TownLifeView life)` と同じUV投影で静的な成長プレビューを作れるようにする。`HighlightNewStage(int previousLevel,int currentLevel): void` は追加部分だけ約1秒強調し、非表示／中断で解除、保存値を変更しない。

- [ ] **1. 失敗する素材／街試験を書く。** `TownDecorationsUseEffective1024Limit`：実読み込み幅高≤1024、Default／Android有効設定、mip／readable=false、3領域alphaと素材存在。`GrowthLayersFollowSavedLevelWithoutChangingPaths` をLv.0〜6／int.MaxValueへ拡張、意図した外観上限だけ3→6へ変更。`GrowthHighlightStopsAndDecorationNeverTakesInput`：中断で強調解除・最終外観保持、背景UVを変えても街に固定、入口／主人公のRaycast成功。新Graphicを初回描画・RectMaskの回帰試験へ含める。

```csharp
foreach (var level in new[]{0,1,2,3,4,5,6,int.MaxValue}) {
    growth.Apply(level);
    Assert.That(growth.VisibleStage, Is.EqualTo(Mathf.Min(level,6)));
}
Assert.That(Flow.GetComponentsInChildren<TownDecorationGraphic>().All(g=>!g.raycastTarget), Is.True);
```

- [ ] **2. 赤を確認する。** Windows EditMode `PresentationContentTests`、PlayModeの拡張 `TownLifeTests`／`ChapterFlowTests`。素材未存在と外観上限3の失敗を確認。
- [ ] **3. 固定装飾と見せ方を実装する。** 透過の歓迎リボン／花アーチ／ガーランドのみをimage_genで新規制作し、既存の明るいPOP・花・細い金縁へ合わせる。1枚の共有atlas、キャラ／背景を描き変えない。元画像と生成記録・配置領域を素材記録へ保存。既存9装飾の位置は維持；歓迎リボン(.58,.69)、花アーチ(.20,.665)、ガーランド(.42,.77)を元背景UVへ置き、順に幅64／52／140 Canvas単位を基準に入口と道を隠さない。保存Lv.だけから再構築。成長画面には背景RawImage＋装飾の静的プレビューを作り、新Bindを使って旧→新で増えたlayerのみ強調する（移動する6名を複製しない）。Pause処理はTownGrowthViewも停止する。高Lv.データを6へ書き戻さない。
- [ ] **4. 素材・入力・復帰を検証する。** 対象Unity試験、Fontチェック、実DLLコンパイルで成功。Lv.1〜3の旧外観、Lv.4〜6の違い、短い縦画面で本文／ボタン／入室を妨げないことをPixelで確認する。PNGだけで入力や性能を合格扱いしない。
- [ ] **5. Commit。** `feat: retain chapter growth with ribbons arches and garlands`。

### 工程7：全体レビュー、Windowsゲート、Pixel試遊と設計の評価

**Files:** 変更 `scripts/build-android.ps1`、`scripts/tests/android-tools.tests.ps1`、`docs/pixel3a-quality-status.md`、`docs/development-handoff-2026-10-11.md`、`docs/google-play-release-readiness.md`、`README.md`。作成 `docs/chapter-game-pixel3a-checklist.md`。修正が必要な製品ファイルは原因と回帰試験を記録してから変更する。

**Interfaces:** 既存 `Assert-TestRun(string ResultPath,int MinimumTests,string[] RequiredSuites)`、`build-android.ps1 -TownQa`、`install-pixel3a.ps1 -Apk <path> -Serial 99RAY1BELH -PackageName com.mofumachi.merge.qa.townlife` を維持。QA ID／出力名／既存保存領域を増やさない。追加必須SuiteはEditMode `ChapterProgressTests,TownFrameClockTests`、PlayMode `StoryTypographyTests,TownMotionPresentationTests,ChapterFlowTests`。

- [ ] **1. 失敗するビルドゲート試験を追加する。** `ChapterBuildGateRejectsMissingSuites`：既存90／43のPassed XMLのみでは拒否、新Suite1つ欠落／skip／failed／件数不一致は拒否。新Suiteをすべて含む今回の全Passed XMLなら通る。ADB模擬試験はIDと-r維持、保存消去／別IDへの誤導がないことを維持する。
- [ ] **2. 赤を確認する。** `pwsh -NoProfile -File scripts/tests/android-tools.tests.ps1`。旧ゲートが新Suite欠落を通す失敗を確認する。
- [ ] **3. 新ゲートを実装する。** 既存必須Suiteへ上記5Suiteを追加。全試験の実発見数を確認して最低数を更新し、現行90／43より下げない。未実行なら最低数の上乗せを推測で確定せず、新Suite検査を先に追加し実測待ちと記録する。今回結果XML、git SHA、APK SHA、actual IDの照合と非消去インストールを維持する。
- [ ] **4. コード全体を検証し、独立レビューを1回行う。** 全Core、6asmdef個別コンパイル、3フォント、画像設定、PowerShellゲートを実行。Windows Unity6000.6.4f1でEditMode／PlayMode全件成功・skip0を受領してからTown QA APK。前回の1024失敗も含め、実行結果／失敗／未実行を別々に記録する。採用済みの順次方式に従い、最後に1人の独立レビューで仕様・保存・入力・表示・ビルドを確認し、指摘があれば対象試験で修正検証する。
- [ ] **5. Pixelで第1章の試遊を評価する。** Town QAに-rで更新し、初回／既存途中の両方をデータ消去なしで確認。第1章終了時にユーザーのFBを受ける：次の目的が自然に分かるか、6名への親しみ、指の移動／停止、街の変化が成果に見えるか、読みやすさ／水と動き。必要な設計変更は理由・変更案・保存への影響を文書化し、重大な方向変更は具体案を確認してから反映する。公開日に合わせて未合格項目を省略しない。
- [ ] **6. 全3章を端末で確認し、12項目の引き継ぎを更新する。** 6依頼→3章末→finale、設定／再選択／TOP、BGM／SE、生成系統／ドラッグ／片づけ、旧1件完了から続行、章末と成長演出中の強制停止／再開。10分以上の操作でFPS・発熱・メモリ・フリーズ・クラッシュを記録。1080×2220、Safe Area、全画面、文字切れ、押しやすさ、最低30fps目標を測定条件付きで評価。セーブの実値保持・報酬1回・Lv.6装飾復帰を確認する。端末を操作できない場合はチェックリストとWindows手順を完成させ、ユーザー結果待ちと明記する。
- [ ] **7. Commit。** `test: gate chapter QA and document Pixel trial evidence`。変更ファイル／完成・仮・未実装／最新SHA／main反映／必要なWindows手順／次3〜5件／公開準備率と最大リスクを報告する。正式音源・署名AAB・Play提出条件は次の公開判断に残し、章実装だけで公開可能としない。

## 計画の照合と見直し地点

設計1〜3（方向・起動導線）は工程5／7、4（3章6依頼）は4／5、5（全画面・締め）は1／5、6（街成長）は6、7（書体）は1、8（動き）は2／3、9（保存・例外）は4／5、10（責任分離）は各Interfaces、11（完成判定）は7に対応する。Review Focusの5項目は上記の名前付き試験へ割り当て済み。

第1章の試遊は価値を評価する地点で、設計変更を禁止するゲートではない。文字・動き・見た目の改善は継続し、6名の正式デザイン・既存セーブ・支払いの保証を守る。独自性は試遊で伝わる体験として評価し、新機能の数で代用しない。日程はその評価と安定性の結果から見直す。
