"""Rasterize unchanged source chips with exported production Graphic vertices.

These are labelled software/material previews, never Unity/Pixel screenshots.
No positions are corrected here; no generative image processing is used.
"""
import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
NAMES = {"noah": "ノア", "momo": "モモ", "lulu": "ルル", "pom": "ポム", "fio": "フィオ", "miel": "ミエル"}
DIRECTIONS = [("front", "正面"), ("left", "左向き"), ("right", "右向き"), ("back", "背面")]
BACKGROUND = (237, 240, 244)
INK = (67, 54, 58)
FONT = "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc"
BOLD = "/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc"
INPUTS = {}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def font(size, bold=False):
    return ImageFont.truetype(BOLD if bold else FONT, size, index=0)


def text(draw, xy, value, size=24, bold=False, fill=INK):
    draw.text(xy, value, font=font(size, bold), fill=fill)


def load_original(path, expected=None):
    path = path.resolve()
    actual = digest(path)
    if expected is not None and actual != expected:
        raise ValueError(f"Source art changed: {path}")
    INPUTS[path.relative_to(ROOT).as_posix()] = actual
    image = Image.open(path)
    image.load()
    return image.convert("RGBA")


def part_chip(original, part):
    x, y, width, height = part["crop"]
    chip = original.crop((x, y, x + width, y + height))
    pixels = np.asarray(chip).copy()
    if "maskRows" in part and part["maskRows"] is not None:
        assert len(part["maskRows"]) == height
        for row, span in enumerate(part["maskRows"]):
            lo, hi = span["min"], span["max"]
            if lo < 0:
                pixels[row] = 0
            else:
                pixels[row, :lo] = 0
                pixels[row, hi + 1:] = 0
    if part.get("clipSum"):
        clip = part["clipSum"]
        total = (np.arange(width)[None, :] + x) + (np.arange(height)[:, None] + y)
        remove = np.zeros((height, width), dtype=bool)
        if clip.get("min", 0) > 0:
            remove |= total < clip["min"]
        if clip.get("max", 0) > 0:
            remove |= total > clip["max"]
        pixels[remove] = 0
    return Image.fromarray(pixels)


def layer_from_quad(chip, vertices, size, scale, baseline):
    # Graphic vertices are BL, TL, TR, BR in image/UV correspondence.
    points = np.asarray([[size[0] / 2 + (x - 31) * scale, baseline - y * scale] for x, y in vertices])
    origin = points[1]
    columns = np.column_stack(((points[2] - origin) / chip.width, (points[0] - origin) / chip.height))
    inverse = np.linalg.inv(columns)
    translation = -inverse @ origin
    assert np.max(np.abs(points[3] - (points[2] + points[0] - points[1]))) < .002
    coefficients = (*inverse[0], translation[0], *inverse[1], translation[1])
    return chip.convert("RGBa").transform(size, Image.Transform.AFFINE, coefficients,
                                         resample=Image.Resampling.BILINEAR).convert("RGBA")


class Character:
    def __init__(self, name, meshes, atlas, bounds):
        self.name = name
        self.mesh_path = meshes / f"{name}-mesh.json"
        self.mesh = json.loads(self.mesh_path.read_text())
        assert self.mesh["native_unity"] is False
        assert (self.mesh["actor_width"], self.mesh["actor_height"]) == (62, 78)
        relative = "noah-directional-parts-2026-10-30/directions-layout.json" if name == "noah" else f"{name}-authored-parts-2026-10-30/layout.json"
        self.layout_path = ROOT / "References/Characters" / relative
        self.layout = json.loads(self.layout_path.read_text())
        INPUTS[self.layout_path.relative_to(ROOT).as_posix()] = digest(self.layout_path)
        self.frames = {(f["state"], f["direction"]): f for f in self.mesh["frames"]}
        self.parts = {}
        self.chips = {}
        for direction in ("left", "right", "back"):
            for index, part in enumerate(self.layout["views"][direction]["parts"]):
                source = self.layout["sources"][part["source"] if name == "noah" else direction]
                path = self.layout_path.parent / source["file"]
                original = load_original(path, source["sha256"])
                assert original.size == (source["width"], source["height"])
                self.parts[direction, index] = part
                self.chips[direction, index] = part_chip(original, part)
        index = ("momo", "lulu", "pom", "fio", "miel", "noah").index(name)
        x, y, w, h = bounds[index]
        if name != "noah":
            assert self.layout["front_crop"] == [x, y, w, h]
        self.front = atlas.crop((x, y, x + w, y + h))

    def render(self, direction, state="idle", size=(480, 450), scale=4.8, baseline=410):
        image = Image.new("RGBA", size)
        if direction == "front":
            ratio = min(62 / self.front.width, 78 / self.front.height) * scale
            sprite = self.front.resize((round(self.front.width * ratio), round(self.front.height * ratio)), Image.Resampling.LANCZOS)
            image.alpha_composite(sprite, (round(size[0] / 2 - sprite.width / 2), round(baseline - sprite.height)))
            return image
        frame = self.frames[state, direction]
        for index, mesh_part in enumerate(frame["parts"]):
            assert mesh_part["id"] == self.parts[direction, index]["id"]
            image.alpha_composite(layer_from_quad(self.chips[direction, index], mesh_part["vertices"], size, scale, baseline))
        return image

    def neck(self, direction):
        if direction == "front":
            # Simple crop of the unchanged formal sprite; no anatomical claim.
            return self.render(direction, size=(960, 900), scale=9.6, baseline=820).crop((240, 340, 720, 540))
        frame = self.frames["idle", direction]
        index = next(i for i, p in enumerate(frame["parts"]) if p["id"] == "body")
        chip = self.chips[direction, index]
        alpha = np.asarray(chip)[:, :, 3]
        cols = np.arange(int(chip.width * .4), int(chip.width * .6))
        rows, column_indices = np.where(alpha[:, cols] >= 128)
        top = int(rows.min())
        column = float(np.median(cols[column_indices[rows == top]]))
        points = np.asarray(frame["parts"][index]["vertices"])
        # Choose only the viewing crop around the body collar. It does not
        # place or move either part, and is never used as a correctness oracle.
        point = points[1] + (points[2] - points[1]) * ((column + .5) / chip.width) + (points[0] - points[1]) * ((top + .5) / chip.height)
        x, y = 480 + (point[0] - 31) * 9.6, 820 - point[1] * 9.6
        return self.render(direction, size=(960, 900), scale=9.6, baseline=820).crop((round(x - 240), round(y - 100), round(x + 240), round(y + 100)))


def white_composite(image):
    result = Image.new("RGB", image.size, BACKGROUND)
    result.paste(image, mask=image.getchannel("A"))
    return result


def save_jpeg(image, path):
    image.save(path, format="JPEG", quality=95, subsampling=0, optimize=True, progressive=True)
    with Image.open(path) as verified:
        verified.load()
        assert verified.format == "JPEG" and verified.size == image.size


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--meshes", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--before-meshes", type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    atlas_path = ROOT / "Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png"
    atlas = load_original(atlas_path, "23d14025f146c429da01b6e3a74ef803254755f8d149f9eb53516aacaa30163d")
    body_source = (ROOT / "Assets/Mofumachi/Presentation/CharacterBodyGraphic.cs").read_text()
    segment = body_source.split("private static readonly Rect[] Bounds={", 1)[1].split("};", 1)[0]
    bounds = [list(map(int, values)) for values in re.findall(r"new Rect\((\d+),(\d+),(\d+),(\d+)\)", segment)[:6]]
    assert len(bounds) == 6
    runtime_files = ["Assets/Mofumachi/Presentation/NoahAuthoredGraphic.cs", "Assets/Mofumachi/Presentation/CharacterAuthoredGraphic.cs", "Assets/Mofumachi/Presentation/NoahAuthoredMotion.cs", "Assets/Mofumachi/Presentation/CharacterBodyGraphic.cs"]
    if (ROOT / "Assets/Mofumachi/Presentation/CharacterHeadBodyCalibration.cs").exists():
        runtime_files.append("Assets/Mofumachi/Presentation/CharacterHeadBodyCalibration.cs")
    runtime_revision = subprocess.check_output(["git", "log", "-1", "--format=%H", "--", *runtime_files], cwd=ROOT, text=True).strip()
    runtime_digest = hashlib.sha256("\n".join(p+":"+digest(ROOT/p) for p in runtime_files).encode()).hexdigest()
    dirty = subprocess.check_output(["git", "status", "--porcelain", "--", *runtime_files], cwd=ROOT, text=True).strip()
    revision_label = "候補 " + runtime_digest[:12] if dirty else runtime_revision[:7]
    output_rows = []
    characters = []
    for name in NAMES:
        character = Character(name, args.meshes, atlas, bounds)
        characters.append(character)
        image = Image.new("RGB", (1980, 2020), "#fffaf7")
        draw = ImageDraw.Draw(image)
        text(draw, (30, 15), f"{NAMES[name]}｜頭・胴体の位置確認", 44, True)
        text(draw, (30, 78), "素材・コード再現プレビュー（Unity／Pixel実画面ではありません）", 28, True, "#a44931")
        text(draw, (30, 123), f"描画コード {revision_label} ／正式素材は無変更／街・影は表示していません", 25)
        for col, (_, label) in enumerate(DIRECTIONS):
            text(draw, (30 + col * 480 + 175, 173), label, 30, True)
        y = 220
        for state, label in [("idle", "静止"), ("walk-a", "歩行の一瞬 A"), ("walk-b", "歩行の一瞬 B")]:
            text(draw, (30, y), label, 25, True)
            y += 36
            for col, (direction, _) in enumerate(DIRECTIONS):
                tile = white_composite(character.render(direction, state))
                ImageDraw.Draw(tile).line((10, 410, 470, 410), fill="#c5c9ce", width=1)
                image.paste(tile, (30 + col * 480, y))
            y += 460
        text(draw, (30, y), "首元の拡大（静止時の同じ配置を拡大）", 25, True)
        y += 40
        for col, (direction, _) in enumerate(DIRECTIONS):
            image.paste(white_composite(character.neck(direction)), (30 + col * 480, y))
        text(draw, (30, 1954), "正面列は現仕様の正式一枚絵です。静止画では方向転換時の遅延・実機の連続動作は判定できません。", 23)
        path = args.output / f"{name}-four-directions.jpg"
        save_jpeg(image, path)
        output_rows.append({"file": path.name, "bytes": path.stat().st_size, "sha256": digest(path), "width": image.width, "height": image.height})
    overview = Image.new("RGB", (1800, 2040), "#fffaf7")
    draw = ImageDraw.Draw(overview)
    text(draw, (30, 15), "6名・前後左右｜頭と胴体の配置確認", 42, True)
    text(draw, (30, 76), "素材・コード再現プレビュー（Unity／Pixel実画面ではありません）", 27, True, "#a44931")
    text(draw, (30, 117), f"描画コード {revision_label} ／静止・同じ表示倍率／詳細JPEGには歩行2位相と首元拡大を収録", 23)
    for col, (_, label) in enumerate(DIRECTIONS):
        text(draw, (190 + col * 400 + 145, 161), label, 29, True)
    for row, character in enumerate(characters):
        y = 205 + row * 290
        text(draw, (30, y + 110), NAMES[character.name], 30, True)
        for col, (direction, _) in enumerate(DIRECTIONS):
            overview.paste(white_composite(character.render(direction, size=(400, 280), scale=3.1, baseline=255)), (190 + col * 400, y))
    text(draw, (30, 1976), "正式素材を現描画座標で再現。背景・接地影・Sorting・Unity圧縮は含みません。実機合格の結果ではありません。", 23)
    path = args.output / "all-six-directions.jpg"
    save_jpeg(overview, path)
    output_rows.insert(0, {"file": path.name, "bytes": path.stat().st_size, "sha256": digest(path), "width": overview.width, "height": overview.height})
    before_mesh_files = {}
    if args.before_meshes:
        comparison = Image.new("RGB", (1910, 2240), "#fffaf7")
        d = ImageDraw.Draw(comparison)
        text(d, (25, 15), "頭・胴体の位置補正 Before / After", 40, True)
        text(d, (25, 76), "原素材と実描画コードの再現画像です。Unity／Pixelの実画面・合格証拠ではありません。", 25, True, "#a44931")
        for col, direction in enumerate(("左", "右", "背面")):
            text(d, (180+col*600, 124), direction+" Before", 25, True)
            text(d, (480+col*600, 124), direction+" After", 25, True)
        for row, current in enumerate(characters):
            before = Character(current.name, args.before_meshes, atlas, bounds)
            before_mesh_files[before.mesh_path.name] = digest(before.mesh_path)
            y = 180+row*330
            text(d, (12, y+140), NAMES[current.name], 24, True)
            for col, direction in enumerate(("left", "right", "back")):
                for version, character in enumerate((before, current)):
                    comparison.paste(white_composite(character.render(direction, size=(300,320), scale=3.5, baseline=300)), (110+col*600+version*300,y))
        text(d, (25, 2180), "頭の位置以外の素材・サイズ・角度・胴体と手足は同一。正面は正式一枚絵のままです。", 25)
        path = args.output / "all-six-before-after.jpg"
        save_jpeg(comparison, path)
        output_rows.append({"file":path.name,"bytes":path.stat().st_size,"sha256":digest(path),"width":comparison.width,"height":comparison.height})
    receipt = {"kind": "software-source-preview-jpegs", "date_jst": "2026-11-02", "runtime_revision": runtime_revision, "working_tree_runtime_digest": runtime_digest, "working_tree_candidate": bool(dirty),
               "native_unity": False, "pixel_verified": False, "adopted": False,
               "geometry": "actual production Graphic methods via minimal CLR geometry API; actor62x78; no placement correction in rasterizer",
               "front": "unchanged formal atlas crop from CharacterBodyGraphic.Bounds; front poses remain the formal single sprite",
               "raster": "original RGBA crops and original builder masks; per-part affine mapping, original draw order; no repainted or generated art",
               "limitations": ["Not Unity/Pixel images", "No atlas packing/compression/masking UI/shadow/Sorting reconstruction", "Static poses cannot verify frame timing or turn lag"],
               "visual_status": "Pending native continuous visual review; source-rendered images and coordinates cannot establish Unity/Pixel acceptance.",
               "runtime_files": {f: hashlib.sha256((ROOT / f).read_text().replace("\r\n", "\n").encode()).hexdigest() for f in runtime_files},
               "source_files": INPUTS, "mesh_files": {c.mesh_path.name: digest(c.mesh_path) for c in characters}, "before_mesh_files":before_mesh_files,"images": output_rows}
    (args.output / "preview-source.json").write_text(json.dumps(receipt, ensure_ascii=False, indent=2) + "\n")
    print(f"Created {len(output_rows)} JPEGs; original source SHA verified; software preview only.")


if __name__ == "__main__":
    main()
