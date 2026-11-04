# 最新レビュー共有

- 現在ブランチ：`feat/daily-world-pass-20261016`
- 最新の確認対象作業commit SHA：`1d86ddddae7737ebed3dfcab90ec55feb2dcc028`
- フォント製品候補commit SHA：`193ab6eeb216d9f457096d9f3df305704f2f962e`（変更なし）
- 今回の目的：Windowsにある最新font-only診断runの小容量テキストを、1つのコピー専用スクリプトでGitHub共有用フォルダへ揃える。Unity exit 2の根本原因調査につなぐ。
- 実施内容：`scripts/share-latest-font-diagnostics.ps1`を追加してpush。スクリプト自身があるリポジトリの`Builds/Diagnostics/`直下で更新日時が最新の`font-only-*`を1件選択し、存在する小容量テキストだけを`References/Review/inbox/latest/`へコピーする。capture.log／capture.xml／font-only-source-info.jsonを優先し、Editor／stdout／stderr／compiler等のログも対象。下位パスと元バイト列を保持する。run-info.txtに元run名・元パス・コピーUTC日時・ファイル一覧・サイズ・SHA256・除外理由を記録する。
- 成功／失敗：コピー専用処理の関連12ケース成功、構文解析エラー0。確認はPowerShell 7.4.6/Linuxの一時フォルダのみ。Windows PowerShell 5.1での実行・実Windows診断ログの共有はまだ未確認。Unity側の調査・フォント候補確認は未完了なので`success=false`、`tests_passed=false`を維持する。これはコピー関連12ケースが失敗したという意味ではない。
- 根本原因：未確定。クラウドの`/workspace/mofumachi-merge/Builds/Diagnostics/`にはfont-only runがなく、Windows側の最新runは未取得。PowerShellの61行目や過去runの原因を今回の原因として扱わない。
- 修正内容：実装追加は上記コピー専用PS1の1ファイルのみ。既存共有summary/statusを最新化。ゲーム本体・フォント・UI・Merge・SaveData・Core・正式素材・キャラ・街・BGMは変更していない。元Diagnosticsも変更・削除していない。
- 次にやること：失敗runがあるWindowsリポジトリへスクリプトを反映し、1回実行する。スクリプトはGit操作を行わないため、生成されたinbox/latestの公開にはWindows側で別途commit/pushが必要。公開後はその1runだけを読み取り、指定7項目を記録・pushして修正案の提示で停止する。Unity／capture再実行・ゲーム修正・全件テスト・APK・Pixelには進まない。

コピー上限は1ファイル4MiB／合計16MiB／64件。画像・APK・ZIP・Library／Temp／Build等を除外し、リンクをたどらない。上限超過やバイナリは切り詰めず除外して理由を記録する。コピー元とコピー先のSHAを検証してから共有先を置き換える。元run不在・対象テキスト0件・コピー失敗の場合は以前の共有先を維持する。

関連12ケース：最新日時の選択、元SHA／更新日時不変、旧共有ファイル置換、任意ファイル欠損、入れ子・日本語／角括弧パス・UTF-16ログ、画像／バイナリ／Build関連除外、ファイル／合計容量・件数上限、対象run不在、対象テキスト不在、リンク元／リンク先の拒否、JSON未生成の失敗runも選択。実Unityやゲームのテストではない。スクリプトはASCIIのみで、UTF-8日本語PS1をCP932として読む問題を持ち込まない。

指定7項目はまだ未判定：最初のUnity側エラー全文、発生ファイル、行番号、フォント変更との関係、最小修正箇所、修正対象ファイル数、推奨修正案。error.logは空のまま維持し、仮のエラーやコピー用fixtureを本物の証跡として保存しない。画像・visual-review.mdは変更していない。

`commit`はこの報告の確認対象作業コミット。共有情報だけの更新コミットとは区別する。`capture_verified`／`pixel_verified`／`formal_fixed`／`adopted`は現在のフォント候補の確認状態で、すべてfalseのまま。過去のMerge Visual Lockを取り消すものではない。

- [機械判定用status.json](status.json)
- [根本エラー抜粋](error.log)
- [Visual確認情報](visual-review.md)
