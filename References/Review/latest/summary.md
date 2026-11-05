# 最新レビュー：最小修正適用・実Unity確認は未実施

- 現在ブランチ：`feat/daily-world-pass-20261016`
- 最新の確認対象作業commit SHA／製品候補SHA：`75cd7c0ef234934a17aa623860937d894a1d875c`
- 今回の目的：承認済みのheightAllowanceだけを修正し、指定Native2テストを1回、両方成功時のみ最小スクリーンショットへ進む。
- 実施内容：ReleaseTypography.FitsのheightAllowanceを指定式へ変更し、既存SHA安全記録の該当値だけ更新。差分とSHAを読み取り照合し、実装2ファイルをpush済み。
- 成功／失敗：実装変更とSHA照合は完了。実Unityテストは**未実施**。このクラウドには有効なライセンス設定を確認できず、DISPLAY／WAYLAND_DISPLAYが未設定、Xvfbもないため、既知の失敗起動を繰り返さず実行前に停止した。`success=false`／`tests_passed=false`は未確認を示す。新しいNativeテスト失敗を観測した状態ではない。
- 根本原因：修正前run `font-only-20261104-204729-31fcbc90`で特定した、高さ14に必要高14.5を+0.5許容で受け入れる問題。今回はその承認済み修正だけを適用。旧runの1成功／1失敗を修正後の結果へ流用しない。
- 修正内容：以下の2ファイルのみを実装コミットへ収録。Hachi採用・UGUI Bold・枠・配置・色・キャラ・アイテム・街・Mergeロジック・SaveData・BGM・0頂点assert・PowerShell終了検知は不変。
- 次にやること：共有記録をpushして停止。ライセンスと描画環境のあるUnityで指定2件だけを1回実行する確認は残っている。2件とも成功した実結果がないため、スクリーンショットは取得していない。全件テスト・APK・Pixelには進まない。

## 実装差分（合計2ファイル）

1. `Assets/Mofumachi/Presentation/ReleaseTypography.cs`：heightAllowanceの式だけ。

```csharp
float heightAllowance =
    ReleaseFontPolicy.CompactMinimumSize(text.name) > 0 ? 0f : .5f;
```

2. `References/VisualLock/font-only-2026-11-04.json`：ReleaseTypographyの既存SHA欄だけ。新SHA256は `75e0a7cd549ad9193d217ab2cff703beed5bec64b635053e94cc3a1d8a7cc645`。before_sha256、他ソースSHA、親manifest、各合格フラグは変更していない。

今回の結果記録として、指定された既存共有先のsummary.md／status.jsonだけを別コミットで更新する。error.log・visual-review.md・images/・受領原本は変更しない。

## 指定テストと実行状況

| 対象 | 今回の状態 |
| --- | --- |
| `ReleaseUxNativeTests.ScopedFontsFitTitleTownMergeBookSettingsAndTodayAtPhoneSizes` | 未実行：Unity実行環境の前提不足 |
| `StoryTypographyTests.StoryRolesAndNoticeFitPortraitSizes` | 未実行：同上 |
| 実Unityスクリーンショット | 未取得：両テスト成功条件が未達 |

Unity起動0回、Nativeテスト呼び出し0回、自動再試行0回。代わりのhostテスト・APIコンパイル・全件テストも実行していない。実Unity／Pixel合格や実画像取得を主張しない。

環境確認はファイル・環境変数の存在を読み取っただけで、Unityを起動していない。ライセンス用環境変数は未設定、標準保存先にULFはなく、Unity設定内でライセンス関連名に一致したのはAudit.logだけ。描画サーバもない。この状態から、過去のNo valid Unity Editor license失敗を今回の新規テストエラーとして作り直さない。

`error.log`は修正前193ab6eのrun204729で受領したassert原文を保持している。修正後75cd7c0のテスト結果ではない。最新状態はstatus.jsonの`minimal_fix_applied_native_validation_blocked`を参照。

- [機械判定用status.json](status.json)
