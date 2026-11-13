# 第8工程：P6 SaveAssets対照probe — Case B確認・停止

branch：`feat/daily-world-pass-20261016`。検証対象source HEAD：`ddbb93f3e6b5a45795be87561f61de5d4194f65c`。
run：`sc-2aedfd75`。レビュー日：2026年11月13日（JST）。
このsource SHAはWindowsの検証対象であり、今回の診断コード・証跡を共有するcommit自身のSHAではない。診断C#2ファイルは受領probe-code.jsonと同梱ソースのSHA256一致を確認した。launcher/testは公開対象ソースのSHAを記録したもので、Windowsに貼り付けたlauncher全体のbyteハッシュは未採取。

**結論：Case B。P6を省略しても、正常終了時に同じProjectSettings mutationが発生した。最終SaveAssetsの削除単独は解決候補NO。**
状態：調査中（対照結果Case Bは確定。個別writerと`i`の妥当性は未確定）。本番修正・AAB/APKには進まない。

| 最終報告項目 | 結果 |
| --- | --- |
| source HEAD | `ddbb93f3e6b5a45795be87561f61de5d4194f65c` |
| probe run ID | `sc-2aedfd75` |
| worktree path | `E:\Mofumachi-Validation\p\sc-2aedfd75` |
| worktree creation count | 1、PASS、Git exit 0 |
| Unity invocation count | 1 |
| Unity exit code | 0 |
| P0 m_APIs | `0b000000` |
| P1 m_APIs | `0b000000` |
| P2 m_APIs | `0b000000`（既存SaveAssets 1回） |
| P3 m_APIs | a/b/cすべて`0b000000` |
| P4 m_APIs | a〜eすべて`0b000000` |
| P5 m_APIs | a〜e・集約P5すべて`0b000000` |
| P6 skipped | YES。sidecar・標準出力marker・P6 stage不存在を照合 |
| E0 m_APIs | `0b000000i` |
| P5 AndroidKeystoreName | 空 |
| E0 AndroidKeystoreName | `'{inproject}: '` |
| P5→E0 file changed | YES |
| automatic retries | 0 |
| BuildPlayer / AAB / APK / Pixel | 0 / 0 / 0 / 0 |
| formal repo unchanged | YES。前後HEAD一致、clean、staged empty |
| classification | Case B |
| production SaveAssets削除候補 | NO（削除単独では防止できない） |
| 第8工程 complete | false |

P0〜P5はSHA256・sizeともbaselineと一致。E0で末尾に実byte `69`（ASCII `i`）が追加され、keystore名が空から`'{inproject}: '`へ変化した。改行もCRLF 846行→LF 846行となった。

- P5：24,343 bytes、SHA256 `d334127167318fe7278575eb17fe2621ab2c0e39c3ab2c53b7c1f39cc555e7fc`
- E0：23,513 bytes、SHA256 `266f17baf9d7101401580553aab9d0ede0390f6c859e137e31f6274d4713f07a`
- P5 raw hex：`30 62 30 30 30 30 30 30`
- E0 raw hex：`30 62 30 30 30 30 30 30 69`

前回`sp-81916893`のP6後と最終SHAが一致する。前回の履歴一式は今回コピーしていない。

## 起動・終了の確認

OS取得の実行ファイル：`C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe`。PID **12904**、親PID **7740**は起動用PowerShellと一致。OS command lineで短縮project・専用executeMethod・source SHA・`-mofumachiProbeSkipP6`を確認。

| 記録 | 日本時間（2026年11月12日） |
| --- | --- |
| process start | 2026-11-12T22:02:14.441+09:00 |
| P0 | 2026-11-12T23:07:28.488+09:00 |
| P5 | 2026-11-12T23:07:31.011+09:00 |
| E0に記録されたfile mtime | 2026-11-12T23:07:34.020+09:00 |
| process exit（Process.ExitTime） | 2026-11-12T23:07:39.391+09:00 |
| Windows側E0直接観測 | 2026-11-12T23:07:39.701+09:00 |

約65分14秒はP0到達前。P0〜P5は約2.5秒。長時間待機の根本原因は未確定であり、プロセス存続のheartbeatを処理進捗や停止の証拠とは扱わない。強制終了は0回。

ファイルmtimeはP5より後、process exitより前。今回絞れたのは正常終了の観測区間であり、個別setter/callback/serializerの排他的writerまでは特定していない。P2のSaveAssetsは従来どおり実行され、P3〜P5の変更・restore後のP6だけが省略されている。P6を省略するだけでは終了時の書き出しを防げない。

## 証跡・検証範囲

受領ZIP：40,838 bytes、SHA256 `be4df6f5a2ff0661366d0c059fdb03429a913303b598da865fe629879b3287a1`。85 member、manifest対象84件のbytes/SHA256を全照合。19 stageの独立JSONとreport、raw hex、独立E0、P5→E0 diff、12件のGit記録・stdout/stderrを照合した。

`References/Review/inbox/latest-release-candidate/skip-p6/sc-2aedfd75/received/summary.json`のCase Bが対照判定の根拠。変更していない旧observerの`probe-report.json`は同じ観測を`ExitOnly`と分類する。矛盾ではなく分類体系が異なる。`p6-skipped.json`と終了markerはともにP6省略を示す。

Editor出力は`-logFile -`で標準出力へ流れ、stage marker・stdout/stderr SHAのみ公開。生のEditorログ・秘密値・keystore・ProjectSettings全文・ビルド成果物は含めない。受領JSON・ログ内容は改変せず、ZIP内Windows区切りのmember名だけを通常のディレクトリ区切りへ展開した。

診断コードは専用C# driver、純粋byte observer、対照launcher、限定判定testだけ。P0〜P5操作は前回と同一。限定データテスト12/12 PASS、実行済みコードSHA一致。product/guard suiteの追加実行なし。

正式C repoは前後unchanged。隔離E worktreeは終了後、Unityによるtracked 6ファイル変更とprobe/メタデータ等のuntrackedが残る。作成直後はclean。修復・削除・cleanupは行っていない。game/Visual Lock/signing経路/version/production builder/source guardは変更せず、`i`の許可追加も0。

今回のUnityは正常終了し、native first errorの記録はない。最新集約の既存error.log／first-error.log／failure.logは、run名が明記された前回CreateWorktree失敗の履歴。今回のCase Bの根本エラーとして流用しない。個別writerの根本原因は未確定。

**次にやること：ここで停止し、別指示待ち。production SaveAssets削除・置換、追加probe、AAB/APK、guard変更は実行しない。**

詳細・一次資料：[今回の対照probe](<../inbox/latest-release-candidate/skip-p6/sc-2aedfd75/summary.md>).
