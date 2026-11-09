# 第8工程：release専用入口追加済み・静的検証で停止

状態：**修正済み・未確認**。本タスクの完了条件「static validation通過」は未達。Windowsでまだ実行しない。

- branch：`feat/daily-world-pass-20261016`。
- release経路追加commit：`ea503db12f34d0eaec418f4aaa507b2d4512d469`。
- 固定ゲーム実装：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- 第7工程：complete=trueを維持。v5の55/55、444/444を再実行していない。

## 追加したファイル

- `Assets/Mofumachi/Editor/AndroidReleaseBuild.cs`と.meta：AAB/APKの独立入口。Development・Script Debugging・Deep Profiling・Autoconnect Profiler OFF、BuildOptions.None、IL2CPP、ARM64、Portraitを明示。
- `scripts/build-android-release.ps1`：4環境変数の署名preflight、公開証明書照合、E上の隔離worktree、AAB成功時だけAPK、各出力1回限りのreceipt、全追跡内容比較とroot .vsconfigのみのallowlist、秘匿値を除いたログ・メタデータ保存。既定はpreflightのみ。実ビルドには明示的な-Buildが必要だが、**今回は未検証のため実行を案内しない**。
- `scripts/tests/android-release.tests.ps1`：新経路だけの限定確認。ゲームfixtureへ追加していない。実装後の実行は下記停止により未実施。

`AndroidBuild.cs`と`build-android.ps1`は固定製品と生バイト／SHA256が一致。既存Development/QA経路・ゲーム・Save・UI・Visual Lock・前工程ランチャーに変更なし。

署名はWindowsローカルの`MOFUMACHI_KEYSTORE_PATH`、`MOFUMACHI_KEYSTORE_PASSWORD`、`MOFUMACHI_KEY_ALIAS`、`MOFUMACHI_KEY_ALIAS_PASSWORD`から取得する実装。実際の秘密値・鍵を読み込まず、Git／JSON／ログへ保存していない。コード内のfixture文字列は合成値。

## 最初の停止と根本原因

Unityを起動しないAPI-only C#コンパイルを1回実行し、exit1で停止した。

```text
Assets/Mofumachi/Editor/AndroidReleaseBuild.cs(29,27): error CS0433: The type 'BuildFailedException' exists in both 'UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
```

こちらで組んだ検証用参照一覧が`Managed/UnityEditor.dll`と`Managed/UnityEngine/UnityEditor.CoreModule.dll`を同時に参照し、Editor型が重複した。113件すべてCS0433で同一の参照競合。**検証コマンドの構成が原因であり、製品のUnity起動・ビルド失敗やゲームソース不具合とは確定していない。**

ユーザー指示の「releaseビルダー追加後にコンパイルエラーがある場合は停止」に従い、コードを追加修正せず、参照修正後の再コンパイルも行っていない。wrapperのthrowを根本原因にしていない。

原本`validation/api-compile.log`と`validation/compile.rsp`、最初のエラー`first-error.log`、検証結果`validation/static-validation.json`を保存した。実装前の期待されたRED（入口未作成）は`validation/red.log`へ保存。実装後の限定チェックはNotRun。合格や静的検証完了へ置き換えていない。

## 判定

| 要件 | 現在の状態 |
| --- | --- |
| release専用入口追加 | YES（未検証） |
| 既存QA経路変更 | NO |
| AAB／APK対応 | YES（実装上の対応・実ビルド未確認） |
| Development OFF強制 | YES（コードへ明示・実効設定未確認） |
| 正式署名preflight | YES（実装済み・Windows実行未確認） |
| 実秘密値Git混入 | NO |
| source_unchanged | true（既存ゲーム・既存QA・設定・Visual Lock。新しいツール追加は別記） |
| static validation | **未合格：API参照競合で停止** |
| 実ビルド | NO |
| 第8工程 complete | **false** |
| 次にAAB/APK実ビルドへ進んで安全か | **NO** |

Unity起動0回、AAB/APK生成0、ゲームテスト再実行0、自動再試行0。Windows正式repoとE上の履歴に触れていない。Pixel確認／Google Play提出へ進んでいない。

次の最小候補は、**一時検証用コンパイラの参照一覧だけを正し、Editor型の定義元を重複させないこと**。このエラーからゲームコードや既存QAビルダーの修正は必要と判断していない。今回はその修正・再実行を行わず停止。

前回preflight記録c988164は`previous-preflight-c988164/`へ同じバイトで保持した。履歴のchecksumsは当時のcommitツリーに対するもの。今回checksumsは現行文書・原本ログ・追加ツール／既存入力のハッシュを別途記録する。実AAB/APKや実機画像はない。

固定共有：`References/Review/inbox/latest-release-candidate/`。第7工程の正式記録はlatest下と元commit740b571に保持。

Codex／AI側から見た次の改善候補：今回はなし（上記の検証入口整合だけが必要）。
