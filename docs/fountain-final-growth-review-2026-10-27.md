# 10月27日：噴水最終Passと全Lv確認の実行手順

ソース修正済み。**新しいUnity実画面・APK・Pixel確認は未実行**。今回の製品変更は`TownFountainWaterLayout.cs`だけで、通常水の芯・青い流水・しぶき・波紋・反射を強め、ショーの白い水を固定線から弧に沿って流れる塊へ変えた。196quad／追加784頂点、QA45秒周期・show6秒、親のY描画順を維持。街の配置・素材・歩行・キャラ・図鑑・BGM・Merge・進行条件・報酬・Save形式は変更していない。

既存の全件QAを一度実行する。前版192546の成功やAPKは新しい最終Passの結果へ流用しない。**新QAが成功するまで、Pixelの現在版を先に削除しない。**

## 1. Windowsで修正版を取得して全件QA

GitHub Desktopで`feat/daily-world-pass-20261016`を選び、修正版をPullする。Unity Editorを閉じてから、Windows PowerShellへ以下をまとめて貼り付ける。`main`へ切り替え・統合しない。

```powershell
& {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    $MofuStage = Get-Content -LiteralPath ".\References\VisualLock\stage-acceptance-2026-10-21.json" -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($MofuStage.base_polish -ne "References/VisualLock/fountain-final-candidate-2026-10-27.json") {
        throw "10月27日の修正版がまだ取り込まれていません。GitHub Desktopで作業ブランチをPullしてください。"
    }
    powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
    if ($LASTEXITCODE -ne 0) { throw "QAが停止しました。表示された出力と、今回のplaymode.xml／playmode.logをお送りください。" }
}
```

予定される棚卸しはEdit144／Play226。**実成功件数ではない**。全件failed0／skip0／inconclusive0と新しい実画像を確認後に、同じsourceの街QA APK・SHA256を生成する。成長確認の別APKが併せて生成されても、今回の初回実進行は**街QAそのもの**で行う。

今回の追加確認は、導入0→1と1→2→3→4→5→6の全6遷移。それぞれ中央／西／東／北／南の成長比較と再読込後の全景を撮影する。計60枚の成長画像を含む**219枚の新しい720×1280全景**、実Update60秒の噴水切出し、旧版の実Before3枚が必要。新しい検査ではLv.4／5や地区別画像・保存証拠が欠けるとAPKへ進まない。模擬メタデータによるスクリプト試験の成功を、この219実画像の確認とは呼ばない。

## 2. 実行後に提出するもの

終了画面に表示された**今回の出力フォルダー**を開き、`five-plaza-review-*.zip`を番号順にすべて提出する。途中で止まった場合は、その出力フォルダーの`playmode.xml`・`playmode.log`と停止画面を提出する。同じ失敗のまま何度も再ビルドしない。

ZIPには原テストXML、source／fingerprint、APKのID・bytes・SHA、全画像、`town-growth-progression.json`、`fountain-live`連続画像と時刻、比較HTMLが入る。特に新しい`32-growth-3-to-4-*.png`・`32-growth-4-to-5-*.png`・各Lv再読込画像を確認する。Beforeはsource98596daf／run192546の実原本3枚で、今回の描き直しをBeforeと称しない。

## 3. 同じ街QAをPixelへ新規インストール

新QAの原本・画像・SHAを照合後、生成済みの同じ街APKを使う。ユーザーの最新指示で、現在の街QAのアンインストールと初期化は承認済み。同じ承認を再度求めない。別アプリの削除や端末全体の初期化は行わない。

既存`install-pixel3a.ps1`に追加した`-FreshTownQa -ExpectedSha256 <今回照合したSHA>`を使用する。**パッケージ`com.mofumachi.merge.qa.townlife`・serial`99RAY1BELH`・Pixel 3a・APK SHA・ARM64/APIを確認してから**、対象の街QAだけをアンインストール→`install`（`-r`なし）→対象QAの復元データを消去→起動する。Androidの自動復元で既存Lv.6が戻ることを防ぐため、インストール直後・初起動前にこのQAだけを消去する。端末全体のバックアップ設定は変えない。

通常のインストールは従来どおり`install-r`で、明示的な`-FreshTownQa`がない限りアンインストール／消去を行わない。製品ID・成長確認ID・別端末へのFresh指定、SHA不一致、別モデルは削除前に拒否する。インストール失敗時に別アプリを削除して回避しない。

このクラウドにはUSB接続がないため、**実際の削除・インストール・消去はまだ行っていない**。上記の実行には、受領後に照合した新APKのパスとSHAを使う。古いAPKのSHAは使用しない。

## 4. 初回から全Lvを確認する

最初に「未選択キャラ／街Lv.0／0Coins／未完了0件／お茶Lv.1×2」を記録する。最初のお茶を合成・納品してLv.1へ進め、その後も実際に生成→合成→受注→納品→報酬→成長を操作する。既存Lv.6セーブ、成長確認APK、Lvの直接設定、完成アイテム付与を使わない。

| 到達Lv | 今回納品する品 | 累計依頼 | 累計Coins |
|---|---|---:|---:|
| 1（導入） | お茶Lv.2×1 | 1 | 30 |
| 2 | パンLv.2×1 | 2 | 70 |
| 3 | 花Lv.2×1 | 3 | 110 |
| **4** | **お茶Lv.3×1＋パンLv.2×1** | **4** | **180** |
| **5** | **花Lv.3×1＋お茶Lv.2×1** | **5** | **250** |
| 6 | パンLv.3×1＋花Lv.2×1 | 6 | 330 |

**各Lvで止まり**、必要条件・生成／合成回数・操作時間、何が増えたか、全5地区、短いLv.UP／Before／Afterを確認する。中央Lv.4／5、北／南Lv.2は今の設計では装飾不変。それを追加済みと報告しない。中央だけでなく、成長画面の地区ボタンを切り替えて実際に変わった場所も見る。

保存後に街QAを完全終了し、ランチャーから再起動する。各Lv・装飾・Coins・依頼／報酬・盤面が同じか、同じ報酬を再受取できないかを記録する。タイトルへ戻るだけでは完全終了確認としない。試験用撮影・メニュー・再起動の時間と、実際に遊ぶ時間を分けて計測する。

現在の必要量は1→2／2→3が各Lv.1素材2個相当、3→4／4→5／5→6が各6個相当で、毎回1依頼。**高Lvほど段階的に長くなる値には変更していない。** 改善案は[現状監査](fountain-growth-audit-2026-10-27.md)へ記載済み。今回の実測後も、承認前にバランスを変更しない。

## 5. 噴水と負荷を確認する

中央Homeを前面にしてメニューを閉じ、待機開始を記録。通常の水が落下し、しぶき・波紋・きらめきが分かるかを見る。QAは45秒周期で、初回は約38秒でshow開始、6秒のshow、1秒で通常復帰する設定。実機で30〜60秒以内の発生・水量増加・高い噴き上げ・復帰を動画または時刻付き連続画像で確認する。メニュー／別画面／アプリ休止中は時計停止、Home内のカメラ移動では時計継続。3分待機を要求しない。

噴水の奥→左右→手前を実際に歩き、前後関係と顔・名前の視認性を確認する。中央Homeが前面の状態で既存の計測も使う。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\capture-pixel-visual-check.ps1" -Label After -Seconds 600 -Serial "99RAY1BELH"
```

10分以上の操作、FPS／フレーム時間、メモリ、開始終了のバッテリー温度・thermal原文、入力・音・停止／クラッシュを記録する。取得不能は未計測、バッテリー温度はCPU温度と混同しない。比較できるBeforeがなければ「負荷増加なし」と断定しない。NUnitや4fpsの画像記録は実機FPSではない。

噴水がユーザーに明確に動いて見え、全Lvの実操作と保存復帰が成立してから合格候補にする。確認前に2.5D・Merge・次工程・mainへ進めない。
