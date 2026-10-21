# 正式Visual Lock／中央マスター工程 — 2026-10-21

**最新2fcab4aの実Unity画像もVisual Lock未達で不採用。中央の構図・成長・住人を統合し直したソース候補は実装済み・実機未確認。中央未合格・ベース版未完成。**

ユーザーが提示済み画像を正式Visual Lockとして承認した。中央の実描画合格まで他地区・Mergeの新たな展開を始めない。今回のソースは中央工程に限定し、既存の未採用候補をベース版の合格へ読み替えない。

## 実画面の判定と中央の構図修正

[初回b25658eの比較](../References/Review/central-master-2026-10-21/central-before-lock-candidate.jpg) に続き、[2fcab4aの実Unity比較](../References/Review/central-master-2026-10-21-r2/central-before-lock-candidate.jpg) を受領した。画像はアップロード原本のまま。最新実行 `20261021-214913-a0b7b926` はEditor6000.6.4f1、テスト3／3、720×1280の中央3枚、固定Before3組、承認画像・SHA・中央入力digestの一致を確認。[最新判定記録](../References/Review/central-master-2026-10-21-r2/native-review.json)。

**中央は引き続き不採用。** 噴水・初期の友達・見える成長花壇は改善し、実コントローラの噴水ループも成功した。ただし、上側の道の空白、Lv6の花壇の積層によるベンチの隠れ、ルルと名札の装飾への重なりが残った。作者と独立した実画像レビューの判定を一致させ、中央の採用を止めた。旧Beforeより良いことだけで正式Visual Lock合格にはしない。

正式原画・高密度な地表・改善した噴水を保持し、中央の構図をまとめて修正したソース候補を用意した。

- 上側の道に既存の掲示板と灯り、小花壇の基礎を配置。北への中央通路と噴水左右の経路は開ける。
- Lv6の既存バンティングを画面内へ移す。柱の位置にはLv1から小花壇を持ち、成長で見えない障害物が生じない。
- 見えない下端の花壇は完成した初期の低い状態へ戻す。見える手前の花壇は低く置き換え、Lv2のベンチ全体が露出する高さにする。旧Lv3／6の同位置の花壇も置き換え、積まない。
- 中央の友達は、既存「広場」活動の石畳へ配置。ベンチや植栽への重なりを避け、名札の前を空ける。正式原画・表示機構・アニメは維持。
- 成長の配置も中央入力digestへ含めた。配置を変えた後に古い画像の合格記録を使い回せない。

この候補は **実装済み・実機未確認**。内部CPU配置図は非Unityと明示した調査用で、Afterや採用証拠にはしない。新候補の実Unity見た目・名札・装飾の重なりは再撮影待ち。中央合格前に他地区・Mergeへ進まない。

## 正式基準と復旧

- `References/VisualLock/approved-design-2026-10-21.json`：承認PNG／JPEGのSHA・中央とMergeの領域・8判定軸を固定。
- `References/VisualLock/stage-acceptance-2026-10-21.json`：中央は未合格。残る工程は未着手。自動合格・自動採用しない。
- 従来の153凍結入力は保持。正式6名・ロゴ・Core・保存・5×6・図鑑・日次・TOP・選曲を変更していない。
- 作業前の復旧タグ `checkpoint/pre-central-visual-lock-20261021` は7085af7。これは未採用候補の退避点であり、良い見た目を実証した版という意味ではない。
- 実UnityのBeforeは製品bd3f357／実行20261021-113845-fbcafb0d。前版の成功を今回へ引き継がない。

## 工程の状態

|工程|状態|
|---|---|
|中央マスター制作|実画面2fcab4aを不採用、構図・成長・住人を統合し直した候補を実装|
|中央の実Unity比較／合格|2fcab4aは実画面・自動歩行確認済み／見た目不採用。新候補は未確認／未合格|
|西／東／北／南の新たな展開|**中央合格待ち、未着手**|
|5地区接続の仕上げ|未着手|
|Mergeの正式Visual Lock化|未着手。現在の5×6機能を保持|
|UI整理／基本ループ回帰／Unity全件|本工程では未実施|
|QA APK／Pixel受入|**未生成／未確認**|

今回の純C#表示／経路253件、正式Lock／段階ゲート・成長入力の改変拒否、Androidスクリプト、撮影識別検査、実DLL参照6asmdefコンパイルが成功（警告／エラー0）。構図の新規4検査、石畳の初期居場所、成長入力改変の各失敗を先に確認してから修正。Core・正式原画・既存テクスチャ・成長／報酬計算は変更なし。

受領した2fcab4aの実コントローラ歩行は確認済み。新候補にはベンチ露出・名札・祝祭飾りの画面内配置を確かめるネイティブ検査を加えたが、DLLコンパイルのみで未実行。旧ネイティブ成功を引き継がず、Pixel操作やFPSも合格としない。

## 中央だけの実Unity確認

GitHub Desktopで現在のfeatureブランチをPull後、ライセンスのあるWindowsのUnityで実行する。

```powershell
& { $ErrorActionPreference="Stop"; Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"; powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -CentralReview }
```

`Builds/CentralReview/<実行ID>/central-master-review.zip` を生成する。既存の隔離撮影と同じ方法でLv1／3／6の中央実PNGを取得し、原本Before・正式Visual Lock・候補を並べた `central-before-lock-candidate.jpg`、source／fingerprint、未合格JSONをまとめる。**APKは生成しない。**

Before用 `Builds/Android/20261021-113845-fbcafb0d` は残す。ない場合は比較未完了として記録し、中央を採用しない。生成素材や内部CPU配置図を実Unity Afterへ混ぜない。

中央が未合格なら通常のAPK生成は停止する。合格フラグに加え、実画像3枚・固定Before3枚・承認画像・比較JPEGのSHA、撮影時の中央入力digestとの一致が必要。中央の共有描画／経路を変更したら再撮影する。後続の地区だけのコミットでHEADが変わったことを理由に合格を失効させるのではなく、中央の依存入力を検査する。中央合格だけでもAPKへは進めず、西から基本ループまでの工程記録を順序どおり確認し、その後に同一ソースのUnity全件を実行する。中央合格後に、西→東→北→南→接続→Merge→UI→基本ループ→Unity全件→QA APK→Pixelを順に進める。

検査記録：[初回候補](verification/central-master-2026-10-21.json)／[噴水等の再調整](verification/central-native-followthrough-2026-10-21.json)／[今回の構図修正](verification/central-composition-2026-10-21.json)。

前回の中央レビュー `Builds/CentralReview/20261021-202704-1d9ab213` と `Builds/CentralReview/20261021-214913-a0b7b926` も残す。画像は不採用判定の証拠であり、新候補のAfterではない。

## 未解決事項

**実装済み・実機未確認。中央の採用は保留。** 再調整後の中央のUnity実描画がまだない。装飾の前後関係・噴水の存在感・四端の材質接続・Lv1／3／6の変化・HUD下の景観を実画面で判定する必要がある。新画像のメモリ増とFPSも実機で測る。地表と配置を内部描画しただけで中央完成とは判定しない。

このクラウドは今回のEditor再起動でライセンス不足（終了198、`No valid Unity Editor license found`）、Android Build Support・Pixel USB経路なし。実行できる段階と阻害される段階を分けて記録する。

## Codex／AI側から見た次の改善候補

今回はなし。中央の実画面合格を先に確定する。
