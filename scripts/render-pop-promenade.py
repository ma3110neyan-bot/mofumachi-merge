"""Render only the original 120 BPM / 32-bar acoustic POP Merge candidate.

python3 scripts/render-pop-promenade.py --sample-dir /path/to/verified/wavs
Uses the existing numpy/scipy/FFmpeg toolchain and pinned CC0 recordings. Never
imports the older generator or writes an existing track. Pixel listening is pending.
"""
import argparse
import hashlib
import json
import re
import subprocess
import tempfile
from pathlib import Path

import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, resample_poly, sosfilt


RATE = 44100
BPM = 120
BARS = 32
BEAT = 60 / BPM
TRACK_ID = "merge-pop-promenade"
RESOURCE = "bgm-" + TRACK_ID
ROOT = Path(__file__).resolve().parents[1]
SAMPLES = {
    "piano3": ("JHPiano_NoSus_Close_C3_vl2_rr1.wav", 48),
    "piano4": ("JHPiano_NoSus_Close_C4_vl2_rr1.wav", 60),
    "piano5": ("JHPiano_NoSus_Close_C5_vl2_rr1.wav", 72),
    "wood4": ("Marimba_hit_Outrigger_C4_soft_01.wav", 60),
    "shaker": ("Mid_ShakerDouble_Down_rr1.wav", 0),
}


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_bank(sample_dir):
    sources = json.loads((ROOT / "References/Preview/release-review-2026-10-09/audio/sources/sources.json").read_text())
    bank, used = {}, []
    for kind, (filename, pitch) in SAMPLES.items():
        source = next(item for item in sources if item["file"] == filename)
        path = sample_dir / filename
        if sha256(path) != source["sha256"]:
            raise ValueError("Sample SHA256 mismatch: " + filename)
        sample_rate, pcm = wavfile.read(path)
        if sample_rate != RATE or pcm.dtype not in (np.int16, np.int32):
            raise ValueError("Unsupported recorded sample format: " + filename)
        pcm = pcm.astype(np.float64) / (2 ** (15 if pcm.dtype == np.int16 else 31))
        if pcm.ndim == 1:
            pcm = np.repeat(pcm[:, None], 2, axis=1)
        active = np.flatnonzero(np.max(np.abs(pcm), axis=1) > .001)
        if len(active):
            pcm = pcm[max(0, int(active[0]) - 220):]
        pcm /= max(.1, np.sqrt(np.mean(pcm[:RATE] ** 2)) * 4)
        bank[kind] = (pitch, pcm)
        used.append(source)
    return bank, used


def compose(bank):
    length = round(BARS * 4 * BEAT * RATE)
    mix = np.zeros((length + RATE * 6, 2))
    cache = {}
    events = []

    def put(kind, pitch, at, duration, gain, pan=0):
        key = (kind, pitch, duration)
        if key not in cache:
            source = min((name for name in bank if name.startswith(kind)), key=lambda name: abs(bank[name][0] - pitch))
            base, pcm = bank[source]
            scale = 2 ** ((pitch - base) / 12)
            pcm = resample_poly(pcm, 1000, max(1, round(1000 * scale))) if base else pcm.copy()
            pcm = pcm[:min(len(pcm), round((duration * BEAT + .22) * RATE))].copy()
            attack = min(len(pcm), round(.007 * RATE))
            release = min(len(pcm) - attack, round(.20 * RATE))
            pcm[:attack] *= np.linspace(0, 1, attack)[:, None]
            if release:
                pcm[-release:] *= np.linspace(1, 0, release)[:, None]
            cache[key] = pcm
        pcm = cache[key] * gain * np.array([1 - pan, 1 + pan])
        start = round(at * BEAT * RATE)
        mix[start:start + len(pcm)] += pcm
        events.append((kind, pitch, at, duration, gain, pan))

    # F6, C/E, Dm7, Am7, Bb6, F/A, Gm7, C7: a sunny acoustic pop cadence.
    harmony = [
        (41, (57, 60, 65)), (40, (55, 60, 64)),
        (38, (57, 60, 65)), (45, (55, 60, 64)),
        (46, (55, 62, 65)), (45, (57, 60, 65)),
        (43, (58, 62, 65)), (36, (58, 64, 67)),
    ]
    bridge = [harmony[i] for i in (4, 5, 6, 7, 2, 3, 6, 7)]
    # The hook starts after the downbeat; short anticipations alternate with rests.
    hook = [
        [(0.5, 69, .5), (1.25, 72, .5), (2, 74, .75), (3.25, 72, .5)],
        [(0, 67, .75), (1.5, 64, .5), (2.5, 67, .5)],
        [(.5, 65, .5), (1.25, 69, .5), (2, 72, .75), (3.25, 69, .5)],
        [(0, 67, .5), (.75, 69, .5), (1.5, 72, .75)],
        [(.5, 70, .5), (1.25, 74, .5), (2.5, 72, .5), (3.25, 70, .5)],
        [(0, 69, .75), (1.5, 65, .5), (2.5, 69, .5)],
        [(.5, 67, .5), (1.25, 70, .5), (2, 69, .75), (3.25, 67, .5)],
        [(0, 64, .75), (1.5, 67, .5), (2.5, 72, .5)],
    ]
    answer = [
        [(0.5, 65, .75), (2, 67, .5), (3, 69, .5)],
        [(0, 69, 1), (1.75, 72, .5), (3, 69, .5)],
        [(.5, 70, .5), (1.5, 69, .5), (2.5, 67, .75)],
        [(0, 67, 1), (2.5, 64, .75)],
        [(.5, 65, .5), (1.25, 69, .5), (2.5, 72, .75)],
        [(0, 72, .75), (1.5, 69, .5), (2.75, 67, .5)],
        [(.5, 65, .5), (1.5, 67, .5), (2.5, 70, .75)],
        [(0, 69, .75), (1.5, 67, .5), (2.75, 64, .75)],
    ]
    for bar in range(BARS):
        section, phrase_bar = divmod(bar, 8)
        bass, chord = (bridge if section == 2 else harmony)[phrase_bar]
        at = bar * 4
        # Root, fifth and anticipations provide motion without a synthetic drum kit.
        for offset, pitch, duration, gain in [(0, bass, .85, .21), (1.5, bass + 7, .45, .12),
                                               (2.5, bass, .65, .17), (3.5, bass + 12, .35, .10)]:
            put("piano", pitch, at + offset, duration, gain, -.06)
        for k, offset in enumerate((.5, 1.5, 2.75, 3.5)):
            if section == 2 and k == 3:
                continue
            for pitch in chord:
                put("piano", pitch, at + offset, .32 if k % 2 else .45, .058 if k % 2 else .069, -.13)
        melody = (answer if section == 2 else hook)[phrase_bar]
        for offset, pitch, duration in melody:
            # A second phrase ending and final reprise avoid an eight-bar copy loop.
            if section == 1 and phrase_bar in (3, 7) and offset == 0:
                pitch += 5
            put("piano", pitch, at + offset, duration, .205 if section != 2 else .175, .035)
        # A low, soft wooden answer occupies the melody's longer breathing spaces.
        if phrase_bar in (1, 3, 5, 7):
            put("wood", chord[1] + 12, at + 3.25, .25, .062, .18)
            if section in (1, 3):
                put("wood", chord[0] + 12, at + 3.75, .20, .047, .12)
        elif section in (1, 3):
            put("wood", chord[0] + 12, at + 3.75, .20, .038, .13)
        for step in range(8):
            # Soft alternating eighths with a tiny deterministic timing/velocity sway.
            offset = step * .5 + (.018 if step % 2 else 0)
            put("shaker", 0, at + offset, .09, (.016 if step % 2 else .010) * (.8 if section == 2 else 1), .10)
    folded = mix[:length].copy()
    folded[:len(mix) - length] += mix[length:]
    room = folded.copy()
    for delay, gain in ((.071, .040), (.113, .024), (.173, .014)):
        room += np.roll(folded, round(delay * RATE), axis=0)[:, ::-1] * gain
    # Process repeated periods and retain the middle one so the EQ is periodic too.
    lowpass = butter(2, 7200, fs=RATE, output="sos")
    room = sosfilt(lowpass, np.tile(room, (3, 1)), axis=0)[length:length * 2]
    room -= room.mean(axis=0)
    room *= min(.44 / np.max(np.abs(room)), .085 / np.sqrt(np.mean(room ** 2)))
    return np.int16(np.clip(room, -1, 1) * 32767), events


def decode(path):
    raw = subprocess.check_output(["ffmpeg", "-hide_banner", "-loglevel", "error", "-i", str(path),
                                   "-f", "f32le", "-ac", "2", "-ar", str(RATE), "-"])
    return np.frombuffer(raw, dtype=np.float32).reshape(-1, 2)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sample-dir", type=Path, required=True)
    args = parser.parse_args()
    bank, sources = load_bank(args.sample_dir)  # Reject unverified samples before writing.
    audio_dir = ROOT / "Assets/Mofumachi/Resources/Mofumachi/Audio"
    protected = {str(path.relative_to(ROOT)): sha256(path) for path in sorted(audio_dir.iterdir())
                 if path.is_file() and path.name not in (RESOURCE + ".ogg", RESOURCE + ".ogg.meta")}
    preview_dir = ROOT / "References/Preview/east-west-pop-2026-10-18/audio"
    preview_dir.mkdir(parents=True, exist_ok=True)
    target, preview = audio_dir / (RESOURCE + ".ogg"), preview_dir / (TRACK_ID + "-8s.mp3")
    pcm, events = compose(bank)
    with tempfile.TemporaryDirectory(prefix="mofumachi-pop-") as temp:
        wav = Path(temp) / (TRACK_ID + ".wav")
        wavfile.write(wav, RATE, pcm)
        subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", str(wav),
                        "-fflags", "+bitexact", "-flags:a", "+bitexact", "-c:a", "libvorbis", "-q:a", "5", str(target)], check=True)
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", str(target), "-t", "8",
                    "-af", "afade=t=out:st=7.7:d=0.3", "-c:a", "libmp3lame", "-q:a", "3", str(preview)], check=True)
    decoded, preview_pcm = decode(target), decode(preview)
    if not np.isfinite(decoded).all() or np.max(np.abs(decoded)) >= .56:
        raise ValueError("Nonfinite or excessively loud decoded audio")
    for path, digest in protected.items():
        if sha256(ROOT / path) != digest:
            raise ValueError("Existing audio changed: " + path)
    loudness = subprocess.run(["ffmpeg", "-hide_banner", "-i", str(target), "-af", "ebur128=peak=true", "-f", "null", "-"],
                              capture_output=True, text=True, check=True).stderr
    integrated = float(re.findall(r"I:\s*(-?[0-9.]+) LUFS", loudness)[-1])
    true_peak = float(re.findall(r"Peak:\s*(-?[0-9.]+) dBFS", loudness)[-1])
    high = sosfilt(butter(2, 6000, fs=RATE, btype="highpass", output="sos"), decoded, axis=0)
    manifest = dict(id=TRACK_ID, title="はずむ花みち", resource=RESOURCE, role="Merge", bpm=BPM,
                    meter="4/4", bars=BARS, seconds=len(decoded) / RATE, previewSeconds=len(preview_pcm) / RATE,
                    arrangement="Original F-major acoustic POP; syncopated piano hook, sampled piano bass, marimba answers and quiet recorded shaker. A/A-variation/B/A-reprise, no synthesized timbres.",
                    fatigueDesign="Moderate melody register, phrase rests, quieter bridge, restrained shaker, gentle 7.2 kHz low-pass and no high bell ostinato. Requires physical listening acceptance.",
                    peakDbFS=float(20 * np.log10(np.max(np.abs(decoded)))),
                    rmsDbFS=float(20 * np.log10(np.sqrt(np.mean(decoded ** 2)))), integratedLUFS=integrated,
                    truePeakDbFS=true_peak, highFrequencyEnergyAbove6kHzRatio=float(np.mean(high ** 2) / np.mean(decoded ** 2)),
                    loopBoundaryDelta=float(np.max(np.abs(decoded[0] - decoded[-1]))),
                    maximumAdjacentSampleDelta=float(np.max(np.abs(np.diff(decoded, axis=0)))),
                    sha256=sha256(target), renderPcmSha256=hashlib.sha256(pcm.tobytes()).hexdigest(),
                    scoreSha256=hashlib.sha256(json.dumps(events).encode()).hexdigest(),
                    preview=preview.name, previewSha256=sha256(preview), sampleLicense="CC0-1.0", sources=sources,
                    protectedAudioSha256=protected, listeningStatus="Not listened in cloud; native Unity and Pixel listening pending")
    (preview_dir / "candidate-audio.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps({key: manifest[key] for key in ("id", "seconds", "previewSeconds", "peakDbFS", "rmsDbFS", "integratedLUFS", "truePeakDbFS", "loopBoundaryDelta")}, indent=2))


if __name__ == "__main__":
    main()
