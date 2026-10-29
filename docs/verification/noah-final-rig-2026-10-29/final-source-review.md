# One fresh source review

Reviewer: gpt-6-astra/high, fresh context, read-only, no sub-reviewers. Range9fbb684..abdffda4. This is source review, not visual/Pixel approval.

Strengths: Noah-only opt-in/default renderer isolation, same formal UVs and inherited root, protocol3 separation of static/actual-walking/shadow evidence, experimental identities/hashes/mode checks and install-r restoration. No Save/Core/other-five implementation changes found.

Critical0. Important1: device install/launch, SurfaceView wait, mode verification and starting PID ran before the protected evidence path. Launch crashes could exit without crash records or result ZIP, then be hidden by current-APK restoration. Required fix: create nullable evidence before launch, guard the entire session, best-effort crash logs and always archive before rethrow; preserve primary error if restoration also fails.

Minor1: failed crash-buffer reads emitted0 events; unavailable ending PID emittedsame_process=false. Executor regraded this Important because the user requires missing measurements to remain unknown and crash assessment must not turn unavailable evidence into concrete outcomes. Addressed in the same failure-path fix pass.

Original verdict: source handoff not ready until failure-path fix; no renderer blocker found. No independent re-review was requested. The fix is verified by the focused RED→GREEN tests described below.

## One fix pass

`Invoke-NoahPixelTrial` now creates the output/nullable report before device operations, archives startup errors and raw surface/crash/PID/memory context in finally, and rethrows afterward. Crash events and process comparison remainnull when unavailable. Success still retains actual known metrics. `Invoke-NoahTrialPairActions` always attempts restoration and preserves the first capture error if restoration also fails.

Observed RED: startup test produced no ZIP with the former entry point; restoration test could not preserve the requested failure path. GREEN: mocked launch/surface/mode/PID errors each produce a ZIP with the expected actual failure, unavailable crash/PID data remainnull, a successful synthetic session retains valid metrics without adoption, and simultaneous capture/restore failures retain the original error. The fixture uses portable temporary paths and asserts exact failure reasons, avoiding a false pass from a nonexistent Windows drive on Linux.

No full suite or device was run. Existing31 Noah CPU checks and3 API compiles cover unchanged renderer source; only relevant PowerShell checks were rerun for this script-only fix. Raw results are preserved separately. No native/Pixel/visual success is inferred.

## Declined to judge and executor rulings

- Static dimensionality, identity preservation in rendered images, blink registration, clipping and grounding need new Unity/Pixel images. Keep visual approval closed; cost if wrong is a rejected final trial, then retire this one-atlas method.
- Pixel FPS, heat, memory, taps, stutter, saved-data retention and crash freedom need the connected Windows device. Keep values unknown; cost is the explicit external device step.
- Successful paired Android builds and Windows PowerShell5.1 execution were not performed here. The scoped command must validate them; platform failures are diagnostic failures, not permission to run whole QA or loosen proof.
- Sampled geometry is not an exhaustive proof over every possible continuous pose. Keep experimental status and inspect actual motion; any defect blocks adoption.
- Whole-town/fountain/North image acceptance and other-five rollout are outside this bounded task. Preserve accepted inputs and the explicit rollout prohibition; do not infer new global visual acceptance.

No deferred minor remains after the executor's regrading. No merge/main/adoption approval is implied.
