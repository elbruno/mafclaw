# Session 2 upgrade regressions

This .NET 10 executable links the actual Sample 22 approval policy and Sample 32
JSON-memory source. It uses no model, credentials, or test-framework packages.

From the Session 2 directory:

```powershell
dotnet run --project .\tests\UpgradeRegression.Tests\MafClaw.UpgradeRegression.Tests.csproj -c Release
```

Fifteen assertions cover approve/deny/retry/exhaustion/cancellation, empty-fact
side-effect prevention, persistence, deduplication, scope mismatch and corrupt
JSON. Deadline outcomes use injected input rather than wall-clock sleeps.
This is **not** live Foundry memory or real console-timeout verification.
Files are isolated beneath this executable's output and removed after the run.
Use `dotnet run`, not an empty `dotnet test` invocation.
