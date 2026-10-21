# 後退禁止のベース版：2026-10-21

**実装済み・実機未確認。今回ソースのUnity実描画・QA APK生成・Pixel確認は未実施。採用／完成／リリース合格は保留。** 前版bd3f357の成功は今回のBeforeとして保持する。

## 今回まとめた変更

- 中央の噴水の両脇に初期から座席と灯りを設け、花の縁・木陰・低い柵で広場を構成。中央と東西南北の接続に同じ素材を置き、各小道の先にベンチ・カフェ席・売り場・掲示板をまとめた。舗装の過大な石の縮尺を抑えた。歩行は依然5つの広い面で、芝生も歩ける。
- 追加家具の基礎は描画と歩行で共有し、Lv0から見える。旧の6つの小道の幅、4つの広い接続、噴水の両側、全入口の経路検査を維持。花壇がかかった道は家具の配置を動かして解消した。
- 「ベンチへ行く」の到着点を実際の中央ベンチ／北の掲示板の席の脇へ合わせた。既存place IDは変更せず、セーブを移行・初期化しない。
- Mergeは平面5×6と48単位以上の入力を維持。ミント／ピーチの浅いマス、原画のローズと金の枠、縦横の安全限界内で大きくした絵、小さめのLv帯に調整。1.06倍の選択拡大と3単位の持ち上げでも、最小マスで絵が隣へ出ない。ドラッグ／候補枠／合成の光・跳ね・Lv.UPは維持。保存盤面へ見栄え用のアイテムを足さない。
- NPCは足元が接近しきる前から避ける。従来の到達／会話／指追従を保ち、向かい合う2名が広い面ですれ違う回帰も検査。名札が他の体を覆う場合は控え、手動ドラッグ中も更新する。メニュー内にはスクロール案内を設けた。

これらはソースで実装した内容であり、「街が完成した」「見た目が改善した」と実機合格した意味ではない。

## 凍結と後退防止

[Visual Lock](../References/VisualLock/base-2026-10-21.json) は153入力を前版で固定。正式6名・ロゴ・既存画像／音源、TOP本文、図鑑、日次、選曲／音設定／保存、Coreの保存・5×6・依頼・納品・報酬が対象。テキストのLF／CRLF差は許容するが、内容とバイナリの変更を拒否する。基準の書き換えで回避しない。

[前版の実描画102枚の記録](../References/VisualLock/baseline-native-2026-10-21.json) を固定した。次の街QAビルドは同名102枚を照合し、`before-after.html` と `visual-adoption.json` を生成する。TOP・5地区・Merge・UI・図鑑・街Lvだけでなく全撮影場面と9月22日基準を確認できる。PNGを描き換えない。同じ画像は「維持」と表示し、撮影日／NPCシミュレーション差を明記する。

採用の6軸は **見た目／操作／情報／世界観／FPS／Pixel負荷**。`Assert-BaseAdoption` は102比較と全軸・Pixelの明示的な合格がない記録を拒否する。ハッシュ・画像・テスト成功だけで合格にはしない。数値・見た目の判定は人の確認を含み、生成HTMLで自動採用しない。

## 確認段階

|段階|今回ソース|
|---|---|
|設計済み|既存5エリア・平面5×6、凍結対象、比較条件を固定|
|実装済み|街・Merge・可読性・後退防止をまとめた候補|
|Unity確認済み|**未確認**。実DLL参照コンパイルは実描画と区別|
|APK生成済み|**未生成**。前版APKを今回のAfterとして使わない|
|Pixel 3a確認済み|**未確認**。採用・完成は保留|

ローカル検証：Core125、純C# Presentation241、6asmdefのUnity実DLL参照コンパイル（警告・エラー0）、凍結／比較／引数／Pixel時刻集計／既存Androidスクリプトの検査。PowerShellの端末テストはモックで、Pixelへ接続したものではない。[照合結果](verification/visual-lock-base-2026-10-21.json)。

この環境のEditorを今回再起動して確認し、有効ライセンスなしで終了198。インストール済みPlaybackEnginesはLinuxStandaloneSupportのみ。クラウドからPixelへのUSB経路もない。新しいUnity／APK／Pixelの証拠をここで作成できない。内部CPU配置図は構成確認にのみ使い、提出用の実Unity Afterに混ぜていない。

## Windowsで同じ候補を一括確認する

GitHub Desktopの現在のfeatureブランチをPullしてから実行する。mainへ切り替えたり、ローカルの保存や変更を消したりしない。

```powershell
& { $ErrorActionPreference="Stop"; Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"; powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa }
```

EditMode全件、PlayMode全件（最低207・新規2件も必須）、実Unity102枚、同一ソース／fingerprintのQA APK、比較HTML／未採用JSON、レビューZIPをまとめて作る。前版ディレクトリ `Builds/Android/20261021-113845-fbcafb0d` はBefore用に残す。ない場合は比較未完了として記録し、採用を保留する。

新しいZIPを確認してから、その実行のAPKを保存維持でPixelへ更新する。今回ソースのAPKはまだ存在しないため、古いAPKの固定パスをAfterとして案内しない。

## Pixelでの比較

[画面と負荷の採取補助](../scripts/capture-pixel-visual-check.ps1) はアプリの保存を消さず、インストールも行わない。旧APKのままBefore、新APKへ保存維持更新後にAfterを採る。各版で同じLv／開始地点／30秒の同じ散歩またはMerge操作を行い、温度・充電状態・画面条件も揃える。

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\capture-pixel-visual-check.ps1" -Label Before
# 新APKの描画確認・保存維持インストール後、同じ条件で実施
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\capture-pixel-visual-check.ps1" -Label After
```

Pixel画像、インストール済みAPKのSHA256、Unity SurfaceViewの実表示時刻・表示FPS・p95表示間隔、PSS、温度状態を新しい証拠フォルダへ保存する。Android windowのgfxinfoをUnity FPSの代用にしない。API／SurfaceFlingerが情報を返さない、フレームが少ない、途中停止の場合は測定未完了とし、良い数値で埋めない。これは現時点で端末未検証の補助で、取得できた値をそのまま自動合格にするものではない。

中央→東西南北、近いすれ違い・指追従、入口のドア、Merge、図鑑／今日、通常の納品／街成長、BGMと強制終了後の保存復帰、10分程度の継続操作を確認する。FPS／負荷が悪化する、または見た目・操作などが悪くなった場合は採用を保留する。

## まだ弱い点

石畳の周期的な模様、装飾とキャラの前後関係、店先・広場の密度、枠の装飾と小さい画面の読みやすさは、今回のUnity／Pixel画像で判定する必要がある。内部図で素材を増やしたことだけを「完成した街」と判定していない。NPCのすれ違いもモデルの検査だけで、顔の見え方や端末の操作感を合格にしない。

## Codex／AI側から見た次の改善候補

今回はなし。新しい機能や高度2.5Dへ進まず、今回候補の実画面・操作・負荷の判定を先に確定する。
