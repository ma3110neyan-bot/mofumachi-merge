# 『もふまちメルジュ』開発引き継ぎ — 2026-10-12・Merge／納品の再改善

今回の承認範囲は第1・第2段階です。生成種類・選択状態・満杯時の整理・受注／納品案内、QA入力記録、実UI回帰テストを修正しています。**Windows Test Runnerのユーザー提供画像でPlayMode77件成功・失敗0・無視0を確認しました。EditModeの最新結果・XML本体・Pixel 3aの実機FBは確認待ちです。公開品質の完成判定は行っていません。**

### Windowsの26件報告への対応

貼り付けられた結果からは10種類のテストを特定でき、すべて通常のQA診断ログがLogAssert.NoUnexpectedReceivedに拒否されています。同メソッドは例外・警告だけでなく通常ログも検査します。診断ログ導入時にこの干渉を見落としていたため、出力条件を修正しました。

LayoutCancelsVisualsAndBusyDeliveryPreservesSavedMergeUntilUnlockは同じ内容が2回掲載され、重複1件です。ソース上はこのテストは1件なので削除していません。ほかのテスト名はChapterReentryAndSaveFailureNeverPayOrAdvance、EveryChapterHasExplicitClosureAndFinale、ReselectionReturnsThroughSettingsToItsOrigin、ConsecutiveMergesReuseOneSmallEffectLayerAndRestartKeepsProgress、FailedAndRejectedMergesDoNotShowSuccessOrConsumeItems、PauseAndSettingsCancelVisualsAndKeepTheSavedMerge、SavedMergeUnlocksBeforeTheLightTailAndNeverReplaysOnReentry、SelectionScalesOnlyTheItemAndDoesNotSave、ShortAndTallLayoutsKeepLevelTextAndSelectedArtInsideCellsです。

**当時の掲載分の共通原因は1種類です。前版26件の全内訳はXML未受領のため不明ですが、最新Test Runner画像ではPlayModeの失敗0を確認しました。** EditorではQA情報ログを出さず、Development Buildのプレイヤーでは出力を維持します。エラー／警告の検査や既存テストは弱めていません。出力条件の回帰1件を追加したため、PlayMode構成は76→77件です。

### 追加で掲載された4テストへの対応

次の4件は重複なしで、共通原因はホーム画面のままDropItemを呼び、合成成功を期待する古いテスト前提でした。現在はCanInteractBoardがMerge画面での操作だけを許可し、DropItemもこの判定を使うため、ほかの画面ではfalseを返します。調査履歴の説明を訂正します。CanInteractBoardの画面制限は以前から存在しましたが、DropItemがこの判定を使うようになったのは基本操作改修の0c72eadです。この変更で4件の未更新のテスト前提が表面化しました。

- HomeBackReturnsToTitleAndStartResumesSavedProgress
- CancelAndSettingsPersistWithoutLosingMergedItems
- TeardownDuringAnimationLeavesNoCallbacksOrStore
- PauseDuringMergeAndDragRebuildsUnlockedBoard

`Assets/Mofumachi/Tests/PlayMode/VerticalSliceFlowTests.cs`の追加変更について承認を受け、4テストの初回DropItem前にShowMergeと画面／操作可能状態の検査を追加しました。計12行の追加だけで、合成・保存復帰・音設定・ロック解除・pause／drag拒否と復帰・破棄後の既存検査はすべて保持しています。対象クラス7件、全体98／77件の構成も変わりません。

適用内容は独立レビュー済み候補との完全一致を確認し、実際の全6アセンブリのコンパイルは警告0／エラー0、Coreの.NET87件も成功しました。ゲーム本体や素材、保存／報酬ルールには今回変更していません。**その後、ユーザー提供のWindows Test Runner画像でPlayMode77件全件成功を確認しました。XML本体は未受領なので個別ケースの結果詳細と実行対象コミットの照合は未実施です。Pixelの実操作は別途確認します。**

追加のテスト参照 `Assets/Mofumachi/Tests/PlayMode/Mofumachi.PlayModeTests.asmdef` の `Unity.InputSystem` 1件は、2026-10-12に追加承認を受けて反映しました。既存の参照3件と全設定を保持。本体asmdef・依存パッケージの変更はありません。最終コンパイルと独立レビュー後、実装コミットをmainへ反映してremoteの同一SHAを確認しました。

正式6名・タイトルロゴ・街背景・Mergeの5×6／配色／盤面とボタンの配置を保持。Core、セーブv3、消費／報酬／街成長ルールは変更していません。フォント役割の変更・街経路の拡張・新しいキャラ動作・追加ゲーム要素は今回の範囲に含めません。

## 1. 現在の実装状況

| 画面／機能 | 状態 | 内容 |
|---|---|---|
| TOP | 仮実装 | 既存ロゴ・初回確認・TOP復帰。今回実機未確認 |
| ホーム／街 | 仮実装 | 背景と6名・既存自由移動／会話を維持 |
| 主人公選択 | 仮実装 | 正式6名から1名選択・保存 |
| キャラ依頼 | 修正中 | 実所持数と必要数、受注と次の依頼を区別 |
| Merge | 修正中 | 生成種類・古い選択・拒否理由・整理を修正。実入力の合格待ち |
| 納品／報酬 | 修正中 | 具体的な不足案内・演出中拒否。消費／報酬のCoreは保持 |
| 街成長／3章 | 仮実装 | 既存3章6依頼・レベル別装飾・章まとめ |
| ショップ | 未実装 | 入口と準備中案内のみ |
| 設定 | 仮実装 | 音量・個別ON／OFF・元画面／TOP復帰 |
| セーブ／復帰 | 仮実装 | 実SaveServiceの検証は成功、今回Pixel復帰は未確認 |
| BGM／SE | 仮実装 | 候補BGM1＋SE6。正式採用待ち |
| Androidビルド | 仮実装 | Build Town QA APKを実行済みとのユーザー報告あり。新APKの成功ログ・SHA256・実機操作結果は未受領 |

## 2. 今回実装・修正した内容

- **生成**：受注時とコントローラー起動／保存復帰時に、依頼の不足条件から生成種類を決めます。パン依頼ならパンが初期選択され、複数条件なら最初の不足種類になります。同じ画面内で選んだ種類は再描画で勝手に戻しません。選択種類をセーブへ追加する変更はありません。
- **タップ**：生成・種類変更で古い選択を解除。同じマスの再タップで解除。種類／Lv.の違うアイテムへのタップは、そのマスを選び直して条件を案内します。同種・同Lv.の2タップは従来の合成へ進みます。
- **ドラッグ**：異種・異Lv.・最高Lv.・盤面外・空の元マス・元のマスを区別。拒否・保存失敗ではアイテムと既存選択を維持します。現在の入力認識しきい値や座標計算を、根拠なく変更していません。
- **整理**：従来の「…」を同じ操作領域の「種類／整理」へ。選択対象の種類とLv.を片づけボタン／確認に表示。取消・保存失敗・対象の変化は消費しません。不明なIDが含まれる有効な旧セーブでも、確認画面を開いて整理できるよう名前を安全に表示します。自動削除は追加していません。
- **納品案内**：未受注なら既存の依頼画面へ進む「依頼を見る」、受注後は必要数をそろえると「納品する」が有効。要求の正確な種類・Lv.・実所持数を表示。合成中に納品を押しても、一度成功した合成を画面再描画で中断しません。待機案内は合成完了／中断保存後に通常の案内へ戻します。
- **QA記録**：Development Buildのプレイヤー限定で `MOFUMACHI_MERGE_INPUT` を出力。Editorでは厳格なログ検査を妨げないよう情報ログを出しません。press、tap、drag開始／終了／取消、元／先の種類・Lv.とロック、選択・生成種類、受注状態、画面・Safe Area・入力モジュール、結果を記録。毎フレームの出力やセーブ全体の出力はありません。
- **文字**：新しい案内で必要な字形を補完。3書体は445→462字形、書体・太さ・既存字形・ライセンスは保持。今回はメルヘン調フォントへの全面適用を行っていません。
- **見た目／キャラ／街／音／セーブ**：元画像・既存演出・音源・セーブ形式・ゲームルールを維持。Mergeの小さな案内領域の行間と文字サイズは2行の理由が収まるよう調整し、Unityでの高さ検査を追加しました。

## 3. FBへの対応状況

| 項目 | 状況 | 今回と今後 |
|---|---|---|
| ① かわいい／メルヘン調の文字 | 対応中 | 今回は字形補完のみ。Kiwi Maruの適用拡大／Klee One比較は基本操作合格後に承認して適用 |
| ② 高級感・枠内の品質 | 対応中 | 現行の金縁・花・光沢・盤面を保持。実機比較による細部調整は継続対象 |
| ③ キャラ／環境の動作 | 対応中 | 今回変更なし。独立した手足、まばたき、休憩等の素材は未制作 |
| ④ 街の行動範囲 | 対応中 | 今回変更なし。現背景に沿う通行／障害物・重なり・名前位置の見直しは基本操作合格後 |

現在のメルヘンチックな街背景、6名の外見、タイトルロゴ、Mergeの基本デザインの描き替えはありません。

## 4. コアループ完成状況

| 流れ | 状況 |
|---|---|
| TOP→ホーム→依頼 | ソース接続あり、今回の実機未確認 |
| 依頼→必要種類の生成 | 案内計算は検証済み。実UI検査を含むWindows PlayMode77件全件成功画像を受領。Pixel操作は確認待ち |
| タップ／ドラッグ→パンLv.2 | **一部不具合ありという実機報告を解消確認待ち** |
| 納品→報酬→街成長→次依頼 | Core検証済み。満杯盤面のUI一周テストを含むPlayMode77件全件成功画像を受領。個別XML／対象コミットの照合とPixel一周は確認待ち |
| セーブ／再起動 | 実SaveServiceで検証済み。今回のPixel復帰は未確認 |

コア調査では茶器Lv.3×28＋パンLv.1×2の満杯盤面から、パンLv.2へ合成→納品→Coins30→70、街Lv.1→2→次のflower-01→ディスク復帰まで成功。二重納品は拒否しています。茶器30個の盤面でも、2個を明示的に除去した後はパンの生成・合成が可能でした。これは実機入力の成功ではありません。

## 5. キャラクター仕様

モモ・ルル・ポム・フィオ・ミエル・ノア。正式の顔・名前・配色・衣装・承認済み全身と3表情を保持。お気に入り1名を選んで保存する設計を維持します。

## 6. テスト結果とPixel 3a確認

| 検証 | 結果 |
|---|---|
| 実Coreの.NET NUnit | **87件成功** |
| 実MergeVisualTimingの.NET計算テスト | **8件成功** |
| 実UIStrings＋Coreの案内計算 | **修正前9件失敗→修正後9件成功** |
| 実MergeInputDiagnosticsの出力条件 | **修正前4ケース失敗→修正後4ケース成功**。Editor／通常プレイヤーはOFF、DevelopmentプレイヤーはON。Windows PlayModeの全件成功画像を受領、個別結果XMLは確認待ち |
| 実Core＋SaveServiceの満杯／納品調査 | **18項目成功** |
| PowerShellビルド前ゲート／インストールスクリプト | 新Suite欠落を拒否→修正後成功。模擬XML／ADBで実機試験ではない |
| Unity実DLLを参照するコンパイル | **実際の6 asmdefを基に全6アセンブリ成功、各警告0／エラー0**。追加参照案の仮設定ではなく、承認後の製品asmdefを使用。Unityランタイム実行ではない |
| 最終独立レビュー | Critical／Importantの未解決指摘なし。旧セーブの整理、合成中拒否、待機案内の解除、参照1件に加え、4テストの画面前提と既存検査の保持を確認。適用した12行はレビュー済み候補と一致 |
| 字形 | 3書体462字形、欠落0。字形収録検査でありFontEngine実描画ではない |
| Windows UnityネイティブPlayMode | **ユーザー提供Test Runner画像で77件成功・失敗0・無視0、56.728秒を確認**。XML本体未受領、個別結果／対象コミットの照合は未実施 |
| Windows UnityネイティブEditMode | 最新98件の実行結果は**確認待ち** |
| 新APK | Build Town QA APKの実行済み報告を受領。成功ログ・SHA256の確認は未実施 |
| Pixel 3a一周／再起動／FPS | **実機FB待ち**。こちらでは未実施・合格未判定 |
| 最新5枚の画像比較 | **未受領・未実施** |

ソースとコンパイル済みアセンブリのテスト属性を照合し、**EditMode98件（EditModeTests87＋BuildTests11）、PlayMode77件**の構成を確認しました。これとは別に、ユーザー提供のWindows Test Runner画像でPlayMode77件全件成功を確認しています。各Suiteの実行詳細はWindowsのXMLで照合します。Merge再改善では、操作6件・不明IDの整理1件・案内計算9件を追加し、今回の診断出力条件1件を加えて計17件追加です。実タッチケースはInput SystemのTouchStateをEventSystemへ渡し、画像追従・合成・2タップ移動を検査します。

| PlayMode構成 | 件数 |
|---|---:|
| 新規MergeProgressionUiTests／MergeGuidanceTests | 7／10 |
| MergeInteractionTests／MergePolishTests／MultiQuestPresentationTests | 4／8／4 |
| その他既存11 Suite | 44 |
| 合計 | 77 |

Pixelでは、既存の詰まった保存状態を保持して、[今回のチェックリスト](merge-progression-pixel3a-checklist.md)で生成／整理／Merge／納品／報酬／街成長／次依頼／再起動を確認します。全画面・文字切れ・Safe Area・音・キャラ・10分連続操作／重さ／フリーズ／クラッシュも記録します。

## 7. BGM／SE

変更なし。BGM1・SE6は候補段階。AudioManager、個別ON／OFF・音量保存を保持。明るいPOP×上品×ファンタジー、ピアノ・マリンバ／ベル・柔らかな弦・軽いアコースティックリズムの方向を継続します。今回新しい成功音や二重音は追加していません。

## 8. 未解決事項・不具合

- 追加参照のコンパイル障害は解消済み。UnityネイティブとPixelの合格を意味するものではありません。
- Pixelでドラッグが失敗する全原因は未特定。最新5枚とQA入力記録を使って切り分けます。
- Windows Test Runner画像ではPlayMode77件全件成功。前版26件の全内訳、個別ケースの最新XML、EditMode98件の最新結果は確認待ち。APKでの今回症状の解消、Pixelの保存復帰、比較画像も未確認。
- 独立レビューで指摘された不明IDの片づけ確認、合成中拒否のテスト整合、解除／pause後に残る待機案内を修正して回帰を追加。二行案内と拒否時の選択維持も対応し、Windows PlayMode77件全件成功画像を受領。個別XML／対象コミットの照合とPixelでの確認が残っています。
- ショップ、正式音源、自然な歩行・生活動作、行動範囲／重なり、公開AAB・提出素材／申告・Play Console要件は未完了。
- フォントの適用役割や新しい素材・ゲーム要素、未承認ファイルの変更が必要な場合は、対象と内容を先に確認します。

## 9. GitHub反映状況・変更ファイル

作業ブランチ：`work`、反映先：`main`。基本Merge改修の実装コミット **`0c72ead2ab79f490956b6405d99836900309fbd8`**、その報告更新は`15e0740`、診断ログ修正は`d3a2522`、承認済みのテスト前提修正は`0102ccd`。今回の追記はPlayMode全件成功画像とAPK実行報告の記録だけで、コード変更はありません。**0102ccdをPull済みなら、この資料更新のためにAPKを再生成する必要はありません。** Pixelでの合格を反映したという意味ではありません。

変更：

- Presentation：`UIFlowController.cs`、`UIStrings.cs`、`MergeBoardView.cs`、`QuestMergeScreens.cs`、`MergeActionMenuView.cs`、`DiscardConfirmationView.cs`。
- 字形補完：`UIFont.ttf`、`UIFontBold.ttf`、`UIFontStory.ttf`（既存書体を保持）。
- PlayMode：`MergeInteractionTests.cs`、`MultiQuestPresentationTests.cs`、`MergePolishTests.cs`、`VerticalSliceFlowTests.cs`（4テストの前提修正を追加承認済み）。
- テスト参照：`Assets/Mofumachi/Tests/PlayMode/Mofumachi.PlayModeTests.asmdef`へ`Unity.InputSystem`1件だけ追加（追加承認済み）。
- スクリプト：`scripts/build-android.ps1`、`scripts/tests/android-tools.tests.ps1`。必須Suiteと98／77件の検査。
- 資料：本報告。

追加：

- `Assets/Mofumachi/Presentation/MergeInputDiagnostics.cs`＋meta。
- `Assets/Mofumachi/Tests/PlayMode/MergeProgressionUiTests.cs`＋meta。
- `docs/merge-progression-pixel3a-checklist.md`。

基本Merge改修の承認範囲の全21変更／追加パスを照合しました。本体asmdef・依存パッケージ・Core・セーブ・素材画像には変更なし。

前回の診断ログ再修正は既存の6ファイルだけです：MergeInputDiagnostics.cs、MergeProgressionUiTests.cs、scripts/build-android.ps1、scripts/tests/android-tools.tests.ps1、本報告、Pixelチェックリスト。今回のテスト前提修正は、追加承認されたVerticalSliceFlowTests.csと、既存の承認対象である本報告の2ファイルだけです。追加ファイル・参照・依存パッケージ・ゲームルール・正式素材の変更はありません。

## 10. Windows PCで次に行う作業

PlayMode77件全件成功の画像とBuild Town QA APK実行済みの報告を受領しました。次は保存したPlayModeのXML、EditMode98件の最新結果、後ほどのPixel 3aのFBを確認します。すでに0102ccdをPullして検証・ビルド済みなら、資料更新のための同じ操作の繰り返しは不要です。以下は未実施の場合の手順です。

1. Unityを閉じ、GitHub Desktopで **Fetch origin→Pull origin**。
2. Unity Hubで6000.6.4f1のプロジェクトを起動。
3. Test Runnerで **EditMode→Run All、PlayMode→Run All**。構成の見込みは98件／77件。今回の2Suiteを含め全件成功・skipなしを確認し、Export Resultsで各XMLを保存。赤い失敗があればAPKへ進まず、失敗名・詳細とXMLを共有してください。
4. **Mofumachi→Android→Build Town QA APK (separate save)**。
5. 同じ街QAへ更新インストールする。アンインストール／データ消去／セーブ初期化は行わない。
6. 既存の詰まった盤面で「種類／整理」→確認付き片づけ→パン生成→合成→納品→報酬→街成長→次依頼。
7. アプリを終了・再起動し、コイン／街Lv.／完了依頼／残った盤面を確認。
8. 同じ画面条件の前後画像とQAログを保存。

全件合格して新しいTown QA APKをビルドした後に使うインストールコマンド：

```powershell
$MofumachiRepo = "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell -NoProfile -ExecutionPolicy Bypass -File "$MofumachiRepo\scripts\install-pixel3a.ps1" -Apk "$MofumachiRepo\Builds\Android\town-life-qa.apk" -Serial "99RAY1BELH" -PackageName "com.mofumachi.merge.qa.townlife"
```

Unityを閉じ、試験・ビルドを一括で行う場合は `scripts/build-android.ps1 -TownQa`。出力先は日時付きフォルダなので、インストールの `-Apk` を実際の出力パスへ差し替えます。詳細なログ／保存コピーのコマンドはチェックリストに記載しています。クラウドの最終APK生成は従来のユーザー方針に従って行っていません。

## 11. 次回の優先作業

1. 成功画像を受領したPlayModeの全件XMLとEditModeの最新結果を照合し、実行詳細を記録。
2. 全件合格後のTown QA APKで、既存Pixelセーブのパン納品・一周・再起動を確認。ドラッグが残ればQAログから原因を特定。
3. 同じ画面条件の前後画像と10分連続操作／文字切れ／保存復帰を確認。
4. 基本操作の合格後に、書体の適用範囲・街経路／生活動作の見本を確認して段階改善。
5. 既存の章と街の楽しさを評価し、追加要素・正式音源・Google Play準備の優先順位を判断。

## 12. リリースに向けた現在地

**50％（概算）**。ソース修正を実機合格には換算していません。10月公開は目標とし、正常な一周と保存・Pixel安定性を優先します。

最大のリスク：①実入力と既存セーブでの進行不能の解消確認、②書体・自然な動き・3章の体験品質、③正式音源・署名AAB・素材／申告／Play Console要件の確認。
