function Assert-MergeFontArtifacts([string]$Directory,[string]$Revision,[string]$Fingerprint,[string]$Packed){
    $Folder=[IO.Path]::GetFullPath((Join-Path $Directory 'screenshots/font-comparison'))
    $Json=Join-Path $Folder 'merge-font-native.json';$Html=Join-Path $Folder 'merge-font.html'
    foreach($File in @($Json,$Html)){if(-not(Test-Path -LiteralPath $File -PathType Leaf) -or (Get-Item -LiteralPath $File).Length -eq 0 -or ((Get-Item -LiteralPath $File -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Font comparison JSON/HTML missing, empty or linked.'}}
    $P=Get-Content -LiteralPath $Json -Raw -Encoding UTF8|ConvertFrom-Json
    if($P.kind -cne 'merge-font-abc-native' -or $P.source_revision -cne $Revision -or $P.product_fingerprint -cne $Fingerprint -or $P.packed_resource_sha256 -cne $Packed -or $P.native_unity -isnot [bool] -or -not $P.native_unity -or $P.disk_verified -isnot [bool] -or -not $P.disk_verified -or $P.image_count -ne 6){throw 'Font comparison identity/completeness differs.'}
    foreach($Flag in @('formal_fixed','adopted','pixel_verified')){if($P.$Flag -isnot [bool] -or $P.$Flag){throw 'Font comparison cannot claim adoption or Pixel acceptance.'}}
    if(-not [IO.Path]::IsPathRooted($P.output_directory) -or -not [string]::Equals([IO.Path]::GetFullPath($P.output_directory).TrimEnd('/','\'),$Folder.TrimEnd('/','\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Font comparison output directory differs.'}
    $Expected=@(foreach($Size in @('720x1480','640x1072')){foreach($Variant in @('A','B','C')){$Variant+'-'+$Size+'.jpg'}})
    $Actual=@(Get-ChildItem -LiteralPath $Folder -File -Filter '*.jpg')
    if(@($P.images).Count -ne 6 -or @($P.images.file|Select-Object -Unique).Count -ne 6 -or @(Compare-Object $Expected @($P.images.file)).Count -or $Actual.Count -ne 6 -or @(Compare-Object $Expected @($Actual.Name)).Count){throw 'Six exact A/B/C JPEGs required.'}
    $Text=Get-Content -LiteralPath $Html -Raw -Encoding UTF8
    if($Text -notmatch '(?i)<!doctype html>'){throw 'Font comparison HTML invalid.'}
    foreach($Row in @($P.images)){
        $File=Join-Path $Folder $Row.file
        if($Row.file -cne ($Row.variant+'-'+$Row.width+'x'+$Row.height+'.jpg') -or $Row.variant -cnotin @('A','B','C') -or ($Row.width -eq 720 -and $Row.height -ne 1480) -or ($Row.width -eq 640 -and $Row.height -ne 1072) -or $Row.width -notin @(640,720)){throw 'Font comparison dimensions/variant differ.'}
        if($Row.sha256 -cnotmatch '^[a-f0-9]{64}$' -or (Get-Item -LiteralPath $File).Length -eq 0 -or ((Get-Item -LiteralPath $File -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash -ine $Row.sha256 -or -not $Text.Contains($Row.file)){throw 'Font comparison file/hash/viewer link differs.'}
        if($Row.state_sha256 -cnotmatch '^[a-f0-9]{64}$' -or $Row.layout_sha256 -cnotmatch '^[a-f0-9]{64}$' -or $Row.font -cne $(if($Row.variant -ceq 'C'){'UIFontStory'}else{'UIFontAccent'}) -or $Row.bold -isnot [bool] -or $Row.bold -ne ($Row.variant -cne 'C') -or [math]::Abs([double]$Row.outline-$(if($Row.variant -ceq 'B'){.35}else{0})) -gt .001){throw 'Font comparison roles/state/layout proof differs.'}
    }
    if(@($P.images.state_sha256|Select-Object -Unique).Count -ne 1){throw 'A/B/C save states differ.'}
    foreach($Width in @(720,640)){if(@($P.images|Where-Object {$_.width -eq $Width}|Select-Object -ExpandProperty layout_sha256 -Unique).Count -ne 1){throw 'A/B/C layouts differ.'}}
    return @(foreach($Name in $Expected+@('merge-font-native.json','merge-font.html')){$File=Join-Path $Folder $Name;@{path=('screenshots/font-comparison/'+$Name);sha256=(Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant();bytes=(Get-Item -LiteralPath $File).Length}})
}
