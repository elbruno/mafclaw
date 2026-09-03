# Session 1 — Finished Sample

This is the completed Session 1 code: a personal finance education assistant built with the Microsoft Agent Framework Harness. It is a copy of [Checkpoint 04](../checkpoints/04-planning-and-todos/README.md) with a distinct assembly name (`MafClaw.Session01`) to avoid build collisions.

## What it does

- Connects to a Foundry model via the Harness (`AsHarnessAgent`)
- Provides a `get_stock_price` tool with mock data (MSFT, NVDA, AMZN)
- Enables hosted web search (Harness default, service-dependent)
- Tracks work items via the Harness-provided `TodoProvider`
- Supports interactive multi-turn conversations

## How to run

From `session-01`:

```powershell
dotnet run --project .\code\MafClaw.Session01.csproj
```

### Prerequisites

1. .NET 10 SDK, Azure CLI, PowerShell 7+
2. Configure credentials (from repo root): `.\tools\configure-user-secrets.ps1 -Session 1`
3. Authenticate: `az login --output none`

### Commands

| Command | Action |
|---|---|
| `/todos` | Show tracked todo items |
| `/exit` | Quit the assistant |

## Configuration

Config keys read via `AddUserSecrets<Program>().AddEnvironmentVariables()`:

| Key | Required | Default |
|---|---|---|
| `Foundry:ProjectEndpoint` | Yes | — |
| `Foundry:Model` | No | `gpt-5-mini` |

Do not publish endpoints, tenant IDs, account names, or credentials.

## ⚠️ Privacy warning

This code has no error-handling wrapper. Raw Azure exceptions may contain tenant IDs, account names, and resource identifiers. **Do not screen-share the terminal when errors occur.**

## Learn the path

Walk through the four checkpoints to see how this code was built incrementally:

1. [01 — Hello, Agent](../checkpoints/01-hello-agent/README.md)
2. [02 — Meet the Harness](../checkpoints/02-harness-agent/README.md)
3. [03 — Tools and Search](../checkpoints/03-tools-and-search/README.md)
4. [04 — Planning and Todos](../checkpoints/04-planning-and-todos/README.md)

[Session 1 home](../README.md) · [Setup](../docs/setup.md) · [Troubleshooting](../docs/troubleshooting.md)

All stock values are mock educational data. This sample is not financial advice.
