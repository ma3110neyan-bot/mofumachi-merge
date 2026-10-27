# 中央→北／ノア撮影テストの探索階層修正

20261027-214011-240b9a49、source31bccbbの実PlayMode XMLは234件中233成功、failed1/skip0/inconclusive0。専用の北3件とノア4件は成功。新しい比較撮影テストだけがExperienceCaptureTests.cs:146で、北接続Graphicのnull検査に失敗した。原XML／ログとSHAは References/Review/full-playmode-2026-10-27-214011/ に改変せず保存した。EditMode144/144成功はコンソール報告で、今回EditMode XML自体は未受領。

原因は撮影テストの探索ルート。TownLifeViewはキャラ用レイヤーで、TownContinuousWorldView.Bindは地面の親をbackground.parentの隣接sceneryとして作る。キャラ側の子孫から地面は見つからない。実機ではなく同じUnity実行で専用北テストがFlowルートから北Graphicを発見して3件成功しており、地面の欠落ではない。比較撮影もそのFlowルートから既存のGraphicを探すよう修正した。非null／正式node名の検査は残し、代替オブジェクトの生成や検査スキップは行わない。

変更は撮影コードの探索先と説明コメント、および同一候補を識別するcapture hash／renderer digestと検証記録だけ。噴水、北・中央の景観、ノア表現、正式原画、歩行、保存、依頼、納品、報酬は変更していない。

修正後の内部検査はUnity APIに対する6assemblyコンパイルが警告0／エラー0、PowerShell17本すべて成功。docs/verification/north-noah-capture-repair-2026-10-27/results.json に記録した。修正後のライセンス付きUnity実行／234件成功、比較画像、APK生成、Pixelは再確認待ち。233件成功を234件成功や修正版の成功と表現しない。更新後は従来と同じ build-android.ps1 -TownQa を再実行し、正常終了した新runを提出する。

Codex／AI側から見た次の改善候補：今回はなし。
