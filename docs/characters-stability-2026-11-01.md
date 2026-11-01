# 6キャラの最終安定化

現在の原画・方向別パーツ・サイズ・接地影・頭接続を維持し、近接時の移動処理だけを修正しました。正式固定は、新候補の全件Unity検査とPixel確認後です。

## 修正と確認

- 停止中の相手の足元付近で、左右の回避候補を交互に選ぶ座標反転を再現。同じ経路点で直前の移動を反転する候補を選ばず、通れない場合は既存の待機・再経路へ渡します。
- 相手を避ける再経路に通常の横押し回避を重ねていたため、短い経路点へ戻る動きも発生。再経路中は計画済みの回避を使い、床・障害物・足元の衝突検査を維持します。NPCも移動放棄の前に一度この経路を試します。
- 新しい目的地・経路点、復元・キャンセルでは進行方向の記憶を解除。ユーザーの方向転換を妨げません。Saveには追加しません。
- 6名それぞれの近接歩行・到着、NPC、障害物付近、手動反転を含む新規9件は修正前8失敗→修正後9成功。既存の頭接続・背面・歩行・接地・経路を含む関連68件すべて成功。純粋C#の結果で、Unity描画やPixel成功ではありません。
- 通常交差144条件は全到着・反転0。近接249条件は、半秒内3回以上の反転が131条件→0、全到着。再経路開始後と次の経路点で2回向きを変える例はあります。最新3座標記録で確認し、毎フレームの反転ループと区別しています。実表示の自然さはPixelで確認します。
- 通常会話は既存のタップのみ。自動挨拶も停止中のため0回/日。既存の連打抑止・ドラッグ時の会話終了を維持します。
- Unity側にも6名の近接移動検査を追加。表示順ソートで対象が重複しないよう対象一覧を固定し、6名の一致を必須にしました。6名撮影には正面交差＋停止相手の回避動画標本と、毎刻みの座標・反転数を追加しました。

証拠は [検算記録](verification/character-stability-2026-11-01/receipt.json)、PDF原本は `References/Review/character-stability-feedback-2026-11-01/`。通常・Android・Editor描画・PlayMode・EditorビルドのAPIコンパイルと関連PowerShell検査は成功しました。新しいUnity実行・全件・APK・Pixelの結果は未受領です。前回7件成功や旧全件の部分成功を今回の全件成功へ合算しません。

## WindowsからPixelへ進むコマンド

Unity Editorを閉じ、Pixelを接続・ロック解除してUSBデバッグを許可した状態で、PowerShellへ以下全体を貼り付けます。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuStabilityGit = Find-SourceGit
    $mofuStabilityBranch = & $mofuStabilityGit branch --show-current
    if ($LASTEXITCODE -ne 0 -or $mofuStabilityBranch.Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチが異なるため停止しました。"
    }
    & $mofuStabilityGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗しました。" }
    & ".\scripts\diagnose-character-stability.ps1"
    & ".\scripts\prepare-characters-final-pass.ps1" -Install -Serial "99RAY1BELH" -MeasureSeconds 600
}
```

最初は関連PlayMode3メソッドだけ（6名の近接移動＋自動会話停止＋実タップ連打）。成功した場合のみ同じコマンド内で最終工程へ進みます。旧生成資源は、既存の所有者・内容検査に通るものだけ可逆退避します。元の素材・Saveは消しません。

最終工程はノア→残り5名を順番→全EditMode/PlayMode各1回→6名表示・接続・交差・タップ→同じ候補のQA APK→Pixel10分です。修正後の6名版を先にインストールします。Pixelで街が開いたらPCのEnterで計測を開始し、その間に6名の移動・方向・すれ違い・タップを確認します。FPS・メモリ・温度・クラッシュ記録と最後の観察回答を合わせて判断します。実機で同時6名を見られなければ、その回答は`unknown`です。

途中で失敗したらそこで停止します。全件を再実行せず、その新しいrunのZIPを共有してください。成功時は最後にエクスプローラーで選択される新しい`characters-authored-...-final-review-*.zip`全パートを共有してください。最初の3件だけのZIPは最終結果の代用になりません。

## 固定の条件

残る確認は、新候補のUnity全件・6名実描画、Pixelの見た目/操作/負荷です。両方が揃うまで`formal_fixed/adopted=false`を維持します。新方式・街・原画・Save・Merge等へは広げません。
