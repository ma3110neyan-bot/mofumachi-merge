# 明るいPOP UI素材（2026-10-10）

## 最新：枠内の色・リボン・水流（2026-10-11）

既存素材と丸い本文フォントを維持し、リボンを角丸・金縁・光沢・影と切れ込みのある端へ変更。依頼／Merge／設定に桃色・水色・ラベンダーを使い、枠内を淡く色付けしました。見出しに薄い金色の影、納品結果に依頼者ルルの肖像とお礼を追加。

噴水2段と奥の滝の流れる水筋・しずく、4つの波紋を既存のGraphicで描き、30Hz上限と入力非遮断・中断停止を維持。実機での見え方と負荷は未測定。新しい製品画像・フォント・パッケージの追加はありません。

[全身化の確認用見本](../References/Preview/fullbody-review-2026-10-11/README.md)はAssets外に置いた未採用案です。正式素材への採用と歩行実装はまだ行っていません。最新のWindows確認件数はEditMode67／PlayMode31。以下の28件は最初のPOP改修時の記録です。

## 追加：華やかなロゴと金枠（2026-10-11）

「さらに華やかに」という回答と正式参照のTOPに沿い、キャラを含めない透過素材を画像生成ツールで作成しました。原本を編集せず同梱し、元のキャラ・街背景・フォントは維持しています。

| 素材 | サイズ／容量 | SHA256 |
| --- | --- | --- |
| [TitleLogo.png](../Assets/Mofumachi/Resources/Mofumachi/TitleLogo.png) | 1659×948 RGBA／2,168,919 bytes | `5cbc8451a620d20887f3c74863509e080eddaa235649209c9c8141e053dc3190` |
| [UIPanelFrame.png](../Assets/Mofumachi/Resources/Mofumachi/UIPanelFrame.png) | 1254×1254 RGBA／900,118 bytes | `4d8c92397af9c9243ca99aafad2c26fdc1fe19898998217ea091b8e95d9e31e7` |

ロゴは固定のゲーム名だけを画像として表示します。本文、名前、必要数、報酬などのゲーム情報は文字で表示。最大インポートサイズ1024、NPOT変更なし、ミップマップなし、透明度と双線形フィルターを使用します。ロゴのアスペクト比は元画像に合わせ、専用領域の中でFitInParentします。

DecorativeFrameGraphicは共有する金枠画像を8つの周縁区画で描き、中央を描きません。短いヘッダー・盤面の外縁は花付きの角を描かず、セルの操作面と情報を守ります。UVは0／0.30／0.70／1で、Androidの縮小後も同じ場所を使います。光沢リボンは小さいUGUI頂点メッシュ、区切り線は既存の花アイコンです。いずれもraycastTarget=false、常時アニメーションなし。

今回のコア62件とUnity 6000.6.4f1実DLLによる全ソースコンパイルは成功。既存フォントは新しい表示文の非空白文字を網羅し、GUID重複なし、原画・街背景のSHA一致を確認。実Unityの描画、インポート、入力、APK、Pixel負荷は未確認です。変更範囲と残りは[12項目引き継ぎ](development-handoff-2026-10-11.md)を参照してください。

ユーザーが承認した[見た目案](../References/Preview/ui-polish-2026-10-10/ui-style-pop-proposal.png)を基準に、既存7画面のUIだけを改修しました。案の3×3図は質感の見本です。実装は5×6、数値はゲーム状態とQuestDefinitionから表示します。

## フォント

本文は **Zen Maru Gothic Medium**、見出し・ボタンは **Bold**。Google Fontsが公開する日本語フォントで、SIL Open Font License 1.1です。[同梱ライセンス](../Assets/Mofumachi/Resources/Mofumachi/UIFont-LICENSE.txt)。

取得元：`https://raw.githubusercontent.com/google/fonts/main/ofl/zenmarugothic/ZenMaruGothic-{Medium,Bold}.ttf`。取得時の原本SHA256：

| 原本 | SHA256 |
| --- | --- |
| Medium | `3cfdb98a13571ede17fcc769f5093a97c38b80a7b9b2ab754a26b4d822092b3b` |
| Bold | `fe24426b9c8b5523a0146a8235c8674eccf0493af354a53ec895c3596d9eb745` |

fontToolsで、ASCII・ひらがな・カタカナ・現在の日本語文字と記号をサブセット化し、派生ファミリー名をMofumachi Pop UI／Mofumachi Pop UI Boldへ変更。各389文字のUnicodeマッピングを含み、合計約202KBです。確認時のソース内非ASCII190文字を両方で網羅しています。U+FFFF、U+0378、U+0416は含まず、既存の欠字拒否試験を維持します。新しい表示文を加える時はサブセットとUIStrings／ビルドのFontEngine検査を更新してください。

`UIFont.otf`を`UIFont.ttf`へ置換し、GUIDとResourcesキー`Mofumachi/UIFont`を維持。Boldは`Mofumachi/UIFontBold`です。フォント名を変更したため、metaのImporterフォント名も更新しました。WindowsのUnityによる実インポートと描画確認は必要です。

## アイコンとパネル

[UIIconAtlas.png](../Assets/Mofumachi/Resources/Mofumachi/UIIconAtlas.png)は承認案に合わせて生成した専用UI素材です。1254×1254 RGBA、約1.4MB。家、封筒、ティーカップ、ティーポット、歯車、コイン、星、チェック、花の9点。正式キャラは生成に含めていません。

原本は画像生成ツールの出力をそのまま同梱しています。UIIconGraphicが個別のUV領域を読み、アスペクト比を保って描画します。Texture2Dは共有し、各アイコンの画像コピーや専用シェーダーは作りません。Lv.1はカップ、Lv.2以上はポットで区別し、正確なLvは文字で表示します。Lv.3以降専用の絵は未制作です。

パネルとボタンはUGUIの標準シェーダーと小さな頂点メッシュで、丸み・薄い明るい縁・上下の光沢差・小さい影を表現します。文字や動的数値を生成画像へ焼き付けていません。

## キャラと環境演出

正式6名の原画と街背景はバイト単位で維持しています。最新のユーザー指定に従い、原画の並びでモモ／ルル／ポム／フィオ／ミエル／ノアを表示します。原画画像内の過去の名前表記より、この指定を優先します。

152×150の原画領域を拡大しすぎないよう、丸いフレームへ収めます。タップで色の選択表示と短い拡縮・傾きを付けます。街画面内の選択表示は1名だけです。これは原画カードの反応で、全身歩行、表情差分、手振り、お気に入りの永続保存は未実装です。

噴水には水滴・波紋、街には小さな光を重ねます。背景のUVクロップを参照して位置を追従し、最大30Hzで1枚の軽いGraphicだけを更新。新しい画像レイヤーや重いパーティクルシステムは使いません。画面離脱で破棄、アプリpauseで停止します。実際のフレームレート、自然な水の位置、発熱はPixel 3aで未測定です。

## 検証の範囲

クラウドで全ソースのUnity 6000.6.4f1 DLLコンパイル（0警告／0エラー）、既存コア62件、PowerShellスクリプト試験を実行。フォントのcmap、透明度、原画・背景のSHA256を検査しました。360幅・Safe Area高さ592／640／740／808の計算上、盤面は正方形48／56／62／62です。コイン最大値の幅もフォント原本の字幅で検査しました。

Unity Editorの描画、Mask／Canvasの実レイアウト、実入力、FontEngineのインポート検査、追加PlayMode試験はクラウドで未実行です。合格したWindowsの旧結果（EditMode67、PlayMode25、旧QA APK）は今回のUI改修の合格証拠には使いません。最初のPOP改修時はEditMode67／PlayMode28（追加3件）、現在の再試験はEditMode67／PlayMode31です。
