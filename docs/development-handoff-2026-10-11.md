# 『もふまちメルジュ』開発引き継ぎ — 2026-10-11 街の生活実装

対象は承認された[7工程の計画](superpowers/plans/2026-10-11-town-life.md)。全身見本はそのまま採用との回答、表情追加の依頼を反映。過去の67／25合格・APK成功・Pixelスクリーンショットは旧版の証拠です。**今回の新しいAPK・Unityネイティブ試験・Pixelでの一周は未確認**です。

## 1. 現在の実装状況

「仮実装」はコード・素材を接続した状態で、実機の完成判定が残っているものです。

| 画面・機能 | 状態 | 現在の内容 |
|---|---|---|
| TOP | 仮実装 | 光沢ロゴ、全身パートナー、はじめる、初回課金案内、設定 |
| 主人公選択 | 仮実装 | 正式6名、全身・紹介・プレビュー、確定して保存、設定から再選択 |
| ホーム／街 | 仮実装 | 6名同時表示、道に沿う自由ドラッグ、他5名の往来・待機・短文会話 |
| キャラ依頼 | 仮実装 | 6依頼、実際の依頼者・台詞・必要品の所持数・報酬を表示 |
| Merge | 仮実装 | 5×6、茶器／パン／花の各Lv.1〜3、9種の絵、生成系統選択、確認付き単品片づけ |
| 納品・報酬 | 仮実装 | 実際の納品者・品物・報酬の固定スナップショット、演出再表示で再払いしない |
| 街成長 | 仮実装 | 保存Lv.1花の装飾、Lv.2花飾り、Lv.3噴水周りの星の光、以後はLv.3外観を維持 |
| ショップ | 未実装 | 入口と「準備中／販売機能は未実装」の案内だけ。商品・購入なし |
| 設定 | 仮実装 | BGM／SE個別ON/OFF・音量、パートナー再選択、戻る、保存してTOPに戻る |
| セーブ／復帰 | 完成（コア検証） | v3、お気に入りを保存、v1／v2移行、失敗巻き戻し、バックアップ。実機再起動は未確認 |
| BGM／SE | 仮実装 | 既存7候補とAudioManagerを維持。正式音源は未決定 |
| Androidビルド | 仮実装 | 既存QAと通常版を維持し、初回試験用の別「街QA」を追加。今回のAPK生成はWindowsで行う |

## 2. 今回実装・修正した内容

- UI：全身選択、実依頼・2種までの要求品、9種のアイテム絵、系統選択／片づけ確認、実納品・報酬画面を接続。既存のロゴ・金縁・花・光沢を維持。
- ロジック：6依頼の順次受注、完了済み旧tea-01から2件目へ続行。初回から全6件なら330 Coins／街Lv.6。1件目は従来どおり30 Coins。失敗した受注／納品／片づけ／選択は巻き戻す。
- キャラクター：正式6名の名前・原画を維持。承認見本から透過全身を作り、通常・笑顔・驚きの3表情。お気に入りを指で移動。他5名は短い道を往復。
- 街・アニメーション：道を外れず噴水を回り込むモデル、上下動、タップ時の笑顔・跳ね、到着時の驚き、同時1つの短文会話。従来の水流・波紋を維持。施設は到着→指を離す→明示入室。
- 音：今回の音源差し替えなし。既存のキャラ／決定／Merge／納品／報酬／成長SEを接続。
- 保存：`favoriteCharacterId`を追加、v3へ移行。不明な旧アイテムは消さず「?／Lv.」表示。街内の一時的な位置・台詞は保存対象外。
- Android：`com.mofumachi.merge.qa.townlife`／`town-life-qa.apk`を追加。インストールは`-r`、実APK ID・ARM64・serialを検査し、自動アンインストール／データ消去を行わない。

## 3. 今回のFBに対する対応状況

| FB | 状態 | 対応・残り |
|---|---|---|
| ①かわいいフォント | 完了（素材・接続） | 丸いZen Maru本文・見出しと光沢タイトルを継続。両フォントに追加文言を収録。Pixelの読みやすさ確認待ち |
| ②商用ゲームの質感 | 対応中 | 金縁・花・リボン・光沢、9点の描き起こし、依頼内容を反映。新APKの枠内余白・配色・装飾密度を実機で再調整 |
| ③動き・反応・水 | 対応中 | 6名の往来・待機、跳ね、笑顔／驚き、短文会話、水筋・波紋。歩行は全身の上下動で、脚のコマ送りや手振りは未実装 |
| ④街内移動・施設導線 | 対応中 | お気に入りを指で自由ドラッグ。道の最短経路で噴水を回避。依頼部屋／ショップへ到着後に入室。ショップ販売は未実装 |

吹き出しは約5.5秒間隔、同時1つ、ドラッグ・施設表示中は抑制。街更新は最大30Hz。これらは負荷を抑える実装方針で、実機30fpsを測定した結果ではありません。

## 4. コアループ完成状況

| 区間 | コア・コード検証 | 今回のPixel判定 |
|---|---|---|
| TOP→初回案内→選択→ホーム | 保存移行・選択コア検証済み、Unity試験追加 | 未確認 |
| ホーム→依頼→Merge | 6依頼の続行、混合合成拒否、所持数検証済み | 未確認 |
| Merge→納品→報酬→街成長 | 全6件各1回、報酬・消費巻き戻し検証済み | 未確認 |
| セーブ→再起動復帰 | 純粋C#の移行・復帰・バックアップ検証済み | 未確認 |

未確認を「一部不具合なし／動作確認済み」とは扱いません。下記の実機手順で一周と再起動を確認し、動作確認済み／一部不具合ありを追記します。

## 5. キャラクター仕様

ID順：`momo` モモ／`lulu` ルル／`pom` ポム／`fio` フィオ／`miel` ミエル／`noah` ノア。6名から1名を選び、確定後にローカル保存。プレビューだけでは書き込みません。再選択は依頼・報酬・盤面を変えません。

元の正式コラージュと街背景は変更していません。透過全身・表情アトラスは承認された全身見本を基に制作。キャラの向きを左右反転したり、背面を新規制作したりしていません。表情の見え方は新APKでご確認ください。

## 6. Pixel 3a実機確認

今回の新機能について、すべて実機未確認です。端末はPixel 3a、serial `99RAY1BELH`、以前の報告ではAPI32。

- 縦全画面、Safe Area、縮小／不自然な余白、文字切れ、設定スクロール、押しやすさ。
- 初回案内を確認後に主人公選択。6名をプレビュー→確定→ホーム。設定から変更して元画面へ戻る。
- 主人公を押して指を動かす。遠い位置へ瞬間移動せず、噴水を横切らず、指を離すと止まる。2本目の指で制御を奪われない。
- 他5名の往来・待機、表情・跳ね・会話。吹き出しはHUD／入室／下ナビを覆わず、ドラッグを奪わない。
- 左の青いひさし前で離す→「依頼部屋に入る」。右のひさし前→ショップの準備中案内。通過・NPC到着だけでは画面を開かない。
- お茶Lv.1同士→Lv.2→納品→報酬30→街Lv.1。次はパン、花へ進む。2種要求の依頼も確認。異系統は合成不可。
- 満杯時はアイテム選択→「…」→片づけ確認。取消で消えない。確認した1個だけ消える。
- BGM／SEのON/OFF・音量0とOFFの違い、再起動後の保存。
- TOPに戻る、アプリをバックグラウンドへ、強制停止・再起動。お気に入り・盤面・Coins・依頼・報酬ID・音量が復帰し、報酬が増えない。
- 動作の重さ、30fps目標、発熱、フリーズ、クラッシュ、10分程度の操作。スクリーンショットだけで保存・音・FPSを合格扱いしない。

## 7. BGM・SE

`AudioManager`でBGMとSEを分離し、設定画面から個別ON/OFF・音量を保存します。既存`bgm-town-loop`と6SE（決定・キャラ・Merge・納品・報酬・成長）は**候補**で正式採用ではありません。

正式音源は明るいPOP・上品・ファンタジー、ピアノ／マリンバ・ベル／柔らかいストリングス／軽いアコースティックリズム。ボタンは柔らかなポン、Mergeはキラッ＋ふわっと、報酬はチャイム、街成長は短いファンファーレ、キャラは小さな反応。全7音源の正式差し替え・音質・ループ継ぎ目・Pixelでのバランス確認が残ります。

## 8. 未解決事項・不具合・検証範囲

クラウドの実測：コア81件成功・失敗0・skip0。Unity 6000.6.4f1の実DLLとnetstandard2.1／C#9で、直接asmdef参照だけを使い6アセンブリを個別コンパイルし、すべて0警告・0エラー。PowerShell 7.4.6のスクリプトゲート成功（ADB／aaptは模擬）。両フォントの表示文字、元素材のSHA、meta GUIDを検査。

- Unityネイティブ実行はライセンス未利用とクラウドAPK保留方針のため未実行。コンパイル成功はUGUI描画・FontEngine・InputSystem・IL2CPP成功の証拠ではありません。
- 今回のWindows対象はEditMode **90件**（コア81＋Build9）、PlayMode **43件**。これはソースから集計した予定数で、合格実績ではありません。テストの削除・skipはしていません。
- 新APK・SHA・実機一周・30fps・発熱・クラッシュは未測定。実機で素材の切れ、足位置、噴水位置、文字、指操作を確認して修正します。
- 全身は1方向の絵と3表情。脚・手の独立アニメーション／完全な会話システムは未実装。
- ショップの販売、課金、広告、図鑑、イベントは未実装。追加範囲を広げず既存一周を優先。
- 6依頼を完了すると「すべての依頼が完了しました」で終了。ランダム依頼・追加施設・Lv.4以降の別背景は未実装。
- v3保存後に古いv2アプリへ戻しても、古いアプリはv3保存を読めません。旧版へ戻す必要がある場合は先にバックアップと別IDを使用し、データを消さないでください。
- 正式音源、製品署名、AAB、ストア申告・素材採用、対象APIと16KBページ対応の提出時確認が残ります。

独立レビュー1回の6件を修正済み：BuildTestsのCore直接参照、TOPの旧肖像6枚テストを5枚＋全身の正式6名検査へ更新、設定テストの選択確定、街のRaycastに描画待ち／depth検査、再選択中の設定の戻り先保持、TOPで主人公を除く5名の表示。既存テストを強化し、削除・skipはしていません。参照エラーは個別コンパイルで失敗を再現し、修正後に全6アセンブリ成功。表示・遷移の回帰テストはWindows実行待ちです。

再利用するクラウド設定の手順も今回の個別コンパイル・PowerShell検証に更新して下書き保存。設定の適用／公開はしていません。

## 9. GitHub反映状況・変更ファイル

作業ブランチ`work`。最新の実装修正コミット`7dd0a04`（独立レビュー指摘の修正）、街QA設定`a97bc0d`。この報告更新も含めmainへ反映済みです。最新コミットは[mainの履歴](https://github.com/ma3110neyan-bot/mofumachi-merge/commits/main/)で確認できます。Windows側でFetch origin→Pull originしてください。

[今回の追加・変更ファイル全一覧](town-life-changed-files-2026-10-11.txt)（metaを含む）。

主要変更（各新規ファイルのmetaも追加）：

- Core：`CharacterCatalog.cs`、`GameState.cs`、`GameStateManager.cs`、`ItemCatalog.cs`、`QuestCatalog.cs`、`QuestDefinition.cs`、`QuestManager.cs`、`MergeBoard.cs`、`TownNavigation.cs`、`TownMovement.cs`。
- Presentation：`CharacterSelectionScreen.cs`、`ItemGraphic.cs`、`MergeActionMenuView.cs`、`DiscardConfirmationView.cs`、`DeliveryPresentation.cs`、`CharacterBodyGraphic.cs`、`BodyReaction.cs`、`TownActor.cs`、`TownLifeView.cs`、`TownGrowthView.cs`、`UIFlowController.cs`、`TopHomeScreens.cs`、`QuestMergeScreens.cs`、`RewardGrowthScreens.cs`、`SettingsScreen.cs`、`MergeBoardView.cs`、`UIWidgets.cs`、`UIStrings.cs`、`ScreenId.cs`。
- Resources：`ItemAtlas.png`、`CharacterBodyAtlas.png`、`UIFont.ttf`、`UIFontBold.ttf`。原画／街背景／音源は変更なし。
- Tests：主人公保存・選択、6依頼、単品削除、移動、表示、街生活、コンテンツ、QA ID、既存入口・マスク回帰を追加・更新。
- Android／手順：`Editor/AndroidBuild.cs`、`scripts/build-android.ps1`、`scripts/install-pixel3a.ps1`、`scripts/tests/android-tools.tests.ps1`、本書、品質状況、素材記録、実機手順、README。

## 10. Windows PCで次に行う作業

1. GitHub Desktopで`mofumachi-merge`を開き、Fetch origin→Pull origin。
2. Unity HubからUnity **6000.6.4f1**で開き、インポートとコンパイルを待つ。Build ProfilesでAndroidがActiveになっていることを確認。
3. Test RunnerのEditMode→Run All、続いてPlayMode→Run All。予定90／43件、赤0・skip0を確認。失敗があればAPKへ進まず、失敗項目と詳細を送ってください。
4. 初回から確認する場合は **Mofumachi→Android→Build Town QA APK (separate save)**。既存QAの続行確認は **Build QA APK (separate save)**。それぞれConsoleの`MOFUMACHI_APK_SUCCESS`と出力名を確認。
5. Pixelのロックを解除し、USB接続・デバッグ許可。PowerShellで下の該当コマンドを貼り付け、Enter。`Installed ...`が出たらPixelで開く。

初回用「街QA」（以前のQAセーブと別）：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\scripts\install-pixel3a.ps1" -Apk "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Android\town-life-qa.apk" -Serial "99RAY1BELH" -PackageName "com.mofumachi.merge.qa.townlife"
```

以前のQA（30 Coins／街Lv.1から続行）：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\scripts\install-pixel3a.ps1" -Apk "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Android\vertical-slice-qa.apk" -Serial "99RAY1BELH" -PackageName "com.mofumachi.merge.qa"
```

旧QAで主人公を選んだ後、依頼がパンの「焼きたてのおすそわけ」に進むこと、30 Coinsを重複受領しないことを確認。別QAも一度進行すると保存されるので、再インストールで毎回初回には戻りません。

再起動確認（街QA）：

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" -s "99RAY1BELH" shell am force-stop com.mofumachi.merge.qa.townlife
```

Pixelで「もふまちメルジュ 街QA」を再び開く。アンインストールや「ストレージを消去」は行わないでください。ビルドを自動化する場合はUnityを閉じて`powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-android.ps1 -TownQa`。生成した日付フォルダのAPKパスをインストーラーへ渡します。

## 11. 次回Codexが優先して行う作業

1. Windowsの90／43件とPixelの初回一周／旧セーブ続行の結果を受け、失敗を修正。
2. 全身・表情・街の位置、文字・装飾・タップ領域、水流の見え方を実機スクリーンショットと操作結果で調整。
3. 30fps・保存／再起動・バックグラウンド復帰・連続操作を確認し、負荷と入力不具合を修正。
4. 正式BGM／SEの決定・差し替えと音量確認。
5. 安定後に製品署名／AAB・Google Play提出設定と素材を確定。

## 12. リリースに向けた現在地

**60％（実装・提出準備の概算。実機安定の達成率ではありません）**。2026年10月末の目標は維持し、公開日より一周の品質を優先します。Google Playへの提出・公開はしていません。

今月中公開の最大リスク：

1. 今回の街入力・描画・移行・6依頼について、Unityネイティブ／Pixelの安定試験が未完了。
2. 正式音源と商用UI・全身の見え方の仕上げが残る。
3. 署名AAB、16KB対応・対象API、Playの申告・必要なテスト期間など提出条件の確定が残る。
