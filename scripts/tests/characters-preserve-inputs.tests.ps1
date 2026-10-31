$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$Script=Join-Path $PSScriptRoot '../preserve-characters-prepared-inputs.ps1'
if(-not(Test-Path -LiteralPath $Script)){throw 'Reversible prepared-input preservation is missing.'}
. $Script
$Git=(Get-Command git).Source
function RejectPreserve([scriptblock]$Action){$Caught=$false;try{& $Action | Out-Null}catch{$Caught=$true};if(-not $Caught){throw 'Unsafe prepared inputs were moved.'}}
function Fixture([string]$Root){
    New-Item -ItemType Directory -Path $Root | Out-Null
    & $Git -C $Root init --quiet
    if($LASTEXITCODE -ne 0){throw 'Synthetic git setup failed.'}
    $Run=Join-Path $Root 'Builds/Diagnostics/characters-authored-synthetic'
    New-Item -ItemType Directory -Path $Run -Force | Out-Null
    foreach($Relative in @('CharacterAuthored','NoahAuthoredTrial')){
        $Folder=Join-Path $Root ('Assets/Mofumachi/Resources/Mofumachi/'+$Relative)
        New-Item -ItemType Directory -Path $Folder -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $Folder '.owner'),$Run)
        if($Relative -eq 'CharacterAuthored'){
            foreach($Id in @('momo','lulu','pom','fio','miel')){New-Item -ItemType Directory -Path (Join-Path $Folder $Id) | Out-Null;[IO.File]::WriteAllText((Join-Path $Folder "$Id/atlas.png"),'synthetic image');[IO.File]::WriteAllText((Join-Path $Folder "$Id/layout.json"),'synthetic layout')}
        }else{[IO.File]::WriteAllText((Join-Path $Folder 'atlas.png'),'synthetic Noah');[IO.File]::WriteAllText((Join-Path $Folder 'layout.json'),'synthetic Noah layout')}
        [IO.File]::WriteAllText(($Folder+'.meta'),"fileFormatVersion: 2`nguid: 11111111111111111111111111111111`n")
    }
    $Packed=Get-PreparedCharactersDigest $Root
    @{gitRevision=('a'*40);productFingerprint=('b'*64);full_qa_verified=$true;single_native_verified=$true;six_native_verified=$true;builds_generated=$true;source_unchanged=$true;error=''} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run 'source-info.json') -Encoding UTF8
    foreach($Kind in @('baseline','authored')){
        $Apk=Join-Path $Run "characters-$Kind.apk";[IO.File]::WriteAllText($Apk,'synthetic '+$Kind)
        @{mode=$Kind;package='com.mofumachi.merge.qa.townlife';apk="characters-$Kind.apk";gitRevision=('a'*40);productFingerprint=('b'*64);packed_resource_sha256=$Packed;sha256=(Get-FileHash -LiteralPath $Apk -Algorithm SHA256).Hash.ToLowerInvariant();full_qa_verified=$true;six_native_verified=$true;pixel_verified=$false;adopted=$false} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Run "characters-$Kind-build.json") -Encoding UTF8
    }
    return $Run
}
$Base=Join-Path ([IO.Path]::GetTempPath()) ('mofu-preserve-tests-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $Base | Out-Null
try{
    foreach($Case in @('foreign-owner','incomplete','changed-resource','tracked','unknown-file','orphan','linked','success','no-inputs')){
        $Root=Join-Path $Base $Case;$Run=Fixture $Root
        $Folder=Join-Path $Root 'Assets/Mofumachi/Resources/Mofumachi/CharacterAuthored'
        switch($Case){
            foreign-owner {[IO.File]::WriteAllText((Join-Path $Folder '.owner'),$Base)}
            incomplete {$File=Join-Path $Run 'source-info.json';$I=Get-Content -Raw -LiteralPath $File | ConvertFrom-Json;$I.full_qa_verified=$false;$I | ConvertTo-Json | Set-Content -LiteralPath $File -Encoding UTF8}
            changed-resource {[IO.File]::AppendAllText((Join-Path $Folder 'momo/atlas.png'),' changed')}
            tracked {& $Git -C $Root add -- 'Assets/Mofumachi/Resources/Mofumachi/CharacterAuthored/momo/atlas.png';if($LASTEXITCODE -ne 0){throw 'Tracking fixture failed.'}}
            unknown-file {[IO.File]::WriteAllText((Join-Path $Folder 'personal-note.txt'),'keep')}
            orphan {Remove-Item -LiteralPath (Join-Path $Folder '.owner') -Force}
            linked {New-Item -ItemType SymbolicLink -Path (Join-Path $Folder 'outside') -Target $Run | Out-Null}
            no-inputs {Remove-Item -LiteralPath $Folder,($Folder+'.meta'),(Join-Path $Root 'Assets/Mofumachi/Resources/Mofumachi/NoahAuthoredTrial'),(Join-Path $Root 'Assets/Mofumachi/Resources/Mofumachi/NoahAuthoredTrial.meta') -Recurse -Force}
        }
        if($Case -eq 'success'){
            $Expected=Get-PreparedCharactersDigest $Root
            $Backup=Move-PreviousCharacterInputs $Root $Git
            if(Test-Path -LiteralPath $Folder){throw 'Inputs were not moved.'}
            $Record=Get-Content -Raw -LiteralPath (Join-Path $Backup 'preserved-inputs.json') | ConvertFrom-Json
            if(-not $Record.completed -or $Record.owner -cne $Run -or $Record.packed_resource_sha256 -cne $Expected -or $Record.files.Count -ne 16){throw 'Preservation manifest incomplete.'}
            foreach($Row in $Record.files){if((Get-FileHash -LiteralPath (Join-Path $Backup $Row.path) -Algorithm SHA256).Hash -ine $Row.sha256){throw 'Backup bytes changed.'}}
            Read-CharactersPair $Run | Out-Null
        }elseif($Case -eq 'no-inputs'){
            if(Move-PreviousCharacterInputs $Root $Git){throw 'Empty inputs should be a no-op.'}
        }else{
            RejectPreserve {Move-PreviousCharacterInputs $Root $Git}
            if(-not(Test-Path -LiteralPath $Folder)){throw 'Rejected inputs were removed.'}
        }
    }
}finally{Remove-Item -LiteralPath $Base -Recurse -Force}
Write-Host 'Prepared-input preservation passed: owner, completed pair, packed hash, tracked/unknown/link refusal, exact-byte reversible backup. Synthetic inputs only.'
