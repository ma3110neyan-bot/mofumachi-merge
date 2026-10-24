# Curved town boundary implementation plan

> **For agentic workers:** Use superpowers:executing-plans inline. Existing bounded user authorization and opt-out of per-file questions apply. One fresh whole-change review before handoff.

**Goal:** Blend the exposed road ends and North side seams into the existing planted banks without changing accepted centers or walking.

**Architecture:**4 narrow curved strips reuse existing West painting in a single UGUI batch below all existing perimeter plants. Pure layout is in the existing boundary layout file; renderer lives beside the existing boundary Graphic. No new resource or walking model.

**Tech Stack:** Unity6000.6.4f1 UGUI/C#9, .NET8 CPU tests, existing PS5.1 QA.

**Spec:** docs/superpowers/specs/2026-10-25-curved-boundary-design.md

## Global Constraints

- Preserve all original art, MasterLayout.Dressing, existing200 perimeter sprites, Core/nav/save/reward/book/music/characters/Merge. No new stages or main.
- Curves only at West/East lower edges outside South painting (absx>=.92), North side edges y>=1.23. Max.006world inside old painting, <=.16world outside.
- One existing West texture,4patches x17columns x4rows =272vertices, no raycasts/new colliders.
- Original354 lock/15JPEGs stay immutable. Historical fingerprint reconstruction remains restricted to old-approval callers.
- Before is frozen5a49 renderer reconstruction, not oldAPK/Pixel. Native and Pixel remain pending until actual evidence.

## Review Focus

- Exact old-edge UV and fully covered cut; no two-texture blur at contact.
- Curved outer contour rather than another straight boundary; finite UV/alpha/bounded vertices.
- No occupation/removal of accepted South art or walking plane; inner overlap samples same texture.
- Shared live/preview projection, depth below existing props and labels, no renderer count assumptions left stale.
- Current fingerprints bind new geometry/capture, metadata does not adopt the town or advance later stages.

### Task1: Boundary layout and renderer

- [x] Save bothPDF/receipt; inspect23pages (done), retain current5a49 native Before reference.
- [x] CPU reflection tests RED: old-edge exact UV/opacity, nonstraight outer shape/range, exclusion of core/walking/South art, finite texture coordinates/budget.
- [x] Add TownBoundaryCurveLayout.GetVertex(patch,along,row) returning (TownPoint World,TownPoint UV,float Alpha),4patches/17columns/4rows. CurvedDepth varies smoothly along patch; outer alpha0, exact old boundary row1.
- [x] Add TownBoundaryCurveGraphic in current Graphic file; bind/refresh with SouthGround in live/preview. Run focused then full CPU and6API assembly builds.

### Task2: Source-bound native comparison and QA

- [x] Change existing11-pair capture: toggle only curve, keep prior perimeter/entry active; remove old466 reconstruction helper. Before5a49, Aftercurrent, same state and camera. Keep124PNGs/222native cases.
- [x] Extend existing seamless native case with resource/raycast checks for curve; retain old ground/entry checks.
- [x] New candidate record keeps exact5original changed paths+4added cs/meta+1capture binding, current source hashes; keep historical original capture hash for approval reconstruction. Candidate state resets Unity/APK/Pixel false. Update only current capture schema literal5a49 (not eligibility thresholds).
- [x] Current/CRLF approval preflight and affected script tests; immutable art/walking/core hashes unchanged. Fresh whole-change reviewer, fix material issues; commit/push feature.
- [ ] Existing full-TownQa then actual images/APK/SHA and Pixel. Do not claim native/Pixel/final-base success in cloud. No fountain/2.5D/Merge yet.

## Ledger

Baseac46d3c, existingfeaturecheckout retained for Windows continuity (no main). Author chose existing-texture curved strips under user bounded authorization; previous vegetation-only fix left J1. Task1 source verified: RED4→GREEN4, CPU315/315, API6compile0warnings/errors, existing200placement code and allCore/resources unchanged. Task2 source verified: Before5a49onlycurveoff, native143/222inventory unchanged, current/historicaldigest separate,3relatedPSgates and409fileCRLF gate passed. Fresh whole-change review0Critical/0Important/0Minor. Native/Pixel aesthetics unavailable: keep acceptance false, hand off existing full QA. User latest sequence supersedes previousMerge-first.
