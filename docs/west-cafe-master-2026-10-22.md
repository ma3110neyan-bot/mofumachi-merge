# 西カフェ実装候補 — 2026-10-22

**実装済み・実機未確認。新しい中央／西のUnity画像は未取得、QA APK未生成、Pixel 3a未確認。**

[了承いただいた西カフェ配置案](../References/Preview/west-cafe-2026-10-22/README.md)を、既存の5地区・正式原画・連続移動へ落とし込んだ。入口・両側の小道・中央への経路を保ち、地表・庭席・ベンチ・街灯・低い花壇をまとめて組み込んだ。配置イメージは実画面のAfterへ読み替えていない。

## 変更と検査

- 西専用の非反復地表を固定ワールド座標で描く。中央と同じ蜂蜜色の石畳と低い花・草。建物・キャラ・家具は地表へ焼き込まず、既存の正式素材を奥行き順に表示する。
- 初期から庭席を置き、既存Lv2の庭席が出たら初期分を隠して置換する。庭席を隠す旧汎用花壇は非表示にし、手前の高い花壇・木立を低い植栽へ置き換える。元画像は変更していない。
- 家具の基礎を歩行判定にも共有。初回の全回帰で通路に触れる基礎を検出し、通路の幅を削らず家具を移した。広い床・左右の小道の幅・既存の入口・中央接続の検査を維持した。
- 中央の噴水・装飾・原画の配置は不変。ただし共有描画／ナビ／撮影コードと入力digestが変わったので、c11e197のUnity合格は履歴として保ち、新候補の中央段階は再確認待ちにした。

純C#の表示／経路258件成功。新規の配置4件はRED→GREEN、旧植栽による庭席の隠れもRED→GREEN。初回全件で検出した既存の広い床／小道2件を、期待値を緩めずGREENへ戻した。Unityの実DLL参照6asmdefコンパイルは警告／エラー0。これはEditor実行ではない。

PowerShell6スイートが成功。西の画像不足・改変・source不一致のRED→GREENを確認し、Before不足の拒否と実際のフォルダ移動・JSON・ZIP生成も検査した。後者のJPEG描画だけはLinuxで代替関数を使用しており、Windows System.Drawingの実出力は未確認。Androidテストのインストール表示はmock出力で、Pixelに接続していない。独立ソースレビューに重大／重要／軽微の指摘なし。静止画の重なり・端末操作は実画面で別に判定する。

## 次の確認手順

GitHub Desktopで既存のfeatureブランチをPullし、Unity Editorを閉じてPowerShellで実行する。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -WestReview
```

中央2検査＋西2検査＋撮影1検査の5件を実行する。中央Lv1／3／6の3PNG、西Lv1／3／6＋接続＋徒歩＋入口の6PNG、計9枚を同じsourceから生成する。中央は正式Lock・固定Beforeと並べる既存JPEG、西は固定Beforeと候補のJPEGおよび別添の了承済み配置案を出力する。動いている3枚に同条件のBeforeはないので、静止6組の比較へ混ぜない。

出力先は `Builds/WestReview/<実行名>/`。成功するとWindowsでフォルダを開く。次の2つをアップロードする。

- `central-master-review.zip`：中央3枚と比較、同一実行のXML／ログ。
- `west-cafe-review.zip`：西6枚、静止Before3組、比較JPEG、配置案、同一source情報。

**APKは作らない。** 西と新しい中央の実画像判定前に東以降へ進めず、ベース版の完成ともしない。通常QA APKの後工程ゲートは閉じたまま。失敗した場合は、この実行フォルダの `central-playmode.xml` と `central-playmode.log` で原因を確認する。

## 後工程として残すFB

ユーザーの最新指示により、Lv1とLv6の変化の分かりづらさ、短い成長間隔で装飾が尽きる懸念、Lvアップで変わった内容を別ページで見られる工夫は後工程へ記録した。今回は街を実装して確認する段階。新ページ・成長間隔・SaveData・納品／報酬を変更していない。

## 状態と未解決事項

設計の方向了承済み → **実装済み** → Unity確認待ち → APK未生成 → Pixel 3a未確認。

今回のクラウドEditor起動は終了198、`No valid Unity Editor license found`。AndroidモジュールとPixel USB経路もない。Windows実画像で、原画の重なり・西の名札・Lv6装飾の見切れ・材質の接続・カフェ入口の到着表示を確認する必要がある。内部CPU配置図は調査用で、実UnityのAfterとして提出しない。追加地表のメモリとFPSはPixel実測まで未確認。

## Codex／AI側から見た次の改善候補

今回は追加なし。承認済みの西カフェを実Unityで確認することを優先し、成長説明など受領済みのFBは後工程へ残す。
