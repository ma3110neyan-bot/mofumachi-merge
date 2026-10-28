# ノア2.5D再試作の単体確認

旧試作はユーザー不合格として保存。今回の製品変更はノアの描画3ファイルだけで、原画PNG・他5名・街・北境界・噴水・図鑑・BGM・Save／依頼／納品／報酬・Mergeは変更していない。

- 既存原画のUVを保ち、薄い凸面と左右の向き、顔／身体のニュートラルな陰影を追加。
- 足の実際の座標をメッシュへ登録し、柔らかな接地影・歩行・停止呼吸・往復手振りを調整。
- 比較Beforeは不合格だった1d11629の描画を同じ時計で再現。旧2Dへ差し替えず、元PNGも上書きしない。比較Beforeの名札だけAfterの自然な表示状態へ揃え、異なる自然Beforeは別保存。

**ソース検査済み・新Unity実画像／新APK／Pixel未確認。** 今回のsource/API検査は見た目の合格ではない。全件EditMode／PlayModeは実行せず、ノアが実画像で承認候補になるまで保留する。他5名への展開も禁止のまま。

GitHub Desktopで既存feature `feat/daily-world-pass-20261016` をPullし、Unityエディターを閉じてから次だけ実行する。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\capture-noah-reprototype.ps1"
```

起動するのはノア専用のUnity PlayModeメソッド1件のみ。北の再検査／全件テスト／APK作成／端末インストール／初期化を行わない。独自のUnity設置場所なら同スクリプトへ `-UnityEditorPath` を渡せる。

成功後、新しいHTMLを既定ブラウザーで開く。6状態のBefore／After、静止→左歩行→停止→右方向転換／歩行→ノア自身の会話の連続比較を確認する。144コマは1/30秒の実AdvanceFrameを2stepごとに記録した9.6秒のJPEG標本で、終端でループする。比較対のPNGは無損失、標本間の省略／CUTはない。実時間動画・Pixel FPSの証拠とは区別する。

噴水前後の2画面は足Yによる表示確認用の位置へ置いたもので、歩行経路の証拠ではない。会話パネルの既存配置は変えていないため、帽子／耳への重なりは従来の制約として残る。正式な目・顔の印象とリアクションを確認する。

出力された `Builds\Diagnostics\noah-reprototype-日時-ID-*.zip` を全パート共有する。各パートは原本合計24MiB以下で、ノアの原PNG／JPEG・JSON・HTML・XML／logだけ。失敗時も同じZIPで切り分け、全件QAを先に再実行しない。

独立レビューで見つかったカメラの奥行き不足、到着標本の誤判定、カスタムUnityパスの未使用は修正した。撮影のCanvas距離を30へ取り、実際のノア全頂点がnear／far内にあることを描画前に検査する。正射影のXYや街本体は変えない。関連検査・修正記録は `docs/verification/noah-reprototype-2026-10-28/` に保存する。PixelのFPS・発熱・メモリ・タップ遅延は未測定。
