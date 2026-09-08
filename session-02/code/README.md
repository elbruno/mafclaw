# Session 2 — Finance advisor app

This folder contains the runnable Session 2 finance advisor agent. It follows the official article's teaching arc: safe file access, human approval for side effects, and durable memory.

## Architecture

The code is intentionally small and demo-friendly. The isolated samples explain each concept first; this app shows the final Microsoft Agent Framework/Harness version.

The app uses:

- `AIProjectClient` to connect to Azure AI Foundry
- `AsIChatClient(...)` to adapt the project client
- `AsHarnessAgent(...)` to create the Harness agent
- `ChatOptions.Tools` and `AIFunctionFactory.Create(...)` to expose bounded C# tools

### Stage 1 — file access

- `portfolio.csv` stored in a working folder
- `read_portfolio_summary` reads only the approved portfolio file
- a user-facing summary of holdings

### Stage 2 — approval path

- `write_portfolio_report` asks approval before writing to disk
- `request_simulated_trade` asks approval before any simulated trade
- safe default rules for read-only file access

### Stage 3 — memory

- `remember_user_preference` stores a preference in local file memory
- `get_memory` reads durable local memory across restarts
- the optional `Foundry:MemoryStore` and `Foundry:EmbeddingModel` secrets are reserved for the next memory-store expansion

## Module structure

```text
code/
  MafClaw.Session02.csproj
  Program.cs
  AgentFinanceTools.cs
  PortfolioHolding.cs
  working/
    portfolio.csv
    memory.json
    reports/
```

## Expected behavior

The final agent should allow the user to:

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
3. step 3 — trade approval
4. step 4 — memory and session continuity

## Run it

From the repository root:

```powershell
.\tools\configure-user-secrets.ps1 -Session 2 -WhatIf
.\tools\configure-user-secrets.ps1 -Session 2
dotnet run --project .\session-02\code\MafClaw.Session02.csproj
```

Try:

```text
What is in my portfolio?
Write a short markdown report about my portfolio.
Remember that I am a conservative investor saving for a house in two years.
What do you remember about my investor profile?
Buy 10 shares of MSFT.
```

This agent remains intentionally readable and demo-friendly, following the session's live-coding goals.
