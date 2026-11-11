# 未取得のB/C実体byteを既存ファイルから採取する手順（未実行）

この調査のCategory Eを解消するための次の読取り候補。今回のクラウド調査ではWindows Eに接続できないため、実行済みと扱わない。元のAABスクリプト・テスト・Unity・preflight・signingを呼ばない。署名環境変数は不要。

Windowsの確認用PowerShellへ以下を1回貼り付けると、既存snapshot-2.bin、current ProjectSettings.asset、保存済みdiffをReadAllBytesで読み、SHA/size/encoding/BOM/改行/graphics block/raw hexだけを新しいE Diagnosticsへ保存する。source/snapshot本体はコピー・変換・修復しない。native run、receipt、旧Diagnosticsを上書き・削除しない。新規診断ファイルの作成だけを行う。

API byte位置とhexは元byte配列から取り、UTF-8/UTF-16のデコードは観察用。encodingを判定できない場合はunknownとし、source変換を試みない。ASCII-onlyの場合はANSI/UTF-8を一意判別できない。現在のCが当時のCと同一かも、mtime/hashとDから別途照合する。

```powershell
& {
    $ErrorActionPreference = 'Stop'
    $RunId = 'rc-20261110-132609-c6829dc7'
    $Run = 'E:\Mofumachi-Validation\Diagnostics\release-candidate\'+$RunId
    $Project = 'E:\Mofumachi-Validation\projects\'+$RunId

    function Assert-NoAuditLink([string]$Path) {
        $P = [IO.Path]::GetFullPath($Path)
        while ($P) {
            if ((Test-Path -LiteralPath $P) -and
                ((Get-Item -LiteralPath $P -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                throw 'Linked input/output path rejected; no source changed.'
            }
            $P = [IO.Path]::GetDirectoryName($P)
        }
    }
    function Get-AuditSha([byte[]]$Data) {
        $H = [Security.Cryptography.SHA256]::Create()
        try { return [BitConverter]::ToString($H.ComputeHash($Data)).Replace('-','').ToLowerInvariant() }
        finally { $H.Dispose() }
    }
    function Describe-AuditFile([string]$Label,[string]$Path,[bool]$Diff) {
        Assert-NoAuditLink $Path
        if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
            return [pscustomobject]@{state=$Label;path=$Path;exists=$false}
        }
        $Before = Get-Item -LiteralPath $Path -Force
        $BeforeLength = $Before.Length
        $BeforeWrite = $Before.LastWriteTimeUtc
        if ($BeforeLength -gt 262144) { throw 'Unexpected input size; no source changed.' }
        $Bytes = [IO.File]::ReadAllBytes($Path)
        $After = Get-Item -LiteralPath $Path -Force
        $Bom = 'none'
        $Encoding = New-Object Text.UTF8Encoding($false,$true)
        if ($Bytes.Length -ge 4 -and $Bytes[0] -eq 255 -and $Bytes[1] -eq 254 -and $Bytes[2] -eq 0 -and $Bytes[3] -eq 0) {
            $Bom = 'UTF-32LE'
            $Encoding = New-Object Text.UTF32Encoding($false,$true,$true)
        } elseif ($Bytes.Length -ge 4 -and $Bytes[0] -eq 0 -and $Bytes[1] -eq 0 -and $Bytes[2] -eq 254 -and $Bytes[3] -eq 255) {
            $Bom = 'UTF-32BE'
            $Encoding = New-Object Text.UTF32Encoding($true,$true,$true)
        } elseif ($Bytes.Length -ge 3 -and $Bytes[0] -eq 239 -and $Bytes[1] -eq 187 -and $Bytes[2] -eq 191) {
            $Bom = 'UTF-8'
        } elseif ($Bytes.Length -ge 2 -and $Bytes[0] -eq 255 -and $Bytes[1] -eq 254) {
            $Bom = 'UTF-16LE'
            $Encoding = New-Object Text.UnicodeEncoding($false,$true,$true)
        } elseif ($Bytes.Length -ge 2 -and $Bytes[0] -eq 254 -and $Bytes[1] -eq 255) {
            $Bom = 'UTF-16BE'
            $Encoding = New-Object Text.UnicodeEncoding($true,$true,$true)
        }
        $Report = [ordered]@{
            state=$Label;path=$Path;exists=$true;sha256=(Get-AuditSha $Bytes);byte_length=$Bytes.Length
            bom=$Bom;created_utc=$Before.CreationTimeUtc.ToString('o');modified_utc=$BeforeWrite.ToString('o')
            changed_during_read=($BeforeLength -ne $After.Length -or $BeforeWrite -ne $After.LastWriteTimeUtc)
            source_written=$false;api_values=@()
        }
        try { $Text = $Encoding.GetString($Bytes) }
        catch {
            $Report['encoding']='Unknown or invalid under BOM/strict UTF-8; no conversion attempted'
            return [pscustomobject]$Report
        }
        $Ascii = @($Bytes | Where-Object {$_ -gt 127}).Count -eq 0
        $Report['encoding'] = $(if ($Bom -ne 'none') {$Bom} elseif ($Ascii) {'ASCII-only; UTF-8-compatible; original ANSI/UTF-8 choice not distinguishable'} else {'Valid UTF-8 without BOM'})
        $Report['crlf_count'] = [regex]::Matches($Text,"`r`n").Count
        $Report['bare_lf_count'] = [regex]::Matches($Text,'(?<!\r)\n').Count
        $Report['bare_cr_count'] = [regex]::Matches($Text,'\r(?!\n)').Count
        $Pattern = '(?m)^  - m_BuildTarget: Android\r?\n    m_APIs: (?<api>[^\r\n]*)\r?\n    m_Automatic: [01]\r?$'
        if ($Diff) { $Pattern = '(?m)^[+-]    m_APIs: (?<api>[^\r\n]*)\r?$' }
        foreach ($Match in [regex]::Matches($Text,$Pattern)) {
            $Value = $Match.Groups['api']
            $Offset = $Encoding.GetByteCount($Text.Substring(0,$Value.Index))
            $Length = $Encoding.GetByteCount($Value.Value)
            if ($Length -eq 0) { continue }
            $RawValue = New-Object byte[] $Length
            [Array]::Copy($Bytes,$Offset,$RawValue,0,$Length)
            $Start = [Math]::Max(0,$Offset+$Length-1-16)
            $Count = [Math]::Min(32,$Bytes.Length-$Start)
            $Context = New-Object byte[] $Count
            [Array]::Copy($Bytes,$Start,$Context,0,$Count)
            $Points = @()
            for ($I=0;$I -lt $Value.Value.Length;$I++) {
                $Point = [char]::ConvertToUtf32($Value.Value,$I)
                $Points += ('U+{0:X4}' -f $Point)
                if ([char]::IsHighSurrogate($Value.Value[$I])) { $I++ }
            }
            $Report.api_values += @{
                value=$Value.Value;raw_hex=[BitConverter]::ToString($RawValue).Replace('-',' ')
                last_raw_byte=('{0:X2}' -f $RawValue[$RawValue.Length-1]);code_points=$Points
                byte_offset=$Offset;context_start=$Start;context_hex=[BitConverter]::ToString($Context).Replace('-',' ')
            }
        }
        if (-not $Diff) {
            $Block = [regex]::Match($Text,'(?ms)^  m_BuildTargetGraphicsAPIs:\r?\n.*?(?=^  [A-Za-z_][^\r\n]*:|\z)')
            $Report['graphics_api_block'] = $(if ($Block.Success) {$Block.Value} else {$null})
        }
        return [pscustomobject]$Report
    }
    function Write-NewAuditText([string]$Path,[string]$Text) {
        $Bytes = (New-Object Text.UTF8Encoding($false)).GetBytes($Text)
        $Stream = [IO.File]::Open($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        try { $Stream.Write($Bytes,0,$Bytes.Length) } finally { $Stream.Dispose() }
    }

    $Rows = @(
        Describe-AuditFile 'B_prebuild_snapshot' (Join-Path $Run 'aab\source-recovery\snapshot-2.bin') $false
        Describe-AuditFile 'C_current_postbuild_file' (Join-Path $Project 'ProjectSettings\ProjectSettings.asset') $false
        Describe-AuditFile 'D_saved_diff' (Join-Path $Run 'aab\source-recovery\pre-restore.diff') $true
    )
    $Directory = 'E:\Mofumachi-Validation\Diagnostics\release-graphics-api-audit-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N')
    Assert-NoAuditLink $Directory
    if (Test-Path -LiteralPath $Directory) { throw 'Unique audit directory already exists.' }
    $null = [IO.Directory]::CreateDirectory($Directory)
    Write-NewAuditText (Join-Path $Directory 'projectsettings-comparison.json') (([ordered]@{
        run_id=$RunId;native_build_source='41529cfaea57ab564da46b3a45c41ede0aa5088b'
        collected_utc=[DateTime]::UtcNow.ToString('o');physical_files_read=$Rows
        expected_snapshot_sha256='d334127167318fe7278575eb17fe2621ab2c0e39c3ab2c53b7c1f39cc555e7fc'
        expected_saved_diff_sha256='e30c5865ddbe65a32cd3e764973a3bde9903bf63f886e0cdff96fe2155a46f4c'
        source_changes=0;unity_invocations=0;build_invocations=0;test_invocations=0;signing_operations=0
    }) | ConvertTo-Json -Depth 12)
    Write-NewAuditText (Join-Path $Directory 'summary.txt') 'Existing B/C/D files read once. Only public hash/encoding/graphics-block metadata exported. No source or snapshot bodies copied. No Unity/build/test/restore/cleanup/signing operation.'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $Zip = $Directory+'-public.zip'
    [IO.Compression.ZipFile]::CreateFromDirectory($Directory,$Zip)
    Write-Host ('PUBLIC AUDIT ZIP: '+$Zip)
    Write-Host 'STOP HERE. Do not rerun the build, edit settings, restore, cleanup or change the guard.'
}
```

PUBLIC AUDIT ZIPだけを添付する。snapshot-2.bin・ProjectSettings全文・AAB・keystore・passwordは添付しない。採取後もUnity/ビルド/修復へ進まない。
