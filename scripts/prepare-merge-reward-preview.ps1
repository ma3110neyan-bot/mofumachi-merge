[CmdletBinding()]
param([string]$UnityEditorPath='',[switch]$Final,[switch]$Install,[string]$Serial='99RAY1BELH')
$ErrorActionPreference='Stop'
$MergeOptions=@{Editor=$UnityEditorPath;Final=[bool]$Final;Install=[bool]$Install;Serial=$Serial}
. (Join-Path $PSScriptRoot 'prepare-characters-direct-pixel.ps1')
function Get-MergeRewardArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed,[ValidateSet('focus','EditMode','PlayMode')][string]$Stage){
    if($Revision -cnotmatch '^[a-f0-9]{40}$' -or $Fingerprint -cnotmatch '^[a-f0-9]{64}$' -or $Packed -cnotmatch '^[a-f0-9]{64}$'){throw 'Exact Merge source/resource identity required.'}
    $A=@('-batchmode','-projectPath',$Repository,'-buildTarget','Android','-runTests','-testPlatform',$(if($Stage -eq 'focus'){'PlayMode'}else{$Stage}),'-testResults',(Join-Path $Directory ($Stage+'.xml')),'-logFile',(Join-Path $Directory ($Stage+'.log')),'-mofumachiSourceRevision',$Revision,'-mofumachiProductFingerprint',$Fingerprint,'-mofumachiPackedResourceSha',$Packed)
    if($Stage -eq 'focus'){
        $Suites=@('MergeRewardCaptureTests','MergeRewardUiTests','MergeInteractionTests','MergePolishTests','MergeProgressionUiTests','MergeGuidanceTests','ProductionFrameUiTests','QualityPassMergeTests','ExperienceMergeTests')
        $A+=@('-testFilter',(($Suites | ForEach-Object {'Mofumachi.Tests.'+$_}) -join ';'),'-mofumachiMergeReward',(Join-Path $Directory 'screenshots'))
    }
    return $A
}
function Assert-MergeRewardProof([string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    $P=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'screenshots/merge-reward-native.json') | ConvertFrom-Json
    if($P.kind -cne 'merge-reward-native-candidate' -or $P.source_revision -cne $Revision -or $P.product_fingerprint -cne $Fingerprint -or $P.packed_resource_sha256 -cne $Packed -or $P.native_unity -isnot [bool] -or -not $P.native_unity){throw 'Current Merge native identity missing.'}
    foreach($Flag in @('formal_fixed','adopted','pixel_verified')){if($P.$Flag -isnot [bool] -or $P.$Flag){throw 'Native Merge capture cannot claim user/device acceptance.'}}
    $Expected=@('initial-720x1480-before-reconstructed.jpg','initial-720x1480-after.jpg','initial-720x1280-before-reconstructed.jpg','initial-720x1280-after.jpg','initial-640x1072-before-reconstructed.jpg','initial-640x1072-after.jpg','bread-request-720x1480-before-reconstructed.jpg','bread-request-720x1480-after.jpg','multi-requirement-720x1480-after.jpg','inventory-ready-720x1480-after.jpg','full-board-720x1280-after.jpg')
    if(@($P.images).Count -ne 11 -or @($P.images.file | Select-Object -Unique).Count -ne 11 -or @(Compare-Object $Expected @($P.images.file)).Count){throw 'Merge native image coverage incomplete.'}
    foreach($Image in @($P.images)){
        if($Image.state_unchanged -isnot [bool] -or -not $Image.state_unchanged -or $Image.writes_unchanged -isnot [bool] -or -not $Image.writes_unchanged -or $Image.state_sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Merge rendering changed isolated state/save writes.'}
        $File=Join-Path $Directory ('screenshots/'+$Image.file)
        if(-not(Test-Path -LiteralPath $File -PathType Leaf) -or ((Get-Item -LiteralPath $File -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or $Image.sha256 -cnotmatch '^[a-f0-9]{64}$' -or (Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash -ine $Image.sha256){throw 'Merge native image missing/changed.'}
        if($Image.before_reconstructed -isnot [bool] -or $Image.before_reconstructed -ne $Image.file.EndsWith('-before-reconstructed.jpg')){throw 'Merge Before/After label differs.'}
        if($Image.before_reconstructed){
            $Partner=@($P.images | Where-Object {$_.scenario -ceq $Image.scenario -and $_.width -eq $Image.width -and $_.height -eq $Image.height})
            if($Partner.Count -ne 2 -or $Partner[0].state_sha256 -cne $Partner[1].state_sha256){throw 'Before/After use different board/save states.'}
        }
    }
    return $P
}
function Assert-MergeRewardApk([string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    $I=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'characters-authored-build.json') | ConvertFrom-Json
    if($I.scope -cne 'characters-direct-pixel-no-tests' -or $I.mode -cne 'authored' -or $I.package -cne 'com.mofumachi.merge.qa.townlife' -or $I.apk -cne 'characters-authored.apk' -or $I.gitRevision -cne $Revision -or $I.productFingerprint -cne $Fingerprint -or $I.packed_resource_sha256 -cne $Packed -or $I.sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Merge APK differs from the tested candidate.'}
    # Child builder does no testing. Parent run holds the actual QA XML evidence.
    foreach($Flag in @('tests_run','full_qa_verified','pixel_verified')){if($I.$Flag -isnot [bool] -or $I.$Flag){throw 'Builder-only record was relabelled as acceptance.'}}
    $File=Join-Path $Directory $I.apk
    if(-not(Test-Path -LiteralPath $File -PathType Leaf) -or (Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash -ine $I.sha256){throw 'New Merge APK bytes missing/changed.'}
    return $I
}
if($MyInvocation.InvocationName -eq '.'){return}
if($MergeOptions.Install -and -not $MergeOptions.Final){throw 'Installation requires the candidate-only -Final full QA run.'}
$Repo=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$Git=Find-SourceGit
$Editor=if($MergeOptions.Editor){$MergeOptions.Editor}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf) -or -not(Test-Path -LiteralPath (Join-Path (Split-Path -Parent $Editor) 'Data/PlaybackEngines/AndroidPlayer'))){throw 'Unity6000.6.4f1 Android support required.'}
if(Get-Process -Name Unity -ErrorAction SilentlyContinue){throw 'Close Unity Editor before this isolated run.'}
$Reference=Join-Path $Repo 'References/Merge/merge_visual_lock_reward.png'
if($MergeOptions.Final -and -not(Test-Path -LiteralPath $Reference -PathType Leaf)){throw 'Original Visual Lock PNG must exist at References/Merge/merge_visual_lock_reward.png before final QA. Focus capture remains available.'}
$Preserved=Move-PreviousCharacterInputs $Repo $Git
Assert-CleanProductSource $Repo $Git;Assert-CharactersVisualLock $Repo;Assert-NoahAuthoredMasterFixed $Repo;Assert-NoahAuthoredFrozenInputs $Repo
$Revision=Read-SourceRevision $Repo;$Fingerprint=Get-CharacterAuthoredSourceFingerprint $Repo
$Run=Join-Path $Repo ('Builds/Diagnostics/merge-reward-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory -Path $Run | Out-Null
$Record=[ordered]@{scope='merge-reward-ui-candidate';gitRevision=$Revision;productFingerprint=$Fingerprint;packed_resource_sha256='';preserved_inputs=$Preserved;focus_verified=$false;full_qa_verified=$false;build_generated=$false;pixel_verified=$false;formal_fixed=$false;adopted=$false;source_unchanged=$false;error=''}
$Failure=$null;$Apk=$null;Write-Host "Merge-only candidate run: $Run"
try{
    foreach($Step in @('prepare-noah','prepare-five')){Invoke-NoahAuthoredUnity $Editor (Get-CharactersDirectArguments $Repo $Run $Step) $Run}
    $Record.packed_resource_sha256=Get-PreparedCharactersDigest $Repo
    Invoke-NoahAuthoredUnity $Editor (Get-MergeRewardArguments $Repo $Run $Revision $Fingerprint $Record.packed_resource_sha256 'focus') $Run
    Assert-TestRun (Join-Path $Run 'focus.xml') 5 @('MergeRewardCaptureTests','MergeRewardUiTests','MergeInteractionTests','ProductionFrameUiTests')
    $Proof=Assert-MergeRewardProof $Run $Revision $Fingerprint $Record.packed_resource_sha256;$Record.focus_verified=$true
    $ReferenceHtml='<p>原本PNG未配置。Visual Lock比較は未完了です。</p>'
    if(Test-Path -LiteralPath $Reference -PathType Leaf){
        if(-not $Proof.original_png_saved -or (Get-FileHash -LiteralPath $Reference -Algorithm SHA256).Hash -ine $Proof.reference_sha256){throw 'Reference differs from native capture.'}
        Copy-Item -LiteralPath $Reference -Destination (Join-Path $Run 'screenshots/merge_visual_lock_reward.png');$ReferenceHtml='<img alt="正式Visual Lock" src="merge_visual_lock_reward.png">'
    }
    @"
<!doctype html><meta charset="utf-8"><title>Merge ごほうび候補</title><style>body{font:16px sans-serif;background:#fff6ed;color:#5c3535}section{display:flex;gap:12px}img{width:min(31vw,360px);height:auto;object-fit:contain;align-self:flex-start}p{max-width:1000px}</style><h1>Merge・同条件の実Unity描画</h1><p>隔離テストデータです。Beforeは旧UIを現在ソースで再構成。ユーザーの保存データ・Pixel画面ではありません。正式採用は未判定です。原本・Before・Afterを比較してください。</p><section>$ReferenceHtml<img alt="Before再構成" src="bread-request-720x1480-before-reconstructed.jpg"><img alt="After候補" src="bread-request-720x1480-after.jpg"></section><p>既存仕様の40 Coinsを表示。見本のジェム報酬は追加していません。</p>
"@ | Set-Content -LiteralPath (Join-Path $Run 'screenshots/merge-reward.html') -Encoding UTF8
    if($MergeOptions.Final){
        foreach($Platform in @('EditMode','PlayMode')){
            Invoke-NoahAuthoredUnity $Editor (Get-MergeRewardArguments $Repo $Run $Revision $Fingerprint $Record.packed_resource_sha256 $Platform) $Run
            $Required=if($Platform -eq 'EditMode'){@('DeliveryTests','SaveServiceTests','QuestCatalogTests')}else{@('MergeRewardCaptureTests','MergeRewardUiTests','MergeInteractionTests','ProductionFrameUiTests','VerticalSliceFlowTests')}
            Assert-TestRun (Join-Path $Run ($Platform+'.xml')) $(if($Platform -eq 'EditMode'){144}else{240}) $Required
        }
        $Record.full_qa_verified=$true
        @{gitRevision=$Revision;productFingerprint=$Fingerprint;packed_resource_sha256=$Record.packed_resource_sha256}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $Run 'direct-context.json') -Encoding UTF8
        Invoke-NoahAuthoredUnity $Editor (Get-CharactersDirectArguments $Repo $Run 'build') $Run
        $Apk=Assert-MergeRewardApk $Run $Revision $Fingerprint $Record.packed_resource_sha256;$Record.build_generated=$true
    }
    if((Get-PreparedCharactersDigest $Repo) -cne $Record.packed_resource_sha256){throw 'Packed resources changed during Merge verification/build.'}
}catch{$Failure=$_;$Record.error=$_.Exception.Message}
finally{
    try{Remove-OwnedCharacterResources $Repo $Run;Remove-OwnedNoahAuthoredResources $Repo $Run}catch{if(-not $Failure){$Failure=$_};$Record.error+=' Cleanup: '+$_.Exception.Message}
    try{
        if((Read-SourceRevision $Repo) -cne $Revision -or (Get-CharacterAuthoredSourceFingerprint $Repo) -cne $Fingerprint){throw 'Merge source changed during the run.'}
        Assert-CharactersVisualLock $Repo;Assert-NoahAuthoredMasterFixed $Repo;Assert-NoahAuthoredFrozenInputs $Repo;$Record.source_unchanged=$true
    }catch{if(-not $Failure){$Failure=$_};$Record.error+=' '+$_.Exception.Message}
    $Record|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $Run 'merge-reward-source-info.json') -Encoding UTF8
    Write-Host "Original results preserved: $Run"
    if($env:OS -eq 'Windows_NT'){Start-Process -FilePath explorer.exe -ArgumentList ('"'+$Run+'"')}
}
if($Failure){throw $Failure}
if($MergeOptions.Install){
    $Sdk=Join-Path (Split-Path -Parent $Editor) 'Data/PlaybackEngines/AndroidPlayer/SDK'
    $Adb=Join-Path $Sdk 'platform-tools/adb.exe';$Aapt=Join-Path $Sdk 'build-tools/36.0.0/aapt2.exe'
    $Serial=Select-AuthorizedDevice (Invoke-CheckedNative $Adb @('devices')) $MergeOptions.Serial
    if((Invoke-CheckedNative $Adb @('-s',$Serial,'shell','getprop','ro.product.model')).Trim() -cne 'Pixel 3a'){throw 'Connect the selected Pixel 3a.'}
    Invoke-PixelInstall (Join-Path $Run $Apk.apk) $Adb $Serial $Apk.package $Aapt -ExpectedSha256 $Apk.sha256
}
Write-Host 'Merge candidate evidence ready. Visual adoption and Pixel acceptance remain separate. No uninstall or save reset.'
