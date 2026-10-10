[CmdletBinding()]
param([string]$Apk,[string]$Adb="$env:ProgramFiles\Unity\Hub\Editor\6000.6.4f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe",[string]$Serial,[ValidateSet('com.mofumachi.merge','com.mofumachi.merge.qa')][string]$PackageName='com.mofumachi.merge')
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
function Invoke-PixelInstall([string]$ApkPath,[string]$AdbPath,[string]$RequestedSerial,[string]$ExpectedPackage,[string]$AaptPath) {
    foreach($Path in @($ApkPath,$AdbPath,$AaptPath)){if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "File missing: $Path"}}
    $ApkPath=(Resolve-Path -LiteralPath $ApkPath).Path
    $Badging=Invoke-CheckedNative $AaptPath @('dump','badging',$ApkPath)
    if($Badging -notmatch "(?m)^package: name='([^']+)' versionCode='([0-9]+)'"){throw 'Cannot identify APK package.'}
    if($Matches[1] -ne $ExpectedPackage){throw "Wrong APK: $($Matches[1]); expected $ExpectedPackage"}
    if($Badging -notmatch "native-code:.*'arm64-v8a'"){throw 'APK has no ARM64 native code.'}
    $Devices=Invoke-CheckedNative $AdbPath @('devices');$Chosen=Select-AuthorizedDevice $Devices $RequestedSerial
    $Api=Invoke-CheckedNative $AdbPath @('-s',$Chosen,'shell','getprop','ro.build.version.sdk')
    $Abi=Invoke-CheckedNative $AdbPath @('-s',$Chosen,'shell','getprop','ro.product.cpu.abilist')
    if($Api.Trim() -notmatch '^\d+$' -or [int]$Api.Trim() -lt 26 -or $Abi -notmatch 'arm64-v8a'){throw 'Device requires Android API26+ and ARM64.'}
    $Install=Invoke-CheckedNative $AdbPath @('-s',$Chosen,'install','-r',$ApkPath)
    if($Install -notmatch '(?m)^Success\s*$'){throw "Update install failed. Keep existing save; do not uninstall automatically.`n$Install"}
    Invoke-CheckedNative $AdbPath @('-s',$Chosen,'shell','monkey','-p',$ExpectedPackage,'-c','android.intent.category.LAUNCHER','1') | Write-Host
    Write-Host "Installed $ExpectedPackage on $Chosen (API $($Api.Trim())). Save data was retained."
    Write-Host "Restart check: & `"$AdbPath`" -s $Chosen shell am force-stop $ExpectedPackage"
    Write-Host "Then reopen the same app on Pixel. APK SHA256: $((Get-FileHash -LiteralPath $ApkPath -Algorithm SHA256).Hash)"
}
if($MyInvocation.InvocationName -eq '.'){return}
if(-not $Apk){throw 'Specify the newly built APK using -Apk.'}
$Sdk=Split-Path (Split-Path $Adb -Parent) -Parent
$Aapt2=Join-Path $Sdk 'build-tools\36.0.0\aapt2.exe'
Invoke-PixelInstall $Apk $Adb $Serial $PackageName $Aapt2
