# もふまちメルジュ｜Codex引き継ぎセット v1.0
更新日: 2026-10-07

## 1. プロジェクト概要
- タイトル: もふまちメルジュ
- ジャンル: かわいいオリジナルキャラ × Merge-2 × 街・部屋づくり × コレクション × ストーリー
- 初期プラットフォーム: Android / Google Play
- 技術方針: Unity 2D / 縦画面 / ローカルセーブ
- 目標: Google Play公開。まずは1周遊べるVertical Sliceを完成させ、その後に課金・広告・イベントへ拡張する。
- 最優先: 設計だけで進んだ扱いにせず、実際に触れる成果物を継続的に作る。

## 2. 固定済みの世界観・キャラクター
### タイトル
- 『もふまちメルジュ』で固定。
- 「メルジュ」は主人公名ではなく、Merge由来のタイトル表現。

### キャラクター
- 2026-10-06採用の6名キャラクターデザインを正式基準とする。
- 旧3名キャラクター版は今後の制作基準に使用しない。
- キャラ名は未確定。命名は後工程。
- 顔・体型・主要配色・衣装・シルエットを試作ごとに変更しない。

### Visual Lock
変更禁止:
1. 6名キャラクターの基本デザイン
2. タイトル『もふまちメルジュ』
3. パステルで上品・かわいい世界観
4. 縦画面前提
5. キャラIPとして育てる前提

背景やUIの微調整は可。ただしキャラの視認性・品質を落とさない。

## 3. キャラクター役割の基本案
- 1名: ブランドの中心キャラ。最初から登場。
- 2名: 序盤で仲間になる案内・応援役。
- 2名: 依頼・街イベントで個性を出す役。
- 1名: 中盤解放で特別感を出す役。
- 主人公を6名から完全選択させる方式より、中心キャラを固定し、ホーム表示の「お気に入りキャラ」を選択できる方式を基本案とする。

## 4. コアループ
TOP → もふまちホーム/街 → キャラ依頼 → Merge盤面 → 納品判定 → 納品 → 報酬 → 街成長 → 街へ戻る → セーブ

初回5分は強い課金導線を出さない。

## 5. 画面構成
### TitleScene
- 起動
- TOP
- はじめる
- BGM/SE設定
- セーブ有無判定

### GameScene
Panel切替で管理:
- もふまちホーム/街
- キャラ依頼一覧
- キャラ依頼詳細
- Merge
- 図鑑
- ショップ
- イベント
- デコレーション
- 設定

### Result
- 納品完了
- 報酬獲得
- 街成長演出

## 6. MergeBoard Core Rules v0.1
- 固定グリッド
- 1マス=1アイテム
- 同じ itemId ＋ 同じ level の2個だけ合成可能
- 合成時、2個消費 → level+1 を1個生成
- 最大level同士は合成不可
- 異なるアイテム位置への誤移動は元位置へ戻す
- 合成中は対象アイテムをロックして二重操作防止

## 7. Quest / 納品仕様
Quest状態:
1. 依頼未受注
2. 受注済
3. Merge制作中
4. 納品可能
5. 報酬受取済
6. 街成長済

納品条件: itemId / level / count

処理順:
ValidateQuest() → ValidateItems() → ConsumeItems() → CompleteQuest() → GrantReward() → EvaluateTownGrowth() → Save()

重要:
- 1個でも不足していればアイテム消費禁止
- 完了済みQuest IDは再納品不可
- 二重タップ・二重報酬禁止
- 報酬付与後は即保存対象

## 8. SaveData v0.1
最低保存項目:
- saveVersion
- coins
- gems
- stamina
- playerLevel
- townGrowthLevel
- activeQuestId
- questProgress / questState
- inventory (itemId + level + count)
- mergeBoard state
- completedQuestIds
- claimedRewardIds
- BGM setting
- SE setting
- lastSaveTime

保存タイミング:
- Merge成功
- Quest受注
- 納品
- 報酬受領
- 街成長
- アプリ一時停止
- アプリ終了

Load失敗時:
- 破損データをそのまま使用しない
- 初期状態または安全なバックアップへフォールバック

## 9. 想定クラス
- GameStateManager
- SaveManager / SaveService
- QuestManager
- MergeBoard
- RewardService
- TownGrowthService
- AudioManager
- UIFlowController

主要メソッド候補:
AcceptQuest(), AddItem(), TryMerge(), CheckDelivery(), TryDeliver(), CompleteQuest(), GrantReward(), EvaluateTownGrowth(), Save(), Load(), ResetSave()

## 10. UI/演出方針
- 縦9:16
- パステル、上品、かわいい、少しツヤ感
- キャラクターがホーム/公園内を軽く歩く・うろうろする
- ボタン押下は軽い沈み込み、光、粒子など触感を返す
- キャラ固有の待機モーションを持たせる
- 過剰に派手にせず、長時間見ても疲れない

## 11. BGM/SE方針
### BGM
現在の簡易Web Audio音は仮。
本採用方向:
- 明るいPOP
- 上品
- ファンタジー
- ゴージャスなビジュアルに負けない音質
- ピアノ
- マリンバ/ベル
- 柔らかいストリングス
- アコースティック系リズム
- 電子音は補助程度
- 昭和ゲーム音/安いピコピコ感は避ける

### SE
- 決定: 柔らかい「ポン」
- Merge: キラッ＋ふわっ
- 報酬: 明るいチャイム
- キャラタップ: 小さな反応音
- UI全体で音量・質感を統一

## 12. Android実機テスト
端末:
- Google Pixel 3a
- SIMなし
- Wi-Fi接続
- Playストア利用確認済み
- ChatGPTアプリログイン済み

初回Smoke Test:
1. 起動
2. 画面比率
3. タップ/ドラッグ
4. 同一アイテム合成
5. 誤合成拒否
6. 不足状態で納品しても消費されない
7. 正常納品
8. 二重納品防止
9. セーブ→アプリ再起動→復帰

本番ではブラウザ枠なしの全画面表示を前提。

## 13. テスト運用ルール
- 「テストできます」と報告するのは、実際に開けるURL・ビルド・APKが存在する場合だけ。
- 仕様だけ完成した状態を「実働」と呼ばない。
- 実装前にFB可能なプレビューを提示する。
- 画面・操作・BGM/SE確認 → FB → 修正 → 本実装 の順で進める。

## 14. 課金・広告
- 優しい課金
- 低価格・高頻度の小額商品
- コスメ/家具
- 任意のリワード広告
- 初回5分は強い課金導線なし
- コアループ完成前に価格詳細を固定しない

## 15. Google Play公開までの優先順位
1. Unityプロジェクト作成・基本Scene構成
2. GameState / SaveData
3. TOP → ホーム → Quest → Merge → 納品 → 報酬 → 街成長
4. AudioManager / BGM / SE
5. Pixel 3a実機確認
6. 不具合修正
7. ショップ/課金/広告
8. 図鑑/デコレーション/イベント最低限
9. Google Play提出素材
10. AABビルド・内部テスト
11. Google Play提出

## 16. Codexに最初に依頼する実装タスク
### Sprint 1: Vertical Slice
目的: Androidで「1周遊べる」状態を最速で作る。

実装対象:
- Unity 2D縦画面
- TitleScene
- GameScene
- Result表示
- GameStateManager
- SaveManager
- QuestManager
- MergeBoard
- RewardService
- TownGrowthService
- AudioManager
- UIFlowController

必須フロー:
TOP → ホーム → 依頼受注 → Merge → 納品 → 報酬 → 街成長 → セーブ → 再起動復帰

完了条件:
- Androidビルド可能
- Pixel 3aで起動
- 上記フローを1周できる
- 二重納品/二重報酬なし
- セーブ/復帰が機能
- 6名正式キャラ基準を維持

## 17. Codexへの最初の指示文
このリポジトリでUnity 2DのAndroid向け縦画面ゲーム『もふまちメルジュ』のVertical Sliceを実装してください。
このREADMEのVisual Lock、Core Loop、MergeBoard、Quest、SaveData、Audio、Android Test条件を絶対条件として扱ってください。
最初に既存プロジェクト構成を確認し、不足している場合は最小構成を提案してください。
キャラクターデザインの変更は禁止です。
まずTOP→ホーム→依頼→Merge→納品→報酬→街成長→セーブ/復帰までを1周できる状態を最優先にしてください。
各変更は小さく分け、実装後にビルド/テスト結果と未解決事項を報告してください。
仕様が曖昧な場合は勝手に大きく変更せず、既存仕様に沿った最小判断をしてください。
