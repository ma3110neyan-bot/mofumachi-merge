# 街表示修正版：生成済みAPKのPixel3a受入

2026-10-25。run `20261025-073237-f8fd542b`、撮影／ビルド元 `5a49e3194260c05a8e96909b95bbd08bca1a6d76`。原XMLの143/143・222/222、failed0／skip0、124画像のSHA／寸法／デコード、capture/buildの版とfingerprint一致を確認済み。Unity画像はPixel確認へ進行可。正式採用は未了です。

原本と判定：`References/Review/town-boundary-2026-10-25-candidate/`。今回の受領・記録ではゲーム／原画／QAツールを変更していません。Pullや再ビルドをせず、既に生成されたAPKを使います。

## 更新インストール

PixelをUSB接続し、ロックを解除してUSBデバッグを許可します。既存の街QAは削除しません。

```powershell
$ErrorActionPreference = "Stop"
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
$MofuApk = ".\Builds\Android\20261025-073237-f8fd542b\town-life-qa.apk"
$MofuExpectedSha = "2b5193896965f805e086a51e5c19fbc1ae6a6ef6583aa8fd9755f0dfe8c6378a"
if ((Get-FileHash -LiteralPath $MofuApk -Algorithm SHA256).Hash -ne $MofuExpectedSha) { throw "APKのSHA256が一致しません。ここで停止してください。" }
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\install-pixel3a.ps1" -Apk $MofuApk -PackageName "com.mofumachi.merge.qa.townlife" -Serial "99RAY1BELH"
if ($LASTEXITCODE -ne 0) { throw "更新が停止しました。保存を消さず、結果画面をお送りください。" }
```

既存スクリプトがAPKの街QA ID・ARM64・端末を検査し、`adb install -r` で更新して起動します。更新方式は保存を残しますが、保存内容の維持は実際に確認するまで合格にしません。失敗時にアンインストール・データ消去・署名回避をしないでください。

APKは118,233,994bytes（112.76MiB）。上記SHAはconsoleと受領build-infoで一致しています。APK原本自体はクラウドへ未送付のため、このローカル照合が必要です。

## 実機での確認

- 中央→西→中央、中央→東→中央、中央→北→中央、中央→南→中央を徒歩で往復。カメラ・タップ・NPCの重なり／詰まりを確認します。
- 境界11地点に相当する上端／下端／左右／入口を実際に移動して確認。大きな緑の空白、背景の途切れ、石目、薄い補助文字、名前札・吹き出し・SafeArea・HUDを確認します。
- J1：西・東の下端の直線境界、北左接続の材質線が通常プレイで目立つか。NP1：外周花壇の反復が目立つか。Unity画像では軽微な持越しで、全ての継ぎ目が解消したとは判定していません。
- TOP→街→会話→依頼→Merge→納品→報酬→街成長→図鑑を、既存セーブで実行できる範囲で確認。既に物語／依頼が完了している場合は、確認できた部分と未確認を分けてください。セーブを初期化・補充・改変して一周を作らないでください。
- 更新前の盤面・Coins・街Lv・依頼・図鑑・BGM設定を維持できたか確認し、アプリ終了・再起動後も比較します。前版の保存復帰報告は今回の確認へ流用しません。
- 600秒以上、放置せず実際に遊び、発熱・重さ・フリーズ・クラッシュ・音切れ／音量・操作反応を確認。30秒計測やEditor描画は10分実試遊の代わりになりません。

結果は更新成功画面、実機の問題地点画面、徒歩往復／保存復帰／10分試遊の結果をお送りください。実際に見たLvだけを報告し、UnityのLv1/3/6と混同しないでください。

## 現在の判定

設計済み・実装済み・Unity実画面確認済み・同版QA APK生成済み。Pixelインストール／表示／操作／保存／10分は未確認。全条件と実機での明確な改善を確認してから正式ベース固定、続いてMerge Quality Passへ進みます。main統合は別承認です。
