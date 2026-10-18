"""Check delivered audio, legacy-byte preservation and rejection of altered samples.

python3 scripts/test-pop-promenade.py --sample-dir /path/to/verified/wavs
These are objective artifact checks; they do not establish listening acceptance.
"""
import argparse
import hashlib
import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

import numpy as np


ROOT = Path(__file__).resolve().parents[1]
AUDIO = ROOT / "Assets/Mofumachi/Resources/Mofumachi/Audio/bgm-merge-pop-promenade.ogg"
MANIFEST = ROOT / "References/Preview/east-west-pop-2026-10-18/audio/candidate-audio.json"
RECORD = json.loads(MANIFEST.read_text())
PREVIEW = MANIFEST.parent / RECORD["preview"]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def decode(path):
    data = subprocess.check_output(["ffmpeg", "-hide_banner", "-loglevel", "error", "-i", str(path),
                                    "-f", "f32le", "-ac", "2", "-ar", "44100", "-"])
    return np.frombuffer(data, dtype=np.float32).reshape(-1, 2)


class PopPromenadeAudioTests(unittest.TestCase):
    def test_full_loop_is_32_bars_at_120_bpm_and_preview_is_eight_seconds(self):
        self.assertEqual((RECORD["id"], RECORD["role"], RECORD["bpm"], RECORD["meter"], RECORD["bars"]),
                         ("merge-pop-promenade", "Merge", 120, "4/4", 32))
        self.assertEqual(len(decode(AUDIO)), 44100 * 64)
        self.assertEqual(len(decode(PREVIEW)), 44100 * 8)
        info = json.loads(subprocess.check_output(["ffprobe", "-v", "error", "-show_streams", "-of", "json", str(AUDIO)]))["streams"][0]
        self.assertEqual((info["codec_name"], int(info["sample_rate"]), info["channels"]), ("vorbis", 44100, 2))

    def test_decoded_loop_has_headroom_and_a_small_seam(self):
        pcm = decode(AUDIO)
        self.assertTrue(np.isfinite(pcm).all())
        self.assertLess(float(np.max(np.abs(pcm))), .5012)  # at least 6 dBFS headroom
        self.assertLess(float(np.max(np.abs(pcm[0] - pcm[-1]))), .005)
        self.assertLess(RECORD["truePeakDbFS"], -6)
        self.assertLess(RECORD["highFrequencyEnergyAbove6kHzRatio"], .03)

    def test_output_hashes_match_the_review_manifest(self):
        self.assertEqual(digest(AUDIO), RECORD["sha256"])
        self.assertEqual(digest(PREVIEW), RECORD["previewSha256"])

    def test_all_26_existing_audio_and_importer_files_are_byte_identical(self):
        self.assertEqual(len(RECORD["protectedAudioSha256"]), 26)
        for path, expected in RECORD["protectedAudioSha256"].items():
            with self.subTest(path=path):
                self.assertEqual(digest(ROOT / path), expected)

    def test_sources_match_pinned_cc0_records_and_an_altered_sample_is_rejected(self):
        for source in RECORD["sources"]:
            with self.subTest(source=source["file"]):
                self.assertEqual(source["license"], "CC0-1.0")
                self.assertEqual(digest(SAMPLE_DIR / source["file"]), source["sha256"])
        before = {path: digest(path) for path in (AUDIO, PREVIEW, MANIFEST)}
        with tempfile.TemporaryDirectory(prefix="pop-unverified-") as temp:
            # The first required sample is enough: verification must fail before render.
            altered = Path(temp) / "JHPiano_NoSus_Close_C3_vl2_rr1.wav"
            shutil.copy(SAMPLE_DIR / altered.name, altered)
            altered.write_bytes(altered.read_bytes() + b"altered")
            result = subprocess.run(["python3", str(ROOT / "scripts/render-pop-promenade.py"),
                                     "--sample-dir", temp], capture_output=True, text=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Sample SHA256 mismatch", result.stderr)
        self.assertEqual({path: digest(path) for path in before}, before)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sample-dir", type=Path, required=True)
    args, remaining = parser.parse_known_args()
    SAMPLE_DIR = args.sample_dir
    unittest.main(argv=[__file__] + remaining)
