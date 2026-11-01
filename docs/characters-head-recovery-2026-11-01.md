# 成功版ノアの頭胴配置へ復旧

現在の状態は**実装修正済み・実機再確認待ち**です。前回の頭同期限定検査だけで行った正式完了判定は、今回のPixelフィードバックを受け撤回しました。6名の実Unity／Pixel結果を受け取る前に「正式完了・未達0名」とは報告しません。

## 成功版との差分・原因

正式ノアの記録は`noah-authored-approved-2026-10-30.json`、基準ソースは`7c98804a78f07546fb8176b320904e8af44f6542`です。原承認PDFは`References/Review/noah-accepted-2026-10-30/user-screenshots.pdf`に残っています。今回の3PDF・22静止画は[受領記録](../References/Review/character-head-restore-feedback-2026-11-01/receipt.json)へ原本SHAと共に保存しました。静止画だけからAPKの版や揺れ周期は推定していません。

| 確認項目 | 成功版と現在の構造 |
| --- | --- |
| Prefab | キャラ専用Prefabではなく、TownActor.Bindから動的生成 |
| Root／Parent | TownActor → VisualRoot → 著作パーツGraphic。頭と胴体は同一Graphicの頂点 |
| Animator／Tween | 頭だけのAnimator／Tween／追従補間なし |
| Sprite切替 | 一回のSetPoseで方向のviewと同フレームposeを更新 |
| LocalPosition／Rotation／Scale | VisualRootで一括処理。パーツごとのWorld移動・二重Flipなし |
| 成功版の描画 | すべてのパーツが共有pose.Partを使用 |
| 後の回帰 | 頭だけを不透明輪郭同士の接点へ再配置し、正式な頭の初期配置を失った |

ゼロ時点のノアの頭原点（素材の600単位座標）を比較すると、右向きは成功版`(20,-268)`から`(-20.230,-258.135)`へ、背面は`(0,-268)`から`(-34.690,-270.475)`へ移動していました。左向きにも小差がありました。別Rootや描画遅延を仮定する必要はなく、頭だけの接続点再計算による回帰です。

前回の検査は、その計算で定義した2点が一致することを確認していました。正式な初期配置や自然な重なりを基準にしておらず、この回帰を見逃しました。旧実Unityの成功XMLは原本のまま保持しますが、復旧版の合格へ流用しません。

## 復旧内容

ノアを先に`pose.Part`による成功版の配置へ戻し、実際のGraphicソースを通す720連続状態で比較差分0を確認しました。その後だけ、残り5名も同じ`pose.Part`＋元の静的offset／pivot／sizeへ戻しました。キャラ別の新しい同期方式や手調整値は加えていません。

製品コードの変更は`NoahAuthoredGraphic.cs`と`CharacterAuthoredGraphic.cs`の頭の選択処理だけです。元のNoahAuthoredMotion.Sample、TownActorのRoot、現在の向き判定、静的登録JSON、正式画像、歩行速度、背面、接地影、表情・会話・すれ違い・街・Saveは無変更です。成功版から後に追加された背面の同Bodyローカル補正は維持し、古いWorld-X固定へ戻していません。この差分を隠して「全ファイルが成功版と同一」とは扱いません。

旧輪郭配置はEditor専用のBefore比較だけに残しました。復旧のための保護例外は[頭復旧記録](../References/VisualLock/characters-head-recovery-2026-11-01.json)で上記2描画ファイルに限定しています。元の53固定記録・歴史manifest・以前のamendmentは改変せず、原画や動作を例外へ追加できないガードです。

## 確認結果

- Noah：復旧前2880頂点不一致 → 復旧後720状態・不一致0。その後に5名へ進行。
- Momo／Lulu／Pom／Fio／Miel：各復旧前2880不一致 → 各720状態・不一致0。
- 合計4320状態で、頭配置・サイズ・角度・全UV・頭以外の頂点が基準どおり。開始／歩行／停止／再開／毎フレーム方向切替、会話・挨拶姿勢、開閉目を含む。
- これは固定した合格ソースと現描画コードを、最小Unity API代替面で動かすCLR回帰検査です。Unity実描画／実入力／Pixel試験ではありません。前向きは元の正式一枚絵のまま無変更です。
- 新しい限定Unity検査は、ノア240連続フレームを先に全成功させてから他の5名へ進み、計1440連続フレーム＋432比較標本・36PNGを保存します。判定基準は成功版のBodyローカル初期配置であり、旧輪郭点一致ではありません。
- 新しいproofのkind／参照ソース／判定基準／順序を必須にし、旧proofを拒否します。PNG撮影はPortraitのbeforeRenderで再度同じposeを固定し、検査した閉じ目と画像が食い違わないようにしました。

## Windows：頭同期1件だけ→新APK→Pixel

Unity Editorを閉じ、Pixel 3aをUSB接続・ロック解除して、以下全体をPowerShellへ貼り付けます。全件EditMode／PlayModeは実行しません。頭同期のPlayMode1メソッドに成功した場合だけ、その後に新APKを作成して同じ街QAへ上書き更新します。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuRestoreGit = Find-SourceGit
    $mofuRestoreBranch = & $mofuRestoreGit branch --show-current
    if ($LASTEXITCODE -ne 0 -or $mofuRestoreBranch.Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチが異なるため停止しました。"
    }
    & $mofuRestoreGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗しました。" }
    & ".\scripts\capture-character-head-sync.ps1"
    & ".\scripts\prepare-characters-direct-pixel.ps1" -Install -Serial "99RAY1BELH"
}
```

途中失敗はそこで停止し、古いAPKを代用しません。成功後は街QAで下→左→右→上→停止→再歩行→連続方向転換を確認してください。保存データの削除・アンインストールはありません。新しい`characters-head-sync-...-review-*.zip`全パートとPixelの結果を照合してから正式固定を判断します。今回はMergeへ進みません。
