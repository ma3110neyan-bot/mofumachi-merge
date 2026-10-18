# はずむ花みち · acoustic POP candidate

Stable ID: `merge-pop-promenade`. Original F-major arrangement, 120 BPM, 4/4,
32 bars / 64 seconds. Piano hook and bass, offbeat piano chords, marimba replies
and a quiet recorded shaker use five SHA-pinned CC0 samples. The eight-bar hook
has a changed second ending, a quieter answering section and a final reprise.

- [8-second preview](merge-pop-promenade-8s.mp3)
- [Full game loop](../../../../Assets/Mofumachi/Resources/Mofumachi/Audio/bgm-merge-pop-promenade.ogg)
- [Sources, hashes and decoded measurements](candidate-audio.json)

Decoded peak −7.09 dBFS, integrated loudness −21.1 LUFS, loop boundary difference
0.00153. These measurements verify headroom and continuity, not how enjoyable
or fatigue resistant the track sounds. No cloud listening was available. Native
Unity playback and Pixel speaker/headphone listening remain pending; this is a
candidate, and existing role defaults remain in use until explicitly selected.

Render only this candidate and check the artifacts with the existing toolchain:

```sh
python3 scripts/render-pop-promenade.py --sample-dir /workspace/setup/mofumachi/audio-sources
python3 scripts/test-pop-promenade.py --sample-dir /workspace/setup/mofumachi/audio-sources -v
```

Run both commands from the repository root. Existing tracks are never rendered
by this script; the manifest pins all 26 pre-existing audio/importer files.
