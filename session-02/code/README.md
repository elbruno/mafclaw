# Session 2 — Finance advisor app

This folder contains the runnable Session 2 finance advisor sample. It follows the official article's teaching arc: safe file access, human approval for side effects, and durable memory.

## Architecture

The code is intentionally small and demo-friendly. The live session can introduce it in layers instead of treating it as one monolithic application.

### Stage 1 — file access

- `portfolio.csv` stored in a working folder
- `read_portfolio` style behavior using the file-access provider
- list/search/read helpers
- a user-facing summary of holdings

### Stage 2 — approval path

- `place_trade` tool with a simulated order action
- approval-required wrapper around a risky action
- safe default rules for read-only file access
- custom policy for small trades below a threshold

### Stage 3 — memory

- file memory for session-persisted watchlist notes
- optional Foundry memory for durable personal facts
- session export/import to restore continuity and demonstrate persistence

## Module structure

```text
code/
  MafClaw.Session02.csproj
  Program.cs
  working/
    portfolio.csv
    reports/
    agent-file-memory/
```

## Expected behavior

The final sample should allow the user to:

- inspect holdings from a real file
- save a Markdown report to disk
- place a simulated trade only after approval
- remember a preference across a restart
- keep a watchlist in file memory

## Safety expectations

- use mock financial values only
- never write secrets or real credentials into the sample
- keep file operations scoped to a designated working folder
- preserve the human approval boundary before side-effects are executed

## Learn the sample in order

1. step 1 — file access and portfolio review
2. step 2 — report writing and safe write approval
3. step 3 — trade approval and standing approvals
4. step 4 — memory and session continuity

## Run it

From the repository root:

```powershell
.\tools\configure-user-secrets.ps1 -Session 2 -WhatIf
.\tools\configure-user-secrets.ps1 -Session 2
dotnet run --project .\session-02\code\MafClaw.Session02.csproj
```

This sample remains intentionally readable and demo-friendly, following the session's live-coding goals.
