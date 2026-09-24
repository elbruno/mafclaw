# Session 4 cumulative code

Target: .NET 10. `Contracts` is plain C#; `Agent` composes the shared MAF
instructions, tools and policies. `Console`, `Evals` and `Hosted` are thin hosts.
No project depends on a previous session's source.

For the final-app walkthrough, start at the selected host's `Program.cs` and
follow its A/B/C comments. Then open `Agent\FinanceAgentFactory.cs` to show how
the same definition selects providers and authority for each host.
`FinanceConsole`, `FinanceTools`, `FinanceTelemetry`, `FinanceEvaluations` and
`Hosted\FinanceWebHost` explain the key execution, approval, observation,
grading and HTTP boundaries beside the code. Present the numbered samples
first; these shared files are the composition reveal, not the introduction.

```powershell
dotnet run --project .\Console\MafClaw.Session04.Console.csproj -- --fixture
dotnet run --project .\Evals\MafClaw.Session04.Evals.csproj -- --mode fixture
dotnet run --project .\Hosted\MafClaw.Session04.Hosted.csproj -- --fixture --urls http://127.0.0.1:8088
```

The original `MafClaw.Session04.csproj` placeholder is retired. See
[setup](../docs/setup.md) for live configuration and
[architecture](../docs/architecture.md) for the intentional local/hosted
differences. All data is synthetic and educational, not financial advice.
