# 第8工程：Android Graphics API不要setter回避／限定検証合格

状態：**関連テスト合格（修正済み・実Unity AAB未確認）**。branch：`feat/daily-world-pass-20261016`。implementation commit：`5a9e6d54e9c4bedf60f738dfacab858875a827ee`。基準：`7c8271a7c4ed0bb49e7a5684bdc0030cc0df93a1`。この実装commitと証跡共有commitを区別する。既存AABの実ビルドsourceは`41529cfaea57ab564da46b3a45c41ede0aa5088b`のままで、新実装でのAABはまだ実行していない。

コード変更は**2ファイルだけ**：`Assets/Mofumachi/Editor/AndroidReleaseBuild.cs`と専用`scripts/tests/android-release-graphics.tests.ps1`。レビュー領域の証跡・集約は別途更新する。source guard、release PowerShell、ProjectSettings、UniversalRP、Packages、ゲーム、Visual Lock、既存QA、署名経路、既存42件テストは不変。

desired条件は**automatic=false AND API配列が非null・長さ1・唯一の要素がOpenGLES3**。private TemporaryGraphicsSettingsが元状態を取得し、desiredならApplyが直ちに戻る。**desired時setter count=0、restore setter count=0**。非desiredなら従来順のSetUseDefaultGraphicsAPIs(false)→SetGraphicsAPIs(OpenGLES3)を維持（計2回）。Changedがtrueのときだけ元API配列→元automaticの順に復元（計2回）。途中のsetter失敗にも備えChangedは最初のsetter直前に立てる。これはsetter開始の記録でありYAML実体変更の証明ではない。

**SaveAssetsへの影響：既存finallyの1回を維持**。署名・backend・architecture等の他の一時設定復元にも必要なため削除しない。graphics専用のSaveAssetsは追加しておらず、graphics helperはSaveAssetsを呼ばない。初期API列（enum名）、initial automatic、already_desired、changed、restoredの5公開フィールドをBuildInfoへ追加。desired時のchanged/restoredはfalse（復元未実施）。秘密値を追加記録しない。

**限定テスト：Total12 / Passed12 / Failed0、exit0。変更後suite実行1回。API-only compile：PASS、exit0、1回、C#エラー/CS0433なし。** 実際のproduction C#全体をmanaged API doublesでコンパイルし、productionのgraphics lifecycleを実行してsetter数・復元値を検査。build本体との接続は静的確認。別途、既存182参照のUnity6000.6.4f1 API-only compileで実型解決を確認した。これは実Unityの保存・serializationを実行した検証ではない。

指定6ケースに加えnull・空・重複API、setter途中例外、Apply前restore、BuildInfo/呼出し接続と既存SaveAssets保護を確認。修正前には新テストが最初の欠落条件で失敗することを1回確認して停止（expected RED：1失敗/11未実行）。その後実装を変更してGREENを1回実行した。**同じ失敗の自動再試行0**。既存42件source guardと第7工程55/444は再実行していない。原本はlimited-tests/redとgreenに分けて保持する。

source clean：**YES（実装commit直後）**。限定テスト/API compileの入力SHAは実装commitのC#と一致。現在の署名代入・他release設定代入は変更前と一致。公開証跡commit後のclean/remote一致はpush工程で確認する。比較補助の初回行単位checkはBuildInfo initializerの末尾変更を拾ったため、Android member参照順・署名/設定の各代入文を個別照合した。製品再修正・テスト再実行は不要だった。

**source guard変更NO／`0b000000i`許可追加NO／ProjectSettings直接編集NO／ゲーム変更NO／Visual Lock変更NO／signing経路変更NO**。iを正常値として追加せず、iを削除する文字列置換も追加していない。今回Unity/AAB/APK/Pixel/preflight/signing/keystore/guard42/Stage7の実行はすべて0、自動再試行0。AAB・APK・keystore・秘密値・コンパイルDLLはGitへ追加していない。

既存run `rc-20261110-132609-c6829dc7`の**E1、Unity内生成PASS／全体FAIL／source_unchanged=false／署名検証NotRun**は履歴として維持。書き込み元・suffixの正当性は未確定。この修正で実体mutationが止まると断定しない。旧native原本・graphics-api-audit・graphics-api-physical-auditは不変。前回集約はgraphics-noop-fix/prior-7c8271aにも原本保存する。

**次にAAB one-shotを1回実行してよいか：YES（別指示での検証に進む準備として）**。新run・cleanな修正済みcommit・既存署名条件・変更していないsource guardを維持し、最初の失敗で停止する。今回の実行許可ではなく、ここで停止する。**APKへ進む判定NO。第7工程complete=true、第8工程complete=false。** 次のAABは別指示待ち。
