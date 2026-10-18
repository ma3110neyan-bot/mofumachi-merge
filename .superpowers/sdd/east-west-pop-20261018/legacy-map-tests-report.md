# Legacy map test integration

Scope: only `Tests/Presentation/SeamlessTownTests.cs`, `TownWalkableAreaTests.cs`, `TownWorldTests.cs`, and `TownWorldRegressionTests.cs`. `TownWorldSurfaceTests.cs` was inspected and remains unchanged: its legacy material/ribbon helper guarantees still apply. No production, Core, saves, map artwork or parent-owned EastWestTownTests edits were made by this worker.

Viewed the actual new `Assets/Mofumachi/Resources/Mofumachi/TownSeamlessMap.png` and read the actual world map, floor polygons, island ellipses, navigation, camera/projection, actor motion, growth placements and UI mask. The old hardcoded portrait-map transform and coordinates were the root cause of the legacy fixture failures.

## Updated guarantees
- Map samples now use the active UV projection. Specific walkable points cover both sides and above/below the fountain, cafe/florist/quest doors, the lower loops and southern plaza. Specific blockers cover the water, all three visible buildings and the four principal garden islands.
- A cafe-to-quest shortcut has on-floor endpoints, is directly blocked by the actual cafe, and still has a valid route around the upper garden gate.
- Fountain routes check valid endpoints, multiple segments, every segment's floor safety and exact arrival. Both upper and lower connections are exercised. Every stable destination still routes at levels 0 and 6.
- The former obsolete connector-gap regression now grazes the north edge of the actual fountain: UV center (.52,.49), world Y radius .16, Y=.57998, endpoints X=center-.05 / center+.07. Both endpoints are floor, the sub-.01-world-unit interior crossing is blocked, and CanTravel rejects it.
- Free taps preserve their actual floor point; building finger input clamps to floor; release stops motion. Avoidance cases run west cafe to east florist, reverse, and north-to-lower plaza around the fountain. All source and destination points are asserted on floor before use.
- Opposing residents pass on the actual bench-to-bench plaza paving without moving the stationary middle resident. No completion/separation/budget/pause assertions were weakened.
- The existing vertical ToWorld/ToLocal legacy transform/roundtrip assertions are preserved. Active-world walking tests use west/central/east semantics, and central copy is now 中央広場. The road tap explicitly crosses the horizontal central-to-west boundary.
- Early flowers and the first level-three lamp are tested at 360×592/640/808, for regional look and +.04-world-Y arrival-follow cameras. Full prop bounds use world width and the actual atlas aspects (flowers 620/327; lamp 161/351). The field mask uses safe bottom/top 24/32 plus controls bottom/top 62/80, with an 8-unit inner margin; the former height-376 and hardcoded offset assumptions are removed.

## Evidence
Commands use `DOTNET_CLI_HOME=/workspace/runtime/dotnet NUGET_PACKAGES=/workspace/toolchains/nuget` and `/workspace/toolchains/unity/6000.6.4f1/Editor/Data/DotNetSdk/dotnet`. The scratch `/tmp/mofumachi-flat-merge-full-presentation/Mofumachi.Visual.Tests.csproj` links all actual pure production and Presentation test sources with isolated intermediate/output directories.

- Baseline full suite before fixture updates: **13 failed, 121 passed, 0 skipped (134 total)**. Failures were obsolete walk/obstacle/map samples, old central name and old growth placement/projection assumptions.
- Updated owned suites plus unchanged surface suite, excluding growth visibility while parent authored growth: **44 passed, 0 failed/skipped**.
- Mutation check in `/tmp/mofumachi-legacy-map-mutation/LegacyMap.Mutation.Tests.csproj`: a temporary copy disabled island containment; **all 3 selected tests failed as intended** (painted blockers, thin fountain crossing, fountain route). Repository production was untouched. This confirms the replacement coordinates still catch unsafe navigation.
- After parent authored new growth positions/world-size rendering, growth visibility: **2 passed, 1 failed, 0 skipped**. The remaining failure is the central lamp at the tall viewport, described below.
- Latest full suite at this report's creation: **1 failed, 134 passed, 0 skipped (135 total)**. The only failure is `FirstFlowersAndLevelThreeLampsAreVisibleFromTheRegionalPhoneView(Central)`; all other tests, including the parent-owned east/west authoring/growth guarantees, passed.
- `git diff --check` passed.

## Concrete product concern reported to parent
The central first lamp is at UV (.38,.66), world width .07. At height 808, scale=404 and the camera looks at square UV (.52,.68). The lamp center projects to X=-169.68; half width=14.14 gives left edge **-183.82**, outside the actual field mask's **-180** edge (and the test's -172 inner margin). This is real partial clipping, not a stale vertical-map expectation. Moving its anchor to UV (.40,.66) would fit and remains off the walkable floor in the left garden island. Parent owns the production correction; this worker intentionally leaves the regression active.

Native Unity and Pixel evidence remain outside this pure-test task. No native render, performance, APK or Pixel acceptance result is claimed.
