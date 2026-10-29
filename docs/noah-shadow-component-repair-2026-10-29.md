# ノア再試作の影コンポーネント修正

最新の単体run `noah-reprototype-20261029-120718-65fcb65c` は、source `5083efa` の実Unityで0/1、failed1／skip0。比較画像の生成前に失敗した。XMLにある235は発見されたメソッド数で、実行はノア専用1件だけ。

原因は新しい `NoahGroundShadowGraphic` の `CanvasRenderer` 欠落。直接継承している `MaskableGraphic` の依存部品は `RectTransform` のみで、ノード生成もそれだけを追加していた。Home再入場で旧画面を無効化すると、マスク解除が存在しないrendererへアクセスし `MissingComponentException`。終了処理の例外も同じ原因。ライセンスやC#コンパイルの失敗ではない。

修正は影クラスの `RequireComponent(typeof(CanvasRenderer))` 1行のみ。原画・陰影・メッシュ・動作・街・北境界・噴水・他5名・保存・納品処理は無変更。例外の無視／期待ログ化や比較許容範囲の拡大は行わない。既存のノア単体撮影と表示テストにはrenderer実在の確認を追加した。

実Unity受領の失敗と、修正前の実Unity API assemblyを使った依存宣言検査の失敗を記録した。修正後は同検査が成功し、Presentation／PlayModeTestsのAPIコンパイルと固定入力の検査が成功。これはクラウド上のソース／メタデータ検査であり、Unityの生成・描画・画面再入場の成功はまだ確認していない。無関係な全件テストは実行していない。

原本3ファイルと診断は `References/Review/noah-reprototype-2026-10-29-120718/`。再添付された205828と222202の旧3ZIPは、既受領記録のSHA256とすべて一致する履歴で、新候補の画像・成功証拠に使わない。

GitHub Desktopで既存 `feat/daily-world-pass-20261016` をPullし、Unityを閉じ、次の**単体撮影だけ**を再実行する。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\capture-noah-reprototype.ps1"
```

成功時は新しいBefore／Afterと連続動作HTMLが開く。表示された新runの `noah-reprototype-*-*.zip` を全パート共有する。失敗時も同じZIPで次の原因を確認し、全件QAを先に繰り返さない。新画像／APK／Pixel／見た目の合格は未確認。6名展開は引き続き禁止。
