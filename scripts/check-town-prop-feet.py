"""Read-only analytical growth-foot audit; this is not Unity render evidence.

python3 scripts/check-town-prop-feet.py

Reports opaque samples in the lower 10% of each atlas crop whose projected
position is inside the actual authored floor. Inspect arches/bunting as supporting
posts, not solid centers. Upper artwork naturally overlaps background paving;
this intentionally probes only the low base. No repository files are written.
"""
import argparse
import re
from pathlib import Path

from PIL import Image


NUMBER = r"([0-9.]+)f?"


def read_floor(repository):
    source = (repository / "Assets/Mofumachi/Presentation/TownWalkableArea.cs").read_text()
    polygons = []
    for line in source.splitlines():
        if "AddMap(new[]" in line:
            polygons.append([tuple(map(float, pair)) for pair in
                             re.findall(r"P\(" + NUMBER + "," + NUMBER + r"\)", line)])
    holes = [tuple(map(float, values)) for values in re.findall(
        r"Island\(" + ",".join([NUMBER] * 4) + r"\)", source)]
    if not polygons or not holes:
        raise ValueError("Could not parse the actual floor polygons and island holes")

    def contains(x, y):
        if any(((x - u) / rx) ** 2 + ((y - v) / ry) ** 2 < .9999
               for u, v, rx, ry in holes):
            return False
        for polygon in polygons:
            inside = False
            for index, point in enumerate(polygon):
                previous = polygon[index - 1]
                if ((point[1] > y) != (previous[1] > y) and
                        x < (previous[0] - point[0]) * (y - point[1]) /
                        (previous[1] - point[1]) + point[0]):
                    inside = not inside
            if inside:
                return True
        return False

    # Avoid silently dropping the holes or reporting reserved cafe furniture.
    assert not contains(.14, .53)
    assert not contains(.147238, .525175)
    return contains, len(polygons), len(holes)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--base-fraction", type=float, default=.10)
    parser.add_argument("--alpha", type=int, default=192)
    arguments = parser.parse_args()
    repository = arguments.repository.resolve()
    if not 0 < arguments.base_fraction <= 1 or not 0 <= arguments.alpha <= 255:
        parser.error("base-fraction must be (0,1], alpha must be [0,255]")
    presentation = repository / "Assets/Mofumachi/Presentation"
    resources = repository / "Assets/Mofumachi/Resources/Mofumachi"
    contains, polygon_count, hole_count = read_floor(repository)
    source = (presentation / "TownGrowthPresentation.cs").read_text()
    enum = re.search(r"enum TownGrowthProp\s*\{([^}]+)\}", source).group(1)
    names = [name.strip() for name in enum.split(",")]
    graphics = (presentation / "TownGrowthPropGraphic.cs").read_text()
    bounds = [tuple(map(int, values)) for values in re.findall(
        r"new Rect\((\d+),(\d+),(\d+),(\d+)\)", graphics)]
    if len(bounds) != len(names):
        raise ValueError("Atlas crop count does not match the enum")
    atlases = [Image.open(resources / name) for name in
               ("TownGrowthPropsAtlas.png", "TownStreetPropsAtlas.png")]
    map_source = (presentation / "TownWorldMap.cs").read_text()
    world_width = float(re.search(r"\bWidth=" + NUMBER, map_source).group(1))
    world_height = float(re.search(r"\bHeight=" + NUMBER, map_source).group(1))
    print(f"Parsed {polygon_count} floor polygons and {hole_count} islands.")
    print(f"Opaque alpha >= {arguments.alpha}; bottom {arguments.base_fraction:.0%}; pixel stride X4/Y2.")
    print("UV samples use strict polygon interiors; Unity's tiny boundary tolerance is omitted.")
    checked = flagged = 0
    for region, block in zip(("West", "Central", "East"), source.split("new[]{")[1:]):
        pattern = (r"P\(TownGrowthProp\.(\w+),(\d+)," + ",".join([NUMBER] * 3) + r"\)")
        for kind, level, anchor_x, anchor_y, width in re.findall(pattern, block):
            ax, ay, prop_width = map(float, (anchor_x, anchor_y, width))
            index = names.index(kind)
            crop_x, crop_y, crop_width, crop_height = bounds[index]
            atlas = atlases[index >= 6]
            prop_height = prop_width * crop_height / crop_width
            overlaps, total = [], 0
            for pixel_y in range(int(crop_height * (1 - arguments.base_fraction)), crop_height, 2):
                for pixel_x in range(0, crop_width, 4):
                    if atlas.getpixel((crop_x + pixel_x, crop_y + pixel_y))[3] < arguments.alpha:
                        continue
                    total += 1
                    map_u = ax + (pixel_x / crop_width - .5) * prop_width / world_width
                    map_v = ay - (1 - pixel_y / crop_height) * prop_height / world_height
                    if contains(map_u, map_v):
                        overlaps.append((map_u, map_v))
            checked += 1
            if overlaps:
                flagged += 1
                u, v = overlaps[len(overlaps) // 2]
                print(f"{region:7} {kind:14} Lv{level} anchor=({ax:g},{ay:g}) "
                      f"width={prop_width:g} on-floor={len(overlaps)}/{total} "
                      f"sample=({u:.6f},{v:.6f})")
    declared=len(re.findall(r"P\(TownGrowthProp\.",source))
    if checked==0 or checked!=declared:
        raise ValueError("Not every declared prop was audited")
    print(f"{flagged}/{checked} placements flagged; inspect actual solid supporting bases.")
    if flagged:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
