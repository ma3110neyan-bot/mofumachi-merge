# Noah Final Rig Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans inline. Do not repeat whole suites; user explicitly requires Noah-only checks.

**Goal:** A single final Noah-only comparison of an atlas-preserving weighted rig and the rejected f451068 renderer, with a reproducible Pixel handoff.

**Architecture:** Keep current classes byte-equivalent; append a pure prepared bone pose/skin helper to the existing Noah file. Opt-in rendering uses the same UV grid, manually projected depth and a floor shadow. Default product rendering remains f451068. One native capture method produces scoped comparisons; a separate build helper creates two clearly experimental APKs from the tested source.

**Tech Stack:** Unity6000.6.4f1, existing uGUI/atlas, pure C#, PowerShell7/Windows5-compatible tools, ADB.

**Spec:** docs/superpowers/specs/2026-10-29-noah-final-rig-design.md

## Global Constraints

- Noah only; no rollout, main, new animation timing, art rewrite, full3D, whole test suite or Save reset.
- Before=f451068 same-clock current renderer, not old2D. Defaults keep Before. Trial-only defines select After APK.
- Frozen town/fountain/North/book/music/Merge/Core stay unchanged. Bind every new trial input separately without changing historical approvals.
- Native/manual fixed samples and CPU geometry are not real-time/Pixel proof. No claims of static visual improvement before actual images.
- One final trial only; if insufficient, formally propose authored split/bone/directional assets instead of iterating this scheme.

## Review Focus

- Face including mouth/chin stays on one affine bone; no eye or smile registration drift.
- Weight seams do not fold or leave holes at extreme left/right, forward/back, stop, default/nonfinite inputs.
- Baseline switch restores exact f451068 vertices/UV/colors/root/shadow and leaves model/Save/other5 unchanged.
- Trial build mode cannot leak into normal APK or overwrite saves; APK identity/hash/mode must be explicit.
- Device samples can be incomplete or crash; preserve raw evidence and keep unknown metrics null.

### Task1: pure weighted rig

Files: append Assets/Mofumachi/Presentation/TownNoah25DTrial.cs; add Tests/Presentation/NoahFinalRigTests.cs.
Interfaces: TownNoahFinalRig.Pose(motion,headFacing,forward), Vertex(u,v,width,height,pose), Support(width,height,pose), Grounding; prepared struct caches transforms and preserves default safety.
- [ ] Write failing tests for rigid face, finite normalized weights, zero ground penetration, static separated projection, all-turn grid orientation and finite fallback.
- [ ] Run only NoahFinalRigTests; watch API absence RED. Implement prepared skinning with fixed 3/4 projection, original UV and planted foot translation.
- [ ] Run focused tests; fix actual failures only. Geometry tests do not assert aesthetic success.

### Task2: opt-in renderer and native evidence

Files: CharacterBodyGraphic.cs, TownActor.cs, existing ExperienceCaptureTests.cs; final trial record and existing bounded bindings.
Interfaces: TownActor.SetNoahFinalRigEnabled(bool), NoahFinalRigEnabled; body PoseNoahFinalRig; no Save/UI option.
- [ ] Add baseline restoration/Noah-only/UV invariants to the existing related native test; add protocol3 comparison branch to the existing one-method capture. Keep protocol2 intact.
- [ ] Render 7-region weighted rig with existing720 body vertices, same material/texture; single soft ellipse/contact Graphic remains grounded.
- [ ] API compile presentation/PlayMode against actual Unity DLLs; no engine execution claim. Validate immutable inputs. Native evidence remains pending user Windows.

### Task3: minimal capture/build/device handoff

Files: new final-trial PowerShell helper, read-only proof validation extension, trial-only Editor builder/meta, focused script tests, docs/verification.
Interfaces: one command runs exact1Noah native capture; -BuildApks additionally generates current/rig APKs only after strict same-source proof. Explicit compile define selects rig. Per-mode JSON includes source/fingerprint/package/hash, scope=experimental-not-release, fullQA=false/adopted=false.
- [ ] Write RED tests for exact filter, missing/failing proof rejection, mode/hash identity, save-preserving device commands and incomplete metrics.
- [ ] Implement source/frozen-input validation, bounded ZIPs, same-source two-APK builder and Pixel wrapper using existing surface/PSS/thermal collector plus crash evidence and manual tap/stutter feedback.
- [ ] Run only related script/CPU/API checks. Fresh final source review once; one fix pass for important findings, no full suites.
- [ ] Commit/push on existing feature and provide exact Windows command. Until returned native/device evidence, mark implemented/Unity pending/APK pending/Pixel pending and no adoption.

Self-review: all requested static/directional/movement/conversation/shadow/device outputs covered; full3D and new directional artwork deliberately excluded. Existing user scope authorizes method choice and inline implementation; no redundant approval pause.
