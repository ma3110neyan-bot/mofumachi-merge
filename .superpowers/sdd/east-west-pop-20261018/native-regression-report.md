# Native regression fixture integration

The commit adding this report changes 15 native test/fixture files only; exact hash is supplied in the worker handoff. Production, ExperienceCaptureTests, EastWestHomeTests, ExperienceMusicTests and ResidentPresenceTests were not edited by this worker.

- Expanded menu interactions now open the actual overlay and select within it, avoiding duplicate default/menu `Nav Merge` controls. Travel, menu cancellation, settings return, daily counting and chapter replay preserve save/arrival assertions. Repeated fast travel reopens the live menu for each tap.
- Compact Home checks use `Navigation/Home quest`, `Next goal title` and `Town growth hint`. Field/action separation checks the native mask bounds and every resident graphic's clipping; title/feedback descendant bounds and font-fit assertions remain intact. Menu travel labels follow the explicit 13-unit font design and additionally verify width fit and >=48-unit targets.
- Drag/tap fixtures use authored world points and WorldPosition/World.Local/World.Source. The valid central floor target is UV (.59,.65), florist-building rejection uses (.815,.37), road-label occlusion uses `Place("west.meeting")`, and camera/region tests exercise horizontal movement. Existing door, pointer, save, pause, dialogue and journey bounds remain asserted.
- Growth lifecycle checks now inspect the actual regional prop collection rather than the removed three continuous-world chapter decorations. Level-growth expectations were checked against the additive catalog. BuildTime coverage includes the new street atlas's import settings and finite geometry/UVs for every prop enum, with input disabled.

Validation:

- All six asmdefs compiled against real Unity 6000.6.4f1 references: zero warnings/errors. Isolated outputs: `/tmp/resident-presence-oct18/native-regression-asmdefs`.
- Full pure Presentation test run: 142 passed, zero failed/skipped (136 repository tests + six local probes for fixture coordinates). TRX: `/tmp/resident-presence-oct18/results/native-regression-pure-suite.trx`.
- Probe checks separately prove exact road-tap arrival, rejection beyond the florist wall clamp range, coasting within both door entry radii, westward quest drag, and physical cafe access at initial level without unlocking the named Core stop.
- `git diff --check` passes for owned files. No tests were disabled, ignored or removed.

Product findings sent to parent and resolved there: default-level `Walk to cafe` originally called the locked named `west.cafe-front` stop and did nothing; parent added physical-point guidance below Lv2. The in-progress capture fixture directly called internal `Surprise`; parent replaced it with reflection. The native cafe journey assertion remains in place.

Native PlayMode/BuildTime execution, actual UGUI/font/mask screenshots and physical gameplay remain pending on licensed Windows Unity. Compilation and pure coordinate probes are not native rendering evidence.
