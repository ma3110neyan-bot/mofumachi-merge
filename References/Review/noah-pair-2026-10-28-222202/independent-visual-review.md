# Independent native visual review — Noah

**Verdict: needs more Noah-only work before being presented as the representative candidate for full QA / APK / Pixel review.** The supplied native captures preserve the original character well and show some additional movement. They do not demonstrate a clearly better 2.5D result at ordinary game scale. This is a visual judgment about the stated goal, not a claim of a technical test failure.

Reviewed 2026-10-28. This review made no source changes and ran no tests. The report is the only authored artifact.

## Evidence inspected

- All six `37-noah-{idle,walk,turn,stop,talk,reaction}-before-left-after-right.jpg` comparisons.
- All twelve corresponding full-game Before-reconstructed / After PNGs at 720 × 1280, plus `37-noah-talk-ui-natural-before.png`.
- Every native frame PNG `37-noah-frame-00.png` through `37-noah-frame-47.png`, individually with `view_image`. These frame PNGs are character-area captures, approximately 596 × 307; the full-game PNGs provide screen-scale context.
- The three private sequence sheets, used only as inspection aids.
- The original `CharacterBodyAtlas.png` at `/workspace/mofumachi-merge/Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png`, including Noah's original face, red/purple hat and jacket, blue bow, cream belly, raised paw and curled tail.

Source context supplied for this review: capture source `5102e9a`; Noah runtime code unchanged from `1d11629`. No independent source audit or pixel-difference verification was performed here. The received success is a single Unity method, 1/1; it is not full QA, APK, Pixel-device acceptance or adoption.

The reconstructed Before uses the same remaining reaction clock, despite the legacy 0.65 s versus candidate 1 s duration; it is not an equal-elapsed comparison of the original reaction behavior. Its resident captions are pinned to the natural After caption state. The natural Before talk capture was inspected separately. These comparisons therefore support a controlled visual comparison, not an unbiased timing or caption-improvement claim.

Frames 00–39 cover the continuous first 4 s with manual 1/30 advancement sampled every three steps. Frames 40–47 are a separate greeting segment around 14.333–15.033 s. I treated that boundary as a cut. This evidence cannot establish real FPS, device frame pacing, or temporal smoothness between samples.

## Critical

None observed in the supplied views. I did not see a detached limb, visible mesh tear, broken face, severed tail, missing clothing section, or major size jump. This is limited to the captured poses and samples.

## Important

1. **The requested improvement over 2D is not yet clearly visible at game scale.** Noah is roughly 150–160 pixels tall in the 720 × 1280 game view. Idle, turn and stop comparisons mainly show a small lean, contour displacement or pose offset. The walking frames add body bob and alternating foot-height changes, but the face and torso continue to present almost directly to the camera. Across the reversal around frames 24–26, the change in screen travel is clearer than a change in Noah's apparent facing. The result still reads predominantly as a front-facing illustrated character moving sideways. There is some animation improvement; I would not describe this evidence as a convincing 2.5D turn or an obvious upgrade visible without close comparison.

2. **A distinct wave and Noah's own talking performance are not established.** The original art already has the viewer-left paw raised. That remains the main pose throughout idle and greeting frames 40–47. Small movement is present, but I cannot identify an unmistakable wave arc or a clearly distinct greeting gesture at normal screen scale. The talk and greeting views name Momo as the speaker and say “こんにちは、ノア♪”; Noah is listening/reacting. These views show Noah under the conversation UI, not a demonstrated Noah-speaking mouth performance. A labeled “talk” image alone does not resolve that gap.

These are reasons to continue focused Noah work and capture the relevant behavior before promoting this as the representative candidate. They are not reasons to change the frozen environment or other residents.

## Minor / observations to retain for the next capture

- **Ground contact is visually weak.** Feet remain near the paving and I saw no gross floating or penetration. A small foot lift is readable in frames such as 10, 12, 16, 27 and 31. However, a contact shadow is difficult to distinguish from the warm paving at game scale, and the nameplate immediately below the feet further limits the grounding cue. I cannot certify a convincing planted-foot cycle or absence of foot sliding from these sampled images. I did not establish a new regression versus Before.
- **The dialogue panel hides the top of Noah's head.** In the talk/reaction full-game captures and greeting frames 40–47, its lower edge overlaps the hat/ear area. The face, bow, body and feet remain readable. The natural Before talk capture also has this overlap, so it is an existing presentation constraint, not evidence of a new Noah regression. It also limits inspection of the head during the greeting.
- **No obvious rubberiness, but little separation of actions.** Head, clothes and tail stay coherent in the supplied samples. The visible deformation is mild; idle breathing, stopping and greeting have similar silhouettes. There is no obvious extreme squash or stretched facial feature, but the small amplitudes also make those states hard to distinguish at game scale.
- **Closed-eye expressions are visible.** Frames 18 and 20 show brief closed-eye happy expressions separated by an open-eye sample; greeting frames 43–45 sustain a closed-eye smile. This establishes an expression change. It does not establish a smooth eyelid transition or natural blink timing between captured frames.

## Behavior-by-behavior result

| Requested behavior | What the evidence supports |
| --- | --- |
| Original-art identity / static appearance | Preserved. Face, ears, hat, bow, jacket, belly and tail remain recognizably Noah; size fits the existing scene. |
| Walking / starting | Additional bob, lean and foot-height variation are visible in frames 08–31. The lateral movement remains predominantly front-facing. |
| Turning | Reversal of travel is visible; a clearly readable facing/depth change is weak in the turn comparison and frames 24–26. |
| Stopping | A small pose settle is visible around frames 32–35, followed by near-idle frames 36–39. The visual distinction is subtle. |
| Blink | Closed-eye expression samples exist; continuous blink quality is unproven. |
| Noah talking | Not demonstrated by this dialogue: Momo is the named speaker. |
| Wave / greeting | Friendly reaction and small movement exist; a distinct wave is not clearly readable. |
| Grounding | No gross separation from the ground observed; contact-shadow and planted-step convincingness remain weak/unproven. |
| Sorting / occlusion | Conversation UI appears in front of the head as it does in natural Before. No sufficiently diagnostic foreground-prop or character-crossing case is shown to establish world sorting correctness. |

## Recommendation

Keep the work confined to Noah and preserve the original artwork. Make facing during reversal and the greeting/wave clearly readable at the existing in-game size, and obtain an actual Noah-speaking example. Retain head/clothing/tail coherence while improving these cues; do not enlarge or redesign the character simply to make the evidence more dramatic. A relevant foreground-overlap example would also make sorting review concrete.

I would **not yet call this ready as the one representative full-QA / APK / Pixel candidate against the user's “clearly better than 2D” goal**. Further Noah-only iteration and focused native evidence should precede that promotion. Final visual acceptance belongs to the user. This report authorizes no adoption or rollout to the other five residents.
