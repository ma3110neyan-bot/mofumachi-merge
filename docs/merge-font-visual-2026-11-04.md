# Mergeフォント A/B/C：Visual確認用候補

基点は `c84e5d9fa1c7d6aaf1305940794250509fa35f8f`。今回の正式採用は未確定。既存レイアウト・配色・5×6・アイテム・ボタン位置・ごほうびタスク・ロジック・Save・街・キャラ・BGMは変更しない。

| 案 | タイトル | 状態 |
|---|---|---|
| A | Hachi Maru Pop + UGUIの軽いBold | 比較候補 |
| B | A + 薄い濃ピンクOutline（0.35参照px、半透明） | 推奨候補、Visual未承認 |
| C | 既存Kiwi Maru Medium | 比較候補 |

Hachi Maru PopはGoogle Fonts公式メタデータでもRegular400のみ。元のTTFを維持し、UGUI Textで調整する。TMP移行は行わない。戻る／生成／整理／依頼／納品／下部ナビはHachi Bold、数値・Lv・Coins・小さい情報は既存Zen Maru Gothicを維持。640幅で欠けていたLvバッジは、枠から行高がはみ出す場合だけ文字9→8へ収める。枠や配置は変えない。

## 画像の区別

[比較一覧](../References/Preview/merge-font-abc-2026-11-04/index.html) と同フォルダの6枚PNGは、実TTFをFreeTypeで描いた**フォント素材比較**。720×1480／640×1072の両サイズを出力した。太字・輪郭は近似であり、ゲーム画面・実Unity描画・実機合格の証拠ではない。`material-proof.json`も `native_unity=false` としている。

添付の元JSON／HTMLは `References/Review/merge-font-baseline-2026-11-04/` に無加工保存。元JSONには同一c84e5d9の11枚・`disk_verified=true`が記録され、HTMLとの画像名対応を確認した。ただし元JPEGバイナリは添付ファイルとして受領していないため、こちらで11枚のSHAを独立再照合したという意味ではない。

細いピンク帯＋中央コインは `CapturePinkBaseline` のBefore再現だけに存在する。実Afterに追加・削除する変更はない。

## 実Unity比較の撮影だけを行う

クラウド環境はUnityライセンス未取得。実UnityのA/B/C比較は未実行。下記はWindowsの新しいクリーンworktreeで、通常11枚に加えて別フォルダへ比較6枚を撮影する。元の未コミット設定・`.utmp`・既存撮影結果・stashは保持する。`pull`／`reset`／`clean`はしない。

PowerShell 5.1に下記ブロックをそのまま貼り付ける。Unity Editorは閉じておく。途中で失敗した場合は自動再試行せず、そのrunを保持する。

```powershell
& {
    $ErrorActionPreference = 'Stop'
    $captureSource = 'C:\Users\User\Desktop\Mofumachi\mofumachi-merge'
    Set-Location -LiteralPath $captureSource
    . '.\scripts\build-android.ps1'
    $captureGit = Find-SourceGit
    $captureBranch = 'feat/daily-world-pass-20261016'
    $currentBranch = & $captureGit branch --show-current
    if ($LASTEXITCODE -ne 0 -or $currentBranch.Trim() -ne $captureBranch) {
        throw 'Unexpected source branch. No capture started.'
    }
    & $captureGit fetch origin $captureBranch
    if ($LASTEXITCODE -ne 0) { throw 'Fetch failed. No capture started.' }
    $captureRevision = (& $captureGit rev-parse ('refs/remotes/origin/' + $captureBranch)).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve candidate.' }
    & $captureGit merge-base --is-ancestor c84e5d9 $captureRevision
    if ($LASTEXITCODE -ne 0) { throw 'Encoding fix missing. No capture started.' }
    $captureTree = Join-Path (Split-Path -Parent $captureSource) ('capture-' + [guid]::NewGuid().ToString('N').Substring(0,8))
    & $captureGit worktree add --detach $captureTree $captureRevision
    if ($LASTEXITCODE -ne 0) { throw 'Worktree creation failed. No capture started.' }
    Set-Location -LiteralPath $captureTree
    $captureStatus = @(& $captureGit status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0 -or $captureStatus.Count -ne 0) { throw 'New worktree is not clean. No capture started.' }
    Write-Host ('Capture candidate: ' + $captureRevision)
    & '.\scripts\prepare-merge-reward-preview.ps1' -CaptureOnly -FontComparison
}
```

結果は新worktreeの `Builds/Diagnostics/merge-reward-<日時>-<ID>/screenshots/font-comparison/`：

- `A-720x1480.jpg`／`B-720x1480.jpg`／`C-720x1480.jpg`
- `A-640x1072.jpg`／`B-640x1072.jpg`／`C-640x1072.jpg`
- `merge-font-native.json`／`merge-font.html`

比較HTMLを自動で選択する。元の `screenshots` 直下11枚＋JSON＋HTML契約は維持。比較6枚の実在・SHA・画像とJSON対応・ソースidentity・同一State／Save書込回数・同サイズ間のRect一致を検査し、後処理後も再検証する。Lv文字の実メッシュも確認する。欠損や後処理失敗時は成功フラグを立てない。

**ここでVisual確認のため停止。** 全件EditMode／PlayMode・APK・Pixel・インストールは実行しない。

## 確認状況

- 素材比較6枚＋タイトル一覧を作成、B／Cの読みやすさと丸文字の差を確認。
- 関連するフォント役割hostテスト25件は成功。追加のhost再実行はローカル通信権限で中断しており、成功に加算しない。
- 撮影artifact／フォントartifact／既存preview／UX限定scopeのPowerShell fixtureは成功。合成証跡を使う契約テストで、実Unity撮影結果ではない。
- Unity API参照コンパイルは0警告・0エラー。実Unity／Visual採用／端末結果は未確認。
- 独立レビューで比較helperのEditor限定参照を検出。非Editor APIコンパイルでCS0117を再現し、撮影コードを条件付きにして0警告・0エラーへ修正した。レビュアーは利用上限で途中終了したため、独立レビュー全体が完了したとは扱わず、残りは作者が差分確認した。
