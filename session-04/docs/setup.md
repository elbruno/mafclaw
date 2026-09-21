# Setup and execution

Install the stable .NET 10 SDK selected by the repository's `global.json`
(baseline 10.0.401). Live paths need an existing Foundry project/model and
approved Azure sign-in. PowerShell 7 is required for the setup scripts and
the optional local shell capability. Hyperlight additionally needs supported
local virtualization; never substitute an unsandboxed interpreter silently.

From the repository root:

```powershell
.\tools\configure-user-secrets.ps1 -Session 4 -WhatIf
.\tools\configure-user-secrets.ps1 -Session 4
.\tools\configure-user-secrets.ps1 -Session 4 -Check
```

All Session 4 live projects share the non-secret `UserSecretsId`
`mafclaw-session-04`. Values are not committed or printed. Set only the
features you intend to use:

| Key / environment variable | Purpose |
|---|---|
| `Foundry:ProjectEndpoint` / `FOUNDRY_PROJECT_ENDPOINT` | Required HTTPS project endpoint for live inference |
| `Foundry:Model` / `FOUNDRY_MODEL` | Required existing model deployment |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Optional HTTP collector **base** URI, such as `http://127.0.0.1:4318` |
| `Purview:Enabled` / `PURVIEW_ENABLED` | Explicit opt-in; false when absent |
| `Purview:ClientId` / `PURVIEW_CLIENT_APP_ID` | Approved application ID when enabling Purview |
| `Foundry:MemoryEnabled` / `FOUNDRY_MEMORY_ENABLED` | Explicitly choose an existing managed memory store instead of local memory |
| `Foundry:MemoryStore` / `FOUNDRY_MEMORY_STORE` | Required existing store when managed memory is enabled |
| `Foundry:MemoryScope` / `FOUNDRY_MEMORY_SCOPE` | Required unique opaque current-user scope from trusted configuration, never model input |

Local file memory does not require Foundry Memory or embedding settings.
Managed memory binds its configured current-user scope at startup and uses only explicit
`Remember ...` requests; the app does not create a cloud memory store.
Purview/managed-memory configuration must be provisioned and authorized separately.
Application Insights settings are supplied to the hosted runtime by its deployment,
not written to source or passed as command-line values.

From `session-04`:

```powershell
dotnet build .\MafClaw.Session04.slnx -c Release
dotnet run --project .\code\Console\MafClaw.Session04.Console.csproj -- --fixture
dotnet run --project .\code\Console\MafClaw.Session04.Console.csproj -- --no-shell --no-codeact
dotnet run --project .\code\Console\MafClaw.Session04.Console.csproj
dotnet run --project .\code\Evals\MafClaw.Session04.Evals.csproj -- --mode fixture
.\scripts\verify-session.ps1 -Mode Offline
```

The first console command is an explicit two-response fixture: ask for the
snapshot once, then `/exit`. The other two use live inference; the last enables
the complete local capability set. `/todos` inspects the actual provider;
`/memory` lists the current user's local memory files, without logging their
contents. The workspace is under the running application's output folder:
`working\data` for files/shell and `working\memory\current-user\facts` for memory.

Use `dotnet run` or the Windows apphost for Hyperlight. Do not assume direct
DLL invocation has equivalent process setup. Writes, simulated trades, shell
and this session's Hyperlight execution require explicit approval.
