# 正式Visual Lock／中央マスター工程 — 2026-10-21

**中央の実装候補。Unity実画面・APK・Pixelは未確認／未生成。中央未合格・ベース版未完成。**

ユーザーが提示済み画像を正式Visual Lockとして承認した。中央の実描画合格まで他地区・Mergeの新たな展開を始めない。今回のソースは中央工程に限定し、既存の未採用候補をベース版の合格へ読み替えない。

## 制作した候補

中央の地表を、非反復の石畳・花の縁・低い草花・座席まわりを一体で描いた素材に置き換える。正式な建物・木・花壇・ベンチ・灯りとキャラは独立した奥行き描画を保つ。地表へキャラ・UIを焼き込まない。

中央の下側の庭はLv1から小花壇を持ち、Lv3／6で同じ場所の景観を置き換える。基礎は初期から見え、描画と衝突で共有。既存の四方向の幅・噴水左右・滞在点・NPC経路を保持する。新しい地表と石畳の上に古い芝生の縁が残らないよう、噴水も透明な独立素材にした。

中央の北側の見切れを避けるため、地表画は中央の見えるアプローチまで固定ワールド座標で延ばす。北の目的地・建物・内容を作り直したものではない。各地区の完成／接続合格は後工程のまま。

## 正式基準と復旧

- `References/VisualLock/approved-design-2026-10-21.json`：承認PNG／JPEGのSHA・中央とMergeの領域・8判定軸を固定。
- `References/VisualLock/stage-acceptance-2026-10-21.json`：中央は未合格。残る工程は未着手。自動合格・自動採用しない。
- 従来の153凍結入力は保持。正式6名・ロゴ・Core・保存・5×6・図鑑・日次・TOP・選曲を変更していない。
- 作業前の復旧タグ `checkpoint/pre-central-visual-lock-20261021` は7085af7。これは未採用候補の退避点であり、良い見た目を実証した版という意味ではない。
- 実UnityのBeforeは製品bd3f357／実行20261021-113845-fbcafb0d。前版の成功を今回へ引き継がない。

## 工程の状態

|工程|状態|
|---|---|
|中央マスター制作|素材・配置・描画・初期／成長の実装候補|
|中央の実Unity比較／合格|**未確認・未合格**|
|西／東／北／南の新たな展開|**中央合格待ち、未着手**|
|5地区接続の仕上げ|未着手|
|Mergeの正式Visual Lock化|未着手。現在の5×6機能を保持|
|UI整理／基本ループ回帰／Unity全件|本工程では未実施|
|QA APK／Pixel受入|**未生成／未確認**|

ソース上の経路・基礎・段階置換を含む純C#表示／経路245件、Core125件、PowerShell5スイート、実DLL参照6asmdefコンパイルが成功（警告／エラー0）。PowerShellの合成画像・XML・mock ADBはゲート検査であり、実Unity／Pixel成功ではない。Windowsでの比較JPEG出力は未実行。これらは見た目・FPS・Pixel操作を合格にするものではない。

## 中央だけの実Unity確認

GitHub Desktopで現在のfeatureブランチをPull後、ライセンスのあるWindowsのUnityで実行する。

```powershell
& { $ErrorActionPreference="Stop"; Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"; powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -CentralReview }
```

`Builds/CentralReview/<実行ID>/central-master-review.zip` を生成する。既存の隔離撮影と同じ方法でLv1／3／6の中央実PNGを取得し、原本Before・正式Visual Lock・候補を並べた `central-before-lock-candidate.jpg`、source／fingerprint、未合格JSONをまとめる。**APKは生成しない。**

Before用 `Builds/Android/20261021-113845-fbcafb0d` は残す。ない場合は比較未完了として記録し、中央を採用しない。生成素材や内部CPU配置図を実Unity Afterへ混ぜない。

中央が未合格なら通常のAPK生成は停止する。合格フラグに加え、実画像3枚・固定Before3枚・承認画像・比較JPEGのSHA、撮影時の中央入力digestとの一致が必要。中央の共有描画／経路を変更したら再撮影する。後続の地区だけのコミットでHEADが変わったことを理由に合格を失効させるのではなく、中央の依存入力を検査する。中央合格だけでもAPKへは進めず、西から基本ループまでの工程記録を順序どおり確認し、その後に同一ソースのUnity全件を実行する。中央合格後に、西→東→北→南→接続→Merge→UI→基本ループ→Unity全件→QA APK→Pixelを順に進める。

検査記録：[central-master-2026-10-21.json](verification/central-master-2026-10-21.json)。

## 未解決事項

**実装済み・実機未確認。中央の採用は保留。** 新しい中央のUnity実描画がまだない。装飾の前後関係・噴水の存在感・四端の材質接続・Lv1／3／6の変化・HUD下の景観を実画面で判定する必要がある。新画像のメモリ増とFPSも実機で測る。地表と配置を内部描画しただけで中央完成とは判定しない。

このクラウドは今回のEditor再起動でライセンス不足（終了198、`No valid Unity Editor license found`）、Android Build Support・Pixel USB経路なし。実行できる段階と阻害される段階を分けて記録する。

## Codex／AI側から見た次の改善候補

今回はなし。中央の実画面合格を先に確定する。
