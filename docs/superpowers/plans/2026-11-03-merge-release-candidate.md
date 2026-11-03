# Merge release candidate validation

Spec: latest user message approves the current pink Merge Visual Gate and authorizes focused Merge, one full EditMode, one full PlayMode, QA APK, then physical Pixel3a. Execute inline; no design work.

Base: af084ecda712d7aaaf3b4b581496a44f1192f97c. Existing feature workspace is isolated from main; initially clean.

## Scope and tasks

1. Record user Visual approval and exact six Merge presentation source hashes in a separate receipt. Keep historical pending/native/device flags untouched. Check that receipt before/after Final QA.
2. Reuse the existing focused → EditMode → PlayMode → APK pipeline without retries. Return the exact completed run directory. Add a Pixel-only continuation that validates its real XML/artifacts/APK, installs with install-r and reuses the existing SurfaceFlinger/PSS/temperature capture; retain crash logs. Resuming a device error never reruns Unity suites.
3. Run only diagnostic-script tests for these changes, verify product/assets/Core remain identical, obtain one bounded fresh review, commit/push the authorized feature branch and provide the Windows execution command.

Pre-flight: the parent run's source JSON and child builder JSON have different scopes. Preserve both; child build flags remain false because it does not execute tests. The Pixel-only continuation reads actual parent XML and verifies the exact child APK SHA, not merely the builder flags.

Ruling: the cloud has no licensed native Unity/Android/USB Pixel. Do not repeat known failed startup probes, run substitute full suites, or invent XML/APK/device success. Complete the executable Windows continuation and leave native results pending; the user must run it on the attached physical device.

Review focus: product diff zero; no new UI, asset or Save changes; approval is visual only; exact artifact/APK identity; false/incomplete parent evidence blocks install; no retry loops or full-suite reruns on Pixel resume; default install preserves app data; measurement is separate from manual acceptance.

## Ledger

- Started: source clean at af084ec; existing Final runs focus and each full suite once, with separate capture output for full PlayMode. Existing install has no performance collection/resume.
- Task1: approved design/reference receipt added; actual receipt/Core/items/character guards passed. Assets/ProjectSettings/Packages diff against af084ec is zero.
- Task2: meaningful contract tests first failed because approval verification was absent, then passed. Focus/full once-per-platform invocation is unchanged. Parent records three original XML hashes; Pixel-only continuation validates exact completed parent/child identity, actual XML and capture artifacts before install-r and existing metrics helper. It never executes Unity.
- Final review: one Important finding, original capture receipt hashes were not compared on Pixel resume. Fixed in the single review fix pass: missing/duplicate/changed receipt and edited HTML regression failed first, then passed after exact path/length/SHA comparison. No Critical findings; no deferred minor changes.
- Task3 verification: release-contract, capture-artifact, preview-routing and Pixel-metric PowerShell fixtures passed. These use synthetic evidence, not actual Unity XML/APK/device results. Actual approved design/original/Core/items/character source guards passed; PowerShell parser and git diff whitespace checks passed. Native Windows PowerShell5.1/Unity/APK/ADB execution remains unverified here.
- Native release stages remain pending; do not mark the release complete. Windows one-command continuation is documented, and exact-directory Pixel resume avoids repeating successful full suites. Authorized normal feature commit/push is the remaining handoff step.

## Received Windows focused failure — 2026-11-03 10:54:56 JST

- Run source ce8ee19, fingerprint2f6889c5, exact CLI/source-info identity agrees. Preparation succeeded. Native focused result53 total/52passed/1failed, skipped/inconclusive0; capture1/1 and reward UI6/6 passed. Full Edit/Play/APK/Pixel were not started.
- Root cause: MergePolishTests.ConsecutiveMergesReuseOneSmallEffectLayerAndRestartKeepsProgress still applies the pre-reward eight-star budget of120vertices. Approved reward geometry substitutes three six-oval flowers for three stars: five9-vertex stars + three78-vertex flowers + two17-vertex glows =313vertices at0.13seconds. The failure reports exactly313; no shader/Save/data corruption is implicated.
- Scope: correct this test only; preserve every production byte and the approved display receipt. Add the exact MergePolishTests path/SHA to the narrow existing test correction allowlist, preserving the historical lock originals. Related guard test, evidence receipt, this ledger/AGENTS and Windows instructions may change. No runtime/asset/UI edits.
- Ruling: do not lower the accepted effect quality or silently raise an arbitrary ceiling. Check the exact approved composition, bounded indices across the lifetime, zero geometry after expiry, no input interception/particles, and retain all existing reuse/save/restart assertions. Raw Windows failing XML is the native RED; no native GREEN can be claimed in this unlicensed cloud.
- Guard regression RED: the existing scope rejected the seventh exact file (the third related test), as expected. Implement only that explicit test path, never an arbitrary test/asset/Core escape.
- Correction verification: unchanged production Sparkle/Cell/Timing compiled in an explicitly geometry-only managed compatibility probe.12 phase samples reproduce the old120 failure, peak313vertices/288triangles; corrected bound passes, finite vertices/indices valid, no geometry at/after expiry. Native PlayMode API build0warnings/0errors. Minimal scope/release/preview/capture contract fixtures and actual approved original/Core/art/character guards pass. All production source/assets still byte-identical to ce8ee19.
- Bounded independent review: no Important/Critical findings; real CanvasRenderer and Pixel performance remain unverified. No full suites were run in cloud. Windows focus must pass before its first full Edit/Play run. User source/failed XML originals and eleven-file SHA receipt retained under References/Review/merge-reward-focus-failed-2026-11-03-105456/; no failed result relabelled as success.
