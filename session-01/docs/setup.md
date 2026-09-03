# Session 1 Setup

## Prerequisites

- .NET 10 SDK
- PowerShell 7 or later
- Azure CLI
- Access to a Microsoft Foundry project with a deployed model (e.g. `gpt-5-mini`)

Verify:

```powershell
dotnet --version
az --version
```

## 1. Authenticate

Use your organization's approved Azure sign-in flow:

```powershell
az login --output none
```

Do not paste account, tenant, subscription, or endpoint details into issues, screenshots, or shared logs.

## 2. Configure with user-secrets

From the repository root:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1
```

Preview without writing:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1 -WhatIf
```

The script stores two keys in the .NET user-secrets store:

| Key | Required | Default |
|---|---|---|
| `Foundry:ProjectEndpoint` | Yes | — |
| `Foundry:Model` | No | `gpt-5-mini` |

All checkpoint projects and the finished sample share one `UserSecretsId`. Configuring once configures all five projects. Do not run `dotnet user-secrets init`.

Environment variables `FOUNDRY_PROJECT_ENDPOINT` and `FOUNDRY_MODEL` also work as fallbacks.

To clear MafClaw Session 1 keys:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1 -Clear
```

## 3. Build and run

From `session-01`, run any checkpoint:

```powershell
dotnet run --project .\checkpoints\01-hello-agent\MafClaw.Checkpoint01.csproj
```

Or run the finished sample:

```powershell
dotnet run --project .\code\MafClaw.Session01.csproj
```

## Configuration details

Each `Program.cs` reads config with three inline lines:

```csharp
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>().AddEnvironmentVariables().Build();
var endpoint = config["Foundry:ProjectEndpoint"]!;
var model = config["Foundry:Model"] ?? "gpt-5-mini";
```

There is no `FoundryConfiguration` class, no `appsettings.json`, and no config-resolution hierarchy. User-secrets and environment variables are the only two sources.

## Live-data boundary

Live prompts and tool results are sent to the configured Foundry service. Hosted web search (enabled by default through the Harness) can also send query content and incur charges. Keep personal, confidential, and real financial data out of demo prompts.

All repository stock quotes are mock values. This sample is educational and is not financial advice.

[Back to Session 1](../README.md) | [Troubleshooting](./troubleshooting.md)
