# Session 1 setup

These instructions assume the published repository layout, with `session-01` and `tools` directly under the repository root.

## Prerequisites

- .NET 10 SDK
- PowerShell 7 or later
- Azure CLI
- For live mode only: access to a Microsoft Foundry project and a compatible deployed model

Verify the local tools:

```powershell
dotnet --version
az --version
```

## 1. Authenticate for live mode

Use your organization's approved Azure sign-in flow:

```powershell
az login --output none
```

Do not paste account, tenant, subscription, project, or endpoint details into issues, screenshots, or shared logs.

Offline mode does not require Azure authentication.

## 2. Configure with user-secrets

The supported setup entry point is the repository script:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1
```

Preview its actions without reading or writing configuration:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1 -WhatIf
```

The script stores these canonical keys in your local .NET user-secrets store:

- `Foundry:ProjectEndpoint`
- `Foundry:Model`

The final compatibility project and all four checkpoint projects share one committed
Session 1 `UserSecretsId`. Configuring Session 1 once through this script configures
all five projects. Do not run `dotnet user-secrets init`.

The script accepts `FOUNDRY_PROJECT_ENDPOINT` and `FOUNDRY_MODEL` as automation inputs. Explicit `-FoundryProjectEndpoint` and `-FoundryModel` parameters are also supported, but values supplied on a command line can remain in shell history.

To clear only MafClaw-owned Session 1 keys:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1 -Clear
```

## 3. Restore and build

The existing compatibility/final implementation remains runnable:

```powershell
cd .\session-01\code
dotnet restore .\MafClaw.Session01.csproj --configfile .\NuGet.Config
dotnet build .\MafClaw.Session01.csproj -c Release --no-restore
```

The four checkpoint projects share this local configuration. Run each exact command from the [Session 1 checkpoint map](../README.md#checkpoint-map).

Checkpoint projects 03 and 04 link the canonical fixture from
`session-01\shared\mock-market-data.json` and copy it to their build output. The
final compatibility project preserves its existing local fixture for compatibility.

## 4. Run

Live mode:

```powershell
dotnet run --project .\MafClaw.Session01.csproj -- --mode live
```

Offline deterministic smoke:

```powershell
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline --scenario stock
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline --scenario plan
```

The `--mode` argument is required. The application never silently changes a failed live run into offline output.

## Configuration resolution

The final implementation resolves the canonical keys in this effective
high-to-low precedence order:

1. .NET user-secrets in Development and other non-Production environments;
2. `appsettings.json` in the process working directory;
3. `appsettings.json` copied beside the built application;
4. canonical environment variables `Foundry__ProjectEndpoint` and `Foundry__Model`;
5. aliases `FOUNDRY_PROJECT_ENDPOINT` and `FOUNDRY_MODEL`.

User-secrets through the repository script are the primary local setup path. Do not commit `appsettings.json`, credentials, endpoints, or local state.

## Live-data boundary

Live prompts and tool results can be sent to the configured Foundry service.
Hosted-search query content can also be processed by the hosted-search
capability, and both model and search usage can incur charges. Hosted search is
available only when the service/model supports it. Keep personal, confidential,
and real financial information out of demo prompts.

All repository stock quotes are mock values. This sample is educational and is not financial advice.

[Back to Session 1](../README.md) | [Troubleshooting](./troubleshooting.md)
