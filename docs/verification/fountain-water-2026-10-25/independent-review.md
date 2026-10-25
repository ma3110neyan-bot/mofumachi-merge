# Independent whole-change review — fountain water, 2026-10-25

Reviewed the uncommitted change in `/workspace/mofumachi-merge` against HEAD/base `2834cb77b7b94bce0c260a37f8088c42c276a451`, the fountain-water spec/plan and its Ledger rulings. This was the single fresh-context whole-change review. Repository, index and HEAD were not changed; this report is the only reviewer-written artifact. No agents were dispatched.

## Verdict

**Ready for the Windows native full TownQa evidence-gathering handoff, subject to completion of the coordinator's currently running regression checks.** No Critical, Important or Minor code-review findings were identified. Subsequent matching-APK Pixel evaluation is appropriate only after native results and images are received and checked. This is not fountain adoption, main/merge approval, or confirmation of native/device success.

The latest explicit user authorization freezes the current five-district graphical base candidate and permits this fountain-only stage. It supersedes the older instruction to hold the fountain stage for all Pixel checks. It does not establish missing device measurements or authorize 2.5D, Merge or main.

## Findings

- Critical: 0.
- Important: 0.
- Minor: 0.

## Reviewed conditions and evidence

1. **Projection, crop and camera movement.** `TownFountainWaterGraphic.Local` uses the same fountain width, feet and height mapping as `TownFountainGraphic.Location`, followed by the same `World.Local` transformation. The three projected basis points in `OnPopulateMesh` preserve that affine mapping. The WorldView change adds the child and refresh notification without modifying the camera, geometry or original fountain/ambient implementation. The cached sampler remains bounded within its stated source UV window. Viewport changes dirty the layer; offscreen population clears it and re-entry rebuilds it. This supports correctness of the mapping, not a subjective judgment of the final rendered artwork.

2. **Hero/NPC depth and input.** The new Graphic is a child of the existing fountain. Existing shared resident/scenery sorting still places the fountain at Y .285 and orders residents by world feet. Consequently the new water inherits the same front/far ordering for both hero and NPCs, without an always-front override or a separate sorting surface. `raycastTarget` is false. The added capture walks the hero to front and behind positions and checks the parent ordering; existing depth and real-walking capture assertions remain present.

3. **Lifecycle and cost.** The addition exits for disabled/inactive state, blocked production simulation, offscreen projection, or invalid elapsed time. The production simulation predicate includes Home activity, modal state and application pause. It clamps delta to .1 seconds, accumulates at the 30 Hz interval, and emits 80 quads / 320 vertices. Sampler storage is cached and its return values are structs/value tuples; the Graphic uses no frame arrays, LINQ or delegate conversions. The focused CPU log reports 5 passed, 0 failed, 0 skipped, including allocation checks for the sampler. Native lifecycle assertions cover disabled, offscreen, re-entry and application pause. Device profiling of the entire Unity UI rebuild remains pending.

4. **Trustworthy same-state pairs.** The new native capture warms the portrait resources before taking its state snapshot, then performs each OFF/ON comparison synchronously without yielding or advancing either water phase. Before disables only the new layer, retaining the original fountain and ambient water. Assertions retain resident positions, camera center, serialized state, save-write count and both phases. Changed pixels must exist and remain within the projected fountain rectangle. Three pairs and four distinct animation phases are produced. Existing capture helpers refresh presentation without manually stepping the world. The comparison is honestly labelled as a new-layer OFF/ON fixture, not reconstruction of the old APK.

5. **Frozen inputs and complete evidence.** The original approved baseline files are unchanged in this diff. The new scope binds the original approval, accepted field-candidate record, user-freeze record, exact additional fountain files and capture input. Non-WorldView prior scene/navigation/growth inputs remain fixed to the accepted hashes. Current fingerprints include the added fountain files, while historical reconstruction omits them only for the original digest. Existing district-only capture method filtering is unchanged. Full TownQa raises PlayMode minimum 224 to 225 and required PNG minimum 144 to 154 while keeping EditMode 144 and existing boundary, field and actual-growth requirements. Missing fountain images/HTML, invalid pair metadata, repeated phases and false device/adoption claims are rejected; metadata and HTML are included in review archives.

## Verification inspected

- `git diff --check` completed successfully.
- HEAD was confirmed as the supplied base revision.
- `cpu-green.log`: focused fountain tests 5/5 passed, 0 skipped.
- `api-final.log`: six assembly compatibility builds succeeded, each with 0 warnings and 0 errors.
- `android-tools-green.log`: native capture and build-gate fixtures, mock Android commands and hostile cases passed. The log explicitly identifies synthetic XML, mock commands and no device execution; its mock install messages were not treated as Pixel evidence.
- `town-boundary-lock.log`: missing/mutated/unrelated input and unknown-scope rejection checks passed.
- `source-gate-green.log`: exact fountain scope and historical identity checks passed.
- The full CPU log was still in progress when inspected; final full-regression completion is the coordinator's responsibility before handoff.

## Behaviors deliberately not judged

- Final native water appearance, exact painted-stream alignment, sparkle readability and actual changed-pixel counts: no current native render output was available. Source projection and bounds are consistent, but API builds and CPU samples cannot establish the rendered result.
- Live front/far body occlusion and NPC readability during movement: hierarchy and test coverage support it; current native footage/device behavior was unavailable. Existing historical screenshots do not establish the new effect's live appearance.
- Actual Unity PlayMode success and generation of the new 154-image set: native execution has not occurred in this review. No cloud-license retry was attempted.
- New APK generation, binary/source identity, Pixel load, temperature, frame pacing, runtime GC, 600-second play, initial growth and restart preservation: no matching current-device evidence was supplied. Sampler allocation tests do not substitute for whole-device profiling.
- Full user acceptance of the fountain or later stages: expressly pending. The supplied PDF/field-freeze receipt was treated as limited historical field evidence and user authorization, not proof of these missing measurements.

No chat images were embedded. No repository mutations, commits, pushes, native runs or device operations were performed by the reviewer.
