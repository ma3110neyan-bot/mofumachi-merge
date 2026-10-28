# ノア比較撮影1件の診断（全件QAではない）

受領原本 `References/Review/full-playmode-2026-10-28-201053/` はsource1d11629、PlayMode234中233成功／1失敗／skip0／inconclusive0。ノア表示・接地・Sorting専用4件、北接続3件、5地区連続移動1件は成功。唯一の失敗は `ExperienceCaptureTests.NorthNoahBeforeAfterAndFixedStepEvidencePreserveSavedState` の比較範囲外5621画素。失敗ペアのID・差分画像が原本にないため、どの表示が変化したかは未特定。

今回の追加は撮影失敗時の証拠保存と単体診断だけ。ゲーム・ノア動作・原画・街・北境界・噴水・保存処理は1d11629から変更していない。比較範囲や「範囲外0」判定は維持し、未特定の差分を許可しない。

## 実行

GitHub Desktopで現在のfeatureをPullする。Unityエディターを閉じ、Windows PowerShellで次を一度実行する。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\diagnose-noah-capture.ps1"
```

このコマンドは**失敗したメソッド1件のみ**を、元のAndroidターゲット・PlayModeで実行する。全EditMode／全PlayMode／APK生成／Pixel操作／セーブ初期化は行わない。テストは従来同様、隔離したテスト用保存状態を使う。

赤字で失敗しても、最後の `Noah single-test diagnostic ZIP:` のパスに診断ZIPを残す。そのZIPを共有する。保存先は `Builds\Diagnostics\noah-pair-日時-ID.zip`。

診断ZIPにはXML/log/source-infoと、失敗した場合に `37-noah-...-FAILED-before.png`／`...-FAILED-after.png`／`...-FAILED.json` が入る。JSONは比較範囲、範囲外画素のbbox、前後のUI Graphicの座標・表示状態・色を記録する。座標原点は左下。北などの無関係な全画像をZIPに加えない。

この診断が1件成功しても全件QA／Pixel／ユーザー採用が済んだとは扱わない。画像で範囲外差分の対象を特定した後に必要な最小修正を行い、関連検証を先に通す。現時点では表示不具合が解消したという報告はしない。
