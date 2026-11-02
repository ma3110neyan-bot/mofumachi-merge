[CmdletBinding()]
param([string]$UnityEditorPath='')
$ErrorActionPreference='Stop'
$HeadSyncEditor=$UnityEditorPath
. (Join-Path $PSScriptRoot 'capture-character-head-sync.ps1') -UnityEditorPath $UnityEditorPath
function Get-HeadCalibrationArguments([string]$Repository,[string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    $A=@(Get-HeadSyncArguments $Repository $Directory $Revision $Fingerprint $Packed)
    for($I=0;$I -lt $A.Count;$I++){$A[$I]=$A[$I].Replace('SixCharactersKeepHeadAndBodySynchronized','SixCharactersKeepCalibratedHeadsAttached').Replace('head-sync-playmode','head-calibration-playmode').Replace('-mofumachiHeadSync','-mofumachiHeadCalibration')}
    return $A
}
function Get-HeadCalibrationDirection([int]$Frame){
    if($Frame -lt 180){return 'Front'};if($Frame -lt 360){return 'Left'};if($Frame -lt 540){return 'Right'};if($Frame -lt 720){return 'Back'};if($Frame -lt 990){return 'Front'};return @('Front','Left','Right','Back')[$Frame%4]
}
function Get-HeadCalibrationImageNames{
    $Boundary=@(179,180,181,359,360,361,539,540,541,719,720,721,749,750,751,809,810,811,989,990,991,1053)
    foreach($Id in @('noah','momo','lulu','pom','fio','miel')){for($Frame=0;$Frame -lt 1054;$Frame++){if($Frame%12 -eq 0 -or $Frame -in $Boundary -or $Frame -ge 990){$Id+'-'+$Frame.ToString('D4')+'.jpg'}}}
}
function Assert-HeadCalibrationProof($P,[string]$Revision,[string]$Fingerprint,[string]$Packed,[string]$Reference){
    if($P.kind -cne 'individual-character-head-calibration' -or $P.gitRevision -cne $Revision -or $P.productFingerprint -cne $Fingerprint -or $P.packed_resource_sha256 -cne $Packed -or $P.reference_sha256 -cne $Reference -or $Reference -cnotmatch '^[a-f0-9]{64}$'){throw 'Individual calibration requires current source, packed resources and exact reference.'}
    foreach($Flag in @('native_unity','non_head_unchanged','head_art_size_angle_unchanged','continuous_verified','jpeg_decode_verified')){if($P.$Flag -isnot [bool] -or -not $P.$Flag){throw "Head calibration invariant failed: $Flag"}}
    foreach($Flag in @('full_qa_verified','pixel_verified','formal_fixed','adopted')){if($P.$Flag -isnot [bool] -or $P.$Flag){throw 'Head-only technical proof cannot claim visual or full acceptance.'}}
    $Ids=@('noah','momo','lulu','pom','fio','miel')
    if(@($P.characters).Count -ne 6 -or (@($P.characters.character) -join ',') -cne ($Ids -join ',') -or @($P.frames).Count -ne 6324){throw 'Individual six-character coverage is incomplete.'}
    foreach($C in @($P.characters)){if($C.result -cne 'PASS' -or $C.visual_result -cne 'PENDING' -or $C.frames -ne 1054 -or $C.misalignments -ne 0){throw 'Every character requires its own current technical PASS; visual acceptance remains separate.'}}
    for($I=0;$I -lt 6324;$I++){
        $S=$P.frames[$I];$Frame=$I%1054;$Id=$Ids[[int][Math]::Floor($I/1054)];$Direction=Get-HeadCalibrationDirection $Frame
        if($S.character -cne $Id -or $S.frame -ne $Frame -or $S.direction -cne $Direction -or $S.front_single_mesh -isnot [bool] -or $S.front_single_mesh -ne ($Direction -ceq 'Front') -or $S.blink -isnot [bool] -or $S.blink -ne ($Id -cne 'noah' -and $Direction -cne 'Front' -and $Direction -cne 'Back' -and $Frame%17 -eq 0)){throw 'Continuous direction, blink or frame order missing.'}
        if($null -eq $S.error -or [double]::IsNaN([double]$S.error) -or [double]::IsInfinity([double]$S.error) -or [double]$S.error -lt 0 -or [double]$S.error -gt .002){throw 'Head/body frame error.'}
        foreach($Coordinate in @('root_x','root_y')){if($null -eq $S.$Coordinate -or [double]::IsNaN([double]$S.$Coordinate) -or [double]::IsInfinity([double]$S.$Coordinate)){throw 'Common root coordinates missing.'}}
        if($Frame -ge 750 -and $Frame -lt 810){$Last=$P.frames[$I-1];if($S.root_x -ne $Last.root_x -or $S.root_y -ne $Last.root_y){throw 'Root moved during stop interval.'}}
        if($Frame -eq 811){$Last=$P.frames[$I-1];if($S.root_x -eq $Last.root_x -and $S.root_y -eq $Last.root_y){throw 'Root did not restart.'}}
    }
}
function Read-HeadCalibrationJpegSize([byte[]]$B){
    if($B.Length -lt 20 -or $B[0] -ne 255 -or $B[1] -ne 216 -or $B[-2] -ne 255 -or $B[-1] -ne 217){throw 'Native JPEG is truncated or invalid.'}
    $I=2;$Size=$null
    while($I+4 -lt $B.Length){
        if($B[$I] -ne 255){throw 'Invalid JPEG marker.'};while($B[$I] -eq 255){$I++};$M=[int]$B[$I];$I++
        if($M -eq 217){break};$Length=256*[int]$B[$I]+[int]$B[$I+1]
        if($Length -lt 2 -or $I+$Length -gt $B.Length){throw 'Invalid JPEG segment.'}
        if($M -eq 218){if($null -eq $Size -or $I+$Length+2 -ge $B.Length){throw 'JPEG scan data missing.'};return $Size}
        if($M -in @(192,193,194)){if($Length -lt 8){throw 'Truncated JPEG size.'};$Size=@((256*[int]$B[$I+5]+[int]$B[$I+6]),(256*[int]$B[$I+3]+[int]$B[$I+4]))};$I+=$Length
    };throw 'JPEG dimensions missing.'
}
function Assert-HeadCalibrationJpegDecoded([string]$File){
    # This capture route runs on Windows Unity. GDI+ performs an independent
    # full decode; no header-only or unsupported-platform fallback is accepted.
    if([IO.Path]::DirectorySeparatorChar -ne '\'){throw 'Native JPEG acceptance requires the Windows full image decoder.'}
    Add-Type -AssemblyName System.Drawing
    $Stream=$null;$Image=$null;$Bitmap=$null;$Graphics=$null
    try{
        $Stream=[IO.File]::OpenRead($File);$Image=[Drawing.Image]::FromStream($Stream,$false,$true)
        if($Image.Width -ne 480 -or $Image.Height -ne 600){throw 'Decoded JPEG dimensions wrong.'}
        $Bitmap=New-Object Drawing.Bitmap 480,600;$Graphics=[Drawing.Graphics]::FromImage($Bitmap);$Graphics.DrawImage($Image,0,0,480,600)
        $null=$Bitmap.GetPixel(479,599)
    }finally{if($Graphics){$Graphics.Dispose()};if($Bitmap){$Bitmap.Dispose()};if($Image){$Image.Dispose()};if($Stream){$Stream.Dispose()}}
}
function Assert-HeadCalibrationImages([string]$Directory,$P){
    $Expected=@(Get-HeadCalibrationImageNames)
    if(@($P.images).Count -ne $Expected.Count -or @($P.images.file | Select-Object -Unique).Count -ne $Expected.Count -or @(Compare-Object $Expected @($P.images.file)).Count){throw 'Character-only JPEG sequence incomplete.'}
    foreach($Image in @($P.images)){
        $File=Join-Path $Directory $Image.file
        if(-not(Test-Path -LiteralPath $File -PathType Leaf) -or ((Get-Item -LiteralPath $File -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or $Image.sha256 -cnotmatch '^[a-f0-9]{64}$' -or (Get-Item -LiteralPath $File).Length -ne $Image.bytes -or (Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash -ine $Image.sha256){throw 'Native JPEG hash/bytes mismatch.'}
        $Size=@(Read-HeadCalibrationJpegSize ([IO.File]::ReadAllBytes($File)))
        if($Image.width -ne 480 -or $Image.height -ne 600 -or $Size[0] -ne 480 -or $Size[1] -ne 600){throw 'Character-only JPEG dimensions wrong.'}
        Assert-HeadCalibrationJpegDecoded $File
    }
}
function Assert-HeadCalibrationResults([string]$Path){
    [xml]$Xml=Get-Content -Raw -Encoding UTF8 -LiteralPath $Path;$Cases=@($Xml.SelectNodes('//test-case'))
    if($Cases.Count -ne 1 -or $Cases[0].fullname -cne 'Mofumachi.Tests.ExperienceCaptureTests.SixCharactersKeepCalibratedHeadsAttached'){throw 'Exactly the individual head calibration method is required.'}
    Assert-TestRun $Path 1 @('ExperienceCaptureTests')
}
if($MyInvocation.InvocationName -eq '.'){return}
$Repository=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path;$Git=Find-SourceGit
$Preserved=Move-PreviousCharacterInputs $Repository $Git
Assert-CleanProductSource $Repository $Git;Assert-NoahAuthoredMasterFixed $Repository;Assert-NoahAuthoredFrozenInputs $Repository
$Editor=if($HeadSyncEditor){$HeadSyncEditor}else{Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'}
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf) -or -not(Test-Path -LiteralPath (Join-Path (Split-Path -Parent $Editor) 'Data/PlaybackEngines/AndroidPlayer'))){throw 'Unity6000.6.4f1 with Android support required.'}
$Revision=Read-SourceRevision $Repository;$Fingerprint=Get-CharacterAuthoredSourceFingerprint $Repository
$Run=Join-Path $Repository ('Builds/Diagnostics/characters-head-calibration-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory -Path $Run | Out-Null
$Record=[ordered]@{scope='individual-head-body-calibration-only';gitRevision=$Revision;productFingerprint=$Fingerprint;packed_resource_sha256='';preserved_inputs=$Preserved;head_native_verified=$false;full_qa_verified=$false;pixel_verified=$false;formal_fixed=$false;adopted=$false;source_unchanged=$false;error=''}
$Failure=$null;$FirstZip='';Write-Host "Exact head-calibration run: $Run"
try{
    $Common=@('-batchmode','-quit','-projectPath',$Repository,'-buildTarget','Android')
    Invoke-NoahAuthoredUnity $Editor ($Common+@('-executeMethod','Mofumachi.Editor.NoahAuthoredBuild.Prepare','-mofumachiNoahAuthoredDirectory',$Run,'-logFile',(Join-Path $Run 'prepare-noah.log'))) $Run
    Invoke-NoahAuthoredUnity $Editor ($Common+@('-executeMethod','Mofumachi.Editor.CharacterAuthoredBuild.PrepareAll','-mofumachiCharacterDirectory',$Run,'-logFile',(Join-Path $Run 'prepare-five.log'))) $Run
    $Record.packed_resource_sha256=Get-PreparedCharactersDigest $Repository
    Invoke-NoahAuthoredUnity $Editor (Get-HeadCalibrationArguments $Repository $Run $Revision $Fingerprint $Record.packed_resource_sha256) $Run
    Assert-HeadCalibrationResults (Join-Path $Run 'head-calibration-playmode.xml')
    $Proof=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $Run 'screenshots/head-calibration/head-calibration-proof.json') | ConvertFrom-Json
    Assert-HeadCalibrationProof $Proof $Revision $Fingerprint $Record.packed_resource_sha256 (Get-FinishTextHash (Join-Path $Repository 'Tests/Fixtures/character-head-calibration-2026-11-02.json'))
    Assert-HeadCalibrationImages (Join-Path $Run 'screenshots/head-calibration') $Proof
    if((Get-PreparedCharactersDigest $Repository) -cne $Record.packed_resource_sha256){throw 'Packed inputs changed during head capture.'};$Record.head_native_verified=$true
}catch{$Failure=$_;$Record.error=$_.Exception.Message}
finally{
    try{Remove-OwnedCharacterResources $Repository $Run;Remove-OwnedNoahAuthoredResources $Repository $Run}catch{if(-not $Failure){$Failure=$_};$Record.error+=' Cleanup: '+$_.Exception.Message}
    try{
        if((Read-SourceRevision $Repository) -cne $Revision -or (Get-CharacterAuthoredSourceFingerprint $Repository) -cne $Fingerprint){throw 'Source changed during head capture.'}
        Assert-NoahAuthoredMasterFixed $Repository;Assert-NoahAuthoredFrozenInputs $Repository;$Record.source_unchanged=$true
    }catch{if(-not $Failure){$Failure=$_};$Record.error+=' '+$_.Exception.Message}
    $Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Run 'source-info.json') -Encoding UTF8
    try{foreach($Zip in @(Write-CharactersReviewArchives $Run)){if(-not $FirstZip){$FirstZip=$Zip};Write-Host "Head-calibration review ZIP: $Zip"}}catch{if(-not $Failure){$Failure=$_};Write-Host $_}
    Write-Host 'One head-only native method for six characters. No full suites, APK/install, passing/conversation change or Save reset.'
    if($FirstZip -and $env:OS -eq 'Windows_NT'){Start-Process -FilePath explorer.exe -ArgumentList ('/select,"'+$FirstZip+'"')}
}
if($Failure){throw $Failure}
Write-Host 'Head calibration technical gate passed: six individual1054-frame sequences. Visual/Pixel acceptance remains pending.'
