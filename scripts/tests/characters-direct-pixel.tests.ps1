$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$Route=Join-Path $PSScriptRoot '../prepare-characters-direct-pixel.ps1'
if(-not(Test-Path -LiteralPath $Route)){throw 'Direct Pixel route missing; existing route still requires Unity tests.'}
. $Route
foreach($Step in @('prepare-noah','prepare-five','build')){
    $Spaced=Join-Path ([IO.Path]::GetTempPath()) 'Mofu project'
    $A=@(Get-CharactersDirectArguments $Spaced (Join-Path $Spaced 'Builds/Diagnostics/new') $Step)
    foreach($Forbidden in @('-runTests','-testPlatform','-testFilter')){if($Forbidden -cin $A){throw 'Direct preview must never start a Unity test runner.'}}
    if('-executeMethod' -cnotin $A -or '-quit' -cnotin $A -or '-batchmode' -cnotin $A){throw 'Expected build/preparation entry only.'}
}
function RejectDirect([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Unfinished/foreign direct APK accepted.'}}
RejectDirect {Get-CharactersDirectArguments 'repo' 'run' 'full-qa'}
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-direct-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $Root | Out-Null
try{
    $Revision='a'*40;$Fingerprint='b'*64;$Packed='c'*64
    $Apk=Join-Path $Root 'characters-authored.apk';[IO.File]::WriteAllText($Apk,'synthetic APK bytes; not a native build')
    $Hash=(Get-FileHash -LiteralPath $Apk -Algorithm SHA256).Hash.ToLowerInvariant()
    $Info=@{scope='characters-direct-pixel-no-tests';mode='authored';gitRevision=$Revision;productFingerprint=$Fingerprint;packed_resource_sha256=$Packed;sha256=$Hash;apk='characters-authored.apk';package='com.mofumachi.merge.qa.townlife';tests_run=$false;full_qa_verified=$false;pixel_verified=$false}
    $Source=@{scope='characters-direct-pixel-no-tests';gitRevision=$Revision;productFingerprint=$Fingerprint;packed_resource_sha256=$Packed;build_generated=$true;source_unchanged=$true;tests_run=$false;full_qa_verified=$false;pixel_verified=$false;error=''}
    $InfoPath=Join-Path $Root 'characters-authored-build.json';$SourcePath=Join-Path $Root 'source-info.json'
    $GoodInfo=$Info | ConvertTo-Json;$GoodSource=$Source | ConvertTo-Json
    $GoodInfo | Set-Content -LiteralPath $InfoPath -Encoding UTF8;$GoodSource | Set-Content -LiteralPath $SourcePath -Encoding UTF8
    Read-CharactersDirectBuild $Root $Revision $Fingerprint $Packed | Out-Null
    foreach($Case in @('revision','packed','package','apk','tests','qa','hash','mode')){
        $Bad=$GoodInfo | ConvertFrom-Json
        switch($Case){revision {$Bad.gitRevision='d'*40};packed {$Bad.packed_resource_sha256='d'*64};package {$Bad.package='com.other.app'};apk {$Bad.apk='../old.apk'};tests {$Bad.tests_run=$true};qa {$Bad.full_qa_verified=$true};hash {$Bad.sha256='d'*64};mode {$Bad.mode='baseline'}}
        $Bad | ConvertTo-Json | Set-Content -LiteralPath $InfoPath -Encoding UTF8
        RejectDirect {Read-CharactersDirectBuild $Root $Revision $Fingerprint $Packed}
    }
    $GoodInfo | Set-Content -LiteralPath $InfoPath -Encoding UTF8
    foreach($Case in @('failed','source','built','scope')){
        $Bad=$GoodSource | ConvertFrom-Json
        switch($Case){failed {$Bad.error='build failed'};source {$Bad.source_unchanged=$false};built {$Bad.build_generated=$false};scope {$Bad.scope='old-full-qa'}}
        $Bad | ConvertTo-Json | Set-Content -LiteralPath $SourcePath -Encoding UTF8
        RejectDirect {Read-CharactersDirectBuild $Root $Revision $Fingerprint $Packed}
    }
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Direct no-test arguments and exact APK/source/packed/failed-run rejection passed. Synthetic route checks only; no Unity/device executed.'
