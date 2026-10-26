# Independent native image review — fountain enlargement and short show

Reviewed 2026-10-26. Read-only product review under `superpowers:requesting-code-review`; no product edits, further agents, Unity/license attempts, or full CPU runs.

**Verdict: eligible to proceed to verification on Pixel using the SAME-RUN town QA APK, after its binary SHA is checked. Fountain adoption remains pending.** The larger landmark and ordinary water are visibly stronger than the supplied actual prior version. The show is distinct at native size. I found no new Critical or Important static scenery/HUD regression that warrants another source change before that device check. Static captures do not establish that the motion feels natural.

Counts: **Critical 0; Important 2 carried acceptance gaps; Minor 2 (one new appearance concern, one inherited).** Important items below prevent final acceptance, not the proposed device verification.

## Evidence and comparison boundaries

- Current run: `20261026-111414-78cde31d`; source `5edfa5db37f1386b811713b43c4833b4cea451ab`; fingerprint `1fa608c9c6242e8e8be28efcfa55bdc5b67d014ba380a5ebfd8e191dd4a1219e`.
- Read the latest repository `AGENTS.md`, `docs/fountain-show-review-2026-10-26.md`, raw comparison JSON, and `integrity-verification.json`. The integrity record reports 162 new native 720×1280 PNGs, 3 prior originals, 185 raw files, all capture hash/size/decode checks passing, and native Edit 144/144 + Play 225/225, all 369 Passed with no failed/skipped/inconclusive cases. I did not rerun those tests. Both APK identities/logs agree according to the supplied integrity record; neither binary nor Pixel evidence was supplied.
- Inspected all new images on private sheets `inspection/all-01.jpg` through `all-07.jpg`, the three full Before/normal/show comparison sheets, and selected full-resolution native files. Detailed checks included native normal and show OFF/ON pairs, all three views, prelude/recovered, four phase frames, fountain character-depth views, East canopy, North edge, street/boundary and district overview captures. No images were embedded in chat.
- Independently recomputed SHA256 for the twelve East/West/North/South Lv1/3/6 overview pairs against `References/Review/fountain-water-2026-10-26-candidate/native`: all twelve match byte-for-byte. Independently checked the three `34-fountain-*-current-before.png` files against the prior `33-fountain-*-after.png`: all three match. These Before originals belong to `8aa7a14264c3bda3050a9e794c576f6af11535c9` / `20261026-093151-b7d557b1`, not the new run.
- Prior-version versus current-version sheets compare actual native renders at different capture times. They are not synchronous controls. The new `33-*-before/after` and `34-*-show-off/show` pairs are the causal controls for the added water layer. In particular, a show-OFF image still has enlarged artwork and original water at the show's instant; it is neither the old version nor a normal-mode capture.

All coordinates below refer to the original 720×1280 PNG, origin at top left. Difference bounds are half-open `[x0,y0,x1,y1)` and compare decoded RGB.

## What the native images support

**Ordinary fountain and landmark size.** The supplied 14% artwork enlargement is visible in all three actual prior/current comparisons. It gives the basin more presence without making it dominate the street. In `33-fountain-behind-after.png`, the sculpture, small upper bowl, falling streams and lower pool remain aligned; the pool is clearly contained by its stone rim. The brighter water, falling stream edges and surface highlights remain in the original blue/white palette and fit the flowered, soft town artwork. The normal water is an observable improvement over the previous very weak addition, although live flow still needs verification. The front-facing character covers much of the basin in the front and overview compositions, so the unobstructed behind view is useful evidence.

The circular paving and side routes remain visually legible around the enlarged artwork. I see no moved bench, flowerbed, lamp, road, or HUD in the causal controls. This is an appearance assessment, not a test of the usable walking area or touch targets.

**Show and containment.** Three taller blue streams are plainly distinguishable from normal water at native size, aligned with the same fountain and returning into its pool. The show reaches higher into the plaza without reaching the top notice/HUD or adjoining benches. My recomputation of the same-instant added-layer OFF/ON images agrees with the integrity record:

| View | Ordinary OFF/ON changed pixels | Show OFF/ON changed pixels | Show difference bounds |
|---|---:|---:|---|
| overview | 923 | 5,061 | `[299,690,421,915)` |
| front | 963 | 5,056 | `[299,481,422,706)` |
| behind | 1,741 | 6,387 | `[299,631,421,857)` |

Ordinary values and zero outside-projection differences are recorded in `raw/fountain-water-comparison.json`. Show metadata separately records 2,344 / 2,361 / 2,345 changed jet pixels above normal UV .84, outside-projection 0, and unchanged clock, original-water phase and state. The 33,132 / 40,507 / 14,278 normal-versus-show aggregate differences include other temporal changes (expressions, name visibility and text/edge pixels); I do not attribute those totals to the show. The overview aggregate even has full-frame-width bounds.

`34-fountain-overview-prelude.png` and `34-fountain-overview-recovered.png` look like ordinary fountain states without a stranded tall jet or clipping artifact. The four `33-fountain-phase-*.png` frames show small surface/stream changes without visible alignment drift. They cannot establish interpolation, exact event timing, or a smooth recovery; original water also changes phase.

**Characters and HUD.** `34-fountain-front-show-off.png` versus `34-fountain-front-show.png` leaves Momo's eyes, face silhouette, dress and name perceptually clear in front of the fountain. This is not a claim of identical character pixels: small RGB changes exist within broad face/edge rectangles. `31-field-fountain-front.png` similarly keeps Miel readable. In the far show pair, the streams correctly appear over Momo's body rather than disappearing behind it. Both eyes remain readable, but the central stream crosses the mouth and torso (M1). The normal far-side pair and `31-field-fountain-behind.png` still show the inherited body-occlusion evidence gap (I2).

Across the sheets I see no broad new scenery/UI degradation in the walking, street, boundary, growth, daily, book or Merge captures. Several captures differ temporally, and unchanged screenshots do not prove unchanged live interaction. The twelve hash-identical noncentral overview images provide stronger evidence for those fixed compositions than a visual impression alone.

## Critical

None found in this native image review.

## Important

**I1 — Inherited East canopy can conceal the character; device interaction acceptance is still missing.** `raw/31-field-East-edge.png` retains Miel's partly exposed crown/head near approximately `x610–719, y755–850`, while pink canopy hides the face/body/name. The new file is hash-identical to the prior candidate. This is not caused by the fountain. The screenshot cannot prove that a player can locate, tap and drag the hidden character back onto the visible path. Verify on Pixel from the existing save without deleting scenery or narrowing navigation by inference.

**I2 — Inherited normal far-side body occlusion and live depth transitions remain unproved.** In `raw/33-fountain-behind-after.png`, Momo's body is above the upper bowl, while the fountain overlaps the name area around `x283–438, y710–750`. `raw/31-field-fountain-behind.png` has the analogous Miel arrangement around `x283–438, y912–957`. These stills do not show a character's body passing behind the solid fountain. The new show pair does prove additional water over the far character, but that does not establish natural solid-art/body occlusion during movement. On the same-run Pixel build, walk front → left/right → far side → front during normal water and show, with the NPC also moving. Check face/name readability and smooth order changes. This is an inherited acceptance gap, not a newly demonstrated sorting bug.

## Minor

**M1 — New show streams can look rigid, particularly over the far-side face.** In `raw/34-fountain-behind-show.png`, the nearly straight central cyan/white line at about `x357–364, y631–745` crosses Momo's lower face, bow and dress. Its OFF control isolates this change: 85 RGB pixels change in the mouth-centre rectangle `[355,637,366,651)`, while sampled left/right eye rectangles `[337,615,352,635)` and `[373,615,388,635)` have zero changed RGB pixels. The matching front view makes the repeated straight segments especially visible against the tan paving above the basin. The overall show reads as water and is stronger than normal; its uniform narrow lines look more geometric than the painted fountain. This is a real appearance concern to judge in live motion, not a reason to claim the complete fountain is worse. Reject or adjust within fountain scope if it looks like rods/strings on Pixel or repeatedly distracts from the character. Do not infer that the entire face is hidden.

**M2 — Inherited North rim/bed repetition remains.** `raw/31-field-North-edge.png` retains the thin exposed upper green margin and repeated upper bed edging. This file is hash-identical to the prior candidate, so it is not a new fountain regression and does not justify changing the frozen district base during this task.

## Eligibility and declined claims/actions

Proceed with the generated town QA artifact for this run, `com.mofumachi.merge.qa.townlife`, expected SHA256 `14594390f993d40512e108e4d1487c6e107c94e9fe57c392e6b285ccb11b899a` and 79,027,285 bytes, after independently hashing the actual binary. Install with the existing save-preserving `install-r` workflow on Pixel `99RAY1BELH`; the metadata/log match alone is not an independent APK hash check or proof of the installed identity.

The device gate must establish visible ordinary flow at normal viewing size; approximately three minutes of eligible central-view time; two-second prelude, six-second show and one-second recovery in practice; menu/pause/offscreen behavior; live character ordering and touch/drag return; ten-minute operation, measured frame pacing/FPS if available, battery temperature/thermal evidence, sound, and restart/save preservation. Unavailable measurements must remain unmeasured. Native frame counts and vertex/update limits do not prove device performance or heat.

Declined: final fountain adoption, user/device approval, all-five-district live acceptance, proof of unchanged navigation or input, exact timing/interpolation from stills, a claim of no FPS loss or heat increase, independent installed identity, 2.5D work, Merge changes, later-stage work, main integration, destructive save actions, additional source edits, full CPU reruns, Unity license retries, or a rebuild solely for this review. The five-district base remains frozen. The next useful step is the bounded same-run Pixel verification, not another static-image claim.

Codex／AI側から見た次の改善候補: M1 only if the live show looks rigid or obscures expression (effect: more natural/cute water; difficulty: small fountain-only tuning; before release). This is a proposal, not an implemented change.
