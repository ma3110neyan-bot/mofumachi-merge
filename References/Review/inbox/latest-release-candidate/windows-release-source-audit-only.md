# 既存AAB worktreeの差分だけを読み取り採取

対象は既存run `rc-20261109-123540-725e4e31`。Unity exit0・AAB生成・release設定は確認済みだが、source guardが追跡済み内容差分と未追跡56件を検知して停止した。署名検証は未到達。AABを再ビルドせず、まず既存worktreeの実際の差分を確認する。

元のPowerShellが入力待ちに戻った画面で、以下のブロック全体を1回コピーして実行する。既存releaseスクリプトは関数定義だけをdot-sourceし、既存の早期returnによりpreflight／Unity／ビルドを実行しない。Git操作はHEAD確認・status・diffだけ。`--no-optional-locks`、external diff/textconv無効で読み取り、正式repo・検証worktree・旧run・receiptを修復／削除／上書きしない。

結果は別のEドライブ診断フォルダーへ保存する。署名環境変数の値を表示せず、diff内に現在の秘密値があれば既存redaction関数で除去する。キー作成・環境変数再設定は不要。

```powershell
& {
    $ErrorActionPreference = 'Stop'
    $AuditRepo = 'C:\Users\User\Desktop\Mofumachi\mofumachi-merge'
    $AuditProject = 'E:\Mofumachi-Validation\projects\rc-20261109-123540-725e4e31'
    $AuditBaseline = 'c0803e6e9faf0ebb5e63e637d94c20dcce3f6e4a'
    if (-not (Test-Path -LiteralPath $AuditProject -PathType Container)) { throw 'STOP: the existing build worktree is missing. Do not recreate it.' }
    # Dot sourcing returns after helper definitions; it does not enter preflight or build execution.
    . (Join-Path $AuditRepo 'scripts\build-android-release.ps1')
    $AuditGit = Find-AndroidReleaseGit
    $AuditSecrets = @('MOFUMACHI_KEYSTORE_PATH','MOFUMACHI_KEYSTORE_PASSWORD','MOFUMACHI_KEY_ALIAS','MOFUMACHI_KEY_ALIAS_PASSWORD') |
        ForEach-Object {[Environment]::GetEnvironmentVariable($_,[EnvironmentVariableTarget]::Process)} |
        Where-Object {-not [string]::IsNullOrWhiteSpace($_)}
    $AuditHead = Invoke-AndroidReleaseGit $AuditGit $AuditProject @('rev-parse','HEAD')
    if ($AuditHead.exit_code -ne 0 -or $AuditHead.stdout.Trim() -cne $AuditBaseline) { throw 'STOP: existing worktree HEAD mismatch. No repair or build was attempted.' }
    $AuditRows = @(
        @{name='working-status';args=@('status','--porcelain=v1','--untracked-files=all')},
        @{name='tracked-name-status';args=@('diff','--no-ext-diff','--no-textconv','--no-color','--name-status',$AuditBaseline,'--')},
        @{name='tracked-content';args=@('diff','--no-ext-diff','--no-textconv','--no-color',$AuditBaseline,'--')},
        @{name='staged-content';args=@('diff','--cached','--no-ext-diff','--no-textconv','--no-color',$AuditBaseline,'--')}
    )
    $AuditDirectory = Join-Path 'E:\Mofumachi-Validation\Diagnostics' ('release-source-audit-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
    $AuditPublic = Join-Path $AuditDirectory 'public'
    $null = [IO.Directory]::CreateDirectory($AuditPublic)
    $AuditResults = @()
    foreach ($AuditRow in $AuditRows) {
        $AuditNative = Invoke-AndroidReleaseGit $AuditGit $AuditProject $AuditRow.args
        $AuditSafeOut = Protect-AndroidReleaseText $AuditNative.stdout @($AuditSecrets)
        $AuditSafeErr = Protect-AndroidReleaseText $AuditNative.stderr @($AuditSecrets)
        [IO.File]::WriteAllText((Join-Path $AuditPublic ($AuditRow.name+'.txt')),$AuditSafeOut,(New-Object Text.UTF8Encoding($false)))
        [IO.File]::WriteAllText((Join-Path $AuditPublic ($AuditRow.name+'.stderr.log')),$AuditSafeErr,(New-Object Text.UTF8Encoding($false)))
        $AuditResults += @{name=$AuditRow.name;exit_code=$AuditNative.exit_code}
    }
    $AuditReport = [ordered]@{
        scope='Read existing worktree status and actual tracked content differences only'
        original_run='rc-20261109-123540-725e4e31'
        project=$AuditProject
        baseline=$AuditBaseline
        head=$AuditHead.stdout.Trim()
        collected_utc=[DateTime]::UtcNow.ToString('o')
        commands=$AuditResults
        command_success=(@($AuditResults | Where-Object {$_.exit_code -ne 0}).Count -eq 0)
        preflight_invocations=0
        unity_invocations=0
        build_invocations=0
        automatic_retries=0
        restore_reset_stash_or_delete=$false
        secret_values_recorded=$false
        stage8_complete=$false
    }
    [IO.File]::WriteAllText((Join-Path $AuditPublic 'source-audit.json'),($AuditReport | ConvertTo-Json -Depth 6),(New-Object Text.UTF8Encoding($false)))
    $AuditHashes = @(Get-ChildItem -LiteralPath $AuditPublic -File | Sort-Object Name | ForEach-Object {
        (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$_.Name
    })
    [IO.File]::WriteAllLines((Join-Path $AuditPublic 'checksums.sha256'),[string[]]$AuditHashes,(New-Object Text.UTF8Encoding($false)))
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $AuditZip = Join-Path $AuditDirectory ('release-source-audit-public-'+[guid]::NewGuid().ToString('N')+'.zip')
    [IO.Compression.ZipFile]::CreateFromDirectory($AuditPublic,$AuditZip)
    $AuditReport | ConvertTo-Json -Depth 6
    Write-Host ('PUBLIC ZIP: '+$AuditZip)
    Write-Host 'STOP HERE. Existing worktree was read only. No preflight, Unity, build, source repair, reset or retry.'
}
```

表示されたPUBLIC ZIPだけをこの会話へ添付する。AAB本体・keystore・password・Libraryを送らない。`.gitignore`やallowlistも変更しない。

構文と呼出scopeだけを確認済み。Windows差分採取は未実行。返却ZIPのtracked-content.txtとtracked-name-status.txtで実際の差分を判断する。LF/CRLF警告だけで誤検知と断定しない。再ビルド・APK・Pixelへは進まない。
