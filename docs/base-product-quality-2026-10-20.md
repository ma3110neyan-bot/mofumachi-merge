# 2026-10-20 統合ベース版の候補と実画面受入

**実装済み・実機未確認。完成判定は保留。** 最新ユーザーの「今回は機能追加ではなく商品化」に従い、街・施設・仲間・Merge・日次の接続を一括で改修した。旧製品 `80e535d` の実Unity90枚／APKの確認結果は、新候補へ引き継がない。

## Windows実行 `20261020-183917-4ab92b8e` の停止と修正

受領した実EditMode XMLは **143件：成功142／失敗1／skip0**。失敗は `PresentationContentTests.CustomGraphicsCanBeClippedDisabledAndDestroyedWithoutRendererErrors`。ログとXMLの同じスタックが `TownVillageDressing.SpriteBounds` → `TownVillageDetailGraphic.Bound` → `OnPopulateMesh` を指す。PlayMode・撮影・APK生成へは進んでいない。

追加した `TownVillageDetailGraphic` は、絵柄の設定前でもUnityのCanvas更新から描画を呼ばれる。初期値 `TownSceneryKind.Cafe` は別アトラスの種類で、村の部品の切出しには使用できない。未設定状態を区別せず切り出そうとした初期化不備だった。

設定前は空のメッシュで終了し、`Bind` では種類を検証してから描画状態を更新するよう修正した。不正な種類は呼出元で拒否し、最後の有効な設定を壊さない。失敗した既存検査を維持し、未設定時は空・設定後8種類は有限な四角形／正しいUV・入力を取らない・再表示と破棄まで確認する内容へ強化した。検査件数は増えていない。

修正後のクラウド再検査は **純粋Presentation232全成功・skip0、実Unity DLL参照の6アセンブリは警告／エラー0**。これはネイティブCanvasでの再実行ではない。**修正後のWindows全件・実描画・新APK・Pixelは確認待ち**。同じ統合候補としてPull後に下記の一括QAを再開する。

受領証拠と修正後のクラウド結果は `/workspace/acceptance/base-rebuild-20261020/windows-183917-4ab92b8e/` に保持した。今回のソース修正は描画部品1つと関連検査のみで、保存・盤面ルール・納品報酬・キャラ原画は変更なし。

## 次のWindows実行 `20261020-184914-b47689c0`

ユーザーの終了画面では **Edit143／143成功**。受領した実PlayMode XMLは **203件：成功197／失敗6／skip0**。撮影を含むPlayMode全件の成功条件に達しておらず、スクリプトはAPK生成前で停止した。

6件を例外・期待値・製品コードへ追跡した結果、承認済みの表示／入場操作へ変更した際に、旧仕様の検査条件が残っていた。以下は**検査条件の修正**で、ゲームの画面改善実績や実機合格には数えない。製品コード・合否ゲート・203件の検査数は変更していない。

|失敗した検査|原因と維持する検査|
|---|---|
|`ExperienceGrowthTests.NextUnlockIsVisibleAndUpdatesFromTheExistingSuccessfulDelivery`|小さい街HUDへ依頼主入りの全文を要求していた。HUDに次Lvと解放物、依頼画面に依頼主と詳細があることを確認し、納品30Coins／重複受取なし／余分な保存なしの検査を維持。|
|`ExperienceGrowthTests.UnknownQuestAndFinaleNeverInventAnotherUnlock`|全依頼完了後の案内を旧「お茶会」の文言へ固定していた。仲間と今日の会話へ戻る現在の案内と、架空の次Lv／依頼／報酬を増やさない保証を確認。|
|`MergeProgressionUiTests.InputSystemTouchDragAndTapReachBreadCellsThroughEventSystem`|意図的に持ち上がるアイコンの中心を指と同じ位置へ要求していた。Canvas内で近くに持ち上がり、候補へ寄り、指の位置へ正しく落とせることを確認。実Touchscreen経由の合成・タップ移動・保存・入力設定復元を維持し、演出後の操作可能状態を上限付きで待つ。|
|`ResidentPresenceTests.PauseAndDisableClearReactionGaitAndKeepPositiveDepthScale`|遠くからショップを開こうとしてモーダルにならず、停止姿勢の検査まで進めなかった。街のメニューを実際に開き、接地・表情・有限な描画・無効化の復元を検査。|
|`TownLifeTests.GrowthLayersFollowSavedLevelWithoutChangingPaths`|同じく遠隔入店を期待していた。成長段階と6名の確認に加え、花屋へ歩く→到着して待つ→ドアをタップ→入店を実行し、進行・Coins・メイン保存が変わらないことを確認。|
|`TownViewportPresentationTests.PauseModalAndResizeReleaseCameraInput`|同じ遠隔入店をカメラ入力解除のきっかけにしていた。街のメニューを開き、操作中の指を持っている状態からメニュー・実際の画面リサイズ・アプリ中断で解除されることを確認。|

同じ旧入店の前提が残っていた、成功済みの `TownMotionPresentationTests.StationaryDragAndLifecycleStopGait` も、実際に指を持ってメニューを開いて解除する条件へ揃えた。既に指が解除された状態だけで停止検査が成功しないようにした。

修正後は6アセンブリの実Unity DLL参照コンパイルを実行し、警告／エラー0。**修正後のネイティブ203件全成功・After画像レビュー・APK・Pixelは未確認**。受領XML／logと診断は `/workspace/acceptance/base-rebuild-20261020/windows-184914-b47689c0/` に保持する。引き続き下記の同じ一括QAから再開する。

## Windows実行 `20261020-190548-99299700` の残り1件

受領した実PlayMode XMLは **203件：成功202／失敗1／skip0**。前回の6件中5件は成功し、Mergeのタッチ検査だけが残った。中心へ近づけたときの吸い寄せは検査を通過し、その次の「中心へ動かす」操作でアイコンの浮きが変わらなかった。

原因は検査の座標指定。`MergeGridLayout` はセルのpivotを左下 `(0,0)` にしているが、検査の `Position` 補助関数は `transform.position` を返していた。これはセルの左下角であり、中心ではない。途中の接近位置は `rect.center` から計算していたため吸い寄せが働き、最後の指先を左下角へ戻したため、中心付近に働く吸い寄せの範囲から外れていた。前回の検査修正で見落としていた前提だった。

補助関数を `rect.TransformPoint(rect.rect.center)` とCanvasのカメラから求めるよう修正し、実際に送る始点・終点がセル中心へ戻せることも確認する。発光枠の比較にも同じ「中心は原点0」の前提が残っていたため、`target.rect.center` と比較するよう訂正した。持ち上げ／吸い寄せ／実タッチによる合成と移動／保存／入力復元の条件は維持している。製品コード・検査件数・合否ゲートは変更なし。

6アセンブリの実Unity DLL参照コンパイルは警告／エラー0。別担当が修正と後続のドロップ・保存・入力解除・タップ移動まで確認し、具体的な指摘なし。**修正後のネイティブ全件成功・After画像レビュー・APK・Pixelは依然として未確認**。今回の受領証拠は `/workspace/acceptance/base-rebuild-20261020/windows-190548-99299700/` に保持し、一括QAで再取得する。

## 実装した体験と、これから目で確かめること

- **街**：中央＋東西南北の役割・座標を保持。反復する樹木の列を整理し、住宅・植栽・カフェ席・花屋の店先・掲示板・庭のまとまりを追加。地面は控えめな草地と分岐する小道へ。新しい透明素材は共有アトラス1枚、Android上限1024・mipmapなし・読み取りコピーなし。画面外の小物は個別に非表示。実描画では密度、色、足元、道の幅、境界の自然さを判定する。
- **歩行と成長**：噴水左右・店先・四方向の経路を保持。固い追加物の配置と歩行除外を同じ宣言から供給する。初期から見える建物だけを固定障害物とし、未来の飾りによる見えない壁を作らない。Lv.3で居場所、Lv.6で花と店舗周辺のまとまりを追加する。透明画像の実際の足元と除外領域の一致、Pixelでのすれ違いと成長差は未確認。
- **HUD**：街をSafeArea全体へ広げ、今日・街Lv・Coins・メニューを小さく上部へ。下部は小さな目的カードとMergeのみ。散歩・会話中は目的カードを引っ込め、景色を見せる。メニューから各施設・図鑑・今日へ接続する。
- **施設**：北の依頼小屋、西カフェ、東花屋へ歩く→入口で待つ→ドアをタップ→開く→対応画面。歩いただけでは自動入場しない。カフェは実際に近くにいる仲間との会話、花屋は既存のお花を作るMergeへ接続する。商品購入・課金・新しい報酬は追加していない。物理タッチでの入口の押しやすさは実画面で確認する。
- **今日と図鑑**：「会いに行く」は仲間の実際の位置へ歩く。案内だけで会話回数を増やさず、会話は本人に会ってから。今日の3項目を行動へつなぎ、受取可能時の花を見つけやすくする。図鑑のNEW／？？？／次の解放を維持し、納品後にも依頼主の図鑑へ戻る導線を追加した。
- **平面Merge**：明るいアイボリーとローズの盤面、見分けやすい大きな元アイテム、選択枠・合成相手の発光を整えた。指で持ち上げ、相手へ近づけ、光って合体し、軽く跳ねて着地する。必要数と不足数を同時表示。生成と納品を優先し、整理は副操作へ。短画面でも30セルと必要アイテム操作の48以上の領域を確保する設計。実際の文字・操作感・演出はUnity／Pixel待ち。
- **キャラ**：正式原画を維持し、速度に沿う足運び、支持足の接地、重心、方向転換、まばたき、目線、花／ベンチを見る動き、到着時の表情を調整。元絵が自然に生きて見えるかは静止画だけでなく操作でも判定する。
- **文字とBGM**：既存の紙枠・リボン・絵本本文を維持し、必要数や入口の案内を整えた。街の「おさんぽタウン」は変更なし。Merge未選曲時の標準を既存のPOP候補へ変更し、保存済みの選曲を優先。タイトル・Mergeの候補、8秒試聴、実画面での試聴を保持。新曲の正式採用は実機試聴後。

## 固定した仕様

Core、SaveData、5×6、納品／報酬計算、依頼進行は変更なし。正式6名の原画・全身アトラス・ロゴ・元街背景のバイト列が改修前と一致することを検査した。既存プレイヤーの空いた盤面を撮影用に埋めない。検査の豊富な盤面・解放済み図鑑は隔離したテスト状態であり、実プレイヤーの進行ではない。外部SDK・有料課金・ガチャ・main統合なし。

## 確認段階

|段階|新候補の状態|
|---|---|
|設計済み|包括承認された5エリア／平面Merge／既存進行内で固定|
|実装済み|一括改修を製品コードへ統合。下記のクラウド検査を実行|
|Unity確認済み|**未実施**。6DLLコンパイルは実描画の証拠ではない|
|APK生成済み|**未実施**。旧APKへ今回の変更は入っていない|
|Pixel 3a確認済み|**未実施**。見た目・操作・音・保存復帰・性能の合否は未判定|

実行した最低条件：Core125／純粋Presentation232は全成功・skip0。実Unity6000.6.4f1 DLLを直接参照する6アセンブリのコンパイルは警告・エラー0。4フォント各568字の必要文字を検査。POP音源の5検査、アトラスの透過・切出し範囲・インポート設定、正式素材の保持を検査した。音源検査は聴感評価ではない。

PowerShellのソース同一性・画像不足・失敗XML・保存を消さない更新手順の検査も成功した。ただしそのログに出る203件／102枚／Pixelインストールは**合成XML・PNGヘッダー・模擬ADB**。実Unity／実APK／実機の証拠ではない。新候補のPlayMode203件はソース上の宣言数であり、実行数・成功数ではない。

全体レビューで発見した「別目的地へ案内しても古い仲間への訪問が再開する」「設定から戻ったNPCの移動先を現在地と表示する」を修正し、実Unityでの再現ケースを追加した。これらのPlayModeケースもまだ未実行。

クラウドの証拠は `/workspace/acceptance/base-rebuild-20261020/tests/`。`core-integrated.trx`、`presentation-final.trx`、`unity-reference-final.log`、`audio-pop.log`、`new-capture-gate-green.log` を区別して保持する。

## 次に行う一度のWindows検証

クラウドは有効なUnityライセンスがなく、既知のネイティブ起動がexit198。Android Build Supportも未導入、PixelへのUSB経路もない。このため、ライセンス済みの既存Windows環境で実描画とAPKを取得する必要がある。認証情報の送信やセーブの削除は不要。

1. Unity Editorを閉じ、GitHub Desktopで `feat/daily-world-pass-20261016` をPullする。ローカル変更がある場合は捨てず、競合の内容を確認する。
2. PowerShellで次の1行を実行する。

```powershell
& { $ErrorActionPreference="Stop"; Set-Location -LiteralPath "C:\Users\User\Desktop\Mofumachi\mofumachi-merge"; powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\scripts\build-android.ps1" -TownQa }
```

3. 成功時の `APK:` と `SHA256:` の画面、およびその実行フォルダの **`five-plaza-review-*.zip` 全部**を照合する。ZIPの分割数は画像容量で変わる。失敗時は同じ実行フォルダの該当XML／logを使い、古い結果へ差し替えない。
4. 実画像を比較し、画面上の品質不足があれば同じまとめ工程内で修正する。検査成功だけでPixel合格としない。
5. 新候補APKのパス・package・SHAを確定してから、`scripts/install-pixel3a.ps1` に `-Apk <そのAPK>`、`-PackageName com.mofumachi.merge.qa.townlife`、`-Serial 99RAY1BELH` を渡して更新する。`-r` で街QAのセーブを保持する。旧APK・本番packageへ取り違えない。

撮影とビルド中にソースが変わった場合や、必須画像が欠けた場合はAPKを受入候補としない。102枚の予定画像は720×1280の実uGUI描画で、Pixelキャプチャではない。

## Before／Afterの照合表

Beforeは実行 `20261020-104223-10416661` の90枚。クラウド保管先は `/workspace/acceptance/cardinal-town-20261019/native-20261020-104223-10416661/`。今回送付されたPixel PDF16ページは `/workspace/acceptance/base-rebuild-20261020/user-feedback/` に別保管している。新しいAfterはまだ存在しない。以下の**同名・同地区・同Lv・720×1280**を並べ、新旧のテスト状態を明記する。新HUDで表示範囲が広がる点は変更そのものとして比較する。

|対象|同条件の画像名|
|---|---|
|TOP|`23-title-letter`／`23-title-garden`（実画面試聴時）|
|中央／西／東／北／南|`04-town-lv1-{Central,West,East,North,South}`|
|街成長|各地区の `04-town-lv1-*`／`lv3-*`／`lv6-*`|
|四方向の接続|`09-connected-path-{west,east,north,south}`|
|歩行・指追従・すれ違い|`06-walking-*`／`16-residents-passing-*`／`17-finger-follow-*`／`17-finger-released`|
|東西と南北の徒歩|`18-continuous-walk-*`／`26-cardinal-*-join`／`*-arrival`|
|施設と入場|`20-quest-*`／`21-shop-*`。新規の到着待ち・西カフェ `21b-cafe-*` は旧同等画面がないことを明記|
|街UI|`24-home-unobstructed`／`24-menu-open`／`24-menu-return`|
|Merge|`01-merge-start`／`02-merge-all-items`／`03-merge-selection`／`27-merge-finale-live-purpose`|
|キャラ|`25-character-idle`／`step-a`／`step-b`／`greeting`／噴水の前後|
|図鑑・NEW|`10-book-*`／`19-book-new-in-town`／`19-book-discovery-first-page`|
|今日のお願い|`11-today-start`／`12-today-ready`／`13-today-flower-collected`|
|BGM・文字|`07-quest-story-goal`／`08-music-selection`／`22-*-candidates`／`23-*`、図鑑／今日の文字も拡大|

追加の `28-meet-friend-*` は実在する相手への徒歩訪問、`29-merge-*` は実際の生成／ドラッグ／合成ハンドラで撮る。旧版との同条件映像がない部分を、比較済みと表示しない。静止画は音・接地感・ドラッグ・フリーズ・保存の合格証拠にならない。

Pixelでは中央から四方向を歩き、自由な面・NPCすれ違い・指追従・3施設のドア・Merge一式・図鑑NEW・日次3項目／花・納品／成長・BGM試聴／選択／音量／ON-OFFを操作する。同じ街QAを強制終了して再起動し、保存済み盤面・進行・音設定を確認する。10分の散歩とMergeでクラッシュ／停止を観察する。品質基準は最後までユーザーの実機体験。

## Codex／AI側から見た次の改善候補

今回はなし。新しい仕組みを増やす前に、この統合候補を実描画とPixelで判定し、必須範囲の品質不足を残さず直す。

環境の再開設定は `start_skill` のみを更新して保存した。古い3地域の分岐・検査件数を最新方針と混同しないための変更で、インストールスクリプト・認証・ネットワークは変更なし。設定の保存は公開／適用の完了を意味しない。
