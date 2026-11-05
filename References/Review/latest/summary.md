# 最新レビュー共有：Windows診断共有機構

- 現在ブランチ：`feat/daily-world-pass-20261016`
- 最新の確認対象作業commit SHA：`d0ccd9ac23f143ef0bd823aaaecfdd3489bb584f`
- フォント製品候補commit SHA：`193ab6eeb216d9f457096d9f3df305704f2f962e`（変更なし）
- 今回の目的：Windowsで現リポジトリだけを検索して安全停止した問題を局所修正し、兄弟worktreeの最新失敗runをパス入力なしで検出する。
- 実施内容：実行元リポジトリを先に確認し、その親フォルダ直下の兄弟作業フォルダも列挙。各Builds/Diagnostics/font-only-*を対象に、更新日時が最も新しい失敗runを全候補から1件だけ選ぶ。Windows例のfont-afc3c3d1も含む。親フォルダの外や深い孫プロジェクトを再帰検索せず、リンク先もたどらない。
- 成功／失敗：一時フォルダで検索ロジックのみ12ケース成功。スクリプト本体・コピー処理・Git共有処理は今回実行していない。既存Git安全処理6関数は修正前後で同一。共有機構の実装状態は完成を維持し、実Windowsの共有成功を主張しない。
- 根本原因：共有検索が実行元のBuilds/Diagnosticsに固定され、兄弟のfont-afc3c3d1等を検索していなかったこと。Unity exit 2自体の根本原因は引き続き未確定で、今回調査・再実行していない。
- 修正内容：実装変更はshare-latest-font-diagnostics.ps1の検索・失敗判定と取得元の記録だけ。run-info.txtへSource repository/worktree、Source run full path、Selected run nameを追加し、元のSource repository HEAD表記をPublishing repository HEADへ明確化。取得元と公開先を混同しない。CMD・既存Git制限・ゲーム・Diagnostics原本は変更していない。
- 次にやること：この修正と共有記録をpushして停止。実Windows Diagnostics共有はまだ実行しない。後日許可された際も同じCMDで動作し、毎回のパス入力やファイル移動は不要。

## 今回の変更ファイル

- `scripts/share-latest-font-diagnostics.ps1`：兄弟worktree検索と最新失敗runの選択、取得元の明示。
- `References/Review/latest/summary.md`：局所修正と検索検証の結果。
- `References/Review/latest/status.json`：対象SHAと検索範囲・未実施事項。

CMDとscripts/.gitattributesは前回のまま。Windowsの通常配置では検索上限は `C:\Users\User\Desktop\Mofumachi` で、その直下の各作業フォルダのBuilds/Diagnosticsだけを対象とする。

## Windows PowerShell 5.1静的確認

| 対象 | 確認結果 |
| --- | --- |
| UTF-8／BOM／CP932 | PS1・CMDはASCIIのみ。BOMなしでもCP932との解釈差がない。他の日本語PS1のBOMは変更しない |
| `::new()` | PowerShell 5.0以降の構文。使用Encodingコンストラクタを.NET Framework 4.8参照メタデータで照合 |
| Generic Collection | List／Stackと必要メソッドを.NET Framework 4.8参照で確認 |
| `Get-FileHash` | PowerShell 4.0以降の標準機能。SHA256を明示 |
| `Sort-Object` | 5.1で利用できる計算プロパティ・降順指定を使用 |
| Windowsパス | .NET FrameworkのパスAPIとLiteralPathを使用。空白・角括弧・日本語パスを検証。Core専用GetRelativePath等は不使用 |
| Git出力の文字コード | Git呼び出し中だけUTF-8へ揃え、Console／OutputEncodingをfinallyで復元。CP932条件で日本語リポジトリの誤読を再現し、修正後成功を確認 |
| Gitのstderr | 5.1のNativeCommandError表現を考慮し、stderrの有無でなくネイティブ終了コードで成功を判定 |
| CMD | ASCII・CRLF。System32のWindowsPowerShell/v1.0/powershell.exeをRemoteSignedで起動。戻り値を保持して表示を残す |

これは静的互換性確認であり、Windows 11／Windows PowerShell 5.1で実行済みという意味ではない。今回、実Windowsでの共有操作は未実施。

## Gitの範囲と停止条件

- add対象は `References/Review/inbox/latest/` だけ。commitも `--only` と同じパスを指定し、他のステージ済み・未ステージ変更を保持する。ignoredのログも対象ディレクトリ内だけでaddする。
- 共有先専用の属性とGit blob照合により、CRLF／UTF-16等のログ原バイトがGitによって変換されることを防ぐ。元run名・元パス・コピー時刻・ファイル一覧・SHAを記録する。
- push対象は確認済みcommit SHAと現在のfeatureブランチのみ。force push・reset・stash削除・自動merge・自動再試行は実装していない。
- 共有外の未pushコミット、リモート進行・分岐、detached HEAD、Git操作途中等は安全に停止する。Git失敗時は原因を表示し、1回で停止。ローカルhookによる別ファイル変更もこの処理内で防ぐ。
- Git共有の22ケースと重点3ケースは前回の確認結果。今回は指定どおり検索のみ12ケースを実行し、コピー・Git共有の再実行はしていない。検索ケースはローカルDiagnostics不在で兄弟から取得、ローカル優先ではなく全体の最新失敗を選択、新しい成功runの除外、親外・深い階層の除外、XML／compilerエラーの失敗判定、未確認runの除外、0件停止、リンク除外、元ハッシュ／更新日時不変。仮のログを実証跡として保存しない。

`commit`は確認対象の作業コミットで、共有情報だけの更新コミットとは区別する。`capture_verified`／`pixel_verified`／`formal_fixed`／`adopted`はフォント候補について未確認のfalseを維持。error.log・visual-review.md・images/は今回変更せず、実ログや画像を作成・流用しない。

- [機械判定用status.json](status.json)
