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
| 1 | Thu Sep 3, 2026 | Meet Your Claw: A Harness in Three Lines of C# | Incremental checkpoints available; live validation environment-dependent | [Session guide](./session-01/README.md) &#124; [Blog](https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/) &#124; [Live event](https://aka.ms/mafclaw/1) |
| 2 | Thu Sep 10, 2026 | Working With Your Data, Safely: Files, Approvals and Memory | Planned | [Blog](https://devblogs.microsoft.com/agent-framework/agent-harness-working-with-your-data-safely/) &#124; [Event](https://aka.ms/mafclaw/2) &#124; Code snapshot coming |
| 3 | Thu Sep 17, 2026 | Scaling the Claw: Skills, Shell, CodeAct and Background Agents | Planned | [Blog](https://devblogs.microsoft.com/agent-framework/agent-harness-scaling-the-claw-or-harness-capabilities/) &#124; [Event](https://aka.ms/mafclaw/3) &#124; Code snapshot coming |
| 4 | Thu Sep 24, 2026 | Production Ready: Observability, Governance and Deployment | Planned | [Blog](https://devblogs.microsoft.com/agent-framework/agent-harness-making-your-claw-production-ready/) &#124; [Event](https://aka.ms/mafclaw/4) &#124; Code snapshot coming |

## Repository layout

- `general/docs/` - Prerequisites, configuration, and troubleshooting for all sessions.
- `general/code/` - Shared code assets and mock data patterns (reference only, not a dependency).
- `session-01/` - Session 1 landing page, incremental checkpoint guides, and the validated final compatibility sample.
- `session-02/` - Session 2 folder contains an unsupported .NET 9 placeholder application, not a finished sample. Real .NET 10 snapshot pending after Session 1 goes live.
- `session-03/` - Session 3 folder contains an unsupported .NET 9 placeholder application, not a finished sample. Real .NET 10 snapshot pending.
- `session-04/` - Session 4 folder contains an unsupported .NET 9 placeholder application, not a finished sample. Real .NET 10 snapshot pending.

## Getting started

### Prerequisites

- **.NET 10 SDK** - Download from https://dotnet.microsoft.com/download.
- **Azure CLI** - Download from https://learn.microsoft.com/cli/azure/install-azure-cli.
- **PowerShell 7+** - For configuration scripts.

### Setup

1. Clone this repository:
   ```bash
   git clone https://github.com/elbruno/mafclaw
   cd mafclaw
   ```

2. For live mode, configure your Foundry credentials (run from the repository root):
   ```powershell
   .\tools\configure-user-secrets.ps1 -Session 1
   ```

3. Navigate to the Session 1 code and restore packages:
   ```powershell
   cd .\session-01\code
   dotnet restore .\MafClaw.Session01.csproj --configfile .\NuGet.Config
   ```

4. Run the sample (the `--mode` argument is required):
   ```powershell
   # Offline mode — no Azure configuration required
   dotnet run --project .\MafClaw.Session01.csproj -- --mode offline

   # Live mode — requires Foundry configuration from step 2
   dotnet run --project .\MafClaw.Session01.csproj -- --mode live
   ```

For detailed setup guidance, see `general/docs/README.md`.

## Session 1 readiness

Session 1 implementation is complete with offline mode fully functional and tested. When you configure Azure Foundry credentials, it provides:

- Real Agent Framework harness implementation using official APIs.
- Live mode (uses Azure Foundry with user-provided credentials) and offline mode (deterministic mock data, no configuration required).
- A local `get_stock_price` tool plus structured planning and a Harness-configured
  `TodoProvider` context-provider instance.
- Full documentation, architecture diagrams, and troubleshooting.

Offline mode is fully functional for rehearsal and demonstration without any configuration. The `--mode` argument is required; the sample does not fall back automatically.

See `session-01/README.md` for the checkpoint path and `session-01/docs/setup.md` for setup instructions.

## Sessions 2-4 status

Sessions 2, 3, and 4 do not yet have real code snapshots. The `session-02`, `session-03`, and `session-04` folders currently contain unsupported .NET 9 placeholder applications — they are not finished samples and do not represent the planned .NET 10 implementations. Do not use them as reference code or attempt to run them as finished samples.

Real .NET 10 snapshots will replace each folder after the corresponding session goes live. Blog posts and live events will follow Session 1 completion. Availability will be announced on the series page.

## Important notes

### Mock data and disclaimer

This project uses generated mock financial data for demonstration purposes only. It is illustrative and not financial advice.

### Two modes

- **Live mode:** Connects to Azure Foundry for real Agent Framework execution. Requires configuration and Azure access. Cost applies.
- **Offline mode:** Runs the deterministic `OfflineClaw` simulation with local mock
  data. Requires `--mode offline` explicitly. It is not agent or model execution.
  Suitable for learning and rehearsal, with no service cost or configuration required.

Both modes are explicitly labeled when active.

### Data handling

When using live mode, review Microsoft's privacy statement and your Azure subscription's data-sharing settings.

## Keep learning

- **Official docs:** https://learn.microsoft.com/en-us/dotnet/
- **Agent Framework series:** https://aka.ms/mafclaw/blog
- **Microsoft Reactor:** https://developer.microsoft.com/reactor

## Support and feedback

- Open an issue: https://aka.ms/mafclaw/repo
- Questions: See `session-01/docs/troubleshooting.md` and the general guidance in `general/docs/README.md`.

## License

This project is provided as-is under the MIT License. See LICENSE for details.
