# Release Quality Pass Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans, inline implementation followed by one independent review.

**Goal:** Make walking, Merge, town growth and six character requests visibly richer in the next QA candidate.

**Architecture:** Reuse B's transient journeys and existing progress. Presentation owns destination choices, scene decoration and dialogue; core transactions remain untouched. Fonts and UI meshes use the existing Unity UI renderer without an SDK.

**Tech Stack:** Unity 6000.6.4f1, UGUI, C#, NUnit, checksum-verified OFL font subsets.

**Spec:** User's 2026-10-15 comprehensive Quality Pass instruction in this conversation, summarized below.

## Global Constraints

- Base `88e47dd`, isolated branch `feat/release-quality-pass-20261015`; no main integration.
- Preserve six formal characters, logo and all three approved background paintings.
- Preserve save v3, 5×6 board, item rules, delivery, rewards and existing quests.
- No payments, SDKs or postrelease features. Existing user authorization covers Presentation/UI/fonts/related tests.
- Native Windows tests and Pixel results for a new revision must be reported as unrun until actually obtained. B's 138/124 passes are baseline evidence only.

## Review Focus

- Repeated/retargeted travel during exit, transfer or settings must not duplicate a hero or strand input.
- Short portrait safe areas must keep choices, close controls, HUD and required item counts readable.
- Visual merge layers must not intercept board input, delay unlock or survive failed transactions.
- Growth and story must derive from persisted progress without granting or replaying rewards.
- All body text must have real font glyphs; mixed numeric/control roles must retain readable UI fonts.

## Task 1: Character-led walking and shortcuts

Files: TownLifeView, TownRegionSession, TownRegionControls, new TownOutingMenu; UIFlowController Back dismissal; TownRegionFlowTests and new QualityPassTests.

Interfaces: `GuideToPlace(string)` remains the single route entry; expose current destination/status read-only. Same destination is idempotent. Separate explicit scenery preview from one-tap walking.

- [x] Add UI tests for character destination choices, one-click shortcuts, repeated input, cancel/Back and cross-town facility walking.
- [x] Implement six destinations, explicit scenery mode, instant button response and status; retain existing B path/fade/cancellation.
- [x] Compile all real Unity assemblies; native UI execution remains a Windows gate.

## Task 2: Lightweight 2.5D Merge

Files: QuestMergeScreens, new MergeCellSurfaceGraphic, ItemGraphic, ItemSelectionFeedback, MergeEffectsView, MergeVisualTiming, visual/native tests.

Interfaces: Same cell rectangles and existing .22-second lock. Decoration has no raycasts. Both consumed items gather before the result lands at the target.

- [x] Add timing/visual tests, observe executable timing failures before implementation.
- [x] Add recessed tray rims, board depth, item polish/ground shadow, floating selection/matching glow and gather/flash/bounce.
- [x] Run .NET visual tests and Unity assembly compilation.

## Task 3: Body font, regional growth and quest stories

Files: UIWidgets and narrative screen builders; new QuestStoryPresentation and regional growth presentation; TownGrowthView; font resource/OFL/subset script; related tests.

Interfaces: Klee One via `NarrativeLabel`; original story headings and numeric/button fonts retained. Dialogue keyed by existing quest IDs; decoration keyed by existing town growth level and region.

- [x] Add tests for all six before/thanks/reunion stories, three regions' progressive decoration, body/control font roles and short portrait fit.
- [x] Apply verified Klee One SemiBold subset to narrative text; preserve logo and formal artwork.
- [x] Add region-specific additive flowers, seating/lamps/arches and growth previews without repainting the cute starting town.
- [x] Connect before/thanks/reunion text and growth descriptions using completed IDs, without new persistence.
- [x] Check glyph coverage, asset limits, core regressions, real assembly compilation and protected-file diff.

## Task 4: Review and handoff

- [x] Independent whole-branch review; resolve material findings and rerun affected checks.
- [ ] Record actual test inventory, validation evidence and Windows/Pixel acceptance steps; commit/push candidate only.
- [ ] New native all-pass/skip-zero XML → QA APK/build-info/SHA → Pixel 10-minute acceptance. If cloud lacks licensing/device, explicitly leave these unrun and provide exact user steps.

## Execution evidence

Native scene tests cannot run in this unlicensed cloud Unity installation. Tests are written before behavior changes and compiled against real Unity DLLs; pure visual/content tests run under .NET. This limitation does not count as native RED/GREEN or Pixel success.

Review: three Important findings corrected in source (shared walking destinations, two-line narrative height, separate error/retry controls); native UI regressions remain unrun. Current compiled inventory: Edit139 / Play135; Core125 and Presentation17 execute successfully under .NET.
