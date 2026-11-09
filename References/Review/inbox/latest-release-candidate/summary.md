# 第8工程：Windows正式署名準備完了・preflight未実行

状態：**完了（署名準備の小工程のみ）**。第8工程全体は未完了。

- branch：`feat/daily-world-pass-20261016`。
- Windows公開署名状態commit：`8c8579026b43359f240ade43fd3eaf863f354d9c`。
- 署名補助実装：`c486b37df3a7c17acfbd852e2b1a4f2d5d95bf77`。
- 固定ゲーム：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- 第7工程complete=true、release API compile PASS／34チェックPASS、署名補助26チェックPASSの以前の証拠は保持。再実行なし。

## 公開7項目の照合

GitHub共有の`signing-status.json`を取得し、以下の7項目だけであること、2つのSHAが提示されたWindows成功画面と完全一致することを確認した。原本JSONは変更していない。

| 許可された記録項目 | 共有結果 |
| --- | --- |
| keystore_prepared | **true** |
| alias_configured | **true** |
| env_vars_configured | **true** |
| certificate_sha256 | `96bb1402b8085570305a0486ae3f7fa3fafe2c60a3b8cd062c2ee8e0f81b8e09` |
| keystore_sha256 | `faac2959a5e9ae6e5e531c110955a2085502d13da7cc96148d91066ad666b4cb` |
| backup_pending | **true** |
| secrets_recorded | **false** |

共有commitの親は2f29b07。変更pathはこの公開JSON1個のみ。秘密鍵本体・password・source変更を含めていない。Codexはprivate keystoreを取得・開封していない。

実作成のpath／alias名／sizeはユーザー画面で確認し、指定どおり実署名記録としてGitへ追加するのは上記7項目に限定した。パスワードのマスク入力数や長さも記録していない。

## 次へ進める範囲

**Windows release preflightへ進んで安全か：YES（同じPowerShellセッションでのpreflight専用確認まで）**。

4環境変数は成功時点のProcess scopeだけ。画面を開いたまま保持し、キー作成CMDを再実行しない。閉じた場合は新しいキーを作らず、既存キーに対する環境変数の再設定を別途行う。

バックアップは未実施のまま。外部媒体／クラウドへのキーコピーは行っていない。backup_pending=trueを保持する。

今回は公開結果の照合だけで、release preflight／AAB／APK／Pixelは未実行。次の実ビルドsource HEAD、ビルド後のsource不変、正式署名による成果物、実機合格はまだ確定しない。公開署名状態のcommitと、将来の実ビルドsource commitを混同しない。

**第8工程 complete=false**。preflight以降へ自動で進んでいない。

## 今回のレビュー操作

Fetch1回で共有commitを取得。公開JSON1pathだけのincoming差分を確認後、証跡だけをfast-forward同期した。キー作成・キー検証・password入力・テスト・Unity・ビルドは全て0回。自動再試行0。ゲーム／既存release・QAビルダー／設定／Visual Lockは変更なし。

更新前の準備待ち記録は`validation/history-2f29b07/`へ保存。第7工程・34チェック・以前のfailure原本も保持。今回のfirst_error／runner_errorはnull。古いfailure.logやlatest/error.logは今回の署名失敗を示さない。

固定共有：`References/Review/inbox/latest-release-candidate/`。commit欄は今回レビューした公開証跡SHAを指し、レビュー文書自身のSHAとは区別する。

Codex／AI側から見た次の改善候補：今回はなし。
