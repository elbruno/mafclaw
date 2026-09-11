# From Model to Agent: The Agent Framework Harness, Live in C#

A 4-part Microsoft Reactor live coding series that builds a personal finance CLI assistant using C#, .NET, and the Microsoft Agent Framework.

## Series resources

- **Series page:** https://aka.ms/mafclaw
- **Blog introduction:** http://aka.ms/mafclaw/blog
- **Sample repository:** http://aka.ms/mafclaw/repo
- **.NET YouTube recordings:**
  - [Session 1](https://www.youtube.com/watch?v=iUs15X1v2w4)
  - [Session 2](https://www.youtube.com/watch?v=V58coa0llUo)
  - [Session 3](https://www.youtube.com/watch?v=rMhX0-oE4aY)
  - [Session 4](https://www.youtube.com/watch?v=dCIBza-WxUc)
- **Target runtime:** .NET 10

## Sessions

| Session | Date | Title | Status | Links |
|---|---|---|---|---|
| 1 | Thu Sep 3, 2026 | Meet Your Claw: A Harness in Three Lines of C# | ✅ Ready — 4 incremental checkpoints + finished sample | [Session guide](./session-01/README.md) &#124; [Blog](https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/) &#124; [Live event](https://aka.ms/mafclaw/1) |
| 2 | Thu Sep 10, 2026 | Working With Your Data, Safely: Files, Approvals and Memory | ✅ Ready — isolated safety samples + final Harness finance advisor app | [Session guide](./session-02/README.md) &#124; [Blog](https://devblogs.microsoft.com/agent-framework/agent-harness-working-with-your-data-safely/) &#124; [Event](https://aka.ms/mafclaw/2) |
| 3 | Thu Sep 17, 2026 | Scaling the Claw: Skills, Shell, CodeAct and Background Agents | 🟡 Offline package ready; live bridge follow-up | [Blog](https://devblogs.microsoft.com/agent-framework/agent-harness-scaling-the-claw-or-harness-capabilities/) &#124; [Event](https://aka.ms/mafclaw/3) |
| 4 | Thu Sep 24, 2026 | Production Ready: Observability, Governance and Deployment | 🔲 Not started | [Blog](https://devblogs.microsoft.com/agent-framework/agent-harness-making-your-claw-production-ready/) &#124; [Event](https://aka.ms/mafclaw/4) |

## Repository layout

- `session-01/` — Session 1: four incremental checkpoints (hello-agent → harness → tools → planning+todos) and the finished sample.
- `session-02/` — Session 2: isolated samples for safe file access, approvals, and memory, plus the final Agent Framework/Harness finance advisor walkthrough.
- `session-03/` — Session 3: an offline runnable package covering skills, confined shell, CodeAct, and background agents (`code/`, `samples/`, `docs/`), cumulative with Session 2.
- `session-04/` — Placeholder folder. Not started; a real .NET 10 snapshot will replace it after the session goes live.
- `general/docs/` — Prerequisites, configuration, and troubleshooting for all sessions.

## Getting started

### Prerequisites

- **.NET 10 SDK** — https://dotnet.microsoft.com/download
- **Azure CLI** — https://learn.microsoft.com/cli/azure/install-azure-cli
- **PowerShell 7+** — for configuration scripts

### Quick start (Session 1)

```powershell
# 1. Clone and enter the repo
git clone https://github.com/elbruno/mafclaw
cd mafclaw

# 2. Configure Foundry credentials
.\tools\configure-user-secrets.ps1 -Session 1

# 3. Authenticate
az login --output none

# 4. Run the first checkpoint
cd session-01
dotnet run --project .\checkpoints\01-hello-agent\MafClaw.Checkpoint01.csproj
```

For the full walkthrough, see the [Session 1 guide](./session-01/README.md).

### Quick start (Session 2)

```powershell
# 1. Enter the repo
cd mafclaw

# 2. Configure Foundry settings for Session 2
$env:FOUNDRY_PROJECT_ENDPOINT = "https://YOUR-FOUNDRY-ENDPOINT.services.ai.azure.com/api/projects/YOUR-PROJECT"
$env:FOUNDRY_MODEL = "YOUR-MODEL-OR-DEPLOYMENT"
$env:FOUNDRY_MEMORY_STORE = "YOUR-MEMORY-STORE"            # optional
$env:FOUNDRY_EMBEDDING_MODEL = "YOUR-EMBEDDING-MODEL"     # optional
.\tools\configure-user-secrets.ps1 -Session 2

# 3. Run the paired base + agentic samples
dotnet run --project .\session-02\samples\10-safe-file-access\MafClaw.Sample10.csproj
dotnet run --project .\session-02\samples\11-safe-file-access-agent\MafClaw.Sample11.csproj
dotnet run --project .\session-02\samples\20-approval-gates\MafClaw.Sample20.csproj
dotnet run --project .\session-02\samples\21-approval-gates-agent\MafClaw.Sample21.csproj
dotnet run --project .\session-02\samples\30-memory-store\MafClaw.Sample30.csproj
dotnet run --project .\session-02\samples\31-memory-store-agent\MafClaw.Sample31.csproj

# 4. Run the full Session 2 finance advisor app
dotnet run --project .\session-02\code\MafClaw.Session02.csproj
```

For the full walkthrough, see the [Session 2 guide](./session-02/README.md).

### Quick start (Session 3)

```powershell
# 1. Enter the repo
cd mafclaw

# 2. No credentials needed — Session 3 is fully offline
cd session-03

# 3. Run the complete cumulative finance advisor
dotnet run --project .\code\MafClaw.Session03.csproj

# 4. Run the numbered sample ladder (skills, confined shell, CodeAct, background agents)
dotnet run --project .\samples\10-skills\MafClaw.Sample10.csproj
dotnet run --project .\samples\11-skills-agent\MafClaw.Sample11.csproj
dotnet run --project .\samples\20-confined-shell\MafClaw.Sample20.csproj
dotnet run --project .\samples\21-confined-shell-agent\MafClaw.Sample21.csproj
dotnet run --project .\samples\30-codeact-calculation\MafClaw.Sample30.csproj
dotnet run --project .\samples\31-codeact-agent\MafClaw.Sample31.csproj
dotnet run --project .\samples\40-background-queue\MafClaw.Sample40.csproj
dotnet run --project .\samples\41-background-agents\MafClaw.Sample41.csproj
```

For the full walkthrough, see the [Session 3 guide](./session-03/README.md).

## Session 4 status

Session 4 does **not** have runnable code yet. The `session-04/` folder contains an unsupported placeholder application — do not use it as reference code. A real implementation will replace it once the session goes live.

Session 3 is now an offline runnable package: a complete finance advisor (`session-03/code/`) carrying forward Session 2's file-access, approval, and memory boundaries plus the new skills, shell, CodeAct, and background-agent concepts, and a numbered `session-03/samples/` ladder (`10`/`11`, `20`/`21`, `30`/`31`, `40`/`41`). The samples intentionally make no model or network calls; a future update will add the live Microsoft Agent Framework Harness wiring once the corresponding preview APIs are pinned and validated.

## Important notes

### Mock data

All stock values are generated mock data for demonstration. This is not financial advice.

### Privacy

When running live, prompts and tool results are sent to the configured Foundry service. Hosted web search can also send queries and incur charges. Do not use personal, confidential, or real financial data. Raw Azure exceptions may contain tenant IDs and account names — do not share them.

## Keep learning

- **Official docs:** https://learn.microsoft.com/en-us/dotnet/
- **Agent Framework series:** https://aka.ms/mafclaw/blog
- **Microsoft Reactor:** https://developer.microsoft.com/reactor

## Support and feedback

- Open an issue: https://aka.ms/mafclaw/repo
- Troubleshooting: `session-01/docs/troubleshooting.md`

## License

This project is provided as-is under the MIT License. See LICENSE for details.
