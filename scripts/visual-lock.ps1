# Read-only gate. A matching hash protects frozen inputs; visual/input/performance
# acceptance still requires same-scene Unity and Pixel comparisons.
function Get-VisualLockDigest([byte[]]$Bytes) {
    $Hasher=[Security.Cryptography.SHA256]::Create()
    try {return ([BitConverter]::ToString($Hasher.ComputeHash($Bytes))).Replace('-','').ToLowerInvariant()} finally {$Hasher.Dispose()}
}
function Assert-BaseVisualLock([string]$RepositoryPath,[string]$ManifestPath) {
    if(-not(Test-Path -LiteralPath $ManifestPath -PathType Leaf)){throw 'Missing Visual Lock baseline.'}
    $Root=[IO.Path]::GetFullPath($RepositoryPath).TrimEnd('/','\')
    $Lock=Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
    if(@($Lock.files).Count -eq 0){throw 'Empty Visual Lock baseline.'}
    foreach($Entry in $Lock.files) {
        $Relative=[string]$Entry.path
        if([IO.Path]::IsPathRooted($Relative) -or $Relative -match '(^|[/\\])\.\.([/\\]|$)' -or $Relative.Contains(':')){throw 'Unsafe frozen input path.'}
        $Path=[IO.Path]::GetFullPath((Join-Path $Root $Relative))
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "Missing frozen input: $Relative"}
        if($Entry.mode -eq 'binary'){$Bytes=[IO.File]::ReadAllBytes($Path)}
        else {
            $Value=[IO.File]::ReadAllText($Path).Replace("`r`n","`n")
            if($Entry.mode -eq 'section') {
                $Start=$Value.IndexOf([string]$Entry.start,[StringComparison]::Ordinal)
                if($Start -lt 0){throw "Missing frozen section: $Relative"}
                $End=$Value.IndexOf([string]$Entry.end,$Start+([string]$Entry.start).Length,[StringComparison]::Ordinal)
                if($End -le $Start){throw "Missing frozen section end: $Relative"}
                $Value=$Value.Substring($Start,$End-$Start)
            } elseif($Entry.mode -ne 'lf-text'){throw "Unknown Visual Lock mode: $($Entry.mode)"}
            $Bytes=[Text.Encoding]::UTF8.GetBytes($Value)
        }
        if((Get-VisualLockDigest $Bytes) -ne $Entry.sha256){throw "Visual Lock changed: $Relative. Restore the frozen input; do not replace the baseline to bypass this gate."}
    }
}
function Write-VisualLockComparison([string]$RepositoryPath,[string]$RunDirectory,[string]$BeforeDirectory='',[string]$OutputDirectory='') {
    $Baseline=Get-Content -LiteralPath (Join-Path $RepositoryPath 'References/VisualLock/baseline-native-2026-10-21.json') -Raw | ConvertFrom-Json
    $AfterBuild=Get-Content -LiteralPath (Join-Path $RunDirectory 'build-info.json') -Raw | ConvertFrom-Json
    $After=Get-Content -LiteralPath (Join-Path $RunDirectory 'capture-manifest.json') -Raw | ConvertFrom-Json
    if($After.gitRevision -ne $AfterBuild.gitRevision -or $After.productFingerprint -ne $AfterBuild.productFingerprint){throw 'Visual comparison has mismatched capture/APK source.'}
    if(-not $OutputDirectory){$OutputDirectory=$RunDirectory}
    if(-not $BeforeDirectory){$BeforeDirectory=Join-Path $RepositoryPath ('Builds/Android/'+$Baseline.run)}
    $Record=[ordered]@{before_run=$Baseline.run;before_revision=$Baseline.build.gitRevision;candidate_revision=$AfterBuild.gitRevision;candidate_apk_sha256=$AfterBuild.sha256;native_pairs_verified=0;visual=$null;input=$null;information=$null;world=$null;fps=$null;pixel_load=$null;pixel_verified=$false;adopted=$false;reason='Native images require review; Pixel input/performance/save verification pending.'}
    if(-not(Test-Path -LiteralPath $BeforeDirectory -PathType Container)) {
        $Record.reason='Before images missing. APK generation is not visual acceptance; restore the recorded baseline images before adoption.'
        $Record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'visual-adoption.json') -Encoding UTF8
        Write-Host 'Before/After pending: baseline run directory is missing. Adoption remains blocked.'
        return
    }
    function Image-Directory([string]$Directory){$Images=Join-Path $Directory 'screenshots';if(Test-Path -LiteralPath $Images -PathType Container){return $Images};return $Directory}
    function Html([string]$Value){return [Net.WebUtility]::HtmlEncode($Value)}
    function File-Uri([string]$Path){return ([Uri]([IO.Path]::GetFullPath($Path))).AbsoluteUri}
    $BeforeImages=Image-Directory $BeforeDirectory;$AfterImages=Image-Directory $RunDirectory
    $Rows=New-Object 'System.Collections.Generic.List[string]'
    foreach($Image in $Baseline.capture.images) {
        if($Image.file -notmatch '^[A-Za-z0-9_.-]+\.png$'){throw 'Unsafe baseline image name.'}
        $BeforePath=Join-Path $BeforeImages $Image.file;$AfterPath=Join-Path $AfterImages $Image.file
        $New=@($After.images | Where-Object {$_.file -eq $Image.file})
        if($New.Count -ne 1 -or -not(Test-Path -LiteralPath $BeforePath) -or -not(Test-Path -LiteralPath $AfterPath)){throw "Missing comparison image: $($Image.file)"}
        if((Get-FileHash -LiteralPath $BeforePath -Algorithm SHA256).Hash -ne $Image.sha256 -or (Get-FileHash -LiteralPath $AfterPath -Algorithm SHA256).Hash -ne $New[0].sha256){throw "Comparison image hash mismatch: $($Image.file)"}
        $Same=if($Image.sha256 -eq $New[0].sha256){' / byte-identical; retained'}else{''}
        $Rows.Add('<section><h2>'+ (Html ($Image.file+$Same)) +'</h2><div class="pair"><figure><figcaption>Before</figcaption><img loading="lazy" src="'+(Html (File-Uri $BeforePath))+'"></figure><figure><figcaption>After candidate / not accepted</figcaption><img loading="lazy" src="'+(Html (File-Uri $AfterPath))+'"></figure></div></section>')
    }
    $Record.native_pairs_verified=$Rows.Count
    $Reference=File-Uri (Join-Path $RepositoryPath 'a_colorful_promotional_collage_ui_concept_board.png')
    $Header='<!doctype html><html lang="ja"><meta charset="utf-8"><title>もふまち Visual Lock 比較</title><style>body{font-family:sans-serif;background:#fff6ef;color:#51383c;max-width:1100px;margin:24px auto}h1{color:#c33366}.pending{background:#ffe4db;padding:16px}.pair{display:flex;gap:16px}figure{margin:0;flex:1}img{width:100%;height:auto}section{margin:36px 0}figcaption{padding:8px;background:#d9f1e5}.reference{max-height:400px;object-fit:contain}</style><h1>Visual Lock / 実Unity Before・After候補</h1><p class="pending">未採用。静止画だけでは操作・FPS・Pixel負荷を合格にしません。全6軸の確認とPixel確認が揃うまで採用を保留します。</p><p>同名の隔離撮影工程・地区・Lv・720×1280。撮影日による今日の相手、NPC位置／表情は異なる場合があります。元絵を描き換えた比較ではありません。</p><p>Before: '+(Html $Baseline.build.gitRevision)+' / After candidate: '+(Html $AfterBuild.gitRevision)+'</p><h2>9月22日デザイン基準</h2><img class="reference" src="'+(Html $Reference)+'">'
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'before-after.html'),$Header+($Rows -join "`n")+'</html>')
    $Record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'visual-adoption.json') -Encoding UTF8
    Write-Host "Native comparison: $OutputDirectory\before-after.html. Pixel/visual adoption remains pending."
}
function Assert-BaseAdoption([string]$RecordPath) {
    $Record=Get-Content -LiteralPath $RecordPath -Raw | ConvertFrom-Json
    foreach($Axis in @('visual','input','information','world','fps','pixel_load','pixel_verified')) {
        if($Record.$Axis -isnot [bool] -or $Record.$Axis -ne $true){throw "Base adoption blocked: $Axis has not passed."}
    }
    if([int]$Record.native_pairs_verified -lt 102){throw 'Base adoption blocked: same-scene comparison is incomplete.'}
}
