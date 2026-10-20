> **現在の候補（2026-10-20・実画像レビュー後）**：[確認済みのこと・画像の問題・追加修正・次の一括検証](docs/base-product-quality-2026-10-20.md)。受領 `20261020-224724-e8a2fc33`／製品 `180369d` はEdit143／Play203全成功、実Unity102枚とAPK生成記録を照合しました。画像では街の舗装、図鑑初画面、今日の受取表示に品質不足が残るため、同じQuality Pass内で修正。**追加修正は実装済み・実機未確認、新しいUnity画像／APK／Pixel受入は未実施**です。受領したAPKに最新修正は含まれません。比較PDFは修正前の受領版と旧版の実画像で、完成後Afterの代用にはしません。

> **今後の開発方針**：[ユーザー指定の開発ルール](AGENTS.md) を優先してください。9月22日基準・5エリアの連続街歩き・高品質な平面Mergeを守り、承認済み方向の改善は自主的に判断します。確認段階と同条件Before／Afterを明示し、報告末尾に難易度／実施時期を添えた改善候補を最大3件載せます。以下は旧版の経緯です。

> **旧版の確認状態（Before）**：追加修正版の受領実行 `20261020-104223-10416661`（記録revision `578963c`、製品変更 `80e535d`）はEdit143／Play193全成功・skip0。90枚の実Unity画像を一覧確認し、代表画面を拡大比較。画像ハッシュ・XML・APKメタデータの対応を確認しました。芝生と舗装の塗り分け、Mergeの白い帯の除去と下部文字を実描画で確認。**Unity確認済み・APK生成済み、実装済み・実機未確認**です。この旧APKはBeforeとして保持し、今回の改修確認には使いません。街の密度・芝生の反復・街成長の差は改善余地があり、最終品質の合格判定は保留です。[実描画比較・同じAPKの上書き更新手順](docs/visual-review-2026-10-20-five-plazas.md)

> 2026-10-18：旧3背景の接続描画を単一の街マップへ置換し、北の依頼小屋・南のショップへの徒歩入場と、TOP／Merge各2曲の試聴候補を実装。**Unity実描画・新QA APK・Pixelは未確認、main未統合。** [実装とWindows一括検証](docs/development-handoff-2026-10-18-seamless-town.md) · [BGM候補4曲の8秒試聴](References/Preview/seamless-town-2026-10-18/README.md)

> 2026-10-17 大幅Polish Pass：広い歩行面・NPC回避・指追従・左右のぼかし削減・台形Merge・紙の文字UI・図鑑NEWを製品コードへまとめた実装候補。Core125／純粋表示81成功、6DLLコンパイル警告・エラー0。**本版のUnity実描画・QA APK・Pixelは未確認、main未統合。** [変更と同一候補の検証手順](docs/development-handoff-2026-10-17-major-polish.md)。

> 2026-10-17実画面再受入：`60f9c7f` のWindows **Edit141／Play161全成功・skip0**、QA APK、36PNGを確認。挨拶と硬い背景端は改善しましたが、草地の反復と石畳の縮尺が接続品質の条件に届かず、専用の接続面アトラス1枚とUVへ修正しました。Core125／純粋表示66成功、6DLLコンパイル警告・エラー0。**この素材修正版のWindows／実画像／APKは再取得待ち、Pixel未実施、main未統合です。** [画像の判定・修正・次の操作](docs/development-handoff-2026-10-17-daily-world-visual-acceptance.md) · [前回のテスト修正](docs/development-handoff-2026-10-17-daily-world-acceptance-fix.md)

> 2026-10-16: 北／中央／南の連続フィールド、斜めMerge、キャラクター図鑑、今日のお願いを候補 `feat/daily-world-pass-20261016` に実装。[実装内容と実機確認項目](docs/development-handoff-2026-10-16-daily-world.md)。最新の受入状況は上記10月17日の記録を参照してください。

> 2026-10-15 · 再試験の個別報告 `UnknownQuestAndFinaleNeverInventAnotherUnlock`（期待3／実際4）は、既存の画面遷移保存1回をテストが数えていないことを確認し、検査を修正しました。製品・保存処理は変更なし。構成は **Edit141／Play151**、最新の両XMLによる全件成功確認待ちです。[修正・再試験](docs/development-handoff-2026-10-15-release-experience-pass.md)。APK・main統合は保留。

> 2026-10-15 · 受領したExperience版PlayMode XMLは **151件：成功15／失敗136／skip0**。共通のCanvasRenderer不足2か所を修正しました。[原因・修正・再試験](docs/development-handoff-2026-10-15-release-experience-pass.md) · [集計](docs/verification/experience-renderer-fix-2026-10-15.json)。修正後の全件合否は新しいXMLを正とします。

> 2026-10-15 · 大規模仕上げ候補は `feat/release-experience-pass-20261015`。道・出口タップの街歩き、キャラ表現、磁器Merge盤面、3曲選曲、成長と次目標を実装。**Windows再試験・APK／Pixel受入待ち、main未統合。** [比較PNG](References/Preview/release-experience-2026-10-15/README.md) · [実装とWindows操作](docs/development-handoff-2026-10-15-release-experience-pass.md)

> 2026-10-15・総合Quality Pass候補：**`feat/release-quality-pass-20261015`**。キャラ起点の3地域歩行、1タップのショートカット、Merge受け皿と合体／着地、Klee One本文、3地域の成長比較、6依頼の会話を実装。Core125／Presentation17成功、実Unity DLLの6アセンブリ警告／エラー0。**今回版のWindows全件・APK・Pixelは未実施。main未統合。** 宣言構成はEditMode139／PlayMode135（ネイティブ実発見ではありません）。[変更・検証・Windows／Pixel手順](docs/development-handoff-2026-10-15-release-quality-pass.md)。

> 2026-10-14・B案3地域の実装候補：西／中央／東、入口へ歩く→短いフェード→到着側で歩く、NPC分散・訪問・交流を追加。正式素材・保存／納品／報酬・5×6を保持。Core125／演出10成功、実6 DLLコンパイル警告／エラー0。**候補のUnity全件・QA APK・Pixelは未実施。mainへ未統合。** 宣言構成はEditMode138／PlayMode124。[引き継ぎとWindows操作](docs/development-handoff-2026-10-14-town-regions-b.md)、[Pixelチェック](docs/town-regions-b-pixel3a-checklist.md)。

> 2026-10-14・再開：ユーザー回答では**EditMode121件と今回版のPixel一周は未実施**。確認と並行して、本文書体比較・Mergeの情報と立体感・街の左右各約1画面拡張・依頼の楽しさを設計資料にまとめた。製品へ未適用で、ゲーム本体・正式素材・保存／納品／報酬・テストは変更していない。[今回の報告](docs/development-handoff-2026-10-14-design-review.md)、[比較と配置図](References/Preview/quality-review-2026-10-14/README.md)、[設計・変更対象](docs/superpowers/specs/2026-10-14-storybook-and-town-expansion-review.md)。資料更新のためだけの再ビルドは不要。

> 2026-10-13・最新確認：ユーザー提供画像で**PlayMode99件成功・失敗0・skip0**を確認し、Town QA APK生成完了の報告を受領。7工程の再承認は既存実装の継続として記録し、現在は第7工程のEditMode結果・APK対応記録・Pixelの一周と比較を確認する段階。収益化・運営は将来設計のまま保持。今回の更新は資料のみで、同じソースのTown QAで試遊を継続できる。[現在の報告](docs/development-handoff-2026-10-13-integrated-quality.md)、[実機の確認項目](docs/integrated-quality-pixel3a-checklist.md)。

> 2026-10-13・PlayMode2件の追加修正 `3405c26`：章再閲覧では正常な保存時刻更新を区別し、寄り道の吹き出しは到着直後に検査する。ゲーム本体・保存／報酬・正式素材・件数を保持。[原因と変更内容](docs/development-handoff-2026-10-13-integrated-quality.md#追記ユーザー共有のplaymode2件への対応)。

> 2026-10-13・統合品質版（製品変更 `218ac51`）：9月22日の世界観と最新の立体表現を統合。同じ街背景内のカメラ／Lv.別訪問先／NPC交流、画像付き不足カード／単一生成行、文章の絵本調書体を実装。正式PNG29点と保存／納品／報酬ロジックを保持。構成は**EditMode121／PlayMode99**。本版のUnity全件成功・APK・Pixel・画像比較は未確認。[引き継ぎ報告](docs/development-handoff-2026-10-13-integrated-quality.md)、[Windows／Pixel確認手順](docs/integrated-quality-pixel3a-checklist.md)、[2.5D段階案](docs/2point5d-staged-prototype-2026-10-13.md)、[将来の収益化・運営設計](docs/monetization-and-operations-direction-2026-10-13.md)。

> 2026-10-13・次期品質改善の第1段階（ゲーム本体 `8700c5a` 時点）：正式6名・ロゴ・街・5×6を保持。丸いMerge受け皿／接地影／合成候補の光／浮遊合成、主人公タップでの寄り道、メニューの絵本調書体を追加。Core94件・演出計算10件成功、実Unity DLLの全6アセンブリ警告／エラー0。テスト構成はEditMode105／PlayMode85で、**変更後のWindows全件・APK・Pixelは未実施**。[実装報告とWindows手順](docs/development-handoff-2026-10-13-next-quality.md)。音楽選曲・購入・広域カメラは設計段階。

> 画像サイズ検査の修正：共有4画像の共通／Androidインポート上限1024を明記。修正前Edit90件中89成功／1失敗、修正後は再試験待ち。Unityを閉じてFetch→Pullし、再起動後EditMode→Run All。詳細は[最新引き継ぎ](docs/development-handoff-2026-10-11.md)。

> 2026-10-11・3章版：絵本調書体、時間・道に沿う移動描画、3章6依頼の目的／まとめ、Lv.4〜6装飾を実装。正式6名・v3セーブを保持。[12項目の報告とWindows手順](docs/development-handoff-2026-10-11.md)、[第1章のPixel試遊チェック](docs/chapter-game-pixel3a-checklist.md)。Core87件／実DLLコンパイル等は確認済み、今回のUnityネイティブ・APK・Pixelは未確認。**Build Town QA APK (separate save)** と同じ街QA保存領域を継続。

# もふまちメルジュ

Unity6000.6.4f1の縦画面Androidゲーム。正式6名の原画を維持し、TOP→初回確認→ホーム→依頼→Merge→納品／報酬→街成長と保存復帰を実装しています。

- [今回の変更・検証結果・未解決事項](docs/pixel3a-quality-status.md)
- [最新の統合改善報告・次の操作](docs/development-handoff-2026-10-13-integrated-quality.md)
- [前段階の品質改善](docs/development-handoff-2026-10-13-next-quality.md)
- [主人公選択・自由移動・会話・複数依頼の承認済み設計](docs/superpowers/specs/2026-10-11-town-life-design.md)
- [承認済み7工程・実装と残る実機検証](docs/superpowers/plans/2026-10-11-town-life.md)
- [採用承認された全身見本・制作記録](References/Preview/fullbody-review-2026-10-11/README.md)
- [街づくりとレベル別外観の提案・現在の実装範囲](docs/town-growth-direction.md)
- [POP UI素材・日本語フォントの出典](docs/ui-pop-assets.md)
- [Windowsビルド／USBでPixel 3aを確認する手順](docs/android-smoke-test.md)
- [Google Playの公開準備](docs/google-play-release-readiness.md)
- [音源・ライセンス](docs/audio-assets.md)

最後に受領したTown QA APKの記録は旧製品のWindows実行 `20261020-104223-10416661` です。今回の統合候補を含む新APKは未生成です。クラウド側は有効なUnityライセンス・Android Build Support・PixelのUSB接続が不足しています。新候補のWindows検証、同一APKのPixel確認が完了するまでは、公開品質の完成とは扱いません。
