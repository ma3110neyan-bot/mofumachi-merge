# Stage1 stability implementation plan

> **For agentic workers:** Use superpowers:executing-plans inline. User has already authorized bounded corrections and acceptance. One final fresh review; no per-file confirmation.

**Goal:** Correct north camera exposure and remove the user-reported floating field captions, then continue actual device acceptance.

**Architecture:** One small change to TownWorldCamera.Clamp and removal of four floating direction Text objects. Preserve the immutable354-file approval; explicitly bind its permitted camera/caption deltas in a separate candidate record. Keep old image evidence historical and new capture identities current.

**Tech Stack:** Existing Unity6000.6.4f1, C#9, .NET8 CPU tests, WindowsPowerShell5.1 acceptance scripts, existing Pixel tools.

**Spec:** docs/superpowers/specs/2026-10-24-stage1-stability.md

## Global Constraints

- Runtime changes only TownWorldLayout.cs camera Clamp and TownContinuousWorldView.cs floating-caption removal. No assets, routes, save, reward, Merge, BGM, UI, character or topology changes.
- Preserve approval638aa24, all15JPEGs, old raw results and existing APK185942 identity.
- New candidate is implemented only until actual new Unity/APK/Pixel evidence arrives. Never infer10min or save from PDF.

## Review Focus

- Both north sides under Jump, repeated Pan and Follow, portrait heights592/740/808; no large off-map view.
- North lateral destinations remain in view, approach clamp continuous; west/east and south framing unchanged.
- No authorization record accepts a second changed production file, wrong old/new SHA, or stale current digest.
- Historical image evidence remains checked against exact prior rendering inputs; actual captures retain current input digest.
- Pixel serial/APK/save/600sec must be established from actual execution or explicit user report.

### Task1: Pin evidence and reproduce camera issue

Files: new References/Review/stage1-device-2026-10-24 receipt/PDFs; Tests/Presentation/NorthCameraBoundaryTests.cs.

- [x] Save suppliedPDF bytes/SHA and per-page findings without inline images.
- [x] Add CPU regression for north Jump/Pan/Follow and visible lateral destinations; watch failure against current camera.

### Task2: Minimal camera correction and bounded QA authorization

Files: TownWorldLayout.cs; scripts/visual-lock.ps1; scripts/central-review.ps1; scripts/tests/visual-lock.tests.ps1; candidate JSON; stage acceptance link.

- [x] Apply only north envelope in existing Clamp; verify CPU regression and complete CPU suite.
- [x] Add allowed-two-file QA gate regression first; watch strict freeze reject candidate.
- [x] Bind camera/caption old/newSHA outside immutable approval; gate all other files unchanged and historical digest restored only for old evidence.
- [x] Prove altered second file, missing authorization and wrong hash are rejected; run strictPS suites and actual preflight.
- [x] Compile actual Unity asmdefs; retain full native222 requirement and actual pending statuses.

### Task3: Device handoff and honest stage report

- [x] Record sourcePDF scope and unresolved stone/material issues; update current instruction/acceptance ledger.
- [x] Fresh code review, address material findings, feature commit/push. No main.
- [x] Give one existing TownQA rebuild command for corrected candidate; use same existing safe installer/capture. Hardware results remain pending.

## Ledger

Start17881e1, clean existing authorized feature checkout. Keep branch continuity with user's Windows setup; no new worktree. Preflight: Task2 gate consumes Task1's unchanged approved source and modifies only candidate camera/caption removal. User's latest bounded repair authorization supersedes the prior no-runtime-change instruction only within this scope. The author's concrete choice is a north-only camera envelope, not a new area or art redesign.

Task1: complete — originalPDF27pages/SHA received; CPU regression RED3/4 at left edge-1.75, expected>=-.60. After north-only Clamp, fullCPU306/306 failed0/skip0.
Task2: in progress — existing strict QA gate observed RED on stale native digest before any gate change. Preserve original approved354/15JPEGs; separate exact2-file correction authorization. New explicit userFB adds floating-caption removal only; retain district HUD/nameplates. Native tests expanded inside existing camera test, native case inventory remains222.
Ruling: the native Editor cannot execute in this licensed cloud; the caption regression is a live-scene assertion for Windows, not claimed locally RED/GREEN. Code removes the exact four labels matching the supplied screenshot text.

Final fresh review: Critical0, Important1 (fresh capture validator used historical digest), Minor1 (two-file/fresh-vs-old coverage). Important fix adds explicit HistoricalApproval mode only at old-approval call; default exporters stay current-source. Real regression observed RED because old capture was accepted as fresh, then fixed. Synthetic fixture now exercises both correction inputs. FullPS first run9passed/4failed exposed legacy South hash prerequisite and incomplete inherited fixture reference; exact old/new authorization binding and complete referenced fixture copy repair these without changing images/old conditions. Final strict run pending.

Task2: complete — CPU306/306 failed0/skip0, actual UnityAPI6asmdefs0warnings/errors. Current strictPS13/13 (focused5/5 plus remaining8/8 after all final code changes). Actual2-file gate preserves352other inputs/15JPEGs/original354snapshot. Fresh-vs-historical regression now GREEN; original approval gates pass, current exports use actualdigest. Reviewer Important fixed in one pass; two-file mutation coverage also added. These are compatibility/CPU/tooling results, not new native or device success.
Task3: software work/handoff ready — latestFB, PDF receipt and scope7-row report saved. New native/all-tests/APK/Pixel/600sec remain pending due existing cloud license/Android/USB limit. User-side -TownQa is the next executable boundary; no main.
