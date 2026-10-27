# もふまちメルジュ：今後の開発方針

## 2026-10-27最新指示：噴水採用固定／中央→北とノア1名だけ

ユーザーは現在の噴水を**リリース版として採用・固定**した。以後の噴水追加改修は不要、派手な演出はリリース後の候補だけ。過去の「噴水未承認」「2.5D着手不可」は履歴であり、この最新の明示承認を優先する。計測していないPixelのFPS・発熱・全Lv再起動を成功扱いにはしない。

現在の承認範囲は**中央→北の地面接続の局所修正＋街のノア1名の2.5D試作を並行**。原画／顔／服／色／体型／他5名／図鑑／BGM／Save／依頼／納品／報酬／Merge／噴水／既存景観／歩行判定を維持。`fountain-release-and-field-freeze-2026-10-27.json` はff7215cの502製品入力を固定し、正規化SHAをゲートへ固定した。旧354入力は今回明示許可されたBody描画の1ファイルだけを検証済み候補へ置換し、原本JSONや原画を上書きしない。

候補は `north-noah-candidate-2026-10-27.json`、手順は `docs/north-noah-trial-2026-10-27.md`。設計・実装済み。内部NUnit365＋125件、PS17本、実Unity APIへの6assemblyコンパイルが成功（描画実行ではない）。**この候補の実Unity描画／APK生成／Pixel確認は未実行**。前版bb3fb82の成功を新版へ流用しない。実同条件Before／Afterと静止・歩行・会話、Pixel負荷確認後に採否を判断し、ノア未承認で6名へ展開しない。今回は**セーブ維持の更新インストール**とし、以前のFresh手順を自動再実行しない。main／Mergeへ進めない。

**2026-10-27 run214011受領：実PlayMode234件中233成功・failed1/skip0。** 北専用3／ノア専用4は成功。新比較撮影がキャラ層の子孫から隣接する北地面を探しnullになった。撮影探索先だけFlowルートへ修正し、景観／ノア／噴水のruntimeは無変更。原本と診断はReferences/Review/full-playmode-2026-10-27-214011/。修正後の234件／画像／APK／Pixelは未確認。手順docs/north-noah-capture-repair-2026-10-27.md。

**run220456受領：修正版feb330d適用済み、実PlayMode233/234・failed1/skip0。** 前回の北Graphic階層エラーは解消。新比較撮影は北9ペアと6往復経路の検査を通り、ノア切抜きでyMax1467.535>1280となった。ShowHomeが正常に位置snapshotを引き継ぐのに、撮影が初期位置へ戻ると誤って想定していた。歩行証拠完了後に別の隔離Homeシーンへ入り、元のNoah/Momo位置と新カメラを検証する。実ゲームのsnapshot／Save／キャラ・噴水は無変更。原本References/Review/full-playmode-2026-10-27-220456/。今回修正後のUnity全件／画像／APK／Pixelは未確認。

以下は過去の検証履歴。

**2026-10-27最新受領：18ZIP・484原本、sourcebb3fb82／run102109-3bbbf9feの実Unity370件全Passed。** Edit144/144・Play226/226、failed0skip0inconclusive0。全219全景＋240実Update切出し＋実旧Before3枚＝462PNGのSHA／bytes／寸法／全デコード、既存全件／境界／歩行／実成長／水／show／連続ゲート、原354／固定景観18binding・歴史digestが成功。captureと街／成長APKのsource／fingerprintdca9b087…が一致。街79,033,873bytes／SHA1a4b9951…、成長79,033,881／SHA907da881…、両成功logの厳密bytes一致。binary自体は未受領、端末で実SHA／IDを照合する。原本と判定はReferences/Review/fountain-final-2026-10-27-candidate/。

**全6実納品・各Lvの5地区／再読込が実Unityで成功、Pixelの初回実操作／完全終了再起動は未確認。** 導入0→1→2→3→4→5→6、Lv4/5を含む60成長／再読込画像。保存時刻以外のGameStateと実表示オブジェクトが再読込で一致。初期無料お茶2個以外は実生成0/2/2/6/6/6・合成1/1/1/4/4/4、累計Coins30/70/110/180/250/330。中央Lv4/5・北南Lv1/2は正しく不変。毎回1依頼、後半同量の現バランスは未変更。Nativeのreward_replay_rejectedは空activeからDeliver拒否で、元完了依頼の再受注は別確認。

**同じ生成済み街QAで承認済みFreshインストールへ進行可、噴水採用は保留。** 主担当が私的に全219／240／旧Before3を確認（今回は別の独立レビューではない）。通常の落下芯／波紋／しぶき／反射、実Update60.117秒・前兆35.966／show状態37.977／明瞭な高jet約38.48／最大水量近く38.731／復帰44.019／通常45.025、44.52標本で高jet消失、60秒まで残留なし。4fps標本を実機FPSと呼ばない。87同一／72変更／60新規全景、非中央Lv1/3/6の12画面byte同一。全差分を水へ帰属しない。通常因果対2215/2237/3529、高jet3686/3723/3691px・投影外0。OFF/ONは追加水全体の比較で旧水との同一瞬間切替ではない。

持越し：公式会話パネルが水面を覆い、初期Lv0の連続記録では80/240標本で部分重なり（show後半含む）。高jetは見えるが実機で見失わないか確認。旧Lv6と条件が違い頻度後退とは断定しない。東木陰／端の視認と自力帰還、固体噴水の歩行前後も実機重点。帯状のshowはMinorで端末の自然さを見て判断。今は推測で街／UIを変更しない。

**次はdocs/fountain-final-growth-pixel3a-2026-10-27.mdの既存SHA／package／serial／model照合→-FreshTownQa。** ユーザー明示の初回化指示どおり街QAだけuninstall／install／復元QAデータ消去。別の承認不要、製品／成長QA／他端末は対象外。現在は削除未実行。今回受領は記録更新だけでゲーム・テスト・QAツール無変更、Pull／再ビルド不要。初期0/0Coins/未選択/2茶Lv1→実納品で全Lv→各Lvの5地区・保存・完全終了再起動→噴水60秒／顔・前後／自動会話／600秒とFPS・熱・音・停止を同じ新APKで確認。Pixel／ユーザー承認・2.5D／Merge／main未了。チャット画像なし。以下は履歴。

**2026-10-27：噴水最終Pass＋全Lvの実進行検証を実装、Windows全件QAへ引渡し準備。** 製品変更はTownFountainWaterLayout1ファイルのみ。通常水の白い落下する芯を長く／太く、青い水の通り道・しぶき・波紋・反射を明確化。showは連続する青い弧を残し白い水が弧に沿って移動する。196quad／784追加頂点・QA45秒／show6秒／release180秒、親のY深度・1.14倍・街全5地区・歩行・BGM／図鑑／Merge／Core・依頼／報酬／Saveは無変更。実機性能・見た目を合格扱いしない。

**新Nativeは全6遷移×5地区の成長比較＋再読込の60枚、全景計219枚を要求。** 導入0→1→2→3→4→5→6を本物の生成／合成／受注／納品／報酬で確認し、全GameState（保存時刻以外）と各地区の実オブジェクトを再読込照合。画面に隣地区が入っても自地区の変更／不変を判別する。中央Lv4/5・北南Lv1/2は不変。Lv4/5欠落・材料／Coins違い・画像欠落はgateで拒否。実Update60秒の噴水も直接Lv6を設定せず初期Lv0で実行する。元の354／固定景観／旧candidateは変えず、新scope fountain-final-candidate-2026-10-27と実98596daf/192546のBefore3枚へbind。

**Source検証：CPU353/353＋Core125/125、噴水RED2→GREEN18、API6組0警告エラー。** VSTestのloopback通信制限／再許可待ちを避け、通信を一切使わない同じNUnit3 frameworkのin-process runnerで実行した。Unity実描画には数えない。PS全15/15成功（PowerShell7、Windows5.1実行は次のQAで確認）。独立最終レビューCritical0／Important0／Minor1（Nativeのreward_replay_rejectedは再読込後の空activeでDeliver拒否を確認し、元依頼の再受注は試していない。Core監査では別途確認済み、Pixelで元依頼の再受注／報酬も別確認）。新Unity／APK／Pixelは未実行。原本と状態はdocs/verification/fountain-final-growth-2026-10-27/。

**Pixel初回化は既存install-pixel3a.ps1の明示-FreshTownQaのみ。** townlifeの正確なID／serial99RAY1BELH／Pixel3a／新APK SHA確認前に削除しない。対象街QAだけuninstall→install（-rなし）→復元されたQAデータのみpm clear→起動。defaultは従来install-rで削除なし、製品・成長QA・他端末Freshは拒否。これはmockで確認しただけで、実機削除は未実行。新しいWindows全件のXML／219画像／同sourceのAPK・SHAを確認後にのみ初回インストールへ。

手順docs/fountain-final-growth-review-2026-10-27.md。現在のfeatureを維持し、main／PR／2.5D／Merge／新機能／進行バランス変更へ進まない。まずWindowsの最新をPull→既存-TownQaを一度→新runのfive-plaza-review全ZIP。成功前にQAを先に削除しない。旧192546成功・既存APKは新版へ流用不可。チャット画像なし。以下は履歴。

**2026-10-26最新ユーザー：10月27日は「噴水最終Pass＋初回からの街Lv実進行」だけ。** 2.5D／Merge／新機能／mainは停止。噴水は通常の明確な流水・しぶき・波紋・きらめき、QA30〜60秒内show5〜8秒・自然復帰・顔と前後・Pixel負荷を最終確認。旧噴水成功を新しい最終Passへ流用しない。12頁PDFは `References/Review/fountain-growth-device-2026-10-26/`、街Lv6／330Coins／ノアと通常・高い噴流を観察したが、動画／FPS／熱／10分／初回進行／プロセス再起動の証明ではない。

**今回に限り、現在の街QAをアンインストールして新規インストールする明示指示が、以前の保存維持install-r方針を上書きする。** 対象はPixel99RAY1BELHの `com.mofumachi.merge.qa.townlife` だけ。最終APKのSHA／packageを照合してから実行。製品／成長確認ID／他アプリは削除しない。自動復元で旧Lv6が戻れば初回成功としない。同じ許可を再質問しない。今はUSB接続なしで削除・インストール未実行。

**現状値のCore監査済み、バランス変更なし。** 初期はHUDもLv0・無料お茶Lv1×2・0Coins。導入0→1の後、指定の1→2→3→4→5→6を全部確認する。固有メイン依頼完了数＝街Lv、追加は毎回1件。1→2／2→3の素材換算2／2個、3→4／4→5／5→6は6／6／6個、合成1／1／4／4／4回で段階的に長くならない。6依頼後は次がなく、表示は6へclamp。値を勝手に変えず先に報告する。全6実Core納品・各Lv完全ディスクセーブ再読込・5地区論理装飾・二重報酬拒否を実行成功、Unity／Android再起動／Pixel実進行とは別。既存Unityは3→4・4→5の全地区再読込を省いており拡充が必要。中央Lv4/5・北南Lv2の装飾不変を誤失敗にしない。

調査・Lv7以降／初回Lv8/10/12比較（推奨10、提案のみ）は `docs/fountain-growth-audit-2026-10-27.md`、実行計画は `docs/superpowers/plans/2026-10-27-fountain-final-and-growth.md`。今回はゲーム・既存テスト・QAツールを変更せず、監査／記録だけ。最終噴水調整／全LvのUnity／新APK／初回Pixelは未実行。各Lvの条件・追加／置換／不変・保存・完全終了再起動を全段階記録し、Lv4を省かない。Lv6は序盤の節目、以降は実依頼・街の変化・保存互換を伴う設計を提示するだけで今は実装しない。自動的な翌日実行を約束しない。チャット画像なし。以下は履歴。

**2026-10-26最新受領：run192546-249dc5eaの15ZIP・430原本を確認、原Unity370件全Passed。** source98596daf／fingerprintca87897b…。Edit144/144・Play226/226、failed0skip0inconclusive0。前回17件の不要ログ失敗が原XMLで解消。新165全景＋240実Update切出し＋旧Before3の全SHA・bytes・寸法・デコード、既存Native／成長／境界／歩行／水／短show／連続証拠ゲート、原354／固定景観18bindingと歴史digestが成功。街APK79,030,001bytes／SHA90c225ca…、成長79,030,049／SHA0b6c747c…、両source／fingerprint／成功log／bytes一致。binary未受領で独立SHAは端末側で照合。原本と判定 `References/Review/fountain-motion-2026-10-26-candidate/`。

**判定：既に生成した同run街QAでPixel確認へ進行可、噴水採用は未承認。** 実Update60.114秒・240枚、前兆35.958／show状態37.971／高い噴き上げ38.472／最大38.723／復帰43.997／通常45.003秒。44.500秒で高いjetは消え60秒まで残留なし。4fps記録を実機FPS／30fpsの滑らかさの証拠にしない。通常の筋・しぶき・波紋・反射の変化、全景で高い3噴流を確認。独立実画像レビューはCritical0／新たなImportant景観後退0。前景の顔・服・名前は読め、奥の水は首／服に重なるが目は読める。

90同一／72変更／3新規全景で、西東北南Lv1/3/6の12全景と実成長6は前版byte同一。72変更の全差分を水へ帰属しない。因果対は通常1578/1612/2698px、show同一瞬間8130/8133/10158px、専用Nativeの高jet3676/3710/3682、投影外0。Beforeは実5edfa/111414原本3枚。持越しは既存会話パネル（48/240標本、通常時のみ。show中は覆わず、旧頻度は未比較）、東木陰の帰還／視認、ライブ固体噴水の前後。新showのリボン状の線はMinorで、実機の自然さを判断してから局所調整。街やUIを推測で改造しない。

**次は192546街QAだけをPixelへ保存維持更新し600秒確認。Pull／再ビルド不要。** 今回は原本受領と記録だけで、ゲーム・テスト・QAツール無変更。`docs/fountain-motion-pixel3a-2026-10-26.md` の新SHA照合→99RAY1BELH／com.mofumachi.merge.qa.townlifeへinstall-r。中央を開いてから計測し、通常／60秒以内show／前後／会話表示／既存東帰還・重さ／温度／音／10分／再起動保存を同版で確認。ユーザーの街固定承認は維持、Pixel／噴水採用・2.5D／Merge／以降／mainは未了。チャット画像なし。以下は履歴。

**2026-10-26最新原本：run185535-c31c8bfbはPlayMode209/226、failed17／skip0。原因は全17件とも新規MOFUMACHI_FOUNTAIN診断Debug.Logを既存NoUnexpectedReceivedが拒否したこと。** 原XML／log／全失敗名とSHAは `References/Review/fountain-motion-qa-failure-2026-10-26/`。source842d8ff想定だが、この2原本だけではproductFingerprintの同一性は未照合。Edit144/144は画面報告。60.56秒の実Updateケースも結果はFailed（同ログ）で、36秒前兆・38秒show・44秒復帰・45秒通常の状態記録を確認するが、Unity全件成功／画像採用／Pixel成功へ読み替えない。

**修正はWaterGraphicの自動ログ出力と不要フィールド／usingの削除だけ。** 水流・色・頂点数・時計・QA45秒・前後描画・街・Save・全テスト／NoUnexpectedReceivedは変更しない。連続原本とJSONのstage／phase証拠はそのまま残す。新SHA／描画digestを同候補へ再bindし、原354／固定景観／歴史digestは維持。最新をPull後に既存TownQaを一度再実行し、全370ケース／failed0skip0と165全景＋連続原本を受領するまで新Unity／APK／Pixelは未合格。今回のログ除去を見た目の再改修と呼ばない。以下は履歴。

**2026-10-26最新ユーザー：噴水は未達。良い方向への改善作業を続行。** 前版Pixelで通常の動き／3分以上後のshowが確認できず、静止画・369成功だけで採用へ進めない。今回runtimeはWaterLayout／WaterGraphicの2ファイルだけ。固定帯の明滅を落下する白い流水へ変え、青縁／しぶき／波紋／反射／曲線showを強化。既存196quad784＋旧672＝1456頂点内、Particle／ライト／製品RT追加なし。原画・14％拡大・親のY深度・5地区・歩行・保存・図鑑・BGM・キャラ・Mergeは不変。

**街QA／Editorは45秒周期、開始38秒・最大約39秒、show6秒・復帰1秒。** 非Developmentは180秒を維持。Home内は画面外でも時計だけ進み、非表示メッシュ更新なし。メニュー／別画面／休止停止・Home再構築で時計保持・低FPSの実時間を切り捨てない。ユーザーの旧端末で未発生だった個別原因は旧実行ログなしに断定しない。

**ソース修正済み・新版Unity／APK／Pixel未確認。** CPU新RED7→GREEN16、Presentation351/351＋Core125/125成功。新Unity226棚卸しは成功結果ではない。Native新165全景に加え、実Update60秒／約4fpsの実描画crop・SHA／時刻／位相／stage・HTMLを必須化。手動Advanceの既存33/34対とは分離。切出しは実レイアウト確定後、各frameで実頂点包含を検査。連続証拠不発生／停止位相／時刻不一致／欠落をQAが拒否し、静止画だけでAPKへ進めない。

新scope `fountain-motion-candidate-2026-10-26.json` は原354／固定街と旧Beforeを保持。Beforeは5edfa5db／111414の実原本3枚。旧candidate/native成功は履歴のまま改変せず、新候補のnative/APK/Pixel採用フラグはfalse。手順 `docs/fountain-motion-review-2026-10-26.md`。今回はコード変更があるので最新Pull後にTownQaを一度実行し、新連続原本を受領してから新SHAの街QAへ保存維持更新。ユーザーが新版を実際に確認するまで噴水合格にしない。負荷不成立なら軽量代案を先に承認取得。2.5D／Merge／以降／main停止、チャット画像なし。以下は履歴。

**2026-10-26最新受領：強化噴水の13ZIP・185原本を照合、実Unity369件Passed。** run `20261026-111414-78cde31d`、source5edfa5db、fingerprint1fa608c9…。Edit144/144・Play225/225、failed0／skip0／inconclusive0。新162PNG＋旧Before3のSHA・bytes・720×1280・デコード、通常水／show因果検査を含む既存ゲートと原354／固定景観18binding成功。両APK/source/fingerprint/成功log/bytes一致。街79,027,285bytes・SHA14594390…、成長79,027,325bytes・SHA3934fdab…。原本と判定は `References/Review/fountain-show-2026-10-26-candidate/`。APKbinary未受領、独立binarySHAはWindows側で照合する。

**判定：同run街QAによる条件付きPixel確認へ進行可、噴水採用は保留。** 私的に全162＋旧Before3を点検し、独立実画像レビューはCritical0／新たなImportant景観後退0。拡大・強い通常水・高い3本の噴き上げは実画面で明確。西東北南Lv1/3/6の12全景と実成長6枚は前版とbyte同一。85同一／69変更／show8追加。通常OFF/ON923／963／1741画素、show同一瞬間OFF/ON5061／5056／6387画素、専用Native検査の高いjet2344／2361／2345画素・投影外0。通常→show33132等の全差分には表情／名前／文字等が混ざるので全て新水へ帰属させない。前兆／復帰は静止画で長いjet残留なし。実時間の動き・滑らかさは未確認。

持越しImportant2は東の木陰からの視認／自力帰還と通常固体噴水／キャラ本体のライブ前後切替。新showは奥の体に水が重なる静的証拠があるがライブの代用でない。Minor2は新showの直線的な細い水流（奥の口／胴に重なるが両目は読める）と既存北外周。今は推測で再改修せずPixelで自然さ／読みやすさを確認。旧showコードレビューI1の「通常波紋と混じる証拠」は、今回実Native因果対で検証できた。

**次は既に生成した111414街QAだけをPixelへ更新し600秒確認。** `docs/fountain-show-pixel3a-2026-10-26.md` の新SHA照合→Pixel99RAY1BELH・既存IDcom.mofumachi.merge.qa.townlifeへ保存維持install-r。今回ゲーム／テスト／QAツールは無変更、記録更新だけでPull／再ビルドを依頼しない。中央の累積約3分・2秒前兆/6秒show/1秒復帰、前後・顔/名前、menu/Pause/画面外、10分／FPS・バッテリー温度・thermal／音・停止・再起動保存を同版で確認。計測不能はnull、Before比較がなければFPS低下／熱増加なしと断定しない。街の正式ベース候補固定は維持し、実機／噴水ユーザー承認・2.5D／Merge／以降／mainは未了。画像をチャットへ貼らない。以下は履歴。

**2026-10-26最新ユーザー：弱い噴水候補は不十分。噴水だけを主役へ強化する指示に限定実装。** 街5地区の正式ベース候補固定は維持。現在の原画を同じY.285から描画だけ14％拡大し、通常の太い6水流／しぶき／波紋／反射、中央が見えて操作可能な累積180秒周期の2秒前兆→6秒ショー→1秒復帰を追加。3本の高い噴き上げも同じ親深度。3runtimeファイルのみ（FountainGraphic／WaterLayout／WaterGraphic）、Nav／Collider／地形・道・花壇・街灯・ベンチ／Core・Save／報酬／正式6名／図鑑／BGM／Mergeは無変更。CWTで同UIFlowのHome再構築を跨ぐ一時時計、Pause／modal／画面外停止、.1dtcap、30Hz時相、追加196quad784＋旧672＝1456頂点。性能の実測値とはしない。

**ソース確認済み・新版Unity／APK／Pixel未確認。** 旧model RED5／10→GREEN10、全CPU345/345 failed0skip0（9m16s）、最終噴水10/10、API6DLL0警告エラー、PS14本成功。単独の独立コードレビューCritical0／Important1／Minor0。I1の通常波紋を混ぜたshow差分を修正し、同一瞬間OFF/ONの通常UV.84より上のjet>40画素／投影外0／時計・旧波紋位相・NPC・camera・Save非変化へ厳密化、artifact gateは実RED→GREEN。新OFF3枚を保存するため新162撮影＋旧Before3。既存field検査の「拡大Feet＋旧高さ」も共有Project(uvY1)へ合わせ、重なり・歩行検査は緩めない。仕様／inline計画は `docs/superpowers/{specs,plans}/2026-10-26-fountain-show*`、証拠は `docs/verification/fountain-show-2026-10-26/`。

**次はPull後の全件TownQaを一度実行。** 手順 `docs/fountain-show-review-2026-10-26.md`。宣言144Edit／225Playは実成功ではない。新162PNGと、8aa7a14／093151の旧現在版Before3原本を別provenanceでZIP化し、通常／前兆／show3視点／復帰と3同一瞬間OFFcontrolsを確認する。新scope town_fountain_show_2026_10_26は18厳密入力・旧BeforeSHAに結び、原354／JPEG15／fieldfreezeを維持。currentdigest f4ddc00d…／歴史dfb2cbc8…、419fileCRLF対応。旧093151は新版の合格証拠へ流用しない。新原XML・両APK/source/SHA・画像を受領するまで新Unity／APK成功と呼ばない。

Pixel99RAY1BELH・街QA IDcom.mofumachi.merge.qa.townlifeへ新SHA照合→保存維持install-r。噴水が通常サイズで明確に流れ、約3分で短いshow、自然な前後と通常復帰、10分／保存を確認する。既存capture-pixel-visual-check.ps1は600秒とバッテリー開始／終了温度・thermal原文を記録でき、SurfaceView有効時刻から実FPSを測る。値が取れなければnull／未計測、バッテリー温度をCPU温度と呼ばない。今回PDF4頁は現状FBだけでFPS／熱／600秒の証明でない。噴水合格まで2.5D／Merge／以降／main停止。画像をチャットへ貼らない。以下は履歴。

**2026-10-26最新受領：噴水版の12ZIP・172原本を照合済み。** run `20261026-093151-b7d557b1`、ソース8aa7a142、fingerprint97e700b2…。原Edit144/144・Play225/225の全369ケースPassed、failed0／skip0／inconclusive0。全154PNGのSHA・bytes・720×1280・デコード、既存全件／境界／歩行／実成長／噴水ゲート、原354・固定景観と17入力bindingが成功。両APKのsource／fingerprint／成功log／bytesが一致。原本と判定は `References/Review/fountain-water-2026-10-26-candidate/`。街QA79,021,005bytes／SHA2971b220…、成長79,021,029bytes／SHA5dd9a110…。APKbinary未受領、独立binarySHAはWindows側で照合する。

**判定は同run街QAによる条件付きPixel確認へ進行可、噴水採用は保留。** 全154枚を私的に点検・単独の独立実画像レビュー。新たなImportant景観後退0、持越しImportant2は東の木陰での視認／タップ・ドラッグ帰還と、奥の噴水がキャラ本体を自然に隠すライブ切替。Minor2は控えめすぎる可能性のある新水と既存北外周。西東北南のLv1/3/6全景12枚は固定前とbyte同一、中央3枚の184画素差は噴水内だけ。87同一／57変更／10追加。日替わり11/12/13・Merge着地29・Southjoin26微差を全て新噴水に帰属させない。新レイヤーOFF/ONの全景39／手前36／奥169画素差、投影外0。顔の知覚できる新隠れなしだがalpha縁の微差があり「キャラ画素完全一致」とはしない。4時相は旧水も進むため全差分を新効果の証明にしない。

**次は既に生成した街QAだけを更新し、通常サイズで水が動いて見えるか／前後／負荷を確認。** 手順 `docs/fountain-water-pixel3a-2026-10-26.md`、Pixel99RAY1BELH・街QA IDcom.mofumachi.merge.qa.townlifeへSHA照合→保存維持install-r。今回ゲーム・テスト・QAツール無変更で記録更新のみ、Pull／再ビルドを依頼しない。既存保存維持／噴水手前→左右→奥→手前／NPC／5地区往復・東帰還／600秒・熱・音・重さ／再起動保存を同版で確認する。街のユーザー正式ベース候補固定は維持。実機・噴水承認は未了で2.5D／Merge／次工程／main停止。画像をチャットへ貼らない。以下は過去段階の記録。

**2026-10-25最新ユーザー：街5地区を正式ベース候補として固定し、第1工程「噴水エフェクト」だけ開始する指示。** 旧「全Pixel条件確認まで噴水も停止」は、この明示の工程移行承認で上書きする。全Pixel負荷／600秒／初期成長確認済みと推論しない。残すPolishはキャラ重なり／会話位置／噴水前後／端詰まり／小境界のみ。街構造・景観の大改造なし。噴水合格まで代表1名2.5D／Merge／図鑑前面化／Tutorial／ReleasePolish／mainへ進めない。

噴水の計画 `docs/superpowers/plans/2026-10-25-fountain-water.md`、仕様対応spec。2834cb7から同featureでinline実装済み。原画・既存波紋を残し、Fountain子の新TownFountainWaterGraphic1個＋純UVQuad samplerを追加。6本水流／小しぶき／3波紋／4反射、80quad320頂点、時相30Hz、画面外・無効・Pause時停止。WorldViewは子bind／camera変更時refresh／既存CanSimulateの読み取りだけで、Depth.285／Nav／地形・原画・保存・報酬・キャラ・BGM・図鑑・Mergeは無変更。添付PDF14頁・ユーザー固定承認は `References/Review/town-field-user-approval-2026-10-25/` と `References/VisualLock/town-field-frozen-2026-10-25.json`。性能・ライブ噴水切替や全初期成長の証明にはしない。

**噴水候補のソース検証済み、実Unity／新APK／Pixel未確認。** 追加CPUはmissingmodel RED5→GREEN5、全CPU340/340 failed0skip0（9m48s）、API6DLL0警告エラー、PS14本成功。PS5area fixtureの新freeze参照2件コピー漏れだけ修正してRED→GREEN。独立全変更レビュー0Critical/Important/Minor。原354／JPEG15／歴史的dfb2cbc8…は不変。新scope town_fountain_water_2026_10_25の17入力と凍結済み景観を厳密に結び、他の既存Nav／成長を再hashしても拒否。currentdigest5fb43abb…、417fileCRLF preflight成功。証拠 `docs/verification/fountain-water-2026-10-25/`。

**次はPull→既存全件 -TownQa を一度実行。** runtime変更なので今回は新ビルドが必要。宣言144Edit／225Play（実成功ではない）、全154PNG＝旧144＋噴水10枚を要求する。33-fountainの全景／手前／奥3対と4時相、metadata／HTMLを同runZIPへ含める。Beforeは新レイヤーのみOFFで旧水・NPC・camera・保存は同じ、旧APK全体再現ではない。噴水投影外差分0／可視変化ありをNativeで検査する。全件failed0skip0・原本・両APK識別／SHAを受領してから新街QAをPixel99RAY1BELHへ既存IDcom.mofumachi.merge.qa.townlifeで保存維持install-r、視認性・ライブ前後・負荷／熱／音・再起動・10分を確認する。旧210356では新効果を検証できない。手順 `docs/fountain-water-review-2026-10-25.md`。噴水合格まで2.5D／Merge／次工程／main停止。画像をチャットへ貼らない。以下は履歴。

**2026-10-25最新原本：210356の11ZIP・160原本を照合し、Edit144/144・Play224/224の全368ケースPassed／failed0／skip0／inconclusive0、144PNGのSHA・bytes・720×1280・デコード正常。** build／growth-build／captureは同じ43a2764／fingerprint21a04dc3…、両APKの成功log・bytes一致。街118247418bytes／SHA0d3426dd…、成長79006293bytes／SHA2b5302be…、全文は `References/Review/town-field-final-2026-10-25-candidate/native-review.json`。APKbinary未受領で独立SHA再計算は未了。実納品1→2／2→3／5→6の5地区実装飾・再読込成功。全15全景＋既存22境界は旧153810とbyte同一。122同名不変／16変更／6新規。北上端・西外側の広い空白は植栽で改善、新景観後退は見当たらない。

**判定は同じ210356の2APKで条件付きPixel確認へ進行可、街正式固定は保留。** 単独の独立実画像レビューCritical0／Important2／Minor1。I1は既存東の木陰で顔・体・名前が隠れ、識別／タップ・ドラッグ帰還が実機未確認。I2は奥の噴水画像が名前札だけ重なり、キャラ本体の遮蔽・ライブ切替は未証明。M1は北上端のわずかな地面・花壇反復。修正を推測せず、Pixelで両重要項目を確認するための進行であり、後工程承認ではない。成長の短い強調・全地区切替／Lv2北南「次はLv.3」も実機で確認する。

今回ゲーム・テスト・QAツール・VisualLock原本は無変更、**Pull／再ビルド不要**。`docs/town-field-final-pixel3a-2026-10-25.md` の正確なSHA照合→Pixel99RAY1BELHへ成長確認com.mofumachi.merge.qa.growthcheck／街QAcom.mofumachi.merge.qa.townlifeの明示ID・install-r（街QAを最後に開く）。既存保存消去／アンインストールなし。5地区連続移動、東木陰から自力帰還、噴水前後、境界、実納品成長と再起動保存、600秒・熱・重さ・音を同版で合格させるまで正式固定・噴水エフェクト／2.5D／Merge／mainは停止。報告は指定8項目、画像をチャットへ貼らない。以下は履歴。

**2026-10-25最新終了画面：210356はEdit144/144・Play224/224、144画像720×1280、街QA／別保存の成長確認APKの2本・各SHA・レビューZIP出力まで正常終了。** run `20261025-210356-e7e1ca30`、期待ソース43a2764。両APKの出力後にPSプロンプトへ戻った。画面SHAprefixは街0d3426dd…／成長2b5302be…、全文は原build-infoから確定する。原XML／failed0・skip0／両build-info／画像原本／APKbinary／Pixelは未受領・未確認。画面記録 `docs/verification/town-field-final-qa-build-screen-report-2026-10-25.json`。次は同runのfive-plaza-review-*.zip全パートを受領し、実画像／原結果／両APKのsource・SHAを確認してからPixelへ進む。記録だけでPull／再ビルドを依頼しない。街の正式固定・次工程・mainは保留継続。チャットに画像を貼らない。以下は履歴。

**2026-10-25最新原本：204913のPlayModeは224中223成功／1失敗／skip0。** 唯一の失敗はSeamlessFieldTests.OneSharedGroundAndFiveLandmarksReplaceTheOpaquePainting:22。境界を4帯→7帯へ追加した際、実メッシュ476頂点に対してテストの旧期待272が残った更新漏れ。原XML/log・SHA・診断は `References/Review/full-playmode-2026-10-25-204913/`。新しい実納品→5地区成長→Scene再読込のケース、元の全撮影、噴水前後、全5端部の実入力／自力帰還はいずれもこの実UnityでPassed。ただし画像原本未受領、表示レビュー／実機合格ではない。

修正はSeamlessFieldTestsの期待値272→476（7帯×17列×4行）と説明だけ。実メッシュの厳密一致／単一共有atlas／非raycastの検査は残し、ケース削除・skip・範囲緩和なし。runtime・原画・装飾・Nav／歩行面・保存／納品／報酬は無変更。API6DLL0警告エラー、関連CPU曲線5/5成功。**修正後の実Unity全件成功・APK2本・Pixelは未確認。** 新版をPull後、既存全件 `build-android.ps1 -TownQa` を一度実行して144/224・failed0/skip0／同版144画像・成長メタデータ／2APK・SHAを受領する。元承認／描画入力digest73b10279…は変更せず、今回のテスト修正を理由に承認値を上書きしない。街正式固定・噴水エフェクト／2.5D／Merge／mainは停止継続。画像をチャットへ貼らない。以下は履歴。

**2026-10-25最新画面：最終フィールド版の全件QAはEdit144/144成功、PlayModeでUnity exit2。** run `20261025-204913-be3b3a03`。PS178行はUnity非0終了の通知で根因ではない。Playの実件数／失敗名／skip／ソース同一性は原XML/log未受領で未確認、今回のAPK2本は未生成。記録 `docs/verification/town-field-final-qa-stop-screen-report-2026-10-25.json`。次は同runのplaymode.xml／playmode.logを受領し原因を特定する。推測の修正・同版の再実行依頼はしない。この記録だけではPull／再ビルド不要。街正式固定・噴水エフェクト／2.5D／Merge／mainは停止継続。画像をチャットへ貼らない。以下は履歴。

**2026-10-25最新：ユーザー「街フィールド最終完成指示」へ限定実装。実装済み・新版Unity／APK／Pixel未確認。** 現featureを維持し、正式固定・噴水エフェクト／2.5D／Merge／mainへ進めない。手順 `docs/town-field-final-pixel3a-2026-10-25.md`、仕様／計画はtown-field-final。画像をチャットに貼らない。

今回のruntime変更は外周GardenLayout/GraphicとGrowthPresentation/ComparisonViewの4ファイルのみ。北天端・東西外側端に元素材の植栽／曲線を足し、全矩形は歩行面の外、既存200外周Spriteの位置・向きは保持。243Sprite／476曲線頂点、共有素材・追加Colliderなし。成長比較を5地区へし、新装飾または次Lvを短く表示。北南の既存Lv3/6条件を守りLv2では「次はLv.3」。MasterLayout／原画・全歩行面／Nav／Core保存・納品・報酬・正式6名・図鑑/BGM/Mergeは無変更。

実納品の検査を追加：一時保存で通常の生成→Merge→受注→納品6件を進め、1→2／2→3／5→6で5地区の実表示と再読込を比較。直接townGrowthLevelや完了依頼を書かない新UnityTestは既存CaptureFixtureを利用し、6枚追加とtown-growth-progression.jsonを必須にした。元の見本撮影は別ケース。噴水奥の撮影点は実歩行可能な.441まで寄せ、Graphic全画面Rectではなく実メッシュ上端との重なりを検査する。これは実機前後関係の合格ではない。

**ソース確認：CPU335/335・Core125/125、API6DLL0警告エラー。Native宣言144Edit／224Playで実行成功ではない。** 全5地区の周縁・障害物脇の往復と重なり脱出は追加検査で停止せず、Nav変更なし。鏡像保持RED→GREEN、独立レビューのImportant（200既存Sprite反転）を修正。Homeからの空振り納品検査もQuest画面からの実試行／Coins・RewardIDs非変化へ修正。全PS14本成功、修正後関連2本再成功、411fileCRLFの現在／歴史的承認ゲート成功。原354／JPEG15／歴史的dfb2cbc8…は不変。新scope town_field_completion_2026_10_25、digest73b10279…、元承認を上書きしない。

**次はPull後に既存全件 build-android.ps1 -TownQa を一度実行。** 今回はruntimeが変わったため再ビルドが必要。新版144/224・failed0/skip0／144PNG／実成長メタデータ・同版APK/SHAを受領し、自然な境界と成長画面を確認。旧153810の成功は履歴で、新版へ流用しない。TownQaは同じテスト済みソースからtown-life-qa.apkとtown-growth-check.apkを生成する。後者はcom.mofumachi.merge.qa.growthcheck／「もふまちメルジュ 成長確認」の通常初期ゲームで、既存街QAのLv6保存を消さず実機1→2→3／5→6を検証する別保存。新しいゲーム機能／初期化／レベル注入はない。両APKのhash/appId/sourceを照合し、Pixel99RAY1BELHへ各IDを明示してinstall-r。削除／保存消去なし。Pixelで端部・東の木による隠れ・噴水横断・境界・実納品成長・再起動保存・600秒を合格させるまで正式固定しない。次回報告はユーザー指定の8項目に絞る。以下は履歴。

**2026-10-25最新原本：153810の11ZIPを照合、原Edit143/143・Play223/223、failed0／skip0、138PNGのSHA・寸法・デコード正常。** build/captureは871808c／fingerprint a7b8df5b…で一致。元APKは118242007bytes／街QA ID／SHA `6f1d163c6b8a14cf7025399e350e7f88e526f53856dd382f20d08b498beed124`。バイナリ未受領なので独立SHA再計算は未了。原本・判定は `References/Review/field-stability-2026-10-25-candidate/native-review.json`。

**判定：同じ153810APKで条件付きの実機検証へ進行可。街ベース合格／native visual cleared／正式採用は保留。** 全15全景は前回e7実原本とbyte同一、97同名画像同一／27変更／14新規。端部の実往復と通知を含む最前面入力、噴水前後の描画順検査は成功。ただし東端・東建物角では保持した木にミエルの顔／体／名前が隠れる。自然なY順と整合し新退行とは未確定だが、raycast false原画の遮蔽は入力テストから判定できない。Pixelで見つけてタップ／ドラッグ帰還できるか未解決。北上端／西左端に外周地面も見える。噴水奥の静止画はシルエットが重ならず、横断時の奥側遮蔽はPixel確認が必要。独立画像レビューCritical0／Important1は受入証拠の不足で、確定した新runtimeバグという意味ではない。景観を削る／歩行を狭める／常時キャラ最前面の推測修正はしない。

今回ゲーム・原画・テスト・QAツール無変更。**記録更新だけではPull／再ビルド不要。** `docs/field-stability-pixel3a-2026-10-25.md`の既存153810APKをローカルSHA照合→Pixel99RAY1BELHへcom.mofumachi.merge.qa.townlifeでinstall-r。保存消去なし。東の隠れ／噴水横断／全5端部・連続移動／現セーブで可能なループ／再起動保存／600秒・熱・音を確認する。実機未確認、正式ベース固定・噴水エフェクト／2.5D／Merge／mainは停止継続。チャットに画像を貼らない。以下は履歴。

**2026-10-25最新画面：通知余白修正版の一括QAは143/143・223/223、138画像、QAAPK生成まで正常終了。** run `20261025-153810-fa9b2be9`、画像720x1280、town-life-qa.apk。画面SHAprefix6f1d163c…、全文はbuild-info原本で確定する（誤転記しない）。記録 `docs/verification/discovery-clearance-qa-build-screen-report-2026-10-25.json`。原XML／build-info／capture-manifest／138PNG／APKbinary未受領、Windowsソース同一性と原failed0/skip0・SHA独立照合は未確認。想定871808cの実装成功と画面成功を原本検証へつなぐ。Pixel／正式ベース固定は未了。

**次は同runのfive-plaza-review-*.zip全パート受領・原本／実画面確認。記録更新だけではPull／再ビルドを依頼しない。** 新しい31-fieldの噴水手前／奥、東建物角往復、5端部往復、通知でキャラが隠れないこと、既存5地区Lv1/3/6の品質を確認。画像をチャットに貼らない。見た目・source照合後は今回生成済み153810APKをSHA照合しPixel99RAY1BELHへ街QA IDcom.mofumachi.merge.qa.townlifeでinstall-r。保存消去なし。通知を含む入力／保存・10分・品質を同版で確認するまで噴水エフェクト／2.5D／Merge／main停止。今回ゲーム・テスト・QAツール変更なし。以下は履歴。

**2026-10-25最新原本：131312の残1失敗は「図鑑の新発見」通知による実際のタッチ遮断。** PlayMode223/222成功/1失敗/skip0。北(.43,2.98)、grab(304.11,583.99)、キャラdepth26/cullFalseの前にNew book discovery(depth92)がいる。描画・イベントcameraは一致。原XML/log/SHA・診断 `References/Review/full-playmode-2026-10-25-131312/`。前回の検査camera補正だけで解消せず、今回の詳細ログで製品側の通知余白不足を特定。

**runtimeはTownContinuousWorldView1ファイルのみ追加修正。** 表示中のTownDiscoveryNoticeの実矩形も既存の上余白へ加える。GetComponentsInChildren(false,reusedList)で非表示・差替え中の旧通知を除外、フレームごとの結果配列を作らない。通知の絵／6秒タイマー／タップ図鑑遷移、図鑑・保存・原画・配置・歩行面・Nav・正式6名・BGM/Mergeは不変。通知を消す／タッチ判定を弱める処理はしない。既存の図鑑通知テストに有効／無効時の余白・キャラと通知の両方の実先頭hit・重なり不在を追加。全5端部の実歩行／ドラッグ／先頭hit検査はそのまま。

実装済み・API6compile0警告エラー・関連PS4本／410fileCRLFゲート成功・独立レビュー0Critical/Important/Minor。原354／15JPEGと歴史的dfb2cbc8…は不変、新候補digest ebe436e8…。**修正版の実Unity全件成功・新APK・Pixelは未確認。** 手順は引き続きfeaturePull→既存全件 -TownQa、failed0/skip0・同版138画像→新APK/SHA→Pixel99RAY1BELHへ保存維持install-r。通知を含む北端と全5地区を確認して初めてベース固定。噴水エフェクト／2.5D／Merge／main停止。証拠 `docs/verification/discovery-edge-clearance-2026-10-25.json`。画像をチャットへ貼らない。以下は履歴。

**2026-10-25最新画面：131312-6c6e5bcbはEdit143/143成功、PlayModeで再びUnity exit2。** 前回125258の原XML223/222/1とは別run。今回の失敗名・件数・skip・実際のsourceは原XML/log未受領で未確認、同じ原因と断定しない。APK未生成。次は `Builds/Android/20261025-131312-6c6e5bcb` のplaymode.xmlとplaymode.logを受領。51063d1の検査には対象名／地区／depth／cull／カメラの詳細を追加済みなので、適用と失敗箇所をまず原本で確認する。推測修正・再実行依頼をしない。今回の記録のみの更新ではPull／ビルド不要。以下は履歴。

**2026-10-25最新原本：125258のPlayMode223中222成功／1失敗／skip0。端部の最前面タッチ検査だけ停止。** 原XML/log・SHAは `References/Review/full-playmode-2026-10-25-125258/`。唯一の失敗はTownViewportPresentationTests.AllFiveEdgesKeepReturnInputAndWalkBackWithoutTeleporting:36、最前面hitが非キャラ（対象名／地区は旧メッセージにない）。噴水前後・全景／5地区往復撮影・既存30マス実raycast検査は実UnityでPassed。PNG原本は未受領なので実画像レビュー済みとは言わない。APK未生成。

追加検査はFrameTownPortraitでWorldSpaceに変更しながらイベントカメラを設定せずnullで投影していた。成功済みの既存FixedViewportをそのまま再利用（internal化だけ）し、描画／投影／Raycastを同じカメラに一致。実描画後も先頭hit必須・5端部／実ドラッグ／帰還を維持、失敗時の対象名／地区／depth／cullを記録する。**修正はテスト2ファイルのみ。ゲーム・原画・歩行・撮影・QAゲートは不変。元の非キャラhitの根因や修正版native成功はまだ未確認。**

API6compile0警告エラー、宣言143/223は棚卸しで実成功ではない。次は修正をPull後、既存の全件 `build-android.ps1 -TownQa` を再実行。failed0/skip0・同版138画像後にのみAPK/SHA→Pixel。984be108…／歴史的dfb2cbc8…と元354／15JPEGは不変。実機合格まで噴水エフェクト／2.5D／Merge／main停止、チャット画像なし。以下は履歴。

**2026-10-25最新画面：安定化版の全件QAはEdit143/143成功、PlayModeでUnity exit2。** run `20261025-125258-acabc327`、PS163行はUnity非0終了の通知で根因ではない。実PlayMode件数／失敗名／skip／source同一性は原XML/log未受領で未確認。APK未生成。画面記録 `docs/verification/field-stability-qa-stop-screen-report-2026-10-25.json`。

次は同runフォルダの `playmode.xml` と `playmode.log` を受領して診断する。推測でカメラ／描画／歩行／fixture／検査を変えず、原因特定前に再実行を依頼しない。今回の記録更新はゲーム・撮影・テスト無変更なのでPull／再ビルド不要。街ベース固定・噴水エフェクト／2.5D／Merge／mainは引き続き停止。以下は履歴。

**2026-10-25最優先：街フィールド最終安定化。次工程停止。** 最新ユーザーは噴水の自然な前後描画／東端から帰還不能／他4地区端部回帰だけを指示。旧e7a8006のUnity143/222・124画像／084947APK成功は履歴。新修正版のUnity実画面・新APK・Pixel成功には流用しない。画像をチャットへ貼らない。

今回runtimeは3ファイルだけ。ContinuousWorldViewで噴水の深度を画像下端.10→物理足元中心.285へ（原画座標は不変）。WorldLayoutでFollow/Return/Restore/Fitに追従キャラの位置を渡し、端で姿・操作対象が消えない範囲を確保。自由Panの北南制限は従来通り。AreaNavigationで初期.078以内のNPCを経路全体で無視せず、現在の距離からさらに接近しない脱出経路へ。東(1.5999999,.45000005)/(1.65,.45000005)から戻る途中の建物角(1.476,.1006)停止を修正。全原画・配置・装飾・歩行面／障害物・Core/Save/報酬・正式6名・図鑑/BGM/Mergeは不変。

**実装済み、CPU325/325・API6DLL成功。実Unity／新APK／Pixelは未確認。** Native宣言143/223（実行結果ではない）。全件TownQaは既存124＋14焦点画像＝138枚と端部の実操作検査を必須とする。新lockは `References/VisualLock/field-stability-candidate-2026-10-25.json`。原354／承認JPEG15は不変。新中央digestは984be108…、歴史的承認dfb2cbc8…を保持。ナビゲータを現在入力に追加し、歴史的digest再現からのみ除外。

次はこのfeatureの修正をPull→既存全件 `scripts/build-android.ps1 -TownQa` →failed0/skip0／同版138画像→新QA APK/SHA→既存Pixel99RAY1BELHへ街QA ID com.mofumachi.merge.qa.townlifeで更新install-r。保存消去／アンインストールなし。噴水手前・奥／全5地区の端部タップ・ドラッグ帰還／東建物角と近接NPC／連続5地区／保存・600秒を確認して初めて街ベース固定。旧084947APKでは今回修正を確認できない。手順 `docs/field-stability-pixel3a-2026-10-25.md`、証拠 `docs/verification/field-stability-2026-10-25.json`。噴水エフェクト→代表1名2.5D→Mergeは今回のPixel合格後、mainは別承認。以下は履歴。

**2026-10-25最新原本：曲線境界e7a8006の10ZIPを照合、Unity実画像はPixel確認へ進行可。** run `20261025-084947-eecbc994`、原136ファイル／PNG124枚全SHA・寸法・デコード、実XML143/143・222/222の全ケースPassed／failed0・skip0を確認。build-info/captureはgit e7a8006、productFingerprint008c82ef…で一致。APK118244517bytes／街QA ID／SHAaae8f682…は先の画面とbuild-infoで一致するが、APKbinary未受領なので独立SHA再計算は未了。原本・判定 `References/Review/curved-boundary-2026-10-25-candidate/`。

97同名PNGは前版と同一、27変更。**5地区Lv1/3/6の15全景は前版5a49とbyte同一。今回11対の再現Beforeは前回実Afterとpixel同一。** 東西下端は直線切断から曲線で植栽へなじむ見た目に改善し、北・上端も軟化。新しい目立つ反射模様・ぼけ・密度低下・歩行幅縮小・名前／HUDの後退は見当たらない。独立実画像レビューCritical0／Important0／Minor2、Pixel進行可。J1は北左x290-331/y250-437の材質帯／北右建物脇x60-194/y587-596の既存線、NP1は既存外周の花壇反復。旧東西下端y737-740が「未改善のまま」とは報告しない。全継ぎ目解消も断定しない。

**次は生成済み084947APKでPixel受入。今回の原本受領・記録だけではPull／再ビルド不要。** `docs/town-boundary-pixel3a-acceptance-2026-10-25.md` は最新084947／SHAaae8…へ更新済み。ローカルSHA確認後、既存installer `-PackageName com.mofumachi.merge.qa.townlife -Serial 99RAY1BELH` のinstall-r。削除／データ消去／署名回避なし。境界曲線・5地区徒歩往復・薄文字／SafeArea／NPC・現在セーブで可能なコアループ・更新保存／再起動・600秒以上の実操作と熱／重さ／音を確認する。物語完了済みセーブを初期化して試験しない。

状態：実装済み・Unity実画面確認済み・同版QAAPK生成済み、Pixel／第1段階合格は未確認。噴水→代表1名2.5D→Mergeは第1段階合格後に順次。main別承認。ゲーム／原画／テスト／QAツールは今回無変更。画像をチャットに貼らない。以下は履歴。

**2026-10-25最新：曲線境界の一括QAは143/143・222/222、124画像、APK生成まで画面上で正常終了。** run `20261025-084947-eecbc994`、報告SHA `aae8f682cce320d55b22910fe4fd38404b1a7c2b225d60c220892c529a3f269a`。記録 `docs/verification/curved-boundary-qa-build-screen-report-2026-10-25.json`。成功XML／build-info／現124画像は未受領で、実ビルドsourceや自然な曲線になったことを画面の件数から推定しない。想定修正版e7a8006と原本のgitRevisionはZIP受領後に照合する。Pixel／第1段階合格は未確認。

**次は同runのfive-plaza-review-*.zip一式を受領して画像確認。記録更新だけではPull／再ビルドを依頼しない。** 直前5a49の描画（曲線のみOFF、既存植栽・入口帯はON）との同条件11対／5地区Lv1/3/6、元の密度・歩行面・名前札・石目を確認。旧073237版の実画像成功は新版へ流用しない。見た目確認後は生成済み084947のtown-life-qa.apkをSHA照合し、そのままPixel3aへ更新install-r（街QA ID／serial99RAY1BELHを明示）。削除／保存消去はしない。

今回は受領記録のみでゲーム／原画／テスト／QAツール無変更。第1段階の境界が実機で合格してから噴水→代表1名2.5D→Merge、mainは別承認のまま。チャットへ画像を貼らない。以下は履歴。

**2026-10-25最新：第1段階の境界を曲線で仕上げる限定実装。次工程順は噴水→代表1名2.5D→Mergeへ変更。** ユーザー「リリース品質改善・第1段階」を最優先。噴水／2.5D／Mergeを同時着手せず、境界の実機合格後に順番に進める。新PDF9+14ページを `References/Review/curved-boundary-device-2026-10-25/` に原本SHA付きで保持。Pixel継続文脈の実機画面で新外周・Coins330/Lv6を確認するが、APK照合・10分・保存復帰成功までは推定しない。

実装はTownBoundaryGardenLayout/Graphicに4本の曲線地表帯を追加し、SouthGroundからライブ／成長プレビューへ共有。原画全体・5地区MasterLayout.Dressing・既存200外周Sprite・歩行／衝突・Core／保存／報酬・正式6名・図鑑／BGM／Mergeは不変。西東下端はSouth原画外(absx>=.92)、北側端はy>=1.23。原画端と同じUVで接続、内側重なり最大.006world・外側曲線幅.065〜.132world。既存WestTexture1共有・272頂点キャッシュ・raycast false。密度や歩行範囲を削らない。

**新候補は実装済み・Unity再確認／新APK／Pixel未確認。** Beforeは直前5a49描画（新曲線のみOFF、外周植栽／入口帯は維持）の再現で、旧466再現を今回のBeforeとしない。歴史的APK／Pixelと偽らず、同条件11対／全5地区Lv1/3/6／124PNGを既存の全件TownQaで撮る。native宣言143/222維持。CPU曲線RED4→GREEN4・全315成功・API6DLL0警告エラー・関連3PSゲート／409fileCRLF入口通過、独立コードレビュー重要指摘なし。ただし自然に見えることは実Unity／Pixelで確認するまで未合格。

現在lockは `References/VisualLock/curved-boundary-candidate-2026-10-25.json`。元354／15画像と歴史的digestは不変、現在digesta613c1a7…へ新SHAだけ束縛。旧5a49の143/222実成功・APK2b519…を新版へ流用しない。次はfeatureをPull→全件 -TownQa→failed0/skip0と124画像・同版APK/SHA→境界Before/After／全5地区確認→Pixel。今回の実コード変更後は再ビルドが必要で、先の「受領記録だけなら再ビルド不要」と区別する。手順／検証は `docs/superpowers/plans/2026-10-25-curved-boundary.md` と `docs/verification/curved-boundary-2026-10-25.json`。main禁止、画像をチャットへ貼らない。以下は履歴。

**2026-10-25最新原本：街外周5a49e31の10ZIPを照合し、実Unity画像はPixel確認へ進行可。** run `20261025-073237-f8fd542b`、原136ファイル／PNG124枚を `References/Review/town-boundary-2026-10-25-candidate/` に保存。124画像全SHA・寸法720x1280・デコード、原XML143/143／222/222の全ケースPassed・failed0/skip0を確認。build-info/captureはgit5a49e31、productFingerprint ec37506a…が一致。APK118233994bytes／街QA ID／SHA2b519389…は先の画面とbuild-infoで一致するが、APK bytes未受領なので独立SHA再計算は未了。

全124画像を確認。大きな緑の外周余白、南の長方形残像／石目ぼけ、東西の接続は改善。5地区Lv1/3/6の構成・密度・成長差・道・正式UI／図鑑を維持。旧浮いた4方向案内は見当たらない。独立実画像レビューCritical0／Important0／Minor2、Pixelへ進行可。**全ての継ぎ目が消えたとは報告しない。** J1＝西東下端y737-740／北左x326-329の既存直線／材質境界、NP1＝新しい外周の楕円花壇・明るい木の反復。採用可の実機判定はまだで、継ぎ目の見え方と反復をPixelで確認する。判定は `native-review.json`、独立報告も同フォルダ。

**次は生成済み073237APKのPixel更新。Pull／再ビルド不要。** 今回はゲーム／原画／テスト／QAツールを変更していない。手順は `docs/town-boundary-pixel3a-acceptance-2026-10-25.md`。ローカルSHAを正確に照合し、既存installerで `-PackageName com.mofumachi.merge.qa.townlife -Serial 99RAY1BELH` の更新install-r。追加承認不要。削除／pmclear／署名回避はしない。徒歩4方向往復・表示／薄文字／SafeArea・コアループ可能範囲・更新保存と再起動・600秒以上の実操作／音／熱／詰まりを確認。物語完了済みセーブを初期化して試験を作らない。

**状態：実装済み・Unity実画面確認済み・同版QAAPK生成済み・Pixel未確認。** source/build5a49と後続の受領記録コミットを混同しない。元354承認／15JPEG／描画digesta8d5f737…は不変。旧221/222原本と今回の成功は別run。正式ベース固定・Merge Quality Pass・mainは未了。画像をチャットに貼らない。以下は履歴。

**2026-10-25最新：街外周修正版の全件143/143・222/222、124画像、QA APK生成を画面で確認。** run `20261025-073237-f8fd542b`、報告SHA `2b5193896965f805e086a51e5c19fbc1ae6a6ef6583aa8fd9755f0dfe8c6378a`。記録 `docs/verification/town-boundary-qa-build-screen-report-2026-10-25.json`。旧南16頂点の検査更新後、今回の全件成功は画面報告として確認。ただし成功XML・build-info・124画像・APK原本は未受領。SHA独立照合／撮影ソース／境界改善・Pixel合格は未確認と分ける。

**今回の記録更新だけではPull／再ビルドを依頼しない。** 次は同runの `five-plaza-review-*.zip` 一式を受領し、原XML・build-info・capture-manifest、11地点の旧描画再現Before／製品After、5地区Lv1/3/6を照合する。チャットへ画像は貼らない。見た目確認後は今回生成済みの `town-life-qa.apk` をそのまま使い、SHA／街QA IDを照合して既存 `install-pixel3a.ps1 -PackageName com.mofumachi.merge.qa.townlife -Serial 99RAY1BELH`（install-r）で更新し、5地区移動・表示・保存／再起動・600秒以上の試遊を確認する。インストールのための追加承認は不要、削除／pmclear／署名回避はしない。以前の保存復帰報告や221/222原本を今回の原本成功／実機結果へ流用しない。

**現在：Unity全件／124画像／APKの画面成功確認済み、原本・実画像レビュー待ち、Pixel未確認。** 街の正式ベース固定・Merge Quality Pass・mainは未了。ゲーム・原画・テスト・QAツールはこの受領記録では変更していない。新しいゲーム改修を加えず、生成済み成果物の確認を優先する。以下は履歴。

**2026-10-25最新原本：街外周版の全件PlayMode222中221成功／1失敗／skip0。南の旧頂点数検査だけ修正。** run `20261025-071609-9974cf36` の原XML/log・SHA・診断を `References/Review/full-playmode-2026-10-25-071609/` に保存。唯一の失敗はSouthMasterReviewTests.GardenGroundUsesTheAcceptedPaintingAndFollowsPanAndRestore:22で旧16頂点を期待、実際40。今回の外周表示修正で南16＋西12＋東12になったことに対する、こちらの検査更新漏れ。ExperienceCapture・5地区歩行統合・南の歩行／Lv表示・Seamlessは実Unityで成功したが、撮影PNG／build-infoは未受領で、景観合格やsource同一性を断定しない。APK未生成。

**変更するC#はテスト1ファイルのみ。** 合計40へ更新し、東西両接続の内外位置・中央UV一致・内端不透明（byte丸め1以内）／外端透明も既存Pan／Restoreで確認する。元の南16部分・地表原画・UV／投影・保存／住人不変の検査は維持。ゲーム本体・原画・歩行・Core／保存／報酬・Merge・撮影コード・QAツールは変更しない。描画digest `a8d5f7372d804161d7d063c524e05abef3685e8e88d44d672b8d5cc306ecf394`、元354承認／JPEG15枚は不変。6asmdefコンパイル0警告エラー・native宣言222・QA前提ゲート確認済み。独立修正レビューCritical0／Important0／Minor0。

**修正版は実装済み・Unity全件再確認待ち・新APK未生成・Pixel未確認。** 受領221成功を修正版222成功へ流用しない。次はGitHub DesktopでfeatureをPullし、既存の全件 `build-android.ps1 -TownQa` を再実行。failed0／skip0・124同版画像を確認した後だけQAAPK／SHAとPixel受入へ。部分テストや検査緩和で通さず、街の品質確認前にMerge／mainへ進まない。記録は `docs/verification/south-entry-mesh-test-repair-2026-10-25.json`。画像をチャットに貼らない。以下は履歴。

**2026-10-24最新：第1段階修正版の全件143/143・222/222とQA APK生成を画面で確認。** run `20261024-220250-ba90ed62`、Unity102画像720x1280、報告SHA `297a7db4b2ab8848d9b4e97cf806563b08c53b62d6d4653d2a91266f1bbe96e8`。記録 `docs/verification/stage1-qa-build-screen-report-2026-10-24.json`。成功XML2件・build-info・APK原本は未受領で、SHA独立照合／新しいPixel実測は未確認。前回221/1原本は履歴として維持。原本未確認を隠さず、現在の画面成功と区別する。

次は生成済み220250APKをそのまま使い、実APKとbuild-info／SHAを照合して `install-pixel3a.ps1 -PackageName com.mofumachi.merge.qa.townlife -Serial 99RAY1BELH` で更新（install-r）。記録更新だけでPullや再ビルドを要求しない。続いて北左右の余白・薄い文字消去・中央四方徒歩往復／タップ／NPC／カメラ／基本ループ／更新保存・再起動／600秒以上の実操作を確認。前版での保存復帰報告を新版の復帰確認へ流用しない。音が小さい／少し熱いという前版FB、S1南石目／J1西東境界は実機で確認。第1段階はまだ合格にしない。新機能・mainはしない。画像をチャットへ貼らない。以下は履歴。

**2026-10-24最新原本：214249のPlayMode222中221成功／1失敗／skip0。旧案内文字テストだけ修正。** 原XML/logとSHAは `References/Review/full-playmode-2026-10-24-214249/`。唯一の失敗はSeamlessFieldTests.OneSharedGroundAndFiveLandmarksReplaceTheOpaquePainting:56の `.Single` が、ユーザー指示で除去済みの `Painted road direction 0` を必須取得していたため。こちらの検査更新漏れ。北カメラ／文字非表示の新CameraKeepsWaterActorsMarkersAndGrowthAlignedはこの実Unityで成功済み。

ゲームは変更せず、SeamlessFieldTestsの旧取得・非表示確認とTownRegionFlowTestsの空になった旧ラベル位置ループを、inactive含む全案内Textの不在確認へ更新。5地区の原画／地表／投影／名前・HUD／操作／保存／納品・報酬の既存検査は維持。Native宣言222件維持、6asmdefコンパイル0警告エラー、描画digest7d1f305a…／354元承認＋許可済み2差分／15画像は不変。原本の221成功を修正版222成功と報告しない。修正版の全件Unity／APK／Pixelは未確認。次はPull後、承認済みの全件 -TownQa を再実行し、failed0/skip0後だけ同版APK/SHA／Pixelへ。画像をチャットに貼らない。以下は履歴。

**2026-10-24最新受信：Windows214249は承認ゲート通過・Edit143/143成功、PlayModeでUnity exit2。** run `20261024-214249-2904cc95` のユーザー画面。前の承認SHA誤停止とは別段階で、PS142はUnity非0終了通知のみ。実PlayMode失敗名／件数／skip／根因は画面にない。原XML/log未受領、APK未生成。次はこのrunの `playmode.xml` と `playmode.log` を受領して診断する。推測でカメラ・UI・fixture・判定を変えず、再実行もまだ案内しない。ログの場所は `C:\Users\User\Desktop\Mofumachi\mofumachi-merge\Builds\Android\20261024-214249-2904cc95`。記録 `docs/verification/stage1-stability-2026-10-24.json`。今回の記録更新を理由にPullや再ビルドさせない。以下は履歴。

**2026-10-24最新：Windows全件QA開始前の承認SHA誤停止を修正。** ユーザー画面の `Original base approval was changed.` は新しい照合のraw SHA判定。実Git core.autocrlf=true checkoutで承認JSONのLF→CRLF1897行、rawSHA e70eb3…→aab50c…、JSON内容同一・Git差分0のまま同停止を再現した。`base_lock_hash_mode=lf-text` を候補に明記し、既存入力lock同様にUTF8/LF正規化して元のe70eb3…と照合する。承認スナップショットや記録SHAを差替えず、実内容変更は拒否する。mode省略の古い記録はbinary厳密のまま。証拠は `docs/verification/stage1-windows-base-hash-2026-10-24.json`。

今回追加の変更は検査スクリプト／候補のhash mode／関連テスト・記録だけ。b20d5e8の北カメラ・薄文字修正、原画・ゲーム・保存はそのまま。CRLF回帰RED→GREENと実Git checkoutの照合成功を確認、全13PS成功、403必要ファイルの実Git CRLF checkoutでも全承認ゲート通過。修正版の全件Unity／APK／Pixelは依然未確認。反映後はGitHub DesktopでPullし同じ -TownQa を再実行。ユーザーへ承認JSONの手修正・Git設定変更・照合無効化を案内しない。以下は履歴。

**2026-10-24最優先：リリース優先・第1段階＋実機追加FB。** 現在ベースを維持し、接続／移動／既存ループ／保存／Pixel600秒実試遊だけを進める。新機能・mainは禁止。ユーザーは添付PDF27ページについてPixel3a・今回TownQAと明示し、再起動保存復帰成功を報告。音が小さい／少し熱いとの体感あり、600秒・FPS・音切れ・クラッシュなしは未確認。原本PDFとSHA／範囲は `References/Review/stage1-device-2026-10-24/receipt.json`。画像をチャットに貼らない。

今回の修正は2ファイルだけ：TownWorldLayoutの北カメラ横範囲（左右空白へのPanを制限、南の既存方式を適用）とTownContinuousWorldViewの浮いた4方向案内Textの生成・更新の除去。後者はユーザーの追加FB「通常プレイ画面では表示されないよう修正」の明示許可。地区HUD・名前札・会話・原画・密度・Walkable Area・NPC／選択キャラ動作・保存・報酬・Merge・BGMを変えない。選択キャラは待機中に自律散歩せず、指示済みの移動と短いドラッグ減速はある。動作変更は優先修正後の確認に回す。

**元の354承認／15画像は不変。** `stage1-display-candidate-2026-10-24.json` に2ファイルの旧→新SHAだけを別記し、他352入力を固定。旧実画像には旧SHAで再構成したdigestを照合し、新撮影／APKには実際の新digestを使う。実画像承認を修正版へ付け替えない。北の回帰はRED3/4後CPU全306/306、6asmdefコンパイル0警告エラー。ただし修正版Unity／APK／Pixelは未確認。手順／進行は `docs/superpowers/plans/2026-10-24-stage1-stability.md` と `docs/verification/stage1-stability-2026-10-24.json`。

185942APKの旧成功は保持するが、この実変更後はPull→全件 -TownQa→新APK/SHA→install-r→実機が必要（記録更新だけなら再ビルド不要の原則は維持）。南石目S1／西東材質・下端J1は原画境界と重ね合わせの軽微課題として実機比較へ保持し、景観を推測で作り直さない。第1段階は全条件の実結果が揃うまで合格にしない。最新指示の報告は接続／5地区移動／コアループ／10分／重大／軽微／合否の7項目に限定。以下は履歴。

**2026-10-24最新画面報告：全件143/143・222/222とQA APK生成完了。次はPixel更新。** run20261024-185942-acab69f4のユーザーPowerShell画面で成功を確認。102Unity画像720x1280、town-life-qa.apk、報告SHA c82c1f8148daad424ebfb7e988f22a08eb48aa57cfd37f80ad4fe2e80902fc67。記録は `docs/verification/five-area-qa-build-screen-report-2026-10-24.json`。**成功XML2件・build-info／APK原本は未受領、SHA独立照合は未完了**と区別。画面報告を原XML／Pixel合格へ昇格しない。以前の221/1、217/5原XMLは履歴として保持。

5エリアの品質固定と実機受入許可は継続。生成済み185942APKをそのまま使い、記録だけの更新で再ビルドさせない。次はbuild-infoと実APKのSHA／街QA IDを照合し、既知のPixel99RAY1BELHへ `install-pixel3a.ps1 -PackageName com.mofumachi.merge.qa.townlife -Serial 99RAY1BELH`（install-r）で更新。削除／pmclear／署名失敗回避をしない。実機の既存保存維持は実測するまで未確認。インストール成功後は5エリア徒歩往復・表示・コアループ・終了再起動保存・600秒以上の実試遊／熱／入力／カメラ／NPCを確認。

現在は「Unity全件／APK成功の画面報告確認済み・原本照合待ち・Pixelインストール／実機未確認」。正式ベース採用・mainは未了。ゲーム354入力・承認画像・描画digestは変えない。画像をチャットへ貼らない。以下は履歴で本段を優先。

**2026-10-24最新：全件222中221成功・残1失敗を受領、会話fixtureの同期1フレームを修正。** run20261024-180636-dc09e121の原XML/logは `References/Review/full-playmode-2026-10-24-180636/`。前回5件中ほか4件の成功を実結果で確認、skip0・新規失敗なし。残件TownRegionLifeTestsのfixtureはModel.Restore後にRender更新だけでActor.RegionIdを先に調べ、Luluの所属キャッシュが旧CentralのままWest抽出から外れていた。RegionIdは通常のWorld.Step→ApplyWorldStepで反映される。前回検査準備の見落としを認め、通常1フレームを加えてから既存の2名・挨拶・停止・再開の検査を行う。判定や人数を緩めない。

**変更はテスト1行＋説明のみ。ゲーム354入力・承認画像・描画digestは不変。** PlayModeDLLコンパイル0警告／0エラー、宣言222件とQA承認ゲートを確認。修正版の実Unity全件成功は未確認なので、次はPull後に全件 -TownQa を再実行。XML failed0/skip0後だけAPKへ。前回302CPUや221成功を222成功と報告しない。5エリアは承認済み、Pixel受入許可継続、正式ベース／APK／Pixel／main未完了。画像をチャットへ貼らない。以下は履歴で本段を優先。

**2026-10-24最新：全件PlayModeの実失敗5件を受領し、検査4ファイルだけ修正。** run20261024-171445-86b77f6eは222件中217成功・5失敗・skip0、Edit143/143は画面報告（原XML未受領）。原本／原因は `References/Review/full-playmode-2026-10-24-171445/`。Windowsの終了code2はこの5テスト失敗で、PS142行は通知。クラウドlicense198と混同しない。

失敗前提：①Westを映して実際にはCentralにいるLuluをタップ、②成長プレビューのbest-fit世界看板まで画面本文の高さ検査、③全小物が単一VillageAtlasという旧前提、④旧3地区人口2/3/1（現5地区は中央2＋四方各1、legacy1/4/1）、⑤WestにNPCが初期2名という旧会話fixture（現1名）。実在Luluを映す／画面本文と縮小世界看板を分ける／全Pieceの素材別atlas紐付け／5地区6名とlegacy対応／会話のみを狙う2NPC明示fixtureへ更新。納品・再会・報酬一度・保存・挨拶・停止復帰などの確認は保持。試験fixtureの配置を歩行到着証拠としない。

**ゲーム本体・原画354件と描画digestは不変、Native222件維持。** CPU302/302・6DLLコンパイル0警告エラー・全件QA入口の検査は通過、独立テストレビュー重大指摘なし。ただし修正版の実Unity222成功は未確認。次はPull後に同じ全件 `build-android.ps1 -TownQa` を実行し、failed0/skip0後だけAPK/SHAへ。対象16だけの成功に置き換えない。5地区承認とPixel受入の許可は継続、追加確認不要。実機未確認・正式採用未了・main別承認。画像をチャットへ貼らない。以下は履歴で本段を優先。

**2026-10-24最優先：ユーザーが638aa24の5エリア統合候補をUnity実画面ベースで明示承認。追加確認不要で受入へ。** 中央・西・東・北・南の役割／色調／密度／Lv1/3/6／南庭園・中央接続を固定。`References/VisualLock/five-area-approved-2026-10-24.json` は実承認・JPEG15枚・承認済みruntime/Core/Resources/Scenesの354ファイルSHAを束縛し、既存全件QA入口で検査する。ゲームコード・原画・保存・報酬・Merge・BGM・キャラ・新機能は変更しない。悪化時ロールバック。軽微S1南石目のぼけ／J1西東材質境界は実機確認へ持越し、これだけで停止・再制作しない。

次の順序は全件UnityEditMode/PlayMode→failed0/skip0→同版TownQAAPK→SHA256→Pixel3a更新install-r→全5地区の徒歩往復／表示／カメラ／NPC→TOP・依頼・Merge・納品・報酬・街成長・図鑑→終了再起動保存復帰→600秒以上の連続実試遊。現在宣言数143EditMode/222PlayModeは棚卸しで、実成功件数ではない。南の旧条件付き45f識別子／JPEGは履歴と順序の証拠として保持し、最新638aa24品質の明示承認を別に関連付けた。古い実画像・16対象ケースを全件／端末成功に流用しない。

**現在：5地区Unity画像承認済み・全件／QAAPK／Pixel受入は未確認。** 実際の -TownQa 起動（870518a／20261024-151246-89bf47bc、Asia/Tokyo）は承認／354入力／sourceゲート通過後、最初の全件EditModeを開始する前にUnityライセンス不足でexit198。XML・APKは生成されなかった。ゲームテストのfailed0/skip0成功結果ではなく、実行前の環境停止として記録する。 クラウドにはAndroidPlayerモジュール・ADB・PixelUSBがない。Windowsの既存 `build-android.ps1 -TownQa`／`install-pixel3a.ps1`／`capture-pixel-visual-check.ps1` を再利用し、新しいゲーム機能・試遊自動化装置を増やさない。手順 `docs/five-area-pixel3a-acceptance-2026-10-24.md`、結果 `docs/verification/five-area-pixel3a-acceptance-2026-10-24.json`。必要なハード実行以外は進め、同じ承認を再要求しない。保存消去・アンインストール・署名不一致回避は禁止。全合格後にのみ正式ベース候補、Pixel未確認を完成としない。main統合は別途承認のまま。画像をチャットへ貼らない。以下は履歴で本段を優先。

**2026-10-24最新受領：5エリア638aa24は実Unityの統合承認候補。ユーザー承認待ち。** repackされた5ZIPの全67原本SHA、manifest05b7cfc96030cb33473ffe64fa17aeae3304918592b9321a4f68564837c0fb11、27新版PNG・Before15・5地区6経路・実XMLを照合。撮影638aa24／run20261024-110359-39b38515は南Finalと同一。対象16/16は同じ既存1回で、再包装で成功件数を加算しない。原本・判定は `References/Review/five-area-2026-10-24-revised-candidate/`。15同寸JPEG／中央互換記録は原本からの派生で、新撮影ではない。

中央・西・東・北21枚中20枚は前版と同一。中央Lv1の差2651pixelは噴水水面61／モモ1352／ルル1238のみ、景観・配置・HUD・名前位置は不変。南6枚は先の南Finalと同一。役割・色調・密度・Lv1/3/6・接続・導線を実画像で確認し、独立レビューもCritical0 Important0。軽微S1南入口の石目の柔らかい重なり、J1西・東の既存材質／石目切替は持越し。全てのテクスチャ継ぎ目が消えたとは報告しない。ゲームC#・原画・歩行面・正式キャラ・保存・報酬・Merge・BGMは今回無変更。

**現在：実装済み・Unity実画面確認済み・5地区ユーザー承認待ち・QA APK未生成・Pixel未確認。** 中央の新3原本と互換記録へレビュー参照を更新しただけで、中央のデザイン／実装は変更していない。統合checkpointは新638aa24のJPEG15枚／digestへ束縛し、qualityはAI確認済みだがuser_approved=false／approved_by=nullを維持。南の旧45f条件付き承認レコードを新画像のユーザー承認へ付け替えない。ユーザー指定「5エリア統合レビュー承認前に進まない」に従い、現在候補を提示して承認確認を行う。明示承認後にのみ全件Unity→QA APK→Pixelへ。main統合は依然別途確認。画像をチャットへ貼らない。以下は履歴で本段を優先。

**2026-10-24最新受領：南638aa24は実Unity確認済み・5エリア統合レビューへ進行可。** run20261024-110359-39b38515のSouthFinal2ZIPを照合。28原本／南の新版PNG6枚／旧PNG10枚／正式Lock／比較JPEG2枚・導線JPEG1枚／同条件カメラ／6経路SHAを確認。実Unity16/16は1回計上。曲線小道・花の芝／円形植栽で庭園構成が復元し、Lv3花と灯り／Lv6アーチ／東屋／休憩名を維持。旧F1密度・F2直線継ぎ目は解消。独立実画像レビューもCritical0 Important0で進行可。軽微S1＝接続の石目に局所的な柔らかい重なり（connection x240–500/y650–780、walking x380–620/y735–855）は統合レビューへ持越し、南を再制作しない。

原本・判定は `References/Review/south-final-2026-10-24-candidate/`。**南の状態は「実装済み・Unity実画面確認済み・Pixel未確認」。** ゲームC#・原画は今回変更していない。中央／西／東／北の承認レコードと固定37入力は不変。今回受領した他4地区は21枚のSHA報告だけ（20同一・中央Lv1のみ差）で、新実PNG21枚そのものは未受領。中央Lv1差分を待機位相だけと断定せず、同じrunからrepackで出す実統合原本を確認する。

次は既存 `scripts/repack-five-area-review.ps1 -RunDirectory .\Builds\FiveAreaReview\20261024-110359-39b38515` で保存済み同撮影をZIP化し、5地区を再審査する。新規撮影を繰り返さない。撮影版638aa24と現在の記録／包装版を混同しない。南の進行可判定はユーザーの新版画像承認・5地区承認ではない。統合user_approved=false、採用・全件Unity・QA APK・Pixel・mainは未実施／閉じたまま。画像をチャットへ貼らない。以下は履歴で本段を優先。

**2026-10-24最新実装：ユーザーがHOLD判定を承認し、南の2点だけ修正を許可。**「南エリア最終修正 → 5エリア統合再審査」を最優先。中央／西／東／北は変更禁止。南の地表を広い石床から曲線の小道・低い歩ける芝／小花・円形の休憩構成へ戻す。大きな障害物を通れる場所へ描かず、歩行面・共有カメラ・ナビ・保存・報酬・Merge・BGM・正式キャラには触れない。東屋・良かったLv3装飾・Lv6花アーチは維持。

接続は南の地表ノード配下の `TownSouthEntryBlendGraphic` が担当。y=-1の境界で既存中央の底辺と同一UV・不透明度1、南側だけで反射サンプルを下方／側方へ減衰する。中央の画像・メッシュは変更せず、北側には描かない。追加テクスチャはなく既存中央Resourceを共有する。37個の固定ファイルは6b1da82のSHAと一致し、南の既存Dressingも不変。

**新版は実装済み・新版Unity実画面／実機未確認。** ソース素材の検討やDLL／CPU／PS成功を見た目の合格としない。`-SouthFinalReview` は既存の対象16ネイティブ検査・同条件撮影を使い、南のBefore45f205d／保留d8e649b／修正版を3列で提出する。接続／歩行比較・導線・原本を `south-final-review-*.zip` に保存。比較に使う10枚の旧原本も各1回同梱する。新しい統合ZIPはこの時点では出さず、全5地区の同一撮影データだけをステージングする。南の実画面確認後、既存repackコマンドで同じ撮影から5地区再審査に進める。

全件Unityテスト・QA APK・Pixelは5地区合格後。main統合は引き続き別途確認。画像をチャットへ貼らない。新規プレビュー／生成素材を実Unity画面として出さない。今回の5地区採用／ユーザー画像承認は未取得。手順は `docs/south-final-review-steps.txt`。以下のd8実Unity成功／HOLDは旧Candidateの記録で、新版の成功ではない。

**2026-10-24最新受領：5エリア実Unityは16/16、統合候補は南2点でHOLD。** d8e649b／20261023-225834-ea7261fa、描画digest b975adbb3212a75bafe5948705a1b30c46c67921875168c455c418d21623cd0aの分割5ZIPを照合。原本67ファイル・実PNG27・前回45f205d Before15・Lv比較JPEG3・5地区導線のSHA／同条件カメラ／正式VisualLockを確認。16テストは1回計上し、全5地区歩行・分離・南各休憩地点も実Unityで成功。原本・手動判定は `References/Review/five-area-2026-10-24-candidate/`。包装処理の修正版へ撮影版を付け替えない。

中央／西／東／北の景観・成長・役割は維持。西／東／北の全18枚、中央Lv3／6の2枚は前版と同一。中央Lv1は噴水水面／キャラの待機表情等の位相差で配置は同じ。南のLv3花／灯り強化、Lv6アーチ、休憩時のキャラと名前、左側の裸芝生／垂直境界の解消は実画像で確認。

**ただし南の新地表は採用しない。** F1 Important＝植栽・曲線小道・円形の石と花の構成が減り、広い反復石床となって庭園／休憩の密度が後退。F2 Important＝south-walking y約710、south-connection y約637に直線の材質切断／平らな緑の肩／縮尺の違う中央石帯が残る。独立実画像レビューもHOLD。Lv3強化や機能成功を理由にこの後退を合格にしない。次の修正範囲は南の庭園密度／構成と接続部だけ。良い4地区・東屋・Lv3強化・Lv6アーチ・歩行面積を維持し、石床を増やして解決しない。

**今回の作業は受領・実画像レビューであり、C#・原画は変更していない。** 状態は「実装済み・Unity実画面確認済み・南の見た目未合格・実機未確認」。15枚の実PNG由来同寸JPEGを登録したが統合user_approved=false。南の旧条件付き承認は保持し、新版の画像承認へ拡大しない。統合採用／全体完成／APK／Pixel最終受入／mainへ進めない。同じ未修正ソースの再撮影は依頼しない。画像はチャットへ貼らない。以下は過去版の履歴で本段を優先する。

**2026-10-23 FiveAreaReviewのZIP停止修正：** ユーザー画面ではUnity16/16を通過後、`[IO.Compression.ZipArchiveMode]` 未解決で分割ZIP作成が停止。Windows PowerShell5.1用に `System.IO.Compression` 本体と `System.IO.Compression.FileSystem` を明示ロードする（旧版は後者のみ）。.NET4.7参照DLLでもModeは本体、ZipFile／ExtensionsはFileSystemに定義されると確認。C#・原画・描画入力は変更しない。

撮影を繰り返さず `scripts/repack-five-area-review.ps1` で直近runの完成済み `five-area-review/review-manifest.json` と全原本SHA、元の3つの版情報、現在の描画digest、5地区27PNG／Before15／5経路／16XMLを検査して、新しい `repacked/<日時>` にZIPだけ作る。元の撮影版d8e649bを現在の包装コード版へ付け替えない。スクリプトだけの修正では描画digestが同じなら元の撮影を使え、描画入力が変わった場合は拒否する。承認・APK・Pixel・mainは進めない。画面上の16/16は受領原本で未照合で、景観合格ではない。画像をチャットへ貼らない。

**2026-10-23最新：南はユーザー条件付き承認、限定4点後の5エリア統合レビューを許可。** 最新ユーザー「南エリア最終調整 → 5エリア統合確認」を最優先とする。旧45f205dのHOLDは実画像の履歴として保持し、南を追加承認待ちで止めない。南の庭園／休憩、Lv3差、花壇／ベンチ／左右／東屋の回遊、空白感だけを調整する。東屋・Lv6花アーチと中央／西／東／北は維持。

実装：南専用の原画テイスト地表（西の原画は無変更）と周縁の透過、南だけのカメラ範囲、既存予約地の鉢／灯り／Lv3花、花壇前を通る6回遊経路。歩行面・当たり判定・報酬・保存・正式キャラ・Mergeは変更しない。条件記録 `docs/verification/south-final-conditions-2026-10-23.json` は実装と固定ファイルのSHAを束縛する。新画像の品質合格を意味しない。

次は `-FiveAreaReview` の非APK実Unity撮影。16検査／実PNG27／前回45f205dの同条件Before15／Lv別比較JPEG3／実投影導線JPEG5／役割説明を `five-area-review-*.zip` の分割一式に出す。XML／ログは1回だけ保存・計上。従来の5ZIPの重複証拠を新しい統合レビューと呼ばない。旧PNGを新しい版の実画面として再利用しない。

**新版は実装済み・実Unity実画面未確認・実機未確認。** クラウドにEditorライセンスがないためCPU／DLL／PS検査をUnity画面合格としない。南の条件付き承認は統合レビュー作成のみを許可し、未確認の画像を承認したことにはしない。統合レビューのユーザー承認までAPK／Pixel最終受入／main統合／大規模追加は禁止。画像はチャットへ貼らない。手順は `docs/five-area-review-steps.txt`。以下は各過去版の履歴で、最新ユーザー指示と本段を優先。

**2026-10-23最新受領：南Candidateは実Unity確認済み・品質HOLD。** 45f205d／20261023-195457-32c20c13の同run5ZIPを照合。描画digest e4cb869ca5d32b4d0ecc3014a86468aa9e39774deb3ef64f57f1c753f0dc4dda、実Unity15/15を1回計上（5XMLを75件と数えない）、実PNG27・原本101ファイル・固定Before15・旧北3・比較JPEG5・北南6経路注釈／SHAを確認。原本と判定は `References/Review/south-garden-2026-10-23-candidate/native-review.json`。中央／西／東15枚中14枚は前版と同一、中央Lv1のモモ表情だけ差。北6枚中4枚同一、Lv3／6は掲示板の控えめな花／灯りのみ追加。北の条件付き承認2点は実Unityでも検証でき、正式採用候補として固定する（新たな無条件ユーザー画像承認を推測しない）。

**南は採用しない。** 景観はBeforeより大幅改善し、Lv1から東屋・花・席・木陰・広い石畳があるが、独立実画像レビューもHOLD：Important2＝①south-rest-arrivedのx約163の垂直切断／左の裸芝生、south-walkingのy約711の水平継ぎ目。②歩行中モモの足が密な植栽上に見え、上側回遊注釈も花／岩の床を横切る。Minor1＝Lv1→3の中間成長が弱い。これらは実際の南歩行／休憩で見えるため「統合工程で後回し」として南を合格にしない。**南は実装・Unity確認済み、見た目HOLD、ユーザー承認なし、Pixel未確認。** 南／統合のユーザー承認はfalse、5エリア統合・APK・Pixel・mainは未着手。コードや素材はこの受領レビューでは変更していない。次の修正対象は南の地面境界・描いた道と通行面の一致・中間成長だけ。中央／西／東／北を維持し、同じ未修正ソースの再撮影を依頼しない。画像はチャットへ貼らず、実JPEGの既存feature HTTPSリンクで案内する。

**2026-10-23 SouthReviewのCount停止修正：** UTF8修正c1ac0a1の次に、北承認ゲートで対象1地区が配列から文字列へ展開され、productionの `Set-StrictMode -Version Latest` 下で `.Count` が停止した。`Assert-TownCheckpointApproved` の対象地区を `[string[]]` で固定し、地区／Lv／画像数／SHAの要件は維持する。従来のヘルパー単体検査は厳格モードを揃えていなかった。日本語再現検査は実build入口をドット読み込みして依存関数／厳格モードを合わせ、承認・South出力検査も厳格モードを明示。関連10スイートをすべてLatestで実行して成功。記録は `docs/verification/south-strict-count-2026-10-23.json`。承認データ・C#・原画・描画入力は不変。南の実Unity画像／Pixelは未確認であり、5エリア統合・APK・mainは閉じたまま。

**2026-10-23 SouthReview起動前のJSON修正：** 39431b0でWindows PowerShellの既定CP932により、BOMなしUTF8の日本語承認記録を誤読してConvertFrom-Jsonが停止。実DotNet CP932＋実JSON解析で再現し、レビュー／ゲート6スクリプト39箇所のJSON読み込みを `-Encoding UTF8` で固定した。承認記録や画像原本は書き換えない。`scripts/tests/windows-json-encoding.tests.ps1` は誤読環境を再現して実South前提ゲートの通過と、未承認の南／統合を引き続き拒否することを検査する。関連10スイート成功。C#／描画入力は不変、Unity再撮影・APK・Pixelは未実行。Pull後は同じ `-SouthReview` を再実行する。正式状態は次段の北条件付き承認・南候補未実画面確認のまま。

**2026-10-23 最新：北条件付き承認＋限定2点後の南着手をユーザーが許可。**「北エリア最終調整 → 南エリア着手指示」を優先する。ed3c5e4の北実画像は条件付き承認。北は小屋・郵便受け・掲示板・荷物・封筒モザイク・花壇・Lv6花アーチ・色調を固定し、横方向回遊／3体同時歩行の検証・導線図修正と、掲示板側の控えめなLv3花／ランタンだけ調整する。既存の面ナビには入口一点を必須にする仕様はなく、旧図が全経路を入口始点で描いていた。新しい6経路は小屋↔左右、左右↔待合、モザイク横断／回遊を実ナビで計算する。NorthMasterTests15/15（Lv1/3/6で3体全員到着・足の分離を含む）を確認し、限定2点実装後の南着手を今回のユーザー発言に基づき許可した。条件記録は `docs/verification/north-final-conditions-2026-10-23.json`。**調整後の北はUnity再確認待ち**であり、旧画像の成功を新版へ流用しない。

南は庭園・散歩・休憩。既存原画の東屋／ベンチ／花壇／木陰／曲線小道／鉢／休憩ポイント、Lv1完成・Lv3賑やか・Lv6華やかを実装する。中央・西・東・北の品質を下げない。南のLv1/3/6、Before/Candidate、実ナビの導線JPEGを非APKのSouthReviewで提出し、同じソースの他4地区は後退確認用に撮影する。これは5エリア統合着手や承認ではない。**南のユーザー画像確認が終わるまで5エリア統合・Pixel・main統合は禁止**。QA APKも保留。旧「北の追加承認待ちで南を止める」記述より今回の条件付き着手許可を優先する。各新版の実Unity状態は未確認へ戻す。

南の候補ソースを実装済み。元の東屋・ベンチ・木陰を維持し、共有の石畳／植栽の地表、初期から完成した休憩席、低い密な花壇、灯り、Lv3の花／鉢、Lv6の東屋花アーチを追加。前景花壇は密度を減らさず下へ配置して休憩場所の名前との重なりを回避。左右／東屋／休憩場所を結ぶ6経路、3体の往来、初期と成長席の置換、名前の前景重なりを検査する。**南は実装済み・Unity実画面未確認・Pixel未確認**。`-SouthReview` は非APKの15ネイティブケース／27PNG／同run5ZIP（中央・西・東・北は後退確認用）。南Lv1/3/6・Before/Candidate・実ナビ導線のJPEGを生成する。手順は `docs/south-review-steps.txt`。ライセンス不足のクラウドでは新しい実画像を作れないため、旧実画像を新版の証拠として再提出しない。以下の北承認待ち・南未着手という記述は過去版の記録で、上段の条件付き承認と今回の状態を優先する。

**2026-10-23 最新：北のモザイク舗装版は実Unity確認済み・ユーザー画像承認待ち。** ed3c5e4／20261023-182917-c360cabd、描画digest a57be9d4a85d420dc3ce5d32b50266289a04f290c05ddbf5efb1e8aaca29ae12の4ZIPを受領。実Unity12/12を1回計上、実PNG21枚・原本78ファイル・Before12・旧Candidate3・参照／比較／導線のSHAを照合。`References/Review/north-quest-2026-10-23-mosaic-candidate/native-review.json` に保存。北の細線状の前庭は手紙と花の石モザイクへ置換され、実画像で質感と明瞭さを確認。Lv3展示／旗、Lv6花入口、歩行／名前／入口を確認。独立実画像レビューはユーザー確認提出可能、Critical0／Important0。中央・西・東は前回15枚中14枚同一、中央Lv1の姿勢だけ差があり景観／HUD保持。

**現在は北の承認を待つ。新しい修正・再撮影・南制作へ勝手に進まない。** `north/north-lv1.jpg`／`north-lv3.jpg`／`north-lv6.jpg` は実PNGを同寸JPEGへ変換したもので、合成図ではない。3枚のSHAと描画入力をuser_review_checkpoints.north_questへ登録したが、user_approved=false。画像ZIPの提出やテスト成功は承認ではない。ユーザーが北候補を明示承認して初めて南へ。南／APK／Pixelは未了で、全体完成ではない。チャットへ画像を貼らず、既存featureのGitHub HTTPS `.jpg` リンクで案内する。

以下の未実行／HOLDは各過去版の記録であり、現状の再作業指示にしない。現在のネイティブ証拠は上段。今はライセンス再試行も追加の283件再実行も不要。次は実画像のユーザー判断。

**2026-10-23 最新受領・北の舗装仕上げ：** 5cbc8bd／20261023-110111-e9d5ab26の4ZIPを受領。実Unity12/12（4XMLを重複計上しない）、実PNG21枚、Before12・旧Candidate3・JPEG4組・導線JSON/JPEGを照合し、原本78ファイルを `References/Review/north-quest-2026-10-23-quality-held/` に保存。撮影診断のログ衝突と直接描画の同期はこの実行で確認済み。中央・西・東は同一8枚と変更7枚を比較し重要な後退なし。北のLv3展示／旗、Lv6花入口も実画像で確認。ただし **北前庭の細線模様が設計ガイド状に見えるため、独立実画像レビューともHOLD**。テスト成功で採用せず、南へ進まない。

現在の修正は **北の細線模様だけを、手紙と花の平らな石モザイクへ置換**。共有の最大512×256・非Readable画像1点、4頂点で描画し、既存の花壇・建物・段階装飾・Walkable Area・キャラ・セーブ／報酬を変えない。新PNG/metaを描画入力digestへ含める。これは承認済み北の仕上げ範囲でのCodex判断であり、ユーザーによる新画像承認ではない。**新版は実装済み・Unity実画面／Pixel未確認**。5cbcの成功を新版へ流用しない。次は新ソースの `-NorthReview` 4ZIPを受け、北実画像と既存3地区を比較する。北のユーザー画像承認までは南／APKを閉じる。

以下の「未実行／再検査待ち」は各過去ソース時点の記録。最新状態は上段と `docs/verification/north-court-finish-2026-10-23.json` を優先する。

**2026-10-23 撮影検査の停止修正：** 受領した20261023-104540-3a733aeaのXMLは12件中11成功・撮影1失敗。北の歩行／ドア／装飾3件は成功したが、Captureの意図的な `Debug.Log(MOFUMACHI_CAPTURE_WORLD...)` が `LogAssert.NoUnexpectedReceived` に抵触した。座標診断をテスト専用 `CaptureWorldTrace.Append` から各地区の `capture-world.txt` へ保存する実装へ変更。通常の警告／エラー検査・画像／座標／保存検査・期待12件は維持し、無視設定で通さない。原本は `References/Review/north-quest-2026-10-23-capture-failure/`。この受領分には版のmetadataと実PNGがなく、実画面合格や新ソースの成功として使わない。修正版はUnity再検査・実画像・北承認待ち。南／APKは保留。

**2026-10-23 最新指示：北Candidateはユーザーにより本採用不可。** 以前のCodexレビューの「北景観PASS」をユーザー承認や現品質合格に使わない。依頼小屋の主役感、お願い・届け物拠点としての独自性、空白感、Lv1→3→6差、すれ違い／入口待機を北だけで仕上げる。中央・西・東の固定品質は維持。南・Merge等へ広げない。提出は北のLv1／3／6についてBefore／受領済み未採用Candidate／修正版Candidate、および歩行導線JPEG。正式Visual Lockも原本を添える。修正版を実Unityで確認し、ユーザー承認後に初めて南へ進む。

今回の北調整：小屋脇の届け物展示と花、小屋と木の間の吊り旗、Lv6の花入口、原画の木札内の「おねがい」表記、通れる石畳の手紙／花の象嵌。既存原画アトラスと、元絵に合わせた独立の荷物素材1点（Unity最大256、共有・非Readable）を用いる。元アトラス・正式キャラ原画は変更せず、新規システム・衝突基礎も追加しない。Lv3で届け物展示を置換し、Lv6で入口のシルエットを変える。旧Candidateの実PNG3枚は `References/VisualLock/north-revision-baseline-2026-10-23.json` のSHAで固定して比較に使う。導線JPEGは同じLv1実撮影のカメラから投影したナビ経路の注釈で、実操作軌跡と偽らない。素の実PNGも保持する。**実装済み・修正版Unity実画面／Pixel未確認。** 接続描画同期修正も同梱し、それだけの小出し確認へ戻さない。

最新の最上位方針は **2026-10-23「北エリア最終調整・南着手前の品質固定指示」** と **2026-10-22「品質固定・5エリア完成指示」**。現在の中央・西・東Candidateを横展開の最低品質として固定し、新しい方向へ作り直さない。以下と後続のユーザー指示は、古い設計書・引き継ぎ・案より優先する。

提示した [中央広場／Mergeの事前画像](References/Preview/design-first-2026-10-21/design-proposal.png) は、ユーザーが **正式Visual Lock** として承認した。9月22日案は補助基準。高密度の花・木・建物・灯り・座席と十分な歩行面、自然な噴水まわりの道、明るい平面5×6・大きな原画・肉球の空マス・明確な選択／Lv.UPを品質の下限とする。承認画像はデザイン基準であって、実装・Unity・Pixelの合格証拠ではない。

**順序を固定する：中央・西・東Candidate固定 → 北のUnity確認・調整 → 北候補の画像確認とユーザー承認 → 南庭制作・Unity確認 → 南候補の画像確認とユーザー承認 → 5地区統合・接続確認 → 統合候補の画像確認とユーザー承認 → Unity全件 → QA APK → Pixel受入。** 各画像確認はLv1／3／6を同条件のBefore／Candidate／Visual Lock参照でJPEG提出する。北が実画像・ユーザー承認の両方を通るまで南を作り込まない。南承認前の統合作り込み、統合承認前のAPKも進めない。今回の5地区品質固定後にMerge仕上げ・本格2.5D・デイリー強化へ。旧方針の「Mergeを再設計してから5地区APK」を今回の前提にしない。まず5地区。9地区、CITY、ガチャ、時間帯、季節、新キャラ、新Merge系統、ショップ拡張、本格2.5Dは今回実装しない。

中央・西・東で固定するのは、97aac99の実Unity確認Candidate（20261022-193224-39bef67c）の密度・花壇・木・ベンチ・街灯・建物・石畳・歩行面・UI量・色調・キャラサイズ。正式Approved Visual Lockは引き続き最上位。共通化や歩行面確保を理由にこれらを簡素化しない。原本は `References/Review/east-florist-2026-10-22-accepted/` に保持し、今回のユーザー指示を品質基準固定の承認として扱う。北／南／統合の承認は未取得であり、包括承認から推測しない。

3段階の承認は `References/VisualLock/stage-acceptance-2026-10-21.json` の `user_review_checkpoints` に、実画像のSHA・同条件Lv・描画入力・承認元ユーザー発言を記録する。自動撮影やCodexの判定で `user_approved` をtrueにしない。`Assert-TownCheckpointApproved` は次工程着手時とQA前に使い、欠落／不承認／画像変更／品質軸未合格／古い統合入力を拒否する。以前の承認は工程順の証拠、統合承認は現在版の描画入力に対応させる。5地区完成候補が揃ってもPixel操作・歩行・接続・重さ・発熱・NPC詰まり・UI・保存確認までは完成扱いにしない。

**Lv1＝すでにかわいい、Lv3＝賑やか、Lv6＝華やか、高Lv＝特別感。** 成長予約地を裸の芝生にしない。初期から小花壇・鉢・草花・席などで仕上げ、成長で置換・追加する。Walkable Area確保を理由に景観を削らず、基礎・柵・植栽等と衝突を対応させる。

TOP、正式6名、ロゴ、図鑑基本構造、BGM選択、保存、依頼／納品／報酬、5×6、今日のお願い／もふまちは維持。従来153凍結入力に加え、正式承認のPNG／JPEGのSHAを `References/VisualLock/approved-design-2026-10-21.json` で固定。基準を書き換えて検査を通さない。

No Regression Gateは見た目／操作／情報／世界観／街密度／Mergeの楽しさ／FPS／Pixel負荷。悪化した候補は採用せず、良かった実証済み版へ戻して再設計。手間や新しさを採用理由にしない。大きな変更は同条件のBefore／Afterを必須とする。

中央・西カフェの前版は **475671f／実行20261022-182106-d5e104a3のUnity工程を通過**。[実画像・SHA・判定](References/Review/west-cafe-2026-10-22-accepted/native-review.json) を保持。中央3＋西6の実PNGと固定Before6組／承認原本／比較JPEG、当時の同ソースと描画入力digest226dfeb1…を照合。ネイティブ6/6（中央2＋西3＋撮影1、両ZIPに同じXMLがあるだけで合計12件ではない）。独立の実画像レビューで中央／西PASS・重大／重要0。カフェの外観・徒歩・到着を隠していた自動会話と常設看板を整理し、Lv1／Lv3の中央発→徒歩→到着待ち→再タップ→カフェ表示が実Editorで成功した。検査準備の位置引継ぎミスも修正済み。生の撮影時未採用JSONは改変せず、手動判定を別記。**ベース全体／APK／Pixel合格ではない。**

中央・西・東は **97aac99／20261022-193224-39bef67cのUnity工程を通過**。[原本・SHA・判定](References/Review/east-florist-2026-10-22-accepted/native-review.json) を保存。実PNG15枚・固定Before9組・承認原本／JPEG・同一ソース／描画入力5e419d03…を照合。ネイティブ9/9（3ZIPの同じXMLを27件とは数えない）。独立実画像レビューは変更5枚を確認し、同一10枚をバイト照合して前回判定を維持。フィオの名前はLv1／Lv3／Lv6すべてで完全に読め、前回の成長ベンチによる隠れは解消。中央・西・東に新たな重大／重要な後退なし。**ベース全体・APK・Pixelの合格ではない。**

2848782で東をHOLDにした記録も[別保存](References/Review/east-florist-2026-10-22/native-review.json)。実テスト成功だけで表示の後退を通さず、純粋テストで再現→配置修正→実Unityで再確認した。初期／成長ベンチは同じ側面でLv3置換。原画・レベル条件／速度・保存・納品・報酬は維持。

現在は北の依頼小屋を実装済み・実Unity／Pixel未確認。既存の小屋・掲示板・依頼進行を維持し、側面のベンチ・灯り・段階置換の花壇・低い原画の桜で前庭を整えた。Lv6の花が掲示板を隠す重なりを抑え、左右の小道と中央への徒歩経路を維持。遠方では小屋の原画を見せ、近接／移動／到着時に入口案内を表示する。共有地表は既存Texture2Dで新規大型テクスチャなし。保存・納品・報酬・レベル条件／速度は変更しない。

**2026-10-23受領分：fabf00e／20261023-094203-35c07a1fは同一ソースの実Unity12/12、実PNG21枚、Before12組、参照／比較JPEG4組を照合。** [原本と判定](References/Review/north-quest-2026-10-23-candidate/native-review.json) を保存。独立実画像レビューは北6枚・Before3枚・正式Lock・既存地区の変更5枚を確認し、残り10枚は固定Candidateと同一。中央／西／東PASS、北の景観は基準相当だが接続画像のキャラ表示が重なり **北HOLD**。レビュー原文の「誘導中／顔の大半」は後に言い過ぎと修正され、静止画だけで制御対象や継続的なNPC衝突を断定しない。原文と補足を別保存する。

原因追跡：WorldのRestore／カメラRefresh直後に、背景・カリングだけが更新され、キャラのRectTransform／名前／奥行きは次の描画フレーム待ちとなる経路。接続撮影はフレーム待ちなしだった。既に算出した投影位置を即時反映し、直接撮影前にモデルを進めない描画更新を実装。NPC経路・間隔・景観・原画は変更しない。既存北ケースへ即時Pan／Restore投影、描画更新時のモデル座標・保存非変更の検査を追加。撮影ログの `MOFUMACHI_CAPTURE_WORLD` は世界位置と表示位置の照合用。**修正後の実Unity検査は未実行**（クラウド再試行もライセンス198）。受領12件成功を修正後の成功に流用しない。北は実装済み・修正版Unity／Pixel未確認、ユーザー承認未取得のまま。南へ進めず、再度 `-NorthReview` の同一run4ZIPで確認する。APKは作らない。

`build-android.ps1 -NorthReview` は中央3＋西6＋東6＋北6の実PNG21枚、固定Before12組、ネイティブ12件（中央2＋西3＋東3＋北3＋撮影1）、4ZIPを同じソースで出す。北のLv1／3／6・接続・徒歩・到着、両側の歩行／ドアの再タップ／既存依頼画面／保存非変更を検査する非APK工程。北の比較JPEGはBefore／Candidate／中央の正式Visual Lockデザイン参照の3列（中央参考を北実画面と表示しない）。以前の中央／西／東15枚合格を次工程着手の証拠として検査するが、共有入力を変えた現在版の合格とは扱わない。北を含む新実画像の判定まで現在の3地区合格フラグは閉じ、北候補のユーザー承認を得てから南へ。

`build-android.ps1 -EastReview` は中央3＋西6＋東6の15実PNG・ネイティブ9件・3ZIPを出す内部検査。名前と前景ベンチの実描画範囲も検査する。この単独モードでは北を採用しない。北以降とQA APK／Pixelは未了。画像はチャットへ表示しない。

`-CentralReview`／`-WestReview` は引き続き単独の内部検査に利用可能。通常QA APKは後続工程未了のため閉じたまま。西で持越した接続部の花による主人公の隠れ・舗装の反復・到着案内の部分的な重なりは接続／UI工程へ。ユーザーのLv差・成長間隔／装飾枯渇・成長説明ページは後工程。保存／納品／報酬／正式原画・凍結153入力は維持する。実Beforeはbd3f357／20261021-113845-fbcafb0d。

`build-android.ps1 -CentralReview` は中央Lv1／3／6の実Unity3枚、固定Beforeと正式Lockを並べるJPEG、未合格記録とZIPを生成し、APKを作らない。中央未合格、証拠改変／欠落、中央入力変更時は通常APK工程を拒否する。中央合格だけでAPKを許可せず、西／東／北／南／接続の完了順と3段階のユーザー画像承認も検査する。Mergeは既存5×6・平面・大きなアイテム・可愛い枠・選択・合成演出を維持し、全件回帰からは外さない。ローカルDLLコンパイル／CPU配置図／素材画像はUnity合格ではない。クラウドは最新の再試行でもUnityライセンス不足（198）、Android Build Support・Pixel USB経路なし。未確認を「完成」と報告しない。

見た目の方向を変える場合は先に画面案を見せる。今回の方向は承認済みで、内部実装をファイル単位で再承認させない。セーブ破壊・5×6ルール・納品報酬・正式6名デザイン・課金・外部SDK・main統合だけは事前確認。

**2026-10-22最新の表示方針：ユーザーは「一度も表示されていないので、画像表示なし。その分を作業へ」と指示した。チャットへの画像貼り付け・直接画像表示は行わず、表示だけを目的とした生成・再送にも時間を使わない。** 作業上の実画面検証とBefore／Afterの証拠保存は継続し、報告はテキストを基本とする。必要な資料はPDF／JPEGとして保存し、ユーザーが必要とする場合に既存featureブランチのGitHub HTTPSリンク（ブラウザログインが必要）で案内する。Codexアプリで取得不能だった `/workspace/...` のPDF／JPEGリンクは繰り返さない。機密資料を一般公開サービスへ無断で移さない。

後続の「表示される拡張子でください」には、正常デコードを確認した実JPEG（`.jpg`）のGitHubファイルリンクで対応する。拡張子だけを変更した偽画像を作らない。現在の中央／西／東比較JPEGは上記acceptedディレクトリ内。北の操作手順は `docs/north-review-steps.txt` にも保存する。ユーザー端末の表示成功を未確認のまま断言しない。

## 1. デザインの第一基準

第一基準は正式承認された [中央／MergeのVisual Lock](References/Preview/design-first-2026-10-21/design-proposal.png)。9月22日採用の [比較画像](a_colorful_promotional_collage_ui_concept_board.png) と [比較基準の記録](References/Preview/integration-baseline-2026-09-22/README.md) は補助基準として参照する。正式6名、明るく華やかな街、POPでかわいいUI、分かりやすいMerge、依頼→Merge→納品→街成長、BGM3曲選択、家庭向けのやさしい世界観を維持する。新案が見劣りするなら元へ戻す。

正式名はモモ・ルル・ポム・フィオ・ミエル・ノア。旧参考画像の仮名へ戻さない。ロゴと正式キャラの元絵を維持する。

## 2. 中央＋東西南北の5エリア

中央＝噴水・イベント・集合、西＝カフェ、東＝花屋／ショップ、北＝依頼小屋・物語、南＝庭・散歩・将来施設候補を最終形候補とする。初回から巨大にする必要はないが、設計上は中央から四方向へ自由に広げられること。縦長一本道へ戻さない。

## 3. 同じ街を歩く

5エリアを別々のメニュー画面にしない。通常はキャラを歩かせ、道を進んで隣へ到達する。内部の背景／カメラの処理方法より、プレイヤーに同じ街の中を歩いているように見せることを優先する。ファストトラベルは補助。

## 4. 自由に動ける面

一本道にしない。噴水を回り、ベンチ・花壇・カフェ席・木陰・店前・入口・小広場・他キャラへ行き、道を選んで隣へ進めるようにする。NPCも重ならず、横へ避ける・一時停止・別ルート・滞在地点の予約・軽い回避で、すれ違いと複数ルートが成立すること。ここも歩けた方が楽しいと判断できる場所は、植栽・建物への侵入や5エリア構成の変更を避けながら、積極的に歩行面へ追加してよい。

## 5. 施設は目的地

北の依頼小屋、西のカフェ、東のショップなどは、徒歩→到着→ドアをタップ→入場→対応画面という体験を基準にする。単なる背景やメニューボタンで済ませない。現候補の徒歩開始後の自動入場や、未実装の施設内体験を、この新方針の達成と取り違えない。

## 6. 平面Merge＋高品質な演出

平面は簡素化を意味しない。9月22日案の明るさ・色・可読性・大きなアイテム・直感性を基準にする。5×6を維持し、種類の識別、必要数、選択、Merge相手、ドラッグ、キラキラ、Lv.UP、不足品、かわいく高品質な盤面枠を一体で仕上げる。見づらい／情報過多なら整理案を提示する。斜め盤面・厚い受け皿へ勝手に戻さない。

## 7. 原画を活かすキャラ

フル3Dを目的にしない。正式キャラのかわいさを守り、影・奥行き・前後関係・軽い拡縮・上下動・手足・まばたき・向き・表情・到着反応・会話相手を見る・ベンチで休む・花を見る動作で2.5Dの生命感を作る。静止画像が滑って見えるなら、指摘を待たず改善対象にする。将来の3D化は、まず1名の試作を確認してから判断する。

## 8. 図鑑の継続強化

良い方向の機能として維持・自主的に強化してよい。NEW、新発見、？？？、次の解放、会話回数、好きな場所／もの、表情、リアクション、キャラ同士の関係で、「図鑑を埋めたい」より「この子をもっと知りたい」と思える体験にする。

## 9. 毎日会いに行く理由

今日のもふまち、今日のお願い、図鑑の発見、街成長、次の解放を連動させ、「今日もこの子たちを見に行こう」と思えるようにする。明日戻る理由が弱いと判断したら、小さな仕掛けを提案する。数値の追加だけで済ませず、初期リリース前に巨大なシステムを増やさない。

街Lvの変化が弱い場合は、街灯・ベンチ・花壇・看板・カフェ席・店舗装飾・アーチ・NPC人数／行動・会話・道・小イベントを候補に積極的に強化してよい。Lv.UPで何が変わったか一目で分かり、街を育てたくなることを重視する。

## 10. BGM

街の「おさんぽタウン」は現在の方向で採用。TOPは明るいワクワク、Mergeは特にPOPでテンポよく楽しく操作したくなる方向。古いゲーム的な電子音主体を避ける。3曲選択・試聴・音量・ON/OFF・保存復帰を維持する。画面に合わない曲は理由と代案を示し、TOP／Mergeは必要なら再選定する。

## 11. 街を隠さないUI

必要情報だけ常設し、その他は折りたたむ／メニューへ収納する判断を自主的に行う。会話中は不要UIを減らす。景色とキャラを最優先にし、HUDが街を隠す場合は改善対象にする。

## 12. まとめて実機確認

内部テストは工程ごとに行える。提示するQA APKは複数の改善がまとまり、明確に進歩した版にする。ボタンだけ・影だけの版を順次Pixel確認へ出さない。

## 13. 方向変更と内部実装の承認

技術的に曖昧なFBや複数の解釈がある場合は、大規模変更する前に、解釈A・解釈B・推奨案・推奨理由・既存仕様への影響を簡潔に示す。

以下は必ず実装前に確認する：5エリア構成そのものの変更、正式6名のデザイン変更、Mergeの5×6ルール変更、SaveDataの破壊的変更、納品／報酬計算変更、課金、ガチャ、外部SDK、main統合、既存セーブが失われる可能性のある変更。

それ以外は承認済みの方向で自主的に改善する。細かなUI位置・アニメーション速度・余白・装飾・NPC動線など、根幹を変えない調整は毎回事前確認しない。以前の「UI全体配置やMerge基本レイアウトという分類だけで一律に確認する」運用へ戻さず、方向性／影響が曖昧な大きな変更なのか、承認済み方向の改善なのかを判断する。

承認済み方向の内部実装は、ファイル単位の確認で止めない。保存中の盤面や進行を、見栄えのために初期化・補充・改変しない。原因不明の失敗を推測で修正したり、検査を緩めたりしない。

## 14. 同条件のBefore／After

改善報告では、街全景・移動・Merge・キャラ・UI・フォント・図鑑・街成長・デイリーを同じ画面・同じ条件で比較する。Unityの隔離テスト状態、実Pixel画像、素材／図解を区別し、生成素材や図解を実画面の代わりにしない。

## 15. 進捗と確認状態

機能の有無より、Pixel 3aで見て操作したときの体験を評価する。コード量・成功テスト数・資料の作成だけを改善の成果としない。未実機確認の実装は必ず **「実装済み・実機未確認」** と表記する。

報告では **設計済み → 実装済み → Unity確認済み → APK生成済み → Pixel 3a確認済み** のどこにいるかを明示する。各証拠は同じ製品版へ対応させ、前版の成功を新版へ引き継がない。Unity実描画、QA APK生成、Pixelの表示／操作確認は別の状態として記録する。テスト成功をPixel合格に置き換えない。最終判定は、Pixel 3aで見て操作し、前より良くなったとユーザーが確認できること。

## 16. 改善判断の優先順位

1. 遊んで楽しいか
2. 操作しやすいか
3. 毎日戻る理由があるか
4. キャラに愛着が湧くか
5. 街を育てたくなるか
6. 見た目がかわいいか
7. 技術的に美しいか

タップしにくい、見づらい、小さい文字、窮屈な画面、一本道、キャラの重なり、遊び方／次目標が不明、達成感不足、単調な操作、不自然なBGM／SE、弱い街成長、図鑑に気づきにくい場合は、指摘がなくても改善候補として取り上げる。コードの美しさよりプレイヤー体験を優先する。

## 17. 意図を守る自主改善

改善は歓迎されているが、キャラの顔・服・色、ロゴ、かわいいファンタジー世界、9月22日基準を変えない。「高品質化」を理由に別ゲームへ作り替えない。FBとゲームとして良い設計が衝突する場合は、黙って従わず、変更した方がよい点と理由を提示する。確認必須の変更を、良い設計という理由だけで進めない。

## 18. 開発報告末尾の改善候補

毎回の開発報告の最後に **「Codex／AI側から見た次の改善候補」** を最大3件載せる。本当に効果がありそうなものだけを選び、効果・実装難易度・リリース前／後を添える。不要なら「今回はなし」。候補は提案と実装済みを区別し、現工程を中断したり、機能を際限なく追加する口実にしない。

## 最終目標

かわいいキャラクターと5つの街を歩き、お願いを聞き、Mergeで届け物を作り、街と仲間が少しずつ成長し、翌日また会いに行きたくなるゲーム。「新しくした」より「前より楽しくなった」を優先する。
