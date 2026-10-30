$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$Route=Join-Path $PSScriptRoot '../prepare-characters-pixel-preview.ps1'
if(-not(Test-Path -LiteralPath $Route)){throw 'Five-character integration route is missing.'}
. $Route
function RejectRollout([scriptblock]$Action){$Caught=$false;try{& $Action}catch{$Caught=$true};if(-not $Caught){throw 'Invalid rollout input accepted.'}}
foreach($Platform in @('EditMode','PlayMode')){
    $A=@(Get-CharactersFullArguments 'repo with spaces' 'out with spaces' $Platform ('a'*40) ('b'*64))
    if('-testFilter' -in $A -or '-quit' -in $A -or $A[[Array]::IndexOf($A,'-testPlatform')+1] -cne $Platform){throw 'Full suite was filtered or quit early.'}
}
RejectRollout {Get-CharactersFullArguments 'repo' 'out' 'invalid' ('a'*40) ('b'*64)}
$A=@(Get-CharactersSixArguments 'repo with spaces' 'out with spaces' ('a'*40) ('b'*64))
if($A[[Array]::IndexOf($A,'-testFilter')+1] -cne 'Mofumachi.Tests.ExperienceCaptureTests.SixAuthoredCharactersPreserveWorldAndInput' -or '-mofumachiCharactersSixPreview' -notin $A){throw 'Wrong six-character native route.'}
$ModeLines=@('1000.0 123 456 I Unity : MOFUMACHI_NOAH_AUTHORED_MODE authored')
foreach($Id in @('momo','lulu','pom','fio','miel')){$ModeLines+="1000.0 123 456 I Unity : MOFUMACHI_CHARACTER_AUTHORED_MODE $Id authored"}
$Log=$ModeLines -join "`n"
Assert-CharactersRunningMode $Log 'authored' 999 '123'
RejectRollout {Assert-CharactersRunningMode $Log 'baseline' 999 '123'}
RejectRollout {Assert-CharactersRunningMode $Log 'authored' 1001 '123'}
RejectRollout {Assert-CharactersRunningMode ($Log.Replace('miel authored','miel current')) 'authored' 999 '123'}
RejectRollout {Assert-CharactersRunningMode ($Log.Replace('noah','other').Replace('NOAH','OTHER')) 'authored' 999 '123'}
RejectRollout {Assert-CharactersRunningMode $Log 'authored' 999 '999'}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-five-route-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $Root | Out-Null
try{
    $Source=@{gitRevision=('a'*40);productFingerprint=('b'*64);full_qa_verified=$true;single_native_verified=$true;six_native_verified=$true;builds_generated=$true;source_unchanged=$true;error=''}
    $Source | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Root 'source-info.json') -Encoding UTF8
    foreach($Kind in @('baseline','authored')){
        $Apk=Join-Path $Root "characters-$Kind.apk";[IO.File]::WriteAllText($Apk,'synthetic guard input '+$Kind)
        @{mode=$Kind;package='com.mofumachi.merge.qa.townlife';apk="characters-$Kind.apk";gitRevision=('a'*40);productFingerprint=('b'*64);packed_resource_sha256=('c'*64);sha256=(Get-FileHash -LiteralPath $Apk -Algorithm SHA256).Hash.ToLowerInvariant();full_qa_verified=$true;six_native_verified=$true;pixel_verified=$false;adopted=$false} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Root "characters-$Kind-build.json") -Encoding UTF8
    }
    Read-CharactersPair $Root | Out-Null
    $InfoPath=Join-Path $Root 'characters-authored-build.json';$Good=Get-Content -Raw -LiteralPath $InfoPath
    foreach($Flag in @('full_qa_verified','six_native_verified','pixel_verified','adopted')){
        $Bad=$Good | ConvertFrom-Json;$Bad.$Flag=-not $Bad.$Flag;$Bad | ConvertTo-Json | Set-Content -LiteralPath $InfoPath -Encoding UTF8;RejectRollout {Read-CharactersPair $Root}
    }
    Set-Content -LiteralPath $InfoPath -Value $Good -Encoding UTF8
    $Source.source_unchanged=$false;$Source | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Root 'source-info.json') -Encoding UTF8;RejectRollout {Read-CharactersPair $Root}
    $Source.source_unchanged=$true;$Source | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Root 'source-info.json') -Encoding UTF8
    [IO.File]::AppendAllText((Join-Path $Root 'characters-authored.apk'),'tampered');RejectRollout {Read-CharactersPair $Root}
    foreach($Folder in @('one','two')){New-Item -ItemType Directory -Path (Join-Path $Root $Folder) | Out-Null;[IO.File]::WriteAllText((Join-Path $Root "$Folder/proof.json"),'synthetic record')}
    $Zips=@(Write-CharactersReviewArchives $Root);if($Zips.Count -ne 1){throw 'Unexpected review batch.'}
    $Archive=[IO.Compression.ZipFile]::OpenRead($Zips[0]);try{$Names=@($Archive.Entries | Select-Object -ExpandProperty FullName);if('one/proof.json' -notin $Names -or 'two/proof.json' -notin $Names -or @($Names | Where-Object {$_ -like '*.apk'}).Count){throw 'Archive flattened paths or included APK.'}}finally{$Archive.Dispose()}
    Remove-Item -LiteralPath $Zips[0]
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Rollout route/mode, pair/source/hash and archive guards passed; synthetic inputs, no Unity/APK/Pixel.'
