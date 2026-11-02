# Merge Reward UI Implementation Plan

> Execute inline using superpowers:executing-plans; one fresh final review. User explicitly waived design menus/intermediate approval.

**Goal:** Apply A usability plus the provided D「ごほうびマージ」visual to the existing Merge screen without changing game rules or data.

**Architecture:** Preserve UIFlowController and all Core files. Extend the existing BuildMerge/ProductionFrameLayout tree so existing button names, raycasts, drag, locks and callbacks stay compatible. Use read-only quest progress and Coins for reward display. Reuse official ItemAtlas, current fonts and existing floral/gold frame art. Only Merge-local theming. Keep thirty5×6 flat input cells and >=48-unit touch sizes; match D's rectangular cells by filling each row/column without perspective. Compact heights reuse the existing five-action row; roomy mode has three generators and a large request/delivery button, plus a reward strip in the already-reserved feedback area. Top quest card always carries reward amount; real feedback may replace the duplicate bottom strip.

**Spec:** User's18-section Merge instruction and the inline941×1672 D reference in this conversation. Fixed reference path References/Merge/merge_visual_lock_reward.png. Image has been visually inspected. Its exact original file bytes are not exposed by this inline message/tool interface; original PNG persistence remains PENDING, never replace it with a generated imitation.

## Global constraints

5columns×6rows flat; official tea/bread/flower art unchanged; Merge/generation/levels/requirements/delivery/reward/Save/town-level logic unchanged. No characters/town/fountain/book/BGM/font changes. Real Coins only, not mock gem10. Related Merge checks only during work. Full Edit/Play+QA APK+Pixel only at candidate stage. No native or Pixel success claimed from CLR/API/image mockups.

## Exact planned files

- Modify QuestMergeScreens.cs: Merge builder and its ProductionFrameLayout only; preserve BuildQuest byte-for-byte.
- Modify MergeCellSurfaceGraphic.cs: rounded cream/mint and faint empty-cell motifs.
- Modify MergeSparkleGraphic.cs: bounded petal embellishments during existing successful burst only.
- Modify MergePerspective.cs: presentation cell rectangles only; retain index order,5×6,4-unit gaps,constant art scale,min48 targets.
- Add MergeRewardPresentation.cs/meta: read-only goal/reward copy and layout arithmetic.
- Add MergeRewardView.cs/meta: Merge-local header/button/quest/reward decoration and responsive adjustment.
- Related presentation and PlayMode tests; native Merge-only capture evidence.
- Update existing MergeInteractionTests.cs and ExperienceMergeTests.cs only for authorized review/rectangular/frame assertions; retain all interaction and busy-state protection.
- Add merge-reward-scope.ps1 and exact UI-only override record; hook existing historical-source guard without weakening original manifests.
- Add focused/final Merge Windows validation script and docs. Reuse existing character-resource preparation and APK builder; do not rerun character-specific acceptance.
- Reference/evidence/plan/AGENTS records. No shared widgets, current fonts, official atlases or Core files edited.

## Review focus

Incomplete accepted requests must still have an obvious review path, completed requests must deliver only through existing code, and merge-busy disables actions as before.
Multiple requirements/inventory-only counts and locked output must use real QuestManager progress; never invent completion/currency.
At320×536 and tall/safe-area sizes, all30 cells/actions remain48+, feedback never obscures cells, decorative graphics never intercept taps.
Empty-cell motifs stay behind items; invalid/full-board/save-failed paths never show a new success burst.
Source scope must retain all Core,ItemAtlas/fonts,characters/town and historical lock hashes; pending original reference/native/device evidence must not become accepted flags.

## Task1: tests and read-only UI model

- [x] Add failing tests for true Coins/progress/capped fraction/multi-quest/empty unknown data and tiny/tall caption budget.
- [x] Run only new Merge tests RED, implement pure helper, GREEN.
- [x] Retain flat30 positions/min48, test D full-width rectangular filling, then update rendering geometry only.

## Task2: Merge visual composition

- [x] Pink/gold title, mint back/settings, large clearly-labelled current quest, actual Coins, missing counts and required generator accent.
- [x] Cream/mint round cells, faint paw/leaf/flower/star only on empty unlocked cells. Preserve ItemGraphic and source UVs.
- [x] Roomy three-generator row, large review/delivery action and real reward/task strip. Responsive fallback keeps all touch targets.
- [x] Short bounded petals inside existing success-only burst; original .22s input lock/.5s FX timing retained.
- [x] Related tests, API builds, source freeze/BuildQuest invariants. Native capture prepared; no fabricated After preview substituted for native evidence.

## Task3: native visual/evidence and guarded QA

- [x] Prepare exact current-source Merge native capture/related tests, reference comparison if raw PNG exists, source/state integrity.
- [x] Narrow UI override against former protected552, exact source-before/new hashes and protected Core/art/fonts. Exactly two related test expectations also amended; old busy-state test retained.
- [x] Prepare candidate-only full Edit/Play once, exact same-candidate APK build/install route; no character acceptance reruns or save clearing.
- [x] One fresh-context final review and bounded corrections: disabled review action during merge lock, updated obsolete square/old-frame assertions. Recheck important issues0.
- [x] Original PNG saved byte-identically from uploaded20261102 ZIP, SHA256 recorded. Product code remains a18e598.
- [x] Authorized feature push of a18e598 verified on remote.
- [ ] Native Visual Gate, full native QA/APK and Pixel. Native license/Android module/USB environment remain unavailable; no success claimed.

## Ledger

Preflight checkpoint/merge-before-reward-20261102=af659256b481aae0697cea7ccfbf5531c6a62c08, UI and protected SHA record saved. User subsequently supplied visible D image; visually inspected. Current PNG file remains unavailable as raw download, so original persistence is not claimed. User's explicit no-confirmation instruction supersedes skill approval menus. Existing rounded-square tests are a previous UI shape preference, not a Core rule: D uses wider rectangles; update presentation expectation but retain30/min48/no overlaps/index/gap/art-scale invariants. Use current font; glyph「捗」is absent, so visible copy uses existing「あと」「現在の依頼」「ごほうび」instead of changing fonts. Original input/FX/generation/quest services remain untouched.

Continuation: original ZIP now received, original PNG saved and hashed. Above preflight missing-file statement is historical. Ruling: add capture-only orchestration before Visual Gate, because the latest explicit user sequence places Merge focused tests after visual comparison. Only workflow/scope metadata and docs change; a18e598 product files remain byte-identical. Capture-only routing and original-reference SHA protection each observed RED→GREEN in synthetic PowerShell tests. Neither synthetic gate represents Unity rendering or device acceptance.

Native attempt after original-save commit be36887: actual capture startup exits1 (no X display); separate no-graphics startup diagnostic exits198 (no valid Unity Editor license). No XML/JPEG produced. Android module and Pixel USB also unavailable. Visual Gate and all downstream testing/build/install remain pending; no product/UI changes made to conceal the environmental failure. Windows capture-only handoff is concrete and committed.
