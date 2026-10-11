# 『もふまちメルジュ』開発引き継ぎ：TOP復帰・水流・次の街体験

2026-10-11更新。ユーザー回答「設計を承認・全身見本は修正点を確認する」を受領し、[承認済み設計](superpowers/specs/2026-10-11-town-life-design.md)から[7工程の実装計画](superpowers/plans/2026-10-11-town-life.md)を作成しました。計画は確認待ち、全身見本の採用は未承認です。**今回の更新は計画と文書のみで、ゲーム機能やAPKは変更していません。** Codexが順に実装し最後に独立レビューする、前回選択された方法を継続します。

以下の実装状況は、先行するTOP復帰・水流・既存UI修正を含む現在地です。主人公選択・自由移動・会話・複数アイテム／依頼は未実装で、計画の完成を機能の完成とは扱いません。

同日先行修正：Windowsの8件の失敗は、設定を閉じる際の共通の`CanvasRenderer`欠如でした。4種の必須指定と回帰テストを維持。今回はTOP復帰2件を追加し、予定件数はEditMode67／PlayMode31です。今回のWindows再試験は未実行。

## 1. 現在の実装状況

| 画面／機能 | 状態 | 実装範囲と残り |
| --- | --- | --- |
| TOP | 修正中 | ロゴ・開始・初回案内。Homeの戻る操作とゲーム中の設定から保存してTOPへ戻る処理を追加 |
| ホーム／街 | 修正中 | 全高背景・6名の肖像・実データHUD。噴水2段と奥の滝の水流・波紋を強化 |
| キャラ選択 | 仮実装 | 一時的な1名の選択表示。専用選択画面と永続保存は未実装 |
| キャラ依頼 | 修正中 | 依頼1件。桃色の見出し、淡いピンクの枠内、完了後のお礼。複数依頼は未実装 |
| Merge | 修正中 | 5×6、お茶のみ。青い見出し・進捗枠。3系統・9段階の絵と操作は設計段階 |
| 納品 | 仮実装 | 成功時の消費・保存、失敗取消、完了済み拒否。今回ロジック変更なし |
| 報酬 | 修正中 | 30 Coinsを1回、短い演出。依頼者ルルの肖像・お礼、色付きカードを追加 |
| 街成長 | 仮実装 | Lv.0→1の保存と演出。街並みの切り替えは未実装 |
| ショップ | 未実装 | 移動、店内、購入を含め未実装 |
| 設定 | 修正中 | BGM／SE・音量・スクロール。ラベンダーの見出しとゲーム中だけ表示する「TOPに戻る」 |
| セーブ／復帰 | 仮実装 | コア試験合格。今回の新APKの再起動確認待ち |
| BGM／SE | 仮実装 | 候補7音源と個別設定。正式採用・実機試聴待ち |
| Androidビルド | 修正中 | 必須素材検査を更新。今回のAPKはWindowsで生成する |

## 2. 今回実装・修正した内容

今回の計画・文書更新：新設計の承認状態を記録し、選択とv3移行→3系統／6依頼→表示→移動モデル→採用済み素材の街体験→実機確認へ分けました。旧30 Coins／Lv.1の進行を保つ移行、保存失敗と二重報酬の回帰、初回実機試験専用の別QAを計画に含めています。以下は先行コミットの実装修正で、今回の計画作成時には追加変更していません。

- UI：リボン中央を角丸・金縁・グラデーション・影と切れ込みのある端へ変更。見出しに薄い金色の影。依頼は桃色、Mergeは水色、設定はラベンダーで区別し、枠内にも淡い色を追加。
- 納品表示：既存の依頼者ルルの肖像と「ありがとう！」を追加。報酬は保存済みの実数値を使用し、1.2秒の表示待ちと二重報酬防止は維持。
- TOP復帰：Homeの戻る操作とゲーム中の設定の「TOPに戻る」から保存後にTitleSceneへ移動。保存失敗なら現在の画面へ残る。設定の通常「戻る」は元の画面へ戻り、TOPから開いた設定には重複したTOPボタンを出さない。
- セーブ：TOP復帰で未確定の音量編集・Merge表示ロックを解放して保存。形式v2、5×6、受注／納品、Coins、街Lv.、初回案内フラグに変更なし。
- キャラ：ユーザーが許可した[全身化の見本](../References/Preview/fullbody-review-2026-10-11/README.md)を作成。これは未採用案で、顔・衣装の差もレビューが必要。Resourcesへ組み込んでいない。正式原画と背景のSHAを維持。
- 街・アニメーション：原画上の噴水の2段と奥の滝へ動く水筋・しずく、水面へ4つの波紋を重ねる。背景のUVに追従、30Hz上限、中断時は停止、入力を遮らない。全身歩行／会話／街外観成長は未実装。
- 音：音源の差し替えなし。BGM／SEの個別設定と再生位置管理を維持。
- Android／Pixel：TOP復帰の回帰2件と初回案内の遮断確認を追加。PlayModeのビルド最低件数を31へ更新。新しい製品テクスチャ／フォント／パッケージの追加なし。

## 3. 今回のFBに対する対応状況

| FB | 状態 | 対応と残り |
| --- | --- | --- |
| ①フォント | 対応中 | 受領PDFで丸い書体と専用ロゴを確認。本文は維持し、見出しに金色の影を追加 |
| ②高級感・完成度 | 対応中 | リボン形状・枠内の色・納品のお礼を改修。今回のPixel確認は未実行 |
| ③動き | 対応中 | 水流と波紋を強化。全身化は未採用の見本作成まで。歩行・会話は設計段階 |
| ④自由移動・施設 | 対応中 | 指による自由移動・道の制限・依頼部屋／ショップ入口を設計。操作実装は未着手 |

## 4. コアループ完成状況

TOP→初回案内→ホーム→依頼→Merge→納品→報酬→街Lv.0→1→保存復帰は実装済みです。**今回の新APKによるPixel一周は未確認**で、各項目の実機確認が必要です。

| 項目 | 実機判定 |
| --- | --- |
| TOP／ホーム | 動作確認済み（前版の静止画表示のみ）。今回のTOP復帰は再確認が必要 |
| 依頼／Merge | 一部不具合あり：表示は確認、依頼が1件・お茶のみで完了後の続行に未対応 |
| 納品／報酬／街成長 | 今回の一周の実機動作確認は未実施。既存処理は実装済み、外観成長は未実装 |
| セーブ／復帰 | 今回の実機確認は未実施。更新で既存進行を消す処理は追加していない |

クラウドではコア62/62合格、全ソースと新回帰テストのUnity 6000.6.4f1実DLLコンパイル0警告・0エラー。新文言のフォント2種の欠字なし、正式原画と街背景のSHA一致を検証。**Unityの描画・PlayMode実行の合格とは区別**します。Windowsの目安はEditMode67件／PlayMode31件（UnityTest30＋通常Test1）です。TOP復帰2件のネイティブ実行はWindows待ち。

## 5. キャラクター仕様

正式名はモモ・ルル・ポム・フィオ・ミエル・ノア。古い仮名へ戻しません。正式原画は変更なし。専用選択画面と選択保存は未実装。全身化は確認用のシートまでで、正式採用・6名別の透過素材・歩行差分は未準備です。見本を認めるまで製品素材として使用しません。

## 6. Pixel 3a実機確認

本日のPDF6ページでTOP・ホーム・完了済み依頼・Merge・設定の表示を確認。QAビルド実行完了はユーザー報告があり、今回変更後のAPKではありません。写真だけで入力・音・保存復帰・安定性を合格にはしていません。

今回の確認項目：①設定の「TOPに戻る」→再開始でCoins・街Lv.・盤面・音量が維持、初回案内を再表示しない、②設定の通常「戻る」は開く前の画面、③ホームの戻る操作→TOP、④噴水2段・奥の滝・波紋が見える、⑤リボン・枠内の色・お礼の文字切れ／重なり、⑥全画面／Safe Area／タップ／ドラッグ／Merge／納品／報酬／街成長、⑦音・保存／復帰・負荷／発熱／フリーズ／クラッシュ。キャラ移動と新依頼は未実装です。

## 7. BGM・SE

[候補7音源](audio-assets.md)を維持。明るいPOP・上品・ファンタジー、ピアノ／マリンバ／ベル／柔らかい弦／軽いアコースティックリズムが方向です。BGMとボタン・キャラ・Merge・納品・報酬・街成長のSEは全点が正式採用前。実機の音量・ループ・音切れとユーザー試聴を経て確定または差し替えます。

## 8. 未解決事項・不具合

- Windowsの8件の失敗の共通原因に修正を入れました。元8件と新回帰テストの実際のPlayMode再試験が残っています。
- 今回のTOP復帰・水流・色・リボンのUnityネイティブ実配置・入力・PlayMode・APK・Pixel確認が未実行。
- クラウドのUnityライセンスは既知のブロッカーで、ネイティブ実行・APKは保留。今回はDLLコンパイルとコア試験で検証。
- 水流強化と従来の装飾の実機メモリ・描画負荷は未測定。今回はフォントに含まれる文言を使い、欠字検査へ追加。
- 専用選択、お気に入り保存、自由移動、会話、施設、歩行素材、ショップは未実装。設計は承認済み。実装計画の確認と見本の修正／採用が残っています。
- レベル別の街並みは未実装。現在の依頼カタログは1件で、初回のLv.1まで。
- 正式音源、署名・AAB・ストア素材・申告／審査準備が残る。

## 9. GitHub反映状況

クラウドの作業ブランチは`work`、反映先は`main`。現在の実装／見本／設計は`0937426`までmain反映済み。今回の計画コミットとPush結果は作業終了時のチャットに記載します。計画だけの更新ではWindowsの再ビルドは不要です。

今回の変更ファイルは`docs/superpowers/plans/2026-10-11-town-life.md`（新規）、`docs/superpowers/specs/2026-10-11-town-life-design.md`、本書、`README.md`の4文書です。

先行実装の主要変更ファイル（確認用画像はAssets外に保管）：

```text
Assets/Mofumachi/Presentation/RibbonGraphic.cs
Assets/Mofumachi/Presentation/UIWidgets.cs
Assets/Mofumachi/Presentation/UIFlowController.cs
Assets/Mofumachi/Presentation/UIStrings.cs
Assets/Mofumachi/Presentation/QuestMergeScreens.cs
Assets/Mofumachi/Presentation/RewardGrowthScreens.cs
Assets/Mofumachi/Presentation/SettingsScreen.cs
Assets/Mofumachi/Presentation/TownAmbientGraphic.cs
Assets/Mofumachi/Tests/PlayMode/LayoutNoticeTests.cs
Assets/Mofumachi/Tests/PlayMode/VerticalSliceFlowTests.cs
scripts/build-android.ps1
References/Preview/fullbody-review-2026-10-11/README.md
References/Preview/fullbody-review-2026-10-11/fullbody-proposal.png
docs/superpowers/specs/2026-10-11-town-life-design.md
docs/development-handoff-2026-10-11.md
docs/pixel3a-quality-status.md
docs/ui-pop-assets.md
README.md
```

## 10. Windows PCで次に行う作業

**今回の計画確認のためにPC操作・APK更新は必要ありません。** 以下は、先行するTOP復帰・水流の修正をまだ実機確認していない場合の手順です。計画にある街QAの追加メニューは未実装なので、現在は下記の既存QAを使用します。

1. Unity Editorを閉じる。
2. GitHub Desktopで`mofumachi-merge`の`main`を開き、Fetch origin→Pull origin。
3. Unity Hubから`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`をUnity 6000.6.4f1で開き、画像のインポート完了を待つ。
4. ウィンドウ→一般→Test Runner。EditModeでRun All（67件）、PlayModeでRun All（31件）。失敗があればそのテストの詳細を確認。元8件、描画前のマスク解除、TOP復帰2件の合格を確認してからビルドへ進みます。
5. `Mofumachi → Android → Build QA APK (separate save)`を実行し、今回の`MOFUMACHI_APK_SUCCESS`を確認。
6. PixelをUSB接続・ロック解除したまま次をPowerShellで実行。

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\scripts\install-pixel3a.ps1" -Apk "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Android\vertical-slice-qa.apk" -Serial "99RAY1BELH" -PackageName "com.mofumachi.merge.qa"
```

7. `Installed com.mofumachi.merge.qa`を確認し、Pixelの「もふまちメルジュ QA」で各画面・入力・音・復帰を確認。更新で既存QA進行を保持し、アンインストールやデータ消去はしません。すでに依頼完了しているデータは、新しい最初の一周の証拠には使いません。

再起動復帰は同じQA版を終了して開き直し、コイン・街Lv.・盤面・依頼・音設定を確認します。必要に応じて次で終了できます。

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" -s "99RAY1BELH" shell am force-stop com.mofumachi.merge.qa
```

## 11. 次回Codexが優先して行う作業

1. 今回のTOP復帰・水流・レイアウトのWindows／Pixel結果を確認し、不具合を修正。
2. 実装計画の確認後、主人公選択・favorite保存・v3の旧進行移行を実装。全身素材を待たずに進められる工程から着手。
3. 3系統・6依頼と納品結果を実装し、現在の完了済みデータから続けられるようにする。
4. 全身案の修正／採用後、指による自由移動・会話・施設導線と限定した街の外観成長を実装。
5. Pixelで一周・保存復帰・音・安定性を確認し、正式音源とGoogle Play提出準備へ進む。

## 12. リリースに向けた現在地

**約50％（作業段階の概算）**。実測の合格率や工数予測ではありません。2026-10-31は品質条件を満たした場合の目標で、実機の一周・保存復帰・音・安定性を合格確認するまで公開品質の完成としません。

最大のリスクは、①今回の描画・TOP復帰・保存・音・負荷の実機合格が未確定、②主人公選択・複数依頼・全身素材・自由移動／会話・街外観成長が未完成、③正式音源とGoogle Play申告／署名／AAB／審査準備が残ることです。
