# 第8工程：Graphics API no-op最終指示への限定確認

状態：**関連テスト合格（実Unity未確認）**。branch：`feat/daily-world-pass-20261016`。implementation commit：`5a9e6d54e9c4bedf60f738dfacab858875a827ee`。今回の作業開始HEAD：`f2bf6e5cf89bd9de322ab583ba4ba9149a163897`。ユーザー指示の基準`7c8271a7c4ed0bb49e7a5684bdc0030cc0df93a1`からのbuilder修正は既にimplementation commitへ含まれるため、そのC#を維持して最終条件を確認した。共有commit自身のSHAは最終Git報告で示す。

今回のコード変更は専用`scripts/tests/android-release-graphics.tests.ps1`だけ（Case2/3/4に計15行追加）。automatic=trueかつGLESのみ、automatic=falseかつ他APIのみ、automatic=falseかつ複数APIの各ケースで、desiredへ変更した後のrestore setter数・元automatic・元API列/順序を明示検査する。builder本体を変更していない。その他は今回のReview evidenceだけ。

desired条件：**automatic=false AND 非nullのAPI配列が1要素 AND OpenGLES3**。desired時のGraphics setter **0回**、restore setter **0回**。non-desired時は従来の2 setterを維持し、変更処理を開始した場合だけ2 setterで元状態へ復元。途中例外時にも復元を試みるためChangedを最初のsetter直前に設定する（実YAML byte mutationの証明を意味しない）。SaveAssetsは他のrelease/signing設定の復元に使う既存finallyの**1回を維持**。graphics helperからのSaveAssets追加は0。

**限定テスト1回：Total12 / Passed12 / Failed0 / NotRun0、exit0。自動再試行0。** 指定最低6ケースと既存の空/null/重複配列・途中例外・未Apply時restore・BuildInfo/呼出し接続を確認。実production C#全体をmanaged doublesでコンパイルしてgraphics helperを実行し、呼出し接続は静的確認する。Unity engineやnative serializationを実行した結果ではない。

C# SHA256は`4e2901614a787bbb7755a99da94d63449dbe873d6d233d2050700e2392c62903`で、既存API-only compile PASSの入力と一致。C#不変なのでAPI-only compileを再実行していない。現在テストSHA256は`1e9d46ede1005abe0a50a8d7cba1da8c3aa4ec243e8d7af69f7a85686433b769`。旧12ケース結果を今回の補強後結果として流用せず、今回のresult/logを新規保存した。既存42件source guardと第7工程55/444は再実行0。

source guard変更 **NO**、`0b000000i`許可追加 **NO**、ProjectSettings変更 **NO**、ゲーム変更 **NO**、Visual Lock変更 **NO**、signing経路変更 **NO**。UniversalRP、Packages、QA、version/versionCode、release PowerShellも不変。i削除・YAML手編集・regex修復・checkout/restore・allowlist追加は0。保護ファイルのSHAはverification.jsonへ保存した。

今回Unity起動／AAB／APK／Pixel／preflight／signing／keystoreは**すべて0**。秘密値・keystore・AAB/APK・コンパイルDLLはGitに追加しない。既存run `rc-20261110-132609-c6829dc7` のE1・source_unchanged=false・全体FAIL・署名NotRunと、書き込み元/Unity正規性未確定の判定は維持し、旧原本を変更しない。

**次のAAB one-shotへ進む準備：YES（別指示で新runを1回検証するための準備のみ）**。今回の実行許可ではない。source clean・local/remote HEAD一致はcommit/push後に最終確認する。通常commit1回・通常push1回後に停止。権限確認ダイアログが表示された場合はその文面を提示して停止し、回避しない。**APKへ進む判定NO、第8工程complete=false**。次工程は別指示待ち。
