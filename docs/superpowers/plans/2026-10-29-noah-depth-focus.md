# Noah Depth Focus Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Implement inline, with one fresh whole-branch review at the end. Steps use checkbox syntax.

**Goal:** Make only Noah's grounding, part depth and facing visibly dimensional while preserving official art and accepted motion direction.
**Architecture:** Same atlas/720-vertex body and97-vertex shadow. Add a pure layered pose helper inside the existing Noah math file, retain source22f2831 calculation branches, and solve actual projected support feet. No new renderer/material/texture/particle system.
**Tech Stack:** C#9, Unity6000.6.4f1 UGUI, NUnit, PowerShell.
**Spec:** docs/superpowers/specs/2026-10-29-noah-depth-focus-design.md

## Global Constraints

No formal art/other-five/world/North/fountain/book/BGM/Merge/save/delivery changes. Noah only. Related minimal checks during iteration; full suites only after visible candidate readiness. No Pixel claims without measurement; no Fresh/uninstall/main. Existing feature checkout is isolated from main and retained for the user's Pull workflow.

## Review Focus

- Face/eyes must move rigidly and outer tail remain unwarped; projected mesh must not fold on reversal, greeting or front/back motion.
- One actual paw remains on floor after yaw/pitch/roll/breath; shadow follows navigation feet and remains readable above the name card.
- Current22f2831 Before must remain exact, distinct from historical1d/legacy2D.
- Actual public navigation and conversation stay enabled in synchronous capture; no frozen timestamps or model-only substitute.
- Capture camera/crop must include all geometry and all front/back phases; no source/Save/other-character or timing mismatch.

### Task1: Pure part-depth rig and planted projection

Files: TownNoah25DTrial.cs, Tests/Presentation/NoahDepthFocusTests.cs.
Interfaces: NoahDepthFocusPose wraps Noah25DTrialPose; TownNoahDepthFocus.Pose(base,stride,walking,headFacing,forward,reactionRemaining), Vertex(u,v,width,height,pose), Project(vertex,pose,sx,sy,sz), Support(width,height,pose,sx,sy,sz), Grounding(bool).

- [x] Add failing focused tests: missing part-depth API; face rigidity/tail identity, stronger head-vs-torso parallax, phase-separated limbs, settled forward tilt, real support after3D transforms, projected grid orientation, bounded neutral light and readable grounding profile.
- [x] Implement helper without changing the existing accepted-direction/Before math methods; run only Noah old/new math tests via in-process NUnit.

### Task2: Noah-only body/shadow integration

Files: CharacterBodyGraphic.cs, TownActor.cs, Noah25DTrialPresentationTests.cs.
Interfaces: TownActor.NoahDepthFocusEnabled/SetNoahDepthFocusEnabled(bool), body.PoseNoahDepthFocus(pose,blink); NoahGroundShadowGraphic.ConfigureGrounding(bool) used by production and capture.

- [x] Track presentation-only head facing and vertical travel from real resident deltas; reset/settle safely. Preserve other-five branches and world-foot sorting.
- [x] Select the new rig only for Noah; preserve exact current22 body/root/shadow when focus OFF. Keep same original UVs and blink registration.
- [x] Verify focused component/mesh/support/Before assertions and two API assemblies; no full native suite.

### Task3: Continuous depth evidence and handoff

Files: ExperienceCaptureTests.cs, noah-reprototype-review.ps1, scripts/tests/noah-reprototype.tests.ps1, current candidate/stage records, docs.

- [x] Update existing one native method to protocol2/source22 Before, 222 continuous samples, eight body pairs plus one separately labeled shadow-only pair, original actual Noah speech and front/back public walking. Use no yields inside manual loop; retain actual clock/frame guards and strict causal scope.
- [x] Update validator and synthetic positive/rejection fixtures for source, side/vertical movement, shadow-only semantics, exact one-method launcher and raw archives; reject obsolete/wrong baseline or frozen/cut sequences.
- [x] Bind only authorized current files in a new depth-focus candidate; keep old candidate and all accepted freezes immutable. Save focused evidence and truthful unverified native/APK/Pixel/adoption state.
- [x] One fresh whole-branch source review; one material-fix pass with focused checks. Commit/push existing feature and hand off single native capture. No rollout/adoption/full-QA success claim.
