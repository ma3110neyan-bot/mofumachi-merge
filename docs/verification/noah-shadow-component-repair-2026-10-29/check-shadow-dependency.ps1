param([string]$PresentationAssembly)
$ErrorActionPreference='Stop'
$UnityData='/workspace/toolchains/unity/6000.6.4f1/Editor/Data'
Add-Type -Path (Join-Path $UnityData 'Tools/BuildPipeline/Compilation/Unity.ILPP.Runner/Mono.Cecil.dll')
$Resolver=[Mono.Cecil.DefaultAssemblyResolver]::new()
foreach($Directory in @((Split-Path $PresentationAssembly),(Join-Path $UnityData 'Managed/UnityEngine'),(Join-Path $UnityData 'Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.2d-cross-platform-2d-7.0.0/ScriptAssemblies'))){$Resolver.AddSearchDirectory($Directory)}
$Parameters=[Mono.Cecil.ReaderParameters]::new();$Parameters.AssemblyResolver=$Resolver
$Assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($PresentationAssembly,$Parameters)
try {
    $Shadow=@($Assembly.MainModule.Types | Where-Object FullName -eq 'Mofumachi.Presentation.NoahGroundShadowGraphic')[0]
    $Requirements=[System.Collections.Generic.HashSet[string]]::new()
    for($Type=$Shadow;$null -ne $Type -and $Type.FullName -ne 'System.Object';$Type=$Type.BaseType.Resolve()) {
        foreach($Attribute in @($Type.CustomAttributes | Where-Object {$_.AttributeType.FullName -eq 'UnityEngine.RequireComponent'})) {
            foreach($Argument in $Attribute.ConstructorArguments){if($null -ne $Argument.Value){$null=$Requirements.Add($Argument.Value.FullName)}}
        }
    }
    Write-Output ('Noah shadow declared component dependencies (real compiled Unity API metadata): '+(($Requirements | Sort-Object)-join ', '))
    if(-not $Requirements.Contains('UnityEngine.CanvasRenderer')){Write-Output 'FAIL: Noah shadow does not require CanvasRenderer; RectTransform alone cannot render or safely unclip the shadow.';exit 1}
    Write-Output 'PASS: Noah shadow declares CanvasRenderer through its actual dependency chain. Metadata check only; Unity object creation/lifecycle still pending.'
} finally {$Assembly.Dispose();$Resolver.Dispose()}
