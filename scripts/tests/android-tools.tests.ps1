$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../build-android.ps1')
. (Join-Path $PSScriptRoot '../install-pixel3a.ps1')
function Expect-Rejection([scriptblock]$Action){$Rejected=$false;try{& $Action}catch{$Rejected=$true};if(-not $Rejected){throw 'Expected rejection did not occur.'}}
$Temporary=Join-Path ([IO.Path]::GetTempPath()) ('mofumachi-script-test-'+[guid]::NewGuid())
New-Item -ItemType Directory -Path $Temporary | Out-Null
try {
    # Exercise the real wrapper before the mocked installer commands below.
    # Windows PowerShell 5.1 turns redirected native stderr into ErrorRecords;
    # newer pwsh does not, so reproduce that legacy stream policy explicitly.
    function Legacy-NativeDiagnostic {
        $global:LASTEXITCODE=0
        Write-Error 'args: [-p, com.mofumachi.merge.qa, -c, android.intent.category.LAUNCHER, 1]' -ErrorId NativeCommandError
        'Events injected: 1'
    }
    $LegacyOutput=Invoke-CheckedNative 'Legacy-NativeDiagnostic' @()
    if($LegacyOutput -notmatch 'args:' -or $LegacyOutput -notmatch 'Events injected: 1'){throw 'Successful stderr diagnostic was lost.'}
    if($ErrorActionPreference -ne 'Stop'){throw 'Native invocation changed the caller error policy.'}
    $NativeScript=Join-Path $Temporary 'native diagnostic.ps1'
    @'
param([int]$Code=0)
[Console]::Error.WriteLine('args: [-p, com.mofumachi.merge.qa, -c, android.intent.category.LAUNCHER, 1]')
[Console]::Out.WriteLine('Events injected: 1')
exit $Code
'@ | Set-Content -LiteralPath $NativeScript -Encoding UTF8
    $ShellExecutable=(Get-Process -Id $PID).Path
    $NativeOutput=Invoke-CheckedNative $ShellExecutable @('-NoProfile','-NonInteractive','-File',$NativeScript,'-Code','0')
    if($NativeOutput -notmatch 'args:' -or $NativeOutput -notmatch 'Events injected: 1'){throw 'Native stdout/stderr capture failed.'}
    Expect-Rejection {Invoke-CheckedNative (Join-Path $Temporary 'missing adb.exe') @()}
    $ExitRejected=$false
    try {Invoke-CheckedNative $ShellExecutable @('-NoProfile','-NonInteractive','-File',$NativeScript,'-Code','7') | Out-Null}
    catch {$ExitRejected=$_.Exception.Message -match 'Command failed \(7\)' -and $_.Exception.Message -match 'args:'}
    if(-not $ExitRejected){throw 'A real nonzero native exit was not reported with its diagnostic.'}
    if($ErrorActionPreference -ne 'Stop'){throw 'Failed native invocation changed the caller error policy.'}
    Write-Host 'Native stdout/stderr and exit-code checks passed (legacy stderr policy simulated).'
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
    # ChapterBuildGateRejectsMissingSuites: read the actual production call lists.
    $BuildSource=Get-Content (Join-Path $PSScriptRoot '../build-android.ps1') -Raw
    function Read-GateSuites([string]$XmlVariable,[int]$Minimum) {
        $Pattern='Assert-TestRun \$'+$XmlVariable+' '+$Minimum+' @\(([^)]+)\)'
        $Match=[regex]::Match($BuildSource,$Pattern)
        if(-not $Match.Success){throw 'Actual gate call was not found.'}
        return @([regex]::Matches($Match.Groups[1].Value,"'([^']+)'") | ForEach-Object {$_.Groups[1].Value})
    }
    function Write-SuiteXml([int]$Total,[string[]]$Suites,[string]$Case='Passed',[int]$Declared=0) {
        if($Declared -eq 0){$Declared=$Total}
        $Nodes=($Suites | ForEach-Object {"<test-suite name='$_' result='Passed'><test-case result='Passed'/></test-suite>"}) -join ''
        $Extra=('<test-case result="Passed"/>'*($Total-$Suites.Count-1))+"<test-case result='$Case'/>"
        "<test-run result='Passed' total='$Declared' passed='$Declared' failed='0' skipped='0'>$Nodes$Extra</test-run>" | Set-Content $Xml
    }
    $EditSuites=Read-GateSuites 'EditXml' 90;$PlaySuites=Read-GateSuites 'PlayXml' 60
    foreach($Gate in @(@{Minimum=90;Suites=$EditSuites;New=@('ChapterProgressTests','TownFrameClockTests')},@{Minimum=60;Suites=$PlaySuites;New=@('StoryTypographyTests','TownMotionPresentationTests','ChapterFlowTests','MergePolishTests')})) {
        $Old=@($Gate.Suites | Where-Object {$_ -notin $Gate.New})
        Write-SuiteXml $Gate.Minimum $Old
        Expect-Rejection {Assert-TestRun $Xml $Gate.Minimum $Gate.Suites}
        foreach($Missing in $Gate.New) {
            if($Missing -notin $Gate.Suites){throw "Missing chapter suite in production gate: $Missing"}
            Write-SuiteXml $Gate.Minimum @($Gate.Suites | Where-Object {$_ -ne $Missing})
            Expect-Rejection {Assert-TestRun $Xml $Gate.Minimum $Gate.Suites}
        }
        Write-SuiteXml $Gate.Minimum $Old
        $EmptySuites=($Gate.New | ForEach-Object {"<test-suite name='$_' result='Passed'/>"}) -join ''
        (Get-Content $Xml -Raw).Replace('</test-run>',"$EmptySuites</test-run>") | Set-Content $Xml
        Expect-Rejection {Assert-TestRun $Xml $Gate.Minimum $Gate.Suites}
        Write-SuiteXml $Gate.Minimum $Gate.Suites;Assert-TestRun $Xml $Gate.Minimum $Gate.Suites
        foreach($BadCase in @('Skipped','Failed','Inconclusive')){Write-SuiteXml $Gate.Minimum $Gate.Suites $BadCase;Expect-Rejection {Assert-TestRun $Xml $Gate.Minimum $Gate.Suites}}
        Write-SuiteXml $Gate.Minimum $Gate.Suites -Declared ($Gate.Minimum+1);Expect-Rejection {Assert-TestRun $Xml $Gate.Minimum $Gate.Suites}
    }
    Write-Host 'Chapter build gates passed (synthetic result XML, Unity tests not executed).'
    Expect-Rejection {Select-AuthorizedDevice 'List of devices attached' ''}
    Expect-Rejection {Select-AuthorizedDevice "Pixel`tunauthorized" ''}
    Expect-Rejection {Select-AuthorizedDevice "Pixel`tdevice`nOther`tdevice" ''}
    if((Select-AuthorizedDevice "Pixel`tdevice`nOther`tdevice" 'Pixel') -ne 'Pixel'){throw 'Wrong serial'}
    $Apk=Join-Path $Temporary 'test app.apk';$Adb=Join-Path $Temporary 'adb.exe';$Aapt=Join-Path $Temporary 'aapt2.exe'
    foreach($Path in @($Apk,$Adb,$Aapt)){Set-Content -LiteralPath $Path 'fixture'}
    $script:Calls=@();$script:Wrong=$false;$script:FailInstall=$false;$script:FixtureId='com.mofumachi.merge'
    function Invoke-CheckedNative([string]$Executable,[string[]]$NativeArguments){
        $script:Calls+=,@($NativeArguments)
        if($Executable -eq $Aapt){$Id=$script:FixtureId;if($script:Wrong){$Id='wrong.app'};return "package: name='$Id' versionCode='1'`nnative-code: 'arm64-v8a'"}
        if($NativeArguments[0] -eq 'devices'){return "List of devices attached`nPixel`tdevice"}
        if($NativeArguments[0] -ne '-s' -or $NativeArguments[1] -ne 'Pixel'){throw 'ADB device command must specify exact serial.'}
        if($NativeArguments -contains 'ro.build.version.sdk'){return '31'}
        if($NativeArguments -contains 'ro.product.cpu.abilist'){return 'arm64-v8a,armeabi-v7a'}
        if($NativeArguments -contains 'install'){if($script:FailInstall){return 'Failure [INSTALL_FAILED_UPDATE_INCOMPATIBLE]'};if($NativeArguments[-1] -ne $Apk){throw 'Space in APK path lost'};return 'Success'}
        return 'Events injected: 1'
    }
    Invoke-PixelInstall $Apk $Adb '' 'com.mofumachi.merge' $Aapt
    foreach($Id in @('com.mofumachi.merge.qa','com.mofumachi.merge.qa.townlife')){
        $script:FixtureId=$Id;Invoke-PixelInstall $Apk $Adb 'Pixel' $Id $Aapt
    }
    $script:FixtureId='com.mofumachi.merge.qa.townlife'
    Expect-Rejection {Invoke-PixelInstall $Apk $Adb 'Pixel' 'com.mofumachi.merge.qa' $Aapt}
    Expect-Rejection {Invoke-PixelInstall $Apk $Adb 'Pixel' 'com.mofumachi.merge.qa.unknown' $Aapt}
    $Validation=(Get-Command (Join-Path $PSScriptRoot '../install-pixel3a.ps1')).Parameters['PackageName'].Attributes | Where-Object {$_ -is [System.Management.Automation.ValidateSetAttribute]}
    if($Validation.ValidValues.Count -ne 3 -or $Validation.ValidValues -notcontains 'com.mofumachi.merge.qa.townlife'){throw 'Installer must allow exactly 3 package IDs.'}
    $script:FixtureId='com.mofumachi.merge'
    if(@($script:Calls | Where-Object {$_ -contains 'install' -and $_ -notcontains '-r'}).Count -gt 0){throw 'Update must retain save using install -r'}
    $script:Wrong=$true;Expect-Rejection {Invoke-PixelInstall $Apk $Adb '' 'com.mofumachi.merge' $Aapt};$script:Wrong=$false
    $script:FailInstall=$true;Expect-Rejection {Invoke-PixelInstall $Apk $Adb '' 'com.mofumachi.merge' $Aapt}
    if(@($script:Calls | Where-Object {$_ -contains 'uninstall' -or $_ -contains 'clear'}).Count -gt 0){throw 'Destructive data command'}
    Write-Host 'Android script gates passed (mock commands, no device tested).'
} finally {Remove-Item -LiteralPath $Temporary -Recurse -Force}
