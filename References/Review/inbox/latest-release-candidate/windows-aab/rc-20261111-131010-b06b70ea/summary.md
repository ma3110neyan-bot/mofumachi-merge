# 第8工程：Graphics no-op修正版AAB／実体i再発・全体FAIL

状態：**調査中（停止条件・再発を確認、書き込み元未確定）**。branch：`feat/daily-world-pass-20261016`。実ビルドsource：`f27eadb407c2bf6fb893580f49256bc02bf6f51c`。Graphics no-op実装：`5a9e6d54e9c4bedf60f738dfacab858875a827ee`。固定ゲーム：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。確認対象のsource SHAと、この証跡共有commit自身を区別する。

対象run：`rc-20261111-131010-b06b70ea`。**AAB1回／Unity1回／自動再試行0**。今回CodexではUnity・ビルド・テスト・preflight・signing・キー操作・source修正・restore・cleanupをすべて実行していない。既存の受領結果を検証・共有しただけ。

**Unity内AAB生成はPASS、Unity exit0、build_errors0。全体はsource recoveryでFAIL。** ネイティブログのBuild Successful、MOFUMACHI_RELEASE_SUCCESS、正常batchmode終了・return code0を照合。PlayerBuildInfoは1件、success=true、ビルド本体1,203,026ms（20分3.026秒）、記録時刻は**11月11日23:40:36.313 JST**。PowerShellが閉じていた原因は未確定だが、閉鎖をAAB生成失敗の原因にしていない。

| 必須項目 | 結果 |
| --- | --- |
| source commit / run ID | `f27eadb407c2bf6fb893580f49256bc02bf6f51c` / `rc-20261111-131010-b06b70ea` |
| AAB / Unity / automatic retries | 1 / 1 / 0 |
| Unity result / exit / build_errors | Passed / 0 / 0 |
| initial graphics APIs / automatic | [OpenGLES3] / false |
| already_desired / graphics changed / restored | true / false / false |
| release入口のgraphics apply / restore setter | 0 / 0（固定C#分岐とnativeフラグから判断。Unity全体のsetter traceではない） |
| prebuild m_APIs | `0b000000`、raw `30 62 30 30 30 30 30 30` |
| observed post-build/current m_APIs | `0b000000i`、raw `30 62 30 30 30 30 30 30 69` |
| suffix i再発 | **YES**。末尾0x69 / U+0069 |
| source recovery | **FAIL**：Android graphics value is not the known OpenGLES3 representation. |
| restored / cleanup count | **0 / 0**（どちらも未到達） |
| backup作成・検証 | NotRun、backup0。File.Replace空パス問題の再発ではない |
| final source guard | **FAIL**（通常afterは実施してFAIL、recovery内finalは未到達） |
| working diff / staged diff / unknown untracked | 差分あり / clean / 56件（.utmp55＋exact Gradle stop log1） |
| source unchanged / formal repo unchanged | **NO / YES**（隔離E / 正式C） |
| AAB path | `E:\Mofumachi-Validation\release-candidate\rc-20261111-131010-b06b70ea\mofumachi-merge-rc-20261111-131010-b06b70ea.aab` |
| AAB size / SHA256 | **66,555,549 bytes** / `7b52c177780f2aec08ef6801554d3d0f3483d3e4ae6c4162e4538c03fb276b86` |
| artifact signature verification | **NotRun（PASS判定不可。署名エラーを検出したという意味ではない）** |
| actual signer SHA256 / signer match | 未取得 / 未確認（YES判定不可、実不一致は未検出） |
| APK / Pixel | 0 / 0 |
| 第7工程 / 第8工程 complete | true / **false** |
| APK one-shotへ進んでよいか | **NO** |

**no-opは期待どおり機能したが、今回の実体mutation回避は未達。** initial API/automaticはGetGraphicsAPIs/GetUseDefaultGraphicsAPIsの実取得フィールド。changed=falseはsetter直前に立つフラグが立っていないことを示し、当該release入口のsetter抑止と整合する。Unity内の別経路やserializerの書込み元・suffixの正当性は未確定。正当なUnity副作用とは断定せず、guard exact許可候補は**NO**。source guard・allowlist・ゲーム・Visual Lock・version・署名経路を変更していない。

Bは24,343bytes・CRLF846、SHA `d334127167318fe7278575eb17fe2621ab2c0e39c3ab2c53b7c1f39cc555e7fc`。Cは23,513bytes・LF846、SHA `266f17baf9d7101401580553aab9d0ede0390f6c859e137e31f6274d4713f07a`。元snapshot.json記録とBの実測メタデータが一致。C/Dの値・raw byteは一致し、DのSHA `e30c5865ddbe65a32cd3e764973a3bde9903bf63f886e0cdff96fe2155a46f4c` と全文byteは前回run `rc-20261110-132609-c6829dc7` と同じ。B/CのSHAも前回の実体auditと同じ。新runのnative source・build identity・no-opフィールド・PlayerBuildInfo・artifact metadataは別記録であり、旧runを流用したものではない。

**raw operatorのpostbuild_evidence_confirmed=falseは保持**する。このフラグは成功したrecoveryのbackup対応を保証するときだけtrueになる設計。今回は失敗後のC実体を直接読んだmetadataがあり、既存recoveryはProjectSettings比較時に未知値を拒否して最初のrestore/backup/cleanupへ到達していない。したがって現在Cにもiがある事実を確認できる。snapshot/C本体は未受領で、クラウドがWindows実体を再読・全byte再hashしたという意味ではない。

既知3ファイルの実差分以外を新たに許可していない。R拒否が全検証前なので生成物の内容検査・cleanup inventory・File.Replace・recovery最終guardは未到達。通常after guardは実行されworking diff1/index diff0/HEAD一致を記録。非許可未追跡56件と、既存allowlistのroot .vsconfig1件を区別する。formal repo unchanged=trueはnative wrapperの実記録。

AAB size/SHAは、失敗後にWindows側で既存AABをGet-Item/Get-FileHashしたoperator記録。native wrapperはrecoveryで止まったためraw aab.size/sha256はnullのまま保持。AAB本体は受領・Git追加していない。local signing identityは既存keystore SHA・keytool実測certificate SHAの照合を通過したが、**実AAB signerの証拠へ流用しない**。expected certificate：`96bb1402b8085570305a0486ae3f7fa3fafe2c60a3b8cd062c2ee8e0f81b8e09`。署名verify/keytool-jarは未到達。親のcertificate環境変数NOT SETは、実測照合とUnity子への設定を阻害していない。

受領ZIP：`graphics-noop-aab-20261111-131009-df2e0a7b574b4c418db8647b02638431-public.zip`、SHA `6fe525a2199ec1f01312e10516d1816be4567118860821ca67c9a61a70853791`。**CRC正常、原本38、public manifest37一致、native manifest受領31一致、公開コピー32件で元/公開SHA一致**。native full build.log（4,137,512bytes）とsnapshot binary3件は未受領。ログは末尾800行。ZIP内Windows区切りを保存先の`/`へ正規化したが、各payload byteは改変せず保存。秘密値・AAB・key・snapshot本体・ProjectSettings全体はGitへ追加しない。

原本・正規化レビューは`windows-aab/rc-20261111-131010-b06b70ea/`。最新集約はinbox直下とReferences/Review/latest。直前の集約は`validation/history-f27eadb-before-graphics-noop-aab-review/`へ原本保存。旧run、graphics-api-audit、graphics-api-physical-audit、no-op限定テスト12/12、42件source guard、第7工程55/444の原本は不変。

**ここで停止し、別指示待ち。** AAB2回目、source/guard修正、i許可・削除、手動restore/cleanup、追加署名検証、APK、Pixelへ進まない。今回の証拠からは書き込み元やUnity保存仕様を確定できないため、次の候補は別工程として指定された範囲の読み取り調査。実装修正や追加ビルドの許可とは扱わない。
