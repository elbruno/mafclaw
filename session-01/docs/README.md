# Session 01: Meet Your Claw

This session introduces the Agent Framework harness by building a minimal personal finance CLI assistant. You'll see the real harness host tools, hosted search, todos, and related facilities, while a small console layer adds local planning and an explicit approval gate.

## Session goals

By the end of this session, you'll understand:

- What a harness is and how it differs from a raw model call.
- How `AsHarnessAgent()` creates the real Agent Framework harness, registers `get_stock_price`, enables hosted search integration, and supplies a harness-managed `TodoProvider`.
- How `ClawConsole` owns local plan/execute mode state and requests structured planning responses.
- How `ClawConsole` requires an explicit yes/no approval before an approved plan executes.
- The difference between live mode (using real Agent Framework APIs) and offline mode (deterministic mock behavior).

## Authoritative reference

See the official blog post for detailed explanation and context:

https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/

## Architecture

Session 01 demonstrates a minimal harness stack with deliberately separated responsibilities:

```
User prompt
    |
    v
Harness agent (AsHarnessAgent)
    |
    +--- Registered tool: get_stock_price
    +--- Hosted search integration
    +--- Harness-managed TodoProvider and related harness facilities
    |
    v
ClawConsole
    |
    +--- Local plan/execute mode state
    +--- Structured PlanningResponse generation
    +--- Explicit Approve plan? (y/n) gate
    +--- /mode, /todos, and /exit commands
    |
    v
Harness execution and response returned to console
```

In live mode, the harness uses real Agent Framework APIs backed by Foundry. In offline mode, a separate `OfflineClaw` branch handles requests deterministically, reusing selected domain logic but never contacting any service.

Implementation note: the harness `AgentModeProvider` is explicitly disabled; `ClawConsole` is the sole owner of this sample's plan/execute state and transitions.

## Prerequisites

Before running this sample:

1. Complete the **Setup and prerequisites** guide in `general/docs/README.md`.
2. Ensure `.NET 10` is installed: `dotnet --version`.
3. For live mode, configure your Foundry credentials from the repository root:
   ```powershell
   .\tools\configure-user-secrets.ps1 -Session 1
   ```

No configuration is required for offline mode.

## Live mode setup

To run in live mode, you need:

- Azure Foundry project and model deployment.
- Foundry project endpoint (configuration, not a secret).
- Foundry model name or deployment identifier.

Run the configuration script from the **repository root** to store these values in your local user-secrets store:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1
```

You'll be prompted interactively for:

- Foundry project endpoint
- Model deployment name

The project's `UserSecretsId` (`8f001de5-00b8-4cd2-835b-e0ea21979f0f`) is committed — the script never mutates the `.csproj` file and `dotnet user-secrets init` is never needed.

Verify that the required keys exist without displaying their stored values:

```powershell
$project = ".\session-01\code\MafClaw.Session01.csproj"
$secretLines = @(dotnet user-secrets list --project $project 2>&1)
try {
    if ($LASTEXITCODE -ne 0) { throw "Unable to inspect Session 1 configuration." }
    foreach ($key in @("Foundry:ProjectEndpoint", "Foundry:Model")) {
        if ($secretLines -match "^$([regex]::Escape($key))\s*=\s*.+$") {
            Write-Host "${key}: configured"
        } else {
            throw "${key}: missing"
        }
    }
} finally {
    Remove-Variable secretLines -ErrorAction SilentlyContinue
}
```

**Alternative — environment variables** (useful for CI or when you prefer not to store values in user-secrets):

```powershell
$env:FOUNDRY_PROJECT_ENDPOINT = "https://your-project.services.ai.azure.com/api/projects/your-project"
$env:FOUNDRY_MODEL = "<your-deployment-name>"
```

## Offline mode setup

No setup required. Run with `--mode offline` to use deterministic mock data without contacting Azure.

The `--mode` argument is mandatory. Running without it exits with code 1. There is no automatic fallback to offline mode.

## Run the sample

From the repository root:

```powershell
cd .\session-01\code

# Live mode (requires Foundry configuration)
dotnet run --project .\MafClaw.Session01.csproj -- --mode live

# Offline mode with interactive commands
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline

# Offline mode with deterministic scenario
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline --scenario stock
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline --scenario plan
```

**Important:** The `--mode` argument is mandatory. Running without it exits with code 1.

### Expected output - Live mode

When you run `dotnet run -- --mode live`:

1. The harness loads Foundry configuration from user secrets.
2. It creates a real `AIProjectClient` and `IChatClient`.
3. It enters an interactive loop where you can type prompts.
4. Prompt: `claw> ` waits for your input.
5. In execute mode, the harness contacts Foundry, invokes available tools, and returns results.
6. In plan mode, `ClawConsole` requests a structured `PlanningResponse` and enforces the explicit approval gate before execution.
7. Output varies based on model inference, service support, and tool execution.
8. Supported commands: `/mode [plan|execute]`, `/todos`, `/exit`

Exit codes:
- 0 = success
- 3 = credential unavailable (run `az login` and retry)

### Expected output - Offline mode

When you run `dotnet run -- --mode offline`:

1. The harness prints a prominent `OFFLINE FALLBACK` banner.
2. It loads deterministic mock stock quotes from `mock-market-data.json`.
3. It enters an interactive loop with prompt: `offline> `
4. Supported commands: `/stock <SYMBOL>`, `/plan`, `/exit`
5. Same commands produce identical output every run (deterministic).

When you run `dotnet run -- --mode offline --scenario stock`:

1. Prints `OFFLINE FALLBACK` banner.
2. Looks up MSFT and NVDA prices from mock data.
3. Prints prices and exits (no interactive loop).
4. Output: `SCENARIO stock` followed by deterministic price lines.

When you run `dotnet run -- --mode offline --scenario plan`:

1. Prints `OFFLINE FALLBACK` banner.
2. Shows a plan with approval gate.
3. Plan cannot execute without explicit approval (always blocks in this scenario).
4. Prints: `Approval granted: no`, `Execution triggered: no`, `Execution blocked until explicit approval.`
5. Exits (no interactive loop).

## Prompts to try

In live mode (`--mode live`), after connecting to Foundry, type prompts at the `claw>` prompt:

```
claw> Plan my next moves for MSFT and NVDA, and look up market mood.
claw> What should I do with my Nvidia and Microsoft positions?
claw> Check the latest prices for MSFT, NVDA, and AMZN.
claw> Compare AMZN and NVDA and tell me which looks more interesting.
claw> Plan my portfolio rebalance based on current prices.
```

> **Note:** `get_stock_price` returns values from local mock data. Supported symbols are **MSFT**, **NVDA**, and **AMZN**. Prompts requesting other symbols will produce a tool error.

In offline mode (`--mode offline`), use interactive commands:

```
offline> /stock MSFT
offline> /stock NVDA
offline> /plan
offline> /exit
```

The sample will:

1. In live mode: Use the real harness for Foundry requests, tools, hosted search, and todos; use `ClawConsole` for local planning, approval, and mode transitions.
2. In offline mode: Use mock data, return deterministic responses, block execution without approval.

## Supported slash commands

### Live mode (`--mode live`)

When connected to Foundry, the interactive console supports:

- `/mode plan` - Switch the console's local state to plan mode (the default), where `ClawConsole` requests a structured plan before action.
- `/mode execute` - Switch the console's local state to direct execute mode for subsequent prompts.
- `/todos` - Display the current list from the harness-managed `TodoProvider`.
- `/exit` - Exit the live session.

### Offline mode (`--mode offline`)

When running offline with interactive loop, the console supports:

- `/stock <SYMBOL>` - Look up a stock price from mock data (e.g., `/stock MSFT`).
- `/plan` - Run the plan scenario with mock data.
- `/exit` - Exit the offline session.

## Limitations

This sample is intentionally minimal:

- **Tool results are mocked.** The `get_stock_price` tool returns values from local mock JSON. There is no `web_search` custom tool in this session; hosted web search is a Foundry Responses capability (on by default, subject to service support) rather than a registered custom tool.
- **No actual trades execute.** The approval gate prevents any real financial actions.
- **Planning is sample-owned.** `ClawConsole` implements this session's structured planning, yes/no approval, and local mode transitions; the harness `AgentModeProvider` is disabled.
- **Harness behavior is simplified.** Full Agent Framework capabilities (like memory and plugins) appear in later sessions.
- **Performance.** Mock data lookups are synchronous and local.

## Mock data and disclaimer

All data in this sample is generated for demonstration purposes only:

- Stock quotes are not real market data.
- Prices do not reflect actual trading.
- This sample is not financial advice.

Use this sample only to understand how the harness framework works, not to make real trading decisions.

## Cost and data-sharing

### Live mode

When running in live mode:

- Your prompts and tool results are sent to Azure Foundry.
- Model inference happens in your Azure subscription.
- Consider data residency, compliance, and privacy implications for your organization.
- Keep personal or sensitive information out of prompts.

Review your Azure subscription's data-sharing and privacy settings before using live mode with real queries.

### Offline mode

Offline mode uses only local data and incurs no Azure costs.

## Troubleshooting

## Troubleshooting

### "Missing value for --mode." (Exit code 1)

The sample requires an explicit mode. Run with:

```powershell
dotnet run --project .\MafClaw.Session01.csproj -- --mode live
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline --scenario stock
```

### "Configuration not found" (Exit code 2)

The `Foundry:ProjectEndpoint` or `Foundry:Model` setting is missing. To use live mode, run the setup script from the repository root:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1
```

Or set environment variables:

```powershell
$env:FOUNDRY_PROJECT_ENDPOINT = "https://your-project.services.ai.azure.com/api/projects/your-project"
$env:FOUNDRY_MODEL = "<your-deployment-name>"
```

If you don't have Foundry access, use offline mode instead:

```powershell
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline
```

### "Live mode credential unavailable: Azure.Identity.CredentialUnavailableException" (Exit code 3)

This means `AzureCliCredential` could not find valid cached credentials from the Azure CLI. Authenticate through your organization's approved Azure sign-in flow without printing account details:

```bash
az login --output none
```

Ensure you select the tenant that contains the Foundry project, then let the sample's safe error output verify access.

Then try the live mode command again:

```powershell
dotnet run --project .\MafClaw.Session01.csproj -- --mode live
```

### "Live mode authentication failed" (Exit code 3)

Your Azure CLI credentials have expired or are no longer valid for your target tenant/subscription. Refresh them without printing account details:

```bash
az login --use-device-code --output none
```

Follow the on-screen instructions and select the tenant that contains the Foundry project.

Then retry the live mode command.

### "Live mode request failed" (Exit code 4)

The harness contacted Foundry but received an error response. This might indicate:

1. Invalid project endpoint in configuration.
2. Model deployment name is incorrect.
3. Foundry service is temporarily unavailable.

Reconfigure safely if needed (from the repository root); the script reports key names and status without echoing values:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1
```

### "Live mode network failure" (Exit code 5)

The harness could not reach Foundry due to a network error. This might indicate:

1. Network connectivity issue.
2. Firewall or proxy blocking the request.
3. DNS resolution failure.

Verify network connectivity:

```bash
ping -c 4 8.8.8.8
```

If the network is working, the Foundry endpoint may be temporarily unavailable. Try again in a moment.

### "Invalid JSON in 'appsettings.json'" (Exit code 2)

The `appsettings.json` file exists but contains invalid JSON. Check the file for syntax errors and fix them, or delete it to use user-secrets instead.

### Stock symbol not found

When running offline with `/stock <SYMBOL>`, the tool looks up symbols in `mock-market-data.json`. The mock data currently includes:

- MSFT (Microsoft): 512.34 USD
- NVDA (Nvidia): 184.72 USD
- AMZN (Amazon): 241.18 USD

Try `/stock MSFT`, `/stock NVDA`, or `/stock AMZN`. To add more symbols, edit `mock-market-data.json` and rebuild from the `session-01\code` folder:

```powershell
dotnet build .\MafClaw.Session01.csproj
```

### Build fails: "TargetFramework not supported"

Ensure .NET 10 is installed:

```powershell
dotnet --version
```

Must report 10.0.0 or later. If not, download and install from https://dotnet.microsoft.com/download.

## Reset and start fresh

To reset the sample:

1. **Clear offline state:**
   ```powershell
   Remove-Item .\session-01-cache.* -ErrorAction SilentlyContinue
   ```

2. **Clear user-secrets** (run from the repository root):
   ```powershell
   .\tools\configure-user-secrets.ps1 -Session 1 -Clear
   ```

3. **Rebuild:**
   ```powershell
   dotnet clean .\MafClaw.Session01.csproj
   dotnet restore .\MafClaw.Session01.csproj --configfile .\NuGet.Config
   dotnet build .\MafClaw.Session01.csproj --no-restore
   ```

Then run again.

## Next steps

- Explore different prompts and observe plan changes.
- Review the code in `Program.cs` to understand the harness composition.
- Compare `OfflineClaw.cs` to `FinanceAgentFactory.cs` to see how deterministic fallback works.
- Read the next session docs to explore safe data handling and approval workflows.

## Recording status

This session is part of a live coding series on Microsoft Reactor. A recording will be available after the live event at:

https://aka.ms/mafclaw/1

## Support

For questions or issues:

1. Check the **Troubleshooting** section above.
2. Review the general **Setup and prerequisites** guide in `general/docs/README.md`.
3. Read the official Agent Framework blog series: https://aka.ms/mafclaw/blog.
4. Open an issue on GitHub: https://aka.ms/mafclaw/repo.
