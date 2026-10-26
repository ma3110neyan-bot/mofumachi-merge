# Independent native visual review — fountain candidate, 2026-10-26

## Verdict

**Eligible for conditional verification on Pixel using the APK from this same run.** I found no demonstrated new scene, character-design, visible character-occlusion, path-appearance or HUD regression in the supplied native images. The fountain addition stays within the fountain projection and preserves the original fountain/water appearance. Its isolated contribution is extremely subtle at 720×1280; actual perceptibility and natural motion require the device review.

This is **not fountain adoption or Pixel acceptance**. Keep the two inherited Important acceptance items below open. The user's five-district formal-base-candidate freeze remains in force; this review does not reopen the field layout or authorize 2.5D, Merge changes, later stages or main integration. Receipt/review of these images supplies no reason for a new rebuild by itself.

## Scope and method

- Independent, read-only review requested under `superpowers:requesting-code-review`; no further reviewers dispatched and no product, test, reference-image, index or HEAD changes made. Only this runtime report was written.
- Current run: `20261026-093151-b7d557b1`; native originals: `/workspace/runtime/fountain-native-review-2026-10-26/raw/`.
- Current source reported by both build manifests: `8aa7a14264c3bda3050a9e794c576f6af11535c9`; fingerprint `97e700b2d5bc5e8298b0dd062fa49fd994ec138acb5e0895a931420c2d4e0d8f`.
- Previous field baseline: `/workspace/mofumachi-merge/References/Review/town-field-final-2026-10-25-candidate/native/` (144 PNGs, source `43a2764`). Applied the newest instructions at the top of repository `AGENTS.md`, the fountain review procedure, and the prior independent visual report.
- Viewed all 154 images on the seven private inspection sheets. Opened all six new OFF/ON originals and all four phase originals at native size, all five Lv6 district originals, the inherited East concealment and North-edge originals, the existing fountain-front/behind originals, and the changed daily/Merge originals against the baseline. Inspected the supplied enlarged fountain crops as aids only; amplified difference images are not product appearance.
- Independently computed RGB differences for all 144 shared names, all three new OFF/ON pairs, and the four phase samples. The coordinator's integrity check reports all 154 originals pass SHA/bytes/720×1280/decode; its baseline file comparison reports 87 byte-identical, 57 changed and 10 new PNG names. My shared-name RGB scan independently found those same 57 changed images.
- Read the bounded capture routine at `Assets/Mofumachi/Tests/PlayMode/ExperienceCaptureTests.cs:95–156` to distinguish the synchronous comparison from time samples. I did not run Unity or inspect a device. The coordinator separately verified native XML results of Edit144/144 and Play225/225, all Passed with zero skipped; those test results are not substituted for visual or device acceptance.

## Native observations and exact evidence

Coordinates below use the 720×1280 original image with origin at the top left. Difference bounding boxes use half-open `[x0,y0,x1,y1)` bounds.

### Isolated new layer

The three `33-fountain-*-before/after.png` pairs compare the same native state with only the new layer OFF/ON. They retain the original fountain, original water, camera, characters and save state. They are not a reconstruction of the old APK.

| Pair under `raw/` | Changed RGB pixels | Bounding box | Pixels with max RGB-channel delta >8 | Strongest pixel, before → after RGB |
| --- | ---: | --- | ---: | --- |
| `33-fountain-overview-before.png` / `33-fountain-overview-after.png` | 39 | `[319,814,401,909)` | 13 | `(338,822)`: `[53,217,251]` → `[108,224,252]` |
| `33-fountain-front-before.png` / `33-fountain-front-after.png` | 36 | `[317,605,401,696)` | 17 | `(336,616)`: `[76,225,246]` → `[139,234,249]` |
| `33-fountain-behind-before.png` / `33-fountain-behind-after.png` | 169 | `[314,755,408,850)` | 108 | `(329,842)`: `[26,163,251]` → `[187,215,241]` |

These independently reproduced counts agree with `fountain-water-comparison.json`, which records zero changed pixels outside the projected fountain and unchanged state for each pair. The new highlights sit on the existing tier/waterfall and pool region; I see no displaced spray, large glow, broad translucent veil, altered stone basin, changed flowers or new marks on nearby paving. The original bright blue pool, white cascades and painted ripples remain visually dominant.

In the front originals, Momo continues to appear over the basin with readable face, clothing and nameplate; there is no perceptible new water line obscuring her. Do not report all character pixels as byte-identical: some semitransparent character-edge/face pixels differ by only one or two RGB levels. For example, front `(338,689)` is `[28,18,27]` → `[29,19,27]`. This is not a visible character-readability regression in the originals.

### Four time samples

`33-fountain-phase-00..03.png` show phases `0.1`, `0.4`, `0.700000048`, `1.00000012` in the supplied metadata. Relative to phase00, phase01/02/03 change 1,656 / 1,665 / 1,521 RGB pixels, within `[290,746,431,851)`, `[291,747,429,850)`, `[294,747,426,851)` respectively. All observed changes remain around the fountain water; character posture/position, surrounding art and UI stay visually consistent.

**Those larger temporal deltas do not isolate the new effect.** The capture code advances both `water.AdvanceWater(.1f)` and `originalWater.AdvanceWater(.1f)` at line142. The original animated water contributes to these differences. The four stills demonstrate sampled native changes, not the new layer's standalone animation quality, continuity or device motion.

### Five-district preservation and other changed snapshots

- The 12 East/North/South/West Lv1/Lv3/Lv6 overview files are byte-identical to the approved field candidate. Their buildings, flowers, growth adornments, paths, character designs, names and HUD have no image change.
- Each of `04-town-lv1-Central.png`, `04-town-lv3-Central.png`, `04-town-lv6-Central.png` has 184 changed RGB pixels, all inside `[316,507,408,602)` over the fountain; everything outside that region is RGB-identical. The full-size Lv6 originals retain the approved density and open courts across all five districts. No new narrowing, simplification, palette change or scene re-layout is visible.
- Of the 57 changed shared-name snapshots, 52 have difference bounds aligning with the visible full or clipped fountain. Examples: East join `[0,422,15,488)`, West join `[710,429,720,488)`, North-left boundary `[674,1003,720,1092)` and the five return frames' fountain behind the existing conversation panel. These corner changes should not be misreported as new boundary seams or moved art.
- The other five differences are separate snapshot content/timing: `11-today-start.png` (20,082 pixels in `[71,3,385,572)`), `12-today-ready.png` and `13-today-flower-collected.png` (13,721 each in `[84,107,385,526)`) show the daily featured companion/location changing from Noah/South to Momo/Central while retaining the same card layout and typography. `29-merge-landed.png` changes 3,587 pixels in `[566,422,681,513)` only over the landed teapot, max channel delta20, consistent with a slightly different landing-animation sample; the board and item/cell are retained. `26-cardinal-South-join.png` differs by 47 pixels in `[412,107,486,124)`, max channel delta4, a visually negligible top-edge/title-region difference. None demonstrates a new field or Merge visual regression. Do not call all 57 differences solely new fountain pixels.
- The unchanged `31-field-East-edge.png`, `31-field-east-corner-out.png` and `31-field-North-edge.png` preserve the inherited concealment/peripheral-polish evidence exactly. The six `32-growth-*` PNGs are also byte-identical; no new growth acceptance claim follows from that identity.

## Critical / Important / Minor

### Critical: 0

No critical visual regression identified.

### Important: 2 inherited acceptance items; 0 newly demonstrated Important visual regressions

**I1 — East canopy still conceals the selected character. Existing field localPolish / device-usability acceptance.**

- `31-field-East-edge.png`: the hero's tuft/ear area around x603–710/y755–817 is visible, while the face, body and name are hidden behind the flowering tree. `31-field-east-corner-out.png` shows substantial concealment around x600–719/y763–954, with only part of the nameplate visible below the canopy.
- Both files are byte-identical to the previous approved field candidate. This is not a new fountain regression and does not establish recurrence of the old navigation deadlock.
- Carry identification plus tap/drag return to Pixel on this candidate. Native raycast/return assertions do not establish that a person can see and select the hidden hero. These stills do not justify speculative scenery/depth/navigation changes.

**I2 — Far-side body occlusion / live crossing remains unproved. Existing depth-acceptance gap.**

- In `33-fountain-behind-before.png` and `33-fountain-behind-after.png`, Momo's feet finish near y709 and the nameplate occupies about x284–436/y711–752. The fountain droplet overlaps the nameplate around x348–374/y720–752, while the opaque body silhouette stays above it. The new layer's changed pixels begin at y755.
- The existing `31-field-fountain-behind.png` similarly overlaps the nameplate (about x284–436/y916–958), with the body/feet ending around y912 and the first droplet pixels near y920. `31-field-fountain-front.png` does show the front character visibly over the fountain.
- Therefore the behind stills and successful sibling-order assertion cannot establish natural fountain-over-body occlusion while the hero/NPC crosses. Observe front → side → far side → front live on Pixel, including overlap recovery. This is a reason to retain the device check, not a demonstrated sorting fault requiring a guessed code change before that check.

### Minor: 2 observations (one new-effect readability concern, one inherited localPolish item)

**M1 — The additive effect is barely distinguishable in natural-size stills. New-effect readability must be judged live.**

- The isolated OFF/ON contribution is only 39/36/169 RGB pixels in overview/front/behind, with only 13/17/108 pixels exceeding an 8-level maximum channel delta. In the front views the foreground character also legitimately hides much of the pool.
- At native size I cannot confidently distinguish all requested thin streams, light splash, ripples and glints as separate new features without the pair comparison/crop. This may be appropriate restraint over the detailed original artwork; it does not prove that live motion is too weak.
- On Pixel, verify that the water can be noticed moving without magnification while the original scenery and faces remain clear. Do not use an amplified diff, vertex count, or the original water's phase changes as evidence that the new contribution is perceptible. No speculative strength increase is recommended from these stills alone.

**M2 — North peripheral ground / repeated planted rims remain. Inherited localPolish.**

- `31-field-North-edge.png` is byte-identical to the baseline. Residual light-green ground near x0–119/y115–168 and x600–719/y123–168, plus repeated large flowerbed rims around y264–310, remain at the extreme camera position.
- This is the prior low-impact residual, not a new loss of density. It does not justify reopening the frozen field structure; judge normal-motion prominence within the already permitted bounded polish.

## Same-APK handoff and declined judgments

The raw build manifests report the following artifacts from the reviewed source/fingerprint:

| APK / app ID | Reported bytes | Reported SHA-256 |
| --- | ---: | --- |
| `town-life-qa.apk` / `com.mofumachi.merge.qa.townlife` | 79,021,005 | `2971b220edc77a7605ded12295bf2b7cbf06b83cbdcca9a86d03175694498cf0` |
| `town-growth-check.apk` / `com.mofumachi.merge.qa.growthcheck` | 79,021,029 | `5dd9a110642a567101bf2d83f278172108537629121535738a593216981cfceb` |

Neither APK binary was supplied to this reviewer, so I did not independently recompute these binary hashes or verify installed identity. Conditional Pixel progression means first matching the local binary hash to this run, then the authorized save-preserving update of the existing town QA ID on Pixel `99RAY1BELH`. The old 210356 APK cannot validate this effect. No uninstall/save clear or additional stage is implied.

I decline to judge the following from these static native images:

- New water's live continuity, smoothness, 30Hz cadence, autonomous motion, aliasing/flicker, perceptibility on the Pixel display or whether the user prefers it.
- Live hero/NPC fountain depth transitions, concealment recovery, East visibility/tap/drag usability, or full touch behavior.
- Actual FPS, GC during a device session, thermal impact, audio, 600-second stability, crash/freeze absence or background/pause/restart behavior. A mesh count/update cap is not a measurement of Pixel cost.
- Real device update-save retention, restart recovery, delivery progression, every district's growth selector, transient growth emphasis or all initial-growth conditions. The prior fixture images are unchanged, not fresh device proof.
- Binary SHA recomputation, installed APK identity, formal fountain adoption, completed device acceptance, 2.5D/Merge/later-stage approval, release readiness or main integration.

**Final assessment: conditional same-run Pixel verification is appropriate; fountain acceptance remains pending.** Preserve I1/I2 and M1 in that check, retain the frozen five districts, and use device evidence before deciding whether any fountain adjustment is needed.

Codex／AI側から見た次の改善候補: 今回はなし。The remaining actions are the existing bounded Pixel acceptance checks above.
