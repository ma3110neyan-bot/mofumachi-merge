#requires -Version 5.1
[CmdletBinding()]
param()
Set-StrictMode -Version 2.0
$ErrorActionPreference='Stop'
$FontCheckSource='75cd7c0ef234934a17aa623860937d894a1d875c'
$FontCheckTests=@(
    'Mofumachi.Tests.ReleaseUxNativeTests.ScopedFontsFitTitleTownMergeBookSettingsAndTodayAtPhoneSizes',
    'Mofumachi.Tests.StoryTypographyTests.StoryRolesAndNoticeFitPortraitSizes'
)
function Get-FontCheckArguments([string]$Project,[string]$Run) {
    return @('-batchmode','-projectPath',$Project,'-buildTarget','Android','-runTests','-testPlatform','PlayMode',
        '-testFilter',($FontCheckTests -join ';'),'-testResults',(Join-Path $Run 'capture.xml'),
        '-logFile',(Join-Path $Run 'capture.log'),'-mofumachiSourceRevision',$FontCheckSource,
        '-mofumachiFontCheckOutput',$Run)
}
function Read-FontCheckXml([string]$Path) {
    if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw 'Unity did not create test result XML.'}
    $Settings=New-Object Xml.XmlReaderSettings;$Settings.DtdProcessing=[Xml.DtdProcessing]::Prohibit;$Settings.XmlResolver=$null
    $Reader=[Xml.XmlReader]::Create($Path,$Settings)
    try{$Doc=New-Object Xml.XmlDocument;$Doc.XmlResolver=$null;$Doc.Load($Reader);return $Doc}finally{$Reader.Dispose()}
}
function Assert-FontCheckResults([string]$Path) {
    $Doc=Read-FontCheckXml $Path;$Root=$Doc.DocumentElement
    if($Root.Name -ne 'test-run' -or $Root.GetAttribute('result') -ne 'Passed' -or
       $Root.GetAttribute('total') -ne '2' -or $Root.GetAttribute('passed') -ne '2' -or
       $Root.GetAttribute('failed') -ne '0' -or $Root.GetAttribute('skipped') -ne '0' -or $Root.GetAttribute('inconclusive') -ne '0'){
        throw 'Both requested tests must pass; exactly two results and no skipped/inconclusive cases are required.'
    }
    $Cases=@($Doc.SelectNodes('//test-case'))
    if($Cases.Count -ne 2){throw 'Expected exactly two test-case results.'}
    foreach($Name in $FontCheckTests){
        $Matched=@($Cases|Where-Object {$_.GetAttribute('fullname') -ceq $Name -and $_.GetAttribute('result') -ceq 'Passed'})
        if($Matched.Count -ne 1){throw "Missing/failed/duplicate requested test: $Name"}
    }
}
function Get-FontCheckError([string]$Run) {
    $Xml=Join-Path $Run 'capture.xml'
    if(Test-Path -LiteralPath $Xml){
        try{
            $Doc=Read-FontCheckXml $Xml
            $Cases=@($Doc.SelectNodes('//test-case[failure]')|Sort-Object { $_.GetAttribute('start-time') })
            if($Cases.Count){
                $C=$Cases[0];$Message=$C.SelectSingleNode('failure/message');$Stack=$C.SelectSingleNode('failure/stack-trace')
                return ($C.GetAttribute('fullname')+"`n"+$(if($Message){$Message.InnerText})+"`n"+$(if($Stack){$Stack.InnerText}))
            }
        }catch{}
    }
    $CaptureError=Join-Path $Run 'capture-error.log'
    if(Test-Path -LiteralPath $CaptureError){return [IO.File]::ReadAllText($CaptureError)}
    $Log=Join-Path $Run 'capture.log'
    if(Test-Path -LiteralPath $Log){
        $Lines=[IO.File]::ReadAllLines($Log)
        for($i=0;$i -lt $Lines.Length;$i++){
            if($Lines[$i] -match 'error\s+CS\d{4}|Scripts have compiler errors|No valid Unity Editor license|Aborting batchmode|Failed to initialize graphics|Build target.*not supported|Unhandled Exception'){
                $Start=[Math]::Max(0,$i-2);$End=[Math]::Min($Lines.Length-1,$i+3)
                return ($Lines[$Start..$End] -join "`n")
            }
        }
    }
    return 'No causal Unity error could be extracted. See the preserved Unity log and source-info record; do not retry automatically.'
}
function Assert-FontCheckImages([string]$Run) {
    if(Test-Path -LiteralPath (Join-Path $Run 'capture-error.log')){throw 'Post-success capture failed; see capture-error.log.'}
    $Path=Join-Path $Run 'capture-native.json'
    if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw 'Post-success native screenshots were not produced.'}
    $Proof=[IO.File]::ReadAllText($Path)|ConvertFrom-Json
    if($Proof.source_revision -cne $FontCheckSource -or $Proof.tests_passed_before_capture -ne $true -or $Proof.image_count -ne 4){throw 'Invalid screenshot provenance.'}
    $Expected=@('merge-720x1480.png','conversation-720x1480.png','merge-640x1072.png','conversation-640x1072.png')
    $Rows=@($Proof.images)
    if($Rows.Count -ne 4){throw 'Four minimum native screenshots required.'}
    foreach($Name in $Expected){
        $Row=@($Rows|Where-Object {$_.file -ceq $Name});$File=Join-Path (Join-Path $Run 'images') $Name
        if($Row.Count -ne 1 -or -not(Test-Path -LiteralPath $File -PathType Leaf)){throw "Native screenshot missing: $Name"}
        if((Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash -ine $Row[0].sha256){throw "Native screenshot changed: $Name"}
        $Bytes=[IO.File]::ReadAllBytes($File)
        if($Bytes.Length -lt 24 -or [BitConverter]::ToString($Bytes,0,8) -ne '89-50-4E-47-0D-0A-1A-0A'){throw 'Invalid native PNG.'}
        $Width=([int]$Bytes[16]*16777216)+([int]$Bytes[17]*65536)+([int]$Bytes[18]*256)+$Bytes[19]
        $Height=([int]$Bytes[20]*16777216)+([int]$Bytes[21]*65536)+([int]$Bytes[22]*256)+$Bytes[23]
        if($Name -notmatch ('-'+$Width+'x'+$Height+'\.png$') -or $Row[0].width -ne $Width -or $Row[0].height -ne $Height){throw 'PNG dimensions do not match the expected phone size.'}
    }
    if(@(Get-ChildItem -LiteralPath (Join-Path $Run 'images') -File).Count -ne 4){throw 'Unexpected screenshot files.'}
}
function Find-FontCheckGit {
    foreach($Name in @('git.exe','git')){$Command=Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue|Select-Object -First 1;if($Command){return $Command.Source}}
    if($env:LOCALAPPDATA){$Bundled=Get-ChildItem -Path (Join-Path $env:LOCALAPPDATA 'GitHubDesktop/app-*/resources/app/git/cmd/git.exe') -File -ErrorAction SilentlyContinue|Sort-Object LastWriteTimeUtc -Descending|Select-Object -First 1;if($Bundled){return $Bundled.FullName}}
    throw 'Git for Windows or GitHub Desktop Git is required.'
}
function Invoke-FontCheckGit([string[]]$Arguments,[switch]$AllowFailure) {
    $SavedConsole=[Console]::OutputEncoding;$SavedOutput=$OutputEncoding;$SavedPreference=$ErrorActionPreference
    try{
        [Console]::OutputEncoding=[Text.UTF8Encoding]::new($false);$OutputEncoding=[Console]::OutputEncoding;$ErrorActionPreference='Continue'
        $Lines=@(& $Git -C $Repo -c ('core.hooksPath='+$Hooks) @Arguments 2>&1|ForEach-Object {$_.ToString()});$Code=$LASTEXITCODE
    }finally{[Console]::OutputEncoding=$SavedConsole;$OutputEncoding=$SavedOutput;$ErrorActionPreference=$SavedPreference}
    if($Code -ne 0 -and -not $AllowFailure){throw ('Git operation failed: '+($Arguments -join ' ')+"`n"+($Lines -join "`n"))}
    return [pscustomobject]@{Code=$Code;Text=($Lines -join "`n").Trim()}
}
function Quote-FontCheckArgument([string]$Value) {
    if($Value.Contains('"')){throw 'Unexpected quote in an argument.'}
    return '"'+$Value+'"'
}
if($MyInvocation.InvocationName -eq '.'){return}
if($env:OS -ne 'Windows_NT'){throw 'This launcher is for Windows 11 and Windows PowerShell 5.1. Unity was not started.'}
$Repo=[IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$Git=Find-FontCheckGit
$Editor=Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'
if(-not(Test-Path -LiteralPath $Editor -PathType Leaf)){throw 'Unity6000.6.4f1 with Android support must be installed at the Unity Hub default location.'}
$EditorVersion=(Get-Item -LiteralPath $Editor).VersionInfo.ProductVersion
if($EditorVersion -notmatch '(?<![0-9])6000\.6\.4f1(?![0-9])'){throw ('Unexpected Unity executable version: '+$EditorVersion)}
if(Get-Process -Name Unity -ErrorAction SilentlyContinue){throw 'Close Unity Editor before starting this one-shot verification.'}
foreach($Name in @('GIT_DIR','GIT_WORK_TREE','GIT_COMMON_DIR','GIT_INDEX_FILE')){if([Environment]::GetEnvironmentVariable($Name)){throw "Custom $Name is set; original repository preserved."}}
$Hooks=Join-Path ([IO.Path]::GetTempPath()) ('font-check-hooks-'+[guid]::NewGuid().ToString('N'))
$null=[IO.Directory]::CreateDirectory($Hooks)
$Run=$null;$Record=$null;$Failure=$null
try{
    if((Invoke-FontCheckGit @('rev-parse','--show-toplevel')).Text.Replace('/','\').TrimEnd('\') -ine $Repo.Replace('/','\').TrimEnd('\')){throw 'Launcher must be inside its repository scripts folder.'}
    $null=Invoke-FontCheckGit @('cat-file','-e',($FontCheckSource+'^{commit}'))
    $Suffix=(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8)
    $Run=Join-Path $Repo ('Builds/Diagnostics/font-only-'+$Suffix)
    $Project=Join-Path (Split-Path -Parent $Repo) ('font-check-'+$Suffix.Substring($Suffix.Length-8))
    $null=[IO.Directory]::CreateDirectory($Run)
    $Record=[ordered]@{scope='font-only-windows-two-tests';gitRevision=$FontCheckSource;project_path=$Project;source_unchanged=$false;capture_tool_sha256='';unity_exit_code=$null;test_invocations=0;tests_passed=$false;capture_verified=$false;image_count=0;success=$false;error='';full_qa_verified=$false;build_generated=$false;pixel_verified=$false;visual_approved=$false}
    [IO.File]::WriteAllText((Join-Path $Run 'source-revision.txt'),$FontCheckSource+"`r`n",[Text.UTF8Encoding]::new($true))
    $null=Invoke-FontCheckGit @('worktree','add','--detach',$Project,$FontCheckSource)
    if((Invoke-FontCheckGit @('-C',$Project,'rev-parse','HEAD')).Text -cne $FontCheckSource){throw 'Pinned source commit differs.'}
    if([IO.File]::ReadAllText((Join-Path $Project 'ProjectSettings/ProjectVersion.txt')) -notmatch 'm_EditorVersion: 6000\.6\.4f1'){throw 'Pinned project Unity version differs.'}
    # One callback-only helper joins the existing PlayMode test assembly; no test is added.
    $Tool=Join-Path $Project 'Assets/Mofumachi/Tests/PlayMode'
    [IO.File]::Copy((Join-Path $PSScriptRoot 'font-validation/FontValidationCallback.cs'),(Join-Path $Tool 'FontValidationCallback.cs'),$false)
    $Record.capture_tool_sha256=(Get-FileHash -LiteralPath (Join-Path $Tool 'FontValidationCallback.cs') -Algorithm SHA256).Hash.ToLowerInvariant()
    $Record.test_invocations=1
    $Record|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $Run 'font-only-source-info.json') -Encoding UTF8
    $Arguments=@(Get-FontCheckArguments $Project $Run)
    [IO.File]::WriteAllLines((Join-Path $Run 'unity-arguments.txt'),$Arguments,[Text.UTF8Encoding]::new($true))
    Write-Host 'Running exactly two font tests ONCE. First import can take several minutes. Do not launch again.'
    Write-Host ('Preserved results: '+$Run)
    $Start=New-Object Diagnostics.ProcessStartInfo;$Start.FileName=$Editor;$Start.UseShellExecute=$false;$Start.WorkingDirectory=$Project
    $Start.Arguments=(@($Arguments|ForEach-Object {Quote-FontCheckArgument $_}) -join ' ')
    $Process=[Diagnostics.Process]::Start($Start);$Ticks=0
    while(-not $Process.WaitForExit(1000)){$Ticks++;if($Ticks % 30 -eq 0){Write-Host 'Unity is still running. Progress is recorded in capture.log.'}}
    $Record.unity_exit_code=$Process.ExitCode
    $SourcePaths=@('Assets/Mofumachi','Assets/Scenes','Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt')
    $Drift=Invoke-FontCheckGit (@('-C',$Project,'diff','--quiet','--')+$SourcePaths) -AllowFailure
    if($Drift.Code -ne 0){throw 'Pinned product inputs changed in the isolated worktree. Evidence is preserved.'}
    $Record.source_unchanged=$true
    if($Process.ExitCode -ne 0){throw ('Unity exit '+$Process.ExitCode+'. No retry will be performed.')}
    Assert-FontCheckResults (Join-Path $Run 'capture.xml');$Record.tests_passed=$true
    Assert-FontCheckImages $Run;$Record.capture_verified=$true;$Record.image_count=4;$Record.success=$true
}catch{
    $Failure=$_
    if($Record){$Record.error=$_.Exception.Message}
    if($Run){$First=Get-FontCheckError $Run;[IO.File]::WriteAllText((Join-Path $Run 'first-error.log'),$First,[Text.UTF8Encoding]::new($true))}
}finally{
    if($Record){$Record|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $Run 'font-only-source-info.json') -Encoding UTF8}
    if(Test-Path -LiteralPath $Hooks){[IO.Directory]::Delete($Hooks,$true)}
}
if($Failure){if($Run){Write-Host ('Stopped. Preserve this folder: '+$Run)};throw $Failure}
Write-Host ('PASS: two tests and four real Unity PNGs. Open '+$Run)
Start-Process explorer.exe -ArgumentList ('"'+$Run+'"')
