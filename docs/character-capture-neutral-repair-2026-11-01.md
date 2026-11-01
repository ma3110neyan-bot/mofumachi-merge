# モモ単体の通常状態比較を修正

受領run：`characters-authored-20261101-110035-1f327185`、停止子run：`momo-authored-20261101-110311-d580ec24`。source `12d52e394ba025e28b946c9f3fe500ca2d9ebc2d`、fingerprint `05e99ce4bc462fac4c67f92fcfbe7fbfddcf8f282a3ab385074772fa71a045d8`。

今回のreview-01 ZIPはCRC正常。原XMLではノア1/1成功・終了0、モモ0/1成功・1失敗・終了2、両方skip／inconclusive0。モモの失敗は `ExperienceCaptureTests.CharacterAuthored.cs:144` の復元mesh照合。全件／APK／Pixelへ進む前の停止である。

原因：`VerifyAuthoredScreenInput` の実クリックが、今回のタップ会話仕様で `TapCharacter → Greet` を起こす。`StartInteraction` は会話枠を消すが、挨拶の残り時間は消さない。その後で旧描画へ切り替えると、通常状態の初期snapshotと挨拶状態のmesh／UVを比較してしまう。ノア単体には前回修正した順序が、残り5名の共通撮影方法に残っていた。

修正は `ExperienceCaptureTests.CharacterAuthored.cs` だけ。通常bodyのenabled／全頂点／全UVの同一性を、入力前に照合する。画面raycast、CanvasGroup遮断、Graphic遮断、mask遮断、親へのDown／Up／Clickの検査は削除せず、実歩行で会話相手へ近づいた後の会話段階で実行する。その実入力から会話が開くことを必須とし、以前の直接Clickの代用も取り除いた。初期Idleや最初の歩行へ、先行した挨拶の状態を混入させない。

ゲームの表示／動作／会話実装、原画、登録JSON、ノア方式、街、Save、保護manifestは変更していない。失敗した実XMLをRED証拠として保持。修正版PlayMode APIコンパイルは0警告／0エラー、Noah26／552保護入力は許可済みNov1コードamendment込みで照合成功。新しいUnity実行の成功はまだ確認していない。無関係な全件テストは実行していない。

原本11記録とSHAは `References/Review/characters-failed-momo-2026-11-01-110035/`。今回part01内の17画像はデコード・寸法・SHAを確認したが、ノアのproof.jsonや親source-infoを含む全パートではない。画像は受領ZIP／runtime保持で、repoの11記録だけを完全なノア品質証拠とはしない。モモ停止の原因調査に必要なXML／log／準備情報／failure-contextは揃っている。

## 次はモモ1件だけ

Unityを閉じ、以下をPowerShellへまとめて貼り付ける。今回はノア、他4名、全件、APK、Pixelを再実行しない。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuRepairGit = Find-SourceGit
    $mofuRepairBranch = & $mofuRepairGit branch --show-current
    if ($LASTEXITCODE -ne 0 -or $mofuRepairBranch.Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチを確認してください。"
    }
    & $mofuRepairGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止します。" }
    & ".\scripts\capture-character-authored.ps1" -Character momo -OpenReview
}
```

終端で今回作成したモモZIPを選択するExplorerが開く。同じ新しい日時／IDのモモZIPを全パート提出する。モモ単体の成功を確認してから、同じ新版の残り工程へ進む。以前の12d52e3のノア成功を、将来の別source／fingerprintの同版証拠へ書き換えない。正式固定は引き続き保留。
