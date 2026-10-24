> 2026-10-25の最新候補は073237版です。[最新のPixel受入手順](town-boundary-pixel3a-acceptance-2026-10-25.md)を使用してください。以下は前工程の履歴です。今回、旧220250版を再インストールしたり再ビルドしたりする必要はありません。

# 承認済み5エリア：全件テスト・QA APK・Pixel3a受入

承認対象は638aa24の5エリア実Unity画面（20261024-110359-39b38515）。今回許可された北カメラと薄い道案内文字の表示だけを調整し、原画・配置・歩行面・保存・報酬・BGM・Mergeは維持します。今回はmain統合を行いません。

現在はrun `20261024-220250-ba90ed62` の143/143・222/222、Unity102画像と修正版APK生成をPowerShell画面で確認済みです。生成済みAPKをそのまま使用し、下の「更新インストール」へ進みます。記録更新のPull・再ビルドは不要です。成功XML・build-info原本照合と、修正版のPixel実機・600秒連続試遊は未完了です。旧185942で報告された起動・保存復帰を修正版の実機結果へ流用しません。

## 実行済み：修正版の全件テストとAPK生成

1. GitHub Desktopで `feat/daily-world-pass-20261016` をPullし、Unityを閉じます。
2. Windows PowerShellで実行します。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
```

EditMode／PlayModeはフィルターなしの全件です。143（通常125＋BuildTime18）／222が実行され、最新のコンソールは全件成功・APK生成完了と報告しています。成功XML原本のfailed0／skipped0／passed=totalは別途照合します。

今回のrunにある `editmode.xml`、`playmode.xml`、`build-info.json`、`capture-manifest.json` を保存してください。失敗時は同じrunのxml／logとPowerShell画面を共有します。画像一式は同runの `five-plaza-review-*.zip`。従来の非APK五地区レビューZIPと混ぜません。

## 更新インストール

以下は生成済み220250版のAPK本体とbuild-infoのSHAが一致した時だけ街QAへ更新します。旧185942は修正前として保持します。署名不一致等が出たら停止し、アンインストール／アプリデータ消去で回避しません。既存保存のない端末は「更新保存の保持」を実証できないため、その点を記録します。

```powershell
$MofuRun = "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Android\20261024-220250-ba90ed62"
$MofuInfo = Get-Content -LiteralPath (Join-Path $MofuRun "build-info.json") -Raw -Encoding UTF8 | ConvertFrom-Json
$MofuApk = Join-Path $MofuRun "town-life-qa.apk"
if (-not $MofuInfo.townQa -or $MofuInfo.appId -ne "com.mofumachi.merge.qa.townlife" -or (Get-FileHash -LiteralPath $MofuApk -Algorithm SHA256).Hash -ne $MofuInfo.sha256) { throw "今回の街QA APKとSHA256を確認できません。" }
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\install-pixel3a.ps1" -Apk $MofuApk -PackageName "com.mofumachi.merge.qa.townlife" -Serial "99RAY1BELH"
if ($LASTEXITCODE -ne 0) { throw "更新インストールが停止しました。保存を消さずに画面を送ってください。" }
```

シリアルは過去に確認したPixelの99RAY1BELH。別端末／シリアルの場合は `adb devices` で確認した対象へ置換します。次のコマンドでモデル・API・画面サイズも記録します。

```powershell
$MofuAdb = "$env:ProgramFiles\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
& $MofuAdb -s "99RAY1BELH" shell getprop ro.product.model
& $MofuAdb -s "99RAY1BELH" shell getprop ro.build.version.sdk
& $MofuAdb -s "99RAY1BELH" shell wm size
& $MofuAdb -s "99RAY1BELH" shell wm density
```

モデルPixel3aと今回のAPK／アプリIDを確認します。通常版や別QAでの成功を混ぜません。インストール成功は画面／保存／試遊の合格ではありません。

## 5エリアと表示

各往復をキャラの徒歩で実施。ファストトラベルだけで合格にしません。

| 往復 | 確認 | 結果 |
| --- | --- | --- |
| 中央→西→中央 | カフェ・道・入口・NPCのすれ違い | 未確認 |
| 中央→東→中央 | 花屋・道・入口・名前札 | 未確認 |
| 中央→北→中央 | 依頼小屋・左右小道・待機場所 | 未確認 |
| 中央→南→中央 | 庭園・左右小道・東屋・ベンチ・接続 | 未確認 |

全地区で文字切れ／Safe Area／名前札／吹き出し／HUDの景観遮蔽／カメラ追従／タップ反応／NPCの詰まりを確認します。Lv1・3・6のうち実際に確認できたLvを記録し、未到達を確認済みにしません。承認Unity画像より景観が大きく劣化していないか比較します。

持越し：南入口の石目のぼけ、西・東の既存材質境界。実機で気になる程度と場所を記録し、これだけで候補を止めたり、その場で景観を再制作したりしません。

## コアループと保存復帰

TOP→街→キャラ移動→依頼→Merge→納品→報酬→街成長→図鑑を一周します。納品前後のCoins／街Lv／依頼状態、盤面・所持、図鑑の発見／会話、設定状態を記録します。報酬計算やセーブ構造は変更しません。

終了前の状態を画面とメモに残し、同じ街QAを終了・再起動して復元を比較します。次はデータ消去ではなくアプリ終了です。

```powershell
& $MofuAdb -s "99RAY1BELH" shell am force-stop com.mofumachi.merge.qa.townlife
& $MofuAdb -s "99RAY1BELH" shell monkey -p com.mofumachi.merge.qa.townlife -c android.intent.category.LAUNCHER 1
```

保存復帰後に同じ依頼を再納品できず報酬が二重にならないこと、盤面／図鑑／設定／Coins／Lvを確認。アプリの更新前から存在した保存の継続も別に記録します。

## 10分以上の連続試遊と動作

保存復帰確認後、600秒以上続けて実際に街移動・NPCすれ違い・依頼・Merge・メニュー操作を行います。起動放置やインストール待ち時間を試遊時間に含めません。開始／終了時刻、実プレイ秒数、停止・中断・進行不能の有無、発熱／重さ／クラッシュを記録します。

```powershell
$MofuPlayStartUtc = [DateTime]::UtcNow
```

実際の連続操作後：

```powershell
$MofuPlayEndUtc = [DateTime]::UtcNow
$MofuPlaySeconds = ($MofuPlayEndUtc - $MofuPlayStartUtc).TotalSeconds
$MofuPlayStartUtc; $MofuPlayEndUtc; $MofuPlaySeconds
if ($MofuPlaySeconds -lt 600) { throw "10分未満です。600秒以上の実試遊が必要です。" }
```

この経過時間だけでは操作継続の証明にはならないため、実施内容と結果も記録します。試遊終盤の30秒は、別PowerShellから次を実行しつつ同じ街の道を歩いてください。実SurfaceView時刻／PSS／thermal／端末PNG／インストール済みAPK SHAが保存されます。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\capture-pixel-visual-check.ps1" -Label After -Seconds 30 -Serial "99RAY1BELH"
```

30秒の計測は10分試遊の代用ではありません。計測不能の場合は代替FPSで成功にせず、生ログと未確認理由を残します。Unity隔離テスト画像と端末PNGも混同しません。

## 提出する結果

今回のXML2件／build-info／SHA、インストール結果、上記4往復、表示、コアループ、保存復帰、実プレイ開始・終了・600秒以上の結果、重大不具合の有無をまとめます。Pixel記録は `Builds\PixelVisualLock` の今回フォルダー。クラッシュや停止時のlogcatは `adb -s 99RAY1BELH logcat -d -s Unity AndroidRuntime`。

全件成功・APK成功・Pixel移動／表示／保存復帰／600秒試遊が揃い、承認Unityの品質が保たれた時にだけ正式ベース候補へ進みます。現時点でその合格を記入しません。main統合は別途承認です。
