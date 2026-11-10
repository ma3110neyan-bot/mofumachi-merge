# 第8工程：AAB source guard最小修正

## 目的

既存AABビルドではUnity内部のAAB生成自体は成功したが、ビルド後の隔離E worktreeでUnity/Gradle/CMake由来の自動変更・生成物が残り、source guardが停止した。

今回の目的は、

**ゲーム・設定内容・Visual Lockを変更せず、release用source guardが既知のビルド副作用だけを安全に処理できるよう最小修正すること。**

今回はAAB再ビルドを行わない。

---

# 根拠となる既存audit

対象run：

`rc-20261109-123540-725e4e31`

baseline：

`c0803e6e9faf0ebb5e63e637d94c20dcce3f6e4a`

read-only source audit：

成功。

Unity / build / preflight実行：

0回。

---

# auditで確定した実内容差分

`git diff`で実際に内容差分があったtracked fileは次の3つだけ。

1.

`Assets/Settings/UniversalRP.asset`

2.

`Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`

3.

`ProjectSettings/ProjectSettings.asset`

---

# UniversalRP.assetで確認されたUnity自動変更

主に、

`m_PrefilteringMode...`

`m_Prefilter...`

`m_Light2DPrefilteringMode`

などのshader prefilter系値。

大量の0/1/4等の切替が発生している。

---

# UniversalRenderPipelineGlobalSettings.asset

`m_RuntimeSettings.m_List`

が、

空リストからUnity生成rid一覧へ変更。

---

# ProjectSettings.asset

確認された内容差分：

`AndroidKeystoreName`

および、

Android graphics APIのserialized representation。

---

# 注意：working-statusでは8 tracked fileがM表示

以下もstatus上では変更扱い：

- ProjectSettings/EditorBuildSettings.asset
- ProjectSettings/GraphicsSettings.asset
- ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json
- ProjectSettings/ProjectAuditorSettings.asset
- ProjectSettings/ShaderGraphSettings.asset

しかし実際の`git diff`内容差分は上記3ファイルのみ。

stderrにはLF→CRLF warningがある。

したがって、

**statusだけを見て8ファイルすべてを内容変更扱いしない。**

一方で、

**LF/CRLFを理由にguard全体を無効化しない。**

---

# 未追跡生成物

確認済み：

`.utmp/` 配下 55件

および、

`build/outputs/logs/unity---stop-build.log`

1件。

`.vsconfig`

は既存allowlist。

`.utmp`内容は、

- CMake
- NDK
- Ninja
- GameActivity
- FramePacing
- compile_commands
- prefab
- Android Gradle native intermediate

等のビルド中間生成物。

---

# 修正対象

原則：

`scripts/build-android-release.ps1`

および直接関連する、

`scripts/tests/android-release.tests.ps1`

だけ。

必要ならrelease source guard専用の小さなhelperを追加してよいが、
既存構造内で済むなら新規ファイルを増やさない。

---

# 変更禁止

変更しない：

- ゲームコード
- Assets内ゲーム素材
- Visual Lock
- ProjectSettings原本
- URP設定原本
- Packages
- AndroidReleaseBuild.cs
- AndroidBuild.cs
- QA build script
- 第7工程
- keystore
- signing設定
- release profile
- version/versionCode

---

# 基本方針

guardを「無視方式」に変更しない。

禁止：

- `.utmp/**`を無条件ignoreしてsource guardをPASSさせるだけ
- ProjectSettings全体をignore
- Assets/Settings全体をignore
- `git diff`を見ない
- tracked変更をpathだけで許可
- `source_unchanged=true`を強制
- guard無効化

---

# 採用する安全方式

## Phase A：ビルド前snapshot

Unity起動前に、

次の3ファイルの**正確なbyte snapshot / SHA256**をE診断領域へ保存する。

- Assets/Settings/UniversalRP.asset
- Assets/Settings/UniversalRenderPipelineGlobalSettings.asset
- ProjectSettings/ProjectSettings.asset

保存先はrelease runのDiagnostics内。

formal C repoへコピーしない。

snapshotはビルド前sourceと完全一致すること。

---

# Phase B：Unity終了後、restore前に差分検査

まず通常のGit読み取りを行う。

確認：

- HEAD baseline一致
- index diffなし
- tracked diff取得成功
- untracked取得成功

tracked内容差分が存在する場合、

**内容差分pathが上記3ファイル以外に1つでも存在したら即FAIL。**

restoreしない。

---

# Phase C：既知3ファイルの変更内容を検証

単にpath一致だけで許可しない。

今回auditで確認されたUnity自動生成領域以外の変更が入っていないことを確認。

## UniversalRP.asset

許可対象はshader-prefilter関連serialized fieldsのみ。

例：

- `m_PrefilteringMode*`
- `m_Prefilter*`
- `m_Light2DPrefilteringMode`
- `m_Light2DKeptVariantCombos`

それ以外の、

- renderer設定
- shadows基本設定
- quality
- assets参照
- gameplay関連設定

等に差分があればFAIL。

---

## UniversalRenderPipelineGlobalSettings.asset

許可対象：

`m_RuntimeSettings.m_List`

におけるUnity生成runtime RID listだけ。

それ以外の設定変更があればFAIL。

---

## ProjectSettings.asset

今回のknown mutationとして許可するのは、

Android release build時にUnityが書き換えることが確認された限定fieldのみ。

- AndroidKeystoreName serialization
- Android graphics API serialized representation

その他のPlayerSettings差分があればFAIL。

特に以下が変化したらFAIL：

- applicationIdentifier
- bundleVersion
- AndroidBundleVersionCode
- minSdk
- targetSdk
- architecture
- scripting backend
- Development
- orientation

---

# Phase D：known tracked mutationのみならrestore

上記検査をすべて通過した場合だけ、

ビルド前snapshotのbyteを使って、

**Eドライブ隔離release worktreeの3ファイルだけ**

元のbyteへ戻す。

正式repo：

`C:\Users\User\Desktop\Mofumachi\mofumachi-merge`

には絶対に書き込まない。

Git checkout / reset --hardは使わない。

snapshotから対象3ファイルへbyte-for-byte復元する。

復元後、

SHA256がビルド前snapshotと一致することを確認。

---

# Phase E：known untracked build生成物

次のものだけをUnity/Gradle/CMake生成物として扱う。

1.

isolated release worktree root直下：

`.utmp/**`

2.

`build/outputs/logs/unity---stop-build.log`

3.

既存allowlist：

root `.vsconfig`

---

# 未追跡生成物の安全条件

`.utmp`は、

**worktree root直下の`.utmp`のみ**

対象。

類似名や他ディレクトリは許可しない。

`build/outputs/logs/unity---stop-build.log`

もexact pathのみ。

その他のuntracked fileが1件でもあればFAIL。

---

# cleanup

既知generated untrackedの内容・pathが条件に一致した場合のみ、

isolated E worktreeから、

- `.utmp/**`
- `build/outputs/logs/unity---stop-build.log`

をcleanup対象としてよい。

この指示は、

**上記2種類の既知生成物をEの隔離release worktree内だけ削除することを許可する。**

禁止：

- formal C repoから削除
- `.vsconfig`削除
- その他untracked削除
- Library削除
- Assets削除
- ProjectSettings削除
- Git reset
- stash

cleanup実施前に対象一覧をDiagnosticsへ記録する。

---

# Phase F：cleanup後の最終source guard

cleanup / tracked restore後に、

通常の完全source guardを再実行。

必須：

- HEAD baseline一致
- working tracked diff=0
- staged diff=0
- unexpected untracked=0
- root `.vsconfig`のみallow
- source_unchanged=true

ここでは特別扱いをしない。

**最終的には通常のclean状態へ戻っていることを要求する。**

---

# formal repo保護

formal repoについて、

開始時と終了時に：

- HEAD
- status
- index
- untracked

確認。

formal repoへUnityを向けない。

formal repo unchanged=true必須。

---

# テスト追加

`scripts/tests/android-release.tests.ps1`

へ、最低限次を追加。

## PASSケース

1. auditで確認された3ファイルだけknown mutation
2. `.utmp/**`のみ生成
3. exact Gradle stop logのみ生成
4. restore後byte完全一致
5. cleanup後source clean

---

# FAILケース

最低限：

1. 4つ目のtracked file変更
2. UniversalRP.assetの許可外field変更
3. UniversalRenderPipelineGlobalSettingsの許可外変更
4. ProjectSettings.assetのapplicationIdentifier変更
5. versionCode変更
6. `.utmp2/`生成
7. nested別場所の`.utmp/`
8. `build/outputs/logs/other.log`
9. unrelated untracked file
10. restore後SHA mismatch
11. cleanup失敗
12. Git diff取得エラー

全てsource guard FAILになること。

---

# audit fixture

今回ユーザーから取得した実audit内容をテストfixtureとして利用してよい。

ただし、

秘密情報は含めない。

実keystore path/passwordは使用しない。

---

# 今回の実行範囲

修正後、

限定release source-guardテストだけ実行。

AAB実ビルド：

0回。

Unity：

0回。

APK：

0回。

Pixel：

0回。

第7工程：

再実行しない。

---

# テスト失敗時

1回目の結果で停止。

自動再試行しない。

AABを再ビルドしない。

最初のfailureを保存。

---

# PASS条件

- source guard限定テスト全PASS
- known tracked mutation厳密検査PASS
- unknown tracked mutation拒否PASS
- known generated cleanup PASS
- unknown untracked拒否PASS
- formal repo保護PASS
- existing QA経路不変
- AndroidReleaseBuild.cs不変
- Visual Lock不変
- game不変
- ProjectSettings原本不変
- automatic retries=0

---

# 共有

更新：

`References/Review/inbox/latest-release-candidate/`

最低限：

- summary.md
- status.json
- source-guard-fix-validation.json
- test log
- changed-files.json
- protected-source-hashes.json

必要に応じて：

`References/Review/latest/summary.md`

`References/Review/latest/status.json`

AAB本体は共有しない。

---

# commit / push

変更内容を確認後、

通常commit。

通常push1回。

force push禁止。

---

# 最終報告

以下を明記：

1. root cause
2. 実際に内容変更されたtracked file数
3. 実際のtracked file 3 path
4. generated untracked数
5. source guard修正file
6. guardを弱めたか：NO必須
7. known mutation検査：PASS / FAIL
8. unknown mutation拒否：PASS / FAIL
9. cleanup限定scope：PASS / FAIL
10. cleanup後clean検査：PASS / FAIL
11. 限定テスト総数
12. passed
13. failed
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