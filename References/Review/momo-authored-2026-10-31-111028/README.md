# Momo native capture — front mesh read failure

User supplied both parts of `momo-authored-20261031-111028-ee089f64`. `native/` preserves all55 original files unchanged, including49 decoded720×1280 images. `receipt.json` records archive and file SHA256/bytes/dimensions; archive CRC, safe paths and duplicate entries were checked. Original CRLF/BOM bytes are retained.

Source `2135f5caa02595cb54e4c774e5829ca8dfdc12e2`, fingerprint `3308fae59311e993ff37df090c50b08575ae9c13c59e0d459007a8582ca713f3`. The XML remains **0 passed / 1 failed / 0 skipped**, with `System.NullReferenceException` at `ExperienceCaptureTests.CharacterAuthored.cs:145` reading the front body's mesh after `walk-0354`.

The previous `stop-0241` name-caption comparison returned successfully and its natural Before was saved separately. Eight pairs were saved, but the method failed before writing completed proof. Independently measured difference bounding boxes are recorded without claiming complete metadata validation or native acceptance.

The capture helper's unconditional final mode setter temporarily disabled the front body and cleared its UGUI mesh. The caller then read that mesh before rebuilding the canvas. The scoped fix restores mode only if disabled, retaining a successfully captured mesh, and adds explicit enabled/non-null/non-empty assertions before the unchanged exact front-vertex comparison. Noah master, character assets and production runtime are unchanged. API compilation and the relevant PowerShell guards passed; **corrected Unity / APK / Pixel remain unverified**. No full suites were rerun.
