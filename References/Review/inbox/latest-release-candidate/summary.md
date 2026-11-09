# 第8工程：Windows release preflight PASS（実ビルドなし）

状態：**完了（preflight小工程のみ）**。第7工程complete=true、第8工程complete=false。

- branch：`feat/daily-world-pass-20261016`。
- 実行source commit：`26d2f150337f00baf89771f44f6802f6f6d6402b`（`commit`欄はこの確認対象SHA。証跡共有文書自身のcommitとは区別）。
- 固定ゲームcommit：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- Windows run：`E:\Mofumachi-Validation\Diagnostics\release-candidate\rc-20261109-120851-5c06de53`。Eドライブへ保存された既存1回の結果だけを確認。再実行なし。
- 受領ZIP SHA256：`9d57a9fe3918dbd679fd17f9c98be5b0cae9bd4eb0f96d1ed5d1bea68ac64df4`。CRC正常、7ファイル、原本manifestの5 SHA一致。`status.json`と`build-info.json`は同一バイト。

| 確認項目 | 結果 |
| --- | --- |
| Windows release preflight | **PASS / PreflightPassed** |
| preflight exit code | **0** |
| 4環境変数 | **SET（実行時、同じPowerShellのProcess scope）** |
| 正式repo clean / unchanged | **YES / true**（untrackedを含む開始時cleanと終了時HEAD/statusの既存gate） |
| source_unchanged | **true** |
| Unity起動 / build / 自動再試行 | **0 / 0 / 0** |
| release profile | Development / Script Debugging / Deep Profiling / Autoconnect **OFF** |
| package / Unity | `com.mofumachi.merge` / `6000.6.4f1`の既定Editor配置確認。Android JDK/keytool実行成功。Unity本体起動なし |
| keystore open / alias | **成功 / 成功**（keytoolのPrivateKeyEntry確認） |
| certificate / keystore SHA256 | **両方とも指定公開値と完全一致** |
| first_error / runner_error / blocking issue | **null / null / なし** |
| 秘密値記録 | **false**。alias/pathはkeytoolログ内でREDACTED。private keystore・passwordは未受領・未記録 |
| ゲーム / テスト / release・QA経路 / 設定 / Packages / Visual Lock / Guard変更 | **すべてNO** |
| AAB / APK / Pixel | **すべて未実行** |

certificate SHA256：`96bb1402b8085570305a0486ae3f7fa3fafe2c60a3b8cd062c2ee8e0f81b8e09`

keystore SHA256：`faac2959a5e9ae6e5e531c110955a2085502d13da7cc96148d91066ad666b4cb`

製品固定commitから実行sourceまでのAssets/Packages/ProjectSettings/VisualLock差分は、許可済みrelease Editor入口の追加2ファイルだけ。既存QA2経路も不変。release入口identityはWindows CRLFの正確なバイトから再構成して一致した。GitのLF blobそのもののSHAとの差を製品変更と扱っていない。keytoolログのJKS形式推奨は警告のみで、キー移行・再生成はしていない。

**次にAAB実ビルドへ進んで安全か：YES（preflight前提条件が成立）**。実ビルド・成果物の署名・Pixel合格を意味しない。今回はAAB/APKへ進まず停止。PowerShellは開いたまま保持し、preflightとキー作成は再実行しない。署名バックアップは以前どおり未実施（backup_pending=true）。

原本7点と照合記録：`windows-preflight/rc-20261109-120851-5c06de53/`。原本は受領ZIPから生バイトのまま保存。固定直下にも最新build-info/build-settings/signing log/source/preflight-reviewを共有し、直下statusはレビュー集約。更新前の集約は`validation/history-26d2f15/`へ保管。過去first-error.log/failure.log/latest/error.logは以前の静的検証履歴であり、今回の失敗を示さない。

Codex側では保存済み証拠の照合と共有だけを実施。preflight・keytool・キー作成・Unity・テスト・ビルド・Pixelすべて0回。受領Windows preflight実行回数1、自動再試行0。第7工程55/444や限定releaseテストは再実行していない。

Codex／AI側から見た次の改善候補：今回はなし。

---

以下は以前の工程・準備状態の履歴。

# 第8工程：Windows release preflight実行待ち

署名準備は完了済み。同じPowerShellセッション内で、既存releaseスクリプトをBuild指定なしで1回だけ実行する指示を受領した。CodexからそのWindowsセッションを直接操作できないため、まだpreflightを実行したとは扱わない。

実行ブロック：`References/Review/inbox/latest-release-candidate/windows-release-preflight-only.md`。

コマンドは構文確認だけ実施。4環境変数の有無とkeystore SHAを確認し、既存スクリプトを1回呼ぶ。終了後にcertificate等を照合し、公開ファイルだけのZIPをEへ作成する。ユーザーはそのZIPだけを共有する。秘密鍵／passwordは含めない。

現在preflight結果・exit codeは未取得。Unity／実ビルド／テスト再実行／キー再生成は0。AAB実ビルドへ進める判定はNO（preflight結果待ち）。第8工程complete=false。成功・失敗とも、保存結果を受領してから固定共有先へ更新する。

---

# 第8工程：Windows正式署名準備完了・preflight未実行

状態：**完了（署名準備の小工程のみ）**。第8工程全体は未完了。

- branch：`feat/daily-world-pass-20261016`。
- Windows公開署名状態commit：`8c8579026b43359f240ade43fd3eaf863f354d9c`。
- 署名補助実装：`c486b37df3a7c17acfbd852e2b1a4f2d5d95bf77`。
- 固定ゲーム：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- 第7工程complete=true、release API compile PASS／34チェックPASS、署名補助26チェックPASSの以前の証拠は保持。再実行なし。

## 公開7項目の照合

GitHub共有の`signing-status.json`を取得し、以下の7項目だけであること、2つのSHAが提示されたWindows成功画面と完全一致することを確認した。原本JSONは変更していない。

| 許可された記録項目 | 共有結果 |
| --- | --- |
| keystore_prepared | **true** |
| alias_configured | **true** |
| env_vars_configured | **true** |
| certificate_sha256 | `96bb1402b8085570305a0486ae3f7fa3fafe2c60a3b8cd062c2ee8e0f81b8e09` |
| keystore_sha256 | `faac2959a5e9ae6e5e531c110955a2085502d13da7cc96148d91066ad666b4cb` |
| backup_pending | **true** |
| secrets_recorded | **false** |

共有commitの親は2f29b07。変更pathはこの公開JSON1個のみ。秘密鍵本体・password・source変更を含めていない。Codexはprivate keystoreを取得・開封していない。

実作成のpath／alias名／sizeはユーザー画面で確認し、指定どおり実署名記録としてGitへ追加するのは上記7項目に限定した。パスワードのマスク入力数や長さも記録していない。

## 次へ進める範囲

**Windows release preflightへ進んで安全か：YES（同じPowerShellセッションでのpreflight専用確認まで）**。

4環境変数は成功時点のProcess scopeだけ。画面を開いたまま保持し、キー作成CMDを再実行しない。閉じた場合は新しいキーを作らず、既存キーに対する環境変数の再設定を別途行う。

バックアップは未実施のまま。外部媒体／クラウドへのキーコピーは行っていない。backup_pending=trueを保持する。

今回は公開結果の照合だけで、release preflight／AAB／APK／Pixelは未実行。次の実ビルドsource HEAD、ビルド後のsource不変、正式署名による成果物、実機合格はまだ確定しない。公開署名状態のcommitと、将来の実ビルドsource commitを混同しない。

**第8工程 complete=false**。preflight以降へ自動で進んでいない。

## 今回のレビュー操作

Fetch1回で共有commitを取得。公開JSON1pathだけのincoming差分を確認後、証跡だけをfast-forward同期した。キー作成・キー検証・password入力・テスト・Unity・ビルドは全て0回。自動再試行0。ゲーム／既存release・QAビルダー／設定／Visual Lockは変更なし。

更新前の準備待ち記録は`validation/history-2f29b07/`へ保存。第7工程・34チェック・以前のfailure原本も保持。今回のfirst_error／runner_errorはnull。古いfailure.logやlatest/error.logは今回の署名失敗を示さない。

固定共有：`References/Review/inbox/latest-release-candidate/`。commit欄は今回レビューした公開証跡SHAを指し、レビュー文書自身のSHAとは区別する。

Codex／AI側から見た次の改善候補：今回はなし。
