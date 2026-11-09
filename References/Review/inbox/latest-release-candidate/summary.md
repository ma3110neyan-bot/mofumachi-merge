# 第8工程：Git探索最小修正・限定releaseテスト34/34合格

状態：**関連テスト合格**。release入口の静的・限定検証小工程は完了。第8工程全体は未完了。

- branch：`feat/daily-world-pass-20261016`。
- Git探索修正commit：`c86941dc65291867fe6400345c0bb97beb51035e`。
- 修正前HEAD：`b03a9814df64cd26f9feb770bde0a3bbefce3d41`。
- release入口追加commit：`ea503db12f34d0eaec418f4aaa507b2d4512d469`。
- 固定ゲーム：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- 第7工程complete=trueとv5の55/55・444/444を保持。再実行なし。

## 修正範囲

コード変更は`scripts/tests/android-release.tests.ps1`の**1ファイルだけ**。変更前49行目のGit探索を、最初のApplicationを1件選び、null／空パスを明確に拒否する形へ変更した。

```powershell
$GitCommand = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
if ($null -eq $GitCommand -or [string]::IsNullOrWhiteSpace($GitCommand.Source)) { throw 'Git Application was not found or has an empty executable path.' }
$Git = $GitCommand.Source
```

その他のテストロジック・PATH・fallback探索は変更なし。修正前の該当箇所は`validation/git-selection-fix/before-git-discovery.txt`、差分は`git-selection-only.diff`へ保存した。旧ファイルの生バイトからこの1箇所を置換した結果が現ファイルと完全一致することを確認。

コードの変更ファイル数1と、ユーザー指定の共有記録更新ファイル数は分けて扱う。

## 実行結果

`scripts/tests/android-release.tests.ps1`を**今回1回のみ**実行。

| 項目 | 結果 |
| --- | --- |
| exit code | **0** |
| Total | **34** |
| Passed | **34** |
| Failed | **0** |
| 自動再試行 | **0** |
| Git fixture初期化 | 成功 |
| release入口／AAB・APKの入口切替 | 合格 |
| Development OFFの宣言確認 | 合格 |
| 署名preflight | 合格（合成fixture） |
| 秘密値非露出 | 合格（合成値のredaction／引数／JSON確認） |
| source guard | 合格（実一時Git・CRLF・tracked/index/untracked判定） |
| QA経路保護 | 合格（別途、生バイトとSHA256照合） |

34はスクリプト内のCheck assertion数。Unity／NUnitケース数として数えていない。ログ`validation/git-selection-fix/release-tests.log`の末尾に全チェックPASSを確認した。LF→CRLF警告3件は一時fixtureでの改行確認時に出たもので、exit0・全チェック合格。

Gitが4件見つかる同じ環境で、選択件数1・SourceはSystem.String・選択先は/usr/local/bin/gitとなることも読み取り確認。PATHは変更していない。

API-onlyコンパイルはb03a981でPASS済み。C#本体とcompile.rspが不変なので、今回は再コンパイルしていない。

## 不変確認

`AndroidReleaseBuild.cs`と.meta、`build-android-release.ps1`、既存`AndroidBuild.cs`／`build-android.ps1`は修正前とSHA256まで一致。Assets／ProjectSettings／Packages／Visual Lockも不変。

**source_unchanged=true**。承認されたGit探索だけのテスト編集は別記し、既存ゲームやビルダーまで無変更と混同しない。

- Unity起動0、AAB/APKビルド0、Pixel操作0。
- 第7工程テスト再実行0。
- 実署名キーの使用0。Windowsの正式署名preflight自体は未実行。
- 今回のfirst_error／runner_error／failed_phaseは全てnull。新しいfirst-errorを生成していない。

root first-error.log／failure.log／latest/error.logはb03a981までの**旧失敗原本**として変更せず保持した。旧限定テストlogも保持し、今回の成功ログとはパスを分離。直前の結果JSON等を`validation/history-b03a981/`へ保存した。c988164／ba32883と第7工程の履歴も保持。

## 判定

**Git探索修正：YES。コード変更1ファイル。限定releaseテストPASS、exit0、Total34／Passed34／Failed0。**

**次に実AAB/APKビルドへ進んで安全か：YES（次のWindows工程への着手可）**。

Windows正式repo clean・既存のローカル正式署名preflight合格を実行前の必須条件とし、AABを1回、成功時だけAPKを1回とする。今回その操作を開始していない。実ビルド成功や署名・実機確認の合格を今回確定したものとは扱わない。

**第8工程 complete=false**。Pixel／保存復帰／Visual／Google Play提出準備の最終合格は未確認。Google Playへ提出していない。

固定共有：`References/Review/inbox/latest-release-candidate/`。commitは上記コード修正の確認対象SHAを示し、共有文書自身のcommitと区別する。

Codex／AI側から見た次の改善候補：今回はなし。
