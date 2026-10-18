# East / west town and readable POP presentation

Authority: user feedback of 2026-10-18, seven Pixel PDFs (15:38–15:43 JST), and the September 22 collage. This supersedes the north/south and tilted-Merge presentation requirements. The user explicitly authorizes this complete bundle without additional design or per-file approval.

## Outcome
The town and residents dominate the phone screen. A single continuous painting opens from the central plaza toward the western cafe and eastern florist, with broad paved loops rather than stacked portrait backgrounds. Street level changes add visible furniture and storefront dressing. Merge returns to an aligned, readable ivory 5×6 grid, with clear selection and merge cues. One acoustic POP candidate lifts the tempo without chiptune timbres. The formal six designs and logo remain byte-identical.

## Selected approach
Re-author one wide map and trace its actual paving; reuse the existing camera, polygon navigation and six-resident avoidance. Rotating the old portrait map would rotate buildings and keeping it would preserve the unwanted north/south bias. Adding separate tiles would reintroduce seams. Use a 3:2 map within world bounds Left=-1, Bottom=-.6, Width=3, Height=2. Stable Core West/Central/East and place IDs remain; new UV anchors are authored after inspecting the final map. Region membership uses X. Camera follows on both axes, clamps to the full opaque map, and reveals the side destinations by walking or explicit fast travel.

## Presentation
Default Home shows a 44-unit top HUD (today, level, coins, menu), a narrow location ribbon and a 56-unit bottom bar (next step / Merge / menu). The field extends behind these floating controls; input is clipped only at actual HUD bounds. Expanded menu contains fast travel, quest guidance, shop guidance, book, daily and settings, closes on action/back, and pauses movement behind it. The large permanent chapter card and 76-unit travel panel are removed from default view. Keep new-discovery/today badges and a compact tappable next-goal hint. Dialogues and facility captions are contextual, never permanently cover residents.

Growth is additive and keyed to existing town level, without changing reward math. Lv1 is a small garden, Lv3 adds paired street lights, benches and cafe furniture, Lv6 adds awnings, storefront flowers, flags and lanterns. Base painting leaves clear verge/patio slots for these additions. Decorative geometry does not occupy the traced walking polygons. All growth props share the map projection and visibility culling.

Character presence uses the unchanged approved atlas: layered contact shadow, depth scale/order, breathing, smooth turn/sway, feet/hand motion and the existing expression strips. No new face or costume generation. Avoid mirroring asymmetric designs. Idle motion remains small and deterministic.

Merge uses equal-width rows, aligned rounded-square cells, a thin ivory panel and warm rim. Items keep constant size across rows. No trapezoid, elliptical bowls or thick lower platform. Retain floating selection, compatible-target glow, pickup, gathering, flash and landing. Stable row-major indexes, hit targets >=48 logical units, rules and saves remain.

BGM keeps town bytes and all existing role preference IDs. Add one original acoustic POP Merge candidate (piano/marimba, bass and light percussion, syncopated hook, about 116–124 BPM), >=32 bars, seamless loop, 8-second preview and role selection/persistence through existing catalog. Prior candidates remain available. Final music adoption is after Pixel listening.

## Constraints and verification
No destructive SaveData change; no Core merge/delivery/reward/quest progression change; no official design/logo change; no SDK/payment/main merge. Retain book, ???, daily flowers and next goals. Test map anchors/routes, blocker crossing, camera bounds, region X classification, popup cancellation, equal-cell hit mapping, role preferences and existing saves. Add actual Unity captures for default/expanded Home, central/west/east, both walks, Lv1/3/6, character idle/walk, flat Merge and music candidates. Cloud can compile against Unity references and run pure suites; native Windows Unity and same-run APK are required next because the cloud Editor has no license/Android module. Never label composed art previews as Unity or Pixel evidence. Physical Pixel gameplay/audio/heat/save acceptance remains pending until user executes the built candidate.
