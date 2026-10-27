[CmdletBinding()]
param([string]$Apk,[string]$Adb="$env:ProgramFiles\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe",[string]$Serial,[ValidateSet('com.mofumachi.merge','com.mofumachi.merge.qa','com.mofumachi.merge.qa.townlife','com.mofumachi.merge.qa.growthcheck')][string]$PackageName='com.mofumachi.merge',[switch]$FreshTownQa,[string]$ExpectedSha256)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Invoke-CheckedNative([string]$Executable,[string[]]$NativeArguments) {
    $PreviousErrorPolicy=$ErrorActionPreference
    try {
        # Windows PowerShell 5.1 represents redirected native stderr as errors,
        # even for a successful command (e.g. monkey's normal args diagnostic).
        # Capture both streams and decide native failure from its exit code.
        $ErrorActionPreference='Continue'
        $Output=& $Executable @NativeArguments 2>&1
        $NativeExitCode=$LASTEXITCODE
    } finally {$ErrorActionPreference=$PreviousErrorPolicy}
    if($NativeExitCode -ne 0){throw "Command failed ($NativeExitCode): $Executable $($NativeArguments -join ' ')`n$($Output -join "`n")"}
    foreach($Entry in @($Output)){
        # Preserve actual PowerShell invocation errors such as a missing exe.
        if($Entry -is [System.Management.Automation.ErrorRecord] -and $Entry.FullyQualifiedErrorId -notmatch '^NativeCommandError(?:Message)?(?:,|$)'){throw $Entry}
    }
    return ($Output -join "`n")
}
function Select-AuthorizedDevice([string]$Output,[string]$Requested) {
    $Devices=@();foreach($Line in ($Output -split "`r?`n")){if($Line -match '^(\S+)\s+(device|offline|unauthorized)\b'){$Devices+=@{serial=$Matches[1];state=$Matches[2]}}}
    if($Requested){$MatchesDevice=@($Devices | Where-Object {$_.serial -eq $Requested});if($MatchesDevice.Count -ne 1 -or $MatchesDevice[0].state -ne 'device'){throw 'Selected device is missing or unauthorized. Unlock Pixel and allow USB debugging.'};return $Requested}
    if($Devices.Count -ne 1){throw 'Connect one device, or select its serial with -Serial.'}
    if($Devices[0].state -ne 'device'){throw 'Unlock Pixel and allow USB debugging.'};return $Devices[0].serial
}
function Invoke-PixelInstall([string]$ApkPath,[string]$AdbPath,[string]$RequestedSerial,[string]$ExpectedPackage,[string]$AaptPath,[switch]$FreshTownQa,[string]$ExpectedSha256) {
    # The user explicitly authorized a fresh test of this QA app on this Pixel.
    # Default/update mode never uninstalls or clears any data.
    if($FreshTownQa -and ($ExpectedPackage -ne 'com.mofumachi.merge.qa.townlife' -or $RequestedSerial -ne '99RAY1BELH')){throw 'Fresh mode is limited to the authorized town QA package on Pixel99RAY1BELH.'}
    foreach($Path in @($ApkPath,$AdbPath,$AaptPath)){if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "File missing: $Path"}}
    $ApkPath=(Resolve-Path -LiteralPath $ApkPath).Path
    if($FreshTownQa -or $ExpectedSha256) {
        if($ExpectedSha256 -notmatch '^[a-fA-F0-9]{64}$' -or (Get-FileHash -LiteralPath $ApkPath -Algorithm SHA256).Hash -ne $ExpectedSha256){throw 'Expected APK SHA256 is required and must match before device/data changes.'}
    }
    $Badging=Invoke-CheckedNative $AaptPath @('dump','badging',$ApkPath)
    if($Badging -notmatch "(?m)^package: name='([^']+)' versionCode='([0-9]+)'"){throw 'Cannot identify APK package.'}
    if($Matches[1] -ne $ExpectedPackage){throw "Wrong APK: $($Matches[1]); expected $ExpectedPackage"}
    if($Badging -notmatch "native-code:.*'arm64-v8a'"){throw 'APK has no ARM64 native code.'}
    $Devices=Invoke-CheckedNative $AdbPath @('devices');$Chosen=Select-AuthorizedDevice $Devices $RequestedSerial
    $Api=Invoke-CheckedNative $AdbPath @('-s',$Chosen,'shell','getprop','ro.build.version.sdk')
    $Abi=Invoke-CheckedNative $AdbPath @('-s',$Chosen,'shell','getprop','ro.product.cpu.abilist')
    if($Api.Trim() -notmatch '^\d+$' -or [int]$Api.Trim() -lt 26 -or $Abi -notmatch 'arm64-v8a'){throw 'Device requires Android API26+ and ARM64.'}
    if($FreshTownQa) {
        $Model=Invoke-CheckedNative $AdbPath @('-s',$Chosen,'shell','getprop','ro.product.model')
        if($Model.Trim() -ne 'Pixel 3a'){throw 'Fresh town QA requires the authorized Pixel 3a.'}
        $Installed=Invoke-CheckedNative $AdbPath @('-s',$Chosen,'shell','pm','list','packages','--user','0',$ExpectedPackage)
        if($Installed -match ('(?m)^package:'+ [regex]::Escape($ExpectedPackage)+'\s*$')) {
            $Uninstall=Invoke-CheckedNative $AdbPath @('-s',$Chosen,'uninstall',$ExpectedPackage)
            if($Uninstall -notmatch '(?m)^Success\s*$'){throw "Town QA uninstall failed; stop before installing or clearing anything.`n$Uninstall"}
        }
        $Install=Invoke-CheckedNative $AdbPath @('-s',$Chosen,'install',$ApkPath)
    } else {$Install=Invoke-CheckedNative $AdbPath @('-s',$Chosen,'install','-r',$ApkPath)}
    if($Install -notmatch '(?m)^Success\s*$'){throw "APK install failed. Stop here; do not retry by deleting other apps.`n$Install"}
    if($FreshTownQa) {
        # Android backup may restore the just-installed QA package. Clear only
        # that package before its first launch; no system backup setting changes.
        $Clear=Invoke-CheckedNative $AdbPath @('-s',$Chosen,'shell','pm','clear','--user','0',$ExpectedPackage)
        if($Clear -notmatch '(?m)^Success\s*$'){throw "Town QA initial-data reset failed; do not claim an initial state.`n$Clear"}
    }
    Invoke-CheckedNative $AdbPath @('-s',$Chosen,'shell','monkey','-p',$ExpectedPackage,'-c','android.intent.category.LAUNCHER','1') | Write-Host
    if($FreshTownQa){Write-Host "Fresh town QA installed/reset on $Chosen. Verify unselected character, TownLv0,0Coins,and two teaLv1 items; device acceptance remains pending."}
    else {Write-Host "Installed $ExpectedPackage on $Chosen (API $($Api.Trim())) using install-r. Verify saved progress after restart."}
    Write-Host "Restart check: & `"$AdbPath`" -s $Chosen shell am force-stop $ExpectedPackage"
    Write-Host "Then reopen the same app on Pixel. APK SHA256: $((Get-FileHash -LiteralPath $ApkPath -Algorithm SHA256).Hash)"
}
if($MyInvocation.InvocationName -eq '.'){return}
if(-not $Apk){throw 'Specify the newly built APK using -Apk.'}
$Sdk=Split-Path (Split-Path $Adb -Parent) -Parent
$Aapt2=Join-Path $Sdk 'build-tools\36.0.0\aapt2.exe'
Invoke-PixelInstall $Apk $Adb $Serial $PackageName $Aapt2 -FreshTownQa:$FreshTownQa -ExpectedSha256 $ExpectedSha256
