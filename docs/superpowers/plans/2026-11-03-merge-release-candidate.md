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
