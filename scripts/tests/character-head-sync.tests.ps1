$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../capture-character-head-sync.ps1')
function RejectHead([scriptblock]$Action){$Caught=$false;try{& $Action}catch{$Caught=$true};if(-not $Caught){throw 'Invalid head-sync evidence accepted.'}}
$Args=@(Get-HeadSyncArguments 'repo with spaces' 'run with spaces' ('a'*40) ('b'*64) ('c'*64))
if($Args[[Array]::IndexOf($Args,'-testFilter')+1] -cne 'Mofumachi.Tests.ExperienceCaptureTests.SixCharactersKeepHeadAndBodySynchronized' -or '-quit' -in $Args){throw 'Head capture must be exactly one native method.'}
RejectHead {Get-HeadSyncArguments 'r' 'd' '' ('b'*64) ('c'*64)}
$Ids=@('fio','lulu','miel','momo','noah','pom');$States=[ordered]@{'front-idle'='Front';'front-to-left-start'='Left';'left-walk'='Left';'left-to-right-walk'='Right';'right-walk'='Right';'right-to-back-walk'='Back';'back-walk'='Back';'back-to-front-stop-before'='Front';'front-stop-after'='Front'}
$Samples=@();foreach($Id in $Ids){foreach($State in $States.Keys){for($Tick=0;$Tick -lt 8;$Tick++){$Samples+=@{character=$Id;state=$State;direction=$States[$State];tick=$Tick;error=0;head_x=1;head_y=2;body_x=1;body_y=2;front_single_mesh=($States[$State] -ceq 'Front');blink=($States[$State] -cne 'Front' -and $Id -cne 'noah' -and $Tick -eq 7)}}}}
$Order=@('noah','fio','lulu','miel','momo','pom');$Continuous=@()
foreach($Id in $Order){for($Tick=0;$Tick -lt 240;$Tick++){
    $Direction=if($Tick -lt 30){'Front'}elseif($Tick -lt 60){'Left'}elseif($Tick -lt 90){'Right'}elseif($Tick -lt 150){'Back'}elseif($Tick -lt 180){'Left'}else{@('Right','Back','Front','Left')[$Tick%4]}
    $Continuous+=@{character=$Id;state='continuous';direction=$Direction;tick=$Tick;error=0;head_x=1;head_y=2;body_x=1;body_y=2;front_single_mesh=($Direction -ceq 'Front');blink=($Id -cne 'noah' -and $Direction -cne 'Front' -and $Tick%17 -eq 0)}
}}
$Good=@{kind='six-character-approved-head-body-restore';reference_revision='7c98804a78f07546fb8176b320904e8af44f6542';oracle='approved-noah-body-local-rest-frame';verification_order=$Order;continuous_samples=$Continuous;noah_first_verified=$true;continuous_verified=$true;gitRevision=('a'*40);productFingerprint=('b'*64);packed_resource_sha256=('c'*64);native_unity=$true;save_unchanged=$true;non_head_unchanged=$true;head_art_size_angle_unchanged=$true;full_qa_verified=$false;pixel_verified=$false;formal_fixed=$false;adopted=$false;misalignments=0;unmet_characters=0;characters=$Ids;samples=$Samples;images=@(1..36 | ForEach-Object {@{file="synthetic-$_.png"}})} | ConvertTo-Json -Depth 8
$P=$Good | ConvertFrom-Json;Assert-HeadSyncProof $P ('a'*40) ('b'*64) ('c'*64)
foreach($Case in @('missing','duplicate','foreign','direction','blink','drift','nan','identity','flag','art','unmet','continuous','order','reference','old-kind')){
    $P=$Good | ConvertFrom-Json
    switch($Case){
        missing {$P.samples=$P.samples[1..431]}
        duplicate {$P.samples[1]=$P.samples[0]}
        foreign {$P.samples[0].character='wrong'}
        direction {$P.samples[0].direction='Back'}
        blink {$P.samples[15].blink=$false}
        drift {$P.samples[8].head_x=2}
        nan {$P.samples[8].error=[double]::NaN}
        identity {$P.gitRevision=('d'*40)}
        flag {$P.formal_fixed=$true}
        art {$P.head_art_size_angle_unchanged=$false}
        unmet {$P.unmet_characters=1}
        continuous {$P.continuous_samples[65].head_x=3}
        order {$P.verification_order=@('fio','noah','lulu','miel','momo','pom')}
        reference {$P.reference_revision=('d'*40)}
        old-kind {$P.kind='six-character-head-body-sync-only'}
    }
    RejectHead {Assert-HeadSyncProof $P ('a'*40) ('b'*64) ('c'*64)}
}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-head-guards-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $Root | Out-Null
try{
    $Path=Join-Path $Root 'test.xml'
    '<test-run total="1" passed="1" failed="0" skipped="0" result="Passed"><test-suite type="TestFixture" name="ExperienceCaptureTests" result="Passed"><test-case fullname="Mofumachi.Tests.ExperienceCaptureTests.SixCharactersKeepHeadAndBodySynchronized" result="Passed" /></test-suite></test-run>' | Set-Content -LiteralPath $Path -Encoding UTF8
    Assert-HeadSyncResults $Path
    (Get-Content -Raw -LiteralPath $Path).Replace('SixCharactersKeepHeadAndBodySynchronized','Unrelated') | Set-Content -LiteralPath $Path -Encoding UTF8;RejectHead {Assert-HeadSyncResults $Path}
    $P=$Good | ConvertFrom-Json;RejectHead {Assert-HeadSyncImages $Root $P}
    $Pixels=[Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAtAAAAUAAQAAAABGtViQAAAAiElEQVR42u3BMQEAAADCoPVPbQwfoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAlwHHDwABi4bK8wAAAABJRU5ErkJggg==')
    $P=$Good | ConvertFrom-Json;$P.images=@()
    foreach($State in $States.Keys){foreach($Tick in @(0,7)){foreach($Kind in @('before','after')){
        $Name=$State+'-'+$Tick+'-'+$Kind+'.png';$File=Join-Path $Root $Name;[IO.File]::WriteAllBytes($File,$Pixels)
        $P.images+=@{file=$Name;sha256=(Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant();bytes=$Pixels.Length;width=720;height=1280}
    }}}
    Assert-HeadSyncImages $Root $P
    $P.images[0].width=1;RejectHead {Assert-HeadSyncImages $Root $P};$P.images[0].width=720
    $P.images[0].sha256=('d'*64);RejectHead {Assert-HeadSyncImages $Root $P}

}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Approved-rest oracle, Noah-first1440 continuous frames,432 comparison samples and old-proof/drift rejection passed. Synthetic evidence only.'
