# North contact candidate — 2026-10-27

Status: implemented; source/CPU/API verified; native Unity rendering, APK, perceived side-walk readability, and Pixel confirmation pending. No commit, index operation, or agent spawn was performed.

## Owned files

- Assets/Mofumachi/Presentation/TownNorthMasterLayout.cs: append public pure TownNorthEntryBlendLayout; existing North layout/dressing/constants untouched.
- Assets/Mofumachi/Presentation/TownNorthGroundGraphic.cs: wire one public TownNorthEntryBlendGraphic child into Bind/BindPreview/SetPreview/Refresh. Original North opaque quad and court mesh retain their original four vertices, textures, and coordinates.
- Tests/Presentation/NorthEntryBlendTests.cs: six dedicated CPU behavior tests.
- Assets/Mofumachi/Tests/PlayMode/NorthEntryBlendReviewTests.cs and .meta: three dedicated native tests prepared and API-compiled, not executed in Unity.

## Cause and bounded rendering change

The later opaque North quad begins at y=1 and overwrites Central's existing y=1..1.23 overlap. Its shared West painting has a wide bottom flare (approximately .82 world units), while Central's contact pavement is approximately .22 wide. The court at y=1.765..1.975 is unrelated.

The public child is **TownNorthEntryBlendGraphic**, GameObject **North central road join**. Its inherited enabled property is sufficient for isolated OFF/ON comparisons; there is no product toggle or production diagnostic UI. Both geometry and alpha are bounded to x[-.60,.60], y[1,1.32]. It reuses the already accepted Central resource through Resources.Load, without creating another texture, material, collider, navigation surface, scenery placement or actor.

UV sampling is TownCentralMasterLayout.TexturePoint((x, 1-.75*(y-1))). At y=1 the contact uses exact Central texels and alpha1. By y=1.32 it samples source y=.76, where the original curbs/pavement are approximately .31 wide, matching the native North neck. Central's mesh/resource remains unchanged.

Road support half-width decreases through world (y,width): (1,.48), (1.05,.42), (1.10,.31), (1.15,.26), (1.23,.23), (1.32,.20). The edge has a .035 alpha feather. Garden-only contact alpha finishes after .045..057 world units with a gentle x-dependent contour; it cannot repaint the existing North banks above y1.07 outside the road support. Paving alpha remains opaque through y1.28 and fades smoothly to zero at y1.32. Two broad side-lane clearances preserve original North paving at |x|=.27..33 above the .012-world exact-contact feather; they taper away after y1.10, where the road mask already withdraws.

The cached mesh has 35 columns x17 rows =595 vertices and1088 triangles, one added graphic/batch. Columns register the side-lane knots and clip to the actual support boundary, so the interpolated mesh preserves the free lanes and zero-alpha outer-bank triangles cannot cover the unchanged banks. Collapsed edge columns form harmless zero-area triangles at narrower rows; the cached vertex count remains595. Camera/preview refresh only reprojects the cache. Pixel/frame cost is unmeasured.

No navigation/obstacle data was modified. The existing three broad seam crossings x=-.30/0/+.30 remain legal. Actual native images and live side-lane walking still need review to establish that new low ground curbs/texture transitions read naturally and preserve perceived walkability; CPU legality does not prove this visually.

## Verification performed

1. Dedicated CPU tests written before implementation. A first runner invocation selected0 cases because the old shared runner resolved its stale copied test assembly; it was rejected as evidence. A dedicated NUnit-only runner was then built under north-runner, loading the actual newly built assembly. Genuine RED:6/6 failed specifically because TownNorthEntryBlendLayout was missing (north-red.xml).
2. After implementation:6/6 dedicated tests passed, failed0/skipped0/inconclusive0 (north-green.xml).
3. Initial focused regression:72/72 passed, failed0/skipped0/inconclusive0,36.483s (north-regression.xml). Fixtures: NorthEntryBlend6; NorthMaster15; NorthCameraBoundary4; TownWalkableArea12; TownFiveDistrict13; TownSceneryLayout6; TownBoundaryGarden5; CurvedBoundary5; SouthEntryBlend5; FiveAreaRoute1.
4. An additional actual-triangle interpolation assertion exposed a CPU mesh-alpha leak of.00089016 at one side-lane sample although the analytic function returned0 (north-mesh-red.xml:5passed/1failed). The final cache uses explicit crossing-lane X knots clamped to the road edge. Strengthened6/6 tests then passed (north-mesh-green.xml); final72-case regression is recorded separately below. This is a numerical source check, not an observed Unity pixel.
5. Current Presentation CPU assembly built with0 warnings/errors. Direct Unity API builds of Mofumachi.Presentation and Mofumachi.PlayModeTests succeeded with0 warnings/errors. This verifies API compilation only, not Unity rendering/execution.
6. git diff --check passed.
7.14 protected Central/geometry/scenery/boundary/fountain/art inputs compared byte-for-byte against ff7215c and matched (north-protected-inputs.txt). Parent owns the complete freeze/gate and full Presentation/Core/6-assembly checks.

Dedicated native tests prepared: (a) live mesh projection after Pan/Restore and OFF/ON invariants for residents, placements/active scenery, camera, StateCodec/Store, water phase/elapsed; (b) repeated preview binds/center updates reuse exactly one join/court and project the same world mesh; (c) actual broad side crossings both directions at Lv1/3/6. Existing original North four-vertex/court contracts remain intact.

## Handoff for actual evidence

Historical actual Before exists at References/Review/fountain-final-2026-10-27-candidate/native/09-connected-path-north.png, source bb3fb82. Preserve its original identity. A true ff7215c native run before implementation is stronger if available on Windows. Candidate same-state child OFF/ON images are explicitly reconstructed causal baseline versus candidate, not historical prior-runtime captures. Parent owns capture/gate implementation.

Use camera(0,.85)/(0,1)/(0,1.15), Lv1/3/6 with no advances between toggles. Require changed pixels inside projected x[-.6,.6],y[1,1.32] plus a small raster guard; require water/state/actors/camera unchanged. Preserve side bank pixels above1.07 outside the support. Inspect broad side lanes, curved curbs, paving scale/feather, every decoration and official faces. Then verify actual Central↔North traversal/return and ±.30 side crossings. On the right, the pre-existing flower island centered(.22,1.35), radii(.12,.13), means the longer .95→1.4 route must use navigation rather than asserting a straight segment.

This cloud has no licensed Unity/USB device route. No synthetic image, diagram, API result or CPU count is represented as a Unity frame or Pixel proof. The candidate remains unadopted pending actual evidence.

## Final owned source identity

[
  {
    "path": "Assets/Mofumachi/Presentation/TownNorthMasterLayout.cs",
    "sha256": "e4b3017ce094570fe1a37123edd9585b673da9f69270ecd4bcc7bb0bea9502f8",
    "bytes": 9630
  },
  {
    "path": "Assets/Mofumachi/Presentation/TownNorthGroundGraphic.cs",
    "sha256": "e6d98497100b11199e0712b6da7ec60184f53c871d5f56f7978e42db2c6d89ae",
    "bytes": 6796
  },
  {
    "path": "Tests/Presentation/NorthEntryBlendTests.cs",
    "sha256": "82a94bba8d6a5f9e7cf2684bd0cfc2db9d455f43749a2cff105838b33786988d",
    "bytes": 7604
  },
  {
    "path": "Assets/Mofumachi/Tests/PlayMode/NorthEntryBlendReviewTests.cs",
    "sha256": "f26d65d94df3576d13ba353245fc9017efb9ed5a340222973767a1447b5ff522",
    "bytes": 7203
  },
  {
    "path": "Assets/Mofumachi/Tests/PlayMode/NorthEntryBlendReviewTests.cs.meta",
    "sha256": "5ef978647b33615f5e03ff1a1ac655415d6c80e7c00466a560917b0fa8fd26a9",
    "bytes": 60
  }
]

Final verification after all mesh changes: north-regression-final.xml reports72/72 passed, failed0/skipped0/inconclusive0, duration39.809s. Fresh Presentation and PlayMode Unity API builds each succeeded with0 warnings/errors. Final owned source hashes were rechecked against north-final-files.json and matched. Owned source files are frozen for parent integration; no further edits are planned.
