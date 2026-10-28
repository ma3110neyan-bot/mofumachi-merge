> 最新：名札の比較条件を修正。全件QAを繰り返す前に [ノア単体再撮影](noah-name-context-fix-2026-10-28.md) を行う。

> 最新：受領した全件QAは233/234で比較撮影1件が停止。全件を繰り返さず、[単体診断](noah-capture-diagnostic-2026-10-28.md)を先に行う。下記の全件QAは切り分け後の受入用。

# ノア1名 2.5D品質候補 — 実画面受入手順

北境界はユーザーにより合格・固定。北・街・噴水・図鑑・BGM・原画・他5名・保存／依頼／納品／報酬・Mergeは変更していない。今回はノア1名だけの候補で、6名展開は未承認。

## 今回の候補

- 接地影：既存2枚を足元へ合わせ、尾の大きさに引っ張られない幅と濃さへ調整。
- 歩行：交互の足上げと重心を読みやすくし、開始・停止は既存の速度補間でつなぐ。
- 向き直り：顔・帽子・尾が重なる原画を守るため、頭の帯だけを横へずらす処理を廃止。接地した足を軸に原画全体を軽く傾け、わずかな奥行き差を付ける。鏡像化や原画の再描画はしない。
- 表情／挨拶：既存の正式なsmileの左右の目を使う短い笑顔、通常のまばたき、ノアだけ約1秒の小さな手振り。
- Sorting：既存のworld足元Yで木・ベンチ・噴水・建物・花壇と前後を判断。描画の上下動でSortingを変えない。

実装候補・関連数値検証とコードレビュー済み。**新Unity実画面／新QA APK／Pixel3aは未確認。現行2Dより明確に良いという採用判定はまだ出していない。**

## 全件QAはこの候補で一度

GitHub Desktopで `feat/daily-world-pass-20261016` をPullしてから、Windows PowerShellで実行する。mainへ切り替えない。作業途中の不具合ごとに全件を回し直す手順ではない。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
```

既存の全EditMode144／PlayMode234と同版撮影・街QA APK・成長QA APKを同じrunで作る。ノア専用は4件のままSorting確認を追加し、撮影は静止／歩行／方向転換／停止／会話／リアクションの6対に増やした。全景PNGは従来250から256になる想定で、実行結果の件数・失敗0／skip0を確認する。

生成した**そのrun**の `five-plaza-review-*.zip` 一式を共有する。以前の `20261028-101255-ecac0943` は今回の強化候補ではない。受領済み378成功や旧APKのSHAを流用しない。

## Before／Afterと連続動作

新runの `screenshots\north-noah-before-after.html` から6枚の `37-noah-*-before-left-after-right.jpg` をブラウザーで確認する。JPEGは実Unity PNGの比較用派生画像で、実画面が生成される前にAfterを合成しない。

Beforeは**同じ瞬間にlegacy描画へ切り替えた再現**であり、過去APKの実行画像ではない。挨拶のremaining clockは同値だが、過去の挨拶は0.65秒、候補は1秒なので、旧版と同じ経過時間の動作比較とは言わない。実際の旧版原本は `References/Review/north-noah-2026-10-28-candidate/native/37-noah-*-after.png` に別途保持する。

48枚の実Unity切抜きのうち0〜39は静止→開始→歩行→反転→停止の4秒連続固定刻み標本。40〜47は後の実際の相互挨拶で、HTMLにCUTと実シミュレーション時刻差を表示する。これは実時間動画や端末FPSではない。会話UIの重なりも隠さず見る。

## Pixel3a

新候補の画像・APK／build-info／source／SHA一致を確認後、既存 `install-pixel3a.ps1` の保存維持install-rで更新する。対象は `com.mofumachi.merge.qa.townlife`、serial `99RAY1BELH`。Fresh／uninstall／データ消去はしない。インストールコマンドのAPKパスとExpectedSha256は新runの実測値を使用する。

ノアの静止・左右歩行・歩き始め・停止・向き直り・会話・挨拶途中の移動開始と、木／ベンチ／噴水前後／建物／花壇での描画を実操作で確認する。操作遅延・カクつき・クラッシュ、実Unity SurfaceViewのFPS、開始／終了時PSS・端末温度を同じ場面・同じ条件で記録する。現在これらの新候補の測定値はない。以前の計測ヘルパーのSurfaceView未特定エラーをFPS成功と扱わず、正しい前景アプリと実レイヤーを特定してから測る。

現行より明確に良いとユーザーが承認するまで6名展開しない。悪化していれば候補を採用しない。
