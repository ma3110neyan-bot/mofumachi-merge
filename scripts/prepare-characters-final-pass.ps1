[CmdletBinding()]
param([string]$UnityEditorPath='',[switch]$Install,[string]$Serial='99RAY1BELH',[ValidateRange(0,600)][int]$MeasureSeconds=600)
$ErrorActionPreference='Stop'
# Preserve caller options before dot-sourcing older parameterised helpers.
$FinalPassOptions=@{Editor=$UnityEditorPath;Install=[bool]$Install;Serial=$Serial;Seconds=$MeasureSeconds}
. (Join-Path $PSScriptRoot 'preserve-characters-prepared-inputs.ps1')
if($MyInvocation.InvocationName -eq '.'){return}
$FinalRepository=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$FinalGit=Find-SourceGit
$Preserved=Move-PreviousCharacterInputs $FinalRepository $FinalGit
$Diagnostics=Join-Path $FinalRepository 'Builds/Diagnostics'
New-Item -ItemType Directory -Path $Diagnostics -Force | Out-Null
$Previous=@(Get-ChildItem -LiteralPath $Diagnostics -Directory | Select-Object -ExpandProperty FullName)
$FinalRun=$null;$FinalArchive=$null
try{
    # Candidate is ready for final acceptance. Existing pipeline runs all five
    # singles sequentially, full suites once, six fixture, then the same-source
    # pair. Its first physical run is the baseline; authored is installed last.
    & (Join-Path $PSScriptRoot 'prepare-characters-pixel-preview.ps1') -UnityEditorPath $FinalPassOptions.Editor -Install:$FinalPassOptions.Install -Mode baseline -Serial $FinalPassOptions.Serial -MeasureSeconds $FinalPassOptions.Seconds
    $New=@(Get-ChildItem -LiteralPath $Diagnostics -Directory | Where-Object {$_.Name -cmatch '^characters-authored-[0-9]{8}-[0-9]{6}-[a-f0-9]{8}$' -and $_.FullName -notin $Previous})
    if($New.Count -ne 1){throw 'Exact new final-pass parent directory not identified.'}
    $FinalRun=$New[0].FullName;$Pair=Read-CharactersPair $FinalRun
    $Source=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $FinalRun 'source-info.json') | ConvertFrom-Json
    $Manual=[ordered]@{six_on_same_screen='unknown';grounding_momo_miel='unknown';size_feet='unknown';directions_walk_stop='unknown';sorting='unknown';save_after_restart='unknown'}
    if($FinalPassOptions.Install){
        Invoke-CharactersPixel $FinalRun 'authored' $FinalPassOptions.Serial $FinalPassOptions.Seconds
        Write-Host 'Final authored app is installed. These answers record observations; unknown is allowed and keeps acceptance pending.'
        $Manual.six_on_same_screen=Read-Host 'Six characters visible together on the actual Pixel town screen: yes / no / unknown (individual views or Unity fixture do not count)'
        $Manual.grounding_momo_miel=Read-Host 'Momo/Miel feet and lateral movement: grounded / floating / unknown'
        $Manual.size_feet=Read-Host 'Six-character sizes/foot positions: natural / inconsistent / unknown'
        $Manual.directions_walk_stop=Read-Host 'Front/back/left/right, walking/turning/stopping: natural / issue / unknown'
        $Manual.sorting=Read-Host 'Occlusion near fountain, trees, benches, buildings, flowers: natural / issue / unknown'
        $Manual.save_after_restart=Read-Host 'After closing and reopening the same app, progress retained: yes / no / unknown'
    }
    $Bundle=$FinalRun+'-final-review';New-Item -ItemType Directory -Path $Bundle | Out-Null
    $Record=[ordered]@{kind='six-character-final-pass-candidate';gitRevision=$Source.gitRevision;productFingerprint=$Source.productFingerprint;original_run=$FinalRun;preserved_inputs=$Preserved;full_qa_verified=$Source.full_qa_verified;six_native_verified=$Source.six_native_verified;manual=$Manual;device_runs=@();pixel_verified=$false;formal_fixed=$false;adopted=$false;scope='Momo/Miel contact shadows only; Noah master and original art unchanged';archive_scope='Original final suite records, six rendered views, Momo/Miel shadow comparisons and device data. Other single-character full image archives remain in their original runs.'}
    foreach($Name in @('source-info.json','context.json','prepared-six.json','characters-baseline-build.json','characters-authored-build.json','full-editmode.xml','full-playmode.xml','six-playmode.xml')){Copy-Item -LiteralPath (Join-Path $FinalRun $Name) -Destination $Bundle}
    Copy-Item -LiteralPath (Join-Path $FinalRun 'screenshots/six-authored') -Destination $Bundle -Recurse
    foreach($Single in $Source.singleRuns){
        $ProofFiles=@(Get-ChildItem -LiteralPath (Join-Path $Single 'screenshots') -Filter proof.json -Recurse)
        if($ProofFiles.Count -ne 1){throw 'Single proof identity is ambiguous.'}
        $Proof=Get-Content -Raw -Encoding UTF8 -LiteralPath $ProofFiles[0].FullName | ConvertFrom-Json
        Assert-CharacterAuthoredProof $Single $Source.gitRevision $Source.productFingerprint $Proof.character
        $Target=Join-Path $Bundle $Proof.character;New-Item -ItemType Directory -Path $Target | Out-Null
        Copy-Item -LiteralPath $ProofFiles[0].FullName -Destination $Target
        if($Proof.character -cin @('momo','miel')){Get-ChildItem -LiteralPath $ProofFiles[0].DirectoryName -Filter 'grounding-*' -File | Copy-Item -Destination $Target}
    }
    foreach($Kind in @('baseline','authored')){
        $DeviceDirs=@(Get-ChildItem -LiteralPath $FinalRun -Directory -Filter ('pixel-'+$Kind+'-*'))
        if($FinalPassOptions.Install -and $DeviceDirs.Count -ne 1){throw 'Exact new device evidence directory required.'}
        foreach($DeviceDir in $DeviceDirs){
            $DeviceRecord=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $DeviceDir.FullName 'characters-pixel.json') | ConvertFrom-Json
            if($DeviceRecord.gitRevision -cne $Source.gitRevision -or $DeviceRecord.expected_apk_sha256 -cne $Pair[$Kind].sha256 -or $DeviceRecord.error -or -not $DeviceRecord.installation_verified -or -not $DeviceRecord.renderer_mode_verified -or -not $DeviceRecord.same_process){throw 'Device/source/installation/process identity failed.'}
            $Record.device_runs+=@($DeviceDir.FullName)
            Copy-Item -LiteralPath $DeviceDir.FullName -Destination (Join-Path $Bundle ('device-'+$Kind)) -Recurse
        }
    }
    $Record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $Bundle 'final-pass.json') -Encoding UTF8
    $Archives=@(Write-CharactersReviewArchives $Bundle);$FinalArchive=$Archives[0]
    foreach($Zip in $Archives){Write-Host "Final-pass evidence ZIP: $Zip"}
    Write-Host 'Candidate evidence prepared. Formal freeze remains pending review of current native images and physical Pixel results.'
}catch{Write-Host 'Final pass stopped. Preserve this run and its exact diagnostics; do not rerun full QA automatically.';throw}
finally{
    if(-not $FinalArchive){
        $New=@(Get-ChildItem -LiteralPath $Diagnostics -Directory | Where-Object {$_.Name -cmatch '^characters-authored-[0-9]{8}-[0-9]{6}-[a-f0-9]{8}$' -and $_.FullName -notin $Previous})
        if($New.Count -eq 1){$Zips=@(Get-ChildItem -LiteralPath $Diagnostics -File -Filter ($New[0].Name+'-review-*.zip') | Sort-Object Name);if($Zips.Count){$FinalArchive=$Zips[0].FullName}}
    }
    if($FinalArchive -and $env:OS -eq 'Windows_NT'){Start-Process -FilePath explorer.exe -ArgumentList ('/select,"'+$FinalArchive+'"')}
}
