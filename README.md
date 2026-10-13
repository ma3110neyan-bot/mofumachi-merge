> 2026-10-13・最新の統合本線指定を受領：[9月22日の基準イメージ](References/Preview/integration-baseline-2026-09-22/README.md)を最上位のビジュアル基準とし、正式6名・現在の街背景・ロゴを維持。[現状分析・優先順位・変更対象ファイル](docs/superpowers/specs/2026-10-13-integration-baseline-review.md)を最新指定へ更新。統合ページはこのチャットの引き継ぎ本文で共有されるため、URLや直接取得を前提にしない。今回の更新は資料のみで、方針確認後に街→Merge→文章UIを実装。2.5Dは設計提示まで。

> 2026-10-13・次期品質改善の第1段階（ゲーム本体 `8700c5a` 時点）：正式6名・ロゴ・街・5×6を保持。丸いMerge受け皿／接地影／合成候補の光／浮遊合成、主人公タップでの寄り道、メニューの絵本調書体を追加。Core94件・演出計算10件成功、実Unity DLLの全6アセンブリ警告／エラー0。テスト構成はEditMode105／PlayMode85で、**変更後のWindows全件・APK・Pixelは未実施**。[実装報告とWindows手順](docs/development-handoff-2026-10-13-next-quality.md)。音楽選曲・購入・広域カメラは設計段階。

> 画像サイズ検査の修正：共有4画像の共通／Androidインポート上限1024を明記。修正前Edit90件中89成功／1失敗、修正後は再試験待ち。Unityを閉じてFetch→Pullし、再起動後EditMode→Run All。詳細は[最新引き継ぎ](docs/development-handoff-2026-10-11.md)。

> 2026-10-11・3章版：絵本調書体、時間・道に沿う移動描画、3章6依頼の目的／まとめ、Lv.4〜6装飾を実装。正式6名・v3セーブを保持。[12項目の報告とWindows手順](docs/development-handoff-2026-10-11.md)、[第1章のPixel試遊チェック](docs/chapter-game-pixel3a-checklist.md)。Core87件／実DLLコンパイル等は確認済み、今回のUnityネイティブ・APK・Pixelは未確認。**Build Town QA APK (separate save)** と同じ街QA保存領域を継続。

# もふまちメルジュ

Unity6000.6.4f1の縦画面Androidゲーム。正式6名の原画を維持し、TOP→初回確認→ホーム→依頼→Merge→納品／報酬→街成長と保存復帰を実装しています。

- [今回の変更・検証結果・未解決事項](docs/pixel3a-quality-status.md)
- [最新の改善報告・次の操作（立体Merge・寄り道・書体）](docs/development-handoff-2026-10-13-next-quality.md)
- [主人公選択・自由移動・会話・複数依頼の承認済み設計](docs/superpowers/specs/2026-10-11-town-life-design.md)
- [承認済み7工程・実装と残る実機検証](docs/superpowers/plans/2026-10-11-town-life.md)
- [採用承認された全身見本・制作記録](References/Preview/fullbody-review-2026-10-11/README.md)
- [街づくりとレベル別外観の提案・現在の実装範囲](docs/town-growth-direction.md)
- [POP UI素材・日本語フォントの出典](docs/ui-pop-assets.md)
- [Windowsビルド／USBでPixel 3aを確認する手順](docs/android-smoke-test.md)
- [Google Playの公開準備](docs/google-play-release-readiness.md)
- [音源・ライセンス](docs/audio-assets.md)

クラウドのUnity最終APK生成はユーザー指示で保留。新しいWindowsネイティブテストとPixel実機の確認が完了するまでは、公開品質の完成とは扱いません。
