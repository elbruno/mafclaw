# Session 03 samples

Session 03 uses the same concept-to-agent ladder as Session 02:

- `10`, `20`, `30`, and `40` isolate each primitive in plain C#.
- `11`, `21`, `31`, and `41` show the corresponding Microsoft Agent Framework
  integration surface.
- `..\code\` composes the full finance advisor for the session.

| Sample | Topic | Plain concept | MAF bridge |
|---|---|---|---|
| 10 / 11 | Skills | Discover and load local `SKILL.md` packages with bundled resources | Load the same files with `AgentSkillsProviderBuilder` and run them through a Harness agent |
| 20 / 21 | Shell | Validate and run one confined command | Expose the boundary as a MAF tool |
| 30 / 31 | CodeAct | Calculate portfolio value with explicit code | Register the calculator as a MAF function |
| 40 / 41 | Background agents | Queue and observe independent research | Model a MAF background-agent handoff |

Samples `10`, `20`, `30`, `31`, `40`, and `41` are offline-safe. Sample `11`
is intentionally a live Microsoft Agent Framework demo: it uses
`AgentSkillsProviderBuilder` to discover the same file-based skill packages
that Sample `10` makes visible, then supplies that provider to a Harness agent.
Configure its Foundry endpoint and model with
`.\tools\configure-user-secrets.ps1 -Session 3` before running it.

## Run the samples

```powershell
dotnet run --project .\samples\10-skills\MafClaw.Sample10.csproj
# Requires Foundry credentials configured for Session 3:
dotnet run --project .\samples\11-skills-agent\MafClaw.Sample11.csproj
dotnet run --project .\samples\20-confined-shell\MafClaw.Sample20.csproj
dotnet run --project .\samples\21-confined-shell-agent\MafClaw.Sample21.csproj
dotnet run --project .\samples\30-codeact-calculation\MafClaw.Sample30.csproj
dotnet run --project .\samples\31-codeact-agent\MafClaw.Sample31.csproj
dotnet run --project .\samples\40-background-queue\MafClaw.Sample40.csproj
dotnet run --project .\samples\41-background-agents\MafClaw.Sample41.csproj
```

All values are mock educational data. These samples are not financial advice.
