# Noah Authored Parts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans inline. Respect the user's Noah-only checks and no repeated full suites.

**Goal:** Replace the retired one-sheet approach with reviewable, faithful directional source art and independent parts for Noah only.

**Architecture:** Author and review new views in References first; the original atlas and product renderer remain unchanged. Only source art that passes identity review may feed an opt-in independent-parts rig. No generated material may be called Unity/device proof.

**Tech Stack:** existing Unity6000.6.4f1/uGUI, existing 2D Animation package if needed, image authoring tool, PNG/JSON/browser review.

**Spec:** docs/superpowers/specs/2026-10-29-noah-authored-parts-design.md

## Global Constraints

- Noah only; keep original face/eyes/colors/clothes/proportions/silhouette and all other five unchanged.
- Do not revive retired weighted-atlas trial, edit frozen town/North/fountain/Core/Save, add dependencies, run full suites or request another Pixel check now.
- Candidate art, browser rendering, Unity execution and Pixel measurements are separate statuses. No acceptance by file count.

## Review Focus

- Directional views accidentally redesign face/eyes, hat, flower, bow or tail: compare to exact source before any import.
- Left/right mirroring moves asymmetric accessories incorrectly: author directions deliberately.
- Part generation invents clothing, holes or visible seams: reject before animation.
- Generated art lacks transparent margins/consistent ground scale: inspect alpha and registered body dimensions.
- A material mockup is mistaken for a game screen: explicit artifact labels and unchanged product paths.

### Task 1: faithful directional material

Files: References/Characters/noah-authored-parts-2026-10-29/; docs/noah-authored-parts-review-2026-10-29.md.
Consumes: original CharacterBodyAtlas.png SHA23d14025…; original Noah top-row region1263,14,254,323.
Produces: original unmodified reference identity; raw generated candidate(s), SHA provenance and identity review; same-scale original/candidate comparison.
- [x] Inspect exact reference and search existing layered/directional sources.
- [x] Generate directional draft from reference using image tool. Inspect transparency, face, colors, clothing, tail and views before choosing a candidate. At most one focused correction before declaring a material issue.
- [x] Save the raw outputs and review. If identity fails, retain as rejected and do not integrate it. Documents/art need no mirrored software tests; validate original hash and image decode.

### Task 2: independent parts, only after material quality

Files: scoped candidate part images and JSON; optional isolated browser comparison, no product Resources.
Consumes: Task1 faithful candidate direction art. Produces: independently authored head/body/limbs/tail and pivots, not a single deformed atlas.
- [x] If Task1 identity holds, author clean independent parts with overlap for hidden joints and a rigid original face; inspect static reconstruction and all directions.
- [x] Expose a clearly labeled material/rig preview for comparison at normal town scale. Preserve raw evidence and exclude rejected art from runtime.
- [ ] If art remains insufficient, mark Task2 blocked by specific asset findings; do not pretend rig/Unity/APK/Pixel completion or send the user more test commands.

### Task 3: scoped handoff

Files: provenance/review records, current AGENTS status, minimal relevant verification if software was actually changed.
- [x] Verify original source and protected product paths remain unchanged; record native/APK/Pixel status accurately.
- [x] One fresh final review of actual deliverables; fix Important issues once, do not repeat full suites.
- [x] Save to existing feature branch and report concrete material/implementation state and next necessary action.

Ruling: user supplied the target/constraints and requested resumption after the concrete proposal; AGENTS13 allows internal choices. Proceed with isolated candidate authoring rather than repeat design approval. New formal character designs remain prohibited; art that drifts cannot be adopted.

Completion boundary: the left-only material/part preview exists and is reviewed, not final character acceptance. Full-direction art/rig, expression and game integration remain pending. Do not regenerate completed material work from unchecked future-stage items.
