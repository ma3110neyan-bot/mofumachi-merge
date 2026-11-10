# 第8工程：File.Replace空パス修正・source guard限定42/42 PASS

状態：**関連テスト合格（この小工程完了）／Windows実AAB未確認**。branch：`feat/daily-world-pass-20261016`。実装commit：`604b5f7fd8442aaf97fc695d0406acdaa6c1bec2`。commit欄は実装SHAで、共有文書自身のSHAとは区別する。第7工程complete=true、第8工程complete=false。

## 修正と実測結果

File.Replaceのbackupへnullを渡す呼出を、同じrun Diagnostics配下の一意な実パスへ変更した。payload/target/backupの境界・存在・非空・既存backup拒否とSHAを事前検査し、targetがsnapshot SHAへ戻ったこと、backupが復元前target SHA/sizeと一致することを記録する。backupは診断証拠として保持し、任意のbackup削除は今回は行わない。成功時payloadはFile.Replaceで消費されるが、失敗後にbackup/payload/targetを追加修正・削除する処理はない。

限定テストを**1回だけ**実行し、**Total42 / Passed42 / Failed0 / exit0 / 170 checks**。Linux PowerShell7.4.6＋実Gitの合成detached worktreeで検証。Windows実Unity／AAB／APK／preflight／署名ツール／Pixel／第7工程／既存release34テストの再実行は全て0。前回の1件失敗は、この明示許可された別の1回で上書きせず履歴に保持し、自動再試行0。

| 確認項目 | 結果 |
| --- | --- |
| File.Replace空パス修正 | **YES** |
| backup path非空／Diagnostics内 | **YES／YES**（実fixture3件、production rootは従来どおりE） |
| known mutation byte-perfect復元 | **PASS** |
| target snapshot SHA／backup復元前target SHA | **一致／一致** |
| 空/null／外部／既存backup拒否 | **全PASS** |
| backup改ざん・payload/target SHA不一致拒否 | **全PASS** |
| unknown tracked・non-hex・unknown untracked拒否 | **全PASS** |
| .utmp exact生成物／exact Gradle終了log cleanup | **PASS／PASS** |
| cleanup後完全source guard／root .vsconfig保持 | **PASS／PASS** |
| 復元SHA異常・cleanup失敗・Git取得異常拒否 | **全PASS** |
| formal repo保護・リンク／hardlink保護 | **PASS** |
| ゲーム・Visual Lock・設定原本・URP原本・Packages・QA・release C# | **変更なし** |
| 保護対象hash | **862 path不変**。許可したPS2件のみ変更（全864検査） |
| Guard弱化／完全判定の変更 | **NO／NO** |
| Unity／AAB再ビルド／APK／Pixel／preflight／署名操作 | **0／0／0／0／0／0** |
| 自動再試行 | **0** |

最初のpositive fixture3件のbackupが実在し、size・SHA・target/snapshot SHA・Diagnostics境界をテスト後にも読み取り再照合した。restore-backup-validation.jsonとsource-guard-backup-fix/positive-caseの関係記録を参照。backupの実体・snapshot binary・AAB本体はGitに追加せず、ローカル/tmp fixture内に保持。Windows Eでの実動作を実測したという意味ではない。

変更ソースはscripts/build-android-release.ps1とscripts/tests/android-release.tests.ps1の2件だけ。変更関数はRestore-AndroidReleaseSnapshotFileとその呼出元に限定。完全source guard、許可field、known generated判定、その他production関数はテキスト不変を構文レビューで確認した。

## 判定と停止地点

**AABを1回だけ再実行する準備が整ったか：YES（この小工程の限定検証範囲）**。**第8工程complete=false**。今回はAABを起動せず、次の明示指示を待つ。旧AAB貼付ブロックは旧builder identity固定の履歴であり、そのまま再利用しない。実行指示時に新commit対応の手順へ切り替える。

元のWindows AABがsource guardで失敗し署名未検証である事実は変わらない。今回のunit検証を、既存AABのsource不変や実artifact署名合格へ流用しない。非hexのiは今回42ケースでも拒否を確認し、known mutationに追加していない。実Unityが未知変更を残せば復元・cleanup前に停止する。

保存先：source-guard-fix-validation.json、source-guard-fix-tests.log、changed-files.json、protected-source-hashes.json、restore-backup-validation.json、source-guard-backup-fix/の原本結果。以前の失敗集約はvalidation/history-75b05a1/、失敗原本はsource-guard-fix/に保持。直下first-error.log/failure.log/latest/error.logは前回0f1b066の失敗履歴で、現在のfirst_errorはnull。

Codex／AI側から見た次の改善候補：今回はなし。

---

以下は以前の失敗とAAB履歴。現在の判定は先頭の42/42 PASS。

# 第8工程：source guard修正草案・限定テスト1件目で停止

状態：**原因確定／修正未完了**。実装commit：`0f1b066c390e61c739d4fc9ed961e14aa5de7120`。branch：`feat/daily-world-pass-20261016`。commit欄は確認対象の実装草案で、共有文書自身のSHAとは区別する。第7工程complete=true、第8工程complete=false。

既存AABの生成成功／source guard停止／未署名検証の原本はwindows-aab配下に不変で保持。今回はAABを実行していない。

## 今回の最初の失敗

source guard専用テストをLinux PowerShell7.4.6＋実Gitの合成worktreeで**1回だけ**実行。**実行1件 / Passed0 / Failed1 / exit1**。残りのケースは未実行。全件・既存release34・第7工程・Unity・preflight・AAB・APK・Pixel・署名ツールは実行していない。

最初のcase：`known three-file mutations restore exact snapshot bytes and final guard passes`。

根本エラー（保存済みrecovery-result.json）：

```text
Exception calling "Replace" with "3" argument(s): "The value cannot be an empty string. (Parameter 'path')"
```

`scripts/build-android-release.ps1:259`の`File.Replace(payload,target,$null)`で発生。payload／targetは存在し、payload SHAはsnapshot0と一致。空のパス引数で復元API呼出が失敗した。wrapperのKnown mutations rejectedというthrowだけを原因にしていない。復元・削除成功は**0件／0件**。snapshot3件のSHAとテスト正式repoのbyte一致・Git cleanを失敗後に読み取り確認した。

**「1回目の結果で停止」指定に従い、追加コード修正・再テストは行っていない。** 次の最小候補はFile.Replaceのbackup引数へ同じDiagnostics内の一意で空でない実パスを渡すこと。これは候補の記録だけで未適用。別途許可された次の修正工程が必要。

## 変更範囲と検査状況

| 項目 | 結果 |
| --- | --- |
| 実装変更 | **scripts/build-android-release.ps1 / scripts/tests/android-release.tests.ps1 の2件のみ** |
| 通常の完全source guard | **本体テキスト不変**。無効化・path ignoreへの置換なし |
| 設計 | prebuild3 byte/SHA snapshot → 全tracked/untracked検査 → 許可fieldのみの検査 → E限定byte復元／既知生成物削除 → 通常guard |
| known mutationの復元全体 | **FAIL**。最初の合成caseで内容検査は通過し、復元I/Oで停止 |
| unknown mutation拒否 | **未実行**。後続caseへ進めていない |
| cleanup限定scope／cleanup後clean | **未実行／未実行** |
| formal repo保護専用case | **未実行**。失敗後の合成正式repoはbyte/Git不変を確認 |
| 保護対象hash | **862 path不変**。許可したPS2件だけ変更（全864検査） |
| ゲーム／設定原本／URP原本／Packages／Visual Lock／既存QA／release C# | **変更なし** |
| Guardを弱めたか | **NO**。最終完全guardは不変で、復元失敗をpassedにしていない。新機能の全安全case合格を主張しない |
| Unity／AAB再ビルド／APK／Pixel／preflight／署名操作 | **全て0** |
| 自動再試行 | **0** |

## 実auditの取扱い

ユーザーaudit報告の実内容差分は3件：Assets/Settings/UniversalRP.asset、Assets/Settings/UniversalRenderPipelineGlobalSettings.asset、ProjectSettings/ProjectSettings.asset。実audit ZIPは未受領。未追跡56件（.utmp55＋exact Gradle停止log1）は以前のAAB原本で照合済み。

共有されたAndroid graphics値`0b000000i`の末尾iは、ユーザーも原本にあるか不明と回答した。**非16進数値をknown mutationとして許可しない**。同じOpenGLES3値の表記差だけに限定し、non-hex／別API／非空keystore bindingは拒否するcaseを作成したが、そのcase自体は今回未実行。実keystore path/passwordを要求・保存していない。

## 現在の判定

**AABを1回だけ再実行する準備が整ったか：NO**。主要ブロッカーは限定テスト最初のsnapshot復元API失敗。**source guard修正complete=false／第8工程complete=false**。再ビルドへは進まない。

保存：source-guard-fix-validation.json、source-guard-fix-tests.log、changed-files.json、protected-source-hashes.json、およびsource-guard-fix/配下の原本テスト結果・呼出記録・失敗case診断。fixtureのbyte snapshot／未使用payloadはローカル/tmpに保持し、原本SHA検証結果を共有。AAB本体・キー・秘密値はGitへ追加しない。

Codex／AI側から見た次の改善候補：今回はなし。未完了箇所は上記の最小修正候補として分離した。

---

以下は以前のAAB結果と準備履歴。

# 第8工程：AAB生成済み・source guard停止（署名検証未到達）

状態：**原因確定（全体判定の停止条件）／実機未確認**。Unity内AABビルドは成功。全体検証はsource guardで失敗。第7工程complete=true、第8工程complete=false。

- branch：`feat/daily-world-pass-20261016`。
- AAB実行source：`c0803e6e9faf0ebb5e63e637d94c20dcce3f6e4a`。固定ゲーム：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。commit欄はこの実行sourceで、証跡共有文書自身のSHAとは区別。
- run：`rc-20261109-123540-725e4e31`。E側保存済み証跡だけを確認。再実行・修正なし。
- JST実行区間：2026-11-09 21:35:40 ～ 2026-11-10 11:02:41（全体約13時間27分）。Unity PlayerBuildInfoは1,341,831ms（約22分22秒）。長時間の根本原因は全ログ未受領のため未確定。ネットワーク／待機チェーン画像だけから原因を断定しない。
- 受領ZIP SHA256：`9a2809ae4f8c2dc28a240830bf3adca849c1c978d9018992e8b8fe01aea9800c`。CRC正常。公開manifest27項目一致。native manifest受領23項目一致。元build.log4,163,104bytesは未収録で、末尾800行と原本SHA/sizeだけ受領。

## 確定した停止理由

after/source-check.jsonの**working_diff_exit=1**で、追跡済み内容差分を検知。index_diff_exit=0、HEADはbaselineと一致。加えて非許可の未追跡56件（`.utmp/`55件とGradle終了ログ`build/outputs/logs/unity---stop-build.log`1件）。`.vsconfig`は既存allowlistどおり記録済み。

追跡済みの変更path一覧・差分本文はZIPに未収録。stderrのUniversalRP.assetに関するLF/CRLF警告だけで、**改行だけの誤検知とは判定しない**。このwarningを変更pathの全容とも扱わない。Gradle終了ログの生成はUnity末尾でも確認。55件はpath上CMake/NDK/Gradle中間生成物だが、個別内容は未受領。ignore/allowlistを勝手に変更していない。

Build Successful、MOFUMACHI_RELEASE_SUCCESS、正常batchmode終了exit0、native result=Passed/build_errors=0を確認。停止操作を案内後に受領した結果だが、実際の終了方式は未確認で、Unityクラッシュ／強制終了失敗とは断定しない。

| 確認項目 | 結果 |
| --- | --- |
| AAB全体検証 | **FAIL（source guard）** |
| Unity内AABビルド | **PASS / build_errors=0** |
| AABビルド／Unity起動 | **1回／1回**（AssetImportWorkerを追加ビルドに数えない） |
| Unity exit／wrapper exit | **0／1** |
| AAB file name | `mofumachi-merge-rc-20261109-123540-725e4e31.aab` |
| AAB size | **66,555,568 bytes** |
| AAB SHA256 | `cda1443b40d3337e80f9139e3137de9efe96ddeca1923ee9551e8af5a527162c`（Windows側Get-FileHash。AAB本体未受領・Git未追加） |
| actual signer SHA／一致 | **未検証／NO（未判定）**。source guardが検証の前で停止。元のsigner_matches=falseは署名不一致の証拠ではない |
| 要求されたcertificate | `96bb1402b8085570305a0486ae3f7fa3fafe2c60a3b8cd062c2ee8e0f81b8e09`（設定snapshotでありartifact signerの検証結果ではない） |
| Development／Script Debugging／Deep Profiling／Autoconnect | **すべてOFF** |
| BuildOptions／custom signing／build_app_bundle | **None／true／true** |
| package／version／versionCode | **com.mofumachi.merge／1.0／1** |
| backend／architecture／orientation | **IL2CPP／ARM64／Portrait** |
| 一時設定復元 | **true（native snapshot）** |
| source_unchanged | **false（検証worktree）** |
| formal_repo_unchanged | **true（正式C repo）** |
| keystore SHA一致 | **true** |
| Visual Lock変更 | **正式repo・このレビューではNO**。検証worktreeの追跡済み全diffは未受領 |
| 自動再試行 | **0** |
| first_error／runner_error | **null／Tracked content or non-allowlisted files changed after the release build.** |
| APK／Pixel | **未実行／未実行** |
| 第8工程complete | **false** |

AAB absolute path：`E:\Mofumachi-Validation\release-candidate\rc-20261109-123540-725e4e31\mofumachi-merge-rc-20261109-123540-725e4e31.aab`。artifact更新UTC：`2026-11-10T02:02:15.9307674Z`。

原本aab-review.jsonのaab_build_invocations=nullは未変更（全体合格しない場合wrapperが未設定にするため）。レビューでは単一PlayerBuildInfo、native snapshot、Unity1回の証跡を合わせ、生成1回を記録。

**次にAPK実ビルドへ進んで安全か：NO**。主要ブロッカーは**検証worktreeのsource不変条件未成立**。再ビルドを保留し、既存worktreeの実diffを読む。署名検証未到達はこのgateで止まった結果で、新しい署名不具合を検出したという意味ではない。

読み取り専用手順：`References/Review/inbox/latest-release-candidate/windows-release-source-audit-only.md`。元のPowerShellの入力待ち画面で1回実行すると、HEAD/status/実diffだけを別E診断フォルダーへ保存して公開ZIPを作る。Unity／preflight／AAB／APK／Pixelを起動せず、旧run/receipt/sourceを修復・削除・上書きしない。Windows採取はまだ未実行。

原本28点とレビュー：`windows-aab/rc-20261109-123540-725e4e31/`。ZIPのbackslash項目名を保存用slashに対応付けただけで、生バイトは保持。更新前集約はvalidation/history-c0803e6/へ保管。過去preflight PASS・第7工程・署名準備の証拠は不変。

今回CodexでのUnity・ビルド・テスト・署名ツール・キー作成・プロセス停止・Fetchは全て0回。ゲーム・既存release/QA入口・設定・Packages・Visual Lock・Guard変更なし。秘密値とAAB本体は共有しない。

Codex／AI側から見た次の改善候補：今回はなし。

---

以下は以前の実行準備・preflight結果の履歴。

# 第8工程：実AABのみ1回・Windows実行待ち

状態：**未着手（実AAB生成）**。既存preflightはPASS、第7工程complete=true、第8工程complete=falseを維持。

- branch：`feat/daily-world-pass-20261016`。
- 既存preflightの共有確認commit：`3e93a338b6ae582635620cc8fbbfdceaa99f7cbe`。preflight実行source：`26d2f150337f00baf89771f44f6802f6f6d6402b`。
- 固定ゲーム：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。AAB実行source HEADはWindows側が実行時に記録する。
- 既存`scripts/build-android-release.ps1 -Build -Format Aab`を1回だけ呼ぶ同一セッション用手順：`References/Review/inbox/latest-release-candidate/windows-release-aab-only.md`。
- 新しいPowerShell/CMDを開かない。単独preflightやキー作成は再実行しない。既存ビルド入口内のチェックとattempt receiptを保持する。
- 既存release/QA入口、ゲーム、テスト、設定、Packages、Visual Lockは変更なし。今回の変更は公開手順と共有記録だけ。
- 現在、実AAB・Unity・APK・Pixel・追加preflight・キー作成はいずれも0回。AAB path/size/SHA/signer/build exitは未取得。PASSを先取りしない。
- 成功・失敗ともE側原本を保持。公開ZIPは小容量の許可済み証跡だけを収録し、AAB本体・keystore/passwordをGitやチャットへ追加しない。
- **次にAPK実ビルドへ進んで安全か：NO（AAB実行結果待ち）**。AABが合格しても自動でAPKへ進まない。
- ブロッカー：既存Windows PowerShellからの1回の実行と公開結果の受領待ち。新しい製品不具合を検出したという意味ではない。

以下のpreflight PASS原本と以前の結果は変更せず保持。直下build-info/build-settings/sourceは引き続き最後の実行済みpreflight証跡であり、未実行AABの結果ではない。

---

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

---

# 第7工程の確定記録（変更せず保持）

# 第7工程：v5実Unity検証完了・第8工程へ着手可能

- 更新：2026-11-09（日本時間）。状態：**完了（第7工程の指定検証範囲）**。
- branch：`feat/daily-world-pass-20261016`。
- 固定ゲーム実装：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- v5ランチャー：`9ee98de7e58eb75cc624c9f30cc055686afebd2a`。
- 正式証跡commit：**`19ddb34ed35838affc191ae98b18e67900613be7`**。
- validation_id：`progress-ui-e-drive-v5`。
- 対象run：`progress-ui-integration-progress-ui-e-drive-v5-20261109-101604-63c260c0`。

## 結論

**第7工程 complete=true**。

**第8工程「Pixel 3a実機確認＋リリース候補ビルド」へ進んで安全か：YES**。

既存Windows実Unity結果のJSON、両XML、source-check、checkout記録、source revision、共有manifestを照合し、以下の全条件を満たすことを確認した。検証を再実行していない。

| 必須条件 | Phase1 direct | Phase2 integration |
| --- | --- | --- |
| Total | 55 | 444 |
| Passed | **55** | **444** |
| Failed | 0 | 0 |
| Skipped | 0 | 0 |
| Inconclusive | 0 | 0 |
| Unity exit code | 0 | 0 |
| source_unchanged | **true** | **true** |
| validation_passed | true | true |
| Unity起動 | 1回 | 1回 |
| 自動再試行 | 0 | 0 |
| 追跡ファイル／欠損 | 7209／0 | 7209／0 |
| HEAD／clean／登録 | 固定commit一致・正常 | 固定commit一致・正常 |

Root result=Passed、validation_passed=true、source_unchanged=true。Unity起動合計2回、自動再試行0。first_error／runner_error／first_failed_phaseは全てnull。ソース不変判定でv4を止めていた箇所はv5の両Phaseで通過した。

## 実XMLとfixture確認

Phase1は`Mofumachi.Tests.ProgressUiIntegrationTests`のみ55ケース。全fullnameが一意で、全ケースPassed。

Phase2は下記の正式9fixtureだけで444ケース。全fullname一意、追加fixture・重複・欠落なし。

| fixture | Passed |
| --- | --- |
| SaveServiceTests | 33 |
| PreferencesTests | 23 |
| CharacterPreferenceTests | 7 |
| MofuPointServiceTests | 68 |
| GenerationCostTests | 33 |
| TownPointRewardTests | 69 |
| RecoveryExtraHelpTests | 71 |
| QuestTownLevelTests | 85 |
| ProgressUiIntegrationTests | 55 |
| 合計 | **444** |

Phase1とPhase2内の同じ55ケースを、別の新規ケースと数えていない。指定の2段階検証が成功したという判定。

## ソース・実行条件の照合

両Phaseのsource-checkで、固定baselineとHEADが46f65b2に一致し、working diff／index diff／untracked取得の終了コードが全て0。problemsとdisallowed_untrackedは空。

実際のコマンド引数も確認し、全追跡パスを比較し、空白無視や広いパス除外が追加されていないことを確認。許可された未追跡は両Phaseともrootの`.vsconfig`だけ。

両Phaseの実Unity引数はUnity6000.6.4f1、Android target、EditMode、Burst無効化に一致。checkout gateも終了0、固定HEAD、clean、正常登録、tracked7209／missing0。

正式repoはCの`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`、検証・Library・TEMP/TMP・UPM cache・Diagnostics・共有worktreeはEの運用を維持した。今回ゲーム・テスト・ランチャー・Visual Lock・Guardを変更していない。ソース不変比較はVisual Lock原本を含む全追跡パスが対象。

## 証跡の完全性

`run-info.json`に記録された**43ファイル**の長さとSHA256を共有原本と照合し、全て一致。source-revision.txtも固定commitと一致。共有commitの親とmanifestのremote_parentも一致し、commit範囲はinboxだけ。

固定共有先：

`References/Review/inbox/latest-progress-ui-integration/`

- rootのprogress-ui-integration-result.json／source-revision.txt／run-info.json。
- direct/results.xml、integration/results.xml。
- 各source-checkと比較exit／stderr。
- 各checkout stdout／stderr／exit／validation。
- 保存されたUnityログと旧履歴。

原本JSON・XML・ログを編集して成功扱いへ変換していない。旧C／v1／v2／v3／v4とobservationsは保持。以前の合格や一時確認データを今回のXMLへ流用していない。

原本の`review_required=true`、`stage8_safe=false`は、ランチャーが証跡レビューを待つための値。その原本は変更せず、**今回の照合完了後の判定をlatest/status.jsonのnext_stage_safe=trueへ記録**する。

## 今回行った操作

Fetch1回で共有済み証跡を取得。既存inbox証跡commitだけをfast-forward同期し、保存原本を読み取り照合した。今回の変更はsummary/statusだけ。

- 新しいCMD／Unity／テスト実行：0。
- 自動再試行：0。
- ゲーム／テスト／ランチャー変更：なし。
- Windows原本／receipt変更：なし。
- reset／stash／force push：なし。
- APK／Pixel／Visual撮影：未実施。

## 次の工程

第8工程へ**着手可能**とする。Pixel 3aの表示・操作確認、リリース候補ビルド、正式リリース判定を今回済ませたとは扱わない。第8工程は自動開始していない。

Windows CMDの「続行するには何かキーを押してください」は検証・共有成功後のpause。キーを押してこのCMDを終了してよい。検証CMDの再実行は不要。

`commit`は既存ランチャー、`implementation_commit`は固定ゲーム、`evidence_commit`は正式Windows証跡、`review_base_commit`は照合した証跡先端を示す。共有文書自身のSHAと区別する。今回の追加改善候補なし。
