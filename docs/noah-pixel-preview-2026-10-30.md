# ノア：本日のPixel3a試験候補

**ゲーム接続の実装済み・実機未確認。クラウドで実Unity実行／AndroidAPK生成／Pixel確認はしていない。正式採用・他5名展開なし。**

前計画の左右・後ろ素材を、ノアだけの独立パーツGraphicへ接続する。既定OFFで、実験用APKの明示defineだけが自動でONになる。正面は正式atlasの表示を維持し、正面の独立リグ、新しい方向のまばたき・表情差分は未完成。正式原画は上書きしていない。方向別素材自体も未承認で、顔・衣装・体型・尾の印象を含めてPixelで判断する。

最新HTMLの6項目ブラウザー確認と、後ろの尾の根が腕のように見えなくなった画像を確認した。これはUnity画面ではない。チャットには可視化を埋め込まない。

## 実装

- 既存TownActor内で、32頂点・共有atlas1枚のパーツ描画を切り替える。世界位置・ナビゲーション・背景との足元Y順・親のマスク・入力を引き継ぐ。常時最前面へ移さない。
- ノアの動作計算は純粋ロジック。左右反転で絵を作らず方向別絵を使い、頭内部は剛体。停止時は既存の歩行強度の補間を使う。後ろの手振りも腕へ反映する。
- 会話中かどうかを既存状態から読むgetterのみ追加し、会話・依頼・報酬・Saveロジックは変えない。
- 原本PNGはReferencesに保管。Editorが所有マーカー付きの一時Resourcesへ24パーツをまとめた最大2048pxのatlasと設定を作る。ETC2_RGBA8、mipmap／CPUreadableなし。生成元のSHAと、生成物・import設定のSHAを単体検査と両APKへ結び付ける。生成物は手順の最後に自分のrunのものだけ削除する。
- 現行版と試作版は同ソースの別APK。通常QAや旧退役リグの検査ゲートは開けない。

## Windowsで行う順序

Unity Editorを閉じ、既存featureブランチを更新してから実行する。未コミットの製品入力や、他runの試作Resourcesがある場合は削除せず停止する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuGit = Find-SourceGit
    $mofuBranch = (& $mofuGit branch --show-current).Trim()
    if ($mofuBranch -ne "feat/daily-world-pass-20261016") {
        throw "現在のブランチは $mofuBranch です。この表示を送ってください。"
    }
    & $mofuGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止します。" }
    & ".\scripts\prepare-noah-pixel-preview.ps1" -Install
}
```

最初のhelper読み込みも上記の子PowerShell内で行うため、親の実行ポリシーがRestrictedでも恒久設定を変えずに開始できる。組織ポリシー等で拒否された場合は停止し、そのエラーを確認する。

2026-10-30のWindows報告 `Frozen input changed: Assets/Mofumachi/Core/link.xml` に対応し、Core／Presentationの2つのlink.xmlをテキストとして改行正規化するよう定義を修正した。552保護対象のパス・期待SHAは変更していない。LF／CRLFだけの違いは許容し、XMLの内容変更や画像のバイト変更は引き続き拒否する。関連のPowerShell回帰検査で失敗を再現してから修正後成功を確認した。XML自体やゲーム本体は書き換えていない。実Unity・APK・Pixelの確認は引き続き未了。

1. 保存済みの原画・街等の入力を確認する。Unityが書き換えるグラフィック／BuildProfileのローカルキャッシュは既存方針に従い勝手にresetしない。
2. 一時atlasを準備し、**新しいノア単体のPlayMode1メソッドだけ**を実行する。公開GuidePoint→AdvanceFrameの14.8秒相当の固定標本、8組比較、タップの親への伝達、OFF復帰、他5名、保存不変、5つの背景順の表示用配置を確認する。固定標本をFPSと呼ばない。
3. 単体が成功し、同じソースと生成画像・import設定が維持された場合だけ、current／authoredの2APKを作る。失敗時はそのrunのログ・画像・最大24MBごとのZIPを保存して停止する。
4. 成功した場合だけPixel99RAY1BELHへ試作版をinstall-r。アンインストール／Fresh／Save初期化は行わない。
5. Pixelで街を開き、既存のキャラ操作からノアを選ぶ。ノアが見える状態でPCへ戻りEnter。実行モードが本当にauthoredであることを新しいUnityログで確認する。
6. 60秒間、静止・左右・奥／手前の歩行・停止・タップ・会話を試す。Unity SurfaceViewの実提示時刻、PSS、バッテリー温度・thermal情報、実PNG、クラッシュバッファを保存する。温度はバッテリーセンサーで、CPU温度ではない。短時間の記録だけで長時間発熱の合格にはしない。

全件EditMode／PlayModeは実行しない。旧capture-noah-final-rigやbuild-android -TownQaへ戻って繰り返さない。新規の失敗があれば、画面に出た**今回のnoah-authored run**のZIP／ログを送る。古いAPKや作成日時の新しいだけのディレクトリを代用しない。

## 再確認と復帰

表示された正確なrunディレクトリを使う。`-Mode current` は同ソース現行版へ戻す。どちらも保存を維持する。

```powershell
# RUN_DIRECTORYは今回表示されたBuilds\Diagnostics\noah-authored-...へ置き換える
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\test-noah-authored-pixel.ps1" -BuildDirectory "RUN_DIRECTORY" -Mode current -Seconds 60
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\test-noah-authored-pixel.ps1" -BuildDirectory "RUN_DIRECTORY" -Mode authored -Seconds 60
```

最初のFBは「現行より奥行きが分かるか／顔・衣装・体型が別キャラに見えないか／接地・歩行・方向転換・タップ・背景順に不自然さがないか」。良い候補になった後に同条件の600秒比較を行う。FPS・熱・メモリ・クラッシュ・タップ遅延の未計測値を成功や0に置き換えず、実機とユーザーの承認前に完成としない。
