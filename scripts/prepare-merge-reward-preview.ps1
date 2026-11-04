[CmdletBinding()]
param([string]$UnityEditorPath='',[switch]$CaptureOnly,[switch]$FontComparison,[switch]$Final,[switch]$Install,[string]$Serial='99RAY1BELH',[ValidateRange(5,600)][int]$MeasureSeconds=180)
$ErrorActionPreference='Stop'
$MergeOptions=@{Editor=$UnityEditorPath;CaptureOnly=[bool]$CaptureOnly;FontComparison=[bool]$FontComparison;Final=[bool]$Final;Install=[bool]$Install;Serial=$Serial;Seconds=$MeasureSeconds}
. (Join-Path $PSScriptRoot 'prepare-characters-direct-pixel.ps1')
. (Join-Path $PSScriptRoot 'merge-font-artifacts.ps1')
function Assert-MergeRewardVisualApproval([string]$Repository){
    $A=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Repository 'References/VisualLock/merge-reward-visual-approved-2026-11-03.json') | ConvertFrom-Json
    if($A.kind -cne 'merge-reward-visual-approved-candidate' -or $A.visual_gate_approved_by_user -isnot [bool] -or -not $A.visual_gate_approved_by_user -or $A.ui_policy -cne 'fix-only' -or $A.source_revision -cnotmatch '^[a-f0-9]{40}$'){throw 'User-approved fix-only Merge candidate required.'}
    $Names=@('QuestMergeScreens.cs','MergeRewardPresentation.cs','MergeRewardView.cs','MergePerspective.cs','MergeCellSurfaceGraphic.cs','MergeSparkleGraphic.cs')|ForEach-Object {'Assets/Mofumachi/Presentation/'+$_}
    if(@($A.files).Count -ne 6 -or @($A.files.path|Select-Object -Unique).Count -ne 6 -or @(Compare-Object $Names @($A.files.path)).Count){throw 'Exact approved Merge presentation files required.'}
    foreach($Row in @($A.files)){
        $File=Join-Path $Repository $Row.path
        if($Row.sha256 -cnotmatch '^[a-f0-9]{64}$' -or -not(Test-Path -LiteralPath $File -PathType Leaf) -or (Get-FinishTextHash $File) -cne $Row.sha256){throw "Approved Merge design changed: $($Row.path)"}
    }
    if($A.reference_path -cne 'References/Merge/merge_visual_lock_reward.png' -or $A.reference_sha256 -cnotmatch '^[a-f0-9]{64}$' -or (Get-FileHash -LiteralPath (Join-Path $Repository $A.reference_path) -Algorithm SHA256).Hash -ine $A.reference_sha256){throw 'Approved Merge original reference changed.'}
    return $A
}
function Get-MergeRewardArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed,[ValidateSet('capture','focus','EditMode','PlayMode')][string]$Stage,[switch]$FontComparison){
    if($Revision -cnotmatch '^[a-f0-9]{40}$' -or $Fingerprint -cnotmatch '^[a-f0-9]{64}$' -or $Packed -cnotmatch '^[a-f0-9]{64}$'){throw 'Exact Merge source/resource identity required.'}
    $A=@('-batchmode','-projectPath',$Repository,'-buildTarget','Android','-runTests','-testPlatform',$(if($Stage -in @('capture','focus')){'PlayMode'}else{$Stage}),'-testResults',(Join-Path $Directory ($Stage+'.xml')),'-logFile',(Join-Path $Directory ($Stage+'.log')),'-mofumachiSourceRevision',$Revision,'-mofumachiProductFingerprint',$Fingerprint,'-mofumachiPackedResourceSha',$Packed)
    if($Stage -in @('capture','focus')){
        $Suites=if($Stage -eq 'capture'){@('MergeRewardCaptureTests')}else{@('MergeRewardCaptureTests','MergeRewardUiTests','MergeInteractionTests','MergePolishTests','MergeProgressionUiTests','MergeGuidanceTests','ProductionFrameUiTests','QualityPassMergeTests','ExperienceMergeTests')}
        $Filter=if($Stage -eq 'capture'){'Mofumachi.Tests.MergeRewardCaptureTests.MergeRewardCandidateScreens'}else{($Suites | ForEach-Object {'Mofumachi.Tests.'+$_}) -join ';'}
        $A+=@('-testFilter',$Filter,'-mofumachiMergeReward',([IO.Path]::GetFullPath((Join-Path $Directory 'screenshots'))))
    }elseif($Stage -eq 'PlayMode'){
        # The capture test now always requires disk output. A full run has its
        # own directory so its capture cannot reuse/overwrite focused evidence.
        $A+=@('-mofumachiMergeReward',([IO.Path]::GetFullPath((Join-Path $Directory 'full-playmode-capture'))),'-mofumachiMergePreparedQa')
    }
    if($FontComparison){if($Stage -cne 'capture'){throw 'Font comparison is CaptureOnly, before Visual Gate.'};$A+=@('-mofumachiMergeFontComparison',([IO.Path]::GetFullPath((Join-Path $Directory 'screenshots/font-comparison'))))}
    return $A
}
function Assert-MergeRewardProof([string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    $P=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Directory 'screenshots/merge-reward-native.json') | ConvertFrom-Json
    if($P.kind -cne 'merge-reward-native-candidate' -or $P.source_revision -cne $Revision -or $P.product_fingerprint -cne $Fingerprint -or $P.packed_resource_sha256 -cne $Packed -or $P.native_unity -isnot [bool] -or -not $P.native_unity){throw 'Current Merge native identity missing.'}
    foreach($Flag in @('formal_fixed','adopted','pixel_verified')){if($P.$Flag -isnot [bool] -or $P.$Flag){throw 'Native Merge capture cannot claim user/device acceptance.'}}
    $Shots=[IO.Path]::GetFullPath((Join-Path $Directory 'screenshots')).TrimEnd('/','\')
    if($P.disk_verified -isnot [bool] -or -not $P.disk_verified -or $P.image_count -ne 11 -or [string]::IsNullOrWhiteSpace($P.output_directory) -or -not [IO.Path]::IsPathRooted($P.output_directory) -or -not [string]::Equals([IO.Path]::GetFullPath($P.output_directory).TrimEnd('/','\'),$Shots,[StringComparison]::OrdinalIgnoreCase)){throw 'Capture proof must verify eleven files in this exact screenshots directory.'}
    $Expected=@('initial-720x1480-before-reconstructed.jpg','initial-720x1480-after.jpg','initial-720x1280-before-reconstructed.jpg','initial-720x1280-after.jpg','initial-640x1072-before-reconstructed.jpg','initial-640x1072-after.jpg','bread-request-720x1480-before-reconstructed.jpg','bread-request-720x1480-after.jpg','multi-requirement-720x1480-after.jpg','inventory-ready-720x1480-after.jpg','full-board-720x1280-after.jpg')
    if(@($P.images).Count -ne 11 -or @($P.images.file | Select-Object -Unique).Count -ne 11 -or @(Compare-Object $Expected @($P.images.file)).Count){throw 'Merge native image coverage incomplete.'}
    $Actual=@(Get-ChildItem -LiteralPath $Shots -File | Where-Object {$_.Extension -ieq '.jpg'})
    if($Actual.Count -ne 11 -or @(Compare-Object $Expected @($Actual.Name)).Count){throw 'Exactly eleven JPEG files must exist on disk.'}
    foreach($Image in @($P.images)){
        if($Image.state_unchanged -isnot [bool] -or -not $Image.state_unchanged -or $Image.writes_unchanged -isnot [bool] -or -not $Image.writes_unchanged -or $Image.state_sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Merge rendering changed isolated state/save writes.'}
        $File=Join-Path $Directory ('screenshots/'+$Image.file)
        if(-not(Test-Path -LiteralPath $File -PathType Leaf) -or (Get-Item -LiteralPath $File).Length -eq 0 -or ((Get-Item -LiteralPath $File -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or $Image.sha256 -cnotmatch '^[a-f0-9]{64}$' -or (Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash -ine $Image.sha256){throw 'Merge native image missing/empty/changed.'}
        if($Image.before_reconstructed -isnot [bool] -or $Image.before_reconstructed -ne $Image.file.EndsWith('-before-reconstructed.jpg')){throw 'Merge Before/After label differs.'}
        if($Image.before_reconstructed){
            $Partner=@($P.images | Where-Object {$_.scenario -ceq $Image.scenario -and $_.width -eq $Image.width -and $_.height -eq $Image.height})
            if($Partner.Count -ne 2 -or $Partner[0].state_sha256 -cne $Partner[1].state_sha256){throw 'Before/After use different board/save states.'}
        }
    }
    return $P
}
function Write-MergeRewardViewer([string]$Directory,[string]$Reference,$Proof,[switch]$ReleaseUx){
    $ReferenceHtml='<p>原本PNG未配置。Visual Lock比較は未完了です。</p>'
    if(Test-Path -LiteralPath $Reference -PathType Leaf){
        if(-not $Proof.original_png_saved -or (Get-FileHash -LiteralPath $Reference -Algorithm SHA256).Hash -ine $Proof.reference_sha256){throw 'Reference differs from native capture.'}
        Copy-Item -LiteralPath $Reference -Destination (Join-Path $Directory 'screenshots/merge_visual_lock_reward.png');$ReferenceHtml='<img alt="正式Visual Lock" src="merge_visual_lock_reward.png">'
    }
    $Links=(@($Proof.images) | ForEach-Object {'<li><a href="'+$_.file+'">'+$_.file+'</a></li>'}) -join "`n"
    $AfterStatus=if($ReleaseUx){'Afterは今回の用途別フォント候補です。配色と構成は前回合格版を維持しています。今回のフォント表示は未承認です。'}else{'AfterはVisual合格済みの正式候補です。'}
    @"
<!doctype html><meta charset="utf-8"><title>Merge ごほうび候補</title><style>body{font:16px sans-serif;background:#fff6ed;color:#5c3535}section{display:flex;gap:12px}img{width:min(31vw,360px);height:auto;object-fit:contain;align-self:flex-start}p{max-width:1000px}</style><h1>Merge・同条件の実Unity描画</h1><p>隔離テストデータです。Beforeは採用済みピンク版（c7329cc）の再構成です。$AfterStatus 青い旧版は新規出力しません。ユーザーの保存データ・Pixel画面ではありません。Mergeの形・配色はfix onlyを維持し、実行テスト・APK・実機確認は別に記録します。</p><section>$ReferenceHtml<img alt="採用済みピンク版Before" src="bread-request-720x1480-before-reconstructed.jpg"><img alt="After候補" src="bread-request-720x1480-after.jpg"></section><p>既存仕様の40 Coinsを表示。見本のジェム報酬は追加していません。</p><h2>全11枚</h2><ul>$Links</ul><a href="merge-reward-native.json">撮影証跡JSON</a>
"@ | Set-Content -LiteralPath (Join-Path $Directory 'screenshots/merge-reward.html') -Encoding UTF8
}
function Assert-MergeRewardArtifacts([string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    $Proof=Assert-MergeRewardProof $Directory $Revision $Fingerprint $Packed
    $Shots=[IO.Path]::GetFullPath((Join-Path $Directory 'screenshots'));$Html=Join-Path $Shots 'merge-reward.html'
    if(-not(Test-Path -LiteralPath $Html -PathType Leaf) -or (Get-Item -LiteralPath $Html).Length -eq 0){throw 'Capture HTML was not generated.'}
    $Text=Get-Content -Raw -Encoding UTF8 -LiteralPath $Html
    if($Text -notmatch '(?i)<!doctype html>'){throw 'Capture HTML is invalid.'}
    foreach($Image in @($Proof.images)){if(-not $Text.Contains($Image.file)){throw 'Capture HTML is missing image links.'}}
    $Names=@($Proof.images.file)+@('merge-reward-native.json','merge-reward.html')
    if($Proof.original_png_saved){
        $Reference=Join-Path $Shots 'merge_visual_lock_reward.png'
        if(-not(Test-Path -LiteralPath $Reference -PathType Leaf) -or (Get-FileHash -LiteralPath $Reference -Algorithm SHA256).Hash -ine $Proof.reference_sha256){throw 'Viewer original reference missing/changed.'}
        $Names+=@('merge_visual_lock_reward.png')
    }
    return @(foreach($Name in $Names){$File=Join-Path $Shots $Name;@{path=('screenshots/'+$Name);bytes=(Get-Item -LiteralPath $File).Length;sha256=(Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant()}})
}
function Confirm-MergeRewardCapture($Record,[string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed,[switch]$FontComparison){
    # Reset before every validation, including final validation after cleanup.
    $Record.capture_verified=$false;$Record.image_count=0;$Record.artifacts=@()
    if($FontComparison){$Record.font_comparison_verified=$false;$Record.font_comparison_artifacts=@()}
    $Record.artifact_directory=[IO.Path]::GetFullPath((Join-Path $Directory 'screenshots'))
    if($Record.source_unchanged -isnot [bool] -or -not $Record.source_unchanged -or $Record.error){throw 'Capture source/cleanup checks failed.'}
    $Artifacts=@(Assert-MergeRewardArtifacts $Directory $Revision $Fingerprint $Packed)
    if($FontComparison){$Record.font_comparison_artifacts=@(Assert-MergeFontArtifacts $Directory $Revision $Fingerprint $Packed);$Record.font_comparison_verified=$true}
    $Record.artifacts=$Artifacts;$Record.image_count=11;$Record.capture_verified=$true
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
if($MergeOptions.FontComparison -and -not $MergeOptions.CaptureOnly){throw 'Use -CaptureOnly -FontComparison; no full QA, APK or install.'}
if($MergeOptions.CaptureOnly -and ($MergeOptions.Final -or $MergeOptions.Install)){throw 'CaptureOnly stops before Visual Gate; no full QA or APK installation.'}
if($MergeOptions.Install -and -not $MergeOptions.Final){throw 'Installation requires the candidate-only -Final full QA run.'}
$Repo=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$Git=Find-SourceGit
$Editor=if($MergeOptions.Editor){$MergeOptions.Editor}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf) -or -not(Test-Path -LiteralPath (Join-Path (Split-Path -Parent $Editor) 'Data/PlaybackEngines/AndroidPlayer'))){throw 'Unity6000.6.4f1 Android support required.'}
if(Get-Process -Name Unity -ErrorAction SilentlyContinue){throw 'Close Unity Editor before this isolated run.'}
$Reference=Join-Path $Repo 'References/Merge/merge_visual_lock_reward.png'
if($MergeOptions.Final -and -not(Test-Path -LiteralPath $Reference -PathType Leaf)){throw 'Original Visual Lock PNG must exist at References/Merge/merge_visual_lock_reward.png before final QA. Focus capture remains available.'}
$Approval=Assert-MergeRewardVisualApproval $Repo
$Preserved=Move-PreviousCharacterInputs $Repo $Git
Assert-CleanProductSource $Repo $Git;Assert-CharactersVisualLock $Repo;Assert-NoahAuthoredMasterFixed $Repo;Assert-NoahAuthoredFrozenInputs $Repo
$Revision=Read-SourceRevision $Repo;$Fingerprint=Get-CharacterAuthoredSourceFingerprint $Repo
$Run=Join-Path $Repo ('Builds/Diagnostics/merge-reward-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory -Path $Run | Out-Null
$Record=[ordered]@{scope='merge-reward-ui-candidate';gitRevision=$Revision;productFingerprint=$Fingerprint;packed_resource_sha256='';visual_gate_approved_by_user=$true;approved_design_revision=$Approval.source_revision;ui_policy='fix-only';preserved_inputs=$Preserved;artifact_directory=([IO.Path]::GetFullPath((Join-Path $Run 'screenshots')));image_count=0;artifacts=@();font_comparison_verified=$false;font_comparison_artifacts=@();qa_artifacts=@();capture_verified=$false;focus_verified=$false;full_qa_verified=$false;build_generated=$false;pixel_verified=$false;formal_fixed=$false;adopted=$false;source_unchanged=$false;error=''}
$Failure=$null;$Apk=$null;$CaptureReady=$false;Write-Host "Merge-only candidate run: $Run";Write-Host "Exact capture output: $($Record.artifact_directory)"
try{
    foreach($Step in @('prepare-noah','prepare-five')){Invoke-NoahAuthoredUnity $Editor (Get-CharactersDirectArguments $Repo $Run $Step) $Run}
    $Record.packed_resource_sha256=Get-PreparedCharactersDigest $Repo
    $Stage=if($MergeOptions.CaptureOnly){'capture'}else{'focus'}
    Invoke-NoahAuthoredUnity $Editor (Get-MergeRewardArguments $Repo $Run $Revision $Fingerprint $Record.packed_resource_sha256 $Stage -FontComparison:$MergeOptions.FontComparison) $Run
    Assert-TestRun (Join-Path $Run ($Stage+'.xml')) $(if($MergeOptions.CaptureOnly){1}else{5}) $(if($MergeOptions.CaptureOnly){@('MergeRewardCaptureTests')}else{@('MergeRewardCaptureTests','MergeRewardUiTests','MergeInteractionTests','ProductionFrameUiTests')})
    $Proof=Assert-MergeRewardProof $Run $Revision $Fingerprint $Record.packed_resource_sha256
    Write-MergeRewardViewer $Run $Reference $Proof -ReleaseUx:(Test-Path -LiteralPath (Join-Path $Repo 'References/VisualLock/release-ux-2026-11-03.json') -PathType Leaf)
    $null=Assert-MergeRewardArtifacts $Run $Revision $Fingerprint $Record.packed_resource_sha256;$CaptureReady=$true
    if($MergeOptions.Final){
        foreach($Platform in @('EditMode','PlayMode')){
            Invoke-NoahAuthoredUnity $Editor (Get-MergeRewardArguments $Repo $Run $Revision $Fingerprint $Record.packed_resource_sha256 $Platform) $Run
            $Required=if($Platform -eq 'EditMode'){@('DeliveryTests','SaveServiceTests','QuestCatalogTests')}else{@('MergeRewardCaptureTests','MergeRewardUiTests','MergeInteractionTests','ProductionFrameUiTests','VerticalSliceFlowTests')}
            Assert-TestRun (Join-Path $Run ($Platform+'.xml')) $(if($Platform -eq 'EditMode'){144}else{240}) $Required
        }
        $Record.full_qa_verified=$true
        $Record.qa_artifacts=@(foreach($Name in @('focus.xml','EditMode.xml','PlayMode.xml')){@{path=$Name;sha256=(Get-FileHash -LiteralPath (Join-Path $Run $Name) -Algorithm SHA256).Hash.ToLowerInvariant()}})
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
        $null=Assert-MergeRewardVisualApproval $Repo
    }catch{if(-not $Failure){$Failure=$_};$Record.error+=' '+$_.Exception.Message}
    if(-not $Failure -and $CaptureReady){
        try{Confirm-MergeRewardCapture $Record $Run $Revision $Fingerprint $Record.packed_resource_sha256 -FontComparison:$MergeOptions.FontComparison;$Record.focus_verified=-not $MergeOptions.CaptureOnly}
        catch{$Failure=$_;$Record.error+=' '+$_.Exception.Message}
    }
    $Record|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $Run 'merge-reward-source-info.json') -Encoding UTF8
    Write-Host "Original results preserved: $Run"
    if($env:OS -eq 'Windows_NT'){
        if($Record.font_comparison_verified){Start-Process -FilePath explorer.exe -ArgumentList ('/select,"'+(Join-Path $Record.artifact_directory 'font-comparison/merge-font.html')+'"')}
        elseif($Record.capture_verified){Start-Process -FilePath explorer.exe -ArgumentList ('/select,"'+(Join-Path $Record.artifact_directory 'merge-reward.html')+'"')}
        else{Start-Process -FilePath explorer.exe -ArgumentList ('"'+$Run+'"')}
    }
}
if($Failure){throw $Failure}
if($MergeOptions.Install){
    & (Join-Path $PSScriptRoot 'test-merge-reward-pixel.ps1') -BuildDirectory $Run -UnityEditorPath $Editor -Serial $MergeOptions.Serial -MeasureSeconds $MergeOptions.Seconds
}
Write-Host 'Approved Merge candidate evidence ready. Native QA and Pixel acceptance are recorded separately. No uninstall or save reset.'
return $Run
