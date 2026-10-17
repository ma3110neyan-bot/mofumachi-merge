# 2026-10-17 Daily World受入・3件の失敗への対応

> 最新の再試験は `4cb3631` 対象、**160成功／1失敗／skip0**。下記の旧3失敗はすべて成功しました。残るNPC表情テストの修正は末尾の「再試験で残った1件」を参照してください。全件合格・APK・Pixelはまだ保留です。

対象は引き続き **`feat/daily-world-pass-20261016`** です。`0799138` 候補のWindows失敗を受け、製品側の原因2つを修正しました。追加機能、main統合、公開AAB作成は行っていません。

**修正版のWindows全件実行・新QA APK・Pixel受入は未実施です。** クラウド検証の成功を、Windowsや実機の成功として扱いません。

## 受領した実行結果

日時フォルダーは `20261016-132434-3b8acdeb`。フォルダー末尾は実行識別子であり、コミットの証明ではありません。

| 対象 | 実際の結果 | 証拠 |
| --- | --- | --- |
| EditMode | 141成功の表示 | PowerShell画像。今回はEditMode XML未受領 |
| PlayMode | **161件・158成功／3失敗／skip0** | 共有されたplaymode.xml／playmode.log |
| QA APK | ビルドへ未到達 | PlayModeのUnity exit2で一括処理停止 |

XML自体にはコミットSHAがありません。受領実行の対象として指定されたのは `0799138` です。過去の成功XMLやAPKで今回の失敗を埋め合わせません。

## 原因と変更

| failing test | 原因 | 修正 |
| --- | --- | --- |
| `DailyWorldTests.SettingsReturnKeepsTheMidWalkWorldPositionWithoutARegionJump` | 北への接続道で、連続カメラの正常な位置を旧地域内0〜1のカメラとしても退避し、`Invalid camera center` が発生 | 連続フィールドではワールド用の一時退避だけを作り、旧地域用の退避をクリア。設定から戻った際のキャラ6名・カメラ・追従を保つ |
| `TownHabitTests.NearbyConversationBelongsToItsActorsAndDoesNotSave` | 北のNPCの実経路では、旧正規化距離0.09以下まで接近しない | 画面と同じ縦横比で投影した距離を使い、小さい方のキャラ幅の1.5倍以内を会話範囲とする |
| `TownRegionLifeTests.TwoResidentsMeetAndResumeInVisibleRegion` | 同上 | 同上。表示内・同地域・入力中ではないこと等の条件、会話中の停止と再開を維持 |

2種類とも製品コードの不具合です。会話が実際に発生することを要求する既存テストは削除・緩和していません。会話テストの待機時間延長、強制的なキャラ移動、skip、ログの抑制も行っていません。

会話距離の回帰は、本物の散歩モデルをLv.0／1／6で120秒進め、北の住人が出会えることを確認します。旧判定で新規4ケースが失敗し、修正後は成功しました。広い画面で遠く離れた相手や、縦方向に離れすぎた相手とは会話しないことも検査しています。

設定復帰テストは実際に北の接続道まで歩き、例外が起きていたカメラ位置に到達してから設定を開きます。復帰後の全員の位置・カメラ・追従と、主人公の移動／ポインター解除を検査します。このUnityテストの修正後実行はWindowsで必要です。

SaveData v3、納品・報酬計算、5×6、正式6名、ロゴ、背景は変更していません。街の途中位置・カメラは従来どおり画面間の一時状態であり、本編セーブへ新しい項目を追加していません。

## 実行済みと未実施

| 検証 | 修正後の結果 |
| --- | --- |
| .NET Core | 125成功／失敗0／skip0 |
| 純粋Presentationロジック | 59成功／失敗0／skip0 |
| Unity6000.6.4f1実DLL参照コンパイル | 全6アセンブリ、警告0／エラー0。Editor実行ではありません |
| PowerShellゲート回帰 | 成功。模擬XML／ADBであり、APK生成や実機試験ではありません |
| 保護ファイル | Core主要4ファイル・正式原画・ロゴ・3背景の計10ハッシュ一致 |
| 独立レビュー | 重大な指摘なし。Windows／Pixel実行は引き続き必要 |
| 修正後Windows／PNG／APK／Pixel | **未実施・未取得** |

宣言構成は **EditMode141／PlayMode161（DailyWorldTests10）** のままです。実発見数・実行結果は新XMLを正とします。クラウドには有効なUnity Editorライセンス、Androidモジュール、接続Pixelがないため、実行済みの範囲を分けて記録しています。

機械可読な記録は [daily-world-acceptance-fix-2026-10-17.json](verification/daily-world-acceptance-fix-2026-10-17.json)。以前の[実装記録](development-handoff-2026-10-16-daily-world.md)は履歴として保持しています。

## Windowsでの次の操作

1. Unity Editorを閉じます。
2. GitHub Desktopの `Current repository` が `mofumachi-merge`、`Current branch` が `feat/daily-world-pass-20261016` であることを確認します。
3. `Fetch origin`、表示された場合は `Pull origin` を押します。`History` の最上段が今回案内する修正版コミットになったことを確認します。変更ファイルがあっても破棄やコミットはしません。
4. PowerShellへ以下を貼り付け、Enterを押します。実行中はUnityを別に開いたり、ブランチを切り替えたりしません。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
```

両方の `Tests passed` の後にも処理は続きます。`APK:`、`SHA256:` と入力待ちの `PS C:\...>` が戻るまで待ってください。赤いエラーがあれば、新しい日時フォルダーのXMLと該当ログから確認し、Pixel更新へ進みません。

完了時は、**今回新しく生成された同じ日時フォルダー**の `editmode.xml`、`playmode.xml`、`build-info.json`、`screenshots.zip`、`texture-memory.txt` とPowerShell終了画面を共有してください。過去のフォルダーや直下の古いAPKは使用しません。

全件Passed／failed0／skip0、必須suite（DailyWorld10・地域・QualityPass・Experience）、36枚の実Unity画像、同じ実行のAPK・SHAを確認してからPixelを更新します。packageIdは `com.mofumachi.merge.qa.townlife`。アンインストールやデータ削除は行いません。

Pixelでは通常歩行とカメラ接続、設定／Back／pause、NPC会話と再開、斜めMerge、図鑑・今日のお願い・1日1回の花、BGM、保存保持・コアループ・再起動・10分試遊を確認します。実画面の楽しさや継ぎ目も未評価です。**main統合は引き続き保留**します。

## 再試験で残った1件

ユーザーが `4cb3631` を確認後に実行した `20261017-094530-05ca1e11` のXML／ログを受領しました。EditModeは141成功の画面表示、PlayMode XMLは **161件・160成功／1失敗／skip0** です。前回の設定復帰・NPC会話2件はすべて成功。DailyWorld10、地域、QualityPass、Experience suiteも全成功でした。APKはPlayMode失敗により未生成です。

残る失敗は `TownLifeTests.NpcsVisitSeveralStreetStopsWithoutChangingProgress` の `Expected: Smile / But was: Normal`（旧90行目）。120秒散歩した同じNPCへ、地域の中心にカメラを向けただけでタップするテストになっていました。

実際の散歩モデルと画面の投影を使った診断では、360×740の縦画面で対象フィオは120秒後に北への接続道 `(0.286, 0.830)` 付近にいます。所属判定は北ですが、地域中心カメラはY=1.320のため、キャラは表示範囲外です。`TownActor.React` は画面外入力を正常に拒否し、表情は通常のままとなります。この診断は純粋モデルであり、修正版Unity実行の代用ではありません。

今回の変更は **TownLifeTests.csだけ**です。製品の入力・会話・表情・保存処理は変更していません。

- 他の実機向けテストと同じ360×740／Safe Areaを固定し、EditorのGameビュー寸法による差を除きます。
- 120秒の散歩、3か所以上の訪問、保存不変の検査を維持します。
- 同じNPCを動かさず、カメラをその現在位置へ向けてからタップします。NPCの位置不変と、身体全体が街の表示内に入ることも検査します。
- 笑顔・上下動の検査を維持し、タップ直後の本人の吹き出しと会話記録+1を追加します。表情を強制設定せず、通常の反応処理を通します。
- 後半のベンチ／噴水／花への実移動、6行き先、48以上の操作領域、到着、pause、保存不変の検査も維持します。

今回もCore125／純粋Presentation59成功、6アセンブリのUnity参照コンパイルは警告・エラー0。宣言構成は141／161（DailyWorld10）のままです。**このテスト修正後のWindows実行は未実施**で、同じ一括コマンドを最初から再実行してください。両XML・36PNG・新APK・build-infoを新しい同一日時フォルダーで揃えます。

最新の機械可読記録は [daily-world-acceptance-retest-2026-10-17.json](verification/daily-world-acceptance-retest-2026-10-17.json)。main統合、旧APKの流用、Pixel更新は保留です。
