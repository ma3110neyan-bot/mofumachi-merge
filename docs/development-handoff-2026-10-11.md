# 『もふまちメルジュ』開発引き継ぎ：ロゴと装飾UI

2026-10-11。本文フォントを維持し、「さらに華やかに」の回答と正式参照を反映した既存UI改修です。街レベル別の外観は[方向案](town-growth-direction.md)として記録し、今回の機能には追加していません。

同日追記：Windowsから届いた8件の失敗は、設定画面を閉じる際の共通の`CanvasRenderer`欠如でした。独自描画4種の必須指定を修正し、回帰テストを追加。元8件のテストは維持しています。最新の予定件数はEditMode67／PlayMode29で、Windows再試験待ちです。

## 1. 現在の実装状況

| 画面／機能 | 状態 | 実装範囲と残り |
| --- | --- | --- |
| TOP | 修正中 | 立体ロゴ、開始、初回課金案内。新ロゴの実機確認待ち |
| ホーム／街 | 修正中 | 全高背景、6名、実データHUD、装飾した依頼入口 |
| キャラ選択 | 仮実装 | 一時的な1名の選択表示。専用選択画面と永続保存は未実装 |
| キャラ依頼 | 修正中 | 最初の依頼1件、受注、必要数、完了。新しい枠とリボン |
| Merge | 修正中 | 5×6、生成、タップ／ドラッグ、合成、納品判定。外縁を装飾 |
| 納品 | 仮実装 | 成功時の消費・保存、失敗取消、完了済み拒否。今回ロジック変更なし |
| 報酬 | 修正中 | 30 Coinsを1回、短い演出。装飾を追加 |
| 街成長 | 仮実装 | Lv.0→1の保存と演出。街並みの切り替えは未実装 |
| ショップ | 未実装 | 移動、店内、購入を含め未実装 |
| 設定 | 修正中 | BGM／SEのON/OFF・音量・スクロール。余白と説明文を改善 |
| セーブ／復帰 | 仮実装 | コア試験合格。今回の新APKの再起動確認待ち |
| BGM／SE | 仮実装 | 候補7音源と個別設定。正式採用・実機試聴待ち |
| Androidビルド | 修正中 | 必須素材検査を更新。今回のAPKはWindowsで生成する |

## 2. 今回実装・修正した内容

- UI：透過の立体タイトルロゴ、花・宝石入りの金枠、光沢リボン、花の区切り線を追加。依頼・報酬・街成長・設定・ナビを統一。本文とゲーム中の数値は文字で表示。
- 配置：HUDのアイコンと数値を近づけ、依頼の情報を整理。設定は420単位の内容にだけ背景を描いて大きな空白を解消。短い画面のスクロール用タップ面を維持。
- ゲームロジック・セーブ：5×6、合成、納品、30 Coins、街レベルの計算、保存形式、プレイヤーデータに変更なし。
- キャラ・街：原画と街背景をバイト単位で維持。新しい歩行、施設、街並み差分は未追加。
- アニメーション・音：既存の選択反応、噴水、報酬演出、音源を維持。今回の装飾は静的。
- Android／Pixel：必須素材検査へ2画像を追加。共有素材、最大インポートサイズ1024、ミップなし。枠の透明な中央を描かず、装飾はタップを受け取らない。盤面のサイズ・間隔は維持。
- 描画不具合：金枠・リボン・アイコン・噴水の4種に`CanvasRenderer`を必須指定。描画前に設定を閉じてもマスクを解除できる回帰テストを1件追加。ゲームロジック・音設定の保存処理は変更なし。

## 3. 今回のFBに対する対応状況

| FB | 状態 | 対応と残り |
| --- | --- | --- |
| ①フォント | 対応中 | 本文の書体はユーザーFBを受け維持。ゲーム名は専用ロゴへ変更、実機確認待ち |
| ②高級感・完成度 | 対応中 | 金枠・花・宝石・リボンと配置を改修。最終の見た目はPixelで確認 |
| ③動き | 対応中 | 選択反応と噴水を維持。全身歩行・表情差分・手振り・草木や建物の独立した動きは未実装 |
| ④自由移動・施設 | 未対応 | 指による自由移動の希望を維持。素材・通行範囲・操作・入口の設計が必要 |

## 4. コアループ完成状況

TOP→初回案内→ホーム→依頼→Merge→納品→報酬→街Lv.0→1→保存復帰は実装済みです。**今回の新APKによるPixel一周は未確認**で、各項目の実機確認が必要です。

クラウドではコア62/62合格、全ソースと新回帰テストのUnity 6000.6.4f1実DLLコンパイル0警告・0エラー。描画4種の依存指定を実DLLで検証し、修正前の失敗／修正後の合格を確認しました。Unityの描画・PlayMode実行の合格とは区別します。Windowsの目安はEditMode67件／PlayMode29件です。

## 5. キャラクター仕様

正式名はモモ・ルル・ポム・フィオ・ミエル・ノア。参考画像内の古い仮名へ戻しません。正式原画は変更なし。お気に入り1名を選ぶ専用画面と選択保存は未実装です。今回の画像は保管済みの正式参照と一致し、姿・衣装・構成の参考にします。独立した6名分の全身透過・歩行差分は未準備です。

## 6. Pixel 3a実機確認

前版の受領写真で、ホーム・依頼・Merge・設定の縦画面描画、丸い本文フォント、円形のキャラ枠、アイコンを確認しました。写真だけで入力・音・保存復帰・安定性を合格にはしていません。

今回の確認項目：ロゴの文字と切れ、全画面／Safe Area／縮小／文字切れ、装飾と情報の重なり、ボタンの押しやすさ、タップ／ドラッグ／Merge／納品／報酬／街成長、音量ドラッグ／ON/OFF／設定スクロール、BGM／SE、更新前の進行保持、再起動後の復帰、重さ／発熱／フリーズ／クラッシュ。キャラ移動は未実装です。

## 7. BGM・SE

[候補7音源](audio-assets.md)を維持。明るいPOP・上品・ファンタジー、ピアノ／マリンバ／ベル／柔らかい弦／軽いアコースティックリズムが方向です。BGMとボタン・キャラ・Merge・納品・報酬・街成長のSEは全点が正式採用前。実機の音量・ループ・音切れとユーザー試聴を経て確定または差し替えます。

## 8. 未解決事項・不具合

- Windowsの8件の失敗の共通原因に修正を入れました。元8件と新回帰テストの実際のPlayMode再試験が残っています。
- 新しい装飾のUnity実インポート、Mask、実配置、入力、APK、Pixel確認が未実行。
- クラウドのUnityライセンスは既知のブロッカーで、ネイティブ実行・APKは保留。今回はDLLコンパイルとコア試験で検証。
- 新しい2画像の実機メモリ・描画負荷は未測定。新しい文字を増やす場合はフォントのサブセットと欠字検査の更新が必要。
- 専用選択、お気に入り保存、自由移動、施設、歩行素材、ショップは未実装。
- レベル別の街並みは未実装。現在の依頼カタログは1件で、初回のLv.1まで。
- 正式音源、署名・AAB・ストア素材・申告／審査準備が残る。

## 9. GitHub反映状況

クラウドの作業ブランチは`work`、反映先は`main`。最新コミットとPush結果は作業終了時のチャットに記載します。反映後はWindowsでFetch origin→Pull origin。

主要変更ファイル（新規ソース・画像にはmetaを同梱）：

```text
Assets/Mofumachi/Presentation/DecorativeFrameGraphic.cs
Assets/Mofumachi/Presentation/RibbonGraphic.cs
Assets/Mofumachi/Presentation/UIWidgets.cs
Assets/Mofumachi/Presentation/TopHomeScreens.cs
Assets/Mofumachi/Presentation/QuestMergeScreens.cs
Assets/Mofumachi/Presentation/RewardGrowthScreens.cs
Assets/Mofumachi/Presentation/SettingsScreen.cs
Assets/Mofumachi/Presentation/UIIconGraphic.cs
Assets/Mofumachi/Presentation/TownAmbientGraphic.cs
Assets/Mofumachi/Resources/Mofumachi/TitleLogo.png
Assets/Mofumachi/Resources/Mofumachi/UIPanelFrame.png
Assets/Mofumachi/Editor/AndroidBuild.cs
Assets/Mofumachi/Tests/BuildTime/PresentationContentTests.cs
Assets/Mofumachi/Tests/PlayMode/LayoutNoticeTests.cs
scripts/build-android.ps1
docs/ui-pop-assets.md
docs/town-growth-direction.md
docs/development-handoff-2026-10-11.md
docs/pixel3a-quality-status.md
README.md
```

## 10. Windows PCで次に行う作業

1. Unity Editorを閉じる。
2. GitHub Desktopで`mofumachi-merge`の`main`を開き、Fetch origin→Pull origin。
3. Unity Hubから`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`をUnity 6000.6.4f1で開き、画像のインポート完了を待つ。
4. ウィンドウ→一般→Test Runner。EditModeでRun All（67件）、PlayModeでRun All（29件）。失敗があればそのテストの詳細を確認。今回の8件と追加した回帰テストの合格を確認してからビルドへ進みます。
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

1. 新UIの文字切れ・装飾の重なり・入力・負荷をWindows／Pixel結果から修正。
2. 既存の一周と保存／再起動復帰の実機結果を記録し、不具合を修正。
3. 初回Lv.0→1で一か所が変わる外観を設計し、保存状態に連動させる。
4. お気に入り保存と、正式デザインを守る自由移動・素材・施設導線を設計。
5. 正式音源と署名・AAB・Google Play提出準備を進める。

## 12. リリースに向けた現在地

**約50％（作業段階の概算）**。実測の合格率や工数予測ではありません。2026-10-31は品質条件を満たした場合の目標で、実機の一周・保存復帰・音・安定性を合格確認するまで公開品質の完成としません。

最大のリスクは、①新しい描画・入力・保存復帰・音・負荷の実機合格が未確定、②街が育つ外観・自由移動・独立したキャラ素材が未完成、③正式音源とGoogle Play申告／署名／AAB／審査準備が残ることです。
