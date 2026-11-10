# 第8工程：File.Replace空パス修正＋限定テスト再実行

## 現在地

最新HEAD：

`75b05a103e138ca360a41ce35f9b0d0397d76327`

source guard修正草案実装commit：

`0f1b066c390e61c739d4fc9ed961e14aa5de7120`

前回の限定テスト：

- Total：1
- Passed：0
- Failed：1
- exit：1
- Unity：0
- AAB再ビルド：0
- APK：0
- Pixel：0
- 自動再試行：0

根本原因：

`scripts/build-android-release.ps1`

内の、

`File.Replace(payload, target, $null)`

で、backup pathへ空値を渡したため例外。

保存済みエラー：

`The value cannot be an empty string. (Parameter 'path')`

---

# 今回の目的

**File.Replaceのbackup pathだけを最小修正し、source guard限定テストを1回だけ再実行する。**

今回はそれ以外を変更しない。

---

# 変更対象

原則この2ファイルだけ。

- `scripts/build-android-release.ps1`
- `scripts/tests/android-release.tests.ps1`

ただしテスト側に追加修正が不要なら、release PS1の1ファイルだけでよい。

---

# 変更禁止

以下は変更しない。

- ゲームコード
- Assets内ゲーム素材
- Visual Lock
- ProjectSettings原本
- URP原本
- Packages
- AndroidReleaseBuild.cs
- AndroidBuild.cs
- QA build script
- signing設定
- keystore
- release profile
- version/versionCode
- 第7工程
- source guardの最終完全判定ロジック

---

# 修正内容

現在：

```powershell
[IO.File]::Replace($PayloadPath, $TargetPath, $null)
```

のように、backupFileNameへ`$null`または空値を渡している箇所を修正する。

## 採用方針

`Diagnostics`配下に、

**実在可能な一意のbackup path**

を生成して渡す。

例：

`<run diagnostics>\source-guard-restore-backup\<guid>\<filename>.bak`

のような形。

条件：

- 空文字禁止
- null禁止
- formal C repo外
- isolated E diagnostics内
- 既存ファイル上書き禁止
- 一意path
- cleanup対象を明示
- backupが作成された場合はSHAを記録

---

# File.Replace使用時の安全条件

File.Replace実行前に、

- payload存在
- target存在
- backup親directory存在
- backup path非空
- backup pathがE Diagnostics配下
- backup path未存在
- payload SHAがsnapshot SHAと一致

を確認する。

1つでも不成立ならFAIL。

---

# restore成功後

確認：

- target SHA == prebuild snapshot SHA
- backup file存在
- backup file SHA == restore前target SHA
- payload / target / backupの関係が記録済み

restore成功と判定できた場合のみ次へ進む。

---

# backup cleanup

restore検証後に限り、

E Diagnostics内で今回生成したbackup fileのみ削除してよい。

削除前に、

- exact path
- SHA
- size

をDiagnosticsへ記録。

削除後に存在しないことを確認。

他ファイルは削除しない。

---

# 失敗時

復元失敗したら、

- 再実行しない
- backup削除しない
- payload削除しない
- targetを追加修正しない
- AABへ進まない

first failureを保存。

---

# 限定テスト

前回停止したsource guard専用テストを、

**最初から1回だけ**

実行する。

自動再試行は禁止。

---

# 必須PASSケース

少なくとも以下がPASSすること。

1. known 3 tracked mutationsをbyte-perfectにrestore
2. restore backup pathが非空
3. backup pathがDiagnostics内
4. restore後snapshot SHA一致
5. backup SHAがrestore前targetと一致
6. `.utmp/**` exact scope cleanup
7. exact `build/outputs/logs/unity---stop-build.log` cleanup
8. cleanup後最終source clean
9. root `.vsconfig`のみallow
10. formal repo unchanged

---

# 必須FAILケース

以下は全て拒否されること。

1. 4つ目のtracked file変更
2. UniversalRP許可外field変更
3. URP Global Settings許可外変更
4. ProjectSettings applicationIdentifier変更
5. versionCode変更
6. non-hex Android graphics API値
7. `.utmp2/`
8. nested別場所の`.utmp/`
9. `build/outputs/logs/other.log`
10. unrelated untracked
11. restore後SHA mismatch
12. backup path空/null
13. backup pathがDiagnostics外
14. backup path既存
15. cleanup失敗
16. Git diff取得失敗

---

# 重要：Guardを弱めない

禁止：

- pathだけ一致すれば許可
- ProjectSettings全体ignore
- Assets/Settings全体ignore
- source_unchanged強制true
- diff failureをwarning扱い
- unknown untrackedをallow
- 最終完全guardを削除
- `.gitignore`変更で隠す

最終判定は必ず、

- HEAD一致
- working tracked diff=0
- staged diff=0
- unexpected untracked=0
- root `.vsconfig`以外なし

で通す。

---

# テスト実行範囲

今回：

- source guard限定テスト：1回
- Unity：0回
- AAB：0回
- APK：0回
- Pixel：0回
- preflight：0回
- signing操作：0回
- 第7工程再実行：0回

---

# テスト失敗時

最初のFAILで停止。

追加修正しない。

2回目のテストを走らせない。

保存：

- first error
- failed case
- stack / line
- restore target
- payload SHA
- target SHA
- backup path
- backup存在有無
- cleanup実施有無
- formal repo状態

---

# PASS条件

今回の小工程complete条件：

- File.Replace空パス修正済み
- backup path安全条件PASS
- known mutation restore PASS
- unknown mutation reject PASS
- known generated cleanup PASS
- cleanup後final clean PASS
- formal repo unchanged
- Visual Lock変更なし
- game変更なし
- ProjectSettings原本変更なし
- QA変更なし
- release C#変更なし
- automatic retries=0
- Unity0
- AAB0
- APK0

---

# 共有

更新：

`References/Review/inbox/latest-release-candidate/`

最低限：

- summary.md
- status.json
- source-guard-fix-validation.json
- source-guard-fix-tests.log
- changed-files.json
- protected-source-hashes.json
- restore-backup-validation.json

必要に応じて：

`References/Review/latest/summary.md`

`References/Review/latest/status.json`

---

# commit / push

限定テストPASS時のみ、

通常commit。

通常push1回。

force push禁止。

FAIL時は、

失敗証跡のみcommit / pushしてよいが、
修正完了扱いにしない。

---

# 最終報告

以下を明記：

1. File.Replace空パス修正：YES / NO
2. backup path非空：YES / NO
3. backup path Diagnostics内：YES / NO
4. known mutation restore：PASS / FAIL
5. restore後snapshot SHA一致：YES / NO
6. backup SHA確認：YES / NO
7. unknown mutation reject：PASS / FAIL
8. generated cleanup：PASS / FAIL
9. final source clean：PASS / FAIL
10. 限定テストTotal
11. Passed
12. Failed
13. exit code
14. Unity起動：0
15. AAB再ビルド：0
16. APK：0
17. formal repo unchanged
18. Visual Lock変更：NO
19. game変更：NO
20. ProjectSettings原本変更：NO
21. automatic retries
22. AABを1回だけ再実行する準備が整ったか：YES / NO
23. 第8工程 complete=false

YESでもAAB再ビルドへ自動で進まない。