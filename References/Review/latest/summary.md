# 第6工程：12依頼＋街Lvの2条件／Windows確認待ち

- branch：`feat/daily-world-pass-20261016`
- 実装commit：`68f0e5c479716029b31ff3e92da47335da25f957`
- ランチャーcommit：`5cbe79068ba0c6096b1d806acc3cb530033f6524`
- 公開先端：`0b74d1d684f50df498197c53776d23076a19264f`。実装・ランチャーは通常push済み。
- 状態：**修正済み・未確認**。第6工程complete=false、Windows実行待ち。
- 第5工程はユーザーの最新指示どおり実装 `0c22f30fae41c18471fe771f67153e4828716629` のWindows **71/71 Passed・合格済み**として扱う。再実装・再実行・再検証なし。共有commit `c0780638836240827dee8d1a145fa6fec52c6ee1` は原本のまま履歴へ取り込んだ。
- 目的：旧6依頼・Story互換を守って12件化し、依頼数AND街行動累計で街Lvを非後退更新する。

## 依頼定義と順序

旧6件のID・要求・Coins・Requester／Title／Dialogueは定義ブロックをそのまま維持し、元ソースとの一致を確認。ChapterCatalog／既存Story本文も変更していない。

| 旧ID | 要求 | Coins | Chapter |
| --- | --- | ---: | --- |
| tea-01 | Tea Lv2×1 | 30 | chapter-01 |
| bread-01 | Bread Lv2×1 | 40 | chapter-01 |
| flower-01 | Flower Lv2×1 | 40 | chapter-02 |
| tea-02 | Tea Lv3×1・Bread Lv2×1 | 70 | chapter-02 |
| flower-02 | Flower Lv3×1・Tea Lv2×1 | 70 | chapter-03 |
| bread-02 | Bread Lv3×1・Flower Lv2×1 | 80 | chapter-03 |

| 新ID | Tea | Bread | Flower | Lv1換算 | Coins |
| --- | --- | --- | --- | ---: | ---: |
| loop-a-01 | Lv1×2 | Lv1×2 | Lv2×1 | 6 | 30 |
| loop-a-02 | Lv2×1 | Lv2×2 | Lv2×2 | 10 | 40 |
| loop-a-03 | Lv3×1 | Lv3×2 | Lv2×2 | 16 | 50 |
| loop-a-04 | Lv3×2 | Lv2×2 | Lv3×2 | 20 | 60 |
| loop-a-05 | Lv2×2 | Lv3×3 | Lv3×2 | 24 | 70 |
| loop-a-06 | Lv3×3 | Lv3×2 | Lv3×2 | 28 | 80 |

新6件の表示用Requester／Title／Dialogueは既存6件のものを再利用し、旧Story本文・新フォント資産を追加変更しない。新IDは旧Chapterへ登録しない。

新規順序：
`tea-01 → bread-01 → loop-a-01 → flower-01 → loop-a-02 → tea-02 → loop-a-03 → loop-a-04 → flower-02 → loop-a-05 → loop-a-06 → bread-02`。

既存ユーザーは進行中の旧依頼を優先し、未完了旧6件を従来順、その後loop-a-01～06。完了旧依頼を再要求しない。既知の未完了activeQuestIdは常に優先、未知のactiveは勝手に差し替えない。正式カタログの受注入口でも選択順を確認し、既存の任意カタログを渡す仕組みは維持する。

### 既存v4と新規ルートの区別

**保存項目は追加せず、Save version4を維持**。既存のeconomy.questRoute列挙へ `ExpandedNewPlayer=2` を追加し、今回以降のCreateInitialだけが2を記録する。既存値 `NewPlayer=0`／`LegacyPlayer=1` は番号も保存内容も変更せず、旧6件優先として扱う。

追加前のv4でも値0が保存されているため、0を新12件ルートへ解釈し直すと旧ユーザーへ割込みが発生する。既存フィールド内の値2で区別し、残高・進行状況からの推測移行や重複フラグを作らない。新2の再読込も混在順を維持。v1／v2／v3→v4のLegacy1補完・原本保護・SaveServiceは変更なし。

## Story6と全12の分離

- `QuestCatalog.Story6Complete` と `ChapterProgress.AllCompleted / Story6Complete` は旧6件だけ。
- `QuestCatalog.All12Complete / ChapterProgress.All12Complete` は全12件。
- どちらも既存完了IDから導出し、新しい保存boolは追加しない。
- 旧Story完了は追加6件が未完でも維持。loop依頼はChapter対応なしなので、追加納品から旧エンディング・章完了演出を再起動しない。
- 報酬は既存のCoinsのまま。旧330＋新330＝全12件660 Coins。もふポイント報酬へ置換していない。

## 街Lv

| Lv | 既知12件のユニーク完了数 | townActionTotal |
| --- | ---: | ---: |
| 1 | 1 | 4 |
| 2 | 2 | 8 |
| 3 | 4 | 40 |
| 4 | 6 | 96 |
| 5 | 9 | 160 |
| 6 | 12 | 224 |

両方を満たす最大Lvを計算し、**max(保存済みLv, 計算Lv)** を保存。最大計算Lv6、重複・未知IDは完了数へ加算しない。Lvを一度に飛び越せる。保存済みLv5／6を下げない。範囲外の既存7等は従来の読込検証で停止し、6へ丸めない。

再評価は依頼納品、主取引中の街行動累計増加、読込／移行／新規初期化後のGameStateManager構築。納品と通常行動のLv変化は**元の保存1回**へ含め、既存Clone／CopyFrom／rollbackで復元する。初期化時は上昇する場合だけ保存。初期化の保存失敗は旧状態へ戻し、既存UI読込停止ゲートでsessionを外すため、その後の回復・終了保存による自動再試行を防ぐ。新rollbackなし、毎フレーム計算なし。

回復16P・追加2P・おたすけ4P・初期24P・生成消費・購入／補償／debugは累計へ接続していない。累計が増えない取引ではLv再評価を呼ばず、第4・第5工程の額面や台帳は変更しない。

参考検算：旧24＋新104＝128個相当、初期無料Tea Lv1×2を除いて生成126個相当・252P、合成84回。閾値224／通常累計最大32で理論7日、8～9日の参考想定は変更していない。実プレイ時間を測定した結果ではない。

## 変更ファイル

Coreの `QuestCatalog.cs`、`QuestManager.cs`、`ChapterProgress.cs`、`TownGrowthService.cs`、`GameState.cs`（既存enumと新規初期値だけ）、`GameStateManager.cs`、Presentationの `UIFlowController.cs`（起動時停止ゲートだけ）、新規 `QuestTownLevelTests.cs` と `.meta`、専用CMD／PS1、固定summary／status。

通常4／6／8P、回復16P、追加2P、おたすけ4P、上限96P、初期24P、生成2P、日次台帳、SaveService移行／pre-v4、ChapterCatalog／既存Story本文、TownActor、Visual Lock、フォント、BGM、正式画像、5×6盤面・ボタンデザインは変更なし。前工程ランチャー10ファイルも生バイト不変。

**UI変更はせずロジックのみ**。次Lvの数値表示は第7工程。既存「あと1お届け」等の案内は新2条件へ合わせる表示整合が次工程に残る。配置・色・サイズを推測で変えていない。

## 確認結果と未確認事項

- 新規fixture：`Mofumachi.Tests.QuestTownLevelTests`、**期待85ケース**。実Total／Passed／Failed／Skippedは**未取得（null）**。
- 旧新定義／12件順／v4の0・1・2ルートと再読込／Story6とall12／全境界と境界-1／非後退・飛越し／既知ID数／読込と旧版移行／主取引rollback／同一保存／納品二重実行／累計対象外を含む。
- Core／Presentation／新テストコードの静的C#コンパイル各1回、エラー0・警告0。Unityテスト合格の代わりにはしない。
- 独立した読み取りレビュー1回、重要指摘0。軽微な未確認はUIFlowController.Initializeの停止・終了経路が静的確認のみである点。純粋な新fixtureはUI本体を生成しない。
- 古い「カタログ6件固定」「納品ごと1Lv」「saveVersion3」を前提にする歴史的テストは今回実行・弱体化していない。第7工程の全体結合時に現行仕様との整合確認が必要。
- PowerShell5.1構文／API／BOMの静的確認、ASTと85件限定EditMode引数の確認成功。一時Git環境の共有3条件（remote先行保護・push拒否1回停止・custom index拒否）も各1回成功。合成データはGitHub実行証跡へ入れていない。
- **Unity起動0回、対象EditMode0回、自動再試行0回**。全件・PlayMode・APK・Pixel・Visual撮影・フォント工程なし。最初のUnity/test失敗は未実行のため未確認。
- error.logは旧工程の履歴として保持し、今回の失敗へ流用しない。

## Windowsの次の操作

**`scripts/verify-quest-town-level-windows.cmd` を1回ダブルクリック。**

Windows11／Windows PowerShell5.1／Unity6000.6.4f1の既存ライセンス、Android target。実装 `68f0e5c479716029b31ff3e92da47335da25f957` の隔離worktreeを固定し、上記fixtureだけをEditModeで実行。Unity最大1回、自動再試行0回。元作業ツリー・staged・他コミットを変更せず、Guard変更・無効化／Visual Lock変更なし。

原本：`Builds/Diagnostics/quest-town-level-<timestamp>-<id>/`。results.xml／unity.log／source-revision.txt／quest-town-level-result.json、失敗時first-error.log。85/85、Failed／Skipped／Inconclusive0、exit0、source入力不変を必須とする。

同じ操作内で **`References/Review/inbox/latest-quest-town-level/`** へ必要な小容量テキストだけコピー・対象だけcommit・通常push1回。remote先端を親とする別共有worktreeで他の変更を混ぜず、SHA／生バイト長をrun-info.jsonへ記録。1ファイル4MiB／合計16MiB、大きいunity.logは原本保持と理由記録を行い、XML／JSON／最初のエラー等を共有。

失敗は理由と原本・share-result.jsonを保持して停止。既存試行receiptがある場合はUnityを再起動せず、保存済み結果の共有だけを扱い、共有済みならpushもしない。

## 第7工程判定

**第7工程「進行UI表示＋全体結合確認」へ進んで安全か：NO**。

主要ブロッカーは**第6工程のWindows実Unity85ケースの結果未取得**。実装済みを工程合格とは扱わない。自動共有された同一runを次のレビューで確認し、今回は第7工程へ進まない。

statusのcommitは確認対象実装を示し、ランチャー・共有文書自身のcommitと区別する。
