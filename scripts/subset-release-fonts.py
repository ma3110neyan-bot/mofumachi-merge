#!/usr/bin/env python3
"""Generate ONLY the release role fonts from pinned OFL sources; never replace legacy assets."""
import argparse
import hashlib
import importlib.util
import json
import sys
import tempfile
from pathlib import Path
from fontTools import subset
from fontTools.ttLib import TTFont

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Assets/Mofumachi/Resources/Mofumachi'
MANIFEST = ROOT / 'References/Fonts/release-fonts.json'
SOURCES = (
    ('hachimarupop/HachiMaruPop-Regular.ttf', 'UIFontAccent', 'Mofumachi Accent', 'Hachi Maru Pop Regular', '78408910c8f1a2f174a279cbc1484b48b71780039eba3fe1be2bfcc5d4df3f98', 'hachimarupop/HachiMaruPop-Regular.ttf'),
    ('ZenMaruGothic-Medium.ttf', 'UIFontReading', 'Mofumachi Reading', 'Zen Maru Gothic Medium', '3cfdb98a13571ede17fcc769f5093a97c38b80a7b9b2ab754a26b4d822092b3b', 'zenmarugothic/ZenMaruGothic-Medium.ttf'),
    ('ZenMaruGothic-Bold.ttf', 'UIFontReadingBold', 'Mofumachi Reading Bold', 'Zen Maru Gothic Bold', 'fe24426b9c8b5523a0146a8235c8674eccf0493af354a53ec895c3596d9eb745', 'zenmarugothic/ZenMaruGothic-Bold.ttf'),
)

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def points():
    spec = importlib.util.spec_from_file_location('legacy_subsets', ROOT / 'scripts/subset-ui-fonts.py')
    module = importlib.util.module_from_spec(spec)
    previous = sys.dont_write_bytecode
    try:
        sys.dont_write_bytecode = True
        spec.loader.exec_module(module)
    finally:
        sys.dont_write_bytecode = previous
    return module.required_codepoints()

def check(required):
    manifest = json.loads(MANIFEST.read_text())
    for _, key, _, original, sha, _ in SOURCES:
        path = OUT / (key + '.ttf')
        with TTFont(path) as font:
            missing = required - set(font.getBestCmap())
            if missing:
                raise ValueError(key + ': missing ' + ','.join(f'U+{p:04X}' for p in sorted(missing)))
        entry = next(f for f in manifest['fonts'] if f['resource'] == key)
        if entry['source_sha256'] != sha or entry['subset_sha256'] != digest(path):
            raise ValueError(key + ': provenance checksum mismatch')
        print(f'{key}: {original}; {len(required)} required characters covered; subset SHA verified')

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-dir', type=Path)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    required = points()
    if args.check:
        check(required)
        return
    if args.source_dir is None:
        parser.error('--source-dir is required when generating')
    entries = []
    with tempfile.TemporaryDirectory() as directory:
        staged = []
        for filename, key, family, original, sha, upstream in SOURCES:
            source = args.source_dir / filename
            if digest(source) != sha:
                raise ValueError('Unverified source: ' + filename)
            with TTFont(source, recalcTimestamp=False) as font:
                if required - set(font.getBestCmap()):
                    raise ValueError('Source glyph coverage incomplete: ' + filename)
                options = subset.Options()
                options.name_IDs = ['*']; options.name_legacy = True; options.name_languages = ['*']
                builder = subset.Subsetter(options=options)
                builder.populate(unicodes=required); builder.subset(font)
                for name in font['name'].names:
                    if name.nameID in (1, 3, 4, 6, 16):
                        name.string = (family.replace(' ', '') if name.nameID == 6 else family).encode(name.getEncoding())
                generated = Path(directory) / (key + '.ttf')
                font.save(generated)
            staged.append((generated, OUT / generated.name))
            entries.append(dict(resource=key, family=family, source_family=original, source_sha256=sha,
                source_url='https://raw.githubusercontent.com/google/fonts/main/ofl/' + upstream,
                subset_sha256=digest(generated), license='SIL Open Font License 1.1'))
        for source, target in staged:
            target.write_bytes(source.read_bytes())
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    MANIFEST.write_text(json.dumps(dict(required_characters=len(required), fonts=entries), ensure_ascii=False, indent=2)+'\n')
    check(required)

if __name__ == '__main__':
    main()
