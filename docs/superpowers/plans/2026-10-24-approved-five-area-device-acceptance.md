# Approved five-area device acceptance execution plan

> **For agentic workers:** Use superpowers:executing-plans inline. User explicitly authorized acceptance execution; no additional plan/file/QA approval gate. One final fresh review.

**Goal:** Move the approved638aa24 five-area appearance into a verified full-test/APK/Pixel acceptance run without altering gameplay or appearance.

**Architecture:** Reuse build-android.ps1 -TownQa, install-pixel3a.ps1 (-r, explicit townQA package) and capture-pixel-visual-check.ps1. Bind the user's actual approval to the current native JPEG15/digest, preserve old conditional-source identities, freeze production inputs in an audit record. Change only stale acceptance metadata/test assumptions and a review-authorization boolean guard if the new approved state exposes it.

**Tech Stack:** Existing Unity6000.6.4f1, WindowsPowerShell5.1, ARM64/IL2CPP/GLES3 TownQA, ADB, existing actual native evidence.

**Spec:** docs/superpowers/specs/2026-10-24-five-area-device-acceptance.md

## Global Constraints

- Accepted native source638aa2474a93d2cc21768e030a7100972c5c446e; render digestdfb2cbc80e81c0d3967341f13426e2465913887f16f26763f696c36c7efe26fa. No Assets game/code/art/scene changes.
- Preserve base153 and current approved rendering inputs; freeze source hashes and reviewed15JPEGs. No save wipe/uninstall, no APK/signature bypass. Main remains forbidden.
- Failed0 and skipped0 required for BOTH full Unity runs; APK/hash/device/soak statuses never inferred from mocks or earlier16cases.

## Review Focus

- Newly approved metadata must permit existing QA entry and still reject wrong/missing approval/evidence/render inputs.
- Post-review booleans may become true legitimately without mislabeling old capture metadata as new adoption.
- WindowsPS5.1 strict JSON/array/encoding behavior and true native exit codes.
- TownQA correct package/update install, signature refusal, same APK SHA, saves preserved; no silent old APK reuse.
- At least600seconds real play plus restart/save, all5round trips and loop/device outcomes remain pending until actual evidence; an instruction sheet is not completed acceptance.

## Task1: Record approval, lock quality and verify acceptance gates

Files: References/VisualLock/stage-acceptance-2026-10-21.json; new five-area-approved-2026-10-24.json; docs/verification/south-final-conditions-2026-10-23.json; only scripts/five-area-review.ps1 if needed and related scripts/tests; AGENTS.md.

- [x] Watch existing fullQA gate fail before approval update; update acceptance test expectations to actual latest user authorization.
- [x] Record explicit5-area approval, conditionalSouth repair fulfillment/current quality linkage and unchanged game/render/evidence hashes; never rewrite old raw receipts.
- [x] If the formerly false-only integration guard rejects valid post-approval metadata, add true/false consistency regression RED, fix minimally, verify GREEN.
- [x] Run all strict PowerShell suites, actual preflight/base/render guards and test inventory. Confirm143EditMode and222PlayMode declared, without claiming native success.

## Task2: Reach actual execution boundary and hand off required hardware work

Files: new docs/five-area-pixel3a-acceptance-2026-10-24.md and docs/verification/five-area-pixel3a-acceptance-2026-10-24.json.

- [x] Save explicit report/checklist matching every user requirement and existing safe build/install/capture commands.
- [ ] Commit clean candidate, probe actual -TownQa entry, attempt licensed cloud execution once if feasible; record exact runtime limit and preserve game hashes.
- [x] One final read-only approval/tooling review; fix only material findings, then commit/push feature. No main.
- [ ] Give Windows build command and upload instructions; keep actual fullNative/APK/Pixel statuses pending until evidence arrives.

## Ledger

Initial: clean existing user feature checkout9daa0e3. Continue there to preserve the user's Windows/Pull continuity; no new worktree or branch. User already supplied native execution preference and file-independent authorization.
Ruling: reuse existing three tools, not a new orchestrator/game feature — avoids new acceptance infrastructure while hardware evidence is still unavailable.
Environment: only LinuxStandaloneSupport installed; no adb/USB host path in cloud. Windows license/device session remains user-side.

Task1 verification: approval gate initially rejected historicalSouth density; approved354runtime files/JPEG15 bound to actual user638aa24 acceptance. Two behavioral regressions observed RED then GREEN: frozen scenery outside route digest, and true post-review approval state with mismatched-state rejection.12/12 strictPS suites passed.143EditMode/222PlayMode compiled inventory is not execution. Tightened stale EditMode minimum142 to143; focused Android-gate regression rerun. Assets remain exact638aa24.

Task1 final: tightened EditMode143 gate Android regression passed, including fail/skip/inconclusive/partial suite/native exit/device/update mocks. All354production files and all15approved JPEGs remain unchanged.

Final independent review: Critical0 Important0, no actionable minor findings.354production hashes and15JPEGs independently verified; runtime directories remain exact638aa24. Existing S1/J1 visual minors remain deferred to device acceptance as explicitly instructed.
