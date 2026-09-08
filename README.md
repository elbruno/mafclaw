# From Model to Agent: The Agent Framework Harness, Live in C#

A 4-part Microsoft Reactor live coding series that builds a personal finance CLI assistant using C#, .NET, and the Microsoft Agent Framework.

## Series resources

- **Series page:** https://aka.ms/mafclaw
- **Blog introduction:** http://aka.ms/mafclaw/blog
- **Sample repository:** http://aka.ms/mafclaw/repo
- **Target runtime:** .NET 10

## Sessions

| Session | Date | Title | Status | Links |
|---|---|---|---|---|
| 1 | Thu Sep 3, 2026 | Meet Your Claw: A Harness in Three Lines of C# | ✅ Ready — 4 incremental checkpoints + finished sample | [Session guide](./session-01/README.md) &#124; [Blog](https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/) &#124; [Live event](https://aka.ms/mafclaw/1) |
| 2 | Thu Sep 10, 2026 | Working With Your Data, Safely: Files, Approvals and Memory | ✅ Ready — isolated safety samples + final Harness finance advisor app | [Session guide](./session-02/README.md) &#124; [Blog](https://devblogs.microsoft.com/agent-framework/agent-harness-working-with-your-data-safely/) &#124; [Event](https://aka.ms/mafclaw/2) |
| 3 | Thu Sep 17, 2026 | Scaling the Claw: Skills, Shell, CodeAct and Background Agents | 🔲 Not started | [Blog](https://devblogs.microsoft.com/agent-framework/agent-harness-scaling-the-claw-or-harness-capabilities/) &#124; [Event](https://aka.ms/mafclaw/3) |
| 4 | Thu Sep 24, 2026 | Production Ready: Observability, Governance and Deployment | 🔲 Not started | [Blog](https://devblogs.microsoft.com/agent-framework/agent-harness-making-your-claw-production-ready/) &#124; [Event](https://aka.ms/mafclaw/4) |

## Repository layout

- `session-01/` — Session 1: four incremental checkpoints (hello-agent → harness → tools → planning+todos) and the finished sample.
- `session-02/` — Session 2: isolated samples for safe file access, approvals, and memory, plus the final Agent Framework/Harness finance advisor walkthrough.
- `session-03/` through `session-04/` — Placeholder folders. Not started; real .NET 10 snapshots will replace them after each session goes live.
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

# 3. Run the isolated concept samples
dotnet run --project .\session-02\samples\01-safe-file-access\MafClaw.Sample01.csproj
dotnet run --project .\session-02\samples\02-approval-gate\MafClaw.Sample02.csproj
dotnet run --project .\session-02\samples\03-memory-store\MafClaw.Sample03.csproj

# 4. Run the full Session 2 finance advisor app
dotnet run --project .\session-02\code\MafClaw.Session02.csproj
```

For the full walkthrough, see the [Session 2 guide](./session-02/README.md).

## Sessions 3–4 status

Sessions 3 and 4 do **not** have runnable code yet. The `session-03/` and `session-04/` folders contain unsupported placeholder applications — do not use them as reference code. Real implementations will replace each folder after the corresponding session goes live.

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
