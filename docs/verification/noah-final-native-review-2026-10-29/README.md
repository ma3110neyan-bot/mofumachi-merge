# Received Noah final trial and logging repair verification

Received source: `861d962f03acbdd77dcf615e992e3d8a87e4c8e5`. The exact single native Unity method **failed**, 0 passed/1 failed, because `TownActor.Bind` emitted `MOFUMACHI_NOAH_TRIAL_MODE current` inside Editor before `LogAssert.NoUnexpectedReceived()`. The 263 decoded native images are valid review material, not a passed test or APK/device result.

The existing failed native test is the engine reproduction. `editor-mode-log-red.log` separately reproduces the same source boundary in compiled IL. The verification-only `log-check.csproj`/`Program.cs` read the actual compiled `TownActor.Bind` method without loading a Unity engine. After the Android-player-only guard, Editor and Android-target Editor have no mode-log prefix or `Debug.Log` call; Android-player compilation retains one. Three Unity API compiles report zero warnings/errors. This is C# compilation/IL inspection, **not Unity engine execution, IL2CPP, an APK build, or a Pixel test**.

`retired-helper-red.log` demonstrates that the previous launcher allowed a retired candidate. The focused existing helper tests now reject true/null/string/missing retirement status and retain the active, unadopted fixture checks. `actual-retired-candidate-green.log` verifies the current rejected record is stopped before Unity, APK or device actions. No shape coefficients or capture assertions were altered.

`north-noah-lock-green.log` contains the frozen-input mutation regression; `results.json` records the validated original354/release502/North20/current24 inputs. Original art, accepted town, fountain, North boundary, the other five characters, Core/Save and normal Android builder are unchanged.

The native originals, their checksums and visual rejection are in `References/Review/noah-final-rig-2026-10-29-202030/`. The next-method proposal is `docs/noah-authored-rig-proposal-2026-10-29.md`. The one-atlas final trial is retired. No new whole suite, native success, APK, Pixel metrics, adoption or six-character rollout is claimed. The user does not need to rerun this trial.
