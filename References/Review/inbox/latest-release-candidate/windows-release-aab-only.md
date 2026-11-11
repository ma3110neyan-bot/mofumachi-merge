> **2026-11-11：AAB実行枠は消費済み。** 最新run `rc-20261110-132609-c6829dc7` はnative生成PASS／source recovery FAIL。以下の旧実行ブロックは履歴のみで、再実行しない。今回の保存済み結果はsummary.mdを参照。

> **2026-11-10：旧版のAAB実行ブロックは使用しない。** File.Replace/source guard小工程は42/42 PASSだが、今回はUnity/AABの実行は禁止。以下は旧builder identity固定の履歴。次の明示的なAAB実行指示時に、新commitに対応した手順へ切り替える。

# 同じPowerShellで実AABだけを1回ビルド

署名キー作成と合格済みpreflightを実行した、現在開いているPowerShellをそのまま使用する。新しいPowerShell／CMDは開かない。キー作成や単独preflightは再実行しない。

以下のPowerShellブロック全体を1回だけコピーし、その画面に貼り付けて実行する。既存`build-android-release.ps1`を`-Build -Format Aab`で1回だけ呼ぶ。既存ビルド入口内の必須チェックはそのまま通し、別のpreflight専用呼び出しは行わない。既存のattempt receiptを削除・上書き・別名化しない。

4環境変数はSET/NOT SETだけを表示し、keystoreと承認済みrelease入口のidentityを既存preflight原本に照合する。正式repoのbranch／HEAD／working tree／index／untracked／Stage7 ancestor／Visual Lock／QA経路は既存スクリプトがUnity開始前に検査する。dirtyや不整合なら止め、reset／restore／stashしない。

現在の共有HEADは`3e93a338b6ae582635620cc8fbbfdceaa99f7cbe`。ゲーム固定点は`46f65b24d58531d5934035fbe4cdb4d5b00104a6`、preflight実行sourceは`26d2f150337f00baf89771f44f6802f6f6d6402b`。AABのsource HEADは実行時に記録する。共有文書commitを製品変更やAAB成果物として扱わない。この手順を読むためのFetch/Pullや新しい画面は不要。

```powershell
& {
    $ErrorActionPreference = 'Stop'
    $Repo = 'C:\Users\User\Desktop\Mofumachi\mofumachi-merge'
    $Names = @('MOFUMACHI_KEYSTORE_PATH','MOFUMACHI_KEYSTORE_PASSWORD','MOFUMACHI_KEY_ALIAS','MOFUMACHI_KEY_ALIAS_PASSWORD')
    $Missing = @()
    foreach ($Name in $Names) {
        $Set = -not [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($Name,[EnvironmentVariableTarget]::Process))
        Write-Host ($Name + ': ' + $(if ($Set) {'SET'} else {'NOT SET'}))
        if (-not $Set) { $Missing += $Name }
    }
    if ($Missing.Count) { throw 'STOP: signing variables are missing. Do not recreate the key or start a build.' }
    $ExpectedKey = 'faac2959a5e9ae6e5e531c110955a2085502d13da7cc96148d91066ad666b4cb'
    $ExpectedCert = '96bb1402b8085570305a0486ae3f7fa3fafe2c60a3b8cd062c2ee8e0f81b8e09'
    $ExpectedIdentity = '6baaef3e35b48bcd083a913a961cb1fd51a6b2aa8d58b472f9890e0d9458972f'
    $KeyBefore = (Get-FileHash -LiteralPath $env:MOFUMACHI_KEYSTORE_PATH -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($KeyBefore -cne $ExpectedKey) { throw 'STOP: keystore SHA256 mismatch. No build was started.' }
    $Root = 'E:\Mofumachi-Validation\Diagnostics\release-candidate'
    $PriorPath = Join-Path $Root 'rc-20261109-120851-5c06de53\status.json'
    $Prior = [IO.File]::ReadAllText($PriorPath) | ConvertFrom-Json
    if ($Prior.result -cne 'PreflightPassed' -or $Prior.signing_certificate_sha256 -cne $ExpectedCert -or
        $Prior.build_entry_identity -cne $ExpectedIdentity -or -not $Prior.source_unchanged -or -not $Prior.formal_repo_unchanged) {
        throw 'STOP: the saved accepted preflight is inconsistent. Do not rerun preflight.'
    }
    $ToolHashes = @(foreach ($Relative in @('scripts\build-android-release.ps1','Assets\Mofumachi\Editor\AndroidReleaseBuild.cs','Assets\Mofumachi\Editor\AndroidReleaseBuild.cs.meta')) {
        (Get-FileHash -LiteralPath (Join-Path $Repo $Relative) -Algorithm SHA256).Hash.ToLowerInvariant()
    })
    $Hasher = [Security.Cryptography.SHA256]::Create()
    try { $Identity = ([BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::ASCII.GetBytes(($ToolHashes -join ':'))))).Replace('-','').ToLowerInvariant() }
    finally { $Hasher.Dispose() }
    if ($Identity -cne $ExpectedIdentity) { throw 'STOP: release builder identity differs from the accepted preflight. No build was started.' }
    $Before = @(Get-ChildItem -LiteralPath $Root -Directory | ForEach-Object {$_.FullName})
    $StartedUtc = [DateTime]::UtcNow.ToString('o')
    $global:LASTEXITCODE = 0
    & (Join-Path $Repo 'scripts\build-android-release.ps1') -Build -Format Aab
    $Code = $global:LASTEXITCODE
    $FinishedUtc = [DateTime]::UtcNow.ToString('o')
    $New = @(Get-ChildItem -LiteralPath $Root -Directory | Where-Object {$Before -notcontains $_.FullName})
    if ($New.Count -ne 1) { throw 'STOP: cannot identify exactly one new run. Do not rerun the build.' }
    $Run = $New[0].FullName
    $StatusPath = Join-Path $Run 'status.json'
    if (-not (Test-Path -LiteralPath $StatusPath -PathType Leaf)) { throw ('STOP: terminal status missing. Do not rerun. Run: ' + $Run) }
    $S = [IO.File]::ReadAllText($StatusPath) | ConvertFrom-Json
    $N = $null
    $NativePath = Join-Path $Run 'aab\unity-build-info.json'
    if (Test-Path -LiteralPath $NativePath -PathType Leaf) { $N = [IO.File]::ReadAllText($NativePath) | ConvertFrom-Json }
    $KeyAfter = (Get-FileHash -LiteralPath $env:MOFUMACHI_KEYSTORE_PATH -Algorithm SHA256).Hash.ToLowerInvariant()
    $Formal = ($S.PSObject.Properties.Name -contains 'formal_repo_unchanged') -and ($S.formal_repo_unchanged -eq $true)
    $A = $S.aab
    $Artifact = $null
    $ArtifactHash = $null
    if ($null -ne $A -and -not [string]::IsNullOrWhiteSpace([string]$A.path) -and (Test-Path -LiteralPath $A.path -PathType Leaf)) {
        $Artifact = Get-Item -LiteralPath $A.path
        $ArtifactHash = (Get-FileHash -LiteralPath $Artifact.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $ArtifactOk = ($null -ne $Artifact) -and ($Artifact.Length -gt 0) -and
        ($Artifact.Extension -ceq '.aab') -and $Artifact.FullName.StartsWith('E:\Mofumachi-Validation\release-candidate\',[StringComparison]::OrdinalIgnoreCase) -and
        ($ArtifactHash -ceq $A.sha256) -and ($Artifact.Length -eq $A.size)
    $SignerOk = ($S.signing_certificate_sha256 -ceq $ExpectedCert) -and ($null -ne $A) -and
        ($A.PSObject.Properties.Name -contains 'signing_certificate_sha256') -and ($A.signing_certificate_sha256 -ceq $ExpectedCert)
    $NativeOk = ($null -ne $N) -and ($N.result -ceq 'Passed') -and ($N.format -ceq 'AAB') -and
        ($N.source_commit -ceq $S.source_commit) -and ($N.product_commit -ceq '46f65b24d58531d5934035fbe4cdb4d5b00104a6') -and
        ($N.signing_certificate_sha256 -ceq $ExpectedCert) -and ($N.package -ceq 'com.mofumachi.merge') -and
        ($N.version -ceq '1.0') -and ($N.version_code -eq 1) -and ($N.unity_version -ceq '6000.6.4f1') -and
        ($N.backend -ceq 'IL2CPP') -and ($N.architecture -ceq 'ARM64') -and ($N.orientation -ceq 'Portrait') -and
        ($N.development -eq $false) -and ($N.script_debugging -eq $false) -and ($N.deep_profiling -eq $false) -and
        ($N.autoconnect_profiler -eq $false) -and ($N.build_options -ceq 'None') -and
        ($N.custom_signing -eq $true) -and ($N.build_app_bundle -eq $true) -and ($N.temporary_settings_restored -eq $true) -and
        ($N.build_errors -eq 0) -and ($null -ne $Artifact) -and ($N.output_path -ceq $Artifact.FullName)
    $RevisionPath = Join-Path $Run 'source-revision.txt'
    $RevisionOk = (Test-Path -LiteralPath $RevisionPath -PathType Leaf) -and ([IO.File]::ReadAllText($RevisionPath).Trim() -ceq $S.source_commit)
    $Passed = ($Code -eq 0) -and ($S.result -ceq 'Passed') -and ($S.build_requested -eq $true) -and
        ($S.unity_invocations -eq 1) -and ($null -eq $S.apk) -and ($null -ne $A) -and ($A.exit_code -eq 0) -and
        ($A.result -ceq 'Passed') -and ($A.source_unchanged -eq $true) -and ($S.source_unchanged -eq $true) -and
        $Formal -and $ArtifactOk -and $SignerOk -and $NativeOk -and $RevisionOk -and
        ($KeyAfter -ceq $ExpectedKey) -and ($S.automatic_retries -eq 0) -and
        ($null -eq $S.first_error) -and ($null -eq $S.runner_error)
    $Blocker = $null
    if (-not $Passed) {
        if ($S.first_error) { $Blocker = $S.first_error }
        elseif ($S.runner_error) { $Blocker = $S.runner_error }
        elseif (-not $ArtifactOk) { $Blocker = 'AAB is missing, empty, outside E, or its recorded size/SHA differs.' }
        elseif (-not $SignerOk) { $Blocker = 'Verified AAB signer certificate does not match the accepted certificate.' }
        elseif (-not $NativeOk) { $Blocker = 'Native release settings/result do not satisfy the requested AAB conditions.' }
        else { $Blocker = 'Saved build/source/exit conditions did not all pass. Preserve this run; no retry.' }
    }
    $Review = [ordered]@{
        result = $(if ($Passed) {'Passed'} elseif ($S.result -ceq 'Blocked') {'Blocked'} else {'Failed'})
        validation_passed = [bool]$Passed
        build_entry_calls = 1
        build_entry_exit_code = $Code
        requested_format = 'Aab'
        started_utc = $StartedUtc
        finished_utc = $FinishedUtc
        source_commit = $S.source_commit
        product_commit = $S.product_commit
        four_environment_variables = 'SET'
        keystore_sha256 = $KeyAfter
        keystore_matches = ($KeyAfter -ceq $ExpectedKey)
        signer_matches = [bool]$SignerOk
        artifact_path = $(if ($Artifact) {$Artifact.FullName} else {$null})
        artifact_file_name = $(if ($Artifact) {$Artifact.Name} else {$null})
        artifact_size = $(if ($Artifact) {$Artifact.Length} else {$null})
        artifact_sha256 = $ArtifactHash
        artifact_last_write_utc = $(if ($Artifact) {$Artifact.LastWriteTimeUtc.ToString('o')} else {$null})
        artifact_signer_sha256 = $(if ($null -ne $A -and ($A.PSObject.Properties.Name -contains 'signing_certificate_sha256')) {$A.signing_certificate_sha256} else {$null})
        unity_exit_code = $(if ($A) {$A.exit_code} else {$null})
        unity_invocations = $S.unity_invocations
        aab_build_invocations = $(if ($Passed) {1} elseif ($S.unity_invocations -eq 0) {0} else {$null})
        source_unchanged = $S.source_unchanged
        formal_repo_unchanged = $Formal
        native_settings_verified = [bool]$NativeOk
        automatic_retries = $S.automatic_retries
        first_error = $S.first_error
        runner_error = $S.runner_error
        failed_phase = $S.failed_phase
        blocking_issue = $Blocker
        aab_substep_complete = [bool]$Passed
        stage8_complete = $false
    }
    [IO.File]::WriteAllText((Join-Path $Run 'aab-review.json'),($Review | ConvertTo-Json -Depth 6),(New-Object Text.UTF8Encoding($false)))
    $Public = Join-Path $Run ('public-aab-'+[guid]::NewGuid().ToString('N'))
    $null = [IO.Directory]::CreateDirectory($Public)
    $Files = @('status.json','build-info.json','build-settings.json','source-revision.txt','checksums.sha256','signing-preflight.log','first-error.log','aab-review.json',
        'worktree-add.stdout.log','worktree-add.stderr.log','worktree-add.exit.json','aab\unity-build-info.json','aab\signature-verification.log','aab\artifact-certificate.log','aab\unity-arguments.txt')
    foreach ($Folder in @('checkout-source','aab\before','aab\after')) {
        foreach ($Name in @('source-check.json','working.stderr.log','index.stderr.log','untracked.stderr.log')) { $Files += (Join-Path $Folder $Name) }
    }
    foreach ($Relative in $Files) {
        $File = Join-Path $Run $Relative
        if (Test-Path -LiteralPath $File -PathType Leaf) {
            $Destination = Join-Path $Public $Relative
            $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $Destination))
            [IO.File]::Copy($File,$Destination,$false)
        }
    }
    if ($Blocker -and -not (Test-Path -LiteralPath (Join-Path $Public 'first-error.log'))) {
        [IO.File]::WriteAllText((Join-Path $Public 'review-blocker.log'),$Blocker,(New-Object Text.UTF8Encoding($false)))
    }
    $Log = Join-Path $Run 'aab\build.log'
    if (Test-Path -LiteralPath $Log -PathType Leaf) {
        $LogInfo = Get-Item -LiteralPath $Log
        $PublicLogDirectory = Join-Path $Public 'aab'
        $null = [IO.Directory]::CreateDirectory($PublicLogDirectory)
        if ($LogInfo.Length -le 2097152) { [IO.File]::Copy($Log,(Join-Path $PublicLogDirectory 'build.log'),$false) }
        else {
            $Tail = Get-Content -LiteralPath $Log -Tail 800
            [IO.File]::WriteAllLines((Join-Path $PublicLogDirectory 'build-log-tail.log'),[string[]]$Tail,(New-Object Text.UTF8Encoding($false)))
        }
        $LogMeta = @{original_path=$Log;original_bytes=$LogInfo.Length;original_sha256=(Get-FileHash -LiteralPath $Log -Algorithm SHA256).Hash.ToLowerInvariant();shared_scope=$(if ($LogInfo.Length -le 2097152) {'full sanitized native log'} else {'last800 lines of sanitized native log; first native error is preserved separately when extracted'})}
        [IO.File]::WriteAllText((Join-Path $PublicLogDirectory 'build-log-info.json'),($LogMeta | ConvertTo-Json),(New-Object Text.UTF8Encoding($false)))
    }
    $Hashes = @(Get-ChildItem -LiteralPath $Public -File -Recurse | Sort-Object FullName | ForEach-Object {
        (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$_.FullName.Substring($Public.Length+1).Replace('\','/')
    })
    [IO.File]::WriteAllLines((Join-Path $Public 'public-checksums.sha256'),[string[]]$Hashes,(New-Object Text.UTF8Encoding($false)))
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $Zip = Join-Path $Run ('release-aab-public-'+[guid]::NewGuid().ToString('N')+'.zip')
    [IO.Compression.ZipFile]::CreateFromDirectory($Public,$Zip)
    $Review | ConvertTo-Json -Depth 6
    Write-Host ('PUBLIC ZIP: ' + $Zip)
    Write-Host 'STOP HERE. No APK or Pixel action. Keep this PowerShell window open. Do not rerun the build, preflight or key creation.'
}
```

## 実行後に共有するもの

表示された`PUBLIC ZIP`のZIPだけをこの会話に添付する。AAB本体、keystore、passwordを添付しない。結果ZIPをCodexが照合してから、固定共有先とlatestへ記録し通常commit/pushする。

ZIPには、存在する範囲のstatus/build-info/build-settings/source revision、native checksums、source checks、worktree作成記録、署名検証記録、Unity設定snapshot、AABのpath/name/size/SHA/signer/timestamp/exit codeを含む公開レビューJSONを収録する。AAB自体はEのrelease-candidate配下に残す。rootの`checksums.sha256`はnative原本であり、公開ZIPへ含めない大容量ログ等の項目もある。公開ZIPに収録したファイルの完全性は別の`public-checksums.sha256`で照合する。

nativeのbuild.logは既存スクリプトが秘密値を除去したものだけを扱う。2MiB以下なら全体を、超える場合は末尾800行と原本SHA/sizeを共有する。抽出された最初のnative errorは別のfirst-error.logとして保持する。throwや末尾だけで原因を断定せず、根本エラーが不足する場合は保存済み原本の該当箇所だけを確認する。

ビルド中はこのPowerShell画面を開いたまま待つ。画面の更新がなくても、もう一度実行しない。成功・失敗ともUnityを再起動せず、APK／Pixel／Google Play提出へ進まない。失敗時は既存runを保持し、共有ZIPまたは表示された具体的な停止内容を確認する。

現時点ではこのWindowsコマンドは未実行で、AAB結果・Unity exit・artifact SHA等は未確認。第8工程complete=false。公開手順の準備をAAB小工程合格とは扱わない。
