# Offline fallback

Offline mode is a deterministic rehearsal and recovery path. It is deliberately separate from the live Agent Framework path.

## What offline mode does

- reads local mock-market fixtures;
- prints a prominent `OFFLINE FALLBACK` banner;
- supports an interactive `/stock <SYMBOL>` command and a local `/plan` demonstration;
- supports deterministic `stock` and `plan` smoke scenarios;
- produces repeatable output without Azure configuration.

## What offline mode does not do

Offline output is **not**:

- model inference;
- Agent Framework `AIAgent` execution;
- Harness execution;
- function calling chosen by a model;
- hosted search;
- a service-backed `TodoProvider`;
- evidence that live credentials, service access, or a model deployment work.

It is a deterministic `OfflineClaw` simulation that demonstrates the scenario,
console contract, mock lookup data, and approval boundary only.

## Run from `session-01\code`

Interactive:

```powershell
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline
```

Deterministic stock scenario:

```powershell
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline --scenario stock
```

Stable markers:

```text
OFFLINE FALLBACK
SCENARIO stock
MSFT: 512.34 USD (mock)
NVDA: 184.72 USD (mock)
```

Deterministic planning boundary:

```powershell
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline --scenario plan
```

Stable markers:

```text
OFFLINE FALLBACK
SCENARIO plan
Approval granted: no
Execution triggered: no
Execution blocked until explicit approval.
```

Interactive commands:

```text
/stock MSFT
/stock NVDA
/stock AMZN
/plan
/exit
```

The final compatibility fixture supports MSFT, NVDA, and AMZN. Checkpoint projects
that use stock data link their canonical fixture from
`session-01\shared\mock-market-data.json`. Every displayed value is mock data and
may intentionally differ from real markets.

## When to use it

Use offline mode for:

- rehearsing the teaching flow;
- checking deterministic output markers;
- working without network or Azure access;
- demonstrating that the approval boundary blocks execution.

Use live mode when you need to demonstrate actual model, Harness, model-selected
tool calling, `TodoProvider`, or hosted-search behavior.

This project is educational and is not financial advice.

[Back to Session 1](../README.md) | [Setup](./setup.md)
