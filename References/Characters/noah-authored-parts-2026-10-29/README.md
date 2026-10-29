# Noah authored directional material candidates — NOT adopted

These are new-pose material drafts, not Unity captures, not Pixel3a images, and not an approved replacement for the formal character atlas. Original atlas stays byte-identical. Only Noah is being studied; no other character or town asset is changed.

`turnaround-draft-01.png` is the raw image-tool output, unedited. Reference: the far-right/top-row Noah in the original CharacterBodyAtlas.png. Generation requested four equally grounded views with original face/eyes/colors/clothes/body/tail. First author review found the left-facing bag on the wrong side; the front is also a re-illustration and must never be substituted for the exact original face without review.

`draft-01-regions.json` records measured visible bounds for review, not production sprite import geometry or a completed rig. Original/derived files and decisions will be bound in provenance.json.

The corrected `turnaround-draft-02.png` keeps the left-view pouch on the far anatomical-right hip. `parts-left-draft-01.png` supplies head, separate limbs/tail/pouch; its torso is excluded because it contained a hand remnant. `torso-left-draft-02.png` is the replacement torso. These are all raw, unedited generated drafts, not formal art approvals.

`parts-layout.json` selects measured crops and registration of eight independent parts. `rig-model.cjs` supplies browser-only rigid bone transforms and grounded transitions. `noah-material-review.html` packages exact original-front comparison, three new direction drafts and the **left-only** independent-part preview. Rebuild the self-contained file using `python3 scripts/build-noah-material-review.py`; it refuses a changed original atlas. Do not call this a Unity/Pixel screen or an all-direction completed character. Full status: `docs/noah-authored-parts-review-2026-10-29.md`.
