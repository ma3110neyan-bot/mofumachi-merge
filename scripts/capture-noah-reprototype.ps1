param([string]$UnityEditorPath='')
$ErrorActionPreference='Stop'
$RequestedUnityEditor=$UnityEditorPath
. (Join-Path $PSScriptRoot 'diagnose-noah-capture.ps1')
. (Join-Path $PSScriptRoot 'noah-reprototype-review.ps1')
function Get-NoahReprototypeArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint) {
    $A=@(Get-NoahDiagnosticArguments $Repository $Directory $Revision $Fingerprint)
    $A[[Array]::IndexOf($A,'-testFilter')+1]='Mofumachi.Tests.ExperienceCaptureTests.NoahReprototypeContinuousSequencePreservesArtAndSavedState'
    return $A
}
function Write-NoahReprototypeArchives([string]$Directory,[long]$MaxPartBytes=24MB,[string[]]$ExtraNames=@()) {
    if($MaxPartBytes -le 0){throw 'Positive archive budget required.'}
    foreach($Name in $ExtraNames){if($Name -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$' -or $Name.EndsWith('.apk')){throw 'Extra review records must be local named metadata/logs, not APKs or external paths.'}}
    $Files=@(foreach($Name in (@('source-info.json','playmode.xml','playmode.log')+$ExtraNames)){$P=Join-Path $Directory $Name;if(Test-Path -LiteralPath $P -PathType Leaf){Get-Item -LiteralPath $P}})
    $Shots=Join-Path $Directory 'screenshots/noah-reprototype';if(Test-Path -LiteralPath $Shots){$Files+=@(Get-ChildItem -LiteralPath $Shots -Recurse -File | Sort-Object FullName)}
    if($Files.Count -eq 0 -or @($Files.Name | Select-Object -Unique).Count -ne $Files.Count){throw 'Empty or colliding diagnostic names.'}
    $Batch=New-Object 'System.Collections.Generic.List[string]';$Bytes=0L;$Part=1
    foreach($F in $Files){if($F.Length -gt $MaxPartBytes){throw 'Single evidence file exceeds budget.'};if($Batch.Count -gt 0 -and $Bytes+$F.Length -gt $MaxPartBytes){$Zip=$Directory+('-{0:D2}.zip' -f $Part);Compress-Archive -LiteralPath $Batch.ToArray() -DestinationPath $Zip -Force;$Zip;$Part++;$Bytes=0L;$Batch.Clear()};$Batch.Add($F.FullName);$Bytes+=$F.Length}
    if($Batch.Count){$Zip=$Directory+('-{0:D2}.zip' -f $Part);Compress-Archive -LiteralPath $Batch.ToArray() -DestinationPath $Zip -Force;$Zip}
}
if($MyInvocation.InvocationName -eq '.'){return}
$Repository=Split-Path -Parent $PSScriptRoot;$Git=Find-SourceGit
Assert-CleanProductSource $Repository $Git
$Revision=Read-SourceRevision $Repository;$Fingerprint=Get-ProductFingerprint $Repository
Assert-ProductUnchanged $Repository $Revision $Fingerprint
Assert-OriginalBaseForCandidate $Repository
Assert-ApprovedVisualLock $Repository
$Editor=if($RequestedUnityEditor){$RequestedUnityEditor}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf)){throw 'Unity6000.6.4f1 editor not found; specify -UnityEditorPath.'}
$OutputDir=Join-Path $Repository ('Builds/Diagnostics/noah-reprototype-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+([guid]::NewGuid().ToString('N').Substring(0,8)))
New-Item -ItemType Directory -Path (Join-Path $OutputDir 'screenshots/noah-reprototype') -Force | Out-Null
$Record=[ordered]@{scope='single-native-Noah-reprototype-not-full-QA';gitRevision=$Revision;productFingerprint=$Fingerprint;native_test_passed=$false;source_unchanged=$false;qa_apk_generated=$false;pixel_verified=$false;adopted=$false;error=''};$Failure=''
try {
    Invoke-Unity (Get-NoahReprototypeArguments $Repository $OutputDir $Revision $Fingerprint) -EditorPath $Editor
    Assert-TestRun (Join-Path $OutputDir 'playmode.xml') 1 @('ExperienceCaptureTests')
    [xml]$Result=Get-Content -LiteralPath (Join-Path $OutputDir 'playmode.xml') -Raw -Encoding UTF8
    $Cases=@($Result.SelectNodes('//test-case'));if($Cases.Count -ne 1 -or $Cases[0].fullname -ne 'Mofumachi.Tests.ExperienceCaptureTests.NoahReprototypeContinuousSequencePreservesArtAndSavedState'){throw 'Not exactly the one Noah method.'}
    $Atlas=(Get-FileHash -LiteralPath (Join-Path $Repository 'Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png') -Algorithm SHA256).Hash.ToLowerInvariant()
    Assert-NoahReprototypeEvidence (Join-Path $OutputDir 'screenshots/noah-reprototype') $Revision $Fingerprint $Atlas
    $Record.native_test_passed=$true
}catch{$Failure=$_.Exception.Message;$Record.error=$Failure}
try{Assert-ProductUnchanged $Repository $Revision $Fingerprint;$Record.source_unchanged=$true}catch{$Failure=$_.Exception.Message;$Record.error=$Failure;$Record.native_test_passed=$false}
$Record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDir 'source-info.json') -Encoding UTF8
foreach($Zip in @(Write-NoahReprototypeArchives $OutputDir)){Write-Host "Noah-only review ZIP: $Zip"}
Write-Host 'One Noah method only. No full EditMode/PlayMode, APK, Pixel install, uninstall or save reset.'
if($Failure){throw "Noah capture failed. Share these ZIPs; do not rerun full QA. $Failure"}
$ReviewHtml=Join-Path $OutputDir 'screenshots/noah-reprototype/noah-reprototype.html'
Write-Host ('Noah Before/After and continuous sequence: '+$ReviewHtml)
Start-Process -FilePath $ReviewHtml
