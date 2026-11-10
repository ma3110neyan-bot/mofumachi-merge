# Release source guard fix implementation plan

Goal: implement the user-approved snapshot → full diff validation → known-only byte restore/cleanup → unchanged full source guard flow, inside the existing release script and its direct test file.

Constraints: no Unity/AAB/APK/preflight/Stage7/key operations. No original settings, game, QA builder or VisualLock edits. Execute only the source-guard test selection once after implementation. First failure ends verification, no retry.

1. Add real-Git isolated-worktree tests first. Cover byte recovery, exact generated paths, unknown content/path rejection, staged/HEAD/Git errors, corrupt snapshot/readback and cleanup failures, symlinks, and formal repository protection.
2. Add capture/recovery helpers inside build-android-release.ps1. Match exactly three known YAML files, validate contents before any write, restrict known graphics representation to semantically identical OpenGLES3. Unconfirmed non-hex trailing i must fail closed. Do not accept a real keystore path change as an empty-value serialization change.
3. Restrict snapshot/restore/deletion to the registered detached release worktree beneath the existing release root and its separate Diagnostics directory. Main entry keeps its literal E-drive checks. Use atomic snapshot replacement to avoid modifying another hard-linked inode; prevalidate all candidates, reject links, list SHA/path before cleanup, delete only enumerated known intermediates and the exact Gradle stop log. Root .vsconfig stays unchanged.
4. Wire snapshot before each Unity invocation and recovery after successful Unity exit. Keep final Get-AndroidReleaseSourceCheck unchanged and mandatory. Keep original formal-repository checks.
5. Review syntax, scope, and invariants before the single source-guard test invocation. Run no RED cycle or full suite because the user's one-run requirement overrides skill defaults. Record the first failing case and stop if needed.
6. Save actual test result, changed paths, protected hashes and limitations to fixed review locations, then normal commit/push once. Preserve old runs, artifacts and receipts. AAB retry readiness cannot be YES until the reported graphics mutation is supported by reliable evidence; no actual build in this task.
