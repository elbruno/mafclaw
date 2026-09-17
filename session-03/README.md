# Session 03 - Skills, Shell, CodeAct and Background Agents

Session 03 extends the Session 02 finance advisor with scalable, inspectable
orchestration:

- discoverable skills
- approval-gated local shell execution
- CodeAct-style calculation boundaries
- background research tickets

The package follows the MafClaw session layout:

- `code/` - complete offline finance advisor, including Session 02 file access,
  approvals, memory, and the new Session 03 concepts
- `samples/` - numbered plain C# concepts followed by MAF bridge samples
- `docs/` - setup, architecture, teaching, and troubleshooting guidance

The official Microsoft Agent Framework post is:

https://devblogs.microsoft.com/agent-framework/agent-harness-scaling-the-claw-or-harness-capabilities/

All data is mock and educational. The complete `code` sample is offline and
does not call a model or access the network.

## Run the complete advisor

```powershell
dotnet run --project .\code\MafClaw.Session03.csproj
```

## Sample ladder

See [`samples/README.md`](samples/README.md) for the complete numbered ladder.

The plain-C# [Sample 20](samples/20-confined-shell/README.md) makes command
policy visible before the MAF shell bridge: `dotnet --version` is allowed,
while `dotnet --info` is denied before launching a child process. Its
five-second timeout terminates the owned process rather than merely ending
the wait. This sample demonstrates an allowlist, not an OS sandbox or a
human approval gate.

The live [Sample 21](samples/21-confined-shell-agent/README.md) then lets the
model propose PowerShell commands in a fresh mock workspace. MAF supplies
shell environment context and approval handling; the console displays real
tool results and the host verifies final filenames and unchanged content
hashes. Previous demo runs are preserved.

## CodeAct execution policy

Sample 31 currently uses **automatic sandbox execution** (`NeverRequire`),
not a per-execution human approval prompt. Its host file-tool scope is
unchanged. See [the CodeAct policy](samples/31-codeact-agent/README.md).

## Documentation

- [`docs/README.md`](docs/README.md)
- [`docs/setup.md`](docs/setup.md)
- [`docs/architecture.md`](docs/architecture.md)
- [`docs/troubleshooting.md`](docs/troubleshooting.md)

## Teaching-oriented source

Every C# file in this package starts with an objective and A/B/C step header.
Short comments before major blocks identify the boundary being demonstrated,
so presenters can explain the code while sharing the screen without adding
implementation noise.

## Main-agent orchestration extensions

Samples **42-47** extend the background-agent ladder without changing 40/41:
specialist teams, selective delegation, research/write/review, responsive
background jobs, partial results with deadlines, and a human-approved report.

Each is a runnable MAF project with `--mode live` and an explicitly scripted
`--mode fixture`. Start with the
[orchestration guide](docs/orchestration.md) for all six commands, boundaries,
and offline regression checks. These extensions do not change the existing
offline `code` advisor or add real trading.
