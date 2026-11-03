# Merge正式候補：Visual合格後のリリース候補確認

最新：失敗8件は実Unityで8/8合格。全件の再実行なしで[QA APK・Pixel確認](merge-reward-reviewed-pixel-2026-11-03.md)へ継続する。以下の`-Final`は現在再実行しない。

最新：全件初回まで進み、重点53/53・Edit144/144・Play252/260となった。現在は`-Final`を再実行せず、[失敗8件だけの確認](merge-reward-failed-eight-2026-11-03.md)へ進む。以下の一括実行手順は初回の履歴として保持する。

ユーザーが現行ピンク「ごほうびMerge」のVisual Gateを合格と判定。表示の正式候補は`af084ecda712d7aaaf3b4b581496a44f1192f97c`で固定し、以後fix only。今回の変更にゲーム製品コード・素材の差分はない。承認記録は`References/VisualLock/merge-reward-visual-approved-2026-11-03.json`。元PNGと表示6ソースのSHAを起動前・終了後に照合する。

**現在の状態：ユーザーVisual合格。Windows初回重点は53件中52成功・1失敗。全件EditMode／PlayMode・QA APK・Pixelは未到達。** 失敗は旧演出テストの120頂点上限（承認済み現行は最大313）。製品を変えずテストのみ訂正し、native再実行結果を待つ。クラウドには有効なUnityライセンス・Androidモジュール・USB実機がないため、以下をWindowsで実行する。合成検査を実UnityやPixel合格へ置き換えない。古い候補manifestの未検証フラグは履歴として保持する。

## 一度だけ実行

今回の更新後に新runを1回実行する。前回は重点段階で停止したため、全件EditMode／PlayModeは今回それぞれ初回となる。旧run `merge-reward-20261103-105456-4aec62dd`のファイルや結果フラグは変更しない。

Unity Editorを閉じ、Pixel3aをUSB接続し、画面ロック解除・USBデバッグを許可する。PowerShellへ次をまとめて貼り付ける。Unity Hubでの操作は不要。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -Command {
    $ErrorActionPreference = "Stop"
    Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
    . ".\scripts\build-android.ps1"
    $mergeGit = Find-SourceGit
    if ((& $mergeGit branch --show-current).Trim() -ne "feat/daily-world-pass-20261016") {
        throw "作業ブランチが異なるため停止しました。"
    }
    & $mergeGit pull --ff-only origin feat/daily-world-pass-20261016
    if ($LASTEXITCODE -ne 0) { throw "更新に失敗したため停止します。" }
    & ".\scripts\prepare-merge-reward-preview.ps1" -Final -Install -Serial "99RAY1BELH" -MeasureSeconds 180
}
```

順序はMerge重点確認→EditMode全件1回→PlayMode全件1回→同一ソース／同一素材のQA APK→既存街QAアプリへの`install -r`→Pixel計測。途中失敗で停止し、自動再試行しない。既存キャラ素材の一時準備は必要だが、キャラ単体テストは実行しない。アンインストール・データ初期化・セーブ書換えは行わない。

インストール後、Pixelの同じQAアプリでMerge画面を開く。PC側の`When the Merge board is visible, press Enter...`が出たらEnter。その後3分、通常の範囲でお茶／パン／花の生成、選択・ドラッグ、Merge／Lv.UP、依頼確認・納品を操作する。満杯／不正Mergeはその状態を自然に確認できる場合に確認し、既存進行を初期化して作らない。

## 結果と再開

冒頭に表示される`Merge-only candidate run: ...`が今回の正確な結果フォルダ。`merge-reward-source-info.json`に重点／全件／APKの結果と元XMLのSHA、`screenshots`に11JPEG・JSON・HTMLを残す。子ビルダーの`tests_run=false`はビルダー自身がテストをしないという意味で保持し、親のXML結果と分ける。

Pixel結果は同フォルダ内`pixel-日時-ID.zip`と、その原本フォルダ。実Unity SurfaceViewのFPS／p95間隔・PSS・バッテリー温度差・thermalservice・実機PNG・クラッシュバッファを記録し、インストール済みAPKのSHAと実行中PIDの継続を照合する。温度はバッテリーセンサー値でありCPU温度ではない。クラッシュログには過去分も含まれるため、`started_utc`／PID／パッケージを見て該当試験を判定する。

計測完了はVisual／入力／発熱／クラッシュの合格判定ではない。見た目の崩れ、タップ・ドラッグ遅延、カクつき、体感発熱、クラッシュ、再起動後の既存進行を実機で確認する。`pixel_verified`／`release_accepted`は結果レビューまでfalse。未確認の項目は未確認のまま残す。

**実機接続・計測だけ失敗した場合、上の`-Final`を再実行しない。** 正確な完了フォルダを指定して、以下のPixel継続だけを実行する。`C:\...\merge-reward-日時-ID`を先に表示された実際のパスに置き換える。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "C:\Users\User\Desktop\Mofumachi\mofumachi-merge\scripts\test-merge-reward-pixel.ps1" -BuildDirectory "C:\...\merge-reward-日時-ID" -Serial "99RAY1BELH" -MeasureSeconds 180
```

この継続は元の3XML・撮影実ファイル・APK SHAを再検証してから同じAPKをインストールする。Unityテスト・ビルドの再実行は0回。重点／全件自体が失敗した場合は新runのログ・XML・JSONを保存し、該当箇所の診断へ進む。全件を反復して成功扱いにしない。

今回の改善候補：なし。承認済みの表示と既存ロジックを保持する。
