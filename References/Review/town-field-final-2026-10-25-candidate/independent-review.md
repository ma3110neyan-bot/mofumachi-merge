# Independent native visual review — town field final, 2026-10-25

## Scope and method

This is the single independent, read-only native visual review requested by the coordinator. I did not dispatch another reviewer, run Unity, change the repository/index/HEAD, or inspect a Pixel device. Private contact sheets/crops and this report are confined to `/workspace/runtime/town-field-final-native-2026-10-25/`.

- Previous actual native set: `References/Review/field-stability-2026-10-25-candidate/native`, source `871808c`.
- Current actual PNG set: `extracted/`, runtime implementation `a20ae99`, subsequent test-only fix `43a2764`; review checkout HEAD `9e7855a6fff5e0d8d21fe50b4a58c5c32dc445aa` contains later documentation.
- Requirements read: `docs/superpowers/specs/2026-10-25-town-field-final-design.md`, corresponding plan, current `AGENTS.md`, and previous `native-review.json`.
- Viewed all 144 PNGs on private thematic contact sheets. Opened all six new growth PNGs, the fountain pair, all five edge positions, the East corner pair, and representative return/UI frames at native size. Compared previous native North/West/East edge and far-side fountain originals directly. Inspected both changed Merge frames side by side.
- Independently compared same-name file bytes: 122 unchanged, 16 changed, six added. All 15 district Lv1/3/6 overview PNGs and all 22 `30-boundary-*` PNGs are byte-identical to the previous verified native set. The 16 changes are the 14 `31-field-*` frames plus `01-merge-start.png` and `29-merge-landed.png`.
- Read the four presentation runtime diffs, the relevant progression/capture test, `TownGrowthView` highlight implementation, and the existing growth screen/scroll container to understand what the evidence proves. This is not a second full source audit.

## Strengths and observed results

**The original five districts retain their art, density, growth differences, and open walking appearance.** The 15 overview frames preserve the original flowers, buildings, trees, benches, paths, characters, names and HUD. Their byte identity supplies stronger regression evidence than a contact-sheet resemblance. No new narrowing of visible paths or simplification of approved scenery was observed.

**The targeted exterior is improved.** In `31-field-North-edge.png`, the previous broad plain ground above the straight boundary around y264 is now largely planted; the visible planted rim curves rather than ending at that old horizontal line. In `31-field-West-edge.png`, the old exposed outer strip at x0–32/y954–1087 is filled by planting. The East extreme view also retains a continuous dense exterior. I did not see a new hard break through an existing walking court. These observations do not certify every seam or every possible camera position.

**The growth comparison is legible and shows actual changes.** In all three comparison PNGs, the five buttons occupy approximately x32–687/y266–361; each two-line district label can be read, and the selected blush button remains distinguishable. The before/after image panels, short change label and level transition fit without truncation:

| Actual PNG | Selected district | Visible addition / label |
| --- | --- | --- |
| `32-growth-1-to-2.png` | Central | Bench appears in the right panel; `花・ベンチ`, Lv1 → Lv2. |
| `32-growth-2-to-3.png` | North | Bunting/flower decoration appears across the right panel; `花飾り・届け物`, Lv2 → Lv3. |
| `32-growth-5-to-6.png` | South | Flower arch visibly surrounds the right gazebo entrance; `花アーチ・花飾り`, Lv5 → Lv6. |

The reload frames show South at Lv2/70 coins, Lv3/110 coins and Lv6/330 coins. Lv3 adds roof flowers and small lamp/flower details; Lv6 adds the large entrance arch. The level/HUD and scenery agree with these milestones. The new test uses generation, merge, acceptance and delivery before capture, then scene reload and all-five decoration comparison (`ExperienceCaptureTests.cs:35`, `:52`, `:71`, `:78`). This is stronger fixture evidence than merely assigning a sample town level, while remaining an isolated fixture.

**No new book, HUD or Merge graphics regression was found.** Existing book/title/music/menu/quest images are among the unchanged images. The changed `01-merge-start.png` pixels are limited to bottom action text at x77–668/y1097–1159; the current text is sharper. The changed `29-merge-landed.png` pixels are limited to the teapot landing area x566–678/y422–510. That difference is consistent with a landing-animation instant; it does not show a board/layout/art downgrade. There is no Merge runtime change in the reviewed implementation. The partly visible book CTA at the bottom of the new growth cards lies within the existing clipped, scrollable paper viewport (`UIWidgets.cs:28`); the screenshot alone is not evidence of a newly blocked button.

## Issues

### Critical: 0

No critical visual regression identified.

### Important: 2 acceptance items; neither establishes a new runtime regression

**I1 — Selected hero remains difficult to identify through the retained East canopy. Inherited, unresolved usability acceptance.**

- `31-field-East-edge.png`: only the upper tuft/ear area is visible near x603–710/y755–817; face, body and name are hidden by the flowering tree.
- `31-field-east-corner-out.png`: the hero is substantially hidden at the right edge around x600–719/y763–954, with a fragment of the nameplate visible below the canopy.
- This matches the earlier native limitation and is consistent with natural retained scenery ordering. The new outer planting did not establish this problem, and these PNGs do not establish that the former navigation deadlock recurs.
- User impact: a player may not see the selected character well enough to find or operate it at these locations. Raycast success is not proof of visible character pixels through non-raycast scenery.
- Required disposition: verify identification and both tap/drag return on the same candidate APK on Pixel. If normal operation is impaired, keep acceptance held and make a bounded correction supported by that result; these stills do not justify deleting art, narrowing the floor, or making characters always foreground.

**I2 — The closer far-side capture still does not visibly demonstrate fountain-over-body occlusion. Acceptance-evidence gap, not a proven sorting bug.**

- `31-field-fountain-front.png` clearly shows the hero over the fountain in the front position, especially x300–419/y759–912.
- In `31-field-fountain-behind.png`, the hero body/feet end at approximately y912, while the first visible fountain droplet begins near y920. The nameplate spans roughly x284–436/y916–958 and is the part overlapped by the fountain at x346–375/y920–956. There is still no visible body silhouette crossing.
- The updated `.441` position is closer than the previous photograph and improves evidence, but the rectangular bounds assertion at `ExperienceCaptureTests.cs:256–260` can pass without visible opaque body pixels intersecting the fountain. Sibling order and overlapping bounds do not substitute for the requested visible crossing.
- User impact remains unproven from this still: it cannot establish a natural front/back transition while walking around the fountain, for the hero or NPCs.
- Required disposition: carry actual front → side → far side → front crossing to Pixel and observe the transition. Do not describe this PNG as conclusive far-side body-occlusion proof, or change production depth solely to manufacture a photograph.

### Minor: 1 visual carryover / residual

**M1 — Some plain exterior ground and repeated planted rims remain visible at the North extreme. Improved from the previous capture; not a new density downgrade.**

- `31-field-North-edge.png`: light green ground is still visible beside/behind the upper overlays, approximately x0–119/y115–168 and x600–719/y123–168. The repeated large flowerbed rims around y264–310 remain visually regular.
- The broad empty cap and its former straight cutoff are substantially improved, so this should not be reported as “unchanged.” The residual has low impact because it is peripheral and partly covered by formal HUD.
- Carry normal-motion prominence and motif repetition to Pixel. Do not claim all exposed ground/material joins have disappeared.

## Evidence qualifications and declined to judge

- **Pixel touch and East return usability:** no device input or device images were supplied. Native input assertions and these stills cannot resolve canopy concealment during real touch.
- **Live fountain depth transitions, NPC crossings and overlap recovery:** still frames do not show the full movement or temporal ordering; I2 remains open.
- **Transient growth glow/pulse:** a still cannot establish its duration or prominence. The fixture checks `IsHighlighting` (`ExperienceCaptureTests.cs:64`), and `TownGrowthView.cs:84–90` starts the existing short scale pulse for its growth layers. That flag does not establish a visible pulse on the focal North/South master-scenery bunting/arch. I do not mark transient emphasis visually cleared.
- **Every district/transition comparison label:** the native test iterates all five buttons and checks their labels, but the six PNGs show only Central 1→2, North 2→3 and South 5→6 plus South reloads. The truthful North/South Lv2 `次はLv.3` label is supported by source/test intent, not directly displayed in these PNGs.
- **Device performance, FPS, temperature, audio, crashes or 600-second continuity:** no Pixel session was observed. Additional sprite/mesh counts do not prove acceptable device cost.
- **Actual APK binary hashes and installed identity:** neither APK binary was received by this review. Raw metadata/log success and hashes must not be called independently recomputed binary hashes.
- **Real player-save update retention/restart and device delivery progression:** the new evidence is an isolated fixture. Its reload test is useful, but is not a Pixel save migration or real-device restart result.
- **Full reward/Core correctness audit:** the bounded runtime diff does not change these systems; I inspected the progression evidence only, not the entire unchanged Core implementation.
- **Formal visual adoption, completed field freeze, later stages, main or merge readiness:** outside this review's authorization and blocked on the required device/user acceptance.

## Assessment

**Ready for conditional Pixel verification? Yes.** The new native visuals preserve the accepted five-area scenery and improve the targeted exposed exterior without a visible density or walking-appearance regression. All six new growth images are usable acceptance evidence for their shown states.

This is permission to perform the remaining same-candidate device verification, not native visual clearance, device success, formal freeze, or main/merge approval. Preserve I1 and I2 explicitly in the Pixel checklist, also checking all five edges, actual growth/next-level labels, brief emphasis, update/save restart, and the required continuous session. There is no image-based reason here to request another rebuild solely because the evidence was received.
