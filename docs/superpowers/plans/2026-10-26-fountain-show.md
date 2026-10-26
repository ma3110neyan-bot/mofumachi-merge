# Fountain Show Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans inline. No implementer delegation; one fresh whole-change reviewer at the end. Steps use checkbox tracking.

**Goal:** Make the central fountain visibly larger and flowing, with one short lightweight show every few minutes, preserving the approved field.
**Architecture:** Existing fountain art and single water child share pure14% display projection. A session clock survives Home rebuilds but advances only visible/unpaused; a bounded196quad sampler supplies ordinary water and a9second show/prelude/recovery window.
**Tech Stack:** Unity6000.6.4f1,UGUI,C#9,.NET8CPU,NUnit,PowerShell5.1-compatibleQA.
**Spec:** docs/superpowers/specs/2026-10-26-fountain-show-design.md

## Global Constraints

- Render-only scale1.14 aroundY.285; noNav/Collider/scenery/characters/save/reward/Merge/BGM/book/main changes.
-196quads784vertices＋old672≤1500;30Hznewphase;dtcap.1;offscreen/pause/modal stop.
-180seconds active-visiblecycle:171normal,2prelude,6show,1recovery;retainclock across Home rebuilds, no saved/wallclockcatchup.
- Original approved/frozen records immutable. New exactscope pins fountain-only source and actual oldBefore SHA; no adopted/Pixel flags without proof.
- All new visual/native/device success requires actual evidence. No newcloudUnitylicense retry.

## Review Focus

- Visible water must be meaningfully stronger than prior39/36/169pixels: broad continuous layered streams and actualnative change assertions, notvertexcountonly.
- Extreme elapsed times/hidden/pause/rebuild must not fast-forward or resetshow; test clock boundaries,clamp,and sharedsession lifecycle.
- Scale/showjets must keep projection bounded and allparentdepth/input invariant; nativefront/farsorting and confinedpixelchanges.
- Beforeimages must remain actual8aa native originals, never repaintedor relabeled currentrun;SHA/source/provenance andarchive checks.
- PixelFPS/heat may be unavailable; validframeparser rejects placeholders, device measurements remainpending not fabricated.

### Task1: Pure timing, stronger geometry and same-depth display

Files: TownFountainWaterLayout.cs(clock/state/projection/sampler),TownFountainWaterGraphic.cs(sessionclock/render),TownFountainGraphic.cs(displayprojection);Tests/Presentation/FountainWaterTests.cs,existingTownMotionPresentationTests.cs.
Interfaces: Layout.DisplayScale=1.14,QuadCount=196;Project(TownPoint,float);Quad(int,float,float showStrength=0,float anticipation=0);ShowAt(float)->stage/strength/glow;TownFountainWaterClock.Phase/Elapsed/Advance(float,bool);Graphic.ShowStage,ShowElapsed,Phase and existingAdvanceWater/Bind.

- [x] Puretests:baseline5green,oldmodelRED5/10→newGREEN10/10;scale/clock/strongwater/budget/GC verified.
- [x] Implemented pureclock+196quadgeometry/sharedsessionGraphic/render-onlyartprojection; focusedGREEN10.
- [x] Extend nativeexistingwatercase for784mesh,1456budget,paused/offscreen/rebuildclock;APIcompile.

### Task2: Honest native Before/normal/show comparisons and narrow gates

Files:ExperienceCaptureTests.cs;build-android.ps1/visual-lock.ps1;newfountain-show-candidate record;stageacceptance;android-tools/town-boundary-locktests.
Interfaces:existing33normal3pairs4phases remain; new34overviewprelude/show/recovered,front/behindshow and3same-instantshowOFF controls8PNG =>162new.3prior33after originals copied into fountain-before/ and included inZIPwithfixedSHA. fountain-show-comparison.json records8aaBefore,exact3names/hashes,stages andvisiblepixelchanges;HTML showsBefore/normal/show. Originalfield/nav/growth bindings remainexact.

- [x] Write newartifact/scopeguardtests and watch oldgates reject newrequirements.
- [x] Implement native3showviews+prelude/recovery and oldBeforecopy/provenance; bindnewfountainrenderinputs without touchingoriginalapproval.
- [x] Require162newframes and3oldBeforeSHA/source withshowmetadata;currentnormal784vertices and meaningfulnewpixels. Keep369testsminimum/allpassedfailed0skip0.
- [x] RunAPIDLLcompiles andrelatedPSguards, includingCRLF/historical/frozen/nonfountainmutationrejection.

### Task3: Device measurement, final verification and handoff

Files:boundedread-onlyPixelmeasurementtool/parser tests ifneeded, docs/verification/fountain-show-2026-10-26/,reviewhandoffdoc.

- [x] Prepare sameAPKPixelmeasurement procedure/tool; neverclaim deviceFPS fromEditor mesh/phase. Unavailabledata staysnull.
- [x] FullCPU/APIs/PS andonefreshwholechange review; oneImportantfixpass ifnecessary, no secondreview.
- [x] Preparefeaturehandoff andoneWindowsTownQacommand.
- [x] Receive actualnative162＋oldBefore3,369Passed0failed0skip/source/same-runAPKmetadata and actualcausalshow proof from111414/5edfa5db. One independent image review eligibleforsameAPKPixel, notadoption.
- [ ] Verify actualAPKbinary/installedidentity andPixelFPS/thermal/10minute/depth/save/useracceptance beforefountainadoption.

## Ledger

Start1fcc888 cleanexistingfeature;baselineactualnative144Edit/225Play369Passed and154PNGverified. Keep existing checkout/userWindowsfeatureworkflow; no main/newworktree.
Ruling: detailed latestuserinstruction authorizes fountain size/strength/timedshow, superseding prior weakcandidate Pixelhandoff; no newimplementation-per-fileapproval. Defaults1.14/180s/2+6+1 areCodexcalls; ifwrongboundedfountainpolish only.
Pre-flight:T1Project/ShowAt/Clock feedT2capture/nativegates; keeplegacy33andold154baseline but updateexpectedmesh784andadd5newshowframes. FrozenFountainGraphicchanges requireexactnewscope whileallotherfrozeninputsremainbound.

Task1 evidence: RED5 because Project/ShowAt/Clock absent and old80quads; GREEN10 after implementation. SixAPI compiles succeeded; five JsonUtility field initialization warnings corrected before final verification. FullCPU in progress, native162/APK/Pixel pending. Existing field/Save/nav unaffected.

Final review: single freshgpt-6-astra review, Critical0/Important1/Minor0. I1: aggregate normal→show pixels included ordinary/original water changes; fixed by same-instant newlayerOFF/ON with >40pixels above normalUV.84 and0outsideprojection, plus unchangedclock/originalphase/game/NPC/camera. Persist3OFFcontrols, making162new＋3oldBefore. Artifact rejection actualRED→GREEN. No runtime change in fix; existing fullCPU345green plus freshfocused10/API6/all14PS validate the targeted evidence correction. No secondreview.
Final: Ruling: actual rendering/perceived water/show/enlargedlandmark/depth/crowding declined — require newnative162 plus sameAPKPixel, not source/API claims — cost ifwrong fountain remains unapproved and bounded correction.
Final: Ruling: APKidentity/FPS/thermal/600s/audio/save-restart declined — exactnewAPK/SHA andSurfaceView/battery/rawthermal evidence onPixel required, unavailablemetrics staynull — cost ifwrong no fountainadoption or nextstage.
Final: Ruling: inheritedEastconcealment/Northrim/depth concerns declined — preserve field and record existinglocalPolish, judge samePixel withoutrebuildingtown — cost ifwrong fieldusability remainsopen.

Final: Ruling: existing field capture used scaledFeet＋unscaledHeight, yielding.426worldtopbelowfarfeet.441 although actualenlargedtop.475. Changed only its projectionconsumer to sharedProject(uvY1), retaining strictbodyoverlap andwalkable assertions. Pureheightcheck added; no runtime/nav/assertionrelaxation. AllCPUproductruntime remains identical to full345green; targetedtests/gates/API rerun for test-onlyfix.

Source completion: full345CPU and afterreviewfocused10,API6zero warnings/errors,all14PSfixtures,419filefinalCRLF/f4ddc00d…/historicaldfb2cbc8…passed. Immutable original/frozen/weakcandidate and3BeforeSHA preserved. Commit/push samefeature for clientWindowsQA; no physical/native/adoption claim.

Native receipt2026-10-26:13ZIP185raw,162new＋3actual8aaBefore,144Edit/225Playall369Passedfailed0skip0inconclusive0. CRC/SHA/bytes/dimensions/decode/18sourcebindings pass. BothAPKsource/fingerprint/log/bytesmatch; binarySHAindependentrecompute pending.12noncentraloverviews＋6growthbyteidentical. Ordinarywater923/963/1741pixels;showOFF/ON5061/5056/6387,above-normaljets2344/2361/2345,outside0. NewshowI1isolation defect proved resolved by actualnativepair. Temporalnormal/show aggregate includesotherpose/name/edgechanges; no causal overclaim.
Native image review:Critical0,newImportantstaticregressions0,inheritedImportant2(Eastcanopy/livefountainsolidbodydepth),Minor2(rigidnewshowstreams/Northrim). Ruling: liveperception/timing/readability andFPS/heat/600s/save/binaryidentity declined — keep same-runPixelgate and do no speculative codechange/rebuild for receipt — costifwrong fountainadoption remainsheld. Userfieldfreeze retained,2.5D/Merge/later/main stopped. Exactinstall/measurement doc:docs/fountain-show-pixel3a-2026-10-26.md.
