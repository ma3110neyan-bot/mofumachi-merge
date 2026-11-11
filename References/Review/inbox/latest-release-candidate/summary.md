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
