# Session 03 setup

The complete Session 03 advisor and all plain-concept samples run offline.
Sample `11-skills-agent` is a live Microsoft Agent Framework example and
requires an Azure AI Foundry project and model.

## Prerequisites

- .NET 10 SDK
- PowerShell 7+
- Azure CLI and an Azure AI Foundry project with a deployed model (Sample 11 only)

Verify the SDK:

```powershell
dotnet --version
```

## Build everything

From `public-staging\session-03`:

```powershell
dotnet build .\code\MafClaw.Session03.csproj
dotnet build .\samples\10-skills\MafClaw.Sample10.csproj
dotnet build .\samples\11-skills-agent\MafClaw.Sample11.csproj
dotnet build .\samples\20-confined-shell\MafClaw.Sample20.csproj
dotnet build .\samples\21-confined-shell-agent\MafClaw.Sample21.csproj
dotnet build .\samples\30-codeact-calculation\MafClaw.Sample30.csproj
dotnet build .\samples\31-codeact-agent\MafClaw.Sample31.csproj
dotnet build .\samples\40-background-queue\MafClaw.Sample40.csproj
dotnet build .\samples\41-background-agents\MafClaw.Sample41.csproj
```

## Run the offline samples

No secrets, endpoints, model deployments, or network access are needed for the
complete advisor or Samples `10`, `20`, `21`, `30`, `31`, `40`, and `41`.

## Run the live Skills bridge

From the repository root, authenticate and configure only the project that
needs credentials:

```powershell
az login --output none
.\tools\configure-user-secrets.ps1 -Session 3
dotnet run --project .\session-03\samples\11-skills-agent\MafClaw.Sample11.csproj
```

Ask `Value 25 shares of MSFT using the mock data.` The agent should advertise
the file-based skill, load its `SKILL.md` instructions and bundled price
reference on demand, then return a mock educational result.
