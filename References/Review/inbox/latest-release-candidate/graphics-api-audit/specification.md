# 第8工程：Android graphics末尾 `i` 発生元・妥当性 読み取り調査

## 目的

既存AAB run：

`rc-20261110-132609-c6829dc7`

で確認された、

```diff
-    m_APIs: 0b000000
+    m_APIs: 0b000000i
```

について、

**末尾 `i` がどこで発生したのか、Unity 6.6の正当な保存表現なのか、異常な外部混入なのかを読み取りだけで判定する。**

今回は実装変更しない。

---

# 現在の確定事項

実ビルドsource：

`41529cfaea57ab564da46b3a45c41ede0aa5088b`

source guard実装：

`604b5f7fd8442aaf97fc695d0406acdaa6c1bec2`

対象run：

`rc-20261110-132609-c6829dc7`

Unity：

`6000.6.4f1`

AAB生成：

PASS

Unity exit：

0

build_errors：

0

source guard：

FAIL

最初の拒否：

`Android graphics value is not the known OpenGLES3 representation.`

対象：

`ProjectSettings/ProjectSettings.asset`

変更前：

`0b000000`

変更後：

`0b000000i`

末尾：

小文字 `i`

Unicode：

`U+0069`

---

# 今回の禁止事項

絶対に行わない：

- Unity起動
- AAB再ビルド
- APKビルド
- Pixel操作
- preflight
- source guardテスト
- source修正
- ProjectSettings修正
- restore
- cleanup
- reset
- checkout
- stash
- git clean
- allowlist変更
- guard変更
- `.gitignore`変更
- keystore操作
- signing操作
- version変更
- commit前のソース変更

---

# 調査対象

## 1. 既存runの保存済み証拠

読む：

`E:\Mofumachi-Validation\Diagnostics\release-candidate\rc-20261110-132609-c6829dc7`

および関連する、

`E:\Mofumachi-Validation\projects\rc-20261110-132609-c6829dc7`

---

# 2. GitHub保存済み証拠

読む：

`References/Review/inbox/latest-release-candidate/windows-aab/rc-20261110-132609-c6829dc7/`

特に：

- `source-recovery/pre-restore.diff`
- `source-guard-result.json`
- `restore-backup-result.json`
- `evidence-review.json`
- `build-info.json`
- `build-settings.json`
- `artifact-metadata.json`
- `signer-verification.json`

存在するものだけ読む。

---

# 3. ProjectSettings.asset の時系列比較

最低限、以下4状態を比較する。

### A. Git baseline

commit：

`41529cfaea57ab564da46b3a45c41ede0aa5088b`

の

`ProjectSettings/ProjectSettings.asset`

### B. prebuild snapshot

今回runでUnity起動前に保存されたProjectSettings snapshot

### C. post-build isolated worktree

`E:\Mofumachi-Validation\projects\rc-20261110-132609-c6829dc7\ProjectSettings\ProjectSettings.asset`

### D. 保存済みdiff

`pre-restore.diff`

---

# 比較条件

各状態について以下を記録：

- file SHA256
- byte length
- line ending
- BOM有無
- `m_BuildTargetGraphicsAPIs` ブロック
- `m_APIs` のraw bytes
- `m_APIs` の文字列表現
- 末尾byte
- Unicode/code point
- 周辺16〜32byte hex dump

---

# 特に確認すること

## A. baselineに `i` が存在するか

期待：

`0b000000`

もしbaselineに既に `i` があるなら調査前提が崩れるため記録して停止。

---

## B. prebuild snapshotに `i` が存在するか

もしprebuild snapshotが：

`0b000000`

で、

post-buildのみ：

`0b000000i`

なら、

**ビルド実行中またはビルド後処理で発生**

まで確定できる。

---

## C. post-build実体ファイルに `i` が存在するか

diffだけでなく実体byteにも存在するか確認。

存在するなら：

貼り付け混入ではない。

---

# 4. Unityログ調査

既存runの保存済みログだけを検索する。

検索語：

- `graphics API`
- `Graphics API`
- `OpenGLES3`
- `m_APIs`
- `PlayerSettings`
- `SetGraphicsAPIs`
- `Android`
- `ProjectSettings`
- `Serialization`
- `serialized`
- `Save`
- `Write`

---

# 確認したいこと

Unity Editor / build pipelineが、

Android graphics API設定を一時変更している証拠があるか。

特に：

- `PlayerSettings.SetGraphicsAPIs`
- Android graphics APIの一時設定
- BuildReleaseAab前後の設定変更
- temporary settings restore

を確認。

---

# 5. release C#コード読み取り

読むだけ。

対象：

`Assets/Mofumachi/Editor/AndroidReleaseBuild.cs`

検索：

- SetGraphicsAPIs
- GetGraphicsAPIs
- GraphicsDeviceType
- OpenGLES3
- Android
- PlayerSettings
- SaveAssets
- AssetDatabase
- ProjectSettings

---

# 判定

もしC#でAndroid graphics APIを一時的に設定しているなら、

その設定前後と `0b000000i` 発生タイミングを比較する。

---

# 6. PowerShell build script読み取り

対象：

`scripts/build-android-release.ps1`

確認：

- ProjectSettingsを直接文字列編集していないか
- `m_APIs` を直接書いていないか
- encoding変換していないか
- CRLF/LF変換を行っていないか
- `Set-Content`
- `Out-File`
- `WriteAllText`
- `WriteAllBytes`
- regex置換
- serialization操作

---

# 7. repo全体検索

読み取りのみ。

検索文字列：

`0b000000i`

`0b000000`

`m_APIs:`

`SetGraphicsAPIs`

`OpenGLES3`

---

# 目的

`i` が、

- repo内コードで生成される可能性
- テストfixture由来
- PowerShell文字列加工
- Unity serializer由来
- 外部ツール由来

のどれかを絞る。

---

# 8. 同Unity versionの既存成功run比較

可能なら、既存保存済みrunでUnity 6000.6.4f1を使った別Android build / validationのProjectSettings差分を読む。

新規実行は禁止。

既存証拠だけ。

見るもの：

- ProjectSettings変更有無
- m_APIs変更有無
- `0b000000i` 出現有無

---

# 9. Unityの正当なserialized表現かの判定

ここは推測禁止。

以下の順で判定する。

### レベル1

既存repo・run内で同じ表現が複数回再現しているか。

### レベル2

Unityが生成した証拠と時系列一致するか。

### レベル3

Unity 6000.6系の正式仕様・公開情報で、この値表現が存在するか。

---

# 外部調査

必要な場合のみ、

Unity公式ドキュメント
Unity Issue Tracker
Unity公式フォーラム
Unity package/source公開情報

を調査してよい。

ただし外部情報とローカル証拠は明確に分ける。

---

# 外部調査で確認する語

- Unity 6000.6 ProjectSettings m_APIs
- Unity Android m_BuildTargetGraphicsAPIs serialization
- OpenGLES3 serialized ProjectSettings
- `0b000000i`
- YAML integer suffix i Unity
- Unity serialized integer suffix i

---

# 推測禁止

次を根拠なしで言わない：

- Unityの仕様
- 正常な表現
- corruption
- encoding bug
- CRLF原因
- PowerShell原因
- user操作原因

---

# 10. raw byte確認

重要。

`0b000000i`

の末尾が本当にASCII `0x69`か確認。

記録：

例：

`30 62 30 30 30 30 30 30 30 69`

のように実byte列を記録。

もし見た目はiでも別文字なら明記。

---

# 11. 文字コード確認

ProjectSettings.assetについて：

- UTF-8
- UTF-8 BOM
- UTF-16
- ANSI
- その他

を判定。

ただし変換しない。

---

# 12. diff生成経路確認

`pre-restore.diff` が、

実worktreeに対するGit diffそのものなのか、
途中でJSON化・エスケープ・整形されたものなのか確認。

実体ファイルとdiff両方で `i` が一致することを要求。

---

# 判定カテゴリ

最終的に以下のどれかへ分類する。

## A：Unity正規副作用

以下全部成立：

- prebuildは`0b000000`
- postbuildは`0b000000i`
- Unity/build処理中に発生
- 実体byteにも`i`
- Unity仕様または再現済み既存証拠で正当表現と確認

---

## B：Unity由来濃厚だが仕様未確認

- Unity実行中に発生
- 実体byteに存在
- repo/PowerShellによる直接編集なし
- 公式仕様確認なし

---

## C：repo/build script由来

コードまたはscriptが生成している証拠あり。

---

## D：外部・異常変更

Unity/build script以外の変更証拠あり。

---

## E：判定不能

証拠不足。

---

# guard変更可否判定

今回の調査ではguardを変更しない。

最終報告でのみ、

### 許可候補YES

とできる条件：

- Category A
- 正当なUnity副作用と十分確認
- OpenGLES3意味値として等価
- 他値との誤認リスクなし
- exact representationだけ許可可能

### NO

- B〜E
- 仕様未確認
- 意味値不明
- 広すぎる許可が必要
- corruption可能性あり

---

# 重要

仮に正当なUnity副作用でも、

`.*i` のような広い許可は禁止。

許可候補にする場合も、

exact path
exact field
exact before
exact after

まで限定する。

---

# 今回の出力

新規Diagnosticsを作る場合：

`E:\Mofumachi-Validation\Diagnostics\release-graphics-api-audit-<timestamp>-<guid>\`

保存：

- summary.txt
- timeline.json
- projectsettings-comparison.json
- raw-bytes.txt
- repo-search.txt
- unity-log-search.txt
- release-code-search.txt
- script-search.txt
- external-research.md
- conclusion.json

---

# Git共有

公開可能な情報だけ：

`References/Review/inbox/latest-release-candidate/graphics-api-audit/`

AAB本体・snapshot binary・秘密値は追加しない。

---

# commit / push

調査結果のみ。

通常commit。

通常push1回。

force push禁止。

---

# 最終報告必須項目

1. baseline m_APIs
2. prebuild snapshot m_APIs
3. post-build m_APIs
4. raw bytes
5. suffix code point
6. baseline SHA
7. snapshot SHA
8. post-build SHA
9. Unityログに関連操作あり／なし
10. release C#にgraphics API操作あり／なし
11. PowerShellに直接編集あり／なし
12. repo内 `0b000000i` 出現箇所数
13. 既存runで同表現再現あり／なし
14. 外部公式情報で確認あり／なし
15. 発生タイミング
16. 推定発生主体
17. 判定カテゴリ A〜E
18. 正当なUnity副作用と断定可能か YES／NO
19. guard exact許可候補にできるか YES／NO
20. guard変更実施：NO
21. source変更：NO
22. Unity起動：0
23. AAB：0
24. APK：0
25. Pixel：0
26. automatic retries：0
27. 次の推奨アクション

---

# 停止条件

調査完了後停止。

guard修正へ進まない。

AAB再実行しない。

APKへ進まない。

Pixelへ進まない。

第8工程complete=falseのまま。