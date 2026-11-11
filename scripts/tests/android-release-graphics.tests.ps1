param([Parameter(Mandatory=$true)][string]$ResultDirectory)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
if (Test-Path -LiteralPath $ResultDirectory) { throw 'Use a new test result directory; no overwrite or automatic retry.' }
$null = [IO.Directory]::CreateDirectory($ResultDirectory)
$Source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../Assets/Mofumachi/Editor/AndroidReleaseBuild.cs'))
# Compile the complete production file against managed API doubles. No Unity assemblies,
# signing inputs, source guard fixtures, native builds, or ProjectSettings files are used.
$Doubles = @'
namespace UnityEngine {
    public enum RuntimePlatform { WindowsEditor }
    public enum UIOrientation { Portrait }
    public static class Application {
        public static RuntimePlatform platform = RuntimePlatform.WindowsEditor;
        public static string unityVersion = "6000.6.4f1", dataPath = "/graphics-test-only/Assets";
    }
    public static class Debug { public static void Log(string message) {} }
    public static class JsonUtility { public static string ToJson(object value, bool pretty) { return "{}"; } }
}
namespace UnityEngine.Rendering { public enum GraphicsDeviceType { OpenGLES3 = 11, Vulkan = 21 } }
namespace UnityEditor.Build {
    public struct NamedBuildTarget { public static readonly NamedBuildTarget Android = new NamedBuildTarget(); }
    public class BuildFailedException : System.Exception { public BuildFailedException(string message) : base(message) {} }
}
namespace UnityEditor.Build.Reporting {
    public enum BuildResult { Succeeded, Failed }
    public class BuildSummary { public int totalErrors = 0; public BuildResult result = BuildResult.Failed; }
    public class BuildReport { public BuildSummary summary = new BuildSummary(); }
}
namespace UnityEditor {
    using UnityEngine.Rendering;
    public enum BuildTarget { Android }
    public enum BuildTargetGroup { Android }
    public enum AndroidArchitecture { ARM64 }
    public enum ScriptingImplementation { IL2CPP }
    public enum AndroidSdkVersions { AndroidApiLevel26 = 26, AndroidApiLevel36 = 36 }
    public enum BuildOptions { None }
    public struct BuildPlayerOptions { public string[] scenes; public string locationPathName; public BuildTarget target; public BuildOptions options; }
    public static class EditorUserBuildSettings {
        public static BuildTarget activeBuildTarget = BuildTarget.Android;
        public static bool development, allowDebugging, buildWithDeepProfilingSupport, connectProfiler, buildAppBundle, exportAsGoogleAndroidProject;
    }
    public class EditorBuildSettingsScene { public bool enabled = false; public string path = ""; }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[0]; }
    public static class AssetDatabase {
        public static int SaveCalls = 0;
        public static void SaveAssets() { SaveCalls++; throw new System.InvalidOperationException("Graphics-only tests must not save assets."); }
    }
    public static class BuildPipeline {
        public static int BuildCalls = 0;
        public static bool IsBuildTargetSupported(BuildTargetGroup group, BuildTarget target) { return true; }
        public static Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions options) {
            BuildCalls++; throw new System.InvalidOperationException("Graphics-only tests must not invoke a build.");
        }
    }
    public static class PlayerSettings {
        public static readonly System.Collections.Generic.List<string> Calls = new System.Collections.Generic.List<string>();
        public static GraphicsDeviceType[] FixtureApis = new GraphicsDeviceType[0];
        public static bool FixtureAutomatic = false, ThrowGraphicsOnce = false;
        public static string bundleVersion = "fixture";
        public static UnityEngine.UIOrientation defaultInterfaceOrientation = UnityEngine.UIOrientation.Portrait;
        public static GraphicsDeviceType[] GetGraphicsAPIs(BuildTarget target) { return FixtureApis == null ? null : (GraphicsDeviceType[])FixtureApis.Clone(); }
        public static bool GetUseDefaultGraphicsAPIs(BuildTarget target) { return FixtureAutomatic; }
        public static void SetUseDefaultGraphicsAPIs(BuildTarget target, bool automatic) { Calls.Add("automatic"); FixtureAutomatic = automatic; }
        public static void SetGraphicsAPIs(BuildTarget target, GraphicsDeviceType[] apis) {
            Calls.Add("apis");
            if (ThrowGraphicsOnce) { ThrowGraphicsOnce = false; throw new System.InvalidOperationException("Synthetic graphics setter failure"); }
            FixtureApis = apis == null ? null : (GraphicsDeviceType[])apis.Clone();
        }
        public static string GetApplicationIdentifier(Build.NamedBuildTarget target) { return "com.mofumachi.merge"; }
        public static ScriptingImplementation GetScriptingBackend(Build.NamedBuildTarget target) { return ScriptingImplementation.IL2CPP; }
        public static void SetScriptingBackend(Build.NamedBuildTarget target, ScriptingImplementation backend) {}
        public static class Android {
            public static bool useCustomKeystore = false;
            public static string keystoreName = "", keystorePass = "", keyaliasName = "", keyaliasPass = "";
            public static AndroidArchitecture targetArchitectures = AndroidArchitecture.ARM64;
            public static int bundleVersionCode = 1;
            public static AndroidSdkVersions minSdkVersion = AndroidSdkVersions.AndroidApiLevel26, targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
        }
    }
}
'@
$Rows = New-Object Collections.Generic.List[object]
$Cases = New-Object Collections.Generic.List[object]
$Log = New-Object Collections.Generic.List[string]
$Failure = $null
$Phase = 'managed-test-compile'
try {
    Add-Type -TypeDefinition ($Source + [Environment]::NewLine + $Doubles) -Language CSharp
    $ScopeType = [Mofumachi.Editor.AndroidReleaseBuild].GetNestedType('TemporaryGraphicsSettings', [Reflection.BindingFlags]::NonPublic)
    function Check([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
    function Fixture([bool]$Automatic, [UnityEngine.Rendering.GraphicsDeviceType[]]$Apis) {
        Check ($null -ne $ScopeType) 'Missing production TemporaryGraphicsSettings: unconditional graphics setters are not yet avoided.'
        [UnityEditor.PlayerSettings]::Calls.Clear()
        [UnityEditor.PlayerSettings]::FixtureAutomatic = $Automatic
        [UnityEditor.PlayerSettings]::FixtureApis = $Apis
        [UnityEditor.PlayerSettings]::ThrowGraphicsOnce = $false
        return [Activator]::CreateInstance($ScopeType, $true)
    }
    function Value($Fixture, [string]$Name) { return $ScopeType.GetProperty($Name).GetValue($Fixture) }
    function Invoke($Fixture, [string]$Name) { $null = $ScopeType.GetMethod($Name).Invoke($Fixture, $null) }
    function Desired {
        Check (-not [UnityEditor.PlayerSettings]::FixtureAutomatic) 'Automatic graphics remained enabled.'
        Check ([UnityEditor.PlayerSettings]::FixtureApis.Length -eq 1 -and [UnityEditor.PlayerSettings]::FixtureApis[0] -eq [UnityEngine.Rendering.GraphicsDeviceType]::OpenGLES3) 'Desired graphics is not exactly OpenGLES3.'
    }
    function Case([string]$Name, [scriptblock]$Body) { $Cases.Add(@{name=$Name;body=$Body}) }
    $Gles = [UnityEngine.Rendering.GraphicsDeviceType]::OpenGLES3
    $Vulkan = [UnityEngine.Rendering.GraphicsDeviceType]::Vulkan

    Case '1 automatic=false and only OpenGLES3 skips both apply setters' {
        $F = Fixture $false @($Gles)
        Check (Value $F 'AlreadyDesired') 'Desired settings were not recognized.'
        Invoke $F 'Apply'
        Check ([UnityEditor.PlayerSettings]::Calls.Count -eq 0) 'Desired apply invoked a setter.'
        Check (-not (Value $F 'Changed')) 'Desired apply was marked changed.'
        Desired
    }
    Case '2 automatic=true requires and applies desired settings' {
        $F = Fixture $true @($Gles)
        Check (-not (Value $F 'AlreadyDesired')) 'Automatic=true was accepted as desired.'
        Invoke $F 'Apply'
        Check ([UnityEditor.PlayerSettings]::Calls.Count -eq 2 -and (Value $F 'Changed')) 'Both apply setters must remain for non-desired settings.'
        Desired
        [UnityEditor.PlayerSettings]::Calls.Clear()
        Invoke $F 'Restore'
        Check (([UnityEditor.PlayerSettings]::Calls -join ',') -ceq 'apis,automatic' -and (Value $F 'Restored')) 'Automatic=true case did not pair apply with restore.'
        Check ([UnityEditor.PlayerSettings]::FixtureAutomatic) 'Automatic=true case lost its original automatic flag.'
        Check ([UnityEditor.PlayerSettings]::FixtureApis.Length -eq 1 -and [UnityEditor.PlayerSettings]::FixtureApis[0] -eq $Gles) 'Automatic=true case lost its original API array.'
    }
    Case '3 another single graphics API requires a change' {
        $F = Fixture $false @($Vulkan)
        Check (-not (Value $F 'AlreadyDesired')) 'Vulkan was accepted as desired.'
        Invoke $F 'Apply'
        Check ([UnityEditor.PlayerSettings]::Calls.Count -eq 2) 'Non-desired single API skipped setters.'
        Desired
        [UnityEditor.PlayerSettings]::Calls.Clear()
        Invoke $F 'Restore'
        Check (([UnityEditor.PlayerSettings]::Calls -join ',') -ceq 'apis,automatic' -and (Value $F 'Restored')) 'Single other API did not pair apply with restore.'
        Check (-not [UnityEditor.PlayerSettings]::FixtureAutomatic) 'Single other API lost its original automatic=false.'
        Check ([UnityEditor.PlayerSettings]::FixtureApis.Length -eq 1 -and [UnityEditor.PlayerSettings]::FixtureApis[0] -eq $Vulkan) 'Single other API was not restored.'
    }
    Case '4 multiple graphics APIs require a change' {
        $F = Fixture $false @($Gles, $Vulkan)
        Check (-not (Value $F 'AlreadyDesired')) 'Multiple APIs were accepted as desired.'
        Invoke $F 'Apply'
        Check ([UnityEditor.PlayerSettings]::Calls.Count -eq 2) 'Multiple APIs skipped setters.'
        Desired
        [UnityEditor.PlayerSettings]::Calls.Clear()
        Invoke $F 'Restore'
        Check (([UnityEditor.PlayerSettings]::Calls -join ',') -ceq 'apis,automatic' -and (Value $F 'Restored')) 'Multiple APIs did not pair apply with restore.'
        Check (-not [UnityEditor.PlayerSettings]::FixtureAutomatic) 'Multiple APIs lost their original automatic=false.'
        Check (([UnityEditor.PlayerSettings]::FixtureApis -join ',') -ceq 'OpenGLES3,Vulkan') 'Multiple APIs were not restored in original order.'
    }
    Case '5 changed graphics restore original order and automatic flag' {
        $F = Fixture $true @($Vulkan, $Gles)
        Invoke $F 'Apply'
        [UnityEditor.PlayerSettings]::Calls.Clear()
        Invoke $F 'Restore'
        Check (([UnityEditor.PlayerSettings]::Calls -join ',') -ceq 'apis,automatic') 'Restore setter count/order changed.'
        Check ([UnityEditor.PlayerSettings]::FixtureAutomatic) 'Original automatic flag was not restored.'
        Check (([UnityEditor.PlayerSettings]::FixtureApis -join ',') -ceq 'Vulkan,OpenGLES3') 'Original API order was not restored.'
        Check ((Value $F 'Changed') -and (Value $F 'Restored')) 'Changed/restored evidence is incorrect.'
    }
    Case '6 already desired skips both restore setters' {
        $F = Fixture $false @($Gles)
        Invoke $F 'Apply'
        Invoke $F 'Restore'
        Check ([UnityEditor.PlayerSettings]::Calls.Count -eq 0) 'Desired lifecycle invoked a setter.'
        Check (-not (Value $F 'Changed') -and -not (Value $F 'Restored')) 'A skipped restore was recorded as performed.'
        Desired
    }
    Case '7 null API array is not already desired' {
        $F = Fixture $false $null
        Check (-not (Value $F 'AlreadyDesired')) 'Null APIs were accepted as desired.'
        Invoke $F 'Apply'
        Desired
    }
    Case '8 empty API array is not already desired' {
        $F = Fixture $false @()
        Check (-not (Value $F 'AlreadyDesired')) 'Empty APIs were accepted as desired.'
        Invoke $F 'Apply'
        Desired
    }
    Case '9 duplicate OpenGLES3 entries are not a single desired API' {
        $F = Fixture $false @($Gles, $Gles)
        Check (-not (Value $F 'AlreadyDesired')) 'Duplicate APIs were accepted as a single API.'
        Invoke $F 'Apply'
        Desired
    }
    Case '10 partial apply failure still permits restoration without retry' {
        $F = Fixture $true @($Vulkan)
        [UnityEditor.PlayerSettings]::ThrowGraphicsOnce = $true
        $Rejected = $false
        try { Invoke $F 'Apply' } catch { $Rejected = $_.Exception.ToString().Contains('Synthetic graphics setter failure') }
        Check $Rejected 'Expected synthetic setter failure was not observed.'
        Check (Value $F 'Changed') 'Partial apply lost its restore obligation.'
        Check ([UnityEditor.PlayerSettings]::Calls.Count -eq 2) 'Apply retried after a setter failure.'
        [UnityEditor.PlayerSettings]::Calls.Clear()
        Invoke $F 'Restore'
        Check ([UnityEditor.PlayerSettings]::Calls.Count -eq 2 -and (Value $F 'Restored')) 'Partial apply did not restore.'
        Check ([UnityEditor.PlayerSettings]::FixtureAutomatic -and [UnityEditor.PlayerSettings]::FixtureApis[0] -eq $Vulkan) 'Partial apply did not recover original graphics.'
    }
    Case '11 non-desired settings not applied do not invoke restore setters' {
        $F = Fixture $true @($Vulkan)
        Invoke $F 'Restore'
        Check ([UnityEditor.PlayerSettings]::Calls.Count -eq 0) 'Restore ran before any apply.'
        Check (-not (Value $F 'Changed') -and -not (Value $F 'Restored')) 'Unapplied graphics were marked restored.'
    }
    Case '12 build wiring and public evidence retain other-setting SaveAssets' {
        Check ($Source -match 'graphicsSettings\.Apply\(\);') 'Build does not invoke the tested apply policy.'
        Check ($Source -match '(?s)finally\s*\{.*graphicsSettings\.Restore\(\);') 'Build finally does not invoke the tested restore policy.'
        foreach ($Field in @('initial_graphics_apis','initial_automatic','graphics_settings_already_desired','graphics_settings_changed','graphics_settings_restored')) {
            Check ($null -ne [Mofumachi.Editor.AndroidReleaseBuild].GetNestedType('BuildInfo',[Reflection.BindingFlags]::NonPublic).GetField($Field)) ('Missing BuildInfo evidence field: '+$Field)
        }
        Check ([regex]::Matches($Source, 'AssetDatabase\.SaveAssets\(\)').Count -eq 1) 'Existing SaveAssets count changed.'
        Check ([regex]::Matches($Source, 'PlayerSettings\.Set(?:UseDefaultGraphicsAPIs|GraphicsAPIs)\(').Count -eq 4) 'Unexpected additional graphics setter call sites.'
        Check ([UnityEditor.AssetDatabase]::SaveCalls -eq 0 -and [UnityEditor.BuildPipeline]::BuildCalls -eq 0) 'Limited tests invoked asset save or build.'
    }
    $Phase = 'graphics-limited-tests'
    foreach ($Case in $Cases) {
        try {
            & $Case.body
            $Rows.Add(@{name=$Case.name;result='Passed'})
            $Log.Add('PASS '+$Case.name)
            Write-Host ('PASS '+$Case.name)
        } catch {
            $Rows.Add(@{name=$Case.name;result='Failed';error=$_.Exception.Message})
            throw
        }
    }
} catch {
    $Failure = $_.Exception.Message
    $Log.Add('FAIL '+$Phase+': '+$Failure)
    [IO.File]::WriteAllText((Join-Path $ResultDirectory 'first-error.log'), $Failure, (New-Object Text.UTF8Encoding($false)))
    throw
} finally {
    $Passed = @($Rows | Where-Object {$_.result -eq 'Passed'}).Count
    $Failed = @($Rows | Where-Object {$_.result -eq 'Failed'}).Count
    $Result = [ordered]@{
        result=$(if ($null -eq $Failure) {'Passed'} else {'Failed'})
        total=$Cases.Count;passed=$Passed;failed=$Failed;not_run=($Cases.Count-$Rows.Count)
        first_error=$Failure;failed_phase=$(if ($Failure) {$Phase} else {$null});cases=@($Rows.ToArray())
        scope='Actual complete production C# compiled against managed API doubles; nested graphics lifecycle executed, build wiring statically checked; not native Unity serialization.'
        unity_invocations=0;aab_builds=0;apk_builds=0;pixel_actions=0;signing_operations=0
        source_guard_test_invocations=0;automatic_retries=0
    }
    [IO.File]::WriteAllText((Join-Path $ResultDirectory 'result.json'), ($Result | ConvertTo-Json -Depth 8), (New-Object Text.UTF8Encoding($false)))
    [IO.File]::WriteAllLines((Join-Path $ResultDirectory 'release-graphics-tests.log'), $Log.ToArray(), (New-Object Text.UTF8Encoding($false)))
}
