# Full PlayMode acceptance regression repair

> Use executing-plans inline. User-approved acceptance continues; no gameplay/art changes or extra confirmation. One fresh final test-review.

Goal: Repair5 failing test setup/measurement assumptions exposed by the actual222-case run, preserving all354approved runtime files.
Spec: actual References/Review/full-playmode-2026-10-24-171445 XML/log plus user acceptance/freeze instruction. No tests removed/ignored,222native cases remain.

- [x] Inspect all5 actual failures, locate their exact source and compare approved runtime behavior.
- [x] Probe current world model outside repo to establish real district population/greeting fixture floors.
- [x] Update only QualityPassStoryGrowthTests.cs, SeamlessFieldTests.cs, TownRegionFlowTests.cs, TownRegionLifeTests.cs. Frame the actual requester before tapping; check story copy separately from world-space miniature signage; verify per-piece shared atlas binding; use approved five-area placement and explicitly arranged NPC greeting fixture. Preserve thanks/reunion/reward/save, visible conversation/pause/resume and layout assertions.
- [x] Compile all6 Unity assemblies, run appropriate CPU regressions/full suite and verify354runtime/JPEG hashes and unchanged renderdigest. Native RED is the received actual5 failures; native GREEN requires Windows rerun. Do not claim native pass from DLL/CPU checks.
- [x] Fresh test-review, fix material findings, commit/push feature and hand off same full -TownQa rerun (not16-case subset).

Review focus: no weakening assertions into current-value mirrors; no test setup teleport concealed as movement evidence; current view visibility before tap; world miniature labels not story paragraphs; exact source provenance and native pending status.

Initial base48413ea. Approved runtime source638aa24/renderdfb2cbc80e81c0d3967341f13426e2465913887f16f26763f696c36c7efe26fa remains frozen. Full222 XML actualRED217/5/skip0 is the failure oracle.

Diagnosis validated: pure current model spawns LuluCentral/PomWest/FioEast/MielNorth/NoahSouth plus heroCentral; legacy1/4/1 and both explicit conversation floor points valid. Runtime React rejects hidden residents; shared atlas catalog deliberately switches per-kind resource. Story test previously included world-miniature best-fit signage, whose font-size preference is not story copy height.

Local final: CPU302/302,0failed/skip;6actualUnityAPIassembly builds0warnings/errors;222native declarations retained; production QApreflight and354game hash checks pass; accepted renderdigest unchanged. Fresh independent review Critical0 Important0, no edits. NativeGREEN remains Windows pending. No game/source-art/persistence edits, new features or adoption.

## Follow-up actual run180636

Received222total/221pass/1fail/skip0. Prior other4failures actually resolved. Remaining conversation fixture fails before the greeting loop: modelRestore changes Lulu toWest but TownActor.RegionId remainsCentral until ApplyWorldStep. The previous render-only refresh does not call ApplyWorldStep. This was the fixture-author synchronization omission, not a game change.

Repair: one normal life.AdvanceFrame(1f/30) before existing actor/pair assertions, followed by the same rendering refresh. Actual TownFrameClock uses30Hz with epsilon and normal World.Step applies all actor states, preserving the real greeting/pause/resume assertions. No count weakening or game edits. PlayModeDLL builds0warnings/errors;222cases retained;354runtime hashes/renderdigest unchanged; nativeGREEN requires Windows rerun. No new CPU/PS suite reruns for a single test-only frame insertion.
