function Assert-FontOnlyArtifacts([string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    $Folder=[IO.Path]::GetFullPath((Join-Path $Directory 'screenshots'))
    $Json=Join-Path $Folder 'font-only-native.json';$Html=Join-Path $Folder 'font-only.html'
    foreach($File in @($Json,$Html)){if(-not(Test-Path -LiteralPath $File -PathType Leaf) -or (Get-Item -LiteralPath $File).Length -eq 0 -or ((Get-Item -LiteralPath $File -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Font-only proof/viewer missing, empty or linked.'}}
    $P=Get-Content -LiteralPath $Json -Raw -Encoding UTF8|ConvertFrom-Json
    if($P.kind -cne 'hachi-font-only-native' -or $P.source_revision -cne $Revision -or $P.product_fingerprint -cne $Fingerprint -or $P.packed_resource_sha256 -cne $Packed -or $P.native_unity -isnot [bool] -or -not $P.native_unity -or $P.disk_verified -isnot [bool] -or -not $P.disk_verified -or $P.image_count -ne 10){throw 'Font-only native identity/completeness differs.'}
    foreach($Flag in @('visual_approved','pixel_verified')){if($P.$Flag -isnot [bool] -or $P.$Flag){throw 'Font-only capture cannot claim visual/Pixel acceptance.'}}
    if(-not [IO.Path]::IsPathRooted($P.output_directory) -or -not [string]::Equals([IO.Path]::GetFullPath($P.output_directory).TrimEnd('/','\'),$Folder.TrimEnd('/','\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Font-only output differs.'}
    $Expected=@(foreach($Size in @('720x1480','640x1072')){foreach($Screen in @('town','conversation','merge','quest','book')){$Screen+'-'+$Size+'.png'}})
    $Actual=@(Get-ChildItem -LiteralPath $Folder -File -Filter '*.png');$Text=Get-Content -LiteralPath $Html -Raw -Encoding UTF8
    if(@($P.images).Count -ne 10 -or @($P.images.file|Select-Object -Unique).Count -ne 10 -or @(Compare-Object $Expected @($P.images.file)).Count -or $Actual.Count -ne 10 -or @(Compare-Object $Expected @($Actual.Name)).Count -or $Text -notmatch '(?i)<!doctype html>'){throw 'Ten exact font-only PNGs and HTML required.'}
    foreach($Row in @($P.images)){
        $File=Join-Path $Folder $Row.file
        if($Row.file -cne ($Row.screen+'-'+$Row.width+'x'+$Row.height+'.png') -or $Row.screen -cnotin @('town','conversation','merge','quest','book') -or ($Row.width -eq 720 -and $Row.height -ne 1480) -or ($Row.width -eq 640 -and $Row.height -ne 1072) -or $Row.width -notin @(640,720)){throw 'Font-only screen/dimensions differ.'}
        if($Row.sha256 -cnotmatch '^[a-f0-9]{64}$' -or (Get-Item -LiteralPath $File).Length -eq 0 -or ((Get-Item -LiteralPath $File -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash -ine $Row.sha256 -or -not $Text.Contains($Row.file) -or $Row.state_sha256 -cnotmatch '^[a-f0-9]{64}$' -or $Row.geometry_sha256 -cnotmatch '^[a-f0-9]{64}$'){throw 'Font-only image/hash/proof/viewer differs.'}
        $Bytes=[IO.File]::ReadAllBytes($File)
        if($Bytes.Length -lt 45 -or [BitConverter]::ToString($Bytes[0..7]) -cne '89-50-4E-47-0D-0A-1A-0A' -or [Text.Encoding]::ASCII.GetString($Bytes,12,4) -cne 'IHDR'){throw 'Font-only file is not PNG.'}
        $Width=([int]$Bytes[16]*16777216)+([int]$Bytes[17]*65536)+([int]$Bytes[18]*256)+$Bytes[19];$Height=([int]$Bytes[20]*16777216)+([int]$Bytes[21]*65536)+([int]$Bytes[22]*256)+$Bytes[23]
        if($Width -ne $Row.width -or $Height -ne $Row.height){throw 'Actual PNG dimensions differ.'}
        if(@($Row.fonts).Count -eq 0){throw 'Actual font selection proof required.'}
        foreach($Font in @($Row.fonts)){
            if($Font.resource -cnotin @('UIFontAccent','UIFontReading','UIFontReadingBold') -or $Font.hachi_fits -isnot [bool] -or $Font.size -lt 8 -or ($Font.resource -ceq 'UIFontAccent' -and (-not $Font.hachi_fits -or $Font.style -cne 'Bold' -or $Font.reason -cne 'Hachi primary')) -or ($Font.resource -cne 'UIFontAccent' -and ($Font.hachi_fits -or $Font.style -cne 'Normal' -or $Font.reason -cne 'Hachi measured overflow; Zen fallback'))){throw 'Hachi-first/fallback proof differs.'}
        }
    }
    return @(foreach($Name in $Expected+@('font-only-native.json','font-only.html')){$File=Join-Path $Folder $Name;@{path=('screenshots/'+$Name);sha256=(Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant();bytes=(Get-Item -LiteralPath $File).Length}})
}
function Confirm-FontOnlyCapture($Record,[string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    $Record.capture_verified=$false;$Record.image_count=0;$Record.artifacts=@()
    if($Record.source_unchanged -isnot [bool] -or -not $Record.source_unchanged -or $Record.error){throw 'Font-only source/cleanup failed.'}
    $Record.artifacts=@(Assert-FontOnlyArtifacts $Directory $Revision $Fingerprint $Packed);$Record.image_count=10;$Record.capture_verified=$true
}
function Write-FontOnlyReviewPackage($Record,[string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    Confirm-FontOnlyCapture $Record $Directory $Revision $Fingerprint $Packed
    $Info=Join-Path $Directory 'font-only-source-info.json';$Record|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $Info -Encoding UTF8
    $Files=@($Record.artifacts|ForEach-Object {Join-Path $Directory $_.path})+@($Info)
    $Expected=@{};foreach($File in $Files){$Expected[[IO.Path]::GetFileName($File)]=(Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant()}
    $Zip=Join-Path $Directory 'font-only-review.zip';Compress-Archive -LiteralPath $Files -DestinationPath $Zip
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $Archive=[IO.Compression.ZipFile]::OpenRead($Zip)
    try{
        if($Archive.Entries.Count -ne 13){throw 'Thirteen review package entries required.'}
        foreach($Entry in $Archive.Entries){
            if(-not $Expected.ContainsKey($Entry.FullName)){throw 'Unexpected review package entry.'}
            $Stream=$Entry.Open();$Hasher=[Security.Cryptography.SHA256]::Create()
            try{$Hash=([BitConverter]::ToString($Hasher.ComputeHash($Stream))).Replace('-','').ToLowerInvariant()}finally{$Stream.Dispose();$Hasher.Dispose()}
            if($Hash -cne $Expected[$Entry.FullName]){throw 'Review ZIP entry differs from actual evidence.'}
        }
    }finally{$Archive.Dispose()}
    $null=Assert-FontOnlyArtifacts $Directory $Revision $Fingerprint $Packed
    foreach($File in $Files){if((Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash -ine $Expected[[IO.Path]::GetFileName($File)]){throw 'Evidence changed after packaging.'}}
    return $Zip
}
