# Direct Pixel Preview Implementation Plan

> **For agentic workers:** Implement inline with superpowers:executing-plans; one final review. The user has waived intermediate approval questions.

**Goal:** Build the frozen six-character authored APK and update Pixel3a without any Unity test execution or Hub interaction.

**Architecture:** Reuse the unchanged Noah/five-character preparers and safe install-r helper. Add a separate Editor build entry with both existing authored defines as build-local options, and a PowerShell route that never invokes runTests or fabricates previous full-suite evidence. Keep the normal full-QA route intact.

**Tech Stack:** Unity6000.6.4f1 Editor C#, Windows PowerShell5.1, adb.

**Spec:** User request: “UnityHubのテストなしでダイレクトにPixel3aで確認”. Existing character art/runtime and final53 Visual Lock remain fixed. The user has already authorized normal GitHub feature push.

## Global Constraints

- No EditMode/PlayMode/native capture, baseline pair, automatic metrics or acceptance claims in this preview route.
- APK compilation still requires Unity; no licensed cloud Android build/device is available.
- Same town QA package, install-r, no uninstall/pm clear/Save changes.
- Three sequential executeMethod invocations: Noah Prepare, Character PrepareAll, direct Build.
- Check frozen53 and existing26/552, clean inputs, exact source/packed/APK identity before installation.

## Review Focus

- An old/failed APK must not be selected by latest-directory scanning; read exact new run metadata and SHA.
- Both existing authored defines must be build-local; do not persist settings or change runtime.
- Dot-sourced helper param blocks must not overwrite caller options.
- A failed build must preserve diagnostics and block install; cleanup only current-owned generated inputs.
- Device absent/unauthorized must fail before the long build; no destructive recovery.

## Task: Dedicated build/install route

**Files:** new Editor CharactersDirectPixelBuild.cs/meta, scripts/prepare-characters-direct-pixel.ps1, related PS guard test and handoff doc. No existing renderer/builder/lock edits.

**Interfaces:** Get-CharactersDirectArguments(repository,directory,step) returns only executeMethod arguments for prepare-noah/prepare-five/build. Read-CharactersDirectBuild(directory,revision,fingerprint,packed) validates source-info and one authored build JSON plus APK hash; unfinished or unrelated artifacts are rejected.

- [x] Write a failing bounded PowerShell test for missing direct route, no-test argv and rejection of stale source/packed/hash/false evidence.
- [x] Run RED, implement standalone build + route, then run only this related guard and Editor API compilation.
- [x] Verify fixed53/art/runtime unchanged, review direct route and provide the exact Windows update/build/install command.
- [x] Prepare the delivery commit for the already-authorized normal feature push; verify remote SHA before the final response. Do not claim APK/device execution in cloud.

## Verification ledger

Related PS test first failed because the direct route was missing, then passed with real generated argv and synthetic APK/source records. A platform-only test fixture used a C: path on Linux; changed that fixture to a portable temporary path with spaces. No product fix was needed for that fixture.

Initial Editor API compilation used an older Presentation DLL and reported missing NoahAuthoredRigConfig in existing NoahAuthoredBuild.cs. Rebuilt the current Editor-configured Presentation references (0 warnings/errors), then the complete Editor code compiled (0 warnings/errors). This was stale local API evidence, not a change to the frozen character sources.

All53 frozen hashes match; runtime, source art, original builders, Visual Lock and historical amendments unchanged. Fresh reviewer direct_pixel_route_review found no Important/Critical finding. Review covered no-test routing, exact source/packed/APK checks, option preservation, owner cleanup, build-local authored defines and update-only device install. Native build/device cannot be run in this cloud environment and remain unclaimed.

Ruling: the user's explicit no-Unity-test request overrides the skill's default full-suite step. Only related command/identity checks and API compilation ran; the Windows route runs no tests. Ruling: retain the established feature checkout and already-authorized push rather than ask another integration question. No main merge or PR.
