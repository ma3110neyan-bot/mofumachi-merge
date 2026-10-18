# Flat Merge implementation report

Read the October 18 spec/Task 3 and viewed `a_colorful_promotional_collage_ui_concept_board.png`. The September 22 flat reference supersedes the former perspective presentation.

## Product changes
- `MergePerspective.Cell` now centers an aligned 5×6 grid of equal squares with 4-unit gutters and >=48-unit input rectangles at all supported dimensions (minimum 256×308). Row-major indexes and public signatures remain unchanged.
- `ArtScale` returns 1 for all thirty cells, so existing item and selected-item presentation uses a constant size across rows.
- `PerspectiveBoardGraphic` draws a thin procedural ivory panel with a warm rim; it no longer projects the old ornate texture onto a trapezoid or draws a lower platform. The existing component and hierarchy names remain stable. The old resource is retained unchanged.
- `MergeCellSurfaceGraphic` draws rounded squares with subtle edging, a white highlight, and the existing state tints. Decorative graphics remain non-raycast targets. The invisible full cell input rectangles remain intact.
- `MergeBoardView`, `QuestMergeScreens`, merge/drag/selection/effect timing, Core and saves are unchanged. Existing selected floating art, compatible glow, pickup, gathering, flash and landing use their existing logic.
- Updated focused native expectations in ExperienceMergeTests, MergePolishTests, QualityPassMergeTests and only Merge-specific methods in DailyWorldTests/MajorPolishTests. Added native geometry checks for rounded corners, aligned top edge, no lower platform and equal item dimensions in all six rows. Existing actual corner/center raycasts, pointer drags, selection, save-failure and animation cancellation tests remain.

## Verification
All commands used `DOTNET_CLI_HOME=/workspace/runtime/dotnet NUGET_PACKAGES=/workspace/toolchains/nuget` and `/workspace/toolchains/unity/6000.6.4f1/Editor/Data/DotNetSdk/dotnet`. Temporary projects link actual repository sources and have their own outputs to avoid concurrent-worker collisions.

1. RED: `dotnet test /tmp/mofumachi-flat-merge-tests/FlatMerge.Tests.csproj --nologo --verbosity minimal` — 4 failed, 21 passed, 0 skipped. Expected failures: old row scaling returned .82 and old cells were nonsquare.
2. GREEN: same focused project with `--no-restore` — 25 passed, 0 failed/skipped. Includes all Merge geometry and timing tests, supported minimum/portrait/wide dimensions, bounds/nonoverlap, row ordering, center/inset-corner hit roundtrips and constant item scale.
3. Broader pure presentation suite: `dotnet test /tmp/mofumachi-flat-merge-full-presentation/Mofumachi.Visual.Tests.csproj --nologo --verbosity minimal` — 129 passed, 0 failed/skipped (current shared sources at run time; parent will repeat after town integration).
4. Owned production geometry/graphic sources compiled against actual Unity 6000.6.4f1 managed references via `/tmp/mofumachi-flat-merge-compile/FlatMerge.Compile.csproj` — 0 warnings/errors.
5. All five modified native test source files plus the existing fixture compiled against actual Unity references and the existing compatible presentation assembly via `/tmp/mofumachi-flat-merge-native-compile/FlatMerge.Native.Compile.csproj` — 0 warnings/errors. This is source/API validation, not native execution.
6. `git diff --check` passed before committing.

## Remaining evidence
Native Unity PlayMode/mesh rendering/actual input tests are unexecuted: cloud Unity has no license. No native capture or Pixel result is claimed. Existing 01/02/03 Merge capture names automatically render this board on the next actual native run; parent owns the shared capture fixture and can add a specifically named flat-board frame. Native viewport captures and Pixel play/audio/save acceptance remain required for the bundled candidate.
