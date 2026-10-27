# 噴水最終版：Pixel 3aで初回から確認

原本18ZIP・370全件成功・219全景／240連続画像・旧Before3枚と、全Lvの納品／シーン再読込を[照合済み](fountain-final-native-review-2026-10-27.md)。**同runの生成済み街QAで実機確認へ進行可。Pixel合格・噴水採用は未了。** 今回は記録更新だけで、Pull／再ビルドは不要。

## 街QAだけを初期化してインストール

ユーザーの指示どおり、現在の**街QAのセーブを消して**初回から検証する。削除・新規インストール・復元されたQAデータの消去は明示承認済み。同じ承認を再度求めない。製品アプリ・成長確認アプリ・他端末は対象にしない。

Pixel 3aをUSB接続し、ロック解除・USBデバッグを許可してから、Windows PowerShellへまとめて貼り付ける。

```powershell
& {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    $MofuApk = ".\Builds\Android\20261027-102109-3bbbf9fe\town-life-qa.apk"
    $MofuSha = "1a4b99518f4e846c2ffa45c80069b46b68ba59d2ece810187f6135420d535ace"
    powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\install-pixel3a.ps1" -Apk $MofuApk -PackageName "com.mofumachi.merge.qa.townlife" -Serial "99RAY1BELH" -FreshTownQa -ExpectedSha256 $MofuSha
    if ($LASTEXITCODE -ne 0) { throw "初回インストールが停止しました。この出力をお送りください。" }
}
```

既存のhelperが実APKのSHA、正確なID、ARM64、serial、API、Pixel 3aを**削除前**に照合する。その後、街QAだけをuninstall→install（`-r`なし）→対象QAの復元データ消去→起動する。Androidの自動復元で旧Lv.6が戻るのを防ぐ。途中で止まったら出力を残し、別アプリ削除・署名回避・端末全体の初期化をしない。

このクラウドにはUSB接続がないので、実際のインストールはまだ行っていない。まずインストールの終了画面と、初回に開いた画面を提出する。

## 初期状態と噴水

「はじめる」から正式6名の選択へ進み、推しを選ぶ。**街Lv.0・0Coins・依頼未完了・盤面にお茶Lv.1が2個**を確認する。タイトルの初期イラストと、保存済みの推し選択を混同しない。旧Lv.6／330Coinsが出た場合は初期状態が成立していないので、その状態で続きを試験せず画面を残す。

中央Homeを前面にし、メニューを閉じて待機開始を記録する。通常の流水／しぶき／波紋／きらめきと、**30〜60秒内のshow**、約6秒の高い噴き上げ・増水、自然な通常復帰を見る。今回のUnity記録はshow状態約37.98秒、最大水量近く約38.73秒、通常復帰約45.03秒。実機でも同じことを確認するまで成功としない。

Home内のカメラ移動では時計が進み、メニュー／別画面／アプリ休止中は停止する。自動会話パネルで水面を見失わないかも見る。噴水の奥→左右→手前を歩き、顔・名前が追えて自然な前後関係になること、東の木陰／端からも自力で戻れることを確認する。

## 全Lvで保存と完全終了・再起動

最初のお茶2個を合成して受注・納品するとLv.1。その後も生成→合成→受注→納品→報酬→成長を通常操作で進める。別の成長確認APKや、完成アイテム／Lvの付与を使わない。

| 到達Lv | 必要品 | 累計Coins | 5地区の追加・不変／保存→完全終了→再起動 |
|---|---|---:|---|
| 1 | お茶Lv.2×1 | 30 | 実機未確認 |
| 2 | パンLv.2×1 | 70 | 実機未確認 |
| 3 | 花Lv.2×1 | 110 | 実機未確認 |
| **4** | **お茶Lv.3×1＋パンLv.2×1** | **180** | **実機未確認** |
| **5** | **花Lv.3×1＋お茶Lv.2×1** | **250** | **実機未確認** |
| 6 | パンLv.3×1＋花Lv.2×1 | 330 | 実機未確認 |

**各Lvで一旦止まり**、中央・西・東・北・南と、追加装飾・短いLv.UP／Before／Afterを確認する。中央Lv.4／5、北／南Lv.2の景観は現在の設計では不変。成長画面の地区ボタンを切り替え、実際に育った地区を見る。人が遊ぶ時間と、撮影・メニュー・試験用再起動の時間を分けて記録する。必要量は未変更なので、毎回1件、後半3段階は同量の材料であることも記録する。

保存後、街QAを完全終了してランチャーから再起動する。タイトルへ戻るだけでは完全終了確認としない。必要なら、接続したPCで以下を使える。

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" -s "99RAY1BELH" shell am force-stop "com.mofumachi.merge.qa.townlife"
```

Lv・Coins・完了依頼／受取済み報酬・盤面・5地区の装飾が同じことを確認する。完了済み依頼が再受注・再報酬を許さないことも確認する。再起動後に同じ報酬が増えたり進行が戻る場合は画面を残す。

## 10分と負荷の記録

全Lvの途中確認を終えたら、同じAPK・中央Homeを前面にして、10分以上連続操作する。既存計測は、リポジトリを開いたPowerShellで以下を実行する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\capture-pixel-visual-check.ps1" -Label After -Seconds 600 -Serial "99RAY1BELH"
```

FPS／フレーム時間・メモリ・開始終了のバッテリー温度・thermal原文・実機画像・インストール済みAPK SHAを記録する。値が取れない項目は未計測。4fpsのUnity画像記録を実機FPSにせず、バッテリー温度をCPU温度にしない。比較可能なBefore測定がなければ「負荷増加なし」と断定しない。

インストール結果・初期状態・通常／show・全Lvごとの復帰結果と、計測終了時の出力フォルダーを提出する。重さ・熱・入力・音切れ・停止／クラッシュと、会話中の水の見やすさも添える。ユーザーが確認するまで噴水採用・2.5D・Merge・mainには進めない。
