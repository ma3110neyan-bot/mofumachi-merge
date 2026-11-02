$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../capture-character-head-calibration.ps1')
function RejectCalibration([scriptblock]$Action){$Caught=$false;try{& $Action | Out-Null}catch{$Caught=$true};if(-not $Caught){throw 'Invalid head calibration evidence accepted.'}}
$Args=@(Get-HeadCalibrationArguments 'repo with spaces' 'run with spaces' ('a'*40) ('b'*64) ('c'*64))
if($Args[[Array]::IndexOf($Args,'-testFilter')+1] -cne 'Mofumachi.Tests.ExperienceCaptureTests.SixCharactersKeepCalibratedHeadsAttached' -or '-quit' -in $Args -or '-mofumachiHeadCalibration' -notin $Args){throw 'Exactly the individual head-only method is required.'}
$Ids=@('noah','momo','lulu','pom','fio','miel');$Frames=[Collections.Generic.List[object]]::new()
foreach($Id in $Ids){for($F=0;$F -lt 1054;$F++){$D=Get-HeadCalibrationDirection $F;$Frames.Add(@{character=$Id;frame=$F;direction=$D;front_single_mesh=($D -ceq 'Front');blink=($Id -cne 'noah' -and $D -cne 'Front' -and $D -cne 'Back' -and $F%17 -eq 0);root_x=$(if($F -lt 750){$F*.01}elseif($F -lt 810){7.49}else{($F-60)*.01});root_y=0;error=0})}}
$Good=@{kind='individual-character-head-calibration';gitRevision=('a'*40);productFingerprint=('b'*64);packed_resource_sha256=('c'*64);reference_sha256=('d'*64);native_unity=$true;non_head_unchanged=$true;head_art_size_angle_unchanged=$true;continuous_verified=$true;jpeg_decode_verified=$true;full_qa_verified=$false;pixel_verified=$false;formal_fixed=$false;adopted=$false;characters=@($Ids | ForEach-Object {@{character=$_;result='PASS';visual_result='PENDING';frames=1054;misalignments=0}});frames=$Frames.ToArray()} | ConvertTo-Json -Depth 8
$P=$Good | ConvertFrom-Json;Assert-HeadCalibrationProof $P ('a'*40) ('b'*64) ('c'*64) ('d'*64)
foreach($Case in @('old','reference','source','missing','duplicate','turn','drift','nan','blink','front','single-fail','fake-visual','pixel','non-head','short-sequence','moving-stop')){
    $P=$Good | ConvertFrom-Json
    switch($Case){
        old {$P.kind='six-character-approved-head-body-restore'}
        reference {$P.reference_sha256=('e'*64)}
        source {$P.gitRevision=('e'*40)}
        missing {$P.frames=$P.frames[1..6323]}
        duplicate {$P.frames[180]=$P.frames[181]}
        turn {$P.frames[180].direction='Front'}
        drift {$P.frames[180].error=.01}
        nan {$P.frames[180].error=[double]::NaN}
        blink {$P.frames[1258].blink=-not $P.frames[1258].blink}
        front {$P.frames[0].front_single_mesh=$false}
        single-fail {$P.characters[4].result='FAIL'}
        fake-visual {$P.characters[0].visual_result='PASS'}
        pixel {$P.pixel_verified=$true}
        non-head {$P.non_head_unchanged=$false}
        short-sequence {$P.characters[2].frames=240}
        moving-stop {$P.frames[760].root_x=9}
    }
    RejectCalibration {Assert-HeadCalibrationProof $P ('a'*40) ('b'*64) ('c'*64) ('d'*64)}
}
$Names=@(Get-HeadCalibrationImageNames)
if($Names.Count -ne @($Names | Select-Object -Unique).Count -or 'noah-0180.jpg' -notin $Names -or 'miel-1053.jpg' -notin $Names -or 'noah-1001.jpg' -notin $Names -or 'miel-1052.jpg' -notin $Names){throw 'Turn-boundary JPEG coverage missing.'}
# Real source-preview JPEG only tests encoding/dimension parsing; not native evidence.
$Repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$Jpeg=Join-Path $Repo 'References/Review/character-head-restore-preview-2026-11-02/noah-four-directions.jpg'
$Size=@(Read-HeadCalibrationJpegSize ([IO.File]::ReadAllBytes($Jpeg)))
if($Size[0] -ne 1980 -or $Size[1] -ne 2020){throw 'JPEG size parser failed.'}
RejectCalibration {Read-HeadCalibrationJpegSize ([byte[]]@(1,2,3))}
RejectCalibration {Read-HeadCalibrationJpegSize ([byte[]]@(255,216,255,192,0,17,8,2,88,1,224,3,1,17,0,2,17,0,3,17,0,255,217))}
Write-Host 'Six individual1054-frame records; old-proof, one-character failure, drift, missing-frame and false acceptance rejected. Synthetic checks only.'
