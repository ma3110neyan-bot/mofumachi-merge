# Visual Lock と既存プレビュー

最上位仕様は [引き継ぎセット v1.0](../mofumachi_codex_handoff_v1.0.md)。ユーザーが指定した参照先を次のとおり用意した。GitHub に追加されたルートの原本からバイト列を変えずにコピーしており、原本も保持している。

| 用途 | 正式な参照先 | 追加された原本 |
| --- | --- | --- |
| 正式6名のキャラクター基準 | [mofumachi_6characters_master.png](Characters/mofumachi_6characters_master.png) | `a_colorful_promotional_collage_ui_concept_board.png` |
| 既存ブラウザ確認版 | [mofumachi_pixel3a_preview_v0.3_6characters.html](Preview/mofumachi_pixel3a_preview_v0.3_6characters.html) | 同名のルートHTML |

画像の顔・体型・主要配色・衣装・シルエットをVisual Lockとして扱う。画像内の仮名や主人公選択の文言を新しい仕様として採用しない。キャラ名は後工程とし、中心キャラを固定してお気に入りのホーム表示を選ぶ引き継ぎの基本案を優先する。

SHA-256:

```text
Characters/mofumachi_6characters_master.png
cd423f6599cf41346064405240c72ac071aaa8d3fbd326d2b82661b13b0892c8
Preview/mofumachi_pixel3a_preview_v0.3_6characters.html
1afe2014e72bb67515d4a74255debd46fc4effc5e2acd2f131ed53c0f6578b54
```

既存v0.3は5画面のナビゲーションとWeb Audioによる仮BGMを持つ。実際のMerge・納品・セーブ処理や正式BGM/SE素材の代わりにはならない。原本の音源生成コードは参考資料として保持する。

正式BGM/SEファイルは未収録。音の方向性は引き継ぎとユーザーの補足に従う。

- BGM: 明るいPOP × 上品 × ファンタジー。ピアノ、マリンバ／ベル、柔らかいストリングス、アコースティック系リズムを中心にする。古いゲーム機風の電子音を避ける。
- 決定: 柔らかいポン。
- Merge: キラッ＋ふわっ。
- 報酬: 明るいチャイム。
- キャラタップ: 小さな反応音。

音の試作・確認と正式素材への差し替えはPresentation実装で行う。正式音源の未収録を、セーブ・Merge・納品コアの実装を止める条件にしない。
