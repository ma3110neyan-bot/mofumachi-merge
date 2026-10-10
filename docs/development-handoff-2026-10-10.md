# 『もふまちメルジュ』開発引き継ぎ：明るいPOP UI改修

2026-10-10。今後の作業終了報告も、この12項目を共通の形式にします。優先順位は「一周遊べる完成度」「見た目」「Pixel 3aの安定性」。追加機能を広げず、正式6名のデザインを維持します。

今回の対象は、ユーザーが承認した明るいPOP案に沿う既存画面の改修です。UI実装コミット：`2cb3abb`、配置点検後の修正：`f5faccf`。キャラの街での自由移動は、全身素材と操作・施設の設計を別に進める合意を維持します。

## 1. 現在の実装状況

「完成」は確認範囲を満たしたものに使います。新UIのWindows実インポート・PlayMode・Pixel実機が未確認のため、画面は修正中として記載します。

| 画面／機能 | 状態 | 現在できること・残り |
| --- | --- | --- |
| TOP | 修正中 | 丸い日本語書体、POPの配色、原画フレーム、開始・初回課金案内。更新後実機確認待ち |
| ホーム／街 | 修正中 | 全高の街背景、実データHUD、6名表示、噴水の水滴・波紋・光、依頼入口 |
| キャラ選択 | 仮実装 | タップで1名のフレームを強調、短い反応とSE。お気に入りの永続保存は未実装 |
| キャラ依頼 | 修正中 | 最初の依頼1件、受注、要求数、実際の報酬、完了表示 |
| Merge | 修正中 | 5×6、生成、タップ／ドラッグ、合成、納品可否。カップ／ポットとLvを表示 |
| 納品 | 仮実装 | 不足・ロック・保存失敗時の無消費、成功時に確定。最初の依頼1件 |
| 報酬 | 仮実装 | 30 Coins、二重付与防止、短い表示演出。更新後実機確認待ち |
| 街成長 | 仮実装 | Lv.0→1、星と拡縮演出。建物の成長差分は未制作 |
| ショップ | 未実装 | 施設への移動・店内・購入は未実装 |
| 設定 | 修正中 | BGM／SEの独立ON/OFF・音量、縦スクロール、元画面へ戻る |
| セーブ／復帰 | 仮実装 | コア試験合格、保存失敗の取消・既存データ移行。更新後IL2CPP実機再確認待ち |
| BGM／SE | 仮実装 | 実録楽器による候補7音源とAudioManager。正式採用・実機試聴は未確定 |
| Androidビルド | 修正中 | 前版QA APKは実機インストール・起動済み。今回のソースを含むAPKはWindowsで生成待ち |

## 2. 今回実装・修正した内容

- **UI**：Zen Maru Gothic Medium／Boldへ変更。ボタン・パネルに丸み、薄い明るい縁、控えめなつや、影。家・封筒・陶器のお茶・歯車等の9アイコンを同梱。盤面にミントのトレー、クリームのセル、ピンクの選択色。文字を背景から読みやすくするメッセージ面を追加。
- **ゲームロジック**：納品成功時に古いMerge／依頼メッセージを消します。納品失敗の理由は残します。合成・納品条件、報酬、コアの状態・保存形式は変更していません。
- **キャラ**：原画の画像バイトを維持し、丸いマスクとフレームへ変更。拡大しすぎないサイズへ調整。正式名を表示。
- **街・アニメーション**：タップした原画フレームの拡縮・傾き。背景に合わせる噴水の水滴・波紋と小さな光。画面離脱・pauseで停止。新しい表情・全身・歩行を描き足していません。
- **BGM／SE・セーブ**：今回の音源変更、保存形式変更、データ消去はありません。
- **Android／Pixel**：フォント2種とアイコンをビルド前の必須素材検査へ追加。PlayModeの必須ゲートを28件・7suiteへ更新。レイアウトと演出の実機確認は必要です。

素材・フォントの出典と検証は[UI素材記録](ui-pop-assets.md)。主要な変更ファイルは次のとおりです。新規ソース・素材にはmetaも同梱しています。

```text
Assets/Mofumachi/Presentation/UIWidgets.cs
Assets/Mofumachi/Presentation/RoundedPanelGraphic.cs
Assets/Mofumachi/Presentation/UIIconGraphic.cs
Assets/Mofumachi/Presentation/TopHomeScreens.cs
Assets/Mofumachi/Presentation/QuestMergeScreens.cs
Assets/Mofumachi/Presentation/RewardGrowthScreens.cs
Assets/Mofumachi/Presentation/SettingsScreen.cs
Assets/Mofumachi/Presentation/PurchaseNoticeView.cs
Assets/Mofumachi/Presentation/UIFlowController.cs
Assets/Mofumachi/Presentation/MergeBoardView.cs
Assets/Mofumachi/Presentation/UIStrings.cs
Assets/Mofumachi/Presentation/CharacterReaction.cs (+ meta)
Assets/Mofumachi/Presentation/TownAmbientGraphic.cs (+ meta)
Assets/Mofumachi/Resources/Mofumachi/UIFont.ttf (+ meta; UIFont.otfを置換)
Assets/Mofumachi/Resources/Mofumachi/UIFontBold.ttf (+ meta)
Assets/Mofumachi/Resources/Mofumachi/UIFont-LICENSE.txt
Assets/Mofumachi/Resources/Mofumachi/UIIconAtlas.png (+ meta)
Assets/Mofumachi/Editor/AndroidBuild.cs
Assets/Mofumachi/Tests/BuildTime/PresentationContentTests.cs
Assets/Mofumachi/Tests/PlayMode/PopPresentationTests.cs (+ meta)
scripts/build-android.ps1
docs/ui-pop-assets.md
docs/development-handoff-2026-10-10.md
docs/pixel3a-quality-status.md
docs/android-smoke-test.md
docs/audio-assets.md
References/Preview/ui-polish-2026-10-10/README.md
README.md
```

## 3. 今回のFBに対する対応状況

| FB | 状態 | 対応・残り |
| --- | --- | --- |
| ①かわいいフォント | 対応中 | 丸い日本語フォント2種を実装。小画面の実描画・文字切れを実機確認して確定 |
| ②UIの質感 | 対応中 | 承認POP案で既存7画面と共通部品を改修。実機写真とユーザーの見た目確認待ち |
| ③選択・アクション・環境 | 対応中 | フレームの選択・反応、噴水・光を追加。全身の待機／歩行／表情差分／手振り、花・木・建物の実レイヤーアニメは未実装 |
| ④街での自由移動・施設導線 | 未対応 | 指でキャラを自由に動かす希望を維持。全身素材、操作、通行可能な場所、依頼部屋／ショップ入口を別に設計する段階 |

## 4. コアループ完成状況

旧版のWindows自動試験はEditMode67/67、PlayMode25/25がユーザー画面で成功。今回のクラウドではコア62/62、Unity実DLLの全ソースコンパイル0警告・0エラー、PowerShellスクリプト試験が成功しました。

**新UIのPixel 3a一周は未確認です。**「未実装」と「実装済み・実機未確認」を混同しないため、以下は確認待ちを明記します。

| 流れ | 実装 | 今回のPixel実機確認 |
| --- | --- | --- |
| TOP→初回案内→ホーム | 実装済み | 未確認 |
| ホーム→依頼受注 | 実装済み | 未確認 |
| Merge（タップ／ドラッグ） | 実装済み | 未確認 |
| 納品（不足時は無消費） | 実装済み | 未確認 |
| 報酬（30 Coinsを1回） | 実装済み | 未確認 |
| 街成長（Lv.0→1） | 実装済み | 未確認 |
| セーブ／pause／再起動復帰 | 実装済み | 未確認 |

今回追加した3件のPlayMode試験は、短い／長い画面で6名の枠が独立して重ならないことと選択表示、タップ演出によるゲーム進行の無変更・pause停止、納品成功後の古いメッセージ消去／保存失敗の理由保持です。実装前に新API未存在のコンパイル失敗を確認し、実装後コンパイルは成功。Unityの実動作RED/GREENはWindowsへ残しており、追加3件を合格扱いにしていません。

## 5. キャラクター仕様

最新のユーザー指定を正式仕様とします。

| 原画の左から | 正式名 | 固定の原画特徴 |
| --- | --- | --- |
| 1 | モモ | ピンクのうさぎ、花付きピンク帽子 |
| 2 | ルル | 白・灰のねこ、青帽子と青い衣装 |
| 3 | ポム | 茶色のくま、緑帽子と緑の衣装 |
| 4 | フィオ | 橙・白のきつね、緑のリボン |
| 5 | ミエル | 白いひつじ、紫の花飾りと淡い青 |
| 6 | ノア | 茶・橙のりす、赤帽子 |

顔・配色・衣装・主要デザイン・雰囲気を変更しません。お気に入り1名を選ぶ設計を基本とし、その選択の永続保存と街での操作は次の設計対象です。現在あるのは元コラージュのみ。6名全員の透過全身・歩行差分は未収録です。描かれていない部位を「原画から抽出した」と扱いません。

## 6. Pixel 3a実機確認

前版QAはAPI32の`99RAY1BELH`へ更新・起動済み。送られたTOP／ホーム／Merge／依頼の写真で縦全高の表示を確認しています。写真だけでは入力・音・再起動・安定性は確定しません。今回のAPKでは全項目を再確認します。

- 縦全高、縮小なし、Safe Area、文字切れ、アイコンの切れ・透明部分、ボタンの押しやすさ。
- 原画フレームのタップ反応、1名のみ強調、噴水の位置と水・光の動き。
- タップ／ドラッグで合成が1回だけ成立、盤面外へのドロップは無消費。
- 納品→30 Coins→街Lv.1、再納品拒否、納品後に古い合成文が残らない。
- BGM／SE独立ON/OFFと音量、ループ、最大音量の音割れ。
- バックグラウンドから復帰、強制停止後の再起動でCoins・Lv・依頼完了・音設定を保持。
- 15分程度の操作で重さ・発熱・フリーズ・クラッシュ・連続操作時の問題を記録。

キャラ自由移動は未実装のため、このAPKでは確認できません。通常版・QA版の既存セーブは消しません。

## 7. BGM・SE

現在は候補音源です。正式採用として扱いません。[既存音素材の出典](audio-assets.md)。

- BGM：ピアノ、マリンバ、ベル、柔らかい弦、シェイカー／ボンゴを用いた録音サンプルのループ候補。
- SE：決定、キャラ、Merge、納品、報酬、街成長の6種類を接続済み。
- AudioManagerはBGM／SE別、設定は個別ON/OFF・音量・ローカル保存。
- 全7音源はPixelのスピーカーで聴き、明るいPOP・上品・ファンタジーの方向と合うか最終調整が必要。ループ継ぎ目、耳障りな高音、安いピコピコ感、報酬音とBGMのバランスを確認します。
- 今回のUI作業で音源を正式版へ差し替えた箇所はありません。

## 8. 未解決事項・不具合

- 新フォント・アトラスのWindows実インポート、UnityのMask／Canvas／FontEngine、追加PlayMode3件、今回のAPKビルドと実機表示は未実行。
- 噴水は静止画に水滴・波紋を重ねた表現。背景クロップに追従する実装ですが、端末で水の位置・自然さ・負荷の確認が必要。
- 原画カードの拡大品質には152×150の元領域の限界があります。サイズを抑えましたが高解像度の全身歩行素材にはなりません。
- お気に入り保存、全身自由移動、依頼部屋／ショップへの歩行・到着遷移、店内は未実装。
- Sliceは最初の依頼1件。再インストールは更新方式なので、既に完了したQAの依頼は完了のままです。新規セーブの一周は自動試験で確認し、端末では既存保存を消さず確認可能な部分から進めます。
- BGM／SEの正式採用、実機の継ぎ目・音割れ・再開音は未確認。
- 通常版の移行・IL2CPP JSON／File.Replace・実機再起動復帰・性能・長時間安定性は未確認。
- adaptive icon、ストアの指定寸法出力・実機写真、運営者情報・Privacy URL、SDK／permission／Data safety監査、正式署名AAB／16KB適合、Play Console要件が残っています。
- 確認済みのクラウド全ソースコンパイルは0警告／0エラー。ネイティブの未実行チェックを「エラーなし」とは判断しません。

## 9. GitHub反映状況

- ブランチ：`main`。
- UI実装：`2cb3abb`（`feat: apply approved bright POP UI and lightweight town reactions`）。配置点検後の修正：`f5faccf`（各原画フレームを個別領域に収める）。
- 本報告と確認手順を追加する文書更新も同じmainに反映します。最新のHEADは[コミット一覧](https://github.com/ma3110neyan-bot/mofumachi-merge/commits/main/)で確認できます。
- Windows側はFetch origin→Pull originが必要。今回の見た目は新APKの再ビルド・更新インストールで反映されます。
- 変更一覧は2項、フォント／アイコンのライセンス・仕組みは[UI素材記録](ui-pop-assets.md)。

## 10. Windows PCで次に行う作業

1. Unityを閉じます。GitHub Desktopで`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`を開き、**Fetch origin→Pull origin**。
2. Unity Hubから**6000.6.4f1**で同じプロジェクトを開き、インポート完了を待ちます。
3. **ウィンドウ→一般→Test Runner**。**EditMode→Run All**、次に**PlayMode→Run All**。目安は**67件／28件**、失敗0。失敗が出た場合は赤い個別テストの詳細を確認します。
4. 両方成功後、**Mofumachi→Android→Build QA APK (separate save)**。Consoleの`MOFUMACHI_APK_SUCCESS`を確認します。
5. PixelをUSB接続・ロック解除したまま、PowerShellで以下を実行します。

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\scripts\install-pixel3a.ps1" -Apk "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Android\vertical-slice-qa.apk" -Serial "99RAY1BELH" -PackageName "com.mofumachi.merge.qa"
```

6. Pixelの**もふまちメルジュ QA**で、6項の表示・動き・操作・保存を確認します。アンインストールやデータ消去は行いません。

日時ごとのXML・ビルド情報を残す方法は[Windowsビルド手順](android-smoke-test.md)。そのスクリプトはUnityを閉じて実行します。

## 11. 次回Codexが優先して行う作業

1. 今回のWindowsテスト・Pixel写真／動画／一周結果を確認し、文字切れ・操作・保存・演出の不具合を直す。
2. 正式6名を維持する全身素材の準備方針と、指で自由に動かす操作・施設到着の設計を具体化する。
3. お気に入り1名の保存と依頼部屋への導線を、既存セーブと一周を壊さず接続する。ショップは入口導線から範囲を判断する。
4. Pixel試聴をもとにBGM／SE候補を調整し、正式音源を確定する。
5. 実機安定が確認できた段階でPlay提出設定・素材・署名AABの残りを埋める。

## 12. リリースに向けた現在地

**約50％（工程ベースの概算）**。一周のソース・自動試験・前版APKの起動まで進んでいますが、今回のUI実機合格・保存復帰の端末証拠・正式音の確認・Play提出物が残っています。公開条件の合格数を測った数値ではなく、工数や公開日の予測にも使いません。

2026年10月末の公開に対する最大のリスクは次の3点です。

1. 新UI・実入力・保存復帰・長時間安定性の実機確認が未完了で、端末特有の修正量が確定していない。
2. 正式6名の全身・歩行素材がなく、街の自由移動と施設導線を満たす準備・設計が必要。
3. 正式音源の確定と、ストア素材・プライバシー／データ申告・署名／16KB／Consoleなどの提出準備が残っている。

一周・品質・実機の条件を優先し、日付だけを理由に公開へ進めません。
