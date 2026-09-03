# Checkpoint 01 — Hello, Agent

[Session 1 home](../../README.md) · Next: [02 — Meet the Harness](../02-harness-agent/README.md)

## What this teaches

The minimum code to talk to a Foundry model using the Microsoft Agent Framework. Eighteen lines of C#, one file, one concept: **create an agent, send a prompt, print the response.**

## What's in the box

| File | Lines | Purpose |
|---|---|---|
| `Program.cs` | 18 | Top-level statements — config, agent creation via `AIProjectClient.AsAIAgent(...)`, one prompt |
| `.csproj` | ~18 | .NET 10 project file with `NoWarn` for preview packages |

No config class. No try/catch. No error handling files. Configuration is three inline lines using `AddUserSecrets<Program>().AddEnvironmentVariables()`.

## How to run

From `session-01`:

```powershell
dotnet run --project .\checkpoints\01-hello-agent\MafClaw.Checkpoint01.csproj
```

### Prerequisites

1. .NET 10 SDK and Azure CLI installed
2. Authenticated via `az login`
3. User-secrets configured (from repo root): `.\tools\configure-user-secrets.ps1 -Session 1`

## What to expect

The model prints a concise response about stock-buying basics, then the program exits. That's it — **this is already an agent.**

## Next step

[Checkpoint 02](../02-harness-agent/README.md) wraps the same behavior in the Harness. The diff between 01 and 02 **is** the lesson.
