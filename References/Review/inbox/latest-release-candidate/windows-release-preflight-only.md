# 同じPowerShellでrelease preflightだけを1回実行

署名作成に成功したPowerShell画面をそのまま使用する。新しいPowerShell／CMDを開かず、キー作成CMDを再実行しない。

以下のブロック全体を1回だけコピーして、そのPowerShellへ貼り付けて実行する。4環境変数はSET/NOT SETだけ表示し、keystore SHAを先に照合する。既存build-android-release.ps1が正式repo／branch／ancestor／Visual Lock／QA／profile／JDK／署名を確認する。Build指定なしで呼ぶのは1回だけ。

終了後、certificate SHA／keystore SHA／Unity0／source不変等を照合し、公開結果だけのZIPを同じE側runへ作る。AAB/APK／Pixelへ進むコマンドは含めていない。

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
    $KeyBefore = (Get-FileHash -LiteralPath $env:MOFUMACHI_KEYSTORE_PATH -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($KeyBefore -cne $ExpectedKey) { throw 'STOP: keystore SHA256 mismatch. No preflight or build was started.' }
    $Root = 'E:\Mofumachi-Validation\Diagnostics\release-candidate'
    $Before = @(if (Test-Path -LiteralPath $Root) { Get-ChildItem -LiteralPath $Root -Directory | ForEach-Object {$_.FullName} })
    $global:LASTEXITCODE = 0
    & (Join-Path $Repo 'scripts\build-android-release.ps1')
    $Code = $global:LASTEXITCODE
    $New = @(Get-ChildItem -LiteralPath $Root -Directory | Where-Object {$Before -notcontains $_.FullName})
    if ($New.Count -ne 1) { throw 'STOP: cannot identify exactly one new run. Do not rerun preflight.' }
    $Run = $New[0].FullName
    $StatusPath = Join-Path $Run 'status.json'
    if (-not (Test-Path -LiteralPath $StatusPath -PathType Leaf)) { throw ('STOP: terminal status missing. Do not rerun. Run: ' + $Run) }
    $S = [IO.File]::ReadAllText($StatusPath) | ConvertFrom-Json
    $KeyAfter = (Get-FileHash -LiteralPath $env:MOFUMACHI_KEYSTORE_PATH -Algorithm SHA256).Hash.ToLowerInvariant()
    $Formal = ($S.PSObject.Properties.Name -contains 'formal_repo_unchanged') -and ($S.formal_repo_unchanged -eq $true)
    $CertMatches = $S.signing_certificate_sha256 -ceq $ExpectedCert
    $KeyMatches = ($KeyBefore -ceq $ExpectedKey) -and ($KeyAfter -ceq $ExpectedKey)
    $RevisionPath = Join-Path $Run 'source-revision.txt'
    $RevisionMatches = (Test-Path -LiteralPath $RevisionPath -PathType Leaf) -and ([IO.File]::ReadAllText($RevisionPath).Trim() -ceq $S.source_commit)
    $Passed = ($Code -eq 0) -and ($S.result -ceq 'PreflightPassed') -and ($S.unity_invocations -eq 0) -and
        ($S.build_requested -eq $false) -and ($null -eq $S.aab) -and ($null -eq $S.apk) -and
        ($S.source_unchanged -eq $true) -and $Formal -and $CertMatches -and $KeyMatches -and
        $RevisionMatches -and ($S.package -ceq 'com.mofumachi.merge') -and ($S.unity_version -ceq '6000.6.4f1') -and
        ($S.automatic_retries -eq 0) -and ($null -eq $S.first_error) -and ($null -eq $S.runner_error)
    $Blocker = $null
    if (-not $Passed) {
        if ($S.first_error) { $Blocker = $S.first_error }
        elseif ($S.runner_error) { $Blocker = $S.runner_error }
        elseif (-not $CertMatches) { $Blocker = 'Certificate SHA256 mismatch.' }
        elseif (-not $KeyMatches) { $Blocker = 'Keystore SHA256 mismatch.' }
        else { $Blocker = 'The saved preflight did not satisfy every requested zero-build/source/exit condition.' }
        $ErrorPath = Join-Path $Run 'first-error.log'
        if (-not (Test-Path -LiteralPath $ErrorPath)) { [IO.File]::WriteAllText($ErrorPath,$Blocker,(New-Object Text.UTF8Encoding($false))) }
    }
    $Review = [ordered]@{
        preflight = $(if ($Passed) {'PASS'} else {'FAIL'})
        preflight_exit_code = $Code
        source_commit = $S.source_commit
        certificate_sha256 = $S.signing_certificate_sha256
        certificate_matches = $CertMatches
        keystore_sha256 = $KeyAfter
        keystore_matches = $KeyMatches
        four_environment_variables = 'SET'
        source_unchanged = $S.source_unchanged
        formal_repo_unchanged = $Formal
        unity_invocations = $S.unity_invocations
        build_requested = $S.build_requested
        build_invocations = $(if (($S.build_requested -eq $false) -and ($S.unity_invocations -eq 0)) {0} else {$null})
        automatic_retries = $S.automatic_retries
        first_error = $S.first_error
        runner_error = $S.runner_error
        blocking_issue = $Blocker
        stage8_complete = $false
    }
    $ReviewPath = Join-Path $Run 'preflight-review.json'
    [IO.File]::WriteAllText($ReviewPath,($Review | ConvertTo-Json -Depth 5),(New-Object Text.UTF8Encoding($false)))
    $Files = @('status.json','build-info.json','build-settings.json','signing-preflight.log','source-revision.txt','checksums.sha256','first-error.log','preflight-review.json') |
        ForEach-Object {Join-Path $Run $_} | Where-Object {Test-Path -LiteralPath $_ -PathType Leaf}
    $Zip = Join-Path $Run ('release-preflight-public-'+[guid]::NewGuid().ToString('N')+'.zip')
    Compress-Archive -LiteralPath @($Files) -DestinationPath $Zip
    $Review | ConvertTo-Json -Depth 5
    Write-Host ('PUBLIC ZIP: ' + $Zip)
    Write-Host 'STOP HERE. No AAB/APK or Pixel action. Keep this PowerShell window open; do not rerun preflight.'
}
```

## 実行後

画面に表示されたPUBLIC ZIPのファイルだけをこの会話へ添付する。ZIPにはstatus、build-info、build-settings、署名確認log、source revision、checksums、必要時のfirst-error、公開レビューJSONだけを含め、keystore／passwordファイルは入れない。

失敗・停止してもコマンドを再実行しない。保存済みの公開結果だけを確認・共有する。PowerShell画面は開いたまま保持する。

Codex側でこのWindowsコマンドは未実行。現在はユーザー操作と公開結果待ちで、preflightのPASS/FAILやexit codeを先取りして記録しない。共有された公開ZIPを照合後、固定GitHub記録へ保存・pushする。
