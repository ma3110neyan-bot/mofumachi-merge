# Frozen XML newline regression — 2026-10-30

Windows screenshot: pull already up to date, then Assert-NoahAuthoredFrozenInputs rejects Assets/Mofumachi/Core/link.xml before run creation, Unity, APK or install. The new candidate manifest classifies both linker XML files as binary. Git's Windows text checkout can turn LF into CRLF without a product content change.

Reproduced with the real PowerShell gate and the stored manifest hashes: the LF fixture passes; a CRLF-only fixture fails with `Frozen input changed: Assets/Mofumachi/Core/link.xml` (exit 1). After setting the two XML normalization flags true, the same newline regression passes (exit 0); both substantive linker XML edits and a modified PNG remain rejected. The test's text fixture selection was corrected to exclude separately copied XML after the classification change, avoiding duplicate fixture paths.

Only these two flags changed. All 552 protected paths and expected SHA256 values remain exactly as committed at d367f14. Existing text canonicalization is reused; the XML originals, runtime, native captures, artwork and save handling are untouched. The actual source's 552 frozen inputs pass. Logs: frozen-newlines-green.log and frozen-after-newline-fix.log. Tests run with PowerShell 7.4.6 on Linux; actual Windows policy, native Unity, APK and Pixel remain unverified. No full suites or device operations were run.

The launcher documentation also now applies RemoteSigned before the first dot-source in a child PowerShell, with ErrorActionPreference Stop. This does not persist an execution-policy change.
