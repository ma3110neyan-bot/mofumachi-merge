# Seamless town / facility / music pass

User authority: 2026-10-18 field rebuild request and Pixel PDF (10 pages). Baseline 46dcf8e. Implement the whole authorized bundle; no main integration.

## Design
One opaque, constant-scale, hand-painted world map replaces the active three-background/faded-lawn renderer. North cafe and quest cottage, central fountain park, south flower workshop and shop share one stone road network. Feet-space polygons are traced against that exact map, including fountain, flower beds, tables, buildings and fences as blockers. Camera bounds derive from the map and actual viewport, with horizontal exploration. Stable Core place IDs remain; Presentation maps IDs to authored new coordinates. NPC avoidance, finger ownership, daily/book services remain.

The painted quest and shop buildings get aligned interactive entrance/door overlays. A destination tap initiates walking; only arrival at the actual threshold starts opening the door and enters the existing screen. No remote screen transition. Nearby foot arrival offers explicit entry. New input, fast travel, pause and leaving Home cancel pending entry. Return from either screen restores the outside world and camera.

Keep town BGM bytes unchanged. Add two original acoustic arrangements for title and two for Merge; role-specific candidate preferences are auxiliary, independent of save v3. Music settings expose title, description, 8-second preview and use-in-game action. Title=anticipation, town=healing, Merge=light rhythm. Selection/volume/on-off survives restart. Preview restores prior role and playback. Final adoption remains pending Pixel listening.

## Constraints
No destructive SaveData change, no 5x6/rule/reward/delivery change, no official six-character redesign, payment, SDK or main merge. Preserve character book, daily flower and storybook Merge. Address existing clipped home/quest text within the same bundle. New assets do not replace official character masters.

## Acceptance
Native Unity captures for all three regions, both connections, both cabins and their entry flow, Merge/book/daily, and music candidate screens. Native tests zero failures/skips. Same-source APK + fingerprint + SHA256. Pixel update with install -r and actual save checks. Pixel walking/camera/music quality, heat and 10-minute play remain unconfirmed until real device evidence. Art source/map-coordinate inspection is not native rendering proof.
