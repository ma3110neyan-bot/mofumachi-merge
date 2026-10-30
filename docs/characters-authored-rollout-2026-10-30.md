# 5名横展開：実装候補／Windows受入手順

ノアの合格方式・素材は固定。モモ→ルル→ポム→フィオ→ミエルを一名ずつ登録し、現在は5名の実装候補が揃った。正式正面は元atlas、左右斜め・後ろは各キャラの独立7パーツ＋閉じ目素材。NoahAuthoredMotionと既存接地影／親mask／足元Sortingを再利用する。新方式、街・北境界・噴水・Core／保存・依頼／納品／報酬・Merge・BGM・図鑑・フォント・街Lv・UIレイアウト・会話発生ルールは変更しない。

ユーザーの最新指示により個別プレビュー／Windows実行待ちは解除済み。ローカルの素材・SHA・接続・純粋歩行モデル・PowerShellガード・API検査と、実Unity／APK／Pixelを区別する。**実装済み・実Unity描画／APK／Pixel未確認。** 方向別原画の最終的な印象、まばたき時の連続性、Sorting、入力、6名同時可視性、負荷は未判定。

## 実行順

新しい `scripts/prepare-characters-pixel-preview.ps1` は、5名の各単体ネイティブ検査→全件EditMode／PlayMode各一回→5名＋固定ノアの一時資源準備→6名同時表示・画面raycast／mask／入力・保存非変更の単体ネイティブ検査→同じソース・素材から比較用2APK、の順。単体で停止した場合、無関係な全件を先に繰り返さない。失敗したrunのXML／ログ／ZIPを保存して対象だけ調査する。

通常版では追加5名の表示は既定OFF。比較用baseline APKは合格済みノアを有効のまま、他5名が従来表示。authored APKだけ追加5名を有効にする。defineはBuildPlayerの一時指定で、PlayerSettingsへ保存しない。双方のpackageは既存街QAと同じ。インストールはinstall-r、アンインストール／セーブ初期化はしない。baselineへ戻してもノア基準は維持する。

## Windows PowerShell

下記をまとめて実行する。処理全体をRemoteSignedの子PowerShell内へ置く。既存featureブランチだけをfast-forwardし、mainへは統合しない。

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
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止しました。" }
    & ".\scripts\prepare-characters-pixel-preview.ps1" -Install -MeasureSeconds 600
}
```

Unity6000.6.4f1とAndroid Build Supportが必要。Pixel99RAY1BELHの既存セーブを保つ。端末で街を開いた後にPowerShellでEnter。新しく起動した街QAのPIDと6名の表示モードが一致してから、実SurfaceFlingerのFPS／P95・PSS・バッテリー温度等を600秒収集する。カクつき・タップ・熱の手動回答も残す。数値や回答を自動合格へ置換しない。各キャラを実際に操作し、左右／前後・停止・会話・挨拶・背景との重なりを確認する。Unityの6名集合は隔離fixtureで、街の通常人口／配置を変更するものではない。

## 結果と再測定

出力する **Exact six-character run** のパスを保持する。各キャラの単体ZIPは兄弟ディレクトリに出力。統合review ZIPはXML／ログ・6名実画像・ソース／APK識別を含む。大きなfull-screenshots原本とAPKはrunへ残す。ブラウザー画像をUnity画像へ置換しない。

既存の完成runを再測定する場合は、全件テスト／ビルドを繰り返さず、以下へ**表示された正確なrunパス**を指定する。「最新フォルダー」を推測で選ばない。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\prepare-characters-pixel-preview.ps1" -BuildDirectory "ここを完成runの絶対パスへ置換" -Install -Mode authored -MeasureSeconds 600
```

比較は同じrunに対して `-Mode baseline`。同じ操作・同じ長さで記録する。戻す場合もbaselineをinstall-rし、保存は消さない。ノア方式・他領域・進行バランスを変えない。自動挨拶1日1回の案は次工程の記録のみで未実装。

## 検査の限界

クラウドではライセンス／Android／USBの不足によりUnity・APK・Pixelは未実行。通常／Android候補define／Editor／PlayModeのAPIコンパイルと、素材・接続・歩行モデル・PSのガードを確認した。dotnet全体テストの標準testhostはローカルsocketの権限で実行前に中断しており、成功とは数えない。Windows PowerShell5.1固有動作も未確認。最終合格は実UnityとPixelの実画像・操作・負荷結果による。
