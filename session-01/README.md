# Session 1 — From an Agent to a Harness

A visual, guided live-coding session that builds one personal-finance assistant incrementally — from a hello-world agent to an interactive assistant with tools, web search, and todos. Each checkpoint adds **one concept** and stays short enough to walk through on screen.

Based on the [official Session 1 article](https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/).

## Checkpoints

Run all commands from `session-01`.

| # | Folder | Concept | Command |
|---|---|---|---|
| 01 | [`01-hello-agent`](./checkpoints/01-hello-agent/README.md) | Minimum code that talks to a Foundry model — **this is already an agent** | `dotnet run --project .\checkpoints\01-hello-agent\MafClaw.Checkpoint01.csproj` |
| 02 | [`02-harness-agent`](./checkpoints/02-harness-agent/README.md) | Same behavior, now through `AsHarnessAgent` — the 3-line diff IS the lesson | `dotnet run --project .\checkpoints\02-harness-agent\MafClaw.Checkpoint02.csproj` |
| 03 | [`03-tools-and-search`](./checkpoints/03-tools-and-search/README.md) | Add a `get_stock_price` tool with inline mock data; hosted web search is on by default | `dotnet run --project .\checkpoints\03-tools-and-search\MafClaw.Checkpoint03.csproj` |
| 04 | [`04-planning-and-todos`](./checkpoints/04-planning-and-todos/README.md) | Interactive REPL with multi-turn conversations, `TodoProvider`, and `/todos` | `dotnet run --project .\checkpoints\04-planning-and-todos\MafClaw.Checkpoint04.csproj` |

The finished code lives in [`code/`](./code/README.md) — a copy of Checkpoint 04 with a distinct assembly name.

## Quick start

1. Install .NET 10 SDK, PowerShell 7+, and Azure CLI.
2. Configure credentials (from repository root):

   ```powershell
   .\tools\configure-user-secrets.ps1 -Session 1
   ```

3. Authenticate:

   ```powershell
   az login --output none
   ```

4. Start at [Checkpoint 01](./checkpoints/01-hello-agent/README.md).

## Configuration

All checkpoints share one `UserSecretsId`. The setup script stores two keys:

| Key | Required | Default |
|---|---|---|
| `Foundry:ProjectEndpoint` | Yes | — |
| `Foundry:Model` | No | `gpt-5-mini` |

Config is read inline in each `Program.cs` — three lines, no config class:

```csharp
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>().AddEnvironmentVariables().Build();
```

## ⚠️ Privacy warning

These samples have **no error-handling wrapper**. Raw Azure exceptions can contain tenant IDs, account names, and resource identifiers. **Do not screen-share the terminal when errors occur.** See [Troubleshooting](./docs/troubleshooting.md) for common errors and fixes.

Do not publish endpoints, account details, tenant identifiers, resource names, or credentials.

## Guides

- [Setup](./docs/setup.md) — prerequisites, authentication, and configuration
- [Troubleshooting](./docs/troubleshooting.md) — common errors and fixes
- [Documentation index](./docs/README.md)

## Navigation

[Series home](../README.md) · [Checkpoint 01](./checkpoints/01-hello-agent/README.md) · [Finished sample](./code/README.md) · [Session 1 event/recording](https://aka.ms/mafclaw/1)

All prices and portfolio examples are mock educational data. Nothing in this sample is financial advice.
