"""Embed raw directional material sources in an offline browser review."""
from pathlib import Path
import base64
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "References/Characters/noah-directional-parts-2026-10-30"
OLD = ROOT / "References/Characters/noah-authored-parts-2026-10-29"
ATLAS = ROOT / "Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png"
EXPECTED = "23d14025f146c429da01b6e3a74ef803254755f8d149f9eb53516aacaa30163d"
if hashlib.sha256(ATLAS.read_bytes()).hexdigest() != EXPECTED:
    raise RuntimeError("Formal atlas changed; do not package.")
layout = json.loads((ART / "directions-layout.json").read_text())
paths = {"original": ATLAS, "whole": OLD / "turnaround-draft-02.png"}
for key, source in layout["sources"].items():
    path = ART / source["file"]
    if hashlib.sha256(path.read_bytes()).hexdigest() != source["sha256"]:
        raise RuntimeError(f"Source mismatch: {path}")
    paths[key] = path
data = {"layout": layout,
        "images": {key: "data:image/png;base64," + base64.b64encode(path.read_bytes()).decode("ascii")
                   for key, path in paths.items()},
        "directions": {"left": [577, 34, 521, 615], "right": [1112, 49, 484, 601],
                       "back": [1632, 37, 472, 610]}}
template = (ART / "review.template.html").read_text()
output = template.replace("__RIG__", (OLD / "rig-model.cjs").read_text())
output = output.replace("__DIRECTIONS__", (ART / "direction-review.cjs").read_text())
output = output.replace("__DATA__", json.dumps(data, separators=(",", ":")))
(ART / "noah-direction-review.html").write_text(output)
print("Offline directional material review built; formal atlas and source hashes verified.")
