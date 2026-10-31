[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'prepare-characters-pixel-preview.ps1')
function Get-StrictPreparedFiles([string]$Folder){
    $Pending=New-Object 'System.Collections.Generic.Stack[string]';$Pending.Push($Folder)
    while($Pending.Count){
        $Current=Get-Item -LiteralPath $Pending.Pop() -Force
        if($Current.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked prepared paths are preserved in place.'}
        foreach($Item in @(Get-ChildItem -LiteralPath $Current.FullName -Force)){
            if($Item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked prepared inputs are preserved in place.'}
            if($Item.PSIsContainer){$Pending.Push($Item.FullName)}else{$Item}
        }
    }
}
function Assert-PreparedPathUnlinked([string]$Repository,[string]$Path){
    $Root=[IO.Path]::GetFullPath($Repository).TrimEnd('/','\');$At=[IO.Path]::GetFullPath($Path)
    if(-not $At.StartsWith($Root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Prepared path outside repository.'}
    while($At.Length -gt $Root.Length){
        if(Test-Path -LiteralPath $At){if((Get-Item -LiteralPath $At -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked prepared ancestry is preserved in place.'}}
        $At=Split-Path -Parent $At
    }
}
function Get-PreparedCharactersDigest([string]$Repository){
    $Rows=New-Object 'System.Collections.Generic.List[string]'
    foreach($Name in @('CharacterAuthored','NoahAuthoredTrial')){
        $Folder=Join-Path $Repository ('Assets/Mofumachi/Resources/Mofumachi/'+$Name)
        foreach($File in @(Get-StrictPreparedFiles $Folder)){
            if($File.Name -ceq '.owner'){continue}
            $Relative=$File.FullName.Substring($Repository.TrimEnd('/','\').Length+1).Replace('\','/')
            $Rows.Add($Relative+':'+(Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash.ToLowerInvariant())
        }
    }
    $Rows.Sort([StringComparer]::Ordinal)
    $Hasher=[Security.Cryptography.SHA256]::Create()
    try{return ([BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes(($Rows -join "`n"))))).Replace('-','').ToLowerInvariant()}finally{$Hasher.Dispose()}
}
function Move-PreviousCharacterInputs([string]$Repository,[string]$Git){
    $Repository=(Resolve-Path -LiteralPath $Repository).Path.TrimEnd('/','\')
    $Names=@('CharacterAuthored','NoahAuthoredTrial');$Prefix='Assets/Mofumachi/Resources/Mofumachi/'
    $Present=@($Names | Where-Object {(Test-Path -LiteralPath (Join-Path $Repository ($Prefix+$_))) -or (Test-Path -LiteralPath (Join-Path $Repository ($Prefix+$_+'.meta')))})
    if(-not $Present.Count){return}
    if($Present.Count -ne 2){throw 'Incomplete prepared folders; preserved in place.'}
    if(Get-Process -Name Unity -ErrorAction SilentlyContinue){throw 'Close Unity before preserving prepared inputs.'}
    $Owner=$null;$MovePaths=New-Object 'System.Collections.Generic.List[string]';$Files=New-Object 'System.Collections.Generic.List[object]'
    foreach($Name in $Names){
        $Folder=Join-Path $Repository ($Prefix+$Name);Assert-PreparedPathUnlinked $Repository $Folder
        $Marker=Join-Path $Folder '.owner';Assert-PreparedPathUnlinked $Repository $Marker
        if(-not(Test-Path -LiteralPath $Marker -PathType Leaf)){throw 'Prepared owner missing; preserved in place.'}
        $ThisOwner=[IO.File]::ReadAllText($Marker)
        if(-not [IO.Path]::IsPathRooted($ThisOwner) -or $ThisOwner -cne [IO.Path]::GetFullPath($ThisOwner)){throw 'Noncanonical prepared owner.'}
        $Diagnostics=Join-Path $Repository 'Builds/Diagnostics'
        if(-not $ThisOwner.StartsWith($Diagnostics+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or (Split-Path -Leaf $ThisOwner) -notlike 'characters-authored-*'){throw 'Unexpected prepared owner; preserved in place.'}
        if($Owner -and $Owner -cne $ThisOwner){throw 'Prepared owners differ; preserved in place.'};$Owner=$ThisOwner
        foreach($File in @(Get-StrictPreparedFiles $Folder)){
            $Sub=$File.FullName.Substring($Folder.Length+1).Replace('\','/')
            $Allowed=if($Name -eq 'CharacterAuthored'){$Sub -cmatch '^(\.owner|(?:momo|lulu|pom|fio|miel)\.meta|(?:momo|lulu|pom|fio|miel)/(?:atlas\.png|layout\.json)(?:\.meta)?)$'}else{$Sub -cmatch '^(\.owner|(?:atlas\.png|layout\.json)(?:\.meta)?)$'}
            if(-not $Allowed){throw "Unknown prepared file preserved in place: $Sub"};$Files.Add($File)
        }
        $MovePaths.Add($Prefix+$Name)
        if(Test-Path -LiteralPath ($Folder+'.meta')){Assert-PreparedPathUnlinked $Repository ($Folder+'.meta');$Files.Add((Get-Item -LiteralPath ($Folder+'.meta') -Force));$MovePaths.Add($Prefix+$Name+'.meta')}
    }
    foreach($Name in @('source-info.json','characters-baseline-build.json','characters-authored-build.json','characters-baseline.apk','characters-authored.apk')){Assert-PreparedPathUnlinked $Repository (Join-Path $Owner $Name)}
    $Pair=Read-CharactersPair $Owner
    $Packed=Get-PreparedCharactersDigest $Repository
    if($Packed -cne $Pair.authored.packed_resource_sha256){throw 'Prepared bytes do not match the completed build; preserved in place.'}
    $Stamp=$Prefix+'QaBuildRevision.txt'
    if(Test-Path -LiteralPath (Join-Path $Repository $Stamp)){
        Assert-PreparedPathUnlinked $Repository (Join-Path $Repository $Stamp)
        if([IO.File]::ReadAllText((Join-Path $Repository $Stamp)).Trim() -cne $Pair.authored.gitRevision){throw 'Unexpected revision stamp; preserved in place.'}
        $Files.Add((Get-Item -LiteralPath (Join-Path $Repository $Stamp) -Force));$MovePaths.Add($Stamp)
    }
    if(Test-Path -LiteralPath (Join-Path $Repository ($Stamp+'.meta'))){Assert-PreparedPathUnlinked $Repository (Join-Path $Repository ($Stamp+'.meta'));$Files.Add((Get-Item -LiteralPath (Join-Path $Repository ($Stamp+'.meta')) -Force));$MovePaths.Add($Stamp+'.meta')}
    foreach($Relative in $MovePaths){
        $Tracked=@(& $Git -C $Repository ls-files -- $Relative)
        if($LASTEXITCODE -ne 0 -or $Tracked.Count){throw 'Tracked prepared inputs are preserved in place.'}
    }
    $Backup=Join-Path $Repository ('Builds/Diagnostics/characters-preserved-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
    Assert-PreparedPathUnlinked $Repository $Backup
    $Record=[ordered]@{owner=$Owner;packed_resource_sha256=$Packed;completed=$false;error='';moves=@();files=@(foreach($File in $Files){@{path=$File.FullName.Substring($Repository.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})}
    New-Item -ItemType Directory -Path $Backup | Out-Null
    $Manifest=Join-Path $Backup 'preserved-inputs.json';$Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Manifest -Encoding UTF8
    try{
        foreach($Relative in $MovePaths){
            $Target=Join-Path $Backup $Relative;New-Item -ItemType Directory -Path (Split-Path -Parent $Target) -Force | Out-Null
            Move-Item -LiteralPath (Join-Path $Repository $Relative) -Destination $Target
            $Record.moves+=@($Relative);$Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Manifest -Encoding UTF8
        }
        foreach($File in $Record.files){if((Get-FileHash -LiteralPath (Join-Path $Backup $File.path) -Algorithm SHA256).Hash -ine $File.sha256){throw 'Preserved-byte verification failed; stop and retain both locations.'}}
        $Record.completed=$true
    }catch{$Record.error=$_.Exception.Message;throw}
    finally{$Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Manifest -Encoding UTF8;Write-Host "Previous inputs preserved at: $Backup"}
    return $Backup
}
if($MyInvocation.InvocationName -ne '.'){throw 'Use the final-pass wrapper; this file only supplies the preservation functions.'}
