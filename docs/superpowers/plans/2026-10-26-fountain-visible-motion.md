# 噴水の可視アニメーションと短時間QA

ユーザーの2026-10-26「未達」指示・「良い方向への改善作業」を仕様とする。既存の承認方向内で自分が実装する。噴水採用はユーザーが新版で動きを確認するまで保留。

## 原因調査

- 前版の通常流は固定座標の帯の明度変化が主で、明るい原画上では流れを認識しづらい。静止画差分・頂点数では可視アニメーションを証明できない。
- 前版の時計は180秒周期かつ可視／操作可能時だけ進み、さらに各フレームを0.1秒へ切り捨てる。待った実時間と一致しない。実機でショー未発生だった個別原因は旧端末ログなしでは断定しない。
- 撮影はAdvanceWaterを手動呼出していた。実Update経路が60秒以内に発生することを検査していなかった。

## 実装方針

原画・広場構造・装飾・歩行・保存は維持。既存の単一UGUIメッシュ196quadを使い、動く白い流水、青い水の縁、着水のしぶき、太い波紋、明滅する反射を描く。ショーは左右と中央の曲線噴流を動かす。Particle／ライト／RenderTextureの製品追加なし。負荷成立はPixelで測定するまで未確認。軽量だから実測合格したとは言わない。

街QAとEditorは45秒周期（通常36秒→前兆2秒→ショー6秒→復帰1秒）、初回のショー開始は38秒。非Development版は180秒を維持。ショー時計に低FPSでの時間切捨てをしない。Home内のカメラ移動で待ち時間を止めず、メニュー／別画面／アプリ休止では停止。再開時に休止時間を加算しない。

## Task 1: 時計・可視流水

- CPUで流水の座標移動、低FPS時の経過時間、QA周期のショー発生・復帰、休止をRED→GREEN。
- WaterLayout／WaterGraphicだけを修正。FountainGraphicの拡大・親のY深度・既存街は不変。
- Expected: FountainWaterTests全件成功、通常の芯が実際に落下、30Hz／196quad以内、管理割当0。

## Task 2: 実Updateの連続確認

- 新UnityTestで通常→ショー→復帰を60実秒、Update経由で観測。AdvanceWater／時計書換え／強制ショーを使わない。
- 実720x1280全景3枚と、実描画から切り出した4fpsの噴水連続PNG／時刻・stage・phase・SHA・切出矩形／HTMLプレイヤーを保存。
- 既存の同一瞬間OFF/ON・前後・成長・全件検査は維持。QAゲートは連続証拠の欠落／不発生／停止フレームを拒否。
- Expected: Unity実行でのみ合格。クラウドのライセンス制約は未確認のまま、CPU/APIをUnity実画面の代わりにしない。

## Task 3: 品質固定と引継ぎ

- 原354／固定街／旧Candidate／実Beforeを改変せず、新候補scope・digestを別記録。旧native成功を新候補へ転記しない。
- 全CPU、6asmdefのAPIコンパイル、PS検査、実行原本のゲート、同feature commit/push。
- WindowsのTownQaで新連続原本を受領後、同版APKでPixelの通常水／38秒以内ショー／キャラ／FPS／熱をユーザーが確認する。
- 2.5D／Merge／以降／mainは未着手。

## Review Focus

1. QAとReleaseの周期選択、低FPSで時間が失われないこと。
2. 再構築・Pause・非表示・メニューで時計の停止／再開が整合すること。
3. 強制Advanceの静止画だけで実Updateの証明を代用しないこと。
4. 連続切出し原本の矩形／SHA／時刻／stageが一致し、他のポーズ差分を新水へ帰属しないこと。
5. 景観・Y深度・保存・歩行範囲・頂点予算を維持し、Pixel性能は未測定とすること。

## Ledger

Start6bc570e clean existing feature checkout. Existing session keeps same branch for Windows QA; no new worktree. Native inline execution per user's direct instruction and AGENTS approved-direction rule.
Pre-flight: Task1 exposes actual QA cycle/clock to Task2; Task2 timeline schema becomes mandatory artifact consumed by Task3 gate. Legacy manual capture remains causal still evidence only.
Ruling: use existing mesh, no alternative heavy Particle architecture — preserves approved field and allocation budget; if perceived motion/load fails Pixel, fountain remains unadopted and only fountain is revised.

Task1: complete CPU RED7/GREEN16、全CPU351/351 failed0skip0（8m54s）。通常の芯の下降と4/8/15/30/60FPSの38秒開始を実際のproductionクラスで検査。2runtimeファイルのみ。
Task2: 実Update60秒・4fps実描画crop・実全景3枚・再生HTMLを実装。Nativeはライセンス制約で未実行。連続証拠ゲートのsynthetic metadata fixtureでRED（関数なし）→GREEN（強制時刻/不発生/停止/改変を拒否）。これは実Unity画像の合格証拠ではない。

Task3: ソース／検査準備完了。CPU351＋Core125、API6（0警告エラー）、PS15本、422ファイルCRLF、18bindingと原354／固定街／歴史digestを確認。Native棚卸し144Edit/226Playであり実成功ではない。検証原本はdocs/verification/fountain-motion-2026-10-26/。
Final review: 独立1回、Critical0／Important1／Minor1。I1は初回Portrait描画前に切出位置を計算する問題。初期横長レイアウト→実Portrait→切出しへ修正し各sampleの実頂点包含を検査。Native regressionは未実行、API成功だけを実画面成功にしない。
Final: Ruling: M1（位相固定／初回ピーク時刻の矛盾を許容）は今回の「動いて見えなかった」再発証拠に直結するためImportantへ格上げし同じ1回のfix passで修正 — negative metadata fixture実RED→GREEN、PS全15成功 — 実画像の知覚的な動きはなおNative/Pixelで確認。
Final: Ruling: actualUnity/pixels/aesthetics/smoothness declined — 新370件＋実Update60秒／新165全景＋連続原本を受領するまで保留 — 確認に失敗すれば噴水未採用のまま。
Final: Ruling: PixelFPS/thermal/600秒/audio/depth/update-save declined — 同新版の実機測定とユーザー確認が必要、頂点予算で性能を代用しない — 実機不成立なら軽量代案を先に承認取得。
Final: Ruling: old missing-show incident/root cause declined — 待機仕様の問題と動きの弱さはコードで確認したが旧端末個別原因は推定しない — 不発生が続けば新stageログと連続原本で診断。
Final: Ruling: old native evidence declined — 前版369／画像は新候補へ流用しない — 新版QAが済むまで採用しない。
Final: Ruling: APKbinary/release runtime declined — 新APKはWindowsQA後の同runSHA照合、release180はソース分岐確認のみ — 違うAPKやreleaseの実機成功を主張しない。
Final: Ruling: OSfocus-only overlay declined — 既存Home/modal/appPause条件を維持し通知操作は実機検証へ — focusだけの中断挙動が不自然なら噴水内の局所修正。
Final: Ruling: final adoption declined — 新版でユーザーが確認できた時だけ合格 — 2.5D／Merge／mainを開始しない。

Final source audit: Application/DebugによるQA判定はBind内だけで行い、MonoBehaviourのfield initializerでは純粋なdefault clockだけを作る。ownerなしでもBind時はQA45秒を選ぶ。CPU対象のLayout/Coreは351＋125成功後に無変更、Graphic/Nativeの最終API6を再確認。Core初回はsandboxのローカルsocket拒否で中断し、承認された再実行で125成功。
