# Session 03 samples

Session 03 uses the same concept-to-agent ladder as Session 02:

- `10`, `20`, `30`, and `40` isolate each primitive in plain C#.
- `11`, `21`, `31`, and `41` show the corresponding Microsoft Agent Framework
  integration surface.
- `..\code\` composes the full finance advisor for the session.

| Sample | Topic | Plain concept | MAF bridge |
|---|---|---|---|
| 10 / 11 | Skills | Discover and load local `SKILL.md` packages with bundled resources | Load the same files with `AgentSkillsProviderBuilder` and run them through a live Harness agent |
| 20 / 21 | Shell | Validate and run one confined command | Expose a `LocalShellExecutor` as an approval-gated `run_shell` tool on a live Harness agent |
| 30 / 31 | CodeAct | Calculate portfolio value with explicit code | Let a live Harness agent read `holdings.csv` via `file_access`, then write and run Python in a Hyperlight sandbox to compute the answer |
| 40 / 41 | Background agents | Queue and observe independent research | Hand a live Harness agent a `TickerResearchAgent` via `BackgroundAgents` so it can fan research out concurrently |

Samples `10`, `20`, `30`, and `40` are offline-safe plain-C# primitives.
Samples `11`, `21`, `31`, and `41` are live Microsoft Agent Framework +
Harness demos that call a real Azure AI Foundry project:

- **11 (Skills)** — discovers the `valuation` and `risk-scoring` file-based
  skills under its own `skills/` folder via `AgentSkillsProviderBuilder`.
- **21 (Shell)** — confines a `LocalShellExecutor` to a seeded
  `working/confirmations` folder and exposes it as an approval-gated
  `run_shell` tool so the agent can tidy up messy trade-confirmation files.
- **31 (CodeAct)** — reads `working/holdings.csv` through the normal
  `file_access` tools, then writes and runs Python in a Hyperlight
  micro-VM sandbox (`HyperlightCodeActProvider`, `AlwaysRequire` approval)
  to compute totals and allocations instead of doing arithmetic in prose.
- **41 (Background agents)** — registers a lean `TickerResearchAgent`
  (plain chat-client agent with only `HostedWebSearchTool`) as a
  `BackgroundAgents` entry so the claw can research multiple tickers
  concurrently and aggregate the findings.

Configure their Foundry endpoint and model with
`.\tools\configure-user-secrets.ps1 -Session 3` before running any of them.

## Run the samples

```powershell
dotnet run --project .\samples\10-skills\MafClaw.Sample10.csproj
dotnet run --project .\samples\20-confined-shell\MafClaw.Sample20.csproj
dotnet run --project .\samples\30-codeact-calculation\MafClaw.Sample30.csproj
dotnet run --project .\samples\40-background-queue\MafClaw.Sample40.csproj

# Requires Foundry credentials configured for Session 3:
.\tools\configure-user-secrets.ps1 -Session 3
dotnet run --project .\samples\11-skills-agent\MafClaw.Sample11.csproj
dotnet run --project .\samples\21-confined-shell-agent\MafClaw.Sample21.csproj
dotnet run --project .\samples\31-codeact-agent\MafClaw.Sample31.csproj
dotnet run --project .\samples\41-background-agents\MafClaw.Sample41.csproj
```

All values are mock educational data. These samples are not financial advice.

