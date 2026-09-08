# Session 2 samples

These samples use a paired structure for each Session 2 concept:

- `10`, `20`, and `30` are small plain C# demos that isolate the concept.
- `11`, `21`, and `31` are the same ideas exposed as Microsoft Agent Framework/Harness tools.
- `..\code\` is the complete finance advisor that combines all three concepts.

## 10 and 11 — safe file access

- `10-safe-file-access` demonstrates a working folder with allowed reads.
- `11-safe-file-access-agent` uses the Harness `FileAccessStore` with the built-in `file_access_*` tools rooted at the approved working folder.

## 20 and 21 — approval gates

- `20-approval-gates` models the human-in-the-loop approval flow before side effects.
- `21-approval-gates-agent` exposes the simulated trade approval as the `request_simulated_trade` agent tool.

## 30 and 31 — memory store

- `30-memory-store` shows durable state stored to disk and restored across a restart.
- `31-memory-store-agent` exposes local memory as the `remember_user_preference` and `get_memory` agent tools.

## Run them

Configure secrets once before running the agentic samples:

```powershell
.\tools\configure-user-secrets.ps1 -Session 2 -WhatIf
.\tools\configure-user-secrets.ps1 -Session 2
```

From the `session-02` folder:

```powershell
dotnet run --project .\samples\10-safe-file-access\MafClaw.Sample10.csproj
dotnet run --project .\samples\11-safe-file-access-agent\MafClaw.Sample11.csproj

dotnet run --project .\samples\20-approval-gates\MafClaw.Sample20.csproj
dotnet run --project .\samples\21-approval-gates-agent\MafClaw.Sample21.csproj

dotnet run --project .\samples\30-memory-store\MafClaw.Sample30.csproj
dotnet run --project .\samples\31-memory-store-agent\MafClaw.Sample31.csproj
```

From the repository root:

```powershell
dotnet run --project .\session-02\samples\10-safe-file-access\MafClaw.Sample10.csproj
dotnet run --project .\session-02\samples\11-safe-file-access-agent\MafClaw.Sample11.csproj

dotnet run --project .\session-02\samples\20-approval-gates\MafClaw.Sample20.csproj
dotnet run --project .\session-02\samples\21-approval-gates-agent\MafClaw.Sample21.csproj

dotnet run --project .\session-02\samples\30-memory-store\MafClaw.Sample30.csproj
dotnet run --project .\session-02\samples\31-memory-store-agent\MafClaw.Sample31.csproj
```
