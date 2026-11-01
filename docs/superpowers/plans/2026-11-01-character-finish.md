# Character final finish implementation plan

> Required workflow: superpowers:executing-plans, inline. User has explicitly waived intermediate approval. Base fb172fea17210bf212bfcd0545de7debd7f8521f.

Goal: complete the existing six-character 2.5D direction without redesign.
Spec: ../specs/2026-11-01-character-finish-design.md
Stack: existing Unity6000.6.4f1 C#, NUnit, PowerShell5-compatible scripts.

1. Noah neck and facing RED→GREEN: add body-socket invariant tests across modes/phases/three part directions, registered-head contact preservation and stable actual-motion facing (north, diagonals, noise, stop). Fix Noah motion/renderer first, then route the five other registrations through the same contact method and actor facing tracker. Preserve every source PNG, registration, scale, planted foot and vertex count. Expected: targeted pure tests and API compile green.
2. Passing RED→GREEN: replay actual head-on/crossing/near-obstacle model cases; measure reversal/facing churn and complete paths with unchanged hard clearance. Add bounded per-resident avoidance smoothing to existing Steer only if reproduction confirms it. Keep world-Y Sorting and floor geometry unchanged. Expected: passing-focused tests green, no teleport/deadlock.
3. Tap-only RED→GREEN: transient tap gate plus native no-auto-bubble and actual pointer tests. Remove automatic pair/idle/rest bubbles; preserve public explicit outings, drag, facilities and existing daily recording. All six clicks show their own reaction/talk; repeat while visible is ignored. Make bubble nonblocking, avoid covering actor bounds, reduce moving name cards. Update affected legacy/native capture tests to actual taps. Expected: tap-focused pure tests and native API compile green.
4. Evidence and handoff: six original/current static+walk+stop+turn+talk captures and actual passing/input focused native tests; original-art and scoped freeze amendment guards with tamper tests; authored-first Pixel capture. Archive PDF receipt. Run focused checks, one fresh review of base..HEAD, fix important findings, push/verify feature only. All full/native/APK/Pixel/final-fixed claims remain pending actual Windows run. No Save reset, uninstall, main merge, unrelated full-test repeats.

Review focus: chin attachment under registered offsets and blink crops; cardinal/dwell updates at zero/tiny deltas; passing near blocked paths; hero tap still supports walking and explicit outings; all automatic bubble entry points removed; exact frozen-art guard and amendment cannot authorise art; physical APK mode matches current candidate.
