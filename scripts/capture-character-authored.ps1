[CmdletBinding()]
param([ValidateSet('momo')][string]$Character='momo',[string]$UnityEditorPath='')
$ErrorActionPreference='Stop'
$RequestedCharacter=$Character;$RequestedEditor=$UnityEditorPath
. (Join-Path $PSScriptRoot 'prepare-noah-pixel-preview.ps1')
$Character=$RequestedCharacter;$UnityEditorPath=$RequestedEditor
function Get-CharacterAuthoredArguments([string]$Repository,[string]$Directory,[string]$Id,[string]$Revision,[string]$Fingerprint){
    if($Id -ne 'momo' -or -not $Repository -or -not $Directory -or $Revision -notmatch '^[a-f0-9]{40}$' -or $Fingerprint -notmatch '^[a-f0-9]{64}$'){throw 'Only the current Momo sequential gate is available.'}
    return @('-batchmode','-projectPath',$Repository,'-buildTarget','Android','-runTests','-testPlatform','PlayMode','-testFilter','Mofumachi.Tests.ExperienceCaptureTests.MomoAuthoredPartsPreserveIdentityInputAndState','-testResults',(Join-Path $Directory 'playmode.xml'),'-logFile',(Join-Path $Directory 'playmode.log'),'-mofumachiScreenshots',(Join-Path $Directory 'screenshots'),'-mofumachiSourceRevision',$Revision,'-mofumachiProductFingerprint',$Fingerprint,'-mofumachiCharacterPreview')
}
function Assert-NoahAuthoredMasterFixed([string]$Repository){
    $Record=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Repository 'References/VisualLock/noah-authored-approved-2026-10-30.json') | ConvertFrom-Json
    if($Record.user_approved -isnot [bool] -or -not $Record.user_approved -or @($Record.frozen_files).Count -eq 0){throw 'Approved Noah master identity required.'}
    foreach($File in $Record.frozen_files){
        $Path=Join-Path $Repository $File.path
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "Noah master missing: $($File.path)"}
        $Bytes=if($File.text_normalized){[Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($Path).Replace("`r`n","`n"))}else{[IO.File]::ReadAllBytes($Path)}
        $Hasher=[Security.Cryptography.SHA256]::Create();try{$Hash=([BitConverter]::ToString($Hasher.ComputeHash($Bytes))).Replace('-','').ToLowerInvariant()}finally{$Hasher.Dispose()}
        if($Hash -ne $File.sha256){throw "Noah master changed: $($File.path)"}
    }
}
function Get-CharacterAuthoredSourceFingerprint([string]$Repository){
    $Temp='Assets/Mofumachi/Resources/Mofumachi/CharacterAuthored';$Entries=@(foreach($Relative in (@(Source-InputPaths)+@('References/Characters/momo-authored-parts-2026-10-30'))){
        $Path=Join-Path $Repository $Relative;if(-not(Test-Path -LiteralPath $Path)){continue}
        $Files=if(Test-Path -LiteralPath $Path -PathType Leaf){Get-Item -LiteralPath $Path -Force}else{Get-ChildItem -LiteralPath $Path -File -Recurse -Force}
        foreach($File in $Files){$Name=$File.FullName.Substring($Repository.TrimEnd('/','\').Length+1).Replace('\','/');if($Name -eq ($Temp+'.meta') -or $Name.StartsWith($Temp+'/')){continue};$Name+':'+(Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash}
    })
    $Hasher=[Security.Cryptography.SHA256]::Create();try{return ([BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes((($Entries | Sort-Object) -join "`n"))))).Replace('-','').ToLowerInvariant()}finally{$Hasher.Dispose()}
}
function Remove-OwnedCharacterResources([string]$Repository,[string]$Directory){
    $Folder=Join-Path $Repository 'Assets/Mofumachi/Resources/Mofumachi/CharacterAuthored';if(-not(Test-Path -LiteralPath $Folder)){return}
    $Marker=Join-Path $Folder '.owner'
    if(-not(Test-Path -LiteralPath $Marker -PathType Leaf) -or -not [string]::Equals([IO.File]::ReadAllText($Marker),[IO.Path]::GetFullPath($Directory),[StringComparison]::OrdinalIgnoreCase)){throw 'Preserve character resources owned by another run.'}
    [IO.Directory]::Delete($Folder,$true);if(Test-Path -LiteralPath ($Folder+'.meta')){[IO.File]::Delete($Folder+'.meta')}
}
function Assert-CharacterAuthoredProof([string]$Directory,[string]$Revision,[string]$Fingerprint){
    [xml]$Xml=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'playmode.xml');$Cases=@($Xml.SelectNodes('//test-case'))
    if($Cases.Count -ne 1 -or $Cases[0].fullname -ne 'Mofumachi.Tests.ExperienceCaptureTests.MomoAuthoredPartsPreserveIdentityInputAndState' -or $Cases[0].result -ne 'Passed'){throw 'Exactly the new single passed Momo method is required.'}
    $Proof=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'screenshots/momo-authored/proof.json') | ConvertFrom-Json
    $Prepared=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'prepared.json') | ConvertFrom-Json
    if($Proof.character -ne 'momo' -or $Prepared.characterId -ne 'momo' -or $Proof.gitRevision -ne $Revision -or $Proof.productFingerprint -ne $Fingerprint -or $Proof.input_sha256 -ne $Prepared.input_sha256 -or $Proof.input_sha256 -notmatch '^[a-f0-9]{64}$' -or $Proof.sorting_examples -ne 5 -or @($Proof.sorting_images).Count -ne 5 -or @($Proof.frames).Count -lt 8){throw 'Incomplete Momo source/material/capture identity.'}
    foreach($Name in @('native_unity','tap_verified','conversation_verified','save_unchanged','greeting_authored','talk_authored','blink_authored')){if($Proof.$Name -isnot [bool] -or -not $Proof.$Name){throw "Unverified Momo result: $Name"}}
    foreach($Name in @('adopted','pixel_verified')){if($Proof.$Name -isnot [bool] -or $Proof.$Name){throw 'Single Momo capture cannot claim Pixel or adoption.'}}
    foreach($Name in @('Front','Left','Right','Back')){if($Name -notin $Proof.observed_directions){throw "Actual Momo walking direction missing: $Name"}}
    if(@($Proof.pairs | Where-Object {$_.changed_pixels -gt 0}).Count -lt 6 -or @($Proof.pairs | Where-Object {$_.outside_allowed_pixels -ne 0}).Count -gt 0){throw 'Momo causal image checks incomplete.'}
    foreach($Pair in $Proof.pairs){foreach($Name in @('frame_unchanged','actors_unchanged','other_five_unchanged','camera_unchanged','water_clock_unchanged','water_phase_unchanged','state_unchanged','store_writes_unchanged','world_model_unchanged')){if($Pair.$Name -isnot [bool] -or -not $Pair.$Name){throw "Changed causal comparison context: $Name"}}}
    $Base=Join-Path $Directory 'screenshots/momo-authored'
    $Images=@($Proof.frames)+@($Proof.sorting_images)+@($Proof.blink_image)+@($Proof.pairs | ForEach-Object {$_.before;$_.after})
    foreach($Image in $Images){
        if(-not $Image -or [IO.Path]::GetFileName($Image.file) -ne $Image.file -or $Image.width -ne 720 -or $Image.height -ne 1280 -or $Image.sha256 -notmatch '^[a-f0-9]{64}$'){throw 'Invalid native image identity.'}
        $Path=Join-Path $Base $Image.file
        if(-not(Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-Item -LiteralPath $Path).Length -ne $Image.bytes -or (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash -ne $Image.sha256){throw 'Native image bytes changed/missing.'}
    }
}
if($MyInvocation.InvocationName -eq '.'){return}
$Repository=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$Git=Find-SourceGit
Assert-CleanProductSource $Repository $Git
$ArtChanges=@(& $Git -C $Repository status --porcelain=v1 --untracked-files=all -- 'References/Characters/momo-authored-parts-2026-10-30')
if($LASTEXITCODE -ne 0 -or $ArtChanges.Count -gt 0){throw 'Commit Momo material inputs first. No files were reset.'}
Assert-NoahAuthoredFrozenInputs $Repository;Assert-NoahAuthoredMasterFixed $Repository
$Folder=Join-Path $Repository 'Assets/Mofumachi/Resources/Mofumachi/CharacterAuthored'
if((Test-Path -LiteralPath $Folder) -or (Test-Path -LiteralPath ($Folder+'.meta'))){throw 'Existing resources preserved; inspect their owning run before retrying.'}
$Editor=if($UnityEditorPath){$UnityEditorPath}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf)){throw 'Unity6000.6.4f1 required. No native test or install started.'}
$Revision=Read-SourceRevision $Repository;$Fingerprint=Get-CharacterAuthoredSourceFingerprint $Repository
$Run=Join-Path $Repository ('Builds/Diagnostics/momo-authored-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory -Path $Run | Out-Null
$Record=[ordered]@{scope='Momo-only sequential native gate';gitRevision=$Revision;productFingerprint=$Fingerprint;native_test_passed=$false;source_unchanged=$false;visual_review_pending=$true;apk_generated=$false;pixel_verified=$false;adopted=$false;error=''};$Failure=''
Write-Host "Momo-only run: $Run"
try{
    Invoke-NoahAuthoredUnity $Editor @('-batchmode','-quit','-projectPath',$Repository,'-buildTarget','Android','-executeMethod','Mofumachi.Editor.CharacterAuthoredBuild.Prepare','-mofumachiCharacterDirectory',$Run,'-mofumachiCharacterId',$Character,'-logFile',(Join-Path $Run 'prepare.log')) $Run
    Invoke-NoahAuthoredUnity $Editor (Get-CharacterAuthoredArguments $Repository $Run $Character $Revision $Fingerprint) $Run
    Assert-TestRun (Join-Path $Run 'playmode.xml') 1 @('ExperienceCaptureTests')
    Assert-CharacterAuthoredProof $Run $Revision $Fingerprint;$Record.native_test_passed=$true
}catch{$Failure=$_.Exception.Message;$Record.error=$Failure}
finally{
    try{Remove-OwnedCharacterResources $Repository $Run}catch{if(-not $Failure){$Failure=$_.Exception.Message};$Record.error+=' Cleanup: '+$_.Exception.Message}
    try{if((Read-SourceRevision $Repository) -ne $Revision -or (Get-CharacterAuthoredSourceFingerprint $Repository) -ne $Fingerprint){throw 'Source changed during the single-character gate.'};Assert-NoahAuthoredMasterFixed $Repository;$Record.source_unchanged=$true}catch{if(-not $Failure){$Failure=$_.Exception.Message};$Record.error+=' '+$_.Exception.Message}
    $Record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'source-info.json') -Encoding UTF8
    try{foreach($Zip in @(Write-NoahAuthoredArchives $Run)){Write-Host "Momo-only review ZIP: $Zip"}}catch{if(-not $Failure){$Failure=$_.Exception.Message};Write-Host "Archive error; originals preserved: $Run"}
}
Write-Host 'One Momo method only. No full suites, APK, Pixel install, uninstall or save reset. Other four remain untouched.'
if($Failure){throw "Momo gate stopped; share this run's ZIP/logs. Do not run full QA. $Failure"}
Write-Host "Momo native capture passed. Visual review still pending: $Run\screenshots\momo-authored"
