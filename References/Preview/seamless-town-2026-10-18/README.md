# Seamless town candidate — 2026-10-18

This directory contains source artwork provenance and four original acoustic BGM arrangements (two TOP, two Merge). It is **not** a native Unity/Pixel After report.

The existing おさんぽタウン OGG and official character masters are unchanged. Four new OGGs are streamed by Unity. Samples are CC0-1.0, with pinned source commits and SHA256 in `../release-review-2026-10-09/audio/sources/sources.json`. Reproduce with `scripts/render-town-candidates.py --sample-dir /path/to/verified/wavs`; no runtime SDK.

| Role | Candidate | Direction | Preview |
|---|---|---|---|
| TOP A | 花あかりの招待状 | 88 BPM, piano/bells, gentle 3/4 waltz | [8 seconds](audio/title-letter-8s.mp3) |
| TOP B | ひだまりの庭の扉 | 96 BPM, marimba/piano/soft strings, bright 3/4 | [8 seconds](audio/title-garden-8s.mp3) |
| Merge A | こもれびピクニック | 104 BPM, wood/piano, light 4/4 | [8 seconds](audio/merge-picnic-8s.mp3) |
| Merge B | リボンを結ぶ午後 | 112 BPM, piano/bells, buoyant 4/4 | [8 seconds](audio/merge-ribbon-8s.mp3) |

Settings: 8-second audition, select for its role, or listen in the actual TOP/town/Merge screen. Selection uses auxiliary per-role preferences; main save schema and volumes unchanged. Old audio resources/IDs remain loadable. Initial A choices are comparison defaults, **not final adoption**. Pixel speaker/listening acceptance remains pending.

`candidate-audio.json` records decoded durations, peaks, RMS, loop boundary deltas and hashes. These are signal checks, not proof of subjective music quality or device playback.
