最新の生成枠改修は[2026-10-13の報告](development-handoff-2026-10-13.md)を参照。今回版は105／85件の全件成功後にAPK・実機確認します。生成枠の一括整理と盤面アイテムの片づけを区別してください。

# Merge／納品修正版 — Pixel 3a確認チェックリスト
2026-10-12。**未実施の確認を成功扱いにしない。** 最新5枚は未受領です。ここにはゲームの新機能やセーブ初期化を含めません。

## 更新前
- インストール先は既存の **com.mofumachi.merge.qa.townlife**。別QA／通常版の保存とは区別する。
- アプリデータを消去・アンインストールしない。可能なら下記の読み取りコピーで詰まった状態を記録する。
- Gitコミット、Edit／Playの全件XML、APK SHA256と実施日時を記録。手動メニュービルドとPowerShellの日時付き出力先を混同しない。
- WindowsのEditMode／PlayModeが全件成功するまで、公開合格とは扱わない。診断出力の回帰1件を追加し、コンパイル済み構成は98件／77件。今回の2SuiteはMergeProgressionUiTests、MergeGuidanceTests。実行件数・合否はWindowsのXMLで確認する。

## 画面と実操作
1. TOP→ホーム→依頼。依頼名、要求種類・正確なLv.・必要数・所持数を確認。パンだけの依頼か、お茶も必要な複数条件か記録。
2. 未受注でMergeへ行くと「未受注」と「依頼を見る」が分かる。受注後は「受注中」で、パンが不足なら「パンを作る」が初期選択。
3. 「種類／整理」を開いて茶器／パン／花を選べる。戻った後は作るボタンの種類が変わる。
4. 満杯時の作る操作はアイテムを失わず、整理方法が表示される。
5. 不要なマスを選択→種類／整理→片づけ。種類とLv.の確認が出る。取消は減らさず、確定は1個だけ減る。
6. 空いた2マスへパンLv.1を2個生成する。生成後に古い茶器の選択が残っていない。
7. パンLv.1を1個タップ→もう1個タップ。パンLv.2が1個でき、1マス空く。演出後、必要数表示と納品ボタンが更新される。
8. 別のパン2個を用意し、**ドラッグも**確認。移動中の絵が指へ追従するか、どのマスからどのマスへ落としたかを記録。
9. 異種／異Lv.／最高Lv.／元のマス／盤面外を試す。拒否理由が分かり、数・Lv.・Coinsが変わらない。拒否前に選択したマスが消えない。
10. 合成中の追加入力と納品を試す。二重合成・二重報酬がない。少し待つと操作できる。
11. 必要数がすべてそろったら納品。対象の種類／Lv.／個数だけが減る。報酬・街成長・次依頼へ進める。
12. 納品後に連打・画面再入場しても再払いがない。アプリ終了・再起動で盤面・コイン・街Lv.・完了依頼・お気に入りを保持し、次の依頼へ進める。

## 画面比較
実際の同じ端末・縦向き・保存条件で、修正前後を記録します。画像を生成した見本は実機比較に含めません。

- 依頼画面：依頼名・要求・所持数。
- Merge：盤面全体、生成ボタン、受注／納品状態。
- 種類／整理メニュー：選択対象・整理導線。
- 片づけ確認：種類とLv.、取消／確定。
- 納品結果／街成長：報酬、街Lv.、次の依頼。

文字切れ・枠の重なり・Safe Area・縮小表示・全画面・ボタンの押しやすさを確認。10分以上の連続操作で音、発熱／重さ、フリーズ／クラッシュも記録します。今回修正版で未実施の項目はそのまま未実施とします。

## PowerShellで保存コピー・QAログを残す
追加参照は承認・反映済みです。まずWindowsで全件検証し、全件合格後に生成した新しいTown QA APKで確認してください。クラウドで新APKやPixel試験を実行したという意味ではありません。

QA情報ログはDevelopment Buildのプレイヤーで出力し、Editorでは出力しません。今回のWindows失敗にあった通常ログとLogAssert.NoUnexpectedReceivedの干渉を避け、厳格なエラー／警告検査を維持するためです。Pixelでは下記のログ取得を継続して使えます。

### 更新前の読み取りコピー（可能な場合）
Pixelを接続・ロック解除し、ゲーム操作を止めて実行。新しいフォルダを作るので以前の記録は上書きしません。

```powershell
$MofumachiRepo = "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
$MofumachiAdb = "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$MofumachiCheckDir = Join-Path "$MofumachiRepo\Builds\Android" ("merge-check-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $MofumachiCheckDir | Out-Null
& $MofumachiAdb -s "99RAY1BELH" pull "/sdcard/Android/data/com.mofumachi.merge.qa.townlife/files/save.json" "$MofumachiCheckDir\save-before.json"
```

コピーが端末の権限で失敗した場合も、初期化や削除で解決しないでください。保存データには手を加えず、表示されたエラーを記録します。

### 操作直後のログ取得
同じPowerShellで、問題の合成または正常な一周を実行した直後に：

```powershell
& $MofumachiAdb -s "99RAY1BELH" logcat -d -v threadtime "Unity:I" "AndroidRuntime:E" "*:S" | Set-Content -LiteralPath "$MofumachiCheckDir\pixel-log.txt" -Encoding UTF8
Get-Content -LiteralPath "$MofumachiCheckDir\pixel-log.txt" | Select-String -SimpleMatch "MOFUMACHI_MERGE_INPUT" | Set-Content -LiteralPath "$MofumachiCheckDir\merge-input.txt" -Encoding UTF8
& $MofumachiAdb -s "99RAY1BELH" pull "/sdcard/Android/data/com.mofumachi.merge.qa.townlife/files/save.json" "$MofumachiCheckDir\save-after.json"
Get-FileHash -Algorithm SHA256 -LiteralPath "$MofumachiRepo\Builds\Android\town-life-qa.apk"
$MofumachiCheckDir
```

最後に表示されたフォルダに保存コピーとログがあります。インストールが日時付きAPKなら、ハッシュのパスをそのAPKへ差し替えます。ログは蓄積分を含むため、試した時刻とAPK SHA256を一緒に残してください。ログバッファやアプリデータは消去しません。

実機ログでは、drag-begin→drag-endの元／先、種類・Lv.、inputAllowed、pressCamera、viewport／Safe Area、drag-cancel、drop-resultを照合します。no ghost／layout changed／no cell hitは未特定部分を切り分ける情報であり、ログが出たことだけで実機合格にはなりません。
