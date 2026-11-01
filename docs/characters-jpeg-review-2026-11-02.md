# Pixel移行前のJPEG確認

JPEGは`References/Review/character-head-restore-preview-2026-11-02/`に7枚あります。`all-six-directions.jpg`が6名一覧、各`*-four-directions.jpg`が静止・歩行2位相・首元拡大です。

**正式素材＋現在の描画座標によるソフトウェア再現で、Unity／Pixel実画面ではありません。** ルルの左右には頭と襟の隙間が見えるため、現状は未合格です。画像側で隙間を消していません。ゲーム・原画・静的レイアウトは変更していません。今回Pixelへは更新しません。

PowerShellで次を実行すると、GitからJPEGを取得し、一覧画像とフォルダだけを開きます。Unity起動・テスト・APK作成・ADB・インストールはありません。USB接続も不要です。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuJpegGit = Find-SourceGit
    $mofuJpegBranch = & $mofuJpegGit branch --show-current
    if ($LASTEXITCODE -ne 0 -or $mofuJpegBranch.Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチが異なるため停止しました。"
    }
    & $mofuJpegGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗しました。" }
    $mofuJpegFolder = Join-Path (Get-Location).Path "References\Review\character-head-restore-preview-2026-11-02"
    Start-Process -FilePath (Join-Path $mofuJpegFolder "all-six-directions.jpg")
    Start-Process -FilePath explorer.exe -ArgumentList ('"' + $mofuJpegFolder + '"')
}
```
