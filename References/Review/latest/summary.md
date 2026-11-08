# 第7工程再開：C/E運用を固定、旧v1/v2原本の読み取り確認待ち

- 更新：2026-11-08（日本時間）。
- 状態：**調査中**。ゲーム実装済み、今回のWindows検証は未実施。
- branch：`feat/daily-world-pass-20261016`。
- 今回確認したクラウドHEAD／GitHub HEAD：`e5f1da243d423e5e7af9a7e4999d7865c51442b6`。Fetchは1回、ahead/behindは0/0。確認開始時のworking treeはclean。
- 固定ゲーム実装：`46f65b24d58531d5934035fbe4cdb4d5b00104a6`。
- 既存E-v2ランチャー：`fea87beb5670d28c8dc3c1efa87b3bd4dee98aa0`。v3は作成・実行していない。

## 今回の目的と保存先

第7工程の検証再開前に、旧v1/v2が未実行であることと既存記録を読み取り確認する。第1〜第6工程は合格済みとして保持し、再実装・再検証しない。

今後の正式運用：

| 場所 | 用途 |
| --- | --- |
| `C:\Users\User\Desktop\Mofumachi\mofumachi-merge` | 唯一の正式Windows repo。移動・リネーム・削除しない |
| `E:\Mofumachi-Validation\projects\...` | 検証worktree／一時UnityプロジェクトとLibrary |
| `E:\Mofumachi-Validation`配下 | TEMP/TMP、UPM cache、Diagnostics原本、receipts、attempt-stops |
| GitHub／正式repo | 必要な小容量JSON/XML/source revision/log/summary/statusのみ |

C空き約39.4GB、古いC検証worktree整理済み、旧E-v1/v2のworktree登録とdirect実体整理済みは**2026-11-08のユーザー報告**。クラウドから再測定／整理はしていない。正式repo、残存Diagnostics、receipts、attempt-stopsを削除・上書きしない。

## 再開時の7項目

| 項目 | 確認結果 |
| --- | --- |
| 1. branch | クラウドは上記feature。Windows正式repoの現在値は未確認 |
| 2. HEAD | クラウド／GitHubは上記e5f1da2。Windows現在値は未確認 |
| 3. working tree | クラウドclean。Windows未確認 |
| 4. E-v1/v2 receipt | v2は以前のユーザー画面で存在・読取成功。現在の両receipt原本／SHAは未取得 |
| 5. Diagnostics | GitHub固定inboxは旧Cのprogress-ui-v1だけ。E-v1/v2原本は未取得 |
| 6. attempt-stops | Windows原本は未確認 |
| 7. 登録worktree | クラウドは`/workspace/mofumachi-merge`だけ。Windows登録解除済みはユーザー報告、現一覧は未取得 |

クラウドにはWindows C/Eの接続がない。読み取り確認を済ませたと偽らず、Windows記録の取得前にv3を作成しない。GitHubを追加Fetchで繰り返し調査しない。

## E-v2について今回新しく確認された報告

2026-11-08のユーザー指示には、旧v2が以下の状態だったと明記されている。

- `result=Preparing`、`validation_passed=false`。
- `unity_invocations=0`。
- `direct=NotRun`、`integration=NotRun`。
- 両Phaseとも`unity_started=false`。
- `first_error=null`、`runner_error=null`。

**報告上は55件／444件とも未実行であり、製品テスト失敗として扱わない。** Preparing JSONはterminal resultではない。上記はユーザー確認済み情報として保存し、E原本をCodexが直接読んだ結果とは区別する。旧v1は未完了との報告であり、その具体的な停止原因／ファイル状態は未確認。

先のPowerShell画面はPS1の実行ポリシーによる読込拒否（PSSecurityException）だった。関数未定義・空パスはその連鎖であり、この画面から容量不足やv2の本来の停止原因は断定できない。今回の再開指示ではCの整理完了が報告されている。追加の移動や削除は実施しない。

## 旧記録の保存状況と次の前提

- GitHubの旧C容量不足run `progress-ui-integration-20261107-174541-8e831d09` は既存5ファイルをそのまま保持。E-v2の証跡へ流用していない。
- Eの旧v1/v2原本、receipt、attempt-stopsは変更・削除していない。
- ユーザーからの今回の状態報告はこのsummary/statusへ保存。
- **未達の前提は、Windowsの旧v1/v2原本とworktree一覧の取得・照合のみ。** v2の中断原因は推測せず未確定のまま。
- 次は下記の読み取り専用コマンドの出力を確認する。旧記録の保存・照合後に限り、必要なら新しい`progress-ui-e-drive-v3`を別receiptで用意する。
- 既存CMDは再実行しない。v3を公開する段階でもゲーム実装・テスト内容・fixture件数を変更しない。

## Windowsでの読み取り専用確認

**以下をPowerShell画面へ一度貼り付ける。PS1ファイルとして実行しない。**

PS1の読み込み、実行ポリシー変更、Unity起動、テスト、Fetch/Push、ファイル作成・削除・上書きは行わない。標準コマンドで既存記録を表示するだけ。Git statusはoptional locksを無効にして確認する。出力にはユーザーPCのパスが含まれるため、この開発スレッドで共有する。

```powershell
& {
    $ErrorActionPreference = 'Stop'
    $repo = 'C:\Users\User\Desktop\Mofumachi\mofumachi-merge'
    $root = 'E:\Mofumachi-Validation'
    $product = '46f65b24d58531d5934035fbe4cdb4d5b00104a6'
    $info = [ordered]@{ repository = $repo; validation_root = $root }
    $git = Get-Command git.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    $gitPath = $null
    if ($git) { $gitPath = $git.Source }
    elseif ($env:LOCALAPPDATA) {
        $bundled = Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA 'GitHubDesktop\app-*\resources\app\git\cmd\git.exe') -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
        if ($bundled) { $gitPath = $bundled.FullName }
    }
    if ($gitPath) {
        $info.branch = @(& $gitPath --no-optional-locks -C $repo branch --show-current)
        $info.head = @(& $gitPath --no-optional-locks -C $repo rev-parse HEAD)
        $info.working_tree = @(& $gitPath --no-optional-locks -C $repo status --short)
        $info.worktrees = @(& $gitPath --no-optional-locks -C $repo worktree list --porcelain)
    } else { $info.git = 'Git not found; Windows branch/HEAD/worktrees remain unconfirmed' }
    $variants = @('progress-ui-e-drive-v1','progress-ui-e-drive-v2')
    $info.attempts = @(foreach ($id in $variants) {
        $receipt = Join-Path $root ('receipts\progress-ui-integration-' + $product + '-' + $id + '.attempted')
        $a = [ordered]@{ validation_id = $id; receipt_path = $receipt; receipt_exists = (Test-Path -LiteralPath $receipt -PathType Leaf) }
        if ($a.receipt_exists) {
            $a.receipt_sha256 = (Get-FileHash -LiteralPath $receipt -Algorithm SHA256).Hash
            $run = [IO.File]::ReadAllText($receipt).Trim()
            $a.run_path = $run
            $boundary = [IO.Path]::GetFullPath((Join-Path $root 'Diagnostics')).TrimEnd('\') + '\'
            if ($run -and [IO.Path]::GetFullPath($run).StartsWith($boundary,[StringComparison]::OrdinalIgnoreCase)) {
                $a.files = @(foreach ($name in @('progress-ui-integration-result.json','direct\results.xml','integration\results.xml','direct\unity.log','integration\unity.log','source-revision.txt','share-result.json','first-error.log')) {
                    $path = Join-Path $run $name
                    [pscustomobject]@{ name = $name; exists = (Test-Path -LiteralPath $path -PathType Leaf) }
                })
                $result = Join-Path $run 'progress-ui-integration-result.json'
                if (Test-Path -LiteralPath $result -PathType Leaf) {
                    $a.result_sha256 = (Get-FileHash -LiteralPath $result -Algorithm SHA256).Hash
                    try {
                        $a.saved_result = ([IO.File]::ReadAllText($result) | ConvertFrom-Json | Select-Object source_commit,validation_id,result,validation_passed,unity_invocations,source_unchanged,first_error,runner_error,direct,integration)
                    } catch { $a.result_read_error = $_.Exception.Message }
                }
                $revision = Join-Path $run 'source-revision.txt'
                if (Test-Path -LiteralPath $revision -PathType Leaf) { $a.source_revision = [IO.File]::ReadAllText($revision) }
            } else { $a.receipt_path_error = 'Empty run path or outside E Diagnostics; not inspected' }
        }
        [pscustomobject]$a
    })
    $diagnostics = Join-Path $root 'Diagnostics'
    $info.diagnostics_exists = Test-Path -LiteralPath $diagnostics -PathType Container
    if ($info.diagnostics_exists) {
        $info.diagnostics_entries = @(Get-ChildItem -LiteralPath $diagnostics -Directory | Where-Object { $_.Name -match 'progress-ui-e-drive-v[12]-' } | Select-Object FullName,LastWriteTimeUtc)
    }
    $stops = Join-Path $root 'attempt-stops'
    $info.attempt_stops_exists = Test-Path -LiteralPath $stops -PathType Container
    $info.attempt_stops = @()
    if ($info.attempt_stops_exists) {
        $info.attempt_stops = @(foreach ($file in (Get-ChildItem -LiteralPath $stops -File -Filter '*.json')) {
            try {
                $s = [IO.File]::ReadAllText($file.FullName) | ConvertFrom-Json
                if ($s.source_commit -eq $product -and $s.validation_id -in $variants) {
                    [pscustomobject]@{ file = $file.FullName; sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash; data = $s }
                }
            } catch { [pscustomobject]@{ file = $file.FullName; read_error = $_.Exception.Message } }
        })
    }
    $info | ConvertTo-Json -Depth 12
}
```

## 今後の検証条件（まだ実行しない）

固定製品commitは46f65b2、Unity6000.6.4f1、Android target、EditModeのみ、両Phaseで`--burst-disable-compilation`。Eへworktree、Library、TEMP/TMP、UPM cache、Diagnostics原本を置く。

Phase1：`Mofumachi.Tests.ProgressUiIntegrationTests`のみ55/55、Failed/Skipped/Inconclusive0、Unity exit0、source_unchanged=trueが必要。

Phase2：Phase1合格時のみ、正式9fixtureの444/444。SaveService33、Preferences23、CharacterPreference7、MofuPointService68、GenerationCost33、TownPointReward69、RecoveryExtraHelp71、QuestTownLevel85、ProgressUiIntegration55。Failed/Skipped/Inconclusive0、Unity exit0、source_unchanged=trueが必要。

各Phase最大1起動、合計最大2。自動再試行0。失敗はfirst errorを保存し、後続へ進まず、同じ起動・推測修正・全件/APK/Pixel/Visualへ拡大しない。必要な小容量証跡だけ既存固定inboxへ共有し、旧C/E-v1/E-v2履歴と新v3を区別する。

## 今回の変更と最終判定

変更は`References/Review/latest/summary.md`と`status.json`のみ。ゲーム、テスト、ランチャー、Visual Lock、Guard、フォント、package、ProjectSettingsは変更していない。Unity起動0、テスト実行0、自動再試行0。

| 項目 | 現在の結果 |
| --- | --- |
| 使用済み検証ID | 旧`progress-ui-e-drive-v1`／`progress-ui-e-drive-v2`。今回新規検証なし |
| 新規v3 | 未作成・未実行 |
| Eドライブ使用 | 既存ランチャーの設定として確認。Windows原本は未取得 |
| Phase1 Total / Passed / Failed | E-v2はユーザー報告`NotRun`。実XML件数は未確認（null） |
| Phase2 Total / Passed / Failed | E-v2はユーザー報告`NotRun`。実XML件数は未確認（null） |
| Unity起動回数 | 今回Codex0、旧v2はユーザー報告0 |
| 自動再試行 | 今回0 |
| source_unchanged | Windows検証値は未確認（null） |
| first_error / runner_error | 旧v2はユーザー報告ともにnull。原本未確認 |
| 第7工程 complete | **false** |

**第8工程「実機Pixel 3a確認＋リリース候補ビルド」へ進んで安全か：NO**。

主要ブロッカーは**第7工程の55件／444件のWindows実検証がまだ完了していないこと**。v3の準備前に旧Windows記録の読み取り照合を済ませる。第8工程へは進まない。

`commit`は確認対象の既存ランチャー、`implementation_commit`は固定ゲーム実装、`review_base_commit`は今回照合した先端を示す。共有文書自身のSHAとは区別する。
