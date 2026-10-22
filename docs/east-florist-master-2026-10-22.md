# 東花屋マスター — 表示修正済み・修正版Unity確認待ち

中央・西のUnity合格475671f／20261022-182106-d5e104a3を経て、承認済みVisual Lockの品質を既存の東花屋へ展開する。新しい街構成やショップ機能、原画の作り替えではない。見た目の採否は次の実Unity比較で判断する。

- 正式花屋を残し、店先に売り台・ベンチ・灯り・低い花壇を配置。中央への接続と花屋の左右を歩ける面を維持。
- 西と同じ地表Texture2Dを、東のワールド座標で反転投影。建物／花／キャラは別々に奥行き順で描く。新規大型テクスチャは追加しない。負荷／FPSはPixel未計測。
- 初期の花売り台は既存Lv2の花売り台へ置換。既存成長・保存・報酬・依頼・5×6には変更なし。Lv差／成長間隔／説明ページは指定どおり後工程。
- 花屋の案内を近接・移動・到着時に表示。遠方では店の原画を隠さない。徒歩到着から再タップして既存花屋画面へ入る。

## 検証状態

受領版2848782／実行20261022-190004-5597dca1は実Unity9/9、実PNG15枚・固定Before9組・承認画像と比較JPEGの同じ版／SHAを検証。中央PASS、西PASS（西6枚は前回合格と完全同一）。[判定と原本](../References/Review/east-florist-2026-10-22/native-review.json) を保存した。

東の店先・移動／再タップ入場は良いが、Lv3／Lv6でフィオの名前を前景ベンチが隠す。Beforeで読めていた情報が失われたため東はHOLD。実テストの成功を採用理由にせず、北への展開を止めた。

成長ベンチ `(1.51,-.76)` が休憩地点 `(1.48,-.60)` の名札にかかることを純粋テストで再現。初期ベンチと成長ベンチを同じ側面の場所へ配置し、Lv3で置換する修正を実施した。ベンチの原画・レベル条件／速度・保存／報酬／依頼を変更せず、初期の席も残す。名札を消す対応はしない。実Unityの撮影にも、名札と前景ベンチの描画範囲を照合する検査を追加した。

修正版の純粋C#回帰270件、実Unity DLLを参照する6 asmdefコンパイル、7 PowerShell検証は通過。修正版のUnity実PNGはまだ0枚。クラウドEditorはライセンス不足のため、下記のWindows実行で確認する。APK未生成、Pixel未確認。

共有描画入力を変更したため中央・西の合格も履歴として残し、現在版の採用フラグは閉じた。同じ修正版で3地区を再検査し、東が合格するまで北へ展開しない。

## Windowsでの一括確認

GitHub Desktopで `feat/daily-world-pass-20261016` を選択して **Pull**。Unity Editorを閉じてPowerShellで実行する。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
if (-not (Select-String -LiteralPath ".\Assets\Mofumachi\Tests\PlayMode\ExperienceCaptureTests.cs" -SimpleMatch "AssertNameClearOfBenches" -Quiet)) { throw "GitHub Desktopで修正版をPullしてください。" }
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -EastReview
```

親PowerShellでbuildスクリプトをドットソースしない。上記は実行ポリシーを子プロセスに限定する。永続設定は変更しない。

成功時はネイティブ9件、中央3＋西6＋東6の15枚。東のLv1／3／6・接続・徒歩・ドア到着と、中央／西の再確認を同じrunで取得する。固定Beforeは従来の `20261021-113845-fbcafb0d` の実PNG9組をSHA照合。正式Visual Lockはデザイン参考、実Unity画像とは区別する。到着後の待機と再タップによる入場はLv1／Lv3の実イベントハンドラで検査する。

開いた `Builds/EastReview/<run>/` の次の3ZIPを送付する。画像をチャットへ貼る必要はない。

- `central-master-review.zip`
- `west-cafe-review.zip`
- `east-florist-review.zip`

各ZIPは32MiB上限。自動生成の採用JSONは未採用のままとし、実画像の密度／景観／道／到着案内を確認して別途判定する。ネイティブXMLは3ZIPで共通、合計9件を27件と数えない。APKは作らない。

失敗時は当該runの `central-playmode.xml` と `central-playmode.log` を送付する。互換性のためファイル名は中央のままだが、東を含む同じ9件の結果である。古いrunを混ぜない。

## 未解決

修正版の名札の実Unity可読性と3地区のNo Regression再判定、Pixelの操作／FPS／メモリ。従来の接続部の花による主人公の隠れ、舗装反復、案内とひさしの部分的重なりは接続／UI工程に持ち越す。北／南／接続／Merge／UI／基本ループ／全件Unity／QA APK／Pixelは未完了。
