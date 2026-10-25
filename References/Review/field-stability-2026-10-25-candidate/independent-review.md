# Native field-stability review — 2026-10-25

Reviewer: independent read-only review, `/root/field_stability_native_review_oct25`.
Run: `20261025-153810-fa9b2be9`; runtime `871808c3971e18aad8ffbbd30a1ce7de40455ab4`.

Recommendation: eligible for **conditional diagnostic Pixel validation with the existing153810 APK**, after local SHA verification. Hold native visual-cleared, adoption and completion claims.

Reviewed all14 new frames at720×1280, relevant changed character/HUD/boundary frames, and runtime/test changes. Independently verified138 manifest SHA/size/decode checks,143/143 EditMode and223/223 PlayMode Passed, and all15 district overviews byte-identical to baseline. No art, density, walking-width or HUD layout regression identified.

Critical0; Important1 acceptance-evidence limitation:

- East visibility remains unresolved. `31-field-East-edge.png`, approximately x603–690/y755–812, exposes only Miel’s crown/ear tuft; foreground blossoms hide her face, body and name. `31-field-east-corner-out.png`, x600–700/y780–955, shows similar partial concealment. Consistent with natural canopy occlusion in retained scenery; evidence does not establish a newly introduced sorting, navigation or footprint defect. The strict test checks bounds and topmost EventSystem hits; raycast-false scenery can conceal pixels while those checks pass. Verify identification and tap/drag return on Pixel. No blanket scenery/nav alteration is justified.

Minor limitations:

- West-edge exterior ground at x0–32/y954–1087; North-edge exterior ground behind top overlays, ending around y264. Visible camera tradeoffs, not demonstrated return failures.
- Fountain-behind still has no hero/fountain silhouette overlap: hero feet around y905, fountain tip around y983. Front overlap and native sibling assertion/source support the rule; confirm actual far-side occlusion while crossing on Pixel.
- Existing material transitions and repeated perimeter flowerbeds remain. Do not claim all seams resolved.

Device FPS, heat, audio,600-second stability, save retention and real-device trajectories were not judged from these artifacts. Native fixed-step return success does not establish those outcomes. Reviewer changed no files.
