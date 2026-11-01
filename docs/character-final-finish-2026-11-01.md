# 2.5D最終仕上げ：2026-11-01

実装済み・新版Unity実画面／Pixel未確認。正式固定は結果確認後。

今回の最新指示を優先し、ノアで接続計算を修正してから同じ処理を残り5名へ適用した。元の分割原画・登録位置・衣装・色・体型・街・Merge・フォント・Core／保存形式は維持。新たなリグ方式、画像生成、フル3D化は行っていない。

重点の純粋C#検査58/58成功。通常・Android・Editor描画・PlayMode・EditorのAPIコンパイル成功。最終レビューで見つかった撮影2点と実ポインター連打1点を修正し、実押下→離す→クリックの回帰検査を追加した。APIコンパイルをUnity実描画成功とは扱わない。検査原本とレビュー判断は `docs/verification/character-final-finish-2026-11-01/`。

|対象|修正|重点検査|
|---|---|---|
|頭／胴体|背面の頭を胴体の首接続点へ追従。頭の傾きは顎の接点を中心にし、正面は正式通常原画＋既存まばたきを維持|各方向・歩行／停止／会話／挨拶で接点と頭の剛性を検査|
|背面／方向転換|移動差分の小さな揺れで毎フレーム反転しない方向ヒステリシスと約0.09秒の安定判定。停止時は向きを保持|北向き、斜めの境界、瞬間的な逆差分、停止・同時刻再描画|
|すれ違い|既存の回避量を約0.16秒で連続的につなぐ。経路計算・歩行可能面・ハード距離・前後Sorting基準は維持|6名の実モデル正面すれ違い：角度の最大変化約56.5度→6.5度、逆走0、全員到着、最短距離約0.111（既存下限0.067以上）|
|会話|接近・待機・休憩到着で自動表示しない。全6名のキャラタップで表示、同じ会話の表示中／短い連打は重複しない|タップゲート、自然遭遇の無言、各キャラの明示クリック、名前一致・既存保存不変|
|視認／操作|会話枠を入力非遮断にし、キャラ矩形との重なりが少ない位置へ置く。移動中・会話中の足元名前枠を控える|短い画面の枠／タップ。文字・フォント・街レイアウトは変更しない|

タップが近くの相手への挨拶を起こす場合も、起点は明示タップだけ。お出かけ・施設の既存APIは維持。1日1回の新しい保存制御は追加せず、既存の日常会話記録だけを使う。

## 変更ファイル

製品表示／動作：
- Assets/Mofumachi/Presentation/NoahAuthoredMotion.cs
- Assets/Mofumachi/Presentation/NoahAuthoredGraphic.cs
- Assets/Mofumachi/Presentation/CharacterAuthoredGraphic.cs
- Assets/Mofumachi/Presentation/TownCharacterFacing.cs（新規）
- Assets/Mofumachi/Presentation/TownCharacterTapGate.cs（新規）
- Assets/Mofumachi/Presentation/TownActor.cs
- Assets/Mofumachi/Presentation/TownLifeView.cs
- Assets/Mofumachi/Presentation/TownWorldWalkModel.cs

検査／撮影／実行：CharacterFinalFinishTests.cs、CharacterPassingTests.cs、TownHabitTests.cs、ExperienceCaptureTests.cs／NoahAuthored／CharacterAuthored／CharactersSix／CharacterFinish、Mofumachi.Visual.Tests.csproj、CharactersRolloutBuild.cs。scriptsのcharacter-finish-scope.ps1／検査、prepare-noah-pixel-preview.ps1、capture-character-authored.ps1、prepare-characters-pixel-preview.ps1、prepare-characters-final-pass.ps1、noah-reprototype-review.ps1。新規Assetsファイルのmetaと文書・原本受領記録を含む。

従来のノア承認manifestと旧552保護manifestは原本のまま。最新の修正指示で許可されたコード5点だけをcharacter-finish-amendment-2026-11-01.jsonで元SHA→今回SHAとして明示。原画／登録JSONのoverrideは検査が拒否する。修正の許可と、完成版の見た目承認は区別する。

## 6名Before／Afterとすれ違い

Windowsの最終実行で、ノアの実描画検査を先に通し、同じ版で5名を順番に確認。その後、全件各1回、6名の正面・左右・背面と、旧2.5Dの頭の配置／修正後の頭の配置を同じ素材・同じポーズ・同じ街で比較撮影する。12組の6名同時比較、6名のタップ会話、実モデルのすれ違い連続標本をsix-authoredへ保存し、finish-review.htmlから参照できる。Editor専用Before比較スイッチはAndroidビルドに入れない。

比較は頭の接続の修正を対象とする。実歩行・停止・方向・まばたき・前後関係は個別native方法、すれ違いとタップは6名方法でも確認する。連続標本は固定シミュレーション刻みであり実機FPS動画ではない。現時点で新しいUnity画像を取得済みとは主張しない。

## 最終実行

Unityを閉じ、Pixelを接続・ロック解除して以下をまとめて実行する。古い「更新不要」「比較版を先に表示」は今回の製品変更には適用しない。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mofuFinishGit = Find-SourceGit
    $mofuFinishBranch = & $mofuFinishGit branch --show-current
    if ($LASTEXITCODE -ne 0 -or $mofuFinishBranch.Trim() -ne "feat/daily-world-pass-20261016") { throw "作業ブランチを確認してください。" }
    & $mofuFinishGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止します。" }
    & ".\scripts\prepare-characters-final-pass.ps1" -Install -Serial "99RAY1BELH" -MeasureSeconds 600
}
```

今回は最初に今回の6名2.5D版をPixelへ入れる。比較用の旧2Dを先に表示しない。街が見えたらPCでEnter、10分間、6名の歩行／停止／前後左右／すれ違い／タップ会話を確認する。FPS・メモリ・熱・クラッシュ記録と体感回答、再起動保存確認を採る。頭／胴／足の接続、すれ違いの高速ブレ、自動会話なし・タップ会話・連打時の非重複も個別回答する。6名同時を見られなかった場合はunknownで記録し、Unity fixtureで実機6名の証拠を代用しない。

終端で開く今回のfinal-review-review ZIPを全パート添付する。失敗時は、その新runのZIP／エラーだけを確認し、全件を自動反復しない。保存消去・uninstall・main統合なし。

残る確認は、新版のUnity実画面、Pixelでの自然さと負荷、結果を受けた正式固定の3点。合格前のformal_fixed/adoptedはfalse。

判断：同じ会話が表示中なら連打を無視し、閉じた後の再タップは許可する。1日制限の保存項目は増やさない。Pixelは今回版を先に入れ、旧版との負荷差の改善は未判定。レビューの軽微な留保として、履歴manifestとamendmentを同時編集する改変に対する独立署名はない。今回はGit基準との差分とSHAで原本不変を確認し、改変不能な証明とは呼ばない。詳細は上記review.md。
