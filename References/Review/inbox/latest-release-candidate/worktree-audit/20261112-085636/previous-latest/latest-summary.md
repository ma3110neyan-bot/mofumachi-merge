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

---

以下は実AAB実行前の準備・限定検証履歴。過去のAAB readiness YESは実行後の今回FAILで更新済み。旧記録は改変せず保持。

# 第8工程：Graphics API no-op最終指示への限定確認

状態：**関連テスト合格（実Unity未確認）**。branch：`feat/daily-world-pass-20261016`。implementation commit：`5a9e6d54e9c4bedf60f738dfacab858875a827ee`。今回の作業開始HEAD：`f2bf6e5cf89bd9de322ab583ba4ba9149a163897`。ユーザー指示の基準`7c8271a7c4ed0bb49e7a5684bdc0030cc0df93a1`からのbuilder修正は既にimplementation commitへ含まれるため、そのC#を維持して最終条件を確認した。共有commit自身のSHAは最終Git報告で示す。

今回のコード変更は専用`scripts/tests/android-release-graphics.tests.ps1`だけ（Case2/3/4に計15行追加）。automatic=trueかつGLESのみ、automatic=falseかつ他APIのみ、automatic=falseかつ複数APIの各ケースで、desiredへ変更した後のrestore setter数・元automatic・元API列/順序を明示検査する。builder本体を変更していない。その他は今回のReview evidenceだけ。

desired条件：**automatic=false AND 非nullのAPI配列が1要素 AND OpenGLES3**。desired時のGraphics setter **0回**、restore setter **0回**。non-desired時は従来の2 setterを維持し、変更処理を開始した場合だけ2 setterで元状態へ復元。途中例外時にも復元を試みるためChangedを最初のsetter直前に設定する（実YAML byte mutationの証明を意味しない）。SaveAssetsは他のrelease/signing設定の復元に使う既存finallyの**1回を維持**。graphics helperからのSaveAssets追加は0。

**限定テスト1回：Total12 / Passed12 / Failed0 / NotRun0、exit0。自動再試行0。** 指定最低6ケースと既存の空/null/重複配列・途中例外・未Apply時restore・BuildInfo/呼出し接続を確認。実production C#全体をmanaged doublesでコンパイルしてgraphics helperを実行し、呼出し接続は静的確認する。Unity engineやnative serializationを実行した結果ではない。

C# SHA256は`4e2901614a787bbb7755a99da94d63449dbe873d6d233d2050700e2392c62903`で、既存API-only compile PASSの入力と一致。C#不変なのでAPI-only compileを再実行していない。現在テストSHA256は`1e9d46ede1005abe0a50a8d7cba1da8c3aa4ec243e8d7af69f7a85686433b769`。旧12ケース結果を今回の補強後結果として流用せず、今回のresult/logを新規保存した。既存42件source guardと第7工程55/444は再実行0。

source guard変更 **NO**、`0b000000i`許可追加 **NO**、ProjectSettings変更 **NO**、ゲーム変更 **NO**、Visual Lock変更 **NO**、signing経路変更 **NO**。UniversalRP、Packages、QA、version/versionCode、release PowerShellも不変。i削除・YAML手編集・regex修復・checkout/restore・allowlist追加は0。保護ファイルのSHAはverification.jsonへ保存した。

今回Unity起動／AAB／APK／Pixel／preflight／signing／keystoreは**すべて0**。秘密値・keystore・AAB/APK・コンパイルDLLはGitに追加しない。既存run `rc-20261110-132609-c6829dc7` のE1・source_unchanged=false・全体FAIL・署名NotRunと、書き込み元/Unity正規性未確定の判定は維持し、旧原本を変更しない。

**次のAAB one-shotへ進む準備：YES（別指示で新runを1回検証するための準備のみ）**。今回の実行許可ではない。source clean・local/remote HEAD一致はcommit/push後に最終確認する。通常commit1回・通常push1回後に停止。権限確認ダイアログが表示された場合はその文面を提示して停止し、回避しない。**APKへ進む判定NO、第8工程complete=false**。次工程は別指示待ち。

---

以下は以前の実装・検証・既存run履歴。今回の新規ビルド実績にはしない。

# 第8工程：Android Graphics API不要setter回避／限定検証合格

状態：**関連テスト合格（修正済み・実Unity AAB未確認）**。branch：`feat/daily-world-pass-20261016`。implementation commit：`5a9e6d54e9c4bedf60f738dfacab858875a827ee`。基準：`7c8271a7c4ed0bb49e7a5684bdc0030cc0df93a1`。この実装commitと証跡共有commitを区別する。既存AABの実ビルドsourceは`41529cfaea57ab564da46b3a45c41ede0aa5088b`のままで、新実装でのAABはまだ実行していない。

コード変更は**2ファイルだけ**：`Assets/Mofumachi/Editor/AndroidReleaseBuild.cs`と専用`scripts/tests/android-release-graphics.tests.ps1`。レビュー領域の証跡・集約は別途更新する。source guard、release PowerShell、ProjectSettings、UniversalRP、Packages、ゲーム、Visual Lock、既存QA、署名経路、既存42件テストは不変。

desired条件は**automatic=false AND API配列が非null・長さ1・唯一の要素がOpenGLES3**。private TemporaryGraphicsSettingsが元状態を取得し、desiredならApplyが直ちに戻る。**desired時setter count=0、restore setter count=0**。非desiredなら従来順のSetUseDefaultGraphicsAPIs(false)→SetGraphicsAPIs(OpenGLES3)を維持（計2回）。Changedがtrueのときだけ元API配列→元automaticの順に復元（計2回）。途中のsetter失敗にも備えChangedは最初のsetter直前に立てる。これはsetter開始の記録でありYAML実体変更の証明ではない。

**SaveAssetsへの影響：既存finallyの1回を維持**。署名・backend・architecture等の他の一時設定復元にも必要なため削除しない。graphics専用のSaveAssetsは追加しておらず、graphics helperはSaveAssetsを呼ばない。初期API列（enum名）、initial automatic、already_desired、changed、restoredの5公開フィールドをBuildInfoへ追加。desired時のchanged/restoredはfalse（復元未実施）。秘密値を追加記録しない。

**限定テスト：Total12 / Passed12 / Failed0、exit0。変更後suite実行1回。API-only compile：PASS、exit0、1回、C#エラー/CS0433なし。** 実際のproduction C#全体をmanaged API doublesでコンパイルし、productionのgraphics lifecycleを実行してsetter数・復元値を検査。build本体との接続は静的確認。別途、既存182参照のUnity6000.6.4f1 API-only compileで実型解決を確認した。これは実Unityの保存・serializationを実行した検証ではない。

指定6ケースに加えnull・空・重複API、setter途中例外、Apply前restore、BuildInfo/呼出し接続と既存SaveAssets保護を確認。修正前には新テストが最初の欠落条件で失敗することを1回確認して停止（expected RED：1失敗/11未実行）。その後実装を変更してGREENを1回実行した。**同じ失敗の自動再試行0**。既存42件source guardと第7工程55/444は再実行していない。原本はlimited-tests/redとgreenに分けて保持する。

source clean：**YES（実装commit直後）**。限定テスト/API compileの入力SHAは実装commitのC#と一致。現在の署名代入・他release設定代入は変更前と一致。公開証跡commit後のclean/remote一致はpush工程で確認する。比較補助の初回行単位checkはBuildInfo initializerの末尾変更を拾ったため、Android member参照順・署名/設定の各代入文を個別照合した。製品再修正・テスト再実行は不要だった。

**source guard変更NO／`0b000000i`許可追加NO／ProjectSettings直接編集NO／ゲーム変更NO／Visual Lock変更NO／signing経路変更NO**。iを正常値として追加せず、iを削除する文字列置換も追加していない。今回Unity/AAB/APK/Pixel/preflight/signing/keystore/guard42/Stage7の実行はすべて0、自動再試行0。AAB・APK・keystore・秘密値・コンパイルDLLはGitへ追加していない。

既存run `rc-20261110-132609-c6829dc7`の**E1、Unity内生成PASS／全体FAIL／source_unchanged=false／署名検証NotRun**は履歴として維持。書き込み元・suffixの正当性は未確定。この修正で実体mutationが止まると断定しない。旧native原本・graphics-api-audit・graphics-api-physical-auditは不変。前回集約はgraphics-noop-fix/prior-7c8271aにも原本保存する。

**次にAAB one-shotを1回実行してよいか：YES（別指示での検証に進む準備として）**。新run・cleanな修正済みcommit・既存署名条件・変更していないsource guardを維持し、最初の失敗で停止する。今回の実行許可ではなく、ここで停止する。**APKへ進む判定NO。第7工程complete=true、第8工程complete=false。** 次のAABは別指示待ち。

---

以下は旧実装runと実体調査の履歴。新実装のビルド実績へ流用しない。

# 第8工程：既存Eドライブ実体byte比較／E1

状態：**完了（今回の既存実体メタデータ比較のみ）**。書き込み元・Unity保存仕様の正当性は未確定。branch：`feat/daily-world-pass-20261016`。確認対象repo/evidence commit：`70d01ccc1ef4a960adfccc068f4e21ca9d832514`。実ビルドsource：`41529cfaea57ab564da46b3a45c41ede0aa5088b`。確認対象commitと、この証跡共有commit自身を区別する。

対象run：`rc-20261110-132609-c6829dc7`。Windows採取時刻：2026-11-11 18:50:06 JST（09:50:06 UTC）。ユーザーが既存B/Cを直接byte読み取りした公開メタデータZIPを受領し、CRC・6原本・重複JSON・値/hex/code point/context/offsetの整合と、元runのsnapshot記録SHA・保存済みdiff SHAを照合した。クラウドでB/C本体を直接読んだ、または本体全byteを受領したという意味ではない。

| 項目 | B：prebuild snapshot-2.bin | C：既存post-build ProjectSettings.asset |
| --- | --- | --- |
| exists | YES | YES |
| SHA256 | `d334127167318fe7278575eb17fe2621ab2c0e39c3ab2c53b7c1f39cc555e7fc` | `266f17baf9d7101401580553aab9d0ede0390f6c859e137e31f6274d4713f07a` |
| size | 24,343 bytes | 23,513 bytes |
| m_APIs | `0b000000` | `0b000000i` |
| value bytes / raw hex | 8 / `30 62 30 30 30 30 30 30` | 9 / `30 62 30 30 30 30 30 30 69` |
| 最終byte / code point | `30` / U+0030 | `69` / U+0069 |
| value offset / context start | 12235 / 12226 | 11812 / 11804 |
| m_Automatic | 0 | 0 |
| BOM / UTF-8 validity / ASCII only | none / true / true | none / true / true |
| CRLF / bare LF / bare CR | 846 / 0 / 0 | 0 / 846 / 0 |
| 最終更新UTC | 2026-11-10T13:34:07.6560780Z | 2026-11-11T04:40:31.6931504Z |
| 採取中のsize/mtime変化 | なし | なし |

**D保存済みdiffのm_APIsは`0b000000i`、rawは`30 62 30 30 30 30 30 30 69`。C/D一致：YES（値・raw byte）。B/C一致：NO（値・file SHA）。prebuild→postbuild実体mutation確認：YES。判定：E1。** Bの実測SHA/sizeは元snapshot.jsonの記録と一致する。末尾iはdiff出力だけの文字化け・公開コピー加工では説明できず、既存C実体の採取結果にも存在する。

発生区間：prebuild snapshot取得後〜今回のC実体観測前。Cのmtimeは2026-11-11 13:40:31 JSTだが、この時刻を正確な書込み時刻・書き込み元の証明にはしない。**proven writer：未確定。Unity正規副作用と断定：NO。guard exact許可候補：NO。** 改行CRLF→LFも観測されたが、追加ASCII iの発生原因や妥当性を改行だけで説明しない。

Git属性調査：**NO**。指示ではC/D不一致の場合だけ許可されており、今回は一致のため実施していない。新しいgraphics調査・検索・外部調査も行っていない。

今回のsource変更／Unity起動／AAB／APK／Pixel／preflight／guardテスト／signing／restore／cleanup／自動再試行は**すべて0**。ゲーム・release入口・QA・ProjectSettings・Packages・Visual Lock・guard・allowlist・gitignoreを変更していない。新規公開メタデータとレビュー集約だけを更新。snapshot binary、ProjectSettings全体コピー、AAB、keystore、秘密値はGitへ追加していない。

**今回のsource_unchanged_by_audit=true。既存AAB runのsource_unchanged=false／全体FAIL／artifact署名未検証は維持。** 過去Unity内AAB生成PASSを、今回の新規AAB実行・release全体合格にしない。第7工程complete=true、第8工程complete=false。次にAPKへ進んで安全か：**NO**。

原本6ファイル、`receipt.json`、`evidence-review.json`、`checksums.sha256`は`References/Review/inbox/latest-release-candidate/graphics-api-physical-audit/`。受領ZIP SHA256：`58833e72761679ef83f3372a9ef4535070f1f10238dd483c4eab665795c4d424`。snapshot/ProjectSettings本体は公開しない。旧`graphics-api-audit/`とnative runの原本は変更していない。

次の推奨アクション：今回の証跡共有後に停止し、**別指示待ち**。必要なら書込み元・Unity native保存仕様の証拠取得を別工程として指定する。今回E1であることだけを根拠にguardを許可・削除・緩和せず、AAB再実行・APK・Pixelへ進まない。

---

以下は今回の実体観測前の履歴。当時の「B/C未確認」は上記E1の物理メタデータ確認で更新済みだが、書き込み元・正当性は引き続き未確定。旧調査原本は保持する。

# 第8工程：Android graphics末尾iの由来・妥当性／読取り調査

状態：**調査中（取得可能な証拠の整理完了、実体未取得）**。判定は**Category E：判定不能**。正当なUnity副作用と断定：**NO**。guard exact許可候補：**NO**。調査対象repo/evidence commit：`ff65ef1be8a0cfb7134b11c958befce920874f36`。実AAB source：`41529cfaea57ab564da46b3a45c41ede0aa5088b`。両者を共有commit自身と混同しない。

1. baselineは`0b000000`（SHA `4043fb3267581fb2660bf61b6ad8da480b4f2d718bf5909b8891058f2a1bc862`、23497bytes、LFのみ・BOMなし）。
2. snapshot記録SHA `d334127167318fe7278575eb17fe2621ab2c0e39c3ab2c53b7c1f39cc555e7fc`・24343bytesはbaselineのCRLF版と一致。記録上`0b000000`と整合するが、snapshot実ファイル未受領。
3. post-build実体はEへ接続できず未受領。**実体SHA・BOM・改行・m_APIs raw bytesは未確認**。
4. 保存済みdiffの追加値は`0b000000i`、9byte `30 62 30 30 30 30 30 30 69`、末尾ASCII0x69/U+0069。このdiff内byteの確認を、実体ファイルのbyte確認とは扱わない。

release C#にはGetGraphicsAPIs108→SetGraphicsAPIs(OpenGLES3)129→復元164→SaveAssets167がある。ログのProjectSettings importと完了markerの順序はこの経路と整合するが、suffixを書いた主体は不明。native snapshotのgraphics_apiは定数初期値、temporary_settings_restoredは経路完了flagであり、どちらもbyte等価の保証ではない。

PowerShellは今回到達した経路でProjectSettingsのm_APIsを直接文字列編集しない。比較用正規化はメモリ内のみで、実体復元・cleanupは検査後（今回未到達）。literal生成コードは負例fixture1件のみで、actual AAB入口から呼ばれず一時専用rootを使用する。調査開始commitでliteralは**15箇所/15ファイル**（fixture1・証跡/報告14）。これは独立再現15件を意味しない。

同版Stage7の2validationは全tracked内容差分0。旧AABのraw差分は未受領で、過去の文章報告を独立byte再現と数えない。公式6000.6.4f1参照ソースでOpenGLES3=11とAPI native委譲は確認できたが、suffix iの保存仕様は未確認。公式manual/Issue Tracker/forumは通信経路403で本文を取得できず、検索0件や仕様不存在とは扱わない。

調査結果は`graphics-api-audit/`のsummary.txt、timeline.json、projectsettings-comparison.json、raw-bytes.txt、repo-search.txt、unity-log-search.txt、release-code-search.txt、script-search.txt、external-research.md、conclusion.json。**追加のB/C実体メタデータ採取手順はwindows-readonly-collection.md（未実行）**。ゲーム・source・guard・allowlist・gitignore・settings・versionを変更していない。Unity/AAB/APK/Pixel/preflight/tests/signing/restore/cleanupは全て0。

**第8工程complete=false。APKへ進む判定NO。** 正当性未確認のままiを許可・削除しない。証跡共有の通常push1回後に停止し、必要なら既存B/C/Dの読取りだけを次に行う。原本native run/旧履歴は不変。

---

以下は前回のAAB受領判定（ff65ef1be8a0cfb7134b11c958befce920874f36）。今回のread-only auditで書き換えて合格にしていない。

# 第8工程：新source guard対応AAB・生成成功／source recoveryで停止

状態：**原因確定（最初のguard拒否条件）／AAB全体検証FAIL**。branch：`feat/daily-world-pass-20261016`。実ビルドsource：`41529cfaea57ab564da46b3a45c41ede0aa5088b`。source guard実装：`604b5f7fd8442aaf97fc695d0406acdaa6c1bec2`。固定ゲーム実装：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。commit欄は今回Windowsでビルドしたsourceであり、この証跡共有commit自身とは区別する。

対象run：`rc-20261110-132609-c6829dc7`。Eドライブの新しい隔離worktreeで実行した**AAB1回／Unity1回／自動再試行0**の保存済み結果だけを受領・照合した。今回CodexでUnity・ビルド・テスト・standalone preflight・署名ツール・キー作成・プロセス終了・Fetchはすべて0。ゲーム・guard・ビルダー・テストコードを変更していない。

## 結論

**Unity内AAB生成はPASS、Unity exit0、build_errors=0。全体はsource recoveryでFAIL。** ネイティブログのBuild Successful、MOFUMACHI_RELEASE_SUCCESS、正常batchmode終了とreturn code0を確認した。強制終了が原因のビルド失敗とは扱わない。停止手順の実施結果は未受領であり、停止操作の実施有無は未確定。

最初の具体的停止理由は`Android graphics value is not the known OpenGLES3 representation.`。保存済みの`aab/source-recovery/pre-restore.diff`に次の実差分が含まれる。

```diff
-    m_APIs: 0b000000
+    m_APIs: 0b000000i
```

末尾は**小文字i（U+0069）**で、変更後は9文字・非16進。共有ZIP内のdiffはnative/public両manifestで検証し、公開コピーで変更されていない。さらに固定commitの元テキストへメモリ上だけでdiffを照合し、3ファイルすべてでdiffのbefore/after Git blob indexが一致した。今回の値は貼付文章だけの未確定情報ではなく、**受領済みのネイティブ差分記録に存在する値**。ただし、末尾iをUnity・外部編集のどちらが作ったかはこの記録だけでは不明。許可範囲拡大・推測修正・手動restoreはしていない。

## 実行結果

| 項目 | 結果 |
| --- | --- |
| AAB全体／Unity内生成 | **FAIL／PASS** |
| AAB／Unity起動／自動再試行 | **1回／1回／0** |
| Unity exit／wrapper exit／native build_errors | **0／1／0** |
| known mutation検査 | **FAIL**：Android graphics非hex値で拒否 |
| byte-perfect restore／生成物cleanup | **未実施／未実施**（restored0／cleaned0） |
| backup作成・検証 | **未到達**。File.Replace空パスの再発ではない |
| 最終通常source guard | **FAIL**：working diff1、staged diff0、HEAD一致 |
| source_unchanged／formal_repo_unchanged | **false／true**（隔離E／正式C） |
| artifact署名検証／実signer SHA | **未実施／null** |
| signer SHA一致 | **未確認（YESとは判定しない）**。raw signer_matches=falseは不一致の検出ではない |
| Development／Script Debugging／Deep Profiling／Autoconnect | **すべてOFF** |
| BuildOptions／custom signing／AAB形式 | **None／true／true** |
| backend／architecture／orientation／graphics | **IL2CPP／ARM64／Portrait／OpenGLES3**（native snapshot） |
| package／Unity／version／versionCode | **com.mofumachi.merge／6000.6.4f1／1.0／1** |
| 最低API／target API／一時設定復元 | **26／36／true** |
| keystore SHA照合／4環境変数 | **一致／開始時すべてSET** |
| first_error／runner_error | **null／Known build-side-effect recovery failed: Android graphics value is not the known OpenGLES3 representation.** |
| APK／Pixel／単独preflight／限定テスト／キー再生成 | **すべて0回** |
| 第7工程complete／第8工程complete | **true／false** |

正式repo・ゲームコード・QA経路・release入口・Packages・Visual Lockは今回変更なし。隔離worktreeの追跡済み実差分は**UniversalRP.asset／UniversalRenderPipelineGlobalSettings.asset／ProjectSettings.assetの3件**。その他のstatus上MとLF/CRLF警告を内容変更に数え替えていない。非許可未追跡は**56件**（root .utmp55件とexact Gradle停止log1件）、root .vsconfig1件は既存allowlist。unknown tracked値の検査で早期停止しており、生成物の内容検査・cleanup inventory・復元へは進んでいない。通常のafter guardは実行されFAIL、recovery内final_guardは未到達。

## AABの保存状態

- path：`E:\Mofumachi-Validation\release-candidate\rc-20261110-132609-c6829dc7\mofumachi-merge-rc-20261110-132609-c6829dc7.aab`
- filename：`mofumachi-merge-rc-20261110-132609-c6829dc7.aab`
- size：**66,555,554 bytes**
- SHA256：`d40cef8a8111619fde29bf12be2ab6e06fbd873eea40d71280175760d60b55de`
- 更新UTC：`2026-11-11T04:40:15.9996956Z`
- 期待certificate SHA256：`96bb1402b8085570305a0486ae3f7fa3fafe2c60a3b8cd062c2ee8e0f81b8e09`
- 実artifact signer SHA256：**未取得**。上記期待値やnative設定値を実AABの署名証拠へ流用しない。

size/SHAはWindows側Get-Item/Get-FileHashの公開記録を照合したもの。AAB本体を受領・Git追加・クラウド再ハッシュしていない。raw aab-review.jsonのaab_build_invocations=nullはそのまま保持し、レビューではCLI・単一native snapshot・PlayerBuildInfo・固定C#入口から生成1回を別途記録した。

## 証跡と限界

受領ZIP：`release-aab-public-24955a6f96b0407c8e1afada14fc4205.zip`、SHA256：`db00bd0fdf191f40c5593d440c981b90cadf33e7c3e9f758a5df3bc5ce9581b3`。**CRC正常／原本37ファイル／public36 hash一致／native受領31 hash一致／公開コピー33件の原本hash一致**。受領した内容を改変せず保存。未受領はnative build.log全体とsnapshot binary3件。full logは**4,149,997 bytes**、SHA256は`d4bc50c7298f8a49dafacedcd2f472a7a0df2e0a748d80f0168fac4c588b3e33`、共有範囲は末尾800行。snapshot3件の記録SHA/sizeは、固定sourceのCRLF checkout bytesと一致したが、実snapshot binary自体の受領検証ではない。

入口所要時間は**15時間14分47.408秒**（日本時間11/10 22:26:09 → 11/11 13:40:56）。PlayerBuildInfoのビルド本体は**1,562,304ms（約26分2秒）**。起動待ちの原因は未確定で、UPM待ちのスクリーンショットを恒久的デッドロックや全所要時間の原因の確証にはしない。

原本：`windows-aab/rc-20261110-132609-c6829dc7/`。レビュー：`evidence-review.json`、差分判定：`source-guard-result.json`、復元/cleanup：`restore-backup-result.json`、署名状態：`signer-verification.json`、artifact：`artifact-metadata.json`。直下first-error.logは今回nativeのまま、failure.logとlatest/error.logは今回の具体的guard条件を記録。以前の集約は`validation/history-41529cf/`に全バイト保存。旧AAB、Stage7、preflight、42/42テスト、旧失敗ログの原本ディレクトリは不変。

## 判定と停止地点

**次にAPK one-shotへ進んで安全か：NO**。source不変条件を満たさず、artifact署名検証も未到達。**第8工程complete=false**。このrunのAAB再実行・Unity再起動・receipt削除・元worktree修復・guard変更はしない。証跡だけを通常commitし、通常push1回後に停止する。次の候補は既存記録とE worktreeの非hex値の発生元についての読み取り調査であり、この共有を新しいビルド許可とは扱わない。

Codex／AI側から見た次の改善候補：今回の共有では実装変更なし。
