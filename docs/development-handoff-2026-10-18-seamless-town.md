# 2026-10-18 単一フィールド・施設・音楽候補

状態：**製品へ実装した候補。Unity実描画・QA APK・Pixelは未確認。main未統合。**

## 今回の変更
- 旧3背景＋縦端フェード＋芝生帯の描画を廃止し、1枚の新しい連続街マップへ置換。北はカフェと依頼小屋、中央は噴水公園、南は工房・花とショップ。同じ石畳がつながる。
- 新しい絵に合わせて歩行面・噴水・植込み・柵・テーブルを設定。全既存目的地へ迂回経路を確認。指追従、NPC回避、3ボタンのファストトラベルを保持。
- 両小屋の入口をタップ→扉まで歩行→開扉→既存画面へ入場。別の行き先・キャラ／カメラのドラッグ・中断時に予約を取消。北の依頼ナビも徒歩案内に変更。ショップ販売・課金は追加していない。
- TOP2曲／Merge2曲の新規候補、8秒試聴、用途別の選択保持、旧選曲の該当用途への引き継ぎ、実際の画面で聴く操作。おさんぽタウンと正式キャラ素材はバイト一致。[候補と試聴](../References/Preview/seamless-town-2026-10-18/README.md)。比較初期値は採用確定ではない。
- 依頼の最終行・Home次目標の表示枠を調整。フォント566字を収録。図鑑・今日・花・5×6・保存／納品／報酬は既存仕様。

## 検証の区別
- Core125件・純粋Presentation102件はクラウドで成功。実Unity DLL参照の6アセンブリはコンパイル成功。これはUnity内の実行成功を意味しない。
- PowerShellの実Git／合成XML／モックADBの検査に成功。これは端末へのインストールではない。
- Windows宣言構成はEdit142／Play174（DailyWorld10、MajorPolish6、SeamlessField7）。実際のXMLが正。
- クラウドUnityは有効なEditorライセンスがなくexit198。AndroidPlayerモジュールもなく、PixelへUSB接続できない。実描画／新APK／PixelはWindowsで続行する。

## 次の一括実行
Unity Editorを閉じ、GitHub Desktopで同じfeatureブランチをFetch→Pullしてから実行。

```powershell
Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"; powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa
```

1回の実行でEdit全件→Play全件＋67枚720×1280→texture-memory→同じソースのAPK→capture-manifest/build-info/SHA256を生成。失敗・skip・不一致時はAPKに進まない。

最初にターミナルの終了画面、同じ実行フォルダのeditmode.xml/playmode.xml/build-info.json/capture-manifest.jsonと67枚を確認する。新しいAPKのパスはこの実行が出力したものを使う。過去APKや単に最終更新時刻が新しいファイルを選ばない。

## Before／Afterの実画面待ち
Before：今回ユーザー提供のPixel PDF（10ページ）と前候補46dcf8eのUnity53枚。
After：今回のWindows生成67枚＋そのAPKを更新したPixel画面。新背景素材や歩行マスクは制作確認用であり、Unity/Pixel After画像として扱わない。

既存01–19のMerge・3地域・接続・図鑑・今日・歩行に加え、20依頼小屋／21ショップは外観・歩行・開扉・室内、22は候補一覧、23は4曲の実画面再生を記録する。

## Pixel受入（未実施）
同じbuild-info/SHAのAPKをinstall -rで更新し、Coins／Lv／お気に入り／現在依頼／盤面／音量設定を前後で照合する。北⇄中央⇄南を通常歩行し、継ぎ目・ぼかし・白画面・フェード・瞬間移動がないか確認。小屋の扉、NPCすれ違い、指追従、Merge、図鑑、今日、花、4候補＋おさんぽタウンを試す。10分以上の一周と再起動で保存・音・クラッシュ・発熱を確認する。これらが終わるまで完成／main統合候補とはしない。

## レビュー対応
独立レビューで見つかった旧BGM選択の引き継ぎ、短い画面の看板、カメラパン後の入場予約、道案内の横幅、極端に縦長な画面の地図カバーを修正。純粋Presentationは最終99件成功。ネイティブ側の短画面／縦長／入場中断の検査は追加・更新し、コンパイルまで確認した。Unity実行成功とは区別する。

実装判断：包括承認の範囲を一括で進めた。既存IDと保存を維持し、新地図の座標だけPresentationへ置いた。旧ローカル座標に依存する呼び出しを移行する必要があるため、Windows全件の合否をAPKの前提とする。

## Windows 102552 実行の停止原因と対応

`20261018-102552-14b9ad5e` の提供XMLはPlay174件中167成功・7失敗・skip0。Edit142成功は端末出力で確認。APK工程へは進んでいない。

- DailyWorld／Pop／TownHabit／TownLifeの4件：EditorのGameビュー寸法で画面内NPCがいない状況を、固定の`First`で取得していた。スマホ寸法の実Canvasを用意し、中央公園の実在NPCと操作キャラを映す。NPCの移動位置や保存は書き換えない。拒否タップでは実際のポインター所有とNPC可視も確認。
- Popの1件：旧地域のローカル座標を、連続ワールドの実座標と比較していた。新マップと同じ投影で照合し、3単位未満の許容差と全身・操作領域の検査を保持。
- QualityPass成長の1件：初期の花・街灯が地図端に寄り、スマホの地域到着ビューにも見えない配置だった。初期カメラ内の花壇付近へ移動。3地域×短／通常／縦長画面の配置回帰3件は修正前に失敗、修正後成功。ネイティブもLv1の花／Lv3の街灯／Lv6を各画面で確認する。
- SeamlessFieldの1件：依頼画面への既存ナビゲーション保存が更新する`lastSaveTime`を完全不変と誤認。既存の保存回数+1と日時形式・ディスク一致を確認し、日時以外の全フィールドは引き続き完全一致させる。保存実装・報酬計算は変更していない。

修正後のWindows PlayMode再実行と67枚の実画像は未確認。成功扱いにはせず、同じ一括スクリプトで再検証する。
