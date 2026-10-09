$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../build-android.ps1')
. (Join-Path $PSScriptRoot '../install-pixel3a.ps1')
function Expect-Rejection([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Expected rejection did not occur.'}}
$Temporary=Join-Path ([IO.Path]::GetTempPath()) ('mofumachi-script-test-'+[guid]::NewGuid())
New-Item -ItemType Directory -Path $Temporary | Out-Null
try {
    $Xml=Join-Path $Temporary 'results.xml'
    Expect-Rejection {Assert-TestRun $Xml 2 @('Required')}
    function Write-Xml([string]$Result='Passed',[int]$Total=2,[int]$Passed=2,[int]$Failed=0,[int]$Skipped=0,[string]$Suite='Required',[string]$Case='Passed'){
        "<test-run result='$Result' total='$Total' passed='$Passed' failed='$Failed' skipped='$Skipped'><test-suite name='$Suite' result='Passed'><test-case result='Passed'/><test-case result='$Case'/></test-suite></test-run>" | Set-Content $Xml
    }
    Write-Xml;Assert-TestRun $Xml 2 @('Required')
    Write-Xml -Result Failed -Passed 1 -Failed 1;Expect-Rejection {Assert-TestRun $Xml 2 @('Required')}
    Write-Xml -Passed 1 -Skipped 1;Expect-Rejection {Assert-TestRun $Xml 2 @('Required')}
    Write-Xml -Total 3 -Passed 3;Expect-Rejection {Assert-TestRun $Xml 2 @('Required')}
    Write-Xml -Suite Other;Expect-Rejection {Assert-TestRun $Xml 2 @('Required')}
    Write-Xml -Case Inconclusive;Expect-Rejection {Assert-TestRun $Xml 2 @('Required')}
    Write-Xml;Expect-Rejection {Assert-TestRun $Xml 67 @('Required')}
    Expect-Rejection {Select-AuthorizedDevice 'List of devices attached' ''}
    Expect-Rejection {Select-AuthorizedDevice "Pixel`tunauthorized" ''}
    Expect-Rejection {Select-AuthorizedDevice "Pixel`tdevice`nOther`tdevice" ''}
    if((Select-AuthorizedDevice "Pixel`tdevice`nOther`tdevice" 'Pixel') -ne 'Pixel'){throw 'Wrong serial'}
    $Apk=Join-Path $Temporary 'test app.apk';$Adb=Join-Path $Temporary 'adb.exe';$Aapt=Join-Path $Temporary 'aapt2.exe'
    foreach($Path in @($Apk,$Adb,$Aapt)){Set-Content -LiteralPath $Path 'fixture'}
    $script:Calls=@();$script:Wrong=$false;$script:FailInstall=$false
    function Invoke-CheckedNative([string]$Executable,[string[]]$NativeArguments){
        $script:Calls+=,@($NativeArguments)
        if($Executable -eq $Aapt){$Id='com.mofumachi.merge';if($script:Wrong){$Id='wrong.app'};return "package: name='$Id' versionCode='1'`nnative-code: 'arm64-v8a'"}
        if($NativeArguments[0] -eq 'devices'){return "List of devices attached`nPixel`tdevice"}
        if($NativeArguments[0] -ne '-s' -or $NativeArguments[1] -ne 'Pixel'){throw 'ADB device command must specify exact serial.'}
        if($NativeArguments -contains 'ro.build.version.sdk'){return '31'}
        if($NativeArguments -contains 'ro.product.cpu.abilist'){return 'arm64-v8a,armeabi-v7a'}
        if($NativeArguments -contains 'install'){if($script:FailInstall){return 'Failure [INSTALL_FAILED_UPDATE_INCOMPATIBLE]'};if($NativeArguments[-1] -ne $Apk){throw 'Space in APK path lost'};return 'Success'}
        return 'Events injected: 1'
    }
    Invoke-PixelInstall $Apk $Adb '' 'com.mofumachi.merge' $Aapt
    $script:Wrong=$true;Expect-Rejection {Invoke-PixelInstall $Apk $Adb '' 'com.mofumachi.merge' $Aapt};$script:Wrong=$false
    $script:FailInstall=$true;Expect-Rejection {Invoke-PixelInstall $Apk $Adb '' 'com.mofumachi.merge' $Aapt}
    if(@($script:Calls | Where-Object {$_ -contains 'uninstall' -or $_ -contains 'clear'}).Count -gt 0){throw 'Destructive data command'}
    Write-Host 'Android script gates passed (mock commands, no device tested).'
} finally {Remove-Item -LiteralPath $Temporary -Recurse -Force}
