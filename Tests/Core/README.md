# Unity ライセンス不要のコア検証

Unityプロジェクトと同じ`Assets/Mofumachi/Core`およびEditMode用NUnitテストを、.NET 8でもコンパイル・実行する。これはUnity EditMode/PlayMode実行やAndroid実機検証の代替ではない。

クラウドではUnity同梱SDKを使う。通常の.NET 8 SDKでも実行できる。

```bash
export DOTNET_CLI_HOME=/workspace/runtime/dotnet
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export NUGET_PACKAGES=/workspace/toolchains/nuget
/workspace/toolchains/unity/6000.6.4f1/Editor/Data/DotNetSdk/dotnet restore Tests/Core/Mofumachi.Core.Tests.csproj --locked-mode
/workspace/toolchains/unity/6000.6.4f1/Editor/Data/DotNetSdk/dotnet test Tests/Core/Mofumachi.Core.Tests.csproj --no-restore
/workspace/toolchains/unity/6000.6.4f1/Editor/Data/DotNetSdk/dotnet build Tests/Compatibility/Mofumachi.Core.Compatibility.csproj
```

成果物は`Tests/Core/bin`と`obj`で、Gitのローカル除外対象。保存テストは個別の一時ディレクトリを使い、実際のプレイヤーセーブを変更しない。

互換性プロジェクトは同じ製品コードを.NET Standard 2.1 / C# 9としてビルドする。Unity Editor、IL2CPPのシリアライズ、Android上のファイル置換はライセンス有効化後に別途検証する。
