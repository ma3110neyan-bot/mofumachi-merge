#!/usr/bin/env python3
"""Build or check bundled UI subsets from checksum-verified, licensed source TTFs."""
import argparse
import hashlib
import os
from pathlib import Path
import tempfile
from fontTools import subset
from fontTools.ttLib import TTFont

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / 'Assets/Mofumachi/Resources/Mofumachi'
SOURCES = (
    ('ZenMaruGothic-Medium.ttf', 'UIFont', 'Mofumachi Pop UI', '3cfdb98a13571ede17fcc769f5093a97c38b80a7b9b2ab754a26b4d822092b3b'),
    ('ZenMaruGothic-Bold.ttf', 'UIFontBold', 'Mofumachi Pop UI Bold', 'fe24426b9c8b5523a0146a8235c8674eccf0493af354a53ec895c3596d9eb745'),
    ('KiwiMaru-Medium.ttf', 'UIFontStory', 'Mofumachi Story UI', 'b2659f300a7d48c3f29eb273ffc5e1b26cc416ac8c37ff6bb2f3e43c2f4d235a'),
    ('KleeOne-SemiBold.ttf', 'UIFontBody', 'Mofumachi Storybook Body', 'b031ec426c23ca1143ef1f7d58bee7a79efe119ed654152f121c922202b303fd'),
)

def required_codepoints():
    text = ''.join(p.read_text(encoding='utf-8') for area in ('Core', 'Presentation')
                   for p in (ROOT / 'Assets/Mofumachi' / area).glob('*.cs'))
    points = {ord(c) for c in text if ord(c) > 127 and not c.isspace()}
    points.update(range(32, 127))
    points.update(range(0x3041, 0x3095))
    points.update(range(0x30a1, 0x30fb))
    for _, key, _, _ in SOURCES:
        path = OUTPUT / (key + '.ttf')
        if path.exists():
            with TTFont(path) as font:
                points.update(font.getBestCmap())
    return points

def check(path, points):
    with TTFont(path) as font:
        cmap = font.getBestCmap()
        missing = points - set(cmap)
        if missing:
            raise ValueError(f'{path.name}: missing '+','.join(f'U+{c:04X}' for c in sorted(missing)))
        if any(c in cmap for c in (0xffff, 0x0378, 0x0416)):
            raise ValueError(f'{path.name}: unsupported-glyph sentinels must stay absent')
        return len(cmap)

def main():
    args = argparse.ArgumentParser(description=__doc__)
    args.add_argument('--source-dir', type=Path, required=True)
    args.add_argument('--check', action='store_true')
    args.add_argument('--only', choices=[source[1] for source in SOURCES],
                      help='Build/check one subset without replacing other font assets')
    opts = args.parse_args()
    active = [source for source in SOURCES if opts.only is None or source[1] == opts.only]
    for filename, _, _, digest in active:
        if hashlib.sha256((opts.source_dir / filename).read_bytes()).hexdigest() != digest:
            raise ValueError(f'Source checksum mismatch: {filename}')
    points = required_codepoints()
    if opts.check:
        for _, key, _, _ in active:
            count = check(OUTPUT / (key + '.ttf'), points)
            print(f'{key}: {count} glyphs, all required text covered')
        return
    # Validate every source and every generated result before replacing any asset.
    with tempfile.TemporaryDirectory(prefix='mofumachi-fonts-') as directory:
        generated = []
        for filename, key, family, _ in active:
            with TTFont(opts.source_dir / filename) as font:
                if points - set(font.getBestCmap()):
                    raise ValueError(f'Source lacks required glyphs: {filename}')
                settings = subset.Options()
                settings.name_IDs = ['*']; settings.name_legacy = True; settings.name_languages = ['*']
                builder = subset.Subsetter(options=settings)
                builder.populate(unicodes=points); builder.subset(font)
                for record in font['name'].names:
                    if record.nameID in (1, 3, 4, 6, 16):
                        record.string = (family.replace(' ', '') if record.nameID == 6 else family).encode(record.getEncoding())
                path = Path(directory) / (key + '.ttf'); font.save(path)
            check(path, points); generated.append((path, OUTPUT / path.name))
        for source, target in generated:
            # Stage next to the destination so os.replace stays on one filesystem.
            staged = target.with_suffix('.ttf.tmp'); staged.write_bytes(source.read_bytes())
            os.replace(staged, target)
    for _, key, _, _ in active:
        print(f'{key}: {check(OUTPUT / (key+".ttf"),points)} glyphs, source SHA verified')

if __name__ == '__main__':
    main()
