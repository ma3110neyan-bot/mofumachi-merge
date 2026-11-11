# 第8工程：既存Eドライブ実体byte比較／E1

状態：**完了（今回の既存実体メタデータ比較のみ）**。書き込み元・Unity保存仕様の正当性は未確定。branch：`feat/daily-world-pass-20261016`。確認対象repo/evidence commit：`70d01ccc1ef4a960adfccc068f4e21ca9d832514`。実ビルドsource：`41529cfaea57ab564da46b3a45c41ede0aa5088b`。確認対象commitと、この証跡共有commit自身を区別する。

対象run：`rc-20261110-132609-c6829dc7`。Windows採取時刻：2026-11-11 18:50:06 JST（09:50:06 UTC）。ユーザーが既存B/Cを直接byte読み取りした公開メタデータZIPを受領し、CRC・6原本・重複JSON・値/hex/code point/context/offsetの整合と、元runのsnapshot記録SHA・保存済みdiff SHAを照合した。クラウドでB/C本体を直接読んだ、または本体全byteを受領したという意味ではない。

| 項目 | B：prebuild snapshot-2.bin | C：既存post-build ProjectSettings.asset |
| --- | --- | --- |
| exists | YES | YES |
| SHA256 | `d334127167318fe7278575eb17fe2621ab2c0e39c3ab2c53b7c1f39cc555e7fc` | `266f17baf9d7101401580553aab9d0ede0390f6c859e137e31f6274d4713f07a` |
| size | 24,343 bytes | 23,513 bytes |
| m_APIs | `0b000000` | `0b000000i` |
| value bytes / raw hex | 8 / `30 62 30 30 30 30 30 30` | 9 / `30 62 30 30 30 30 30 30 69` |
| 最終byte / code point | `30` / U+0030 | `69` / U+0069 |
| value offset / context start | 12235 / 12226 | 11812 / 11804 |
| m_Automatic | 0 | 0 |
| BOM / UTF-8 validity / ASCII only | none / true / true | none / true / true |
| CRLF / bare LF / bare CR | 846 / 0 / 0 | 0 / 846 / 0 |
| 最終更新UTC | 2026-11-10T13:34:07.6560780Z | 2026-11-11T04:40:31.6931504Z |
| 採取中のsize/mtime変化 | なし | なし |

**D保存済みdiffのm_APIsは`0b000000i`、rawは`30 62 30 30 30 30 30 30 69`。C/D一致：YES（値・raw byte）。B/C一致：NO（値・file SHA）。prebuild→postbuild実体mutation確認：YES。判定：E1。** Bの実測SHA/sizeは元snapshot.jsonの記録と一致する。末尾iはdiff出力だけの文字化け・公開コピー加工では説明できず、既存C実体の採取結果にも存在する。

発生区間：prebuild snapshot取得後〜今回のC実体観測前。Cのmtimeは2026-11-11 13:40:31 JSTだが、この時刻を正確な書込み時刻・書き込み元の証明にはしない。**proven writer：未確定。Unity正規副作用と断定：NO。guard exact許可候補：NO。** 改行CRLF→LFも観測されたが、追加ASCII iの発生原因や妥当性を改行だけで説明しない。

Git属性調査：**NO**。指示ではC/D不一致の場合だけ許可されており、今回は一致のため実施していない。新しいgraphics調査・検索・外部調査も行っていない。

今回のsource変更／Unity起動／AAB／APK／Pixel／preflight／guardテスト／signing／restore／cleanup／自動再試行は**すべて0**。ゲーム・release入口・QA・ProjectSettings・Packages・Visual Lock・guard・allowlist・gitignoreを変更していない。新規公開メタデータとレビュー集約だけを更新。snapshot binary、ProjectSettings全体コピー、AAB、keystore、秘密値はGitへ追加していない。

**今回のsource_unchanged_by_audit=true。既存AAB runのsource_unchanged=false／全体FAIL／artifact署名未検証は維持。** 過去Unity内AAB生成PASSを、今回の新規AAB実行・release全体合格にしない。第7工程complete=true、第8工程complete=false。次にAPKへ進んで安全か：**NO**。

原本6ファイル、`receipt.json`、`evidence-review.json`、`checksums.sha256`は`References/Review/inbox/latest-release-candidate/graphics-api-physical-audit/`。受領ZIP SHA256：`58833e72761679ef83f3372a9ef4535070f1f10238dd483c4eab665795c4d424`。snapshot/ProjectSettings本体は公開しない。旧`graphics-api-audit/`とnative runの原本は変更していない。

次の推奨アクション：今回の証跡共有後に停止し、**別指示待ち**。必要なら書込み元・Unity native保存仕様の証拠取得を別工程として指定する。今回E1であることだけを根拠にguardを許可・削除・緩和せず、AAB再実行・APK・Pixelへ進まない。
