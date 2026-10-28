# 北接続の範囲検証：Unityと同じ精度で照合

10月28日追記：修正版のrun101255-ecac0943は全Unity144＋234件／250画像／北ノア検証を通り、両QA APK生成まで正常終了したと画面報告を受領。原本／実画像／Pixelは未確認。次は生成済み同runのZIP全パート確認で、記録更新だけの再ビルドは不要。以下は10月27日の停止時点の診断。

最新のユーザーコンソール画面はEditMode144/144、PlayMode234/234、実画像250枚720×1280の生成を報告している。Unityは完了し、その後のPowerShell範囲検証で停止した。原XML／今回のJSON／画像はまだ受領しておらず、全体の原本照合・視覚採用・Pixelを完了扱いにしない。今回の出力フォルダーとsource SHAは画面だけでは確認できない。先頭のexplorer行は前回フォルダーなので今回runと誤記しない。

C#のNorthWorldBoundsはfloat（Single）でx最小-.6f、最大.6f、y最小1f、最大1.32fを定義する。UnityのJsonUtilityは.6fを0.6000000238418579として記録し得る。受領済みの実Unity fountain-live.jsonでもphaseに同じ表現を確認した。旧PowerShellはdoubleへ変換して±.6のdoubleリテラルと完全一致を求め、同じ座標を範囲変更と誤判定した。

修正は検証側の精度だけ。Numberの欠落／型／NaN／Infinity拒否を残し、4座標を元のSingle精度で完全一致判定する。許容幅を広げたり、元JSON／画像／地面を変更したりしない。y最大の旧1e-6許容もやめて、他3座標と同じ元Single値にそろえる。実際の別のSingle座標は、隣の表現可能値1刻みでも全4項目で上下とも拒否する。

REDでは単精度を正確に表したJSONが旧ゲートで同じNorth changed its bounded scopeを再現。GREENでは短い小数表記と元のSingleの両方を受理し、8つの隣接Singleへの改変を拒否する。これはメタデータ検証で、実画面レビューの代わりではない。ゲーム本体・撮影C#・テスト件数・キャラ・噴水・歩行・Saveは変更しない。

更新後は従来どおりbuild-android.ps1 -TownQaで、同一runの全件成功→原本検証→QA APK生成へ進む。新しいシステムや検査の省略は加えない。画像・APK・Pixelは成功した今回runの証拠を確認するまで保留する。

Codex／AI側から見た次の改善候補：今回はなし。

修正後のPowerShell全17本が成功。独立レビューCritical0／Important0。証拠はdocs/verification/north-noah-bounds-precision-2026-10-27/。C#・Unityテストの実装は変わっていないため、内部API／CPUスイートの成功数を新たに増やして報告しない。
