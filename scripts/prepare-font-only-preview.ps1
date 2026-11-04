[CmdletBinding()]
param([string]$UnityEditorPath='')
$ErrorActionPreference='Stop';$FontOnlyEditorOption=$UnityEditorPath
. (Join-Path $PSScriptRoot 'prepare-merge-reward-preview.ps1')
. (Join-Path $PSScriptRoot 'font-only-scope.ps1')
. (Join-Path $PSScriptRoot 'font-only-artifacts.ps1')
function Get-FontOnlyArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    $A=@(Get-MergeRewardArguments $Repository $Directory $Revision $Fingerprint $Packed capture)
    $At=[Array]::IndexOf($A,'-testFilter');if($At -lt 0){throw 'Explicit font test filter required.'}
    $A[$At+1]='Mofumachi.Tests.ReleaseUxNativeTests.ScopedFontsFitTitleTownMergeBookSettingsAndTodayAtPhoneSizes;Mofumachi.Tests.StoryTypographyTests'
    $At=[Array]::IndexOf($A,'-mofumachiMergeReward')
    if($At -lt 0){throw 'Expected capture arguments changed.'}
    $A=@(for($Index=0;$Index -lt $A.Count;$Index++){if($Index -eq $At){$Index++;continue};$A[$Index]})
    return @($A)+@('-mofumachiFontOnly',([IO.Path]::GetFullPath((Join-Path $Directory 'screenshots'))))
}
if($MyInvocation.InvocationName -eq '.'){return}
$Repo=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$Git=Find-SourceGit
$Editor=if($FontOnlyEditorOption){$FontOnlyEditorOption}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf)){throw 'Unity6000.6.4f1 is required.'}
if(Get-Process -Name Unity -ErrorAction SilentlyContinue){throw 'Close Unity Editor before this isolated capture.'}
$null=Assert-MergeRewardVisualApproval $Repo;$null=Read-FontOnlyAmendment $Repo
$Preserved=Move-PreviousCharacterInputs $Repo $Git
Assert-CleanProductSource $Repo $Git;Assert-CharactersVisualLock $Repo;Assert-NoahAuthoredMasterFixed $Repo;Assert-NoahAuthoredFrozenInputs $Repo
$Revision=Read-SourceRevision $Repo;$Fingerprint=Get-CharacterAuthoredSourceFingerprint $Repo
$Run=Join-Path $Repo ('Builds/Diagnostics/font-only-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory -Path $Run|Out-Null
$Record=[ordered]@{scope='font-only-native-capture';gitRevision=$Revision;productFingerprint=$Fingerprint;packed_resource_sha256='';preserved_inputs=$Preserved;source_unchanged=$false;capture_verified=$false;image_count=0;artifacts=@();full_qa_verified=$false;build_generated=$false;pixel_verified=$false;visual_approved=$false;error=''}
$Failure=$null;$Ready=$false
try{
    foreach($Step in @('prepare-noah','prepare-five')){Invoke-NoahAuthoredUnity $Editor (Get-CharactersDirectArguments $Repo $Run $Step) $Run}
    $Record.packed_resource_sha256=Get-PreparedCharactersDigest $Repo
    Invoke-NoahAuthoredUnity $Editor (Get-FontOnlyArguments $Repo $Run $Revision $Fingerprint $Record.packed_resource_sha256) $Run
    Assert-TestRun (Join-Path $Run 'capture.xml') 2 @('ReleaseUxNativeTests','StoryTypographyTests')
    $null=Assert-FontOnlyArtifacts $Run $Revision $Fingerprint $Record.packed_resource_sha256
    if((Get-PreparedCharactersDigest $Repo) -cne $Record.packed_resource_sha256){throw 'Prepared resources changed during font capture.'}
    $Ready=$true
}catch{$Failure=$_;$Record.error=$_.Exception.Message}
finally{
    try{Remove-OwnedCharacterResources $Repo $Run;Remove-OwnedNoahAuthoredResources $Repo $Run}catch{if(-not $Failure){$Failure=$_};$Record.error+=' Cleanup: '+$_.Exception.Message}
    try{
        if((Read-SourceRevision $Repo) -cne $Revision -or (Get-CharacterAuthoredSourceFingerprint $Repo) -cne $Fingerprint){throw 'Font-only source changed during capture.'}
        Assert-CharactersVisualLock $Repo;Assert-NoahAuthoredMasterFixed $Repo;Assert-NoahAuthoredFrozenInputs $Repo;$null=Assert-MergeRewardVisualApproval $Repo;$Record.source_unchanged=$true
        if(-not $Failure -and $Ready){Confirm-FontOnlyCapture $Record $Run $Revision $Fingerprint $Record.packed_resource_sha256}
    }catch{if(-not $Failure){$Failure=$_};$Record.error+=' '+$_.Exception.Message}
    $Record|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $Run 'font-only-source-info.json') -Encoding UTF8
    if($Record.capture_verified){
        try{
            $Zip=Write-FontOnlyReviewPackage $Record $Run $Revision $Fingerprint $Record.packed_resource_sha256
            if($env:OS -eq 'Windows_NT'){Start-Process explorer.exe -ArgumentList ('/select,"'+$Zip+'"')}
        }catch{
            $Failure=$_;$Record.capture_verified=$false;$Record.image_count=0;$Record.artifacts=@();$Record.error+=' Package: '+$_.Exception.Message
            $Record|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $Run 'font-only-source-info.json') -Encoding UTF8
        }
    }elseif($env:OS -eq 'Windows_NT'){Start-Process explorer.exe -ArgumentList ('"'+$Run+'"')}
    Write-Host "Font-only evidence preserved: $Run"
}
if($Failure){throw $Failure}
Write-Host 'Twelve actual Unity PNGs ready for Visual review. No ABC, full tests, APK, install or Pixel run.'
return $Run
