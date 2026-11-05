# 最新レビュー共有：Windows診断共有機構

- 現在ブランチ：`feat/daily-world-pass-20261016`
- 最新の確認対象作業commit SHA：`7c2082e4b0bb8a90e198203f6365032a014afd71`
- フォント製品候補commit SHA：`193ab6eeb216d9f457096d9f3df305704f2f962e`（変更なし）
- 今回の目的：Windows PowerShell 5.1向けの、最新Diagnostics取得から共有先限定commit・featureブランチへのpushまでを1回で行う機構を確定・公開する。
- 実施内容：前回の中断時にローカルだけに残っていた上記作業コミットをGitHubへpushし、CMD追加・PS1更新・CMD用改行設定の公開を確認。共有statusを今回の完成状態へ更新。元コードは前回の関連22ケース通過時と同一で、再開後の静的確認と重点3ケースも成功。
- 成功／失敗：共有機構の実装・静的確認・一時Gitリポジトリでの検証は成功。`state=diagnostics_sharing_mechanism_complete`、`automatically_commits_or_pushes=true`。`success=true`と`tests_passed=true`の対象は共有機構のみであり、ゲームやフォント候補の合格ではない。
- 根本原因：Unity exit 2は引き続き未確定。実Windowsの最新Diagnosticsは今回取得・公開していない。PowerShellの61行目や過去runの原因を流用しない。
- 修正内容：PS1に共有先限定のGit操作を追加し、ダブルクリック用CMDと、そのCMDだけをCRLFにする属性を追加した。ゲーム本体・フォント・UI・Merge・SaveData・Core・正式素材・キャラ・街・BGM・Diagnostics原本は変更していない。
- 次にやること：今回は機構と共有記録の公開で停止。実際の共有実行が別途許可された際の操作は、失敗runがあるfeatureブランチのWindowsリポジトリで `scripts/share-latest-font-diagnostics.cmd` をダブルクリックするだけ。Unity・captureの再実行は不要。

## 追加・変更ファイル

| ファイル | 内容 |
| --- | --- |
| `scripts/share-latest-font-diagnostics.ps1` | 最新runの小容量テキストコピー、共有先だけのadd／commit、同じfeatureブランチへのpush |
| `scripts/share-latest-font-diagnostics.cmd` | Windows標準のpowershell.exeを呼ぶダブルクリック起動。失敗理由を表示して停止 |
| `scripts/.gitattributes` | 上記CMDだけをCRLFでチェックアウトする指定 |
| `References/Review/latest/summary.md` | 今回の公開・静的確認結果 |
| `References/Review/latest/status.json` | 機械判定の完成状態、対象SHA、自動commit/push有効化、未実施事項 |

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
- 関連22ケースは一時ファイル／一時Gitリポジトリ／ローカルbare remoteで確認済み。再開後は未コミット変更保持、日本語パスとCP932、push拒否時に1回で停止する重点3ケースを再確認。Windows本番共有・Unity・ゲームのテストではない。

`commit`は確認対象の作業コミットで、共有情報だけの更新コミットとは区別する。`capture_verified`／`pixel_verified`／`formal_fixed`／`adopted`はフォント候補について未確認のfalseを維持。error.log・visual-review.md・images/は今回変更せず、実ログや画像を作成・流用しない。

- [機械判定用status.json](status.json)
