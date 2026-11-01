# Final diff review and disposition

Reviewed range: `fb172fea17210bf212bfcd0545de7debd7f8521f..66df0a0`, one fresh read-only reviewer. No critical findings. Three important findings received one correction pass; no repeat review requested.

1. Noah capture compared a neutral legacy snapshot with the post-tap greeting mesh. The restoration invariant now runs before the tap. A greeting cannot contaminate this same-state comparison.
2. Hero tapping recenters the camera and hid the right-hand fixture actors. The six-character collector now restores its camera before comparisons and each tap, and after hero input. Every actor's projected position must pass the existing tappable viewport margins.
3. Hero pointer-down destroyed speech before duplicate-click suppression. Same-speaker press/release now preserves the bubble; beginning a real drag cancels it. A native regression was added before changing event handling. Both immediate and delayed full down/up/click sequences must preserve the same bubble and increment meetings only once. The six-character collector exercises these full pointer sequences too.

The new native regression compiled before and after the event fix. **Native runtime RED/GREEN is unavailable in this cloud environment**; API compilation is not proof of a passing Unity assertion. The mandatory Windows run must execute these checks. The restoration/camera corrections are capture-harness changes, not replacement render evidence.

Deferred minor: the amendment carries historical manifest digests, but those digests are not independently signed or pinned outside the same repository. Coordinated edits to a historical manifest and amendment could evade that consistency check. Direct art overrides are rejected. For this submission the two original manifests and all source art/registration files are independently compared with the base Git revision and remain unchanged. Broader tamper-proof provenance is not claimed; no guard expansion was made during the final correction pass.

Reviewer declined to judge actual Unity rendering, overlap appearance, device responsiveness/performance and installed APK behavior, because licensed Unity and physical Pixel access are unavailable here. Those results remain pending.

Rulings carried forward:

- Repeat suppression lasts while the same bubble is visible or for a 0.4-second short repeat window. No new per-day save field. Cost: a character may speak again after dismissal/cooldown.
- Install the current authored candidate first. Cost: this run does not establish a baseline-relative performance improvement; a separate comparison is needed if absolute device measurements show concern.
- Formal fixation remains pending current native images and current Pixel results. Historical native successes do not certify this candidate.
