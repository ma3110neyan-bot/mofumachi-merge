# キャラ比較撮影の初期状態と画角修正

run20261027-220456-e7e5ea0bの実ログは修正版feb330d適用を確認。実PlayMode234件中233成功、failed1/skip0/inconclusive0。前回の階層エラーは解消し、北の9比較ペアと6往復経路の検査を終えた後、ノア切抜きのyMax1467.535が1280画像を超えて停止した。画像原本は今回未受領なので視覚採用は行わない。原XML／ログはReferences/Review/full-playmode-2026-10-27-220456/に保存した。

原因は撮影fixtureがShowHomeを位置初期化として使ったこと。UIFlowController.RenderはHomeWorldSnapshotへ現在地を保存し、TopHomeScreensはこれを復元する。歩行検査末尾はNoah(.30,.95)で、撮影が想定するsquare(0,-.25)とは違う。カメラだけ(0,-.05)へ固定したため人物が画面外へ出た。Momoも前の歩行時間中の現在地を引き継いでいた。

修正は撮影のセットアップだけ。6経路の実歩行・到着・保存非変更を確認した後、撮影カメラを破棄し、既存EnterHome helperで別の検査用シーンへ入る。新しいFlow、nullの一時snapshot、Noahの実初期square、Momoの元のArrivalFriendを検証してから、新カメラと切抜きを決定する。これは別の撮影episodeの準備で、行き詰まりをteleportで救済した歩行証拠ではない。JSONにも別シーン開始と検証結果を記録する。

全48枚の画面内／切抜き内包含検査、実歩行の検査、会話双方の反応・向き・保存／同一時刻比較を維持する。失敗時はframe番号・mode・world座標・camera・crop・actor矩形をassert messageへ含める。Debug.Logや製品用ラベルは追加しない。

実ゲームの位置引き継ぎ、カメラ、Save、移動、噴水、街、原画、ノア表現は変更しない。内部検証は6assemblyのAPIコンパイルが警告0／エラー0、PowerShell17本すべて成功、独立レビューのCritical0／Important0。実製品モデルのCPUプローブでも前終了地点(.30,.95)と新シーンの元のNoah/Momo初期座標を確認した。証拠はdocs/verification/north-noah-capture-scene-repair-2026-10-27/へ記録した。これらを修正版のUnity描画成功として扱わない。修正後の実Unity全件／画像／APK／Pixelは再確認待ち。更新後は従来どおりbuild-android.ps1 -TownQaを実行する。

Codex／AI側から見た次の改善候補：今回はなし。
