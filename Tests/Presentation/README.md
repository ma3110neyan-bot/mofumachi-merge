# Merge visual timing tests

Requires .NET 8. This project compiles the actual presentation-only `MergeVisualTiming.cs` without Unity stubs. It checks bounded selection scale, gathering completion, bounce reset at the existing .22-second lock boundary, the .5-second light lifetime, and invalid elapsed times.

```shell
dotnet restore Tests/Presentation/Mofumachi.Visual.Tests.csproj --locked-mode
dotnet test Tests/Presentation/Mofumachi.Visual.Tests.csproj --no-restore
```

These eight calculation tests do not execute Unity, UGUI, Android, or Pixel 3a. Native interaction/lifecycle/layout checks are in `Assets/Mofumachi/Tests/PlayMode/MergePolishTests.cs` and must run through Unity Test Runner. Run the existing Core test project as well when verifying the game and save rules.
