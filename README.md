> 2026-10-13・PlayMode2件の追加修正：章再閲覧の比較では正常な保存時刻更新を区別し、寄り道の吹き出しは到着直後に検査する。ゲーム本体・保存／報酬・正式素材・件数を保持。Core110件／演出計算10件成功、6アセンブリ警告／エラー0、独立レビュー済み。**Windows再試験は未実施**。Fetch／Pull後、この2件→EditMode121／PlayMode99の全件検証→全件合格後にTown QA APK。[原因と変更内容](docs/development-handoff-2026-10-13-integrated-quality.md#追記ユーザー共有のplaymode2件への対応)。

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

クラウドのUnity最終APK生成はユーザー指示で保留。新しいWindowsネイティブテストとPixel実機の確認が完了するまでは、公開品質の完成とは扱いません。
