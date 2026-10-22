# 北の依頼小屋 — 実装済み・Unity実画面／Pixel未確認

最上位手順は [5エリア品質固定と画像承認](town-quality-checkpoints-2026-10-22.md)。中央・西・東Candidateを維持し、北候補→南候補→5地区統合候補の各段階で画像に対するユーザー承認を得てから次へ進む。北の実Unity結果もまだ未取得。

中央・西・東の修正版97aac99／20261022-193224-39bef67cはUnity工程を通過し、フィオの名前もLv1／3／6すべてで読めることを確認した。原本と判定は `References/Review/east-florist-2026-10-22-accepted/native-review.json` に保存。今回の北はその後の共有描画／歩行入力を変更するため、前版の合格を新版へ引き継がない。

## 実装した範囲

- 既存の依頼小屋・掲示板・入口・依頼画面を維持し、側面ベンチ・街灯・段階置換の低い花壇を配置。
- 元の桜を同じ場所で低く表示し、休憩場所の名前を隠さない余地を確保。Lv6で掲示板を隠していた花の重なりも抑える。
- 小屋の両側、休憩場所、入口、中央までの複数経路を維持。装飾の基礎と通行判定は同じ定義を使う。
- 入口案内は近接／移動／到着時に表示。歩いて到着したあと、ドアの再タップで既存の依頼画面へ入る。
- 地表は西と同じ既存Texture2Dをワールド上へ投影し、建物・キャラ・花を別々に描画する。新規大型テクスチャなし。FPSやPixel負荷の改善は未計測。

正式6名、TOP／ロゴ、図鑑の基本構造、BGM選択、保存、依頼進行、納品／報酬、5×6ルール、街Lv条件／速度には変更なし。成長間隔・装飾の寿命・成長説明ページは指示どおり後工程に残す。

## 現時点の確認

ローカル純粋テスト276件、実Unity DLL参照6asmdefのコンパイル、PowerShell8組が通過。北の未実装時に経路／表示／案内の6件が失敗することを確認してから実装した。証拠照合は153凍結入力・承認画像・前版15PNGの保持を確認し、現在版の未確認状態ではAPK工程を拒否する。

独立ソースレビューを依頼したがサービスの利用上限で実行されなかった。代わりに作者が差分を再点検した。独立レビュー済みとは扱わない。実画面の密度・遮蔽・連続移動、Pixelの操作／FPS／メモリは未確認。

**北：設計済み → 実装済み。Unity確認済み／APK生成済み／Pixel 3a確認済みには達していない。** クラウドのUnityはライセンス不足（直近確認：終了198）、AndroidモジュールとPixel接続経路もないため、次の実Unity確認はライセンスのあるWindows環境で行う。

## Windowsでの実Unity確認

GitHub Desktopで `feat/daily-world-pass-20261016` を選択してPullし、Unity Editorを閉じて実行する。コピー用は [north-review-steps.txt](north-review-steps.txt)。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -NorthReview
```

親PowerShellでbuildスクリプトをドットソースしない。実行ポリシーは子プロセスだけに適用し、永続設定は変更しない。古いスクリプトは新しい `-NorthReview` を受け付けないため、旧版で撮影を進めない。

成功時はネイティブ12件（中央2＋西3＋東3＋北3＋撮影1）、実PNG21枚（中央3＋西6＋東6＋北6）、同条件Before12組。北のLv1／3／6・接続・徒歩・ドア到着を取得し、中央・西・東も同じソースで再確認する。

開いた `Builds/NorthReview/<run>/` から、次の同一runの4ZIPを送付する。

- `central-master-review.zip`
- `west-cafe-review.zip`
- `east-florist-review.zip`
- `north-quest-review.zip`

比較資料は各ZIP内のJPEG（`.jpg`）。北はLv1／3／6を、Before／Candidate／中央のVisual Lockデザイン参照の3列で示す。参照は北の実Unity画面ではない。チャットへ画像を貼る必要はない。各ZIPは32MiB以下を検査する。採用JSONのfalse／nullは撮影時の未採用原本として保持し、実画面確認後の判定を別に保存する。同じXMLが4ZIPにあるだけなので、12件を48件とは数えない。

失敗時は当該runの `central-playmode.xml` と `central-playmode.log` を送付する。北を含む同じ12件の結果だが、互換性のためファイル名は従来どおり。**APKは生成しない。** 北の新実画面の確認と、北候補画像へのユーザー承認の両方が揃うまでは南へ進めない。

## 未解決と次の候補

中央・西・東の既存持越しは、接続部の花による主人公の隠れ、舗装の反復／境界、入口案内とひさしの部分的重なり。接続／UI工程で対処する。北／南／接続／Merge／UI／基本ループ／全件Unity／QA APK／Pixelの最終合格は未了。

Codex／AI側から見た次の改善候補：接続時の主人公の見失いを減らす（リリース前・難易度中）、入口案内と建物の重なりを整える（リリース前・難易度低）。いずれも予定する接続／UI工程内の候補で、新機能ではない。
