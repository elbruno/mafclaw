# Session 4 cumulative code

Target: .NET 10. `Contracts` is plain C#; `Agent` composes the shared MAF
instructions, tools and policies. `Console`, `Evals` and `Hosted` are thin hosts.
No project depends on a previous session's source.

```powershell
dotnet run --project .\Console\MafClaw.Session04.Console.csproj -- --fixture
dotnet run --project .\Evals\MafClaw.Session04.Evals.csproj -- --mode fixture
dotnet run --project .\Hosted\MafClaw.Session04.Hosted.csproj -- --fixture --urls http://127.0.0.1:8088
```

The original `MafClaw.Session04.csproj` placeholder is retired. See
[setup](../docs/setup.md) for live configuration and
[architecture](../docs/architecture.md) for the intentional local/hosted
differences. All data is synthetic and educational, not financial advice.
