> 画像サイズ検査の修正：共有4画像の共通／Androidインポート上限1024を明記。修正前Edit90件中89成功／1失敗、修正後は再試験待ち。Unityを閉じてFetch→Pullし、再起動後EditMode→Run All。詳細は[最新引き継ぎ](docs/development-handoff-2026-10-11.md)。

> 2026-10-11街の生活版：主人公選択／v3保存移行、3系統・6依頼、指での街移動・会話・表情を実装。[実装状況・未確認事項・Windows／Pixel手順](docs/development-handoff-2026-10-11.md)。クラウドはコア81件と実DLLコンパイル検証済み。修正後のWindows再試験／Pixelは未確認。初回用の別アプリは **Build Town QA APK (separate save)**（town-life-qa.apk）。既存QAの保存も維持します。

# もふまちメルジュ

Unity6000.6.4f1の縦画面Androidゲーム。正式6名の原画を維持し、TOP→初回確認→ホーム→依頼→Merge→納品／報酬→街成長と保存復帰を実装しています。

- [今回の変更・検証結果・未解決事項](docs/pixel3a-quality-status.md)
- [最新の12項目引き継ぎ報告・次の操作（主人公選択・街の生活）](docs/development-handoff-2026-10-11.md)
- [主人公選択・自由移動・会話・複数依頼の承認済み設計](docs/superpowers/specs/2026-10-11-town-life-design.md)
- [承認済み7工程・実装と残る実機検証](docs/superpowers/plans/2026-10-11-town-life.md)
- [採用承認された全身見本・制作記録](References/Preview/fullbody-review-2026-10-11/README.md)
- [街づくりとレベル別外観の提案・現在の実装範囲](docs/town-growth-direction.md)
- [POP UI素材・日本語フォントの出典](docs/ui-pop-assets.md)
- [Windowsビルド／USBでPixel 3aを確認する手順](docs/android-smoke-test.md)
- [Google Playの公開準備](docs/google-play-release-readiness.md)
- [音源・ライセンス](docs/audio-assets.md)

クラウドのUnity最終APK生成はユーザー指示で保留。新しいWindowsネイティブテストとPixel実機の確認が完了するまでは、公開品質の完成とは扱いません。
