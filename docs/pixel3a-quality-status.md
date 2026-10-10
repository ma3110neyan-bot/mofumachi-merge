# Pixel 3a品質向上 — 実装報告

2026-10-09実装、2026-10-10テスト結果追記。承認済み[設計](superpowers/specs/2026-10-09-pixel3a-quality-design.md)と[7工程の計画](superpowers/plans/2026-10-09-pixel3a-quality.md)に沿ったソース実装です。**Windowsのユーザー画面でEditMode67件・PlayMode25件の成功、QA版APKのビルド成功、USBの許可済み端末、QA更新インストールと起動コマンドの成功を確認。一周・保存復帰・画面・音の実機合格は未確認です。Vertical Sliceの実機安定完成／Google Play公開合格とは報告しません。** 起動スクリプトの修正後、Windows PowerShellで最後のAPK SHA256出力まで完了しました。

## 実装内容

- TOP／ホームを承認済みの街背景全面表示、正式6名原画カード、角丸・影・控えめなツヤ・押下反応で再構成。画像の比率を独立して維持します。縦端末はSafe Area全体、横長Editorだけ中央9:16。Canvas360×640／Expand、背景は画面全体です。コイン／街レベルは実セーブから表示します。
- 初回「課金について」の指定全文／「確認しました」、背景入力遮断、保存成功後の遷移、連打・戻る・GameScene直起動の迂回防止を実装。保存v2へ確認フラグを追加し、旧v1の既存進捗・音OFFを維持します。実購入処理は追加せず、将来の購入確認を別に行う設計です。
- 依頼カード、必要数／所持数／報酬、5×6正方形盤面、ドラッグ／2タップ、茶アイコン／レベル、納品可能時だけ有効なボタン。生成／合成／納品の保存失敗では成功したように扱いません。pause／遷移でghost・選択・合成ロックを解放します。
- 納品の消費・30 Coins・街成長・完了IDは既存コアの一回の保存。表示は成功前後の不変スナップショットを読み取り、納品→報酬→街成長の短い演出と各成功音を一回ずつ再生。再表示で再支払いしません。
- 拒否された仮オシレーターを削除。承認された原曲／CC0実録楽器からBGMループと6SEを同梱。常駐AudioManager・BGM／SE別ソース・個別ON/OFF／音量、シーン移動でBGMを巻き戻さない構成。連打の音が加算されない一声のSE制御です。
- 音量ドラッグは試聴、操作終了で一回保存。音量0とOFFを別保存し、保存失敗／途中退出／pauseで保存済みの値・音・表示へ戻す構成。設定は元画面へ戻り、納品エラーを持ち越しません。
- Android API36／minimum26／ARM64／IL2CPP／GLES3、全画面・Safe Area対応設定を継続。通常版と独立保存のQA版、新XML全件合格後のビルド、SHA／ID／versionCode等のbuild-info、USB端末／APK照合・更新インストールを準備しました。

## 検証と証拠の範囲

| 検証 | 今回の結果 |
| --- | --- |
| 実製品Coreを.NET8 NUnitで実行 | 62成功／失敗0／スキップ0 |
| 全Core／Presentation／Editor／全テストをUnity6000.6.4f1の実DLL・customNUnitでコンパイル | 成功、警告0／エラー0 |
| PowerShell7.4.13の実スクリプトゲート | XML／端末指定／package／更新失敗のmock検査成功。起動時stderrのPS5.1挙動を模擬した回帰試験と、実pwsh子プロセスのstdout／stderr／終了コード0・7の検査成功。Windows PS5.1／実ADBの実行ではありません |
| 七つのOGGをFFmpegデコード | 有限、各ピーク≤-3dBFS、ループ37.647秒。BGM境界差分0.001363。[全SHA／波形値](audio-assets.md) |
| UIFont | 完全版Noto CJK JPからOFL subset。HasCharacterの欠落文字誤検出を修正し、FontEngineの同梱字形照合へ変更。更新後のWindows EditMode全67件成功で確認 |
| 正式画像／承認済み背景 | 原画Resources SHA `cd423f6599cf41346064405240c72ac071aaa8d3fbd326d2b82661b13b0892c8`、背景SHA `971461342858e74c8db5f669f28c3e5c0c1a0f9d364e645ec55b9ea2834e7b51` 一致 |
| meta／GUID／asmdef／シーンbootstrap／diff | 静的検査。独立レビュー結果は下記へ追記 |
| Windowsネイティブ | 2026-10-10のユーザー画面：EditMode再試験67成功・失敗0・未実行0、PlayMode再試験25成功・失敗0・未実行0。XMLは未受領 |
| 新Android APK／Pixel | QAメニュービルドの成功marker／52,792,734 bytesとADBの `99RAY1BELH device` をユーザー画面で確認。修正後のWindowsインストーラーがQA ID／ARM64照合・API32端末への更新・起動コマンドを完了しSHAを出力。APK本体は未受領、通常版更新と新APKの一周・音・復帰試験は未確認 |

コアは旧形式移行／保存失敗／不完全データ／前回エラー残留の実行RED→GREENを確認。新しいUnity APIの欠落をコンパイルRED→GREENで確認しましたが、ネイティブUI／音の挙動RED／GREENはクラウドで実行していません。旧40／5のWindows成功と旧APKのPixel画像は、新実装の証拠へ転用しません。

### Windows初回EditModeの文字検査修正（2026-10-10）

`PresentationContentTests.ContentGateRejectsMissingAssetsAndUnsupportedGlyphs` が失敗。存在しないU+FFFFに対して `ValidateGlyphs` が期待した `BuildFailedException` を出さず、`Expected: BuildFailedException / But was: null` になりました。同梱OTFのcmapとシステムFreeTypeでU+FFFF／U+0378／U+0416の字形indexが0であることを確認。動的描画側の `Font.HasCharacter` に依存する検査を、Unity TextCore `FontEngine.LoadFontFace`／`TryGetGlyphIndex` による同梱字形の照合へ変更しました。

不足文字3種・フォント未設定の拒否と、必要な日本語／空白の受け入れを同じ3件のContentテストで確認する構成です。テストの削除・スキップ・合格条件の緩和は行っていません。クラウドのソースフォント照合は必要な非ASCII89文字で成功。e805e2f更新後のユーザー画面でWindows EditMode全67件成功を確認しました。

### Windows初回PlayModeのドラッグテスト修正（2026-10-10）

`MergeInteractionTests.PointerDragAndTwoTapsProduceOneMerge` のドラッグ後に、期待アイテム数1に対して実際2で失敗。ほか24件（直接Mergeする一周テスト、盤面サイズ、保存失敗、音、設定、報酬／成長を含む）はユーザー画面で成功しました。

失敗したテストは `ShowMerge` で盤面を新規生成した同じフレームでbegin／drag／endを実行していました。Unity同梱UGUIの `GraphicRaycaster` は未描画depth=-1のGraphicを除外し、標準 `GraphicRaycasterButtonTests` は生成後に1フレーム待ちます。これを根拠としてテストの描画待ちとドラッグ開始後のフレームを追加。移動後の2タップも描画後に実行し、各入力位置の実Raycast先が対応セルか検査します。合成の期待数・Lv.2・二重入力抑止・ロック・移動の検証は維持しています。

今回の変更は入力テストの準備と診断のみで、製品の盤面処理は変更していません。a23a0b1更新後のユーザー画面でPlayMode全25件成功・失敗0・未実行0を確認。セル描画depthと実Raycast先の検査を含むドラッグテストも成功しました。Pixelの実タッチはまだ確認していません。

### WindowsのQA版APKビルド（2026-10-10）

Test Runner確認後に `Mofumachi → Android → Build QA APK (separate save)` を実行。ユーザーのUnity画面下部に次の成功markerを確認しました。

```text
MOFUMACHI_APK_SUCCESS C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Android\vertical-slice-qa.apk (52792734 bytes)
```

これはa23a0b1更新後のWindows QAメニュービルドの結果です。今回のAPK本体はクラウドに受領しておらず、versionCodeは未記録。SHAは後述のWindowsインストール結果で取得しました。メニュービルドのため日時フォルダー／XML／build-info.jsonを生成するPowerShell手順とは区別します。

### USB認識とWindows PowerShellの起動処理修正（2026-10-10）

ユーザーのADB画面で `99RAY1BELH device` を確認。初回のQAインストールスクリプトは `shell monkey -p com.mofumachi.merge.qa -c android.intent.category.LAUNCHER 1` へ到達し、通常の `args: ...` 診断行を `NativeCommandError` として扱って停止しました。スクリプト順序から、端末許可・APKのQA ID／ARM64・端末API／ABI・更新インストールの `Success` 検査を通過して起動処理まで進んだと判断できます。この初回画面には端末APIの値／APK SHA／起動完了の出力がありませんでした。

Windows PowerShell 5.1はnative stderrをErrorRecordに変換するため、終了コード0でも全体の `ErrorActionPreference=Stop` で停止していました。外部コマンドの呼び出し中だけ出力を捕捉し、終了コードで成否を判定するよう修正。呼び出し後のエラーポリシーを復元し、実際の非0終了や実行ファイル欠落は引き続き失敗として扱います。

修正前にPS5.1のErrorRecord挙動を模擬して同じ停止を再現し、修正後に同じ回帰試験が成功。実pwsh子プロセスによるstdout／stderr両方の捕捉・終了コード0の成功・7の拒否・欠落実行ファイルの拒否も確認しました。クラウドはpwsh7.4.13のため、Windows PS5.1実行の合格を代用しません。全PowerShellファイルの構文検査とdiff検査は成功。変更はPC側スクリプトと検査／記録だけで、今回のQA APKの再ビルドは不要です。

dbe28b1更新後のユーザーのWindows PowerShell画面で、通常診断行を含んだまま次の最終出力まで完了したことを確認しました。

```text
Events injected: 1
Installed com.mofumachi.merge.qa on 99RAY1BELH (API 32). Save data was retained.
APK SHA256: 9303F6A4E4C062A30482A04B2FBDB4AA7AE355C34AEDCC0C484EB81F43966D78
```

これは実ADBを使ったWindowsの再実行成功です。実APKのQA ID／ARM64・端末API32・更新成功・起動コマンドの終了コード0を確認するスクリプトが完了しています。`Save data was retained` はデータを消さず `install -r` で更新したことの表示であり、進捗の内容や再起動復帰を検査した結果ではありません。起動コマンド成功と、Pixelの画面・入力・音・一周合格も区別します。

### 実機フィードバックと次の設計（2026-10-10）

ユーザーは街と正式キャラを評価し、フォントのかわいらしさ、その他UIの質感、キャラ選択／アクション／噴水の動き、街から依頼部屋・ショップへ進む操作の改善を希望。移動方法は **「指でキャラを自由に動かして目的地へ進む」** と回答しました。目的地タップによる自動移動は選択されていません。

ユーザーから今回のPixelのTOP／ホーム／Mergeのスクリーンショットを受領しました。3画面とも縦画面全体に背景とUIが表示され、Mergeの5×6盤面も収まっています。この画像だけでSafe Areaの数値、全画面の文字切れ、実タッチ・音・再起動復帰が合格したとは扱いません。

| 実機画面 | 確認した表示と改善対象 |
| --- | --- |
| TOP | 大きな半透明タイトル枠、拡大された顔カード、5名の顔カード、「はじめる」、設定。正式原画と背景は維持し、タイトル／フォント／ボタン／設定アイコンの質感と、顔の切れ・拡大の粗さを改善する |
| ホーム | 街Lv.1、30 Coins、完了したお茶会の依頼、6枚の顔カード、下部3ボタン。カードが街から浮いて見えるため、街のキャラ表示・選択・自由移動と入口の配置を設計する。HUDと依頼カードの占有、単色アイコン／メニューの質感も見直す |
| Merge | 5×6の空盤面、「お茶会の準備は完了しました」、納品ボタン無効、「キラッ！ お茶が育ちました。」が同時表示。納品消費後の空盤面を欠損と決めつけず、完了状態と一時メッセージの寿命を区別する。盤面の台座・マス・お茶の図柄・操作ボタンのまとまりを改善する |

ソース照合では、顔の切り出しは152×150ピクセルで、TOPで画面幅42%へ拡大しています。高解像度の正式画像なしに鮮明な全身キャラへ仕上げられるとは報告しません。ホームの6名は原画カードのButtonで、タップ時のSEだけが接続されています。噴水は背景に描き込まれた静止画です。Mergeのメッセージは画面別Dictionaryに保持され、納品成功でMergeの合成メッセージを消していないため、完了後に戻ると前の成功文が残ります。

この方向は要求と実機観察として記録し、具体的な操作・見た目・素材・既存一周との接続を設計します。現在のカード表示を街の歩行キャラと呼び替えません。正式6名は変更せず、未収録の透過全身／歩行素材と背景レイヤーの準備も区別します。素材確認へのユーザー回答は **「元の画像のみ」** でした。全身が描かれていない部分を原画から抽出したと扱わず、素材準備の問題を残します。ショップへ案内する導線と購入機能は区別し、現在の課金・広告なしの一周を維持します。

## Windowsでの再現操作とPixelの次の確認

今回のQA版は更新インストール・起動コマンドまで成功済みです。次はPixelで「もふまちメルジュ QA」の画面と一周を確認します。以下は今後APKを再ビルドする場合の手順で、今回の証拠記録だけの更新では再ビルド不要です。詳細は[Windows／Pixel手順](android-smoke-test.md)。正しいASCIIフォルダーでPullし、6000.6.4f1でインポート後にUnityを閉じます。

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-android.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\build-android.ps1 -Qa
```

成功した今回のAPKを `install-pixel3a.ps1 -Apk "今回のAPK" -Serial "Pixelのシリアル"` で更新。QA版は `-PackageName com.mofumachi.merge.qa` を追加します。アンインストールやデータ消去は行いません。

通常版は現在のCoins30／街Lv.1／完了・音OFFの保持を確認。QA版は0からTOP→初回案内→ホーム→受注→不足時は無消費→Merge→納品→30 Coins→街Lv.1→再納品拒否→音設定→pause／再起動を確認します。全7画面と案内の全高、Safe Area、文字、ボタン48dp、ドラッグ、各音・最大音量・BGMループ、再起動復帰を記録してください。

## 未解決事項と公開判断

- 新しいWindows XML／APK／Pixel合格が必要です。IL2CPP JSON移行・File.Replace・実入力・音再生は実機で確認してください。実測が悪ければその箇所を直す段階です。
- 六名の透過全身／歩行素材は未収録。承認済みの原画カードを使用しています。正式デザインを補う再生成はしていません。
- ストア画像は生成マスター候補。指定寸法の出力は生成ツールが対応しなかったため、Windowsのexport-play-art.ps1で512×512／1024×500へ書き出す手順を用意しました。この書き出しと最終確認は未実行です。
- adaptive icon、実機ストア写真、運営者／Privacy URL、SDK／Diagnostics／Data safety／permission監査、release AAB／正式署名／16KB適合、Console要件が残ります。[公開準備の残項目](google-play-release-readiness.md)
- Sliceは最初の依頼1件です。機能を広げず、まず一周と保存復帰の品質を確定します。10月31日公開は品質条件を満たした場合の目標です。

## 独立レビュー

承認どおり、772c418→7e6c1f6の実装全体を独立レビューしました。Criticalなし、Important3件を確認して修正しています。

1. 2本の音量スライダーを同時編集した時、保存失敗後に旧UIの取消callbackが未保存音量を再適用する問題。旧UI停止後に保存済み音量を最後に適用し、両方を操作してどちらを先に離しても一致するテストを追加。
2. Safe Area高さ592でTOPの小さい原画列と開始ボタン、ホームの下の原画と依頼カード、エラー文と開始ボタンが重なる問題。中央表示／固定アクション／メッセージの領域を分け、実際に要素を配置した画面の交差テストを追加。592／640／740／808の境界数式をクラウドでも確認。
3. 街成長の保存失敗で遷移できない時に、理由が見えない問題。街成長にも画面別エラーを表示し、保存失敗→表示→再試行のテストを追加。

trailing whitespaceも修正しました。レビュー修正後の全ソースコンパイル0警告／0エラー、コア62件とスクリプトゲートは成功。続くWindows試験の結果と修正は上記へ追記しています。独立レビューや数式検査を実機合格と呼びません。

## 変更ファイル一覧

承認計画コミット772c418以降。原画・CharacterMasterは変更していません。Unityの新規ソース／素材にはmetaも追加しています。

```text
Assets/Mofumachi/Core/GameState.cs
Assets/Mofumachi/Core/GameStateManager.cs
Assets/Mofumachi/Editor/AndroidBuild.cs
Assets/Mofumachi/Presentation/AudioManager.cs
Assets/Mofumachi/Presentation/DeliveryPresentation.cs
Assets/Mofumachi/Presentation/DeliveryPresentation.cs.meta
Assets/Mofumachi/Presentation/MergeBoardView.cs
Assets/Mofumachi/Presentation/PurchaseNoticeView.cs
Assets/Mofumachi/Presentation/PurchaseNoticeView.cs.meta
Assets/Mofumachi/Presentation/QuestMergeScreens.cs
Assets/Mofumachi/Presentation/QuestMergeScreens.cs.meta
Assets/Mofumachi/Presentation/ResponsiveUILayout.cs
Assets/Mofumachi/Presentation/ResponsiveUILayout.cs.meta
Assets/Mofumachi/Presentation/RewardGrowthScreens.cs
Assets/Mofumachi/Presentation/RewardGrowthScreens.cs.meta
Assets/Mofumachi/Presentation/RoundedPanelGraphic.cs
Assets/Mofumachi/Presentation/RoundedPanelGraphic.cs.meta
Assets/Mofumachi/Presentation/ScreenContext.cs
Assets/Mofumachi/Presentation/ScreenContext.cs.meta
Assets/Mofumachi/Presentation/SettingsScreen.cs
Assets/Mofumachi/Presentation/SettingsScreen.cs.meta
Assets/Mofumachi/Presentation/TopHomeScreens.cs
Assets/Mofumachi/Presentation/TopHomeScreens.cs.meta
Assets/Mofumachi/Presentation/UIFlowController.cs
Assets/Mofumachi/Presentation/UIIconGraphic.cs
Assets/Mofumachi/Presentation/UIIconGraphic.cs.meta
Assets/Mofumachi/Presentation/UIStrings.cs
Assets/Mofumachi/Presentation/UIStrings.cs.meta
Assets/Mofumachi/Presentation/UIWidgets.cs
Assets/Mofumachi/Presentation/UIWidgets.cs.meta
Assets/Mofumachi/Presentation/VolumeControl.cs
Assets/Mofumachi/Presentation/VolumeControl.cs.meta
Assets/Mofumachi/Resources/Mofumachi/Audio.meta
Assets/Mofumachi/Resources/Mofumachi/Audio/bgm-town-loop.ogg
Assets/Mofumachi/Resources/Mofumachi/Audio/bgm-town-loop.ogg.meta
Assets/Mofumachi/Resources/Mofumachi/Audio/se-character.ogg
Assets/Mofumachi/Resources/Mofumachi/Audio/se-character.ogg.meta
Assets/Mofumachi/Resources/Mofumachi/Audio/se-confirm.ogg
Assets/Mofumachi/Resources/Mofumachi/Audio/se-confirm.ogg.meta
Assets/Mofumachi/Resources/Mofumachi/Audio/se-delivery.ogg
Assets/Mofumachi/Resources/Mofumachi/Audio/se-delivery.ogg.meta
Assets/Mofumachi/Resources/Mofumachi/Audio/se-growth.ogg
Assets/Mofumachi/Resources/Mofumachi/Audio/se-growth.ogg.meta
Assets/Mofumachi/Resources/Mofumachi/Audio/se-merge.ogg
Assets/Mofumachi/Resources/Mofumachi/Audio/se-merge.ogg.meta
Assets/Mofumachi/Resources/Mofumachi/Audio/se-reward.ogg
Assets/Mofumachi/Resources/Mofumachi/Audio/se-reward.ogg.meta
Assets/Mofumachi/Resources/Mofumachi/TownBackground.png
Assets/Mofumachi/Resources/Mofumachi/TownBackground.png.meta
Assets/Mofumachi/Resources/Mofumachi/UIFont.otf
Assets/Mofumachi/Tests/BuildTime/AndroidBuildTests.cs
Assets/Mofumachi/Tests/BuildTime/Mofumachi.BuildTests.asmdef
Assets/Mofumachi/Tests/BuildTime/PresentationContentTests.cs
Assets/Mofumachi/Tests/BuildTime/PresentationContentTests.cs.meta
Assets/Mofumachi/Tests/BuildTime/QaIdentityTests.cs
Assets/Mofumachi/Tests/BuildTime/QaIdentityTests.cs.meta
Assets/Mofumachi/Tests/EditMode/PreferencesTests.cs
Assets/Mofumachi/Tests/EditMode/PreferencesTests.cs.meta
Assets/Mofumachi/Tests/EditMode/SaveServiceTests.cs
Assets/Mofumachi/Tests/PlayMode/AudioManagerTests.cs
Assets/Mofumachi/Tests/PlayMode/AudioManagerTests.cs.meta
Assets/Mofumachi/Tests/PlayMode/LayoutNoticeTests.cs
Assets/Mofumachi/Tests/PlayMode/LayoutNoticeTests.cs.meta
Assets/Mofumachi/Tests/PlayMode/MergeInteractionTests.cs
Assets/Mofumachi/Tests/PlayMode/MergeInteractionTests.cs.meta
Assets/Mofumachi/Tests/PlayMode/RewardGrowthTests.cs
Assets/Mofumachi/Tests/PlayMode/RewardGrowthTests.cs.meta
Assets/Mofumachi/Tests/PlayMode/SettingsScreenTests.cs
Assets/Mofumachi/Tests/PlayMode/SettingsScreenTests.cs.meta
Assets/Mofumachi/Tests/PlayMode/SliceUiTestFixture.cs
Assets/Mofumachi/Tests/PlayMode/SliceUiTestFixture.cs.meta
Assets/Mofumachi/Tests/PlayMode/VerticalSliceFlowTests.cs
README.md
References/Release/README.md
References/Release/feature-graphic-proposal.png
References/Release/icon-proposal.png
docs/android-preparation-status.md
docs/android-smoke-test.md
docs/audio-assets.md
docs/google-play-release-readiness.md
docs/pixel3a-quality-status.md
docs/superpowers/plans/2026-10-09-pixel3a-quality.md
docs/superpowers/specs/2026-10-09-pixel3a-quality-design.md
scripts/build-android.ps1
scripts/export-play-art.ps1
scripts/install-pixel3a.ps1
scripts/tests/android-tools.tests.ps1
```
