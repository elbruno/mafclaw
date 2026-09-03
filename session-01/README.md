# Session 1 — From an agent to a Harness

Session 1 builds one personal-finance assistant four times so the value of the Microsoft Agent Framework Harness is visible rather than assumed.

The first checkpoint is a short prelude based on the plain Agent Framework `AIAgent` pattern in the [Microsoft Foundry hosted-agent sample](https://github.com/Azure-Samples/microsoft-foundry-hosted-agents/blob/main/01-MAF-Agent-CS/Program.cs). The remaining checkpoints follow the [official Session 1 article](https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/): make the chat-client boundary explicit, add the Harness and capabilities, then surface planning and todos.

> **Current compatibility sample:** The previously published, validated implementation remains in [`code`](./code/README.md). The four incremental checkpoints are now present; live output still depends on your configured Foundry service and model.

## One scenario, four checkpoints

**Scenario:** Help a learner compare a small watchlist, inspect illustrative prices, gather recent public market context when supported, and plan the next research steps.

What stays the same:

- the personal-finance assistant purpose and concise educational tone;
- the same Foundry provider, Responses API, deployment/model, instructions,
  beginner-stock prompt, and intended educational behavior for checkpoints 01 and 02;
- no trades and no personalized financial advice.

What does **not** stay the same is client construction. Checkpoint 01 uses the
`AIProjectClient.AsAIAgent(...)` convenience path backed by `FoundryChatClient`.
Checkpoint 02 explicitly obtains the Project OpenAI Responses client, adapts it
to `IChatClient`, and passes that client to `AsHarnessAgent(...)`.

Checkpoints 01 and 02 intentionally use a generic beginner-stock prompt. Specific
tickers and local mock quotes enter at checkpoint 03, when tools and search become
the teaching focus.

What changes:

- who constructs the agent;
- whether the `IChatClient` boundary is visible;
- whether common capabilities are assembled individually or by the Harness;
- whether the sample exposes tools, hosted search, todos, local modes, and approval.

Tool calling is not unique to the Harness. The Harness is valuable because it standardizes capability composition around a chat client while keeping those capabilities configurable.

## Checkpoint map

Run commands below from `session-01`.

| Checkpoint and exact path | Concept | Command | Prompt to try | Expected stable marker | Limitation | Previous / next |
|---|---|---|---|---|---|---|
| 01 — [`checkpoints/01-standard-agent`](./checkpoints/01-standard-agent/README.md) | Plain Agent Framework `AIAgent` prelude | `dotnet run --project .\checkpoints\01-standard-agent\MafClaw.Checkpoint01.csproj` | `What are three things a beginner should consider before buying a stock?` | `LIVE · checkpoint 01 · standard agent`, then one model response and exit code 0 | No Harness; no sample stock tool, hosted search, todos, or planning | Start / [02](./checkpoints/02-harness-core/README.md) |
| 02 — [`checkpoints/02-harness-core`](./checkpoints/02-harness-core/README.md) | Explicit Foundry Responses `IChatClient`, then minimal `AsHarnessAgent(...)` | `dotnet run --project .\checkpoints\02-harness-core\MafClaw.Checkpoint02.csproj` | `What are three things a beginner should consider before buying a stock?` | `LIVE · checkpoint 02 · harness core`, then one model response and exit code 0 | Harness extras are deliberately disabled to isolate the construction change | [01](./checkpoints/01-standard-agent/README.md) / [03](./checkpoints/03-tools-and-search/README.md) |
| 03 — [`checkpoints/03-tools-and-search`](./checkpoints/03-tools-and-search/README.md) | `get_stock_price` plus hosted search | `dotnet run --project .\checkpoints\03-tools-and-search\MafClaw.Checkpoint03.csproj` | The built-in prompt requests the illustrative MSFT price and recent NVDA news | `LIVE · checkpoint 03 · tools and search`, then `[Hosted web search was used.]` or the explicit `not used` marker | The used marker verifies search tool content, not citation annotations; inspect citations separately when returned | [02](./checkpoints/02-harness-core/README.md) / [04](./checkpoints/04-planning-and-todos/README.md) |
| 04 — [`checkpoints/04-planning-and-todos`](./checkpoints/04-planning-and-todos/README.md) | Harness-configured `TodoProvider` plus sample-owned planning, modes, and approval | `dotnet run --project .\checkpoints\04-planning-and-todos\MafClaw.Checkpoint04.csproj` | `Plan how to review MSFT and NVDA, including prices, recent context, and risks.` | `LIVE · mafclaw · checkpoint 04 · planning and todos`, the full `Mode starts in plan...` command line, then clarification or approval output | Approval gates generated-plan execution; `/mode execute` opts into direct execution and remains sticky until changed | [03](./checkpoints/03-tools-and-search/README.md) / [final](./code/README.md) |

For deterministic rehearsal today, use the final compatibility sample:

```powershell
cd .\code
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline --scenario stock
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline --scenario plan
```

Stable offline markers include `OFFLINE FALLBACK`, `SCENARIO stock`, `SCENARIO plan`, and `Execution blocked until explicit approval.` Offline output is not model, Harness, hosted-search, or service execution.

## Quick start

1. Install the .NET 10 SDK, PowerShell 7+, and Azure CLI.
2. From the repository root, configure live mode:

   ```powershell
   .\tools\configure-user-secrets.ps1 -Session 1
   ```

   The script is the primary setup path and stores `Foundry:ProjectEndpoint` and `Foundry:Model` in local user-secrets without printing configured values.

3. Authenticate through your approved Azure CLI flow:

   ```powershell
   az login --output none
   ```

4. Start at [checkpoint 01](./checkpoints/01-standard-agent/README.md), or run the [final compatibility implementation](./code/README.md).

Do not publish project endpoints, account details, tenant identifiers, resource names, credentials, or shared-terminal secret listings.

## Live and offline mean different things

- **Live:** prints an explicit `LIVE` label and runs an actual Agent Framework
  agent against the configured Foundry service. Harness composition configures
  and provides a `TodoProvider` instance by default; `TodoProvider` is a reusable
  Agent Framework context provider.
- **Offline:** runs the separate deterministic `OfflineClaw` path with local fixtures. It never contacts Azure or Foundry and does not execute a model, Harness, or hosted search.

The final sample requires an explicit `--mode live` or `--mode offline`; it never silently falls back.

## Current final implementation

The final sample in [`code`](./code/README.md):

- explicitly constructs an `IChatClient`;
- calls `AsHarnessAgent(...)`;
- registers `get_stock_price` using mock local values;
- leaves hosted search enabled, subject to service support;
- retrieves the `TodoProvider` instance configured and provided by the Harness;
- disables Harness `AgentModeProvider` and file memory;
- delegates local mode state, structured planning, and approval to `ClawConsole`;
- offers a clearly labeled, separate offline fallback.

Memory, file-backed watchlists, and session export/import appear in the official article, but are supplemental here. There is no runnable checkpoint 05 until implementation and validation are complete.

## Guides

- [Setup](./docs/setup.md)
- [Ownership comparison](./docs/comparison.md)
- [Offline fallback](./docs/offline.md)
- [Troubleshooting](./docs/troubleshooting.md)
- [Documentation index](./docs/README.md)

## Navigation

[Series home](../README.md) · [Checkpoint 01](./checkpoints/01-standard-agent/README.md) · [Final compatibility sample](./code/README.md) · [Session 1 event/recording](https://aka.ms/mafclaw/1)

All prices and portfolio examples are mock educational data. Nothing in this sample is financial advice.
