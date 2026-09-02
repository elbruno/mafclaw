# Session 01 implementation

Real Microsoft Agent Framework sample for the official Session 1 post:  
https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/

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

Configuration is resolved in this precedence order:

1. `appsettings.json` in the session code folder (gitignored, optional).
2. .NET user-secrets — active in Development and all non-Production environments. `UserSecretsId` `8f001de5-00b8-4cd2-835b-e0ea21979f0f` is committed; `dotnet user-secrets init` is never needed.
3. Environment variables: `Foundry__ProjectEndpoint` / `Foundry__Model` (canonical) or `FOUNDRY_PROJECT_ENDPOINT` / `FOUNDRY_MODEL` (aliases).

Canonical keys (satisfied by sources above):

- `Foundry:ProjectEndpoint`
- `Foundry:Model`

Authentication uses `AzureCliCredential` via `az login` locally.  
No API-key setting is supported or required.
Hosted web search is enabled by the Harness for current market context.

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

## Run live

Use the setup script (recommended) or set values directly:

```powershell
# Recommended: use the configure script from the repo root
# (session-01\code is two levels deep; ..\..\tools reaches the repo root)
..\..\tools\configure-user-secrets.ps1 -Session 1

# Direct: set individual keys (run from session-01\code)
dotnet user-secrets set "Foundry:ProjectEndpoint" "https://<your-project>.services.ai.azure.com/api/projects/<your-project>" --project .\MafClaw.Session01.csproj
dotnet user-secrets set "Foundry:Model" "gpt-5.4" --project .\MafClaw.Session01.csproj

dotnet run --project .\MafClaw.Session01.csproj -- --mode live
```

Live console commands:

- `/mode plan|execute`
- `/todos`
- `/exit`

Approval is required only in plan mode before switching to execute. Use `/mode execute` to bypass planning and execute directly.
The console owns the plan/execute state; the Harness `AgentModeProvider` is disabled because its
mode-transition notification can be classified as a jailbreak prompt by some Foundry safety policies.
