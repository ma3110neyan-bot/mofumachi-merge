# Town Quality Checkpoints Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans for this bounded update; do not delegate implementation. Steps use checkbox syntax.

**Goal:** Apply the user's latest quality freeze and three explicit approval checkpoints without modifying game scenery or rules.

**Architecture:** Preserve accepted97aac99 central/west/east evidence. Extend the existing development stage record/gate, not the game. Native North capture remains the next step; cloud license failure prevents actual rendering here.

**Tech Stack:** PowerShell, JSON, existing Windows System.Drawing JPEG export.

**Spec:** User's 2026-10-22「品質固定・5エリア完成指示」; current rendering source f9ed0a8. The implementation ledger is `/workspace/acceptance/town-quality-checkpoints-20261022/progress.md`.

## Global Constraints

- No changes to Assets, accepted source art, protected153 inputs or Approved Visual Lock hashes.
- Lv1 cute / Lv3 lively / Lv6 festive; scenery and walking coexist.
- User approval required at north_quest, south_garden, five_area before advancing.
- QA ordering is now five-area integration/connections → full Unity → QA APK → Pixel. Merge finish and full2.5D follow five-area acceptance; preserve existing Merge tests.
- Displayable JPG files, same-condition Lv1/3/6; no inline image attempts or public hosting.
- Current flags remain pending; author/local test success cannot create user approval or native evidence.

## Review Focus

- A passed native suite is not user approval: reject missing/false approval and missing user reference.
- A stale final integrated image set cannot authorize an APK: bind the final approval to current world inputs.
- Good prior-area images must remain intact: compare preserved files and frozen inputs after changes.
- Stage names alone cannot substitute for reviewed images: require source identity, native/quality axes and exact district/Lv JPEG hashes.
- Licensed Windows drawing/native execution is unavailable here: distinguish synthetic gate tests and unexecuted JPEG drawing from Unity verification.

### Task 1: Quality checkpoint gate and native handoff

**Files:** Modify AGENTS.md, scripts/visual-lock.ps1, scripts/tests/approved-design.tests.ps1, scripts/north-review.ps1, References/VisualLock/stage-acceptance-2026-10-21.json, docs/north-quest-master-2026-10-22.md, docs/north-review-steps.txt. Create docs/town-quality-checkpoints-2026-10-22.md and verification JSON.

**Interfaces:** Existing `Assert-BaseStagesReadyForAcceptance(repository)` consumes the ordered five-area completion list and `user_review_checkpoints`; new `Assert-TownCheckpointApproved(repository, checkpoint)` validates approvals through the requested checkpoint. North capture still yields12 native cases /21PNG /fourZIP /noAPK. Existing `Write-NorthComparisonJpeg(directory)` adds the immutable central Visual Lock design column alongside North Before/Candidate, labelled as reference design, not North native art.

- [x] Add meaningful gate fixtures: missing/false/wrong-author/missing-reference approvals, changed JPEG, false quality axis, stale final input; five ordered area stages + three valid approvals allow QA preparation without Merge redesign.
- [x] Run approved-design suite and observe expected RED against old eight-stage/missing-approval implementation.
- [x] Implement the smallest sequential approval validator; update required five-area stage list and pending real records; do not mark anything newly accepted.
- [x] Update North JPEG layout and handoff/policy docs. Formal reference art and original PNG captures stay unchanged.
- [x] Run all eight PowerShell suites and real frozen/prior-evidence/current-gate checks. Verify no Assets or accepted evidence changed, normalize-render digest unchanged, diff clean.
- [ ] Commit/push feature only, preserve workspace, record blocked native North and concrete Windows handoff; no main integration or south work.
