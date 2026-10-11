# Google Play公開準備 — 2026-10-11

2026-10-31は品質条件を満たした場合の目標。現在はソース実装とWindows／Pixel手順整備で、公開合格・審査提出はまだです。課金、広告、図鑑、イベント等を追加する段階には進めていません。

現ソースは正式6名の全身・選択・移動、3系統×3段階、6依頼まで接続済み。TOP・案内・選択のPixel表示を受領しましたが、全6依頼・保存・性能の実機合格は未確認です。[3章で最初の物語を遊び切る構成](superpowers/specs/2026-10-11-chapter-game-design.md)は確認待ちの設計案。最新の未完成範囲は[引き継ぎ](development-handoff-2026-10-11.md)を参照してください。

## 現行ID・バージョンと提出物

- 名称：もふまちメルジュ。通常ID `com.mofumachi.merge`、QA ID `com.mofumachi.merge.qa`、街QA ID `com.mofumachi.merge.qa.townlife`。
- 現行bundleVersion1.0、AndroidBundleVersionCode1。公開後のアップデートは同じID／署名を維持し、versionCodeを以前の提出より大きくします。QA IDの成果物を通常ストアへ提出しません。
- Pixel開発APKはARM64／IL2CPP、minimum26／target36／GLES3、Development Buildとdebug署名。**これをGoogle Playへ出すことはできません。** 公開前にDevelopment／debugging OFF、AAB、正式署名・Play App Signingを別途設定する必要があります。秘密鍵・パスワードはGitへ入れません。
- API36設定だけでPlay全要件の適合とは判断しません。Consoleの最新target API、16KBページサイズのネイティブライブラリ適合、64bit、AAB署名・配信チェックを実際のリリースAABで確認します。

## 素材・説明案

[街のアイコン／feature候補](../References/Release/README.md)はキャラなしの新規生成マスター。512×512／1024×500提出用書き出しはWindowsスクリプトで準備し、最終素材確認とadaptive icon設定を行います。正式6名は再生成していません。スクリーンショットは実装後のPixel実機画面を用意し、HTMLモックを実機画像として使いません。

短い説明案：

> かわいい仲間とお茶やパン、花をつくって届けよう。合成する楽しさが、もふまちの成長につながるゲームです。

本文案：

> もふもふの仲間たちが暮らす「もふまち」。6名からパートナーを選び、お茶やパン、花を合成して仲間に届けましょう。依頼を完了するとコインを受け取り、街が少しずつ成長します。

説明はリリース時に実際に遊べる内容と正式音源に合わせます。現Sliceは6依頼、反復依頼や購入・自由建築は未実装。章の締めや最初の物語完了は設計段階です。現在の遊びの長さで一般公開する体験が十分か、Pixelで遊び切ってから判断します。未実装の章・施設・正式音源を採用済みとして宣伝しません。

## 実データ監査とプライバシー

自作Core／Presentationはsave.json／健全バックアップを `Application.persistentDataPath` へ保存します。コイン・盤面・依頼進捗・音設定・初回確認・選択したパートナーを端末へ保存し、独自の送信先、ログイン、課金、広告、位置情報、カメラ／連絡先処理は実装していません。購入案内は将来の購入確認とは独立しています。

ただし「データ収集なし」は**まだ確定していません**。manifestにはUnity Purchasing5.4.4、GDK等の依存パッケージが残り、UnityConnectはm_Enabled0／Analytics0／Ads0／Purchasing0／CrashReporting0でも、Insightsのm_EngineDiagnosticsEnabled1、PlayerSettingsのsubmitAnalytics1が存在します。名前だけで通信有無を判断せず、リリースAndroidManifestのpermission、同梱SDK、自動初期化、Diagnostics設定、実際の起動・復帰・オフライン通信を確認してからPrivacy／Data safetyを記入します。Androidのバックアップ設定も確認します。

公開前に運営者／問い合わせ先、プライバシーポリシーの公開URL、保存データの扱い・削除方法、データ送信の有無と第三者SDKを実態に合わせて確定します。架空の運営者や「収集なし」の宣言を自動掲載しません。

## リリース前に残ること

1. 新Windows XML全件成功→新APK SHA確定→Pixelの一周／再起動／全画面・音合格。
2. 承認された正式6名の透過全身と3表情は収録済み。移動がぎこちないとのFBを修正し、新書体・章の一区切り・Lv.4〜6外観を実装／実機確認。脚の独立した歩行コマは未収録。
3. 録音BGMループ／6SEを実スピーカーで最終確認。[CC0/OFL出典](audio-assets.md)と正式キャラのストア利用権を確認。
4. 提出寸法素材、adaptive icon、実機スクリーンショット、ストア内容・対象年齢・コンテンツレーティングを確定。
5. Privacy／Data safety、SDK／permission／Diagnostics・バックアップ監査。
6. release AAB／正式署名／16KB適合とConsole検査。Consoleアカウントの該当する内部／クローズドテスト・製品版アクセス要件を確認。
7. ストア提出・公開は今回実行していません。審査日程は品質確認後に判断します。
