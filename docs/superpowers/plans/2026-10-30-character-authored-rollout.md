# 承認ノア方式の5名横展開 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement sequentially. Never dispatch parallel character implementers.

**Goal:** ノアを固定し、残り5名を同方式・同品質で一名ずつ実装・検査する。

**Architecture:** NoahAuthoredMotion／Graphic／Buildとノア素材を不変にし、追加キャラ用Graphic／Editor準備／対象キャラのactor分岐を別に置く。方式・部位の剛体変換・既存世界／Sortingは流用し、登録データで原画の違いを表す。

**Tech Stack:** Unity6000.6.4f1、C#、既存uGUI、PNG、PowerShell5.1互換、Python素材検査。

**Spec:** docs/superpowers/specs/2026-10-30-character-authored-rollout-design.md

## Global Constraints

- 作業順momo→lulu→pom→fio→miel。次キャラの前に現在キャラの実Unity関連検査を必要とする。
- ノアの承認SHA・正式atlas・街・Core等は維持。別方式・5名同時変更・全件検査の前倒し禁止。
- 顔・目・衣装・配色・体型・シルエットは変更しない。新方向素材は原画と個別照合し、別キャラ化を許容しない。
- ネイティブ／APK／Pixel未実行を明記。保存reset・mainなし。

## Review Focus

- front／斜め／後ろ切替で高さ・足元・顔の印象が飛ばない。
- 無いしっぽ／小物をノアから流用して生やさない。
- 閉じ目切替で目や顔を変形しない。
- 新Graphicがactor入力・親mask・足元Sortingを外れない。
- 他キャラ／ノアの表示と保存、会話の発生ルールを変えない。

### Task 1: 承認記録とモモ単体

**Files:** 新規References/VisualLock/noah-authored-approved-2026-10-30.json、References/Characters/momo-authored-parts-2026-10-30/、Presentation/CharacterAuthoredGraphic.cs、Editor/CharacterAuthoredBuild.cs、Tests/PlayMode/ExperienceCaptureTests.CharacterAuthored.cs、Tests/PlayMode/CharacterAuthoredCaptureRoute.cs、scripts/capture-character-authored.ps1。変更Presentation/TownActor.csの追加分岐のみ。

**Interfaces:** 既存NoahAuthoredMotion.Sample／DirectionとNoahAuthoredPose.Partを消費。SetCharacterAuthoredEnabled(bool)は対象キャラ単体の追加Graphicを切替え、falseで元表示へ戻す。Resources/Mofumachi/CharacterAuthored/<id>をEditorで準備する。

- [x] 正式モモとノア方式を参照し方向別独立素材を作る。原本と指示を保存、左右・後ろの顔／服／耳／尾を視認。
- [x] 登録データの欠落・誤キャラ・破損素材／原画変更を拒否する関連テストを先に失敗させる。
- [x] ノア方式を直接再利用する追加GraphicとMomo専用actor分岐、Editor準備と単体routeを実装する。
- [x] ブラウザー素材確認・関連APIコンパイル・保護SHA検査を実行。Unity成功とは呼ばない。
- [ ] Windowsで対象1名の表示／歩行／停止／方向／会話／挨拶／Sorting／入力／エラー検査を行い、画像原本を確認する。
- [ ] モモ合格ならコミットしTask2へ。未確認ならTask1を進行中に保つ。

### Task 2–5: ルル→ポム→フィオ→ミエル

各タスクは対応するCharacter/idの素材と登録、同じモモ単体テストだけを実行。前のキャラとノアを変更しない。各ネイティブゲート後にのみ次タスクへ進む。

### Task 6: 全員完了後の統合

全件Edit／Play、6名同時表示、QA APK、Pixel3a。各方向・動作・会話・挨拶の証拠、FPS／メモリ／発熱／入力／クラッシュの実測とユーザー受入を提出。未測定をゼロにしない。最後にfresh-contextの一回のコードレビューを行い、問題を該当キャラだけで修正する。
