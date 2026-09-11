# Session 03 - Skills, Shell, CodeAct and Background Agents

Session 03 extends the Session 02 finance advisor with scalable, inspectable
orchestration:

- discoverable skills
- confined shell execution
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
