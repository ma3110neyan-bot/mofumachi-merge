# Head Body Sync Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans inline; one fresh final review.

**Goal:** Keep all six heads attached to their body neck frame, with no independent translation or delayed update.

**Architecture:** Keep the existing single-mesh directional parts and NoahAuthoredMotion samples. Register a neck contact from each unchanged source part's central opaque contour at build time; resolve the active head from the same body's current transform and that contact, including closed-eye materials. No runtime texture reads or secondary motion interpolation.

**Tech Stack:** Unity6000.6.4f1 C#, NUnit, PowerShell.

**Spec:** Latest user's “キャラクター2.5D 最終修正指示”, PDF183629/183822 and received stability-focused ZIP. Scope is head/body synchronisation only.

## Global Constraints

- Do not change art, eyes, colors, costume, shape, nominal scale, speed, shadow, rear materials, turning policy, motion samples, conversation, passing, town/UI/fountain/Merge/Save/book/BGM/font.
- No full-suite reruns. Only head/body transitions/start/walk/stop for each of six IDs.
- Continue without approval round in this explicitly authorized scope. Keep the established feature checkout and authorized normal feature push.
- No formal fixed/zero-failure claim until current six-character rendered evidence passes. Cloud maths/API are not actual Unity/Pixel.

## Review Focus

- Left/right/back registrations have different transparent padding: rectangle edges must not be used as anatomical joints.
- Blink materials can have different crop/pivot and must share the same body socket.
- Direction and movement phase change in the same frame; no cached previous-frame head pose.
- Non-head vertices, texture UV/colors, root scales and all motion sample values must stay unchanged.
- Prepared resources must match this run; only one focused native method before optional same-candidate APK install, never full suites or Save reset.

### Task 1: Shared neck frame

**Files:** new `CharacterNeckAttachment.cs`, both authored Graphic classes and their Editor builders; new focused tests and source-alpha fixture. Historical art/layout unchanged. Explicit code-only lock amendment for Noah builder/Graphic.

**Interfaces:** Build-time `FindJoint` from opaque part pixels; runtime `Resolve` from the body transform, body neck and current head angle/neck. Build metadata belongs to the generated view, not raw artwork.

- [x] Capture the current renderer's mismatched actual neck contacts with source-alpha fixtures for18 directional views.
- [x] Write focused RED tests for shared body/head contact through idle, start, walk, stop and all directional transitions; closed-eye registration and invalid source cases.
- [x] Add one pure resolver/registration path and use it for both renderers. Keep original Sample and non-head transforms untouched.
- [x] Run head-only tests, source-alpha contact checks, relevant API compiles and exact protected-input guards; preserve evidence and commit.

### Task 2: Focused native verification and delivery

**Files:** head-only6-character native mesh/transition capture, bounded prepare/capture/build script, docs/receipt/AGENTS.

**Interfaces:** All6 prepared unchanged assets, one native method, source revision/fingerprint/packed digest, same candidate QA identity.

- [x] Native method checks all6 identities from a frozen array, actual mesh head/body contact, non-head vertices unchanged in Before/After, front/rear/turn/start/stop/blink. Save frames and head-sync counters.
- [x] Strict exact-one XML/proof checks; new run archives and Explorer on success/failure. No APK/install or full-suite rerun in the head-only capture; flags remain false for full QA and acceptance.
- [ ] Fresh review, fix important findings, verify API/guards, normal feature push and remote SHA.
- [ ] Report true per-character results; formal freeze waits for the native six-person zero-failure evidence. Do not start Merge modifications.

## Ledger

Base f2b31339d106744f96d21786bb5256d141dcc4e8, clean checkout. User waived intermediate checks. Incoming ZIP is CRC-correct, native3/3 passed, sourcef2b3133 and record/CLI matchFP40a725a3. This is old passing/tap coverage, not head acceptance. PDFs24 pages, including unrelated Merge screenshots; only head/body evidence is in scope.

Investigation: both renderers already update the head and body in one UGUI mesh from one pose; no independent Animator/Tween. Existing attachment pins the rectangular head crop's bottom, whereas central opaque chin and body collar differ by material/direction. Previous unit tests proved rectangle-pivot maths, not anatomical contact. Preserve source pixels and derive a common body neck frame instead of per-character hand offsets. Validate this hypothesis before product edits.

Ruling: existing full-suite pending status is retained, not rerun for this head-only request. Native render/Pixel cannot run in the current cloud licence/device environment; provide a concrete focused Windows route and never replace it with synthetic success.

Ruling: keep this submission to one head-only native capture. A new APK pipeline is not necessary to prove or fix the requested mesh synchronisation and would expand scope; existing APK/full/device evidence is not re-labelled.

Task1 progress: actual prior C# renderer neck offsets captured for18 source views (contact-before.json); shared binding tests RED0/7→GREEN7/7. Source art, motion samples and non-head game code remain untouched. Build-time alpha-derived registration and shared runtime body-frame resolver implemented. Task2 native mesh capture and strict432-sample/36-image gate implemented, pending bounded verification and fresh review.

Fresh review: one Important finding—median X plus extreme Y can be transparent. Reproduced against46 unchanged cropped/masked alpha inputs: all6 new source tests RED, original10 math tests green. Corrected selection to an actually opaque contour pixel centre; reviewer-requested regression suite16/16 green. Registration includes both open and closed heads, no per-character hand corrections. Re-evaluate source contacts with the validated opaque fixtures; earlier hypothetical contacts are not acceptance evidence.

Verification completed locally: final focused16/16; actual46 source-alpha contours checked opaque; normal/Android/Editor renderer/PlayMode/Editor builder/pure project API compilation all0 warnings/errors; exact10 amendments and original26/552 guard valid. Head proof/one-method/image guards pass with synthetic data clearly labelled. No new Unity, APK, Pixel or full tests run. Formal acceptance remains pending the one-method Windows capture.

Implementation commit909952102dfee843c149c912ef6456d1c38d9690. Final native head acceptance still pending;16 local passing tests are not substituted for the missing native proof.
