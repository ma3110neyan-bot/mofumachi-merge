# SaveData v4第1工程：Guard整合確認済み・Windows実行待ち

更新日：2026-11-06（日本時間）。状態：**修正済み・未確認**。

- branch：`feat/daily-world-pass-20261016`
- 検証対象の実装commit：`2b81174496a5b4703bad0516645829893d4297b9`。SaveData v4は再実装していない。
- Windows検証ランチャーcommit：`ae909aea406d3424b449e41329d149a608a790ee`（初版`3b20fbd`を含む）。共有文書commitとは区別する。
- 今回の目的：正しい既存検証入口を確定し、第1工程の直接検証を1回だけ行う準備を完了する。
- 実施：Guardの参照・scope確認、現行の既存キャラGuardの読取チェック1回合格、専用Windowsランチャー作成、構文／API／文字コード／引数の静的確認。
- Unity起動：0回。SaveData直接テスト：**未実行（NotRun）**。自動再試行：0回。実行結果をPassed/Failedと推測しない。
- SaveData v4第1工程完了：**false**。第2工程へ進める：**false**。
- 主要ブロッカーは1つ：**Windows実Unityでの対象3fixture・63ケースの検証結果が未取得。**

## 公開状態

専用ランチャーと今回共有記録はローカルでコミット済み。**今回のpushは未完了**。通常環境のプロキシ接続失敗（exit 128）後、追加ネットワーク権限の承認処理がユーザーにより中断され、成功receiptは得られていない。force push・追加のpush試行・Unity再試行は行わない。この通信状態をSaveData不具合やUnityエラーとして扱わない。

## Guard問題の結論

以前選択した`Assert-OriginalBaseForCandidate`は、SaveData専用のGuardではなく旧Visual候補／製品受入れ用の入口だった。

1. `scripts/build-android.ps1:258`の全体入口は無条件にこのGuardを呼ぶ。前工程でもこの関数を直接選択していた。
2. `scripts/visual-lock.ps1:187`→`Get-BasePolishPatch`（34〜41行）は、`stage-acceptance-2026-10-21.json`の`base_polish`を読む。
3. この参照は`noah-final-rig-candidate-2026-10-29.json`に残り、scopeは`town_noah_quality_2026_10_28`、記録自体のstatusは`rejected_visual_retired`／`trial_discontinued=true`。
4. 89行のSHA検査で旧TownActor候補と不一致になった。SaveDataテストからこのVisual Guardを呼ぶ依存はない。全体ビルド入口を使うと旧候補検査が先に走る。

| TownActorの記録 | LF正規化SHA256 |
| --- | --- |
| 旧Noah候補 | `10dc0c0a8f041bc511ef13c0d89bb773ef6e8a0fc564f39325e20a8a0c68c57e` |
| 現行キャラ固定記録／現行／SaveData実装前後 | `1591e855b186f5e232600f24cc7ab72c70523eee6975b2b62d0002632e8905b2` |

現在の`characters-25d-final-2026-11-01.json`は後者を固定しており、既存の`Assert-CharactersVisualLock`（`scripts/character-finish-scope.ps1:49`）が現行固定記録と既存head recovery/calibrationの限定記録を検証する。Coreを凍結する旧Noah実験やフォント専用のscopeをSaveDataへ流用しない。

**この既存Guardの読取チェックは同作業内で1回、exit 0／Passed。** 最新の指示後に繰り返していない。旧失敗Guardは再実行していない。

Guard変更：**NO**。Guard無効化／弱体化：**NO**。新しい例外追加：**NO**。Visual Lock原本変更：**NO**。TownActor／ゲームコード変更：**NO**。

旧Visual／全体ビルド入口そのものは未修正のまま保持する。今回確定したのはSaveData直接テストの入口であり、Visual承認・APK受入れが通るという意味ではない。

## 正しい既存検証入口

**既存Unity Test FrameworkのEditModeコマンドライン実行**を使用する。既存の呼出し例は`build-android.ps1:302`。全体ビルドスクリプトは実行せず、以下を明示する。

- assembly：`Mofumachi.EditModeTests`。既存asmdefはEditor専用で、参照は`Mofumachi.Core`のみ。
- fixture：`Mofumachi.Tests.SaveServiceTests`（33）、`Mofumachi.Tests.PreferencesTests`（23）、`Mofumachi.Tests.CharacterPreferenceTests`（7）。
- 合計63は固定ソースのTest/TestCase属性数。まだ実行された件数ではない。
- `-testFilter`は上記3クラスの完全な名前空間prefixを正規表現で限定。全件実行へフォールバックしない。
- Android target／EditMode／`-nographics`。Unity起動は最大1回、再試行0回。
- PlayModeは今回実行しない。移行・保存・CopyFrom/rollbackは上記Core fixtureで対象に含まれ、UI／Visual確認や2回目のUnity起動を増やさない。

## Windowsで行う操作

**最新のリポジトリにある`scripts/verify-savedata-v4-windows.cmd`を1回ダブルクリック。**

Windows 11、Windows PowerShell 5.1、Unity Hub既定場所のUnity 6000.6.4f1＋Android Build Support、既存Windowsライセンスを使用。起動中のUnityがある場合は事前条件で停止する。

ランチャーは`2b81174496a5b4703bad0516645829893d4297b9`固定の新規detached worktreeを兄弟フォルダへ作り、元ブランチ・index・未コミット変更・プレイヤーセーブを変更しない。既存Git hookは維持する。固定worktreeで既存キャラGuardと入力一致を確認し、対象テストを1回だけ起動する。フォント用ランチャー／preview処理には依存しない。

`Builds/Diagnostics/savedata-v4-yyyyMMdd-HHmmss-xxxxxxxx/`へ保存：

- `editmode.xml`、`unity.log`、`stdout.log`、`stderr.log`
- `source-info.json`、`source-revision.txt`、`unity-arguments.txt`
- 失敗時のみ`first-error.log`。コンパイル／起動エラーまたは最初のtest failure全文・stackを抽出し、exit codeだけを根本原因扱いしない。未抽出なら未確定と記録する。

`source-info.json`はsuccess/result、14項目別のverification_itemsと対応test-case名、stage1_complete、stage2_safe、最初の失敗、固定SHA、起動回数・再試行回数を持つ。入力不一致、63件不足、skip/inconclusive、未合格、必須証拠欠損では合格にしない。事前条件の失敗もBlockedとしてJSONへ保存する。

同commitの起動記録`.attempted`は排他作成し、自動削除・上書きをしない。2回目のダブルクリックは停止する。隔離worktreeとDiagnosticsも自動削除しない。

## 必須項目と固定テストの対応

現時点の全項目は**未実行**。成功時のみJSONへPassedと実際のtest-case名を記録する。

| 必須確認 | SaveServiceTestsの対象 |
| --- | --- |
| 新規v4＝24P | NewV4HasUnclaimedEconomyAnd24Points |
| v1／v2／v3→v4 | LegacyDiskMigrationKeepsEveryFieldAndOriginalBytes(1)/(2)/(3) |
| 既存項目保持・初回24Pは1回だけ | LegacyDiskMigrationKeepsEveryFieldAndOriginalBytes、V4ZeroBalanceIsNotInitializedAgain |
| v4再読込で再付与なし・0Pでも再初期化なし | V4ZeroBalanceIsNotInitializedAgain |
| pre-v4生原本保持 | LegacyDiskMigrationKeepsEveryFieldAndOriginalBytes |
| 既存pre-v4を上書きしない | ExistingPreV4IsNeverOverwrittenOrRetried |
| 不正v4を自動修復しない | InvalidV4BlocksEvenWhenHealthyBackupExists、MissingRequiredV4EconomyMemberCannotFallbackOrRepair |
| 未対応versionで停止 | UnknownVersionIsNotLoaded |
| 移行失敗で新規Saveへ置換しない | FailedCommitKeepsLegacySourceProtectionAndCandidate、ProtectionCreationFailureDoesNotTouchLegacyPrimary |
| CopyFrom／rollback | CopyAndFailedMutationRestoreCompleteEconomyWithoutAliasing |

PreferencesTests／CharacterPreferenceTestsも同じ1回に含め、音設定・キャラ設定の移行保持／保存失敗を確認する。

## 確認範囲と次の判定

PS1はUTF-8 BOM、CMDはASCII CRLF。PowerShell 5.1対応の構文・.NET APIを静的確認し、PowerShell 7.4.6で構文エラー0件・限定引数・63ケース／14証拠定義・Unity起動箇所1か所を確認した。**Windows PowerShell 5.1やUnityを実際に実行した証拠ではない。** クラウド環境／ライセンス調査は繰り返していない。

以前の旧Guard失敗は履歴としてerror.logに保持。今回新しいUnity/test failureはない。途中の静的引数確認でLinuxにC:ドライブがないエラーが出た点はstatus.jsonのtooling_notesへ分離した。既存ローカルパスでの引数確認のみ行い、ゲームコード修正やUnity再試行はしていない。

今回の変更は専用ランチャー2ファイルとsummary/statusのみ。ゲーム／SaveData／テストソース／正式素材／フォント／Visual Lock原本／既存Guardは変更していない。

**第2工程「もふポイント管理基盤」へ進んで安全か：NO。** 主要ブロッカーはWindows実Unityの対象テスト結果が未取得であること。結果が63/63で、14項目・原本保護・失敗停止・入力固定の確認を満たした場合のみ、第1工程「SaveData v4移行基盤：完了」へ更新する。全ゲーム完成・実機リリース合格とは扱わず、現在は第2工程へ進まない。
