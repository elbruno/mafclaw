# Session 1 troubleshooting

Commands below assume you are in `session-01\code` unless stated otherwise.

## The app prints usage and exits with code 1

`--mode` is mandatory:

```powershell
dotnet run --project .\MafClaw.Session01.csproj -- --mode live
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline
```

`--scenario stock|plan` is valid only with `--mode offline`.

## Live configuration exits with code 2

One of the canonical keys is missing or invalid:

- `Foundry:ProjectEndpoint`
- `Foundry:Model`

From the repository root, rerun:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1
```

The endpoint must be an absolute HTTPS URI. Avoid printing its value in shared terminals or logs.

## Authentication exits with code 3

The sample uses `AzureCliCredential`. Refresh the Azure CLI session through your approved sign-in flow:

```powershell
az login --output none
```

Then retry live mode. Do not paste account or tenant output into an issue.

## A live request exits with code 4

The service rejected the request. Common causes include:

- the configured project endpoint is incorrect;
- the configured model/deployment name is unavailable;
- your identity lacks access;
- the requested capability is unsupported by the service;
- the service is temporarily unavailable.

Reconfigure through the user-secrets script and confirm access in the Foundry portal without sharing identifiers.

## A live request exits with code 5

The client could not reach the service. Check your connection, proxy, firewall, and organization network policy, then retry. Use offline mode if you need a deterministic rehearsal while connectivity is unavailable.

## Startup exits with code 6

The local mock market-data fixture is missing, inaccessible, or malformed.
Restore `mock-market-data.json` from the repository and rebuild. The console
intentionally suppresses filesystem paths, parser details, and stack traces.

## Stock lookup fails

The local `get_stock_price` tool and offline command use mock JSON fixtures.
Checkpoint projects link the canonical fixture from
`session-01\shared\mock-market-data.json`; the final compatibility project keeps
its existing local copy. Supported symbols are:

- MSFT — 512.34 USD (mock)
- NVDA — 184.72 USD (mock)
- AMZN — 241.18 USD (mock)

These are illustrative fixtures, not current quotes and not financial advice.

## Hosted search is absent

Hosted search is service-dependent. The Harness enables it in the current live configuration, but the configured Foundry service/model must support it. A normal response without the stable marker

```text
[Hosted web search was used.]
```

does not prove that search ran.

Try a clearly time-sensitive prompt such as:

```text
Find recent public market context for NVDA and cite the sources.
```

The used marker verifies hosted-search tool content, but does not verify citation
annotations. Inspect citations separately when returned. Model output and
citations vary. Never claim hosted search from offline output.

Hosted search can incur charges and can send query context to the configured
service capability. Review your organization's cost, data-sharing, residency,
and logging policies.

## `/todos` says `No todos yet.`

The Harness configures and provides the `TodoProvider` instance by default.
`TodoProvider` is a reusable Agent Framework context provider, not a custom tool.
A todo list appears only after the model/provider creates items. Use plan mode
with a multi-step request, approve the plan if appropriate, then run `/todos`.

## Plan mode does not behave like the official console

That difference is intentional in the current final sample:

- Harness `AgentModeProvider` is disabled;
- `ClawConsole` owns local `plan`/`execute` state;
- `PlanningResponse` and the console own clarification and approval;
- approval here is console approval, not Harness tool approval.
- approval gates execution of the generated plan only;
- `/mode execute` explicitly opts into direct execution without that planning turn;
- mode remains sticky for the console session until changed.

## A content filter or safety policy refuses the request

Respect the refusal. Do not repeatedly rephrase the prompt, weaken safeguards,
or use evasive wording to bypass the policy. Remove unnecessary sensitive
content and choose a clearly benign educational request. If the request still
is not allowed, stop rather than retrying evasively.

## Memory or session commands are unavailable

The current final sample disables file memory and does not implement `/session-export` or `/session-import`. Memory/session resume appears in the [official article](https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/) as supplemental material, not as a runnable Session 1 checkpoint in this repository.

## Build or restore fails

Confirm .NET 10 is installed:

```powershell
dotnet --list-sdks
```

Restore with the repository configuration:

```powershell
dotnet restore .\MafClaw.Session01.csproj --configfile .\NuGet.Config
dotnet build .\MafClaw.Session01.csproj -c Release --no-restore
```

[Back to Session 1](../README.md) | [Setup](./setup.md) | [Offline guide](./offline.md)
