# 『もふまちメルジュ』開発引き継ぎ — 2026-10-11・3章版

最新のMerge品質改善・Windows手順は[2026-10-12の引き継ぎ](development-handoff-2026-10-12.md)を参照してください。以下は前版の記録です。

承認された[7工程](superpowers/plans/2026-10-11-chapter-game.md)を順次実装。正式6名・既存セーブv3・6依頼・5×6盤面を維持。「仲間へのお届けが街の景色に残る暮らし」を軸にします。10月公開は目標で、第1章の試遊で設計と日程を見直せます。

**最新のWindows受領結果：PlayMode52件中48成功・4失敗・skip0。下記4件の修正ソースを用意しましたが、Windowsでの再実行はまだ必要です。修正後のクラウドではコア87成功／失敗0／skip0、Unity6000.6.4f1実DLLで6アセンブリ個別コンパイル0警告／0エラー。新APK、Pixel実機の一周／保存／性能は未確認です。コンパイルを実機合格としません。**

## 1. 現在の実装状況

「仮実装」はコード接続済みでネイティブ／実機の完成判定待ち。

| 画面・機能 | 状態 | 現在の内容 |
|---|---|---|
| TOP | 仮実装 | 光沢ロゴ・全身パートナー・初回購入案内。物語の説明を絵本調へ |
| 主人公選択 | 仮実装 | 6名の全身・紹介・保存・再選択。名前12以上／紹介14 |
| ホーム／街 | 仮実装 | 章の目的／実進行、自由ドラッグ、5名の往来・会話、名前・足元と姿の動きを分離 |
| キャラ依頼 | 仮実装 | 6依頼・章名・実要求数／報酬。未知受注は安全な案内 |
| Merge | 仮実装 | 5×6、茶器／パン／花のLv.1〜3、系統選択、ドラッグ／2タップ、片づけ確認 |
| 納品／報酬 | 仮実装 | 保存済み納品の実値スナップショット、消費と報酬の再払い防止 |
| 3章・物語完了 | 修正中 | 各章2依頼、明示章末・6名のお祝い。短い縦画面の説明枠を拡張、Windows再確認待ち |
| 街成長 | 仮実装 | Lv.1〜3の花／星を保持、4リボン・5花アーチ・6ガーランド。静的プレビューと新しい層だけ強調 |
| ショップ | 未実装 | 入口と準備中案内のみ。販売は追加しない |
| 設定 | 仮実装 | BGM／SE個別ON/OFF／音量、選び直し、元画面・TOPへ戻る。章末からも復帰 |
| セーブ／復帰 | 完成（コア） | v3・既存値保持、失敗巻き戻し、バックアップ。ネイティブ／Pixel再起動は未確認 |
| BGM／SE | 仮実装 | 既存BGM1＋6SE候補、分離AudioManager。正式音源は未採用 |
| Androidビルド | 仮実装 | Windowsゲートへ新5クラス追加。Town QA ID／保存領域を維持。今回APKは未生成 |

## 2. 今回実装・修正した内容

今回の4件への対応：

- UI：章末の説明枠を高さ40→48へ。ユーザー環境の実測44を収め、14の文字サイズを維持。592／640／808の各高さで、文字切れと実値の合計欄への重なりを検査します。
- キャラ関連：2件のテストが全身画像を旧配置から参照していたため、子の表示部品を参照し、存在・所属・キャラID／笑顔を検査。足元と姿を分離した滑らかな動きの構造は維持。
- 中断／復帰：中断中にドラッグを開始する旧テスト手順を修正。「中断中は拒否→復帰後に開始→再中断で表示を除去→アイテムを保持」を検査。製品の中断中入力防止は維持。
- ロジック・保存・報酬・原画・音源・Android設定の変更なし。テストの削除・skip・合格条件の緩和なし。PlayModeの予定件数は受領結果と同じ52件。

以下は直前の3章版の実装内容です。

- UI：Kiwi Maru Mediumの同梱subset（OFL）を名前・会話・章・物語へ。操作／数量／課金注意はZen Maru Medium／Bold。自動縮小なし。タイトルの既存光沢ロゴは維持。
- ロジック：完了済み6依頼IDから読み取り専用の3章進行を算出。未知ID保持、既知未完了受注を優先、追加報酬・保存フラグなし。
- キャラ／動き：1/30秒・最大3ステップの移動、余り保持・停止負債の破棄、道の角に沿う描画補間。移動距離で姿だけ最大2単位弾み、開始／停止約.12秒。静止上下動を廃止。反応.65秒／小さな呼吸、Pause・別指・退出を制御。
- 街：水筋を下向きに進め、濃さを調整。6名・吹き出し1つ・経路と入口を維持。新しい透過共有atlasでLv.4〜6装飾を保存値から再構築。成長画面にNPCを複製しない。
- 章末：Home／Growthから保存成功後だけ開く。設定／再選択でスナップショット保持。再起動はホーム、任意の再表示で再払いしない。
- BGM／SE：差し替えなし。音候補の正式採用は別工程。
- Android：新Suite欠落・空のPassed Suite・skip／failed／件数不一致を拒否。旧90／43件XMLだけでは今回APKの合格にならない。インストールは-r、消去なし。

## 3. 今回のFBに対する対応状況

| FB | 対応状況 | 内容／残る確認 |
|---|---|---|
| ① メルヘン調フォント | 対応中 | 絵本調書体へ接続・字形確認済み。短い縦画面のネイティブ測定／実機待ち |
| ② 上品なUI品質 | 対応中 | 光沢・金縁・花・リボンを保持し、章の目的／締め／成長プレビューへ。実機の枠内密度・文字位置は評価待ち |
| ③ アクション・環境 | 対応中 | 補間／停止／距離連動の姿の動き、水流を修正。滑らかさはPixelで判定。独立した脚や手を振るコマは追加なし |
| ④ 街の移動導線 | 対応中 | 指の自由移動・到着後の明示入室を維持。依頼部屋へ、ショップは準備中。入口操作／見え方は再確認待ち |

## 4. コアループ完成状況

| 流れ | 確認状況 |
|---|---|
| TOP→ホーム | 旧版の表示証拠あり。今回版の実機操作は未確認 |
| ホーム→依頼→Merge | コード・テスト接続済み、ネイティブ／実機未確認 |
| 合成→納品→報酬→街成長 | コア動作確認済み（87件）。今回UIの実機一周は未確認 |
| 各章末→最初のお祝い | 一部不具合あり（Windowsの短い縦画面の文字枠）。修正ソースあり、再確認待ち |
| セーブ／復帰 | コア動作確認済み。Pixelの強制停止／再起動は未確認 |

新規基準：章1＝2件70 Coins／Lv.2、章2＝4件180／Lv.4、章3＝6件330／Lv.6。画面は実セーブの値を表示し、この基準値で上書きしません。

## 5. キャラクター仕様

モモ・ルル・ポム・フィオ・ミエル・ノア。ID `momo,lulu,pom,fio,miel,noah`。承認済み全身・通常／笑顔／驚き、顔・衣装・色を保持。お気に入り1名を保存し、設定で変更可能。鏡像・背面・独立した脚の歩行コマ・新衣装を制作していません。

## 6. Pixel 3a実機確認

今回版は全項目未確認：[第1章試遊と全3章のチェックリスト](chapter-game-pixel3a-checklist.md)。縦全画面・Safe Area・縮小／文字切れ・押しやすさ・タップ／ドラッグ／停止・Merge／納品・章末／TOP・キャラ移動／会話・水／装飾・BGM／SE・保存／再起動・10分以上のFPS／発熱／メモリ・フリーズ／クラッシュを記録します。

端末操作はクラウドからできません。過去のAPK成功やスクリーンショットを今回の合格へ流用しません。

## 7. BGM・SE

常駐AudioManagerのBGM／SE別ソース・個別ON/OFF／音量を維持。現状の `bgm-town-loop`、`se-confirm/character/merge/delivery/reward/growth` は候補7本で、全て正式採用判断が残ります。

正式版の方向は明るいPOP×上品×ファンタジー。ピアノ／マリンバ・ベル／柔らかい弦／軽いアコースティックリズム、ピコピコ音は避ける。操作は柔らかなポン、合成はキラッとふわっと、報酬チャイム、街成長ファンファーレ、キャラの小さな反応。Pixelスピーカーで音量・ループ・連打を確認後、採用／差し替えを判断します。

## 8. 未解決事項・不具合・技術的リスク

- Windows PlayMode52件中4失敗を受領：`ChapterPagesFitShortPortraitAndActualValues`（説明文44＞枠40＋許容1）、`CharacterReactionDoesNotWriteOrChangeGameProgress`／`FullBodyActorsFollowBackgroundCropOnShortAndTallScreens`（旧配置参照でnull）、`PauseDuringMergeAndDragRebuildsUnlockedBoard`（中断中の入力拒否）。対応ソースを用意し、修正後コア87件と6実DLLアセンブリのコンパイルは成功。**元4件を含むWindows全52件の再合格は未確認**です。
- 今回APK・IL2CPP・Pixelの一周／保存／性能は未実行。直前の書体／画像／スクリプト検証結果は維持しますが、今回のクラウド検証はコアとコンパイルです。
- 前回のEditMode90中1件（全身幅1536＞1024）のmeta修正は維持。修正後の実インポートサイズ・全件XMLは未受領。今回も1024上限を緩めない。
- 3章6依頼の楽しさ・遊ぶ長さ、滑らかな見え方、水流／装飾の視認性、Safe Areaの重なりは実機評価待ち。見た目と歩行コマは同義ではない。
- 正式音源・ストア用画像／adaptive icon、Privacy／Data safetyの実態確認、署名AAB・16KB適合・Play Console条件は未完了。販売日優先で省略しない。
- ショップ販売・課金・広告・イベント・自由建築は未実装で当面広げない。

### 今回の実装上の判断

1. 既存workチェックアウトを再利用：クラウド環境と承認計画に従う。誤りなら作業分離方法の再確認が必要。
2. skillの補助スクリプト／レビューテンプレートが取得できず、同等の工程別brief・ledger・レビュー指示を使用。誤りなら工程記録の再照合が必要。
3. Unityランタイム未利用／クラウドAPK保留の既存条件を維持し、コア・実DLL・素材チェックのみここで実行。誤りならネイティブ検証場所の再設定が必要。実機合格とはしない。
4. BuildTimeテストに直接UnityEngine.UI参照を追加：MaskableGraphic API検証に必要。誤りならテストasmdefの参照1件を見直す。
5. ネイティブ最低件数90／43を維持し、新5Suiteは実テストを含むPassedを要求。実発見数未受領のため推測で最低数を上乗せしない。Windows XML受領後に更新。

独立レビューの結果・修正記録は末尾へ追記します。

## 9. GitHub反映状況

最新の4件対応は`work`で実施。main反映後の最新コミットは最終応答で記載します。Windows側はUnityを閉じてFetch origin→Pull originが必要です。

今回の変更ファイル6点：`Assets/Mofumachi/Presentation/ChapterSummaryScreen.cs`、`Assets/Mofumachi/Tests/PlayMode/ChapterFlowTests.cs`、`PopPresentationTests.cs`、`VerticalSliceFlowTests.cs`、`docs/development-handoff-2026-10-11.md`、`docs/pixel3a-quality-status.md`。

作業ブランチ `work`。基準 `f47b77c`。主な実装コミット：`9b02f92`書体、`75f9b93`Clock、`56e97c6`動き、`b2c6d23`章モデル、`b042019`章導線、`e83726c`Lv.4〜6装飾。最終レビュー／main反映の状態は末尾記録を参照。

主要変更ファイル：Core `TownFrameClock,TownNavigation,ChapterCatalog,ChapterProgress`、Presentation `UIWidgets,UIStrings,TownActor,TownLifeView,TownAmbientGraphic,BodyReaction,UIFlowController,ChapterPresentation,ChapterSummaryScreen,TopHomeScreens,QuestMergeScreens,RewardGrowthScreens,TownGrowthView,TownDecorationGraphic,ScreenContext,CharacterSelectionScreen,PurchaseNoticeView`、Resources `UIFont,UIFontBold,UIFontStory,UIFontStory-LICENSE,TownDecorationAtlas`、Editor `AndroidBuild`、各対応EditMode／PlayMode／BuildTime試験とBuildTests.asmdef、`scripts/subset-ui-fonts.py,build-android.ps1,tests/android-tools.tests.ps1`、本報告・チェックリスト・素材記録・公開準備資料。変更一覧はGitHub差分でも確認できます。

## 10. Windows PCで次に行う作業

1. Unityを閉じる。GitHub Desktopでmofumachi-mergeのmainを選び、Fetch origin→Pull origin。
2. Unity HubからUnity6000.6.4f1で同じプロジェクトを開く。インポート／コンパイルを待ち、Consoleの赤エラーを確認。
3. Window→General→Test Runner。EditModeでRun All、PlayModeでRun All。今回のPlayModeは**52件すべて成功・失敗0・skip0**を確認し、各Export ResultsのXMLを保存。4件だけのRerun Failedを行った場合も、最後にRun Allで全件を確認します。エラーが残る間はAPKビルドへ進まず、失敗したテストの詳細を確認します。
4. 合格後、Mofumachi→Android→**Build Town QA APK (separate save)**。`MOFUMACHI_APK_SUCCESS`を確認。出力は `Builds\Android\town-life-qa.apk`。
5. USB接続・Pixelロック解除済みで、PowerShellへ次を1行ずつ貼り付け。

```powershell
$MofumachiRepo = "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell -NoProfile -ExecutionPolicy Bypass -File "$MofumachiRepo\scripts\install-pixel3a.ps1" -Apk "$MofumachiRepo\Builds\Android\town-life-qa.apk" -Serial "99RAY1BELH" -PackageName "com.mofumachi.merge.qa.townlife"
```

`Installed ...`とAPK SHA256が出たら街QAを試遊。更新は保存を保持します。失敗したらアンインストールせずログを確認してください。

Unityを閉じて自動テスト→ビルド→XML／SHA記録をまとめて作る場合は、上のmenu手順3・4の代わりに次を実行できます。今回の全テスト合格が必須です。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "$MofumachiRepo\scripts\build-android.ps1" -TownQa
```

出力された日時付きフォルダに `editmode.xml,playmode.xml,build-info.json,town-life-qa.apk` が入ります。インストールの `-Apk` は表示された実際の日時付きAPKパスに差し替えてください。既存固定パスの古いAPKを誤って使わないようSHAを照合します。

保存復帰を確認する場合：

```powershell
$MofumachiAdb = "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
& $MofumachiAdb -s "99RAY1BELH" shell am force-stop com.mofumachi.merge.qa.townlife
```

Pixelで**同じ街QA**を開き直し、コイン・依頼・盤面・主人公・音量・装飾が戻るか確認します。データ消去コマンドは使いません。

## 11. 次回Codexが優先して行う作業

1. 今回のWindows PlayMode全52件（元4件を含む）の再実行結果と全件XMLを確認し、残る失敗を修正。
2. 全3章・章末／TOP・保存再起動を確認し、報酬と消費が一回であることを証拠付きで確定。
3. 10分以上のPixel操作でFPS／発熱／メモリと画面の質感を調整。
4. 正式BGM／SEを実スピーカーで選定・差し替え。
5. 体験の一区切りが十分か設計を評価後、署名AAB・素材・Privacy／Play提出条件を整備。

## 12. リリースに向けた現在地

**準備進捗60％（担当者の概算、品質合格率ではありません）**。章・書体・成長のソースは進みましたが、今回版のネイティブ／Pixel証拠と正式音源・提出準備が未了のため公開可能とは判定しません。

最大のリスク：①実機の文字／入力／保存／性能がまだ未検証、②3章の楽しさ・遊ぶ長さの評価と設計調整、③正式音源／署名AAB・Play提出条件／必要なConsoleテスト期間。日程はこれらの結果で見直します。

### 独立レビューと修正（2026-10-11）

1人の独立レビュアーが基準f47b77cからbc32a0cまで確認。重要指摘2件を1回の修正工程で対応：①章末のお祝いで6名の列が全幅へ上書きされ重なる→共通行の中に独立した6列、②短い縦画面の依頼カードで章名とキャラが11.52単位重なる→キャラの表示枠を別領域へ。修正前のソース配置再現チェックは両件失敗、修正後成功。592／640／808の実Boundsを検査するUnity回帰試験を追加し、実DLLコンパイル成功。**このUnity回帰試験のネイティブ実行は未了**です。Critical／軽微な保留指摘はなし。

レビュアーが判定を保留した範囲への判断：①実FontEngine／UGUI／画像品質はWindowsで実測（欠字／文字切れ／Raycast／importの不具合が残る可能性）、②IL2CPP／APK照合／インストール／冷起動はWindows＋Pixelの新成果物で確認（ビルド／ID／復帰の不具合が残る可能性）、③PixelのSafe Area・操作感・音・FPS／発熱／遊びの質は試遊チェックに従って確認（見た目／性能／設計変更が必要になる可能性）。いずれもレビューやコンパイルで合格扱いしません。

### main反映・Windows Pull

最新製品修正コミット **198e251d5a67aacb187b007d83e88df12122cb69**。mainへ通常のfast-forward pushで反映済み（基準f47b77cから）。作業はworkブランチで行い、既存リポジトリを保持。**Windows PCではFetch origin→Pull originが必要**です。この報告の最終追記もmainに反映し、最終応答のコミットで照合できます。新APKをクラウドで生成したという意味ではありません。
