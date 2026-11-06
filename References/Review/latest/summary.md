# SaveData v4：ワンクリック検証・共有ランチャー準備完了

更新日：2026-11-06（日本時間）。状態：**ランチャー作成・公開は完了／SaveData実検証はWindows実行待ち**。

- branch：`feat/daily-world-pass-20261016`
- 対象実装commit：`2b81174496a5b4703bad0516645829893d4297b9`。SaveData v4は再実装していない。
- ランチャーcommit：`c59a0febbcfa1cef13086e9b467f2f1fa42041ee`。共有文書commitとは別。
- **savedata_windows_launcher_ready=true**
- 実行ファイル：`scripts/verify-savedata-v4-windows.cmd`（補助PS1は同じscripts内）。
- 今回の目的：Windowsで1回の実検証と結果共有まで行える状態にする。
- 今回の変更：専用CMD／PS1、summary.md／status.jsonだけ。ゲーム・SaveData・テストソース・正式素材は変更していない。
- クラウドUnity／SaveDataテスト／Guard再実行／環境再調査：**0回**。
- SaveData第1工程完了：**false**。第2工程へ進める：**false（次のレビュー待ち）**。
- 公開状態：**通常push成功**。公開commit `fa82a9ae606a106cb2f214d30a75635f68385bfc`、GitHub側HEADの読み戻し一致、CMD／PS1両方のblob存在確認済み。共有記録の最新化は後続commitで行う。

## ユーザーが次にする操作

**`scripts/verify-savedata-v4-windows.cmd`を1回ダブルクリック。**

Windows 11／Windows PowerShell 5.1／Unity Hub既定場所の6000.6.4f1＋Android Build Support／既存Windowsライセンスを使用。起動中のUnity Editorは事前に閉じる。

元作業ツリーのブランチ・index・既存変更・既存セーブを変更せず、対象commit固定のdetached worktreeを作る。対象commitがローカルにない場合だけ取得する。元ブランチのpull／merge／reset／stashは行わない。

## 検証scope

**SaveData検証は独立したCore EditModeテストとして実行する。Noah候補やキャラのVisual Guardを開始条件・合否条件にしない。** これは今回の明示指示によるscope分離であり、Guard実装やVisual Lock原本はそのまま保持する。

Guard変更／無効化／弱体化：**NO**。Visual Lock原本変更：**NO**。TownActor変更：**NO**。過去のNoah不一致の調査・失敗実行は繰り返していない。

既存Unity Test FrameworkのコマンドラインEditMode runnerを使用し、assemblyは`Mofumachi.EditModeTests`、Android target。完全な名前空間prefixで以下だけに限定する。

| 対象fixture | 固定ソースの期待ケース数 |
| --- | ---: |
| Mofumachi.Tests.SaveServiceTests | 33 |
| Mofumachi.Tests.PreferencesTests | 23 |
| Mofumachi.Tests.CharacterPreferenceTests | 7 |

合計**63ケース**。Unity起動最大1回・自動再試行0回。同commitの起動記録は排他作成し、2回目の実行を拒否する。全件・PlayMode・APK・Pixel・Visual撮影・フォント工程は実行しない。MergeProgressionUiTestsのsaveVersion期待値変更は第1工程合否に含めない。

## 結果と判定

`Builds/Diagnostics/savedata-v4-<timestamp>-<id>/`へ保存する。

- 必須：`results.xml`、`unity.log`、`source-revision.txt`、`savedata-v4-result.json`
- 補助：`stdout.log`、`stderr.log`、`unity-arguments.txt`
- 失敗時：`first-error.log`。最初のコンパイル／起動エラー、または最初のtest failure全文とstack。抽出不能なら未確定と記録し、exit codeだけを原因にしない。Unity未起動ならXML/logを捏造せず、Blockedと理由をJSONへ記録する。
- 共有結果：`share-result.json`。検証JSONとは分離し、共有した証跡の元バイトを後から変更しない。

`savedata-v4-result.json`はsource_commit、Unity version、requested_tests、total、total_tests、passed、failed、skipped、first_error、automatic_retries、save_version、validation_passedを保存する。件数は実XMLから取得し、Unity終了コードが非0の場合もXMLがあれば記録する。未起動／XML未取得の件数はnullのままとし、成功を推測しない。

JSONの14確認項目には、新規24P、v1/v2/v3移行、既存情報保持、初回24P一度限り、再読込で再付与なし、0Pの再初期化なし、pre-v4生原本保持、上書きなし、不正v4停止、未対応版停止、移行失敗時の新規置換防止、CopyFrom/rollbackを含む。成功時は対応する実際のtest-case名を記録する。

63件すべてのPassed・skip/inconclusiveなし・必須証拠・固定ソース一致を満たした場合のみ検証成功。失敗時は再実行・自動修正せず停止する。**Windowsで成功してもstage2_safe=false／review_required=trueを維持し、次回レビューで初めて第2工程YESを判断する。**

## 同じ操作内の結果共有

検証の成功・失敗にかかわらず、存在する小容量テキスト証跡だけを`References/Review/inbox/latest-savedata/`へ自動共有する。1ファイル4MiB、合計16MiBを上限とし、APK・画像・巨大ZIP・Library等をコピーしない。run-info.jsonに元run・SHA・ファイル一覧を記録し、Git格納後も生バイト一致を確認する。

公開用には**GitHub側の最新feature HEADを親にする別worktree**を使う。`latest-savedata/`だけをadd／commitし、実commitの差分がその配下だけであることを検証して通常pushを1回行う。元のローカルHEAD／index／staged変更／未pushコミットは使用・変更しない。リモートが先行していても元ブランチを同期操作せず、リモートの他の更新を保持する。

既存hookは有効のまま。force push・reset・stash・既存変更破棄なし。GitHubの同時更新等でpushが拒否された場合も再試行せず、理由を表示し、全Diagnosticsと隔離worktreeを保持する。

## 今回実施した関連確認

一時ローカルGit環境による共有処理の限定確認：**2/2合格**。

- リモート先行＋無関係なローカル未push／staged／未追跡変更がある成功条件：共有先だけcommitされ、リモートの他ファイル・元HEAD・indexの生バイト・元ファイルを保持。古いrunのファイルも新runへ混在しない。
- push拒否条件：push試行は1回で停止し、元HEAD／index／ファイル／ローカル証跡を保持。

合格／失敗の合成XMLを使った件数抽出の限定確認も2/2合格。実Unity・SaveDataテストを実行した結果ではない。

構文エラー0件（PowerShell 7.4.6 parser）。PowerShell 5.1で利用可能な構文・APIを静的確認。PS1はUTF-8 BOM、CMDはASCII CRLF。**この限定確認をWindows 5.1実行合格やSaveData移行合格として扱わない。** 実Unity結果はまだ未取得で、全必須項目は未実行。

次の作業はユーザーによるCMDの1回実行と、その共有結果のレビューだけ。現時点では第2工程へ進まない。

## 過去の公開停止と今回の解消

以前は通常環境のネットワーク無効とproxy:8080接続失敗、承認UI非表示により公開できなかった。**今回、許可された制限外Git実行で通常pushを1回行い成功した。** `git ls-remote`でもGitHub側feature HEADが公開commitと一致し、同commit内のCMD／PS1両blobを確認した。環境設定・Guard・Visual Lock・TownActorは変更していない。

過去の停止理由と未公開コミットの33KB退避bundleは履歴として保持する。現在のランチャー公開のブロッカーではない。
