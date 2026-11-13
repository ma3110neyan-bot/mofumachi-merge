# 第8工程：ProjectSettings完全一致transition recovery・限定検証PASS

branch：`feat/daily-world-pass-20261016`。実装base：`2d20b012433033446895ba272fd3bf06afcf1416`。確認日：2026年11月13日（JST）。
`commit`欄は比較baseであり、この報告自身の共有commit SHAではない。今回の検証済み実装は同梱ファイルSHA256で特定し、共有commitはこの報告を含むGitHub commitとして確認する。ユーザー指定の通常commit/push各1回を守り、自己SHA追記のための追加commitは作らない。

**状態：関連テスト合格。変更されたProjectSettings全体のbefore/after SHA・size・diff fingerprintが完全一致した場合だけ、snapshotからbyte-perfect restoreする。実Unity/AABでのrecoveryは未確認。第8工程complete=false。**

## 完全一致条件

| 対象 | SHA256 | size |
| --- | --- | --- |
| before snapshot | `d334127167318fe7278575eb17fe2621ab2c0e39c3ab2c53b7c1f39cc555e7fc` | 24343 |
| after physical file | `266f17baf9d7101401580553aab9d0ede0390f6c859e137e31f6274d4713f07a` | 23513 |

保存するcanonical diff SHA256：`06237adb85a8490fbe6ea1f32b59fdbf8f0548cdbc706d9cd1d6d15a8e87b596`。
beforeはCRLF846改行、afterはLF846改行。全847 split行を比較し、変更行が次の2行だけであることを要求する。

- 277行：AndroidKeystoreNameが空から`'{inproject}: '`へ変化。
- 439行：Androidブロックのm_APIsが`0b000000`から`0b000000i`へ変化。直前のAndroid target行と直後のautomatic=0も固定。

ProjectSettingsの物理bytesが変わったら必ずこの照合を通し、未知SHA/size/diffを旧正規化比較へフォールバックさせない。`i`の一般値許可、suffix削除、regex修復は追加しない。unchanged入力とUniversalRPの既存比較はそのまま。ordinary source guardは引き続き差分を検出する。

全tracked/untracked/生成物検査が終わってから既存File.Replaceでsnapshotを復元する。backup親は実在する当該Diagnostics配下、backupは既存ファイルを上書きせず保持。復元後はtarget=before SHA、backup=postbuild SHAを既存処理で検証し、新しい完全一致照合でもtarget＋backupの組を再検証する。最後のordinary source guardも維持する。

## 変更ファイル

- `scripts/build-android-release.ps1`：strict helperを1個追加し、Repair-AndroidReleaseBuildSourceだけを変更。
- `scripts/tests/android-release-exact-transition.tests.ps1`：専用17ケース。
- `scripts/tests/android-release.tests.ps1`：必要なprivate fixtureのbeforeを実証済みCRLF、afterを実証済みLF＋2項目へ合わせ、unknown suffixをjに変更。URP CRLFの既存ケースは維持し、PS全体の未知fingerprintを正例にしない。
- 今回のReview evidenceと固定latestのsummary/status/checksum。

## 検証と限界

限定テスト **17/17 PASS、FAIL0**。実System.IO.File.Replaceによるbyte-perfect復元、実在するDiagnostics内backupのSHA/size/保持、復元後target破損とbackup破損の拒否を確認。未知before/after、別suffix、別keystore値、unknown field、size違い、未知untracked/4番目tracked、suffixを除いた別fingerprintも拒否した。

テストはLinux PowerShell上で実行。Git transportとscope gateはtest-only doubleで、hash/bytes/file IO/File.Replace/restore/source-check本文は実コードを使う。ordinary final guardのネイティブWindows/Git実行合格を主張しない。既存42件suiteの全実行はせず、変更したKnown/Change fixture helperだけを読み込む限定ケースで、既存URP2ファイル＋exact PSの3ファイル復元も確認した。Git config・Git commit・worktree作成を伴う既存suite setupは実行していない。

静的AST比較で、ordinary guard、recovery scope、UniversalRP比較、生成物判定/cleanup、File.Replace helper、Git呼び出し関数の本文がbaseと完全一致。既知の`.utmp`とexact Gradle stop log以外へcleanupを広げていない。

ProjectSettings/URP/Packages実体、AndroidReleaseBuild.cs、game/Visual Lock、signing経路、version、.gitignoreは変更0。full asset/snapshot/backup本体・keystore・ビルド成果物は共有しない。canonical diffは既知2行の公開可能な値だけ。

## 最終報告項目

| 項目 | 結果 |
| --- | --- |
| implementation commit | この報告を含む共有commit。検証コードSHAはstatic-verification.json |
| before / after expected SHA | 上記2値、sizeも完全固定 |
| exact diff fingerprint | `06237adb85a8490fbe6ea1f32b59fdbf8f0548cdbc706d9cd1d6d15a8e87b596`、CRLF→LF＋277/439行だけ |
| broad allowance / i一般許可 | NO / NO |
| backup検証 / restore後SHA | PASS / PASS（実filesystem fixture） |
| unknown mutation拒否 | PASS |
| limited tests total/pass/fail | 17 / 17 / 0 |
| ordinary source guard変更 | NO |
| ProjectSettings直接変更 / game / Visual Lock | NO / NO / NO |
| Unity / AAB / APK / Pixel | 0 / 0 / 0 / 0 |
| automatic retries | 0 |
| formal source clean | YES。11月13日のユーザー読取結果でHEAD ddbb93f、status/staged出力とも空 |
| 次のAAB one-shot準備 | YES（別指示・新共有commitのWindows取り込み・clean再確認後の1回のみ） |
| 今回AABを実行する許可 | NO、今回0回で停止 |
| 第8工程 complete | false |

正式Windows repoには今回書き込みを行っていない。WindowsのHEADはまだddbb93fなので、次回AABでは新共有commitの取り込みが必要。今のGitHub手動操作は不要で、後続指示時に同期を案内する。cloud作業場所に以前からある対象外未追跡5件は、変更・削除・stash・commitせず保持する。

前回Case Bは既存`skip-p6/sc-2aedfd75`を参照し、履歴一式は複製しない。個別writer/一般的なi表現の正当性は依然断定せず、ユーザーが承認した完全一致の既知副作用にだけrecoveryを限定した。最新error.log等はrun名が明記された以前のCreateWorktree失敗履歴で、今回のエラーとして流用しない。

**通常commit/push各1回とremote HEAD確認後に停止。追加probe・Unity・AAB/APK・Pixelには進まない。**

詳細：[今回の限定検証・条件](<exact-transition/20261113/summary.md>).
