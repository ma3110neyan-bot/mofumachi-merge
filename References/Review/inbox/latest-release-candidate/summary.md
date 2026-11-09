# 第8工程：API参照競合は解消・限定releaseテストで停止

状態：**原因確定**。参照修正とC#コンパイルは成功。本小工程の「限定テストPASS」は未達のため、実AAB/APKビルドはまだ実行しない。

- branch：`feat/daily-world-pass-20261016`。
- 確認対象HEAD：`ba328838d1ca72ec5abc0ba8b5f259ca445070a0`。
- release入口追加commit：`ea503db12f34d0eaec418f4aaa507b2d4512d469`。
- 固定ゲーム：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- 第7工程はcomplete=trueを維持。正式v5の55/55・444/444は再実行していない。

## 参照構成の最小修正

Unity6000.6.4f1の実DLLをCecilで読み、必要なEditor型11種が両DLLで実際に定義されることを確認した。Unityを起動せず、メタデータだけを読んだ。

`Managed/UnityEditor.dll`に全11型があるため、`validation/compile.rsp`から`Managed/UnityEngine/UnityEditor.CoreModule.dll`の参照1行だけを削除。その他の参照・オプション・対象ソースは変更なし。根拠は`validation/editor-type-definitions.json`、厳密な1行差分は`validation/reference-only.diff`。

API-onlyコンパイルは**今回1回、exit0、PASS**。CS0433なし、最初のC#エラーなし。`AndroidReleaseBuild.cs`は変更せず型解決・コンパイルに成功。出力DLLのサイズ・SHAをstatic-validation.jsonへ記録し、DLL自体をGitへ追加していない。

既存のコンパイラオプション警告CS2023（rsp内の/noconfigを無視）は残るが、ソースエラーではなくexit0。参照だけの変更範囲を守り、今回そのオプションを変更していない。

## 限定テストの最初の失敗

コンパイル成功後、既存`scripts/tests/android-release.tests.ps1`を**今回1回**実行し、exit1で停止。

```text
&: /workspace/mofumachi-merge/scripts/tests/android-release.tests.ps1:52
Line |
  52 |  & $Git -C $Repo init --quiet
     |    ~~~~
     | The term '/usr/local/bin/git /usr/bin/git /bin/git
     | /opt/codex/runtimes/codex-primary-runtime/dependencies/bin/fallback/git'
     | is not recognized as a name of a cmdlet, function, script file, or
     | executable program. Check the spelling of the name, or if a path was
     | included, verify that the path is correct and try again.
```

根本箇所は51行目の`$Git = (Get-Command git -CommandType Application).Source`。この環境ではGitが4件見つかり、SourceはSystem.Object[]になる。52行目でその配列を1つの実行ファイルとして起動しようとして失敗した。

読み取り確認で、`/usr/local/bin/git`、`/usr/bin/git`、`/bin/git`、fallback/gitの4件を確認。記録は`validation/git-discovery.json`。Git fixtureの初期化に未到達で、releaseビルダーやゲーム実行の失敗とは判定していない。チェック完走・PASSにはしていない。

今回の変更対象は参照構成だけなので、テスト／PowerShellビルダーを修正せず、PATHを変えた再実行も行わず停止した。自動再試行0。

## ソースと実行範囲

- `AndroidReleaseBuild.cs`と.meta、`build-android-release.ps1`、`android-release.tests.ps1`はba32883から生バイト不変。
- 既存`AndroidBuild.cs`と`build-android.ps1`も不変。
- ゲーム・Packages・ProjectSettings・Visual Lockは不変。`source_unchanged=true`。
- Unity起動0、実AAB/APKビルド0、Pixel操作0、第7工程の再実行0。
- 実署名キー・パスワードは使用していない。限定チェック内の署名情報は合成fixtureのみ。

現在のfirst-error.log／failure.log／latest/error.logはこの限定テストの最初のエラー。前回のCS0433ログ・rsp・結果・first-errorは`validation/history-ba32883/`へ同じバイトで保持。以前のc988164 preflight履歴も保持。

## 最終判定

| 項目 | 結果 |
| --- | --- |
| 参照競合修正 | **YES** |
| API-only compile | **PASS、exit0** |
| release限定テスト | **FAIL、exit1** |
| releaseビルダー本体変更 | NO |
| QA経路変更 | NO |
| ゲーム変更 | NO |
| Visual Lock変更 | NO |
| source_unchanged | **true** |
| 実AAB/APK build | NO |
| 次に実AAB/APKビルドへ進んで安全か | **NO** |
| 第8工程 complete | **false** |

主要ブロッカーは限定テストのGit探索が配列を返すこと。次の最小候補は**テスト補助のGit探索を1実行ファイルへ明示的に限定すること**。今回の参照修正範囲外なので実施していない。ゲームやreleaseビルダーの変更は必要と判断していない。

固定共有：`References/Review/inbox/latest-release-candidate/`。この文書のcommitは共有結果で、上記確認対象HEADや既存release実装commitとは区別する。

Codex／AI側から見た次の改善候補：今回はなし（上記の必須検証ブロッカーのみ）。
