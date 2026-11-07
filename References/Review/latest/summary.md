# 第7工程：進行UI実装・専用Windows確認待ち

- branch：`feat/daily-world-pass-20261016`
- 実装commit：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`
- ランチャーcommit：`eaefce789728bcf08059c717fda5d92e93ce5db4`。いずれも通常push済み。
- 状態：**修正済み・未確認／Windows実行待ち**。**第7工程 complete=false**。
- 第6工程は製品68f0e5c、Windows85/85 Passedで正式合格済み。再実装・再実行なし。
- 目的：既存枠内の進行表示と、主要導線の限定結合確認。

## 表示と更新

既存Merge補助Textを「**所持24P ・ 生成2P**」の実値表示に使用。財布はGameStateManager.MofuPointsだけ、生成コストは既存LevelOneGenerationCostを参照する。狭い画面では既存通知が補助行を隠すため、生成成功の既存Feedbackにも確定残高を表示。通知中の回復後・再描画・サイズ変更後も元の数値を使わず、同じ管理元から現在値を再取得する。0/1P不足時も既存Feedbackに不足メッセージと実残高を表示する。新Panel／Popupなし。

既存次目標2行を「**Lv3まで おねがい3/4**」「**まち交流32/40**」相当へ変更。12件の既知ユニーク完了数とtownActionTotalを使い、分母はTownGrowthServiceの正式配列から取得。UIへ別の閾値表を定義していない。Lv6では「**街Lv6 MAX**」として次条件を消す。Home／依頼カード／今日の案内に反映し、旧Story6の完了判定は維持。

更新はBind／再表示／保存成功通知だけ。GameStateManagerと、独立保存するQuestManagerの成功後に通知する。毎フレーム取得・別残高キャッシュなし。Save中の仮値は通知せず、失敗時は元の表示を維持し、既存Clone／CopyFromで状態復元。View例外で保存済み取引を失敗へ戻さない。

Core変更は通知と正式閾値の読み取りAPIだけ。Save version4、移行／pre-v4、EconomyState、MofuPointService、生成2P消費、QuestCatalog12件、成長表／判定、Story／章、MergeBoard、TownActor、原画、フォント／BGM、Visual Lock／Guard、Packages／ProjectSettings、前工程ランチャーは無変更。既存Textの座標・枠・ボタン配置・色は変更していない。

## ユーザー確定：新規24Pと回復入口

従来Initializeは新規にも回復16Pを即加算して40P表示にしていた。この衝突を確認し、ユーザーから**新規開始直後24Pを維持する方針**の明示回答を受けた。

新規LoadStatus.NewGameでの開始時回復と、通常会話／散策／花受領直後の付随自動回復を外した。既存「今日」画面を開く時、実際のアプリ復帰、既存セーブ再開時を回復入口として維持する。16P・1日1回・96P上限・累計対象外・時計非後退・rollbackは変更なし。初期24Pの再配布でもない。採用内容は`docs/game-design/latest.md`へ保存済み。

## 確認と未確認

新`Mofumachi.Tests.ProgressUiIntegrationTests`は**55ケース**。初期24P／無料Tea2個／新ルート、3系統生成、0/1/2P、96P、4/6/8報酬、回復・追加・おたすけ累計除外、閾値、12件ユニーク、MAX、Story6、納品、日付、Save/Load、9種の保存失敗時UI、購読切替、実UGUI枠、compact通知中と回復・サイズ変更後の残高表示を含む。

SaveServiceTestsでは第6工程で正式化した新規ExpandedNewPlayer=2に合わせ、古いNewPlayer=0期待の1行だけを整合。移行assertを削除せず33件を維持し、他の既存fixtureは変更していない。

| Windowsフェーズ | 対象 | 期待件数 | 実結果 |
| --- | --- | ---: | --- |
| 直接確認 | 新ProgressUiIntegrationTestsだけ | **55** | **未実行** |
| 結合確認 | 限定9fixture | **444** | **未実行** |

結合対象：SaveServiceTests33、PreferencesTests23、CharacterPreferenceTests7、MofuPointServiceTests68、GenerationCostTests33、TownPointRewardTests69、RecoveryExtraHelpTests71、QuestTownLevelTests85、新ProgressUiIntegrationTests55。両フェーズの実XML件数は**未取得（null）**。

静的C#コンパイルはCore／Presentation／選択9fixtureで最終エラー0・警告0。初回は新テストのreceipt準備でCS1503を検出し、既存WalkRewardReceipt型を確認して当該テスト準備だけ修正。元メッセージと解決をerror.logへ記録した。Windowsテスト失敗や再試行ではない。

独立レビュー1回の重要指摘「compact通知で唯一の残高行が隠れる」へ、既存通知の最小修正と実ProductionFrameLayout回帰4ケースを追加。静的コンパイル成功、55ケース実合格はまだ主張しない。枠へ収まらなければWindowsの直接確認で停止し、UIを勝手に作り直さない。

PowerShell5.1構文/API・UTF8 BOM・ASCII CRLF CMDを静的確認。合成XMLの55／444受理と範囲外／重複／非Passed拒否、一時Gitの共有3条件が成功。合成データは実Unity証跡へ共有していない。**クラウドUnity起動0、実テスト0、自動再試行0**。全件／PlayMode／APK／Pixel／Visual撮影なし。

## Windowsの次の操作

最新版取得後、**`scripts/verify-progress-ui-integration-windows.cmd` を1回ダブルクリック**。

Windows11／PowerShell5.1／Unity6000.6.4f1既存ライセンス／Android target。実装commitを隔離worktreeへ固定し、**55件だけを先に実行**。その合格・source不変確認後だけ**限定9fixture444件を1回**。Unity合計最大2回、両方に`--burst-disable-compilation`、自動再試行0。直接確認で失敗すれば結合へ進まず停止する。

専用Diagnosticsへphase別XML／Unity log／引数、source、機械判定JSON、失敗時の最初のエラー／phase／fixtureを保存。既存attemptがあれば両方とも再起動しない。同操作で**`References/Review/inbox/latest-progress-ui-integration/`**へ必要な小容量テキストだけをSHA・生バイト確認付きで共有する。

共有はremote先端の別worktreeからinboxだけcommit・通常push1回。他の未コミット／未pushを混ぜず、旧共有runはprevious-runsへ保持。大きいlogは原本保持と理由記録。push失敗は再試行せず原本と理由を残す。

## 第8工程判定

**第8工程「実機Pixel 3a確認＋リリース候補ビルド」へ進んで安全か：NO**。

主要ブロッカーは**Windows直接55件と限定結合444件が未実行**の1点。実装済みを工程完了／UI実描画合格とは扱わず、ここで停止する。

summary/statusのcommitは実装、launcher_commitは専用検証入口を示し、文書自身のcommitとは区別する。
