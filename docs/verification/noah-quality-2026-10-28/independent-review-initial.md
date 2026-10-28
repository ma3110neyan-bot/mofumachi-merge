# Independent review — initial findings, before remediation

Review base: d54812f; working branch feat/daily-world-pass-20261016.
Reviewer changed no repository files. This record preserves findings and exact output from focused reviewer reproductions; it is not native Unity or Pixel evidence.

## Important findings

1. Combined gait and greeting folds the mesh. Original TownNoah25DTrial.cs lines36/51 allowed Sample(π/2,1,1,0,2,.4,false,true).Hand=1.083383. Exact C# loaded through PowerShell Add-Type gave the second triangle for u=.18–.30,v=.44–.49 negative signed area.
2. Head-turn mask stretched the visible original tail. Original line50 applied its vertical head mask across all u; at u=.85 the x offsets differed by1.6 logical pixels between v=.44 andv=.49. Original tail test sampled onlyv≤.44.
3. Candidate scope downgrade bypassed the new accepted-North freeze. Original visual-lock.ps1 lines60/152 selected freeze enforcement only from mutable scope_id. An isolated test fixture changed scope_id back to town_north_noah_trial_2026_10_27, appended a forbidden change to TownNorthMasterLayout.cs, rebound its after_sha256, and passed both Get-BasePolishPatch and Assert-OriginalBaseForCandidate.

## Exact scope reproduction command and observed output

The exact generated PowerShell script from the initial review is preserved beside this report as independent-review-initial-scope-repro.ps1. It builds a temporary fixture, runs existing focused lock checks, then appends the downgrade reproduction. It modifies only its temporary fixture. The submitted original command was:

```text
/workspace/toolchains/powershell/7.4.6/pwsh -NoProfile -File /tmp/noah-review-scope-repro.ps1
```

Exact observed tool output, transcribed here from the initial execution result:

```text
REPRO: accepted-North edit passed Get-BasePolishPatch and Assert-OriginalBaseForCandidate after scope_id downgrade.
```

The execution returned exit_code0. This was observed against the initial code before freeze-gate remediation; rerunning against fixed gate code should reject the downgrade.

## Exact C# numeric reproduction output

PowerShell Add-Type loaded the two unmodified files TownCharacterMotion.cs and TownNoah25DTrial.cs. For Sample(π/2,1,1,0,2,.4,false,true), actual mesh triangle(a,c,d) at x=.18/.30,y=.44/.49, art size61.33746×78:

```text
Exact current C# combined gait+greeting second triangle signed area: -7.12574927368768
Hand=1.083383
```

A separate call with the same pose returned:

```text
Tail u=.85 v=0.44 offset: 0,0
Tail u=.85 v=0.49 offset: 1.6,0
Tail u=.85 v=0.6 offset: 1.6,0
```

## Minor evidence limitation

Reconstructed OFF/ON comparisons preserve remaining reaction seconds even though new/legacy durations differ. This is a same-clock rendering-switch comparison, not equal-elapsed prior behavior or historical native Before.

## Declined judgments

No native-render quality, Unity runtime success, FPS, thermal, memory, or Pixel approval inferred. North product files matched BASE. Other five residents retained their branches. Sorting used world-footY and the new fixture checked the correct sibling ordering. No unrelated full suite was run by this reviewer.
