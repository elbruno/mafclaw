# Session 01 final compatibility implementation

Real Microsoft Agent Framework sample for the official Session 1 post:  
https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/

This folder preserves the previously published, validated final sample while the
incremental teaching snapshots live under [`../checkpoints`](../README.md#checkpoint-map).

[Session 1 landing page](../README.md) · [Checkpoint 04](../checkpoints/04-planning-and-todos/README.md) · [Setup](../docs/setup.md)

## Target

- .NET SDK: **10.x**
- Project framework: **net10.0**

## Locked package graph (validated by .NET 10 API spike)

- `Azure.AI.Projects` **2.1.0-beta.4** _(required by the Foundry 1.20 preview adapter)_
- `Azure.Identity` **1.21.0**
- `Microsoft.Agents.AI` **1.20.0**
- `Microsoft.Agents.AI.Harness` **1.20.0**
- `Microsoft.Agents.AI.Foundry` **1.20.0-preview.260831.1** _(required preview adapter for the 1.20 family)_

## Configuration contract

Configuration is resolved in this effective high-to-low precedence order:

1. .NET user-secrets — active in Development and all non-Production environments. `UserSecretsId` `8f001de5-00b8-4cd2-835b-e0ea21979f0f` is committed; `dotnet user-secrets init` is never needed.
2. `appsettings.json` in the process working directory.
3. `appsettings.json` copied beside the built application.
4. Canonical environment variables: `Foundry__ProjectEndpoint` / `Foundry__Model`.
5. Alias environment variables: `FOUNDRY_PROJECT_ENDPOINT` / `FOUNDRY_MODEL`.

Canonical keys (satisfied by sources above):

- `Foundry:ProjectEndpoint`
- `Foundry:Model`

Authentication uses `AzureCliCredential` via `az login` locally.  
No API-key setting is supported or required.
Hosted web search is enabled by the Harness for current market context, but it
runs only when the configured service/model supports it.

## Build and test

From `session-01\code`:

```powershell
dotnet restore .\MafClaw.Session01.csproj --configfile .\NuGet.Config
dotnet restore .\tests\MafClaw.Session01.Tests\MafClaw.Session01.Tests.csproj --configfile .\NuGet.Config
dotnet build .\tests\MafClaw.Session01.Tests\MafClaw.Session01.Tests.csproj -c Release --no-restore
dotnet test .\tests\MafClaw.Session01.Tests\MafClaw.Session01.Tests.csproj -c Release --no-build
```

The test project references the app project; building it builds both.

## Run offline

Interactive:

```powershell
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline
```

Deterministic smoke:

```powershell
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline --scenario stock
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline --scenario plan
```

Offline mode prints `OFFLINE FALLBACK` and does not use Azure, network, or Foundry config.
It is a separate deterministic `OfflineClaw` path—not model, Agent Framework
agent, Harness, tool-calling, or hosted-search execution.

## Run live

Use the repository setup script as the primary configuration path:

```powershell
# Run from the repository root
# (session-01\code is two levels deep; ..\..\tools reaches the repo root)
..\..\tools\configure-user-secrets.ps1 -Session 1

dotnet run --project .\MafClaw.Session01.csproj -- --mode live
```

Live startup prints `LIVE · mafclaw · Session 01`.

Live console commands:

- `/mode plan|execute`
- `/todos`
- `/exit`

Approval is required only in plan mode before switching to execute. Use `/mode execute` to bypass planning and execute directly.
Approval gates execution of the generated plan; it is not a universal execution
gate. `/mode execute` explicitly opts into direct execution, and the selected
mode remains sticky for the console session until changed.

Ownership in this final sample:

- the Harness composes the agent and hosted search, and configures/provides a
  `TodoProvider` instance by default; `TodoProvider` is a reusable Agent Framework
  context provider, not a custom tool;
- application code owns the local mock `get_stock_price` tool;
- `ClawConsole` owns plan/execute state, structured planning, and explicit approval;
- the Harness `AgentModeProvider` and file memory are disabled;
- memory and session resume are official-article supplemental material, not implemented here.

Tool calling is not unique to the Harness; the Harness standardizes how capabilities
are composed around the `IChatClient`.

All stock values are mock educational data. This sample is not financial advice.
