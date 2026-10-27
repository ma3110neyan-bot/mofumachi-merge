# North / Noah bounded gate integration — 2026-10-27

Implemented in `scripts/visual-lock.ps1`, `scripts/central-review.ps1`, one new `scripts/tests/north-noah-lock.tests.ps1`, `scripts/south-review.ps1`, and existing Visual Lock / boundary / five-area / South / Android fixture compatibility.

The new scope is `town_north_noah_trial_2026_10_27`. It retains the historical fountain-final cumulative scope, field-freeze reference and actual historical fountain Before. It requires exactly thirteen changed historical files (existing nine + North layout/ground + TownActor + CharacterBodyGraphic), ten added rendering inputs (existing eight + Noah helper and meta), and the existing one capture input. All historical Before hashes still match the unchanged five-area approval. CharacterBody Before is explicitly tested as original 354-input hash `1ab679902e3bc8a9631a999c58bfb03d624c7505ee089d8cf076417c4b5c900d`.

The separate adopted release manifest is checked by the pinned LF digest `221bb4b9b2417937b374f4b5bccc60d978df09523ce8f3d4efcfdec5b619e5b7` in both the production gate and `release_freeze_lf_sha256`, exact repository path, `authorized_by=user`, `fountain_adopted=true`, and full approved revision `ff7215c504efae5fee220d3540272ca4b260ade8`. Every manifest file is then passed to `Assert-BaseVisualLock` with **no replacement hashes**. The frozen files therefore cannot be changed by refreshing a candidate hash. Manifest file count is not hardcoded; parent owns its final source/cache policy.

Fresh capture fingerprints now include CharacterBodyGraphic and Noah helper/meta. Historical reconstruction omits those three newly bound paths, preserving the exact original `dfb2cbc80e81c0d3967341f13426e2465913887f16f26763f696c36c7efe26fa` digest. The existing original baseline JSON was not modified. `Assert-OriginalBaseForCandidate` allows only the explicitly authorized body replacement from an already validated new-scope patch, and is shared by the test and real TownQa entry; the other 353 original entries remain checked normally.

## TDD

- RED observed: new fixture failed `Unknown bounded display correction scope` at the old production gate.
- GREEN observed: standalone new fixture exited 0 after the initial gate extension.
- Additional RED observed: the shared original-base helper was absent; after adding it the positive authorized-body/353-unchanged check passed.
- Additional RED observed: rehashing both the release JSON and candidate accepted a forbidden atlas replacement. The release digest is now pinned literally in production; final standalone GREEN exited 0 (`ps-fixtures/north-noah-lock.log`).
- A test-only legacy check initially expected the old scope to reconstruct a changed North/Noah fixture without its new bindings; corrected it to test the legacy bounded-file validator. New-scope reconstruction independently proves the exact historical digest.
- Negative cases cover stale body/helper/meta hashes; refreshed candidate bindings for FountainGraphic, FountainWaterLayout, TownAreaNavigation, CharacterBodyAtlas; changed release JSON; false adoption/user/source flags even when the freeze content hash is refreshed; missing body binding. CRLF release checkout remains valid.

## Final verification

All **17/17 PowerShell fixture scripts passed**, failed **0**, skipped **0**. Each script ran in a fresh PowerShell 7.4.6 process. The three root-dependent stale-binding failures from the first batch were rerun after final2 rebind and passed. The modified final2 native-evidence/JPEG packaging fixture also passed. Original first-attempt logs and its 14-pass/3-fail summary remain preserved under `ps-final/attempt-1/`; they are historical staging results, excluded from the final pass/fail totals.

Final machine-readable results: `ps-final/final-summary.json` (also `ps-final/summary.json`), with every script name, exit code, log path and SHA256. No unresolved PowerShell fixture failure remains.

Final candidate rendering digest: `3ae960672bc504de00506dc4f5d2d90d520686898e7c157a7af3f6dedb4941e6`. Historical reviewed digest remains `dfb2cbc80e81c0d3967341f13426e2465913887f16f26763f696c36c7efe26fa`. Pinned release remains `221bb4b9b2417937b374f4b5bccc60d978df09523ce8f3d4efcfdec5b619e5b7`.

No Unity rendering, Android build, Pixel execution or visual adoption is claimed by these synthetic gate fixtures.

## Historical North condition compatibility

The existing South prerequisite directly compared NorthMasterLayout to its historical condition hash, which correctly turned RED after the authorized North edit. The precise successor exception now requires the validated new scope, a Before hash equal to the unchanged North condition, and an After hash equal to actual current layout. It never changes the historical approval or marks a new review passed. Its fixture copies the complete current authorization and release freeze, and rejects wrong scope, wrong Before, wrong historical condition, or an unbound layout mutation.

The Android XML fixture now synthesizes three cases for `NorthEntryBlendReviewTests` and four for `Noah25DTrialPresentationTests` and includes both in missing/empty-suite negative checks. Capture integration owns the production suite additions and both real original-base call sites.

The real South pipeline fixture now passes with the new scope, including wrong-scope / wrong-Before / wrong-historical-condition rejection. Android XML fixture passes at the actual new PlayMode minimum 234.

Final script results:

- `android-tools.tests.ps1`: exit 0
- `approved-design.tests.ps1`: exit 0
- `central-review.tests.ps1`: exit 0
- `east-review.tests.ps1`: exit 0
- `five-area-packaging.tests.ps1`: exit 0
- `five-area-review.tests.ps1`: exit 0
- `fountain-live.tests.ps1`: exit 0
- `north-noah-evidence.tests.ps1`: exit 0
- `north-noah-lock.tests.ps1`: exit 0
- `north-review.tests.ps1`: exit 0
- `pixel-visual-check.tests.ps1`: exit 0
- `south-review.tests.ps1`: exit 0
- `stage1-camera-lock.tests.ps1`: exit 0
- `town-boundary-lock.tests.ps1`: exit 0
- `visual-lock.tests.ps1`: exit 0
- `west-review.tests.ps1`: exit 0
- `windows-json-encoding.tests.ps1`: exit 0

## Last capture-proof update verification

The final strict mutual Noah/Momo greeting proof and per-step NPC pause renewal changed capture/evidence fixtures only. The four affected scripts were rerun in fresh processes against candidate `3ae960672bc504de00506dc4f5d2d90d520686898e7c157a7af3f6dedb4941e6`; all four exited 0: approved-design (22.930s), five-area-packaging (48.073s), five-area-review (175.724s), north-noah-evidence (6.247s). Unaffected earlier passes were retained. Latest aggregate remains **17 passed / 0 failed / 0 skipped**. Previous final2 pass logs and summary are preserved under `ps-final/prior-final2/`; last-wave results are `ps-final/attempt-3-results.json`.
