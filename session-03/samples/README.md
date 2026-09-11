# Session 03 samples

Session 03 uses the same concept-to-agent ladder as Session 02:

- `10`, `20`, `30`, and `40` isolate each primitive in plain C#.
- `11`, `21`, `31`, and `41` show the corresponding Microsoft Agent Framework
  integration surface.
- `..\code\` composes the full finance advisor for the session.

| Sample | Topic | Plain concept | MAF bridge |
|---|---|---|---|
| 10 / 11 | Skills | Read a discoverable skill catalog | Register the catalog as an agent context surface |
| 20 / 21 | Shell | Validate and run one confined command | Expose the boundary as a MAF tool |
| 30 / 31 | CodeAct | Calculate portfolio value with explicit code | Register the calculator as a MAF function |
| 40 / 41 | Background agents | Queue and observe independent research | Model a MAF background-agent handoff |

The bridge samples are intentionally offline-safe: they show the C# tool
registration boundary without requiring Foundry credentials. The complete live
`AsHarnessAgent` wiring belongs in the session `code` app once the hosted
feature APIs are pinned and validated.

## Run the samples

```powershell
dotnet run --project .\samples\10-skills\MafClaw.Sample10.csproj
dotnet run --project .\samples\11-skills-agent\MafClaw.Sample11.csproj
dotnet run --project .\samples\20-confined-shell\MafClaw.Sample20.csproj
dotnet run --project .\samples\21-confined-shell-agent\MafClaw.Sample21.csproj
dotnet run --project .\samples\30-codeact-calculation\MafClaw.Sample30.csproj
dotnet run --project .\samples\31-codeact-agent\MafClaw.Sample31.csproj
dotnet run --project .\samples\40-background-queue\MafClaw.Sample40.csproj
dotnet run --project .\samples\41-background-agents\MafClaw.Sample41.csproj
```

All values are mock educational data. These samples are not financial advice.
