param([string]$UnityEditorPath='')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'build-android.ps1')

function Get-NoahDiagnosticArguments([string]$RepositoryPath,[string]$Directory,[string]$Revision,[string]$Fingerprint) {
    if([string]::IsNullOrWhiteSpace($RepositoryPath) -or [string]::IsNullOrWhiteSpace($Directory) -or $Revision -notmatch '^[a-f0-9]{40}$' -or $Fingerprint -notmatch '^[a-f0-9]{64}$'){throw 'Diagnostic requires exact source identity and nonempty paths.'}
    return @('-batchmode','-projectPath',$RepositoryPath,'-buildTarget','Android','-runTests','-testPlatform','PlayMode',
        '-testFilter','Mofumachi.Tests.ExperienceCaptureTests.NorthNoahBeforeAfterAndFixedStepEvidencePreserveSavedState',
        '-testResults',(Join-Path $Directory 'playmode.xml'),'-logFile',(Join-Path $Directory 'playmode.log'),
        '-mofumachiScreenshots',(Join-Path $Directory 'screenshots'),'-mofumachiSourceRevision',$Revision,'-mofumachiProductFingerprint',$Fingerprint)
}
function Write-NoahDiagnosticArchive([string]$Directory) {
    $Files=@(foreach($Name in @('source-info.json','playmode.xml','playmode.log')){
        $Path=Join-Path $Directory $Name;if(Test-Path -LiteralPath $Path -PathType Leaf){Get-Item -LiteralPath $Path}
    })
    $Screenshots=Join-Path $Directory 'screenshots'
    $Failed=Join-Path $Screenshots 'failed-pairs'
    if(Test-Path -LiteralPath $Failed){$Files+=@(Get-ChildItem -LiteralPath $Failed -File)}
    # A successful diagnostic may also produce six review JPEGs. Keep them,
    # but do not upload unrelated North captures or turn this into full QA.
    if(Test-Path -LiteralPath $Screenshots){$Files+=@(Get-ChildItem -LiteralPath $Screenshots -File | Where-Object {$_.Name -like '37-noah-*.jpg' -or $_.Name -eq 'north-noah-comparison.json'})}
    if($Files.Count -eq 0){throw 'No diagnostic evidence to archive.'}
    $Archive=$Directory+'.zip'
    Compress-Archive -LiteralPath @($Files.FullName) -DestinationPath $Archive -Force
    return $Archive
}
if($MyInvocation.InvocationName -eq '.'){return}

$RepositoryPath=Split-Path -Parent $PSScriptRoot
$GitPath=Find-SourceGit
Assert-CleanProductSource $RepositoryPath $GitPath
$Revision=Read-SourceRevision $RepositoryPath
$Fingerprint=Get-ProductFingerprint $RepositoryPath
Assert-ProductUnchanged $RepositoryPath $Revision $Fingerprint
$UnityEditor=if($UnityEditorPath){$UnityEditorPath}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $UnityEditor -PathType Leaf)){throw 'Unity6000.6.4f1 editor was not found; specify -UnityEditorPath.'}
$OutputDir=Join-Path $RepositoryPath ('Builds/Diagnostics/noah-pair-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+([guid]::NewGuid().ToString('N').Substring(0,8)))
New-Item -ItemType Directory -Path (Join-Path $OutputDir 'screenshots') -Force | Out-Null
$Record=[ordered]@{scope='diagnostic-single-noah-capture-not-full-qa';gitRevision=$Revision;productFingerprint=$Fingerprint;native_test_passed=$false;source_unchanged=$false;qa_apk_generated=$false;pixel_verified=$false;adopted=$false;error=''}
$Failure=''
try {
    Invoke-Unity (Get-NoahDiagnosticArguments $RepositoryPath $OutputDir $Revision $Fingerprint)
    Assert-TestRun (Join-Path $OutputDir 'playmode.xml') 1 @('ExperienceCaptureTests')
    [xml]$Result=Get-Content -LiteralPath (Join-Path $OutputDir 'playmode.xml') -Raw
    $Cases=@($Result.SelectNodes('//test-case'))
    if($Cases.Count -ne 1 -or $Cases[0].fullname -ne 'Mofumachi.Tests.ExperienceCaptureTests.NorthNoahBeforeAfterAndFixedStepEvidencePreserveSavedState'){throw 'Diagnostic did not execute exactly the requested one native method.'}
    $Record.native_test_passed=$true
} catch {$Failure=$_.Exception.Message;$Record.error=$Failure}
try {Assert-ProductUnchanged $RepositoryPath $Revision $Fingerprint;$Record.source_unchanged=$true}
catch {$Failure=$_.Exception.Message;$Record.error=$Failure;$Record.native_test_passed=$false}
$Record | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $OutputDir 'source-info.json') -Encoding UTF8
$Archive=Write-NoahDiagnosticArchive $OutputDir
Write-Host "Noah single-test diagnostic ZIP: $Archive"
Write-Host 'Diagnostic only. No EditMode suite, no unrelated PlayMode tests, no APK build, no Pixel install or save reset.'
if($Failure){throw "Single Noah capture did not pass. Share the diagnostic ZIP above. $Failure"}
Write-Host 'One native capture method passed. Full QA and Pixel/user acceptance remain pending.'
