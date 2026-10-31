[CmdletBinding()]
param([string]$UnityEditorPath='',[switch]$Install,[string]$BuildDirectory='',[ValidateSet('baseline','authored')][string]$Mode='authored',[string]$Serial='99RAY1BELH',[ValidateRange(0,600)][int]$MeasureSeconds=600)
$ErrorActionPreference='Stop'
$RolloutOptions=@{Editor=$UnityEditorPath;Install=[bool]$Install;Directory=$BuildDirectory;Mode=$Mode;Serial=$Serial;Seconds=$MeasureSeconds}
. (Join-Path $PSScriptRoot 'capture-character-authored.ps1')
. (Join-Path $PSScriptRoot 'test-noah-authored-pixel.ps1')
function Copy-CharactersFailedSingleRecords([string]$Repository,[string]$Parent,[string]$Single,[string]$Revision,[string]$Fingerprint){
    $Repository=[IO.Path]::GetFullPath($Repository).TrimEnd('/','\');$Diagnostics=Join-Path $Repository 'Builds/Diagnostics'
    $Parent=[IO.Path]::GetFullPath($Parent);$Single=[IO.Path]::GetFullPath($Single)
    if(-not [string]::Equals((Split-Path -Parent $Single),$Diagnostics,[StringComparison]::OrdinalIgnoreCase) -or -not [string]::Equals((Split-Path -Parent $Parent),$Diagnostics,[StringComparison]::OrdinalIgnoreCase) -or (Split-Path -Leaf $Single) -cnotmatch '^(momo|lulu|pom|fio|miel)-authored-[0-9]{8}-[0-9]{6}-[a-f0-9]{8}$' -or (Split-Path -Leaf $Parent) -cnotmatch '^characters-authored-[0-9]{8}-[0-9]{6}-[a-f0-9]{8}$'){throw 'Exact parent and failed single directories under diagnostics required.'}
    $Id=(Split-Path -Leaf $Single).Split('-')[0]
    $Files=@(foreach($Name in @('source-info.json','playmode.xml','playmode.log','prepare.log','prepared.json',('prepared-'+$Id+'.json'))){$File=Join-Path $Single $Name;if(Test-Path -LiteralPath $File -PathType Leaf){Get-Item -LiteralPath $File -Force}})
    # Validate ancestry and each whitelisted record before reading or creating
    # the destination. Never traverse a link to diagnose a different run.
    foreach($Path in @($Parent,$Single)+@($Files.FullName)){
        $At=$Path
        while($At.Length -gt $Repository.Length){
            if(Test-Path -LiteralPath $At){if((Get-Item -LiteralPath $At -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked failure diagnostics are preserved in place.'}}
            $At=Split-Path -Parent $At
        }
    }
    $Source=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Single 'source-info.json') | ConvertFrom-Json
    if($Source.gitRevision -cne $Revision -or $Source.productFingerprint -cne $Fingerprint){throw 'Failed-single source identity differs from this parent attempt.'}
    $Target=Join-Path $Parent 'failed-single';if(Test-Path -LiteralPath $Target){throw 'Preserve existing failed-single diagnostic copy.'}
    New-Item -ItemType Directory -Path $Target | Out-Null
    $Rows=@(foreach($File in $Files){
        $Hash=(Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $Destination=Join-Path $Target $File.Name;Copy-Item -LiteralPath $File.FullName -Destination $Destination
        if((Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash -ine $Hash){throw 'Failed-single copied bytes differ.'}
        @{file=$File.Name;bytes=$File.Length;sha256=$Hash}
    })
    @{original_single_run=$Single;gitRevision=$Revision;productFingerprint=$Fingerprint;passed=$false;adopted=$false;pixel_verified=$false;files=$Rows} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Target 'failure-context.json') -Encoding UTF8
    return $Target
}
function Get-CharactersFullArguments([string]$Repository,[string]$Directory,[string]$Platform,[string]$Revision,[string]$Fingerprint){
    if($Platform -cnotin @('EditMode','PlayMode') -or $Revision -notmatch '^[a-f0-9]{40}$' -or $Fingerprint -notmatch '^[a-f0-9]{64}$'){throw 'Invalid full QA identity/platform.'}
    return @('-batchmode','-projectPath',$Repository,'-buildTarget','Android','-runTests','-testPlatform',$Platform,'-testResults',(Join-Path $Directory ('full-'+$Platform.ToLowerInvariant()+'.xml')),'-logFile',(Join-Path $Directory ('full-'+$Platform.ToLowerInvariant()+'.log')),'-mofumachiScreenshots',(Join-Path $Directory 'full-screenshots'),'-mofumachiSourceRevision',$Revision,'-mofumachiProductFingerprint',$Fingerprint)
}
function Get-CharactersSixArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint){
    if($Revision -notmatch '^[a-f0-9]{40}$' -or $Fingerprint -notmatch '^[a-f0-9]{64}$'){throw 'Invalid six-character identity.'}
    return @('-batchmode','-projectPath',$Repository,'-buildTarget','Android','-runTests','-testPlatform','PlayMode','-testFilter','Mofumachi.Tests.ExperienceCaptureTests.SixAuthoredCharactersPreserveWorldAndInput','-testResults',(Join-Path $Directory 'six-playmode.xml'),'-logFile',(Join-Path $Directory 'six-playmode.log'),'-mofumachiScreenshots',(Join-Path $Directory 'screenshots'),'-mofumachiCharactersDirectory',$Directory,'-mofumachiSourceRevision',$Revision,'-mofumachiProductFingerprint',$Fingerprint,'-mofumachiCharactersSixPreview')
}
function Assert-CharactersRunningMode([string]$Raw,[string]$Expected,[long]$Since,[string]$ProcessId){
    if($Expected -cnotin @('baseline','authored') -or $ProcessId -notmatch '^[1-9][0-9]*$'){throw 'Unknown running mode.'}
    $Seen=@{};$Noah=''
    foreach($Line in ($Raw -split "`r?`n")){
        if($Line -notmatch '^\s*(\d+\.\d+)\s+(\d+)\s+\d+\s+[VDIWEF]\s+Unity\s*:' -or [double]$Matches[1] -lt $Since -or $Matches[2] -cne $ProcessId){continue}
        if($Line -match 'MOFUMACHI_NOAH_AUTHORED_MODE (authored|current)\b'){$Noah=$Matches[1]}
        if($Line -match 'MOFUMACHI_CHARACTER_AUTHORED_MODE (momo|lulu|pom|fio|miel) (authored|current)\b'){$Seen[$Matches[1]]=$Matches[2]}
    }
    $Want=if($Expected -eq 'authored'){'authored'}else{'current'}
    if($Noah -cne 'authored'){throw 'Accepted Noah mode is absent/wrong in this launch.'}
    foreach($Id in @(Get-CharacterAuthoredIds)){if($Seen[$Id] -cne $Want){throw "New launch mode absent/wrong: $Id. Do not attribute an old screen to the new APK."}}
}
function Read-CharactersPair([string]$Directory){
    $Source=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'source-info.json') | ConvertFrom-Json
    foreach($Flag in @('full_qa_verified','single_native_verified','six_native_verified','builds_generated','source_unchanged')){if($Source.$Flag -isnot [bool] -or -not $Source.$Flag){throw "Unfinished rollout: $Flag"}}
    if($Source.error){throw 'This build attempt failed; preserve its diagnostics.'}
    $Pair=@{}
    foreach($Kind in @('baseline','authored')){
        $Info=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory "characters-$Kind-build.json") | ConvertFrom-Json
        if($Info.mode -cne $Kind -or $Info.package -cne 'com.mofumachi.merge.qa.townlife' -or $Info.apk -cne "characters-$Kind.apk" -or $Info.gitRevision -cne $Source.gitRevision -or $Info.productFingerprint -cne $Source.productFingerprint -or $Info.packed_resource_sha256 -notmatch '^[a-f0-9]{64}$'){throw 'Build/source/package identity mismatch.'}
        foreach($Flag in @('full_qa_verified','six_native_verified')){if($Info.$Flag -isnot [bool] -or -not $Info.$Flag){throw 'Native build gates incomplete.'}}
        foreach($Flag in @('adopted','pixel_verified')){if($Info.$Flag -isnot [bool] -or $Info.$Flag){throw 'Build cannot claim device acceptance.'}}
        if($Info.sha256 -notmatch '^[a-f0-9]{64}$' -or (Get-FileHash -LiteralPath (Join-Path $Directory $Info.apk) -Algorithm SHA256).Hash -ine $Info.sha256){throw 'APK bytes do not match.'};$Pair[$Kind]=$Info
    }
    if($Pair.baseline.packed_resource_sha256 -cne $Pair.authored.packed_resource_sha256 -or $Pair.baseline.sha256 -ceq $Pair.authored.sha256){throw 'Distinct same-resource/source pair required.'};return $Pair
}
function Write-CharactersReviewArchives([string]$Directory){
    # Desktop PowerShell may not load the enum/stream types through FileSystem.
    # Focused QA has no earlier Compress-Archive call to load them incidentally.
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $Files=@(Get-ChildItem -LiteralPath $Directory -Recurse -File | Where-Object {$_.Extension -ne '.apk' -and $_.FullName -notlike (Join-Path $Directory 'full-screenshots/*') -and $_.FullName -notlike (Join-Path $Directory 'pixel-*') } | Sort-Object FullName)
    $Batches=New-Object 'System.Collections.Generic.List[object]';$Batch=New-Object 'System.Collections.Generic.List[object]';$Bytes=0L
    foreach($File in $Files){
        if($File.Length -gt 24MB){throw 'Single review record exceeds24MB; original remains.'}
        if($Batch.Count -and $Bytes+$File.Length -gt 24MB){$Batches.Add($Batch);$Batch=New-Object 'System.Collections.Generic.List[object]';$Bytes=0L};$Batch.Add($File);$Bytes+=$File.Length
    };if($Batch.Count){$Batches.Add($Batch)};$Part=0
    foreach($Group in $Batches){
        $Part++;$Zip=$Directory+('-review-{0:D2}.zip' -f $Part);if(Test-Path -LiteralPath $Zip){throw 'Preserve existing review archive.'}
        $Archive=[IO.Compression.ZipFile]::Open($Zip,[IO.Compression.ZipArchiveMode]::Create)
        try{foreach($File in $Group){$Relative=$File.FullName.Substring($Directory.TrimEnd('/','\').Length+1).Replace('\','/');[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($Archive,$File.FullName,$Relative,[IO.Compression.CompressionLevel]::Optimal) | Out-Null}}finally{$Archive.Dispose()};$Zip
    }
}
function Invoke-CharactersPixel([string]$Directory,[string]$Kind,[string]$Device,[int]$Seconds){
    if($Seconds -gt 0 -and $Seconds -lt 5){throw 'Use0for preview or5..600seconds for metrics.'}
    $Directory=(Resolve-Path -LiteralPath $Directory).Path;$Info=(Read-CharactersPair $Directory)[$Kind]
    $AdbPath=Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe'
    $Device=Select-AuthorizedDevice (Invoke-CheckedNative $AdbPath @('devices')) $Device
    if((Invoke-CheckedNative $AdbPath @('-s',$Device,'shell','getprop','ro.product.model')).Trim() -ne 'Pixel 3a'){throw 'Pixel 3a required.'}
    $Output=Join-Path $Directory ('pixel-'+$Kind+'-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory -Path $Output | Out-Null
    $Record=[ordered]@{mode=$Kind;gitRevision=$Info.gitRevision;expected_apk_sha256=$Info.sha256;serial=$Device;installation_verified=$false;renderer_mode_verified=$false;pixel_verified=$false;visual_accepted=$false;adopted=$false;metrics=$null;pid_start=$null;pid_end=$null;same_process=$null;manual_stutter='unknown';manual_tap='unknown';manual_heat='unknown';error=''};$Failure=$null
    try{
        $Since=(Invoke-CheckedNative $AdbPath @('-s',$Device,'shell','date','+%s')).Trim();if($Since -notmatch '^\d+$'){throw 'Device time unavailable.'}
        $Sdk=Split-Path (Split-Path $AdbPath -Parent) -Parent
        Invoke-PixelInstall (Join-Path $Directory $Info.apk) $AdbPath $Device $Info.package (Join-Path $Sdk 'build-tools/36.0.0/aapt2.exe') -ExpectedSha256 $Info.sha256
        $Record.installation_verified=$true
        Write-Host 'On Pixel open town. Check all six characters, their directions, walking, stopping, conversations and greetings. Existing save is preserved.'
        Read-Host 'When town is visible, press Enter to verify this launch and start measurement' | Out-Null
        $Layer=Wait-NoahAuthoredSurface $AdbPath $Device $Output
        $Record.pid_start=(Invoke-CheckedNative $AdbPath @('-s',$Device,'shell','pidof',$Info.package)).Trim();if($Record.pid_start -notmatch '^[1-9][0-9]*$'){throw 'A unique running QA PID is required.'}
        $Log=Invoke-CheckedNative $AdbPath @('-s',$Device,'logcat','--pid',$Record.pid_start,'-d','-v','epoch','-s','Unity');[IO.File]::WriteAllText((Join-Path $Output 'renderer-modes.txt'),$Log);Assert-CharactersRunningMode $Log $Kind ([long]$Since) $Record.pid_start;$Record.renderer_mode_verified=$true
        if($Seconds -gt 0){
            $Capture=Join-Path $Output 'surface-capture';$Label=if($Kind -eq 'baseline'){'Before'}else{'After'}
            & (Join-Path $PSScriptRoot 'capture-pixel-visual-check.ps1') -Label $Label -Seconds $Seconds -Serial $Device -Adb $AdbPath -Layer $Layer -OutputDirectory $Capture
            $Metrics=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Capture 'pixel-evidence.json') | ConvertFrom-Json
            if($Metrics.installed_apk_sha256 -ine $Info.sha256){throw 'Installed APK changed during measurement.'};$Record.metrics=$Metrics
            $Record.manual_stutter=Read-Host 'Stutter: none / observed / unknown';$Record.manual_tap=Read-Host 'Taps: responsive / delayed / unknown';$Record.manual_heat=Read-Host 'Heat: similar / hotter / unknown'
        }
        $Record.pid_end=(Invoke-CheckedNative $AdbPath @('-s',$Device,'shell','pidof',$Info.package)).Trim();$Record.same_process=$Record.pid_start -eq $Record.pid_end
    }catch{$Failure=$_;$Record.error=$_.Exception.Message}
    finally{
        try{[IO.File]::WriteAllText((Join-Path $Output 'crash-buffer.txt'),(Invoke-CheckedNative $AdbPath @('-s',$Device,'logcat','-b','crash','-d','-v','epoch','-t','2000')))}catch{}
        $Record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $Output 'characters-pixel.json') -Encoding UTF8
        Write-Host "Pixel result directory: $Output"
    }
    if($Failure){throw $Failure};Write-Host 'Device evidence collected; visual/performance adoption remains pending.'
}
if($MyInvocation.InvocationName -eq '.'){return}
if($RolloutOptions.Directory){if(-not $RolloutOptions.Install){throw 'Use -Install with an exact completed -BuildDirectory.'};Invoke-CharactersPixel $RolloutOptions.Directory $RolloutOptions.Mode $RolloutOptions.Serial $RolloutOptions.Seconds;return}
$Repository=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$Git=Find-SourceGit
Assert-CleanProductSource $Repository $Git;Assert-NoahAuthoredFrozenInputs $Repository;Assert-NoahAuthoredMasterFixed $Repository
foreach($Id in @(Get-CharacterAuthoredIds)){$Changes=@(& $Git -C $Repository status --porcelain=v1 --untracked-files=all -- ('References/Characters/'+$Id+'-authored-parts-2026-10-30'));if($LASTEXITCODE -ne 0 -or $Changes.Count){throw 'Commit all five source candidates first; no files were reset.'}}
foreach($Relative in @('Assets/Mofumachi/Resources/Mofumachi/CharacterAuthored','Assets/Mofumachi/Resources/Mofumachi/NoahAuthoredTrial')){if((Test-Path -LiteralPath (Join-Path $Repository $Relative)) -or (Test-Path -LiteralPath (Join-Path $Repository ($Relative+'.meta')))){throw 'Existing prepared resources preserved; inspect their owner.'}}
$Editor=if($RolloutOptions.Editor){$RolloutOptions.Editor}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf) -or -not(Test-Path -LiteralPath (Join-Path (Split-Path -Parent $Editor) 'Data/PlaybackEngines/AndroidPlayer'))){throw 'Pinned Unity with Android support required.'}
$Revision=Read-SourceRevision $Repository;$Fingerprint=Get-CharacterAuthoredSourceFingerprint $Repository
$Run=Join-Path $Repository ('Builds/Diagnostics/characters-authored-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory -Path $Run | Out-Null
$Record=[ordered]@{gitRevision=$Revision;productFingerprint=$Fingerprint;full_qa_verified=$false;single_native_verified=$false;six_native_verified=$false;builds_generated=$false;source_unchanged=$false;pixel_verified=$false;adopted=$false;singleRuns=@();failedSingleRun='';error=''};$Failure=$null;$AttemptedCharacter=''
Write-Host "Exact six-character run: $Run"
try{
    foreach($Id in @(Get-CharacterAuthoredIds)){
        $AttemptedCharacter=$Id
        $Before=@(Get-ChildItem -LiteralPath (Join-Path $Repository 'Builds/Diagnostics') -Directory | Select-Object -ExpandProperty FullName)
        & (Join-Path $PSScriptRoot 'capture-character-authored.ps1') -Character $Id -UnityEditorPath $Editor
        $New=@(Get-ChildItem -LiteralPath (Join-Path $Repository 'Builds/Diagnostics') -Directory | Where-Object {$_.Name.StartsWith($Id+'-authored-') -and $_.FullName -notin $Before})
        if($New.Count -ne 1){throw 'Exact new single-character directory not identified.'};$Single=$New[0].FullName
        Assert-CharacterAuthoredProof $Single $Revision $Fingerprint $Id;$Record.singleRuns+=@($Single);$AttemptedCharacter=''
    };$Record.single_native_verified=$true
    foreach($Platform in @('EditMode','PlayMode')){
        Invoke-NoahAuthoredUnity $Editor (Get-CharactersFullArguments $Repository $Run $Platform $Revision $Fingerprint) $Run
        Assert-TestRun (Join-Path $Run ('full-'+$Platform.ToLowerInvariant()+'.xml')) $(if($Platform -eq 'EditMode'){144}else{240}) @()
    };$Record.full_qa_verified=$true
    @{gitRevision=$Revision;productFingerprint=$Fingerprint;singleRuns=$Record.singleRuns} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Run 'context.json') -Encoding UTF8
    $Common=@('-batchmode','-quit','-projectPath',$Repository,'-buildTarget','Android','-mofumachiCharactersDirectory',$Run)
    Invoke-NoahAuthoredUnity $Editor ($Common+@('-executeMethod','Mofumachi.Editor.CharacterAuthoredBuild.PrepareAll','-mofumachiCharacterDirectory',$Run,'-logFile',(Join-Path $Run 'prepare-five.log'))) $Run
    Invoke-NoahAuthoredUnity $Editor ($Common+@('-executeMethod','Mofumachi.Editor.NoahAuthoredBuild.Prepare','-mofumachiNoahAuthoredDirectory',$Run,'-logFile',(Join-Path $Run 'prepare-noah.log'))) $Run
    Invoke-NoahAuthoredUnity $Editor ($Common+@('-executeMethod','Mofumachi.Editor.CharactersRolloutBuild.SealPrepared','-logFile',(Join-Path $Run 'seal.log'))) $Run
    Invoke-NoahAuthoredUnity $Editor (Get-CharactersSixArguments $Repository $Run $Revision $Fingerprint) $Run
    Assert-TestRun (Join-Path $Run 'six-playmode.xml') 1 @('ExperienceCaptureTests');$Record.six_native_verified=$true
    Invoke-NoahAuthoredUnity $Editor ($Common+@('-executeMethod','Mofumachi.Editor.CharactersRolloutBuild.BuildPair','-logFile',(Join-Path $Run 'build.log'))) $Run
    $Record.builds_generated=$true
}catch{
    $Failure=$_;$Record.error=$_.Exception.Message
    if($AttemptedCharacter){
        try{
            $Stopped=@(Get-ChildItem -LiteralPath (Join-Path $Repository 'Builds/Diagnostics') -Directory | Where-Object {$_.Name -cmatch ('^'+$AttemptedCharacter+'-authored-[0-9]{8}-[0-9]{6}-[a-f0-9]{8}$') -and $_.FullName -notin $Before})
            if($Stopped.Count -eq 1){$Record.failedSingleRun=$Stopped[0].FullName;Copy-CharactersFailedSingleRecords $Repository $Run $Record.failedSingleRun $Revision $Fingerprint | Out-Null}
        }catch{$Record.error+=' Diagnostic copy: '+$_.Exception.Message}
    }
}
finally{
    try{Remove-OwnedCharacterResources $Repository $Run;Remove-OwnedNoahAuthoredResources $Repository $Run}catch{if(-not $Failure){$Failure=$_};$Record.error+=' Cleanup: '+$_.Exception.Message}
    try{if((Read-SourceRevision $Repository) -cne $Revision -or (Get-CharacterAuthoredSourceFingerprint $Repository) -cne $Fingerprint){throw 'Source changed; installation blocked.'};Assert-NoahAuthoredMasterFixed $Repository;Assert-NoahAuthoredFrozenInputs $Repository;$Record.source_unchanged=$true}catch{if(-not $Failure){$Failure=$_};$Record.error+=' '+$_.Exception.Message}
    $Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Run 'source-info.json') -Encoding UTF8
    try{foreach($Zip in @(Write-CharactersReviewArchives $Run)){Write-Host "Six-character review ZIP: $Zip"}}catch{if(-not $Failure){$Failure=$_};$Record.error+=' Archive: '+$_.Exception.Message;$Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Run 'source-info.json') -Encoding UTF8}
    Write-Host "All original results preserved: $Run"
}
if($Failure){throw $Failure}
Read-CharactersPair $Run | Out-Null
Write-Host "New six-character APK: $Run\characters-authored.apk"
Write-Host 'Full QA and native gates ran; Pixel/user adoption remains pending. No uninstall, save reset or main merge.'
if($RolloutOptions.Install){Invoke-CharactersPixel $Run $RolloutOptions.Mode $RolloutOptions.Serial $RolloutOptions.Seconds}
