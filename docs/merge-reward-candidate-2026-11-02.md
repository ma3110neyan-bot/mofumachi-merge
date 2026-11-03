# Merge ごほうびUI 実装候補

2026-11-03更新：ユーザーが現行af084ecのVisual Gateを合格と判定。表示は正式候補・fix only。以後の実行手順と未取得結果は[リリース候補確認](merge-reward-release-candidate-2026-11-03.md)を参照。以下は原本保存時点の履歴であり、現在のVisual承認を取り消すものではない。

状態：**実装済み・実機未確認。正式完了／採用は未判定。**

ユーザーのD画像を目視確認し、正式参照先を `References/Merge/merge_visual_lock_reward.png` に固定。ユーザー添付`20261102_merge_visual_lock_reward.zip`から原本PNGを再エンコードせず保存済み。941×1672、RGB、2,216,784 bytes。ZIP内と保存先はバイト一致。SHA256は`827825aac8943b32b29f143f1a98d6142e5cbbcce80de39b7f999a3374d6e6f3`。受領記録は参照先隣の`.reference.json`。別の画像を原本として置いていない。`a18e598`の製品実装は変更していない。

## 変更ファイル

製品側は以下のみ。

- `Assets/Mofumachi/Presentation/QuestMergeScreens.cs`：Mergeのみ。現在の依頼・依頼確認導線・装飾の接続。BuildQuestは元と同一。演出中の操作ロック維持。
- `Assets/Mofumachi/Presentation/MergeRewardPresentation.cs` / `MergeRewardView.cs` と各meta：実依頼／実Coinsからの表示、Mergeローカルのピンク／ミント／クリーム／金、現在の依頼、必要な生成ボタン、大きな依頼／納品ボタン、タスク帯。短い画面では同じ報酬情報を上部へ集約し、操作領域を確保。
- `Assets/Mofumachi/Presentation/MergePerspective.cs`：表示セルをDの長方形へ。5列×6行・順序・4間隔・48以上・平面を維持。
- `Assets/Mofumachi/Presentation/MergeCellSurfaceGraphic.cs`：空セルだけに淡い肉球・花・葉・光。
- `Assets/Mofumachi/Presentation/MergeSparkleGraphic.cs`：既存の成功時演出に限定した少数の花びら。入力ロック／演出時間は不変。

関連検証：`MergeRewardPresentationTests.cs`、`MergePerspectiveTests.cs`、Presentationテストcsproj、`MergeRewardUiTests.cs`、`MergeRewardCaptureTests.cs`とmeta。既存の`MergeInteractionTests.cs`と`ExperienceMergeTests.cs`は、新しい依頼確認／長方形セル／花の枠に対応する期待値だけ変更し、入力・48以上・30セル・公式画像・操作ロックの検査を維持。

手順／保護：`prepare-merge-reward-preview.ps1`、`merge-reward-scope.ps1`、その2組の合成テスト、`character-finish-scope.ps1`の限定接続、`References/VisualLock/merge-reward-ui-2026-11-02.json`、参照receipt／README、preflight／元SHA／計画／AGENTS／本報告。

Core、UIFlowController、ItemGraphic、公式ItemAtlas、フォント、キャラ、街、SaveData、依頼条件、納品・報酬計算は変更していない。元の26/552/53ファイル保護記録も変更していない。

## Before / After

Beforeの復元点：`checkpoint/merge-before-reward-20261102` = `af659256b481aae0697cea7ccfbf5531c6a62c08`。元ファイルSHAはpreflight記録に保存。

原本保存後に新しいUnity撮影を試行したが、クラウドのXサーバー不足で起動停止（終了1）。描画なしの起動診断でも`No valid Unity Editor license found`で終了198。新しいJPEG／テストXMLは0件。記録は`docs/verification/merge-reward-original-capture-2026-11-02.json`、原ログは記載のBuilds/Diagnostics配下。以前の画像を今回のAfterとして提出しない。Windowsの重点撮影では同じ隔離保存データを維持し、旧表示をEditor内で再構成したBeforeと、候補AfterをJPEGで11枚保存する。PlayerにはBefore切替を持たせない。通常2種の縦長比率、小さい320×536相当、パン依頼、複数条件、所持品だけで納品可能、満杯を含む。実ユーザーのセーブは使用しない。JPEGはUnityで全デコードし、保存状態・保存回数・画像SHAを記録する。

原本が配置済みなら`merge-reward.html`に原本・Before・Afterを並べる。未配置なら明記する。Visual Lockとの差分として確定しているのは、既存の公式アイテム／フォントを維持し、見本のジェムではなく実Coinsを表示する点。花や枠・全体の再現度の合否は、実描画と原本の比較が残っている。

## 検証済みと未実施

- Merge関連CLRテスト：86/86、失敗0、skip0。全件テストではない。
- Presentation／PlayModeTestsのUnity APIコンパイル：成功。Unityの実行・描画の成功を意味しない。
- Merge限定scope／手順の合成検査：成功。旧キャラ限定scopeの2組も成功。
- 実リポジトリの正式キャラ・旧Noah・凍結入力ガード：成功。BuildQuest不変とCore／原画の保護を確認。
- 独立レビュー：演出中の依頼ボタン有効化と旧表示テストの不整合を修正。再点検の重要指摘0件。
- Unity実描画：起動を試行したが失敗。利用可能なXサーバー／Unityライセンスがなく、新規描画0件。Androidビルドモジュールもない。
- 原本とのVisual Gate、今回の実Unity Merge重点確認、全件EditMode・PlayMode、QA APK：前段未達のため実施していない。a18e598時点の86件はCLRの計算検査であり、この後段の実Unity合格へ転用しない。
- Pixel 3a：未実施。USB実機はこの環境にない。表示崩れ・負荷・発熱・操作感は未判定。

## Windowsの重点撮影

Unity Editorを閉じ、次をPowerShellへまとめて貼り付ける。`-CaptureOnly`は同条件JPEG撮影の1メソッドだけで、Visual Gateより後のMerge重点テスト・全件テスト・APK・インストールを先行実行しない。終了時に結果フォルダが開く。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mergeGit = Find-SourceGit
    if ((& $mergeGit branch --show-current).Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチが異なるため停止しました。"
    }
    & $mergeGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止します。" }
    & ".\scripts\prepare-merge-reward-preview.ps1" -CaptureOnly
}
```

失敗時は再実行を重ねず、開いた新しい`Builds/Diagnostics/merge-reward-*`のログ・XML・JSONを共有する。過去のAPKで代用しない。

候補のVisual Gate確認後のみ、`-CaptureOnly`を外し`-Final -Install -Serial "99RAY1BELH"`を付ける。Merge重点確認→合格後に全件EditMode／PlayMode各1回→同じ候補のQA APK→既存QAアプリへinstall-rの順。既存キャラ素材の準備は行うが、途中にキャラ単体テストを挟まない。uninstall／セーブ初期化なし。子APKビルダーはテストを実行しないため、その記録のtests_run=falseを変更せず、親runの実XMLと同一ソース／素材SHAで照合する。インストール成功だけではPixel合格にしない。

## 残課題

1. Unityで同条件JPEGを取得し、保存済み原本とのVisual Gateを確認する。
2. 合格後にMerge重点確認、その後1回の全件QA／APK。
3. Pixel 3aで表示・タップ／ドラッグ・生成・Merge・納品・カクつき／発熱を確認する。

次の改善候補：今回はなし。今回のMerge範囲と既存ロジック維持を優先する。
