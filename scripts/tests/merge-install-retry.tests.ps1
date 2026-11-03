$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../install-pixel3a.ps1')
$Root=Join-Path ([IO.Path]::GetTempPath()) ('mofu-install-retry-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $Root|Out-Null
try{
    $Apk=Join-Path $Root 'same checked.apk';$AdbTool=Join-Path $Root 'adb.exe';$AaptTool=Join-Path $Root 'aapt2.exe'
    foreach($File in @($Apk,$AdbTool,$AaptTool)){[IO.File]::WriteAllText($File,'synthetic fixture, not executable')}
    $Hash=(Get-FileHash $Apk -Algorithm SHA256).Hash.ToLowerInvariant();$script:Calls=@();$script:FailInstall=$false
    function Invoke-CheckedNative([string]$Executable,[string[]]$NativeArguments){
        $script:Calls+=,@($NativeArguments)
        if($NativeArguments -contains 'uninstall' -or $NativeArguments -contains 'clear'){throw 'Save-preserving retry must never delete/reset data.'}
        if($NativeArguments -contains 'badging'){return "package: name='com.mofumachi.merge.qa.townlife' versionCode='1'`nnative-code: 'arm64-v8a'"}
        if($NativeArguments -contains 'devices'){return "List of devices attached`n99RAY1BELH`tdevice"}
        if($NativeArguments -contains 'ro.build.version.sdk'){return '30'}
        if($NativeArguments -contains 'ro.product.cpu.abilist'){return 'arm64-v8a'}
        if($NativeArguments -contains 'install'){if($script:FailInstall){throw 'synthetic transport failure'};return 'Success'}
        if($NativeArguments -contains 'monkey'){return 'Events injected:1'}
        throw ('Unexpected native call: '+($NativeArguments-join ' '))
    }
    Invoke-PixelInstall $Apk $AdbTool '99RAY1BELH' 'com.mofumachi.merge.qa.townlife' $AaptTool -ExpectedSha256 $Hash -NonStreaming 6>$null
    $Install=@($script:Calls|Where-Object {$_ -contains 'install'})
    if($Install.Count -ne 1 -or $Install[0] -notcontains '-r' -or $Install[0] -notcontains '--no-streaming' -or $Install[0][-1] -cne $Apk){throw 'Retry must use the exact same checked APK with install-r and no streaming.'}
    $script:Calls=@();$script:FailInstall=$true;$Rejected=$false
    try{Invoke-PixelInstall $Apk $AdbTool '99RAY1BELH' 'com.mofumachi.merge.qa.townlife' $AaptTool -ExpectedSha256 $Hash -NonStreaming 6>$null}catch{$Rejected=$true}
    if(-not $Rejected -or @($script:Calls|Where-Object {$_ -contains 'install'}).Count -ne 1 -or @($script:Calls|Where-Object {$_ -contains 'monkey'}).Count){throw 'Failed install must stop after one attempt, before launch.'}
    $script:Calls=@();$Rejected=$false
    try{Invoke-PixelInstall $Apk $AdbTool '99RAY1BELH' 'com.mofumachi.merge.qa.townlife' $AaptTool -ExpectedSha256 ('0'*64) -NonStreaming 6>$null}catch{$Rejected=$true}
    if(-not $Rejected -or $script:Calls.Count){throw 'Wrong APK SHA must block all device commands.'}
    . (Join-Path $PSScriptRoot '../test-merge-reward-pixel.ps1')
    $Record=@{crash_log_captured=$false};$Crash=Join-Path $Root 'crash.txt';$Rejected=$false
    try{Write-MergeCrashEvidence $Record '- waiting for device -' $Crash}catch{$Rejected=$true}
    if(-not $Rejected -or $Record.crash_log_captured){throw 'Device waiting is not a captured crash buffer.'}
    Write-MergeCrashEvidence $Record '' $Crash
    if(-not $Record.crash_log_captured){throw 'An empty readable crash buffer is valid evidence.'}
}finally{Remove-Item -LiteralPath $Root -Recurse -Force}
Write-Host 'Same-APK non-streaming install-r, one attempt only, SHA guard and disconnected-crash rejection passed. Synthetic native transport only; no phone accessed.'
