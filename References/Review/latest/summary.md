# 最新レビュー：Unity exit 2 原因特定・修正案のみ

- 現在ブランチ：`feat/daily-world-pass-20261016`
- 最新の確認対象共有commit SHA：`9f408badda76e3647d0c53213f6efb472751c17f`
- run／製品ソースSHA：`font-only-20261104-204729-31fcbc90`／`193ab6eeb216d9f457096d9f3df305704f2f962e`
- 今回の目的：このrunだけから最初の原因と指定7項目を確定し、修正せず停止する。
- 実施内容：共有run-infoの11ファイルのバイト数・SHA256を原本と照合。capture.logのCLIと親JSONのsource／fingerprint／packed SHAも一致。別runは使用せず、このrunが記録するソースSHAの該当箇所を読み取り照合した。
- 成功／失敗：原因特定は完了。保存済みNative結果は2件中1件成功・1件失敗、skip／inconclusive0。`capture_verified=false`、検証済み画像数0。今回、テストは実行していない。
- 根本原因：Hachiの必要高14.5を高さ14.0の固定欄へ、収容判定の許容差+0.5で誤って受け入れたこと。文字生成0頂点のassert失敗によりUnity Test Runnerがexit2となった。
- 修正内容：なし。今回の変更はこのsummary、status.json、error.logの3ファイルのみ。ゲーム・フォント・UI・テスト・Visual Lock・共有スクリプト・受領原本は変更していない。
- 次にやること：以下の修正案を提示して停止。PowerShell／Unity／captureの再実行、コード修正、追加テスト、APK、Pixelへ進まない。

## 指定7項目

1. **最初のUnity側エラー全文**（capture.xmlの唯一の失敗test-caseのfailure/message）：

```text
  Current request caption: generated glyphs [font=UIFontAccent, size=10, rect=(x:-62.20, y:-7.00, width:124.40, height:14.00), preferredHeight=14.5]
  Expected: greater than 0
  But was:  0
```

2. **発生ファイル**：`Assets/Mofumachi/Tests/PlayMode/HachiFontCapture.cs`。
3. **行番号**：50（AssertGeneratedGlyphsの頂点数assert）、161（Runからの呼び出し）。これは不具合の検出箇所。収容判定の原因箇所は `Assets/Mofumachi/Presentation/ReleaseTypography.cs:28` と29。
4. **フォント変更との関係**：対象は「現在の依頼」の `Current request caption`、`UIFontAccent`＝Hachi、UGUI Bold、サイズ10。ログで枠高14.00・必要高14.5・生成頂点0を確認。同じソースのHachi TTFにはこの短文の全字形が存在し、フォントメトリクス上もサイズ10の行高は14.48。`ReleaseTypography.Fits`はMerge brand以外に0.5の高さ余裕を加えるため、`14.5 <= 14.0 + 0.5`がtrueとなる。既存MergeCompactFontFitはここで縮小を止め、Truncateの14高枠で文字生成できない状態が残る。フォント自体の一括置換やUI再設計を必要とする問題ではない。
5. **最小修正箇所**：`ReleaseTypography.cs`の`Fits`にあるheightAllowance決定1箇所。既存の小さいMergeヘッダー文字に限定し、許容差を0へ揃える。`MergeTypography.cs:84`の既存縮小ループと下限8は変更しない。
6. **修正対象ファイル数**：製品コードは **1ファイル**。ただし現行SHA安全ガードを維持して適用するには、`References/VisualLock/font-only-2026-11-04.json`のReleaseTypography行のSHA更新も必要なので **適用時は合計2ファイル（製品1＋安全記録1）**。テストやassertを緩める変更は不要。今回の報告3ファイルとは区別する。
7. **推奨修正案**：既存 `ReleaseFontPolicy.CompactMinimumSize(text.name) > 0` の3ラベル（Current request caption／Merge reward coins／Merge brand）だけを厳密な高さ判定にする。以下は未適用の案：

```csharp
float heightAllowance =
    ReleaseFontPolicy.CompactMinimumSize(text.name) > 0 ? 0f : .5f;
```

Hachi優先・軽いBold・既存のフォント下限を維持し、必要時だけ既存ループで収まるサイズへ下げる。固定サイズ10を強制しない。枠・配置・色・正式素材・Mergeロジック・SaveData等を変えない。修正後の実描画結果は未確認。`HachiFontCapture.cs`の0頂点Fail条件とPowerShellの終了検知は正常なため変更しない。

## このrunで確認できる補足

- `StoryTypographyTests.StoryRolesAndNoticeFitPortraitSizes`はPassed。
- prepare-noah.log／prepare-five.logはともに終了0。capture.log:273のScriptAssembliesも終了0、275でbuild success。その後678で「One or more tests failed」によりexit2。
- capture.log:101にライセンス更新警告があるが、108でライセンス更新成功、110で初期化完了となり、実際にテストまで進んでいる。今回の終了2を発生させた失敗は上記assert。
- capture.log自身がCLIの-logFile出力。共有フォルダには別名のEditor／stdout／stderrファイルはない。存在しないファイルを作る処理や再実行は行わない。
- 親JSONのimage_count=0は検証済み枚数。共有対象はテキストだけであり、途中PNGの実枚数や見た目を確認したとは扱わない。
- `success=false`／`tests_passed=false`はこの保存済み失敗runの状態。`analysis_complete=true`は今回の読み取り調査完了。共有機構の自動commit/push有効状態は維持する。
- [最小ログ抜粋と完全なstack trace](error.log)／[機械判定用status.json](status.json)
