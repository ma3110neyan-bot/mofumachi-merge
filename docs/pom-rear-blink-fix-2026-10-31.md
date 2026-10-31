# ポム：後ろ向きの瞬きで未使用パーツを参照する不具合

run `pom-authored-20261031-114725-622e5edd` の3ZIPを受領した。source4fa3a9e、実Unityは成功0／失敗1／skip0。画像・proofは最後まで出力されているが、追加キャラの描画処理が空のパーツIDを参照し、厳格なログ検査で不合格となっている。原本77点／画像70枚を保存し、全デコード・SHA・bytes・寸法を照合した。11画像対の範囲外0も再計算したが、それをUnity合格とは扱わない。

原因は、後ろ向きの瞬き時に `closedHead != null` だけで閉じ目パーツを選んでいたこと。後ろ姿には目がなく、その任意フィールドはUnityのJSONで空オブジェクトとして残っていた。追加5名の共通rendererで、後ろ向きは常に元の後頭部を使うよう選択を限定した。ノア本体、原画、登録値、街・北境界・噴水・保存等は変更しない。左右の閉じ目と手足の選択は維持する。

関連する6ケースだけを既存NUnitの同一プロセスrunnerで実行し、旧選択式の4成功／2失敗から修正後6成功／失敗0を確認した。通常／Android候補／PlayModeのAPIコンパイルと関連PowerShellガードも成功。固定ノアと552保護入力は不変。Unity側にも、後ろ姿の瞬きを毎回明示的に描画し、28頂点・完全な形状／UV不変・エラーなしを確認する検査を単体メソッドへ追加した。自然な閉じ目の撮影とは区別し、後ろ姿だけの画像を閉じ目の証拠にしない。ログを無視する処理は追加していない。

**修正後の実Unity／APK／Pixelは未確認。次はポム1件だけ再実行する。** 全件QA・他キャラ検査・APK作成はこのコマンドに含まれない。前のモモ・ルルの通過表示は保持するが、共通renderer修正後の同一ソース6名QAは後で確認する。個別プレビュー承認待ちは復活させない。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuGit = Find-SourceGit
    if ((& $mofuGit branch --show-current).Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチが異なるため停止しました。"
    }
    & $mofuGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗しました。" }
    try {
        & ".\scripts\capture-character-authored.ps1" -Character pom
    }
    finally {
        Invoke-Item -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Diagnostics"
    }
}
```

終了後に今回出力された `Character-only review ZIP` の全パートを確認する。原本と関連検査の証拠は `References/Review/pom-authored-2026-10-31-114725/`。実機のSave初期化・アンインストール・main統合は行わない。
