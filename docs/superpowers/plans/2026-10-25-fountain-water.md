# Fountain Water Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans inline. No implementer delegation; one fresh whole-change reviewer at the end. Steps use checkbox tracking.

**Goal:** Preserve the frozen five-district field and add lightweight, depth-correct animated fountain water with native Before/After evidence.

**Architecture:** A pure UV-quad sampler supplies one additive UGUI child of the existing fountain. Existing painted fountain/water, world projection and Y sorting remain. Full TownQa gains one native capture case and10images, keeping existing movement/growth/save gates.

**Tech Stack:** Unity6000.6.4f1, C#9, UGUI, NUnit, .NET8 CPU verification, PowerShell5.1-compatible acceptance tools.

**Spec:** docs/superpowers/specs/2026-10-25-fountain-water-design.md

## Global Constraints

- 43a2764／run210356 field art, layout, walking geometry and density are frozen by latest user instruction. No Save/reward/Merge/character/BGM/book changes.
- One new Graphic,80quads／320vertices maximum,30Hz maximum new-water refresh; invisible or disabled stops the added animation. Clamp resumed dt to.1s.
- Fountain parent depth staysatY.285; no always-front actors, new Collider/Canvas/texture/Shader/ParticleSystem.
- Original approval hashes stay immutable. New narrowly authorized fountain inputs and their capture hash remain fail-closed; unknown scopes/unrelated files/mutations rejected.
- Native success is not API compile; Pixel/thermal/600second/fountain adoption remain unconfirmed until actual evidence. No2.5D/Merge/main before approval.

## Review Focus

- Water outside the crop or shifted after camera/viewport change: test bounded UV/projected mesh and native pixel changes confined to the fountain.
- Front hero hidden or far hero over water: child hierarchy/raycast assertions plus unchanged front/far hero/NPC depth checks.
- Hidden fountain or pause producing unnecessary updates: native phase unchanged offscreen/disabled and clamped finite elapsed; pure sampler has no per-frame allocations.
- Effect pair changes save/NPC/camera/UI: paired captures at one instant with same state/writes/positions, only new Graphic toggled.
- Old approvals or capture minimums accidentally bypassed: exact source/freeze binding and reject missing frames/metadata/adoption claims/unrelated source changes.

### Task1: Freeze acknowledgement and bounded water layer

Files: new TownFountainWaterLayout.cs／TownFountainWaterGraphic.cs with.meta; TownContinuousWorldView.cs binding only; Tests/Presentation/FountainWaterTests.cs and csproj; existing native TownMotionPresentationTests.cs.
Interfaces: Layout.QuadCount=80; Quad(int,float) returns(int Kind,TownPoint A,B,C,D,float Alpha), kindsflow0/splash1/ripple2/glint3; Graphic.Bind(TownContinuousWorldView),AdvanceWater(float),NotifyViewportChanged(),Phase. Existing World.Local and fountain projection are consumed unchanged.

- [x] CPU tests first: all4categories animate, bounded finite sourceUV/alpha, max320vertices, periodic finite sampling, no allocations after warmup, invalid inputs rejected. RED5 on missing model.
- [x] Add pure cached sampler and one Graphic; wire fountain child and camera-only refresh. Keep original Ambient and all depth/geometry untouched.
- [x] Extend native existing water case for mesh/raycast/child/depth, paused/hidden phase and viewport re-entry. FocusedGREEN5 and six API builds passed; native execution remains pending.

### Task2: Native comparison and narrow source gates

Files: ExperienceCaptureTests.cs; build-android.ps1／central-review.ps1／visual-lock.ps1 and related PS tests; new field-freeze and fountain-candidate records; stage-acceptance selects candidate without overwriting original approval.
Interfaces: FountainWaterBeforeAfterPreservesFieldAndCharacterDepth() UnityTest;33-fountain-{overview,front,behind}-{before,after}.png plus33-fountain-phase-00..03.png; fountain-water-comparison.json records3pairs/4phases/state and pixel bounds/new320vertices, Pixel/adoptedfalse.

- [x] Add new native capture case using existing portrait camera/capture helpers; toggle only new layer, compare same-state pixels, show4time phases and retain original capture filter for old district modes.
- [x] Require same-run154PNG／225Play and fountain metadata forTownQa; preserve144Edit/full suite/failed0skip0 and existing actual-delivery-growth evidence. Package comparison HTML/metadata with existing ZIPs.
- [x] Write user field-freeze receipt (not blanket device-pass), bind exact new files/scope/capture and hashes. Existing/frozen/CRLF checks and hostile cases remain strict. Relevant14PS fixtures passed after fixture-only missing-reference repair.

### Task3: Regression, independent review and handoff

- [x] Run full CPU340/340, six API assemblies0warnings/errors and relevant14PS checks. Read results; no new native/device claim.
- [x] One fresh-context whole-change review: Critical0／Important0／Minor0. Preserve all declared device-only limitations.
- [x] Commit/push feature only. Give one Windows fullTownQa command for actual native BeforeAfter/newAPK/SHA. No further stage before fountain approval.
- [ ] Receive actual native artifacts, then matching-APK Pixel visual/depth/load acceptance. Fountain adoption only after user/device result.

## Ledger

Start2834cb7 clean feature checkout feat/daily-world-pass-20261016, same tested product inputs as43a2764. Existing feature isolation is retained; no main work or redundant checkout. Baseline native receipt has144Edit／224Play allPassed and144verifiedPNG. New work is fountain only.

Ruling: latest user explicitly freezes current field candidate and authorizes fountain stage. Treat older field-complete-before-fountain stop as superseded, but do not fabricate missing Pixel heat/10minute/growth/restart proof. Carry remaining5localPolish items, not a field redesign.

Ruling: use the additive Graphic approach as an implementation choice under the user's detailed bounded instruction; retain existing rings and make honest same-state OFF/ON comparisons. No per-file or extra design approval interruption. The plan itself is the persistent inline ledger; cloud skill script paths are not local executables.

Pre-flight: Task1 sampler/Graphic signatures match Task2 capture consumers; Task2 added-path list must also be included in central fingerprint and omitted only for historical reconstruction. Existing old capture method filter is preserved so district-only review counts do not change.

Verification: CPU focused missing-modelRED5→GREEN5; wholeCPU340/340 failed0skip0,9m48s. SixAPI DLLs0warnings/errors; declaredNative144Edit／225Play only, not native execution. Android capture gate initially failed because old function ignored RequireFountain; new missing/pair/phase/pixel/adoption guards pass. Old committed source gate rejects new scope (actualRED), new exact17bindings pass. Original354／JPEG15 and historicaldfb2cbc8… preserved; current5fb43abb… also passes417fileCRLF fixture. All14PS fixtures green. Five-area-review fixture initially lacked the newfreeze/accepted-reference files; copied those2inputs only, watched RED→GREEN, no production gate relaxation. No current Unity/APK/Pixel execution.

Whole-change reviewer fountain_water_final_review_oct25 returned0Critical/Important/Minor. No product fix requested, no second review. One later PS fixture-only input-copy repair is verified by that fixture, not a reason to re-dispatch review.

Final: Ruling: native water appearance/stream alignment/sparkle legibility and changed-pixel proof were declined — require new realUnity3pairs＋4phases before visual judgment, not generated mockups — cost if wrong is a bounded water correction, never scenery simplification.
Final: Ruling: live front/far body occlusion/NPC readability declined — preserve inheritedY.285 and require actual movement on Pixel, retaining allowed localPolish — cost if wrong is held fountain approval and a local depth correction.
Final: Ruling: actual native144/225／154images declined — API compatibility and CPU tests are not substitutes; hand off one fullWindows run without retrying known unavailable cloudlicense — cost if wrong is diagnosis from same-run XML/log.
Final: Ruling: newAPK identity/deviceGC/framepacing/heat/600seconds/growth/restart declined — require same-source build/SHA and actualPixel; Core/save/rewards untouched and existing fullnative checks remain — cost if wrong is fountain not accepted, no later stage.
Final: Ruling: user adoption/nextstage declined — latest instruction freezes only currentfieldcandidate and permits fountain, not2.5D/Merge/main — cost if wrong is premature scope expansion, prevented by recorded false approval flags.
