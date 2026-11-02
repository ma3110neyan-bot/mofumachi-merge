# 2026-11-02最新：フィオ左右微調整後はPixelへ直接更新

最新ユーザー指示は、フィオ左右の頭Xだけを直した後、画像の再確認なしで全6名をPixel 3aで試すこと。[現在の実行手順](fio-pixel-2026-11-02.md)を使用します。頭同期のUnityテスト・全件テスト・画像承認は今回の前提にしません。既存の直接ビルド入口で新しい6名入りAPKを作り、保存を維持して上書き更新します。以下の旧推奨記載は履歴です。

# 固定済み6キャラをテストなしでPixel 3aへ更新

**最新の頭胴復旧では、[復旧用の限定手順](characters-head-recovery-2026-11-01.md)を使用します。** 前回の正式完了はPixel再発により撤回しました。下の直接ビルド入口自体は維持していますが、今回の推奨手順は新しい頭同期1件→この直接ビルドの順です。

Unity Hubの操作、EditMode／PlayMode、単体撮影、全件テストは実行しません。ノアと5名の同じ原素材を準備し、既存の2.5Dフラグを指定したAPKを1本だけ作成し、同じ街QAへ上書き更新します。APK作成のためUnity6000.6.4f1本体は自動起動します。

キャラ原画・描画・動作・正式Visual Lock53ファイルは無変更です。新しい入口は`scripts/prepare-characters-direct-pixel.ps1`とEditor専用`CharactersDirectPixelBuild.cs`のみで、通常の全件QAルートは変更していません。

## Windowsで実行

Unity Editorを閉じ、Pixel 3aの画面ロックを解除してUSB接続します。USBデバッグの許可が出たら許可してください。PowerShellへ以下全体を貼り付けます。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuDirectGit = Find-SourceGit
    $mofuDirectBranch = & $mofuDirectGit branch --show-current
    if ($LASTEXITCODE -ne 0 -or $mofuDirectBranch.Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチが異なるため停止しました。"
    }
    & $mofuDirectGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗しました。" }
    & ".\scripts\prepare-characters-direct-pixel.ps1" -Install -Serial "99RAY1BELH"
}
```

成功するとPixelの**「もふまちメルジュ 街QA」**が起動します。街を開き、6名の左右・背面・歩行・停止・方向転換を確認してください。`install -r`で更新し、アンインストールやデータ初期化は行いません。10分計測や入力待ちの確認質問もありません。

## 記録と確認状態

この入口は、元のフルQAを省略するユーザー指定の実機プレビューです。架空のテストXMLや成功フラグで旧ビルドゲートを通さず、準備2回＋APKビルド1回の`executeMethod`だけを呼びます。テスト・実機合格フラグはfalseです。正式採用済みの見た目を解除するものではありません。

前処理でUSB認可とPixelの機種を確認し、正式Visual Lockと既存保護入力を照合します。元の生成資源は既存の所有者検査に通るものだけ可逆退避します。今回の生成資源だけを後片付けし、新規runのソース・資源・APKハッシュが一致する場合だけインストールします。エラー時に古いAPKを探して代用しません。

出力は`Builds/Diagnostics/characters-direct-pixel-日時-ID/`です。`characters-authored.apk`、ビルドJSON、`source-info.json`、3工程のログを保存します。失敗した場合は画面のエラーと、この新規runのログを共有してください。過去の全件テスト用コマンドの再実行は不要です。

クラウドで確認したのは、新しい経路の関連PowerShellガードとUnity APIコンパイル（0警告／0エラー）です。正式固定53ファイルの一致と独立レビューも確認しました。Windows上の実APKビルド・Pixelインストール成功を先に報告するものではありません。
