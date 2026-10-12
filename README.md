> 2026-10-12・Merge品質改善：背景・正式6名・ロゴ・5×6を保持し、陰影／Lv.バッジ、選択時の拡大、短い合成／光を追加。Core87件＋演出計算8件成功、Unity実DLLコンパイル成功。**Windowsネイティブ・新APK・Pixelは確認待ち**。PlayModeはソース上60件。[最新12項目の報告とWindows手順](docs/development-handoff-2026-10-12.md)、[Pixelチェックリスト](docs/merge-polish-pixel3a-checklist.md)。

> 画像サイズ検査の修正：共有4画像の共通／Androidインポート上限1024を明記。修正前Edit90件中89成功／1失敗、修正後は再試験待ち。Unityを閉じてFetch→Pullし、再起動後EditMode→Run All。詳細は[最新引き継ぎ](docs/development-handoff-2026-10-11.md)。

> 2026-10-11・3章版：絵本調書体、時間・道に沿う移動描画、3章6依頼の目的／まとめ、Lv.4〜6装飾を実装。正式6名・v3セーブを保持。[12項目の報告とWindows手順](docs/development-handoff-2026-10-11.md)、[第1章のPixel試遊チェック](docs/chapter-game-pixel3a-checklist.md)。Core87件／実DLLコンパイル等は確認済み、今回のUnityネイティブ・APK・Pixelは未確認。**Build Town QA APK (separate save)** と同じ街QA保存領域を継続。

# もふまちメルジュ

Unity6000.6.4f1の縦画面Androidゲーム。正式6名の原画を維持し、TOP→初回確認→ホーム→依頼→Merge→納品／報酬→街成長と保存復帰を実装しています。

- [今回の変更・検証結果・未解決事項](docs/pixel3a-quality-status.md)
- [最新の12項目引き継ぎ報告・次の操作（Merge品質改善）](docs/development-handoff-2026-10-12.md)
- [主人公選択・自由移動・会話・複数依頼の承認済み設計](docs/superpowers/specs/2026-10-11-town-life-design.md)
- [承認済み7工程・実装と残る実機検証](docs/superpowers/plans/2026-10-11-town-life.md)
- [採用承認された全身見本・制作記録](References/Preview/fullbody-review-2026-10-11/README.md)
- [街づくりとレベル別外観の提案・現在の実装範囲](docs/town-growth-direction.md)
- [POP UI素材・日本語フォントの出典](docs/ui-pop-assets.md)
- [Windowsビルド／USBでPixel 3aを確認する手順](docs/android-smoke-test.md)
- [Google Playの公開準備](docs/google-play-release-readiness.md)
- [音源・ライセンス](docs/audio-assets.md)

クラウドのUnity最終APK生成はユーザー指示で保留。新しいWindowsネイティブテストとPixel実機の確認が完了するまでは、公開品質の完成とは扱いません。
