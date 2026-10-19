# Five connected plazas, reference-led Merge

Latest authority: user October19 feedback and twelve real Pixel screenshots in `Screenshot (2026_10_19 15_34_00) (1).pdf`. The user rejects the October18 wide painting, not its art style. Existing broad presentation/world/UI authorization and the request to continue without repeated approval gates apply. No document is a completed deliverable.

## Required player outcome
Five full-screen-sized places in a cross: central fountain plaza, west cafe, east florist/shop, north quest cottage, south garden. Walk and drag freely across broad floor areas; camera follows across joins without scene switches. Five quick-travel choices are secondary. Each place must read as its own broad scene with a landmark, loop, rest stops and space for residents, not a zoomed crop of one small garden. No large blur boundaries.

## Architecture decisions
Build a continuous ground surface and paving from the navigation geometry; place transparent building/plant/furniture art on it. Preserve existing illustration style, official six, logo and title. Native world model, camera and avoidance remain, with presentation-only cardinal districts. Stable Core West/Central/East region/place IDs and saved progression remain; North and South use Central as the legacy activity category. New presentation-only destinations are not written into SaveData. Daily walking records crossing any cardinal district, without changing goals/rewards.

World units: five districts at Central(0,0), West(-1.2,0), East(1.2,0), North(0,2), South(0,-2). Each district spans1.2×2 units; continuous bounds[-1.8,1.8]×[-3,3]. Camera source/project use the actual field RectTransform. Portrait view width1.10; buildings occupy roughly0.45..0.58 units, fountain0.34. Distinct broad floor polygons and planted/building holes cover each district with multiple passage routes. Corner land outside the cross remains naturally landscaped/blocked, not blank.

## Merge
September22 reference remains authority: warm, POP, clearly separated flat5×6 cells, visible large item art, a complete but thin decorative frame. Preserve saved30 cells and exact merge/delivery/reward behavior. Never fill an existing board for the sake of a screenshot. Selected item information and current purpose are visible; after story completion show actual daily merge progress and next daily action rather than duplicate terminal all-complete messages. Keep all management and delivery actions reachable; avoid a row of tiny competing controls.

## Evidence and constraints
Keep BGM and book/daily state intact; no SDK/payment/main merge. Pure geometry/routes/progress tests plus native input/mask/font/lifecycle checks. Actual Unity screen captures and same-source APK required; cloud reference compilation is not Unity rendering. Do not claim Pixel acceptance without updated-device screenshots and user play feedback. The provided Pixel screenshots are the rejected Before; generated art and diagrams are assets, never After proof.
