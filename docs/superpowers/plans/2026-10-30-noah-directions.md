# Noah Directional Parts Continuation Plan

> **For agentic workers:** Use superpowers:executing-plans inline, one scoped final review. User's minimal Noah-only testing overrides full-suite defaults.

**Goal:** Add right/back independent-part material examples and a continuous directional review without adopting or integrating them.
**Architecture:** Preserve yesterday's source and HTML; create a sibling candidate folder, reuse its rigid bone model, add per-direction art registration and controls.
**Tech Stack:** PNG authoring tool, JSON, existing browser canvas/Python packaging/Node tests.
**Spec:** docs/superpowers/specs/2026-10-30-noah-directions-design.md

## Global Constraints
- Noah only; formal face/eyes/colors/costume/body/silhouette unchanged. No full3D, other-five rollout, product-path edits, full suites, native/APK/Pixel claims or requests.
- Front remains exact formal atlas; new left/right/back art is unapproved material. Do not claim all-direction completed rig or game interaction.
- Preserve raw authoring outputs and prompts, source SHA, prior artifacts and provenance. No flipX substitute for direction art.

## Review Focus
- Anatomical right pouch must not switch hips; inspect both new sheets against original and directional reference.
- Heads/limbs must not duplicate or leave residual body pieces; inspect all source crops and assembled static poses.
- Back layering must place tail correctly without floating feet; inspect180px and320px assembly and phase samples.
- Direction switches must not create double silhouettes or shear faces; use one material direction at a time with rigid transforms.
- Browser results must not be confused with native or performance; explicit labels/false flags and protected-path digest.

### Task 1: Right/back source materials
Files: References/Characters/noah-directional-parts-2026-10-30/.
Consumes: original atlas SHA23d14025… and 2026-10-29 corrected candidate sheet. Produces: raw right/back component sheets, measured crop/pivot/source registration and provenance.
- [x] Author right then back independent head/body/arms/feet/tail/pouch using image tool, inspect and reject identity errors; at most one targeted correction per material.
- [x] Measure alpha bounds and register at the same600-unit floor/height; retain direction-specific draw order. Source art needs decode/hash/visual validation, not mirrored tests.

### Task 2: Continuous directional material review
Files: direction-review.cjs, scripts/build-noah-direction-review.py, scripts/tests/noah-direction-review.test.cjs and focused browser check.
Consumes: Task1 registration, prior rigid rig.pose/blend. Produces: selectDirection(vx,vy,previous), sampleSequence(seconds), poseForDirection(seconds,mode,direction), offline review HTML.
- [x] Write focused tests: direction dead-zone retains facing, vertical preference chooses back/front, invalid values safe; sequence covers left/right/back and ends talk; transform distances stay rigid, feet grounded in each view.
- [x] Run direct Node file; expect missing module/API RED. Implement pure direction/pose mapping and package offline comparison; run scoped Node checks GREEN.
- [ ] Browser check material switch/poses/sequence, exact front source, grounded feet, pause/resume,390px layout, noJSerrors/noexternalrequests. Capture sources clearly as material previews.

### Task 3: Scoped record and handoff
Files: docs/noah-directional-parts-review-2026-10-30.md, manifest, current AGENTS status, docs/verification/noah-directional-parts-2026-10-30/.
- [x] Verify raw PNG bytes/SHA, original atlas, empty protected product diff. One fresh scoped review; one fix pass for Important findings. No repetitive full QA or device instructions.
- [x] Commit to existing feature; remote upload remains independently confirmed or pending. Do not repeat earlier stalled network approval loops.

Ruling: detailed method and quality constraints already supplied; AGENTS13 and explicit next-work instruction authorize isolated continuation. Internal materials are not formal design replacement. Front parts/expression/game integration are still future work, never implied complete.

Execution status: initial/fitted browser checks passed; last rear-tail ordering change remains visually unverified because the final scoped browser execution was interrupted. Preserve pre-final screenshots as historical, not final evidence. Final review/handoff may record this blocker; do not claim all tasks complete or adopt/integrate art.
