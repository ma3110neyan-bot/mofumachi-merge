# 第7工程：v5 Phase2のUnity起動処理へ到達・完了待ち

- 更新：2026-11-09（日本時間）。状態：**調査中**。
- branch：`feat/daily-world-pass-20261016`。
- 固定ゲーム実装：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- 既存v5ランチャー：`9ee98de7e58eb75cc624c9f30cc055686afebd2a`。
- Fetch1回でGitHub先端`3891889a427ad1404eb42e33a2dbc02ffaa1ab09`を確認。inbox rootは旧v4の保存結果で、v5の正式terminal結果は未共有。
- 今回はユーザー画面と既存コードの照合のみ。ゲーム・テスト・ランチャー・Visual Lockは無変更、CMD／Unity／テスト起動0、再試行0。

## 確認対象

`progress-ui-integration-progress-ui-e-drive-v5-20261109-101604-63c260c0`

ユーザーが既存runのroot JSONと、10秒間隔のUnityプロセス／directログ状態を読み取り、その表示画面を提供した。

## 画面で確認した状態

| 項目 | 表示 |
| --- | --- |
| root result | Running |
| Phase1 | **Passed** |
| Phase2 | NotRun |
| first_error / runner_error | null / null |
| direct log bytes | 両sampleとも3,513,166 |
| direct log更新時刻 | 2026-11-09 10:56:09.7731476 +09:00 |
| sample1 | 10:58:57.8507550 +09:00 |
| sample2 | 10:59:08.0162889 +09:00 |
| 対象Unityからたどったprocesses | 両sampleとも空配列 |

先に受領したstartup logは10,562 bytes、末尾はInitial Refresh Startだった。その後directログは約3.5MBへ増え、Phase1がPassedまで進んだ。先のsnapshotのまま停止していたとは扱わない。

公開済みv5コードは、指定55ケースのXML成功とsource内容不変確認の両方が通った後にdirect.result=Passedを書き込む。この制御順序から、以前のsource判定箇所を通過したと解釈できる。**ただし今回の画面には実XML件数やsource_unchangedフィールド自体は出ていないため、原本照合は未完了**。

## processes空配列の解釈

今回の読み取りコマンドは、run名を含むUnity.exeを起点に、その子プロセスだけを列挙した。

対象Unityが終了したPhase1後で、Phase2のUnityがまだ起動していなければ、空配列になる。これはPhase1 Passed／Phase2 NotRunと整合する。

**親のPowerShell・cmd・worktree作成中のgitの生存を確認したコマンドではない**。従って空配列から検証全体の終了・ハングを断定しない。direct完了後にdirectログが増えないことも、それだけでは異常と判定しない。

コード上、この保存状態はintegration worktree作成・検証から次のUnity起動までの準備段階と整合する。正確なpreparation_step、Git作成の終了コード、現時点の親プロセス状態、元CMDの最新表示は未取得。

## 最新追記：Phase2起動処理への到達

ユーザーが元CMDの末尾を提示。integration worktree作成の表示に続き、`integration: exactly the pinned EditMode fixtures, one Unity invocation, Burst disabled.`まで進んでいる。

公開済みコードの位置から、Phase1のテスト合格・source不変確認と、integration worktreeの事前確認を通過したと判断できる。この表示はProcess.Start直前であり、実際の起動成否・終了コード・444件結果はまだ未取得。

停止／エラー表示はない。処理中の進捗をconsoleへ逐次出さず、Unityの終了待ちをするため、画面が変わらないことだけで停止とは判断しない。

今回の追加確認は画面と既存コードの照合だけ。新しいCMD／Unity／テストは起動していない。Fetch0回。

## 次の最小確認

**元CMDをそのまま保持し、既存Phase2の終了と自動共有を待つ。** 新たな実行や手動でのPhase2開始は不要。

新しい検証CMD・Unityは起動せず、既存CMDを閉じない。旧run、receipt、Diagnosticsを削除・変更しない。新しい検証IDや推測によるコード修正を行わない。

現在の記録にfirst/runner errorはなく、次の準備状況を確認する段階。Phase2失敗と決めつけたり、Phase1のやり直しへ進んだりしない。

## 共有と保持

変更はsummary/statusだけ。先に保存したv5 startup抜粋、旧v4の55/55実XMLと全履歴は保持。

v5の画面でのPassedを、未受領のXMLやPhase2成功へ置き換えない。次のterminal結果取得時に、同じrunのPhase1 55/55、Phase2 444/444、Unity exit0、source_unchanged=true、自動再試行0を確認する。

- **第7工程 complete=false**。
- **第8工程「Pixel 3a実機確認＋リリース候補ビルド」へ進んで安全か：NO**。
- 理由：**Phase2は起動処理へ進んだが、444/444・exit0と最終source不変結果が未確認**。

`commit`は既存v5ランチャーを示し、今回の観測共有commitとは区別する。追加改善候補は今回はなし。
