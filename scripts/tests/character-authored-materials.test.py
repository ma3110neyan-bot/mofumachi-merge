"""One-character source/material checks. These are not Unity or Pixel evidence."""
import hashlib
import json
from pathlib import Path
import sys
import unittest
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
CHARACTER = sys.argv.pop(1) if len(sys.argv) > 1 else 'momo'
ART = ROOT / f'References/Characters/{CHARACTER}-authored-parts-2026-10-30'

class AuthoredMaterials(unittest.TestCase):
    def test_formal_front_and_accepted_noah_are_unchanged(self):
        record = json.loads((ROOT / 'References/VisualLock/noah-authored-approved-2026-10-30.json').read_text())
        self.assertTrue(record['user_approved'])
        for entry in record['frozen_files']:
            self.assertEqual(hashlib.sha256((ROOT / entry['path']).read_bytes()).hexdigest(), entry['sha256'], entry['path'])
        self.assertEqual(hashlib.sha256((ROOT / 'Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png').read_bytes()).hexdigest(),
                         '23d14025f146c429da01b6e3a74ef803254755f8d149f9eb53516aacaa30163d')

    def test_three_directions_have_separate_rigid_parts_and_blink_heads(self):
        layout = json.loads((ART / 'layout.json').read_text())
        self.assertEqual(layout['characterId'], CHARACTER)
        self.assertEqual(set(layout['views']), {'left', 'right', 'back'})
        self.assertEqual(layout['front_crop'], [17, 12, 266, 324])
        for direction, view in layout['views'].items():
            self.assertEqual({p['id'] for p in view['parts']}, {'head','body','farArm','nearArm','farLeg','nearLeg','tail'})
            self.assertEqual(len(view['parts']), 7)
            source = layout['sources'][direction]
            raw = ART / source['file']
            self.assertEqual(hashlib.sha256(raw.read_bytes()).hexdigest(), source['sha256'])
            image = Image.open(raw)
            self.assertEqual(image.mode, 'RGBA')
            self.assertEqual(image.getchannel('A').getextrema()[0], 0)
            parts = view['parts'] + ([view['closedHead']] if direction != 'back' else [])
            for part in parts:
                x,y,w,h = part['crop']
                self.assertTrue(x>=0 and y>=0 and w>0 and h>0 and x+w<=image.width and y+h<=image.height)
                self.assertGreater(part['scale'], 0)
                self.assertGreater(image.getchannel('A').crop((x,y,x+w,y+h)).getextrema()[1], 100)
                self.assertTrue(all(0<=v<=1 for v in part['pivot']))
                if 'maskRows' in part:
                    self.assertEqual(len(part['maskRows']), h)
                    for row in part['maskRows']:
                        self.assertTrue(-1 <= row['min'] <= row['max'] < w)
            self.assertNotIn('pouch', [p['id'] for p in view['parts']], 'Do not invent a Noah pouch on Momo.')

if __name__ == '__main__':
    unittest.main()
