# Individual Head / Body Calibration Plan

> **For agentic workers:** Execute inline with superpowers:executing-plans; one fresh final review. User waived intermediate approval questions.

**Goal:** Keep all six characters' head/body relationship stable through the user's ten-step walk/turn/stop/restart sequence, using individual directional calibration and one common body-local resolver.

**Architecture:** Keep the existing one-mesh Graphic inside CharacterRoot/VisualRoot. Its body frame is the common parent. Head translation comes from a constant per-character/per-view body-local pivot; head angle and every non-head transform remain unchanged. Do not apply static head translation through the independently nodding head rotation. Preserve original art, UV, scale/pivots and normal front single sprite. Tune static head pivots against each original collar, including visible collar openings, not a shared rectangular origin or global opaque extremum.

**Tech Stack:** Unity6000.6.4f1 C#, PowerShell, existing CLR renderer probe/source-mask diagnostic.

**Spec:** User's latest 12-section final instruction and inline “キャラクター2.5D正解見本” image. The image is a placement reference, not an asset-edit target; original back designs remain unchanged.

## Constraints

- Head/body positions only; no source pixels, expression, motion/speed, direction hysteresis, reflection, shadow, passing/conversation, town/UI/Merge/Save/audio/font changes.
- No global head offset copied across six characters. Front remains the complete original sprite; no fabricated front split.
- Related head-only checks during work. Full-suite checks only after all six have current native visual PASS, once. No premature freeze or Merge work.
- Preserve old manifests/evidence. New calibration authorization is exact and additive, never general art unlock.

## Review focus

- A zero mesh-formula error is not absence of a visible collar gap; independently inspect original alpha/collar opening geometry.
- Per-view offset changes must occur in the same pose update for all parts, including blink and rapid turns.
- Head parent-frame position must remain constant while head nod angle changes; do not move the head offset through head rotation.
- Existing body, hands, legs, tail, UV, scale and head angles must remain identical.
- Native sequence must be isolated to characters, record each ID separately, and not run town/Merge/Save/BGM/full-screen checks.

## Task1 — common root and individual calibration

**Files:** new `Assets/Mofumachi/Presentation/CharacterHeadBodyCalibration.cs`/meta; two existing Graphic head branches; focused probe/expected calibration fixture.

- [ ] Reproduce phase-dependent parent-frame drift and per-collar separation using actual renderer source; record RED.
- [ ] Add common `Resolve(id, direction, bodyFrame, headAngle)` and per-ID/direction fixed pivots. Keep head angle and all other part transforms unchanged.
- [ ] Calibrate directional values individually against collar anatomy; record original masks/measurement windows and compare source renders without repainted pixels.
- [ ] Run the exact ten-step sequence per character at fixed60Hz, plus head-angle/blink boundaries; require fixed body-local pivot and unchanged non-head geometry/UV/size/angle.

## Task2 — native proof and guarded delivery

**Files:** character-only native sequence/crop capture, current head capture script/proof verifier, scoped calibration guard/metadata, documentation.

- [ ] Native sequence: Front3s → Left3s → Right3s → Back3s → Front → stop → restart, each ID separately; record root/attachment on every frame and small character-only images/continuous viewer.
- [ ] No scene/town/Merge/Save/audio acceptance checks. Reject old432/1440 proofs and report distinct per-character technical/visual states.
- [ ] Verify only affected APIs/scope guards and source immutability; independent review; normal already-authorized feature push.
- [ ] Leave native/visual acceptance pending when unavailable. Never substitute CLR/source preview for Unity/Pixel PASS. Merge remains gated.

## Investigation ledger

Base9be23ab, clean feature checkout. Same-mesh hierarchy already prevents independent WorldPosition/Animator/Tween updates. Five-character head path still applies material offset via pose.Head.Child, so nod rotation rotates its translation. Static placement also leaves visible collar openings. Source mask diagnostics show different directional gap patterns: Noah right body-pivot gap1.375..1.625 world units; Pom left .875..1.0625; Fio right .5625...75; Lulu collar opening needs visual landmark calibration despite near-zero alpha-tip distance. Momo/Miel already overlap due hair and must not receive the same blanket downward shift. Original image alpha components are connected, so this is not a stray disconnected pixel cause.

Ruling: use per-character directional pivot data and the existing shared body frame; do not add separate animated world roots or alter direction policy. Fixed0 offsets for untouched bodies/front single sprites are intentional, not a copied head calibration. The user's detailed no-pause instruction supersedes skill approval menus; related-only test instruction supersedes generic full-suite defaults.

## Execution ledger (2026-11-02)

- Task1 implementation/CLR complete, native acceptance pending: baseline9be renderer reproduced5232 head-vertex mismatches over1774 frames for Noah; candidate all6 each1774frames0 mismatches, preserving non-head/UV/head angle and edge vectors. Original alpha-to-actual-quad checks each1308 split frames0 collar/garment failures. No runtime image fitting or art edits.
- Task2 native route prepared, not executed: new exact1 isolated-Canvas method with six individual1054-frame sequences, each3s directional walk, stop/restart and rapid turns;972 small JPEGs and continuous viewer. Source/reference/packed identity, individual counts and failure flags guarded. Native/visual/full/Pixel acceptance remains false or pending.
- Validation:5 related PowerShell scripts passed. Actual26-master/552-frozen guard path and final53+limited overrides passed. Presentation/Editor/PlayModeTests API builds0warnings0errors.8 source-preview JPEGs fully decoded and SHA checked. No full suites, APK, installation, town/Save/BGM/Merge acceptance performed.
- Final fresh review found3 important capture-only defects: moving Root during stop,12-frame image sampling aliasing rapid4-frame turns, header-only fake JPEG acceptance. Each reproduced RED. One fix pass freezes/records Root, captures all rapid frames, rejects scanless JPEG plus enforces full Unity and Windows-GDI decode. Related proof mutation tests GREEN; PlayModeTests API build GREEN. Windows GDI/native graphics are not executable in this cloud and are explicitly left unverified.
- Scope decision: preserve existing single Graphic/common Root rather than create separate GameObjects. Front original single sprite remains inseparable. Costs/limits: native timing, atlas compression and visible transitions must still be judged on current native output; zero CLR errors do not imply completion. User's no-intermediate-approval and related-only testing rules override generic skill confirmation/full-suite defaults. GitHub feature push remains explicitly authorized.
- No formal completion or Merge task is authorized by the current evidence. All six native visual results PENDING; don't convert these to PASS.
