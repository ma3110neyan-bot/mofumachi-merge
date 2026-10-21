# 正式Visual Lock／中央マスター工程 — 2026-10-21

**中央実Unity画像を受領・確認し、b25658eはVisual Lock未達で不採用。中央を再調整したソース候補は実装済み・実機未確認。中央未合格・ベース版未完成。**

ユーザーが提示済み画像を正式Visual Lockとして承認した。中央の実描画合格まで他地区・Mergeの新たな展開を始めない。今回のソースは中央工程に限定し、既存の未採用候補をベース版の合格へ読み替えない。

## 実画面の判定と中央の再調整

[実UnityのBefore／Visual Lock／b25658e比較](../References/Review/central-master-2026-10-21/central-before-lock-candidate.jpg) はアップロード原本のまま。ユーザー実行 `20261021-202704-1d9ab213`、Editor6000.6.4f1、テスト3／3、720×1280の3枚、固定Before3組、承認画像・SHA・中央入力digestの一致を確認した。[判定記録](../References/Review/central-master-2026-10-21/native-review.json)。

**中央は不採用。** 旧Beforeより花・緑は増えたが、承認画像より噴水が小さく円形石畳の空白が勝つ。成長用の追加花壇は下端UIに隠れ、Lv1／3／6差が弱い。初期NPCは他地区のみで、中央に友達がいない。街灯の存在感も弱い。テスト成功を見た目の合格へ読み替えない。

正式原画・高密度な地表・広い歩行面を保ち、中央だけを再調整した。

- 噴水幅0.36→0.58。元の透明原画の比率を保ち、描画・ランドマーク・丸い衝突基礎を共通定数へ揃えた。噴水左右の経路を維持。
- 新しい丸い噴水に不要な旧四角い前面障害物を削除。原画の底面の幅を測り、見えない角でベンチへの道を塞がないようにした。旧すれ違いテストを緩めず、失敗の原因を取り除いた。
- 旧下端の仕上げを残し、下部UIより上にLv0／3／6の置換花壇を追加。最初から小花壇があり、上昇時に重ねず置き換える。
- 正式原画の街灯を読み取れる大きさへ。新規画像やテクスチャは追加していない。
- 西に重複していたNPC1名を既存の中央ベンチ前へ。中央に主人公と友達、他4地区にも1名ずつ。会話数や依頼・セーブを自動加算しない。

この新しい候補の実Unity見た目は未確認。中央の合格前に他地区・Mergeへ進まない。

## 正式基準と復旧

- `References/VisualLock/approved-design-2026-10-21.json`：承認PNG／JPEGのSHA・中央とMergeの領域・8判定軸を固定。
- `References/VisualLock/stage-acceptance-2026-10-21.json`：中央は未合格。残る工程は未着手。自動合格・自動採用しない。
- 従来の153凍結入力は保持。正式6名・ロゴ・Core・保存・5×6・図鑑・日次・TOP・選曲を変更していない。
- 作業前の復旧タグ `checkpoint/pre-central-visual-lock-20261021` は7085af7。これは未採用候補の退避点であり、良い見た目を実証した版という意味ではない。
- 実UnityのBeforeは製品bd3f357／実行20261021-113845-fbcafb0d。前版の成功を今回へ引き継がない。

## 工程の状態

|工程|状態|
|---|---|
|中央マスター制作|実画面b25658eを不採用、中央の再調整候補を実装|
|中央の実Unity比較／合格|b25658eは実画面確認・不採用。再調整後は未確認／未合格|
|西／東／北／南の新たな展開|**中央合格待ち、未着手**|
|5地区接続の仕上げ|未着手|
|Mergeの正式Visual Lock化|未着手。現在の5×6機能を保持|
|UI整理／基本ループ回帰／Unity全件|本工程では未実施|
|QA APK／Pixel受入|**未生成／未確認**|

ソース上の経路・基礎・段階置換を含む今回の純C#表示／経路249件、正式Lock／段階ゲート検査、実DLL参照6asmdefコンパイルが成功（警告／エラー0）。新しい4検査は実装前の失敗を確認した。全件で見つけた通路の失敗も原因を直して再確認。Core・正式原画・既存テクスチャ入力は変更なし。旧b25658eについてはWindowsの実撮影・比較JPEG生成も受領できたが、新候補のネイティブ検査は未実行。これらは見た目・FPS・Pixel操作を合格にするものではない。

## 中央だけの実Unity確認

GitHub Desktopで現在のfeatureブランチをPull後、ライセンスのあるWindowsのUnityで実行する。

```powershell
& { $ErrorActionPreference="Stop"; Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"; powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -CentralReview }
```

`Builds/CentralReview/<実行ID>/central-master-review.zip` を生成する。既存の隔離撮影と同じ方法でLv1／3／6の中央実PNGを取得し、原本Before・正式Visual Lock・候補を並べた `central-before-lock-candidate.jpg`、source／fingerprint、未合格JSONをまとめる。**APKは生成しない。**

Before用 `Builds/Android/20261021-113845-fbcafb0d` は残す。ない場合は比較未完了として記録し、中央を採用しない。生成素材や内部CPU配置図を実Unity Afterへ混ぜない。

中央が未合格なら通常のAPK生成は停止する。合格フラグに加え、実画像3枚・固定Before3枚・承認画像・比較JPEGのSHA、撮影時の中央入力digestとの一致が必要。中央の共有描画／経路を変更したら再撮影する。後続の地区だけのコミットでHEADが変わったことを理由に合格を失効させるのではなく、中央の依存入力を検査する。中央合格だけでもAPKへは進めず、西から基本ループまでの工程記録を順序どおり確認し、その後に同一ソースのUnity全件を実行する。中央合格後に、西→東→北→南→接続→Merge→UI→基本ループ→Unity全件→QA APK→Pixelを順に進める。

検査記録：[初回候補](verification/central-master-2026-10-21.json)／[実画面後の再調整](verification/central-native-followthrough-2026-10-21.json)。

前回の中央レビュー `Builds/CentralReview/20261021-202704-1d9ab213` も残す。前回の画像は不採用判定の証拠であり、新候補のAfterではない。

## 未解決事項

**実装済み・実機未確認。中央の採用は保留。** 再調整後の中央のUnity実描画がまだない。装飾の前後関係・噴水の存在感・四端の材質接続・Lv1／3／6の変化・HUD下の景観を実画面で判定する必要がある。新画像のメモリ増とFPSも実機で測る。地表と配置を内部描画しただけで中央完成とは判定しない。

このクラウドは今回のEditor再起動でライセンス不足（終了198、`No valid Unity Editor license found`）、Android Build Support・Pixel USB経路なし。実行できる段階と阻害される段階を分けて記録する。

## Codex／AI側から見た次の改善候補

今回はなし。中央の実画面合格を先に確定する。
