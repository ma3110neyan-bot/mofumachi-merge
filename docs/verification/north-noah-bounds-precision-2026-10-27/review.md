# Scoped review: North/Noah bounds precision

Verdict: approve the scoped validation fix. Critical: 0. Important: 0. No actionable finding in the exact `review.patch` against base `4975fb1319ee61aa6f46c09cb023cd007ebe12dd`.

- `scripts/north-noah-review.ps1:33` compares all four bounds as exact `Single` values, matching the unchanged C# `NorthWorldBounds` float fields (`ExperienceCaptureTests.cs:46`). Both compact decimal JSON and the expanded native float encoding represent the same permitted runtime coordinates. Adjacent representable float coordinates remain unequal; the old `y_max` tolerance is removed.
- The existing `Number` helper still runs before each conversion. Missing/null values, strings, arrays/objects, booleans, NaN and infinity remain rejected. An oversized finite number cannot become an expected finite bound through the `Single` cast.
- `scripts/tests/north-noah-evidence.tests.ps1:40–53` covers compact bounds, expanded Single-derived numeric JSON, and both adjacent IEEE754 Single encodings for each of the four fields (eight negative cases). Each field is restored before proceeding; the negative `x_min` encoding reverses numeric direction but still tests both neighbors.
- The added casts, `BitConverter` overloads, hashtable operations and control flow are available in Windows PowerShell 5.1/.NET Framework. This is static compatibility review, not a Windows execution result.

Evidence reviewed: exact patch, current producer/type guard, existing RED/GREEN logs, and the earlier native `fountain-live.json:1797` value `0.6000000238418579`. The RED log reaches the original bounds exception; GREEN completes the synthetic metadata suite. No suites were rerun for this review. No C# producer, runtime, art, navigation, Save, fountain or rendering input is changed by the reviewed patch.

This verdict concerns the metadata precision repair only. The new raw native XML/JSON/PNGs have not been received, and this review does not establish full-suite completion, native image acceptance, APK generation, Pixel verification or adoption.

Codex／AI側から見た次の改善候補：今回はなし。
