# Session 2 — Working With Your Data, Safely: Files, Approvals and Memory

This session builds on the first live-coding episode and adds the three capabilities that make a personal finance agent useful and trustworthy in the real world:

- file access over a controlled working folder
- approval gates for risky actions
- durable memory for preferences and watchlists

The official source article is:

- [Working With Your Data, Safely: Files, Approvals and Memory](https://devblogs.microsoft.com/agent-framework/agent-harness-working-with-your-data-safely/)

## What this session teaches

The agent from Session 1 can talk, browse the web, and plan. Session 2 adds the boundary that turns it from a clever conversation partner into a practical assistant for real user data.

The isolated samples first teach each safety primitive as plain C# so the behavior is easy to see. The final app in `code/` connects those primitives to the agent world by registering them as Microsoft Agent Framework Harness tools.

### Core concepts

1. File access
   - read a portfolio from disk
   - search, list, and read only under a safe working folder
   - write a report to a Markdown file only when asked

2. Approvals
   - risky actions should pause and ask before running
   - review prompts provide a safe human-in-the-loop boundary
   - standing approvals and auto-approval rules keep low-risk flows smooth

3. Memory
   - file memory stores files the agent decides to curate, such as a watchlist
   - Foundry memory remembers facts about the user across sessions
   - both are useful, but they solve different problems

## The sample story

The assistant keeps working inside the same personal-finance scenario used in Session 1:

- inspect a portfolio file
- summarize holdings and risk
- save a report to disk
- place a simulated trade only after approval
- remember personal preferences and watchlist items

## Planned sample flow

The live demo is expected to evolve in this order:

1. Ask: "What is in my portfolio?"
2. Ask: "Write me a short report on my portfolio and save it."
3. Tell the assistant: "I am a conservative investor saving for a house in two years."
4. Ask: "Buy 10 shares of MSFT."
5. Ask: "Add SPY to my watchlist."
6. Restart the session and ask: "What is on my watchlist?" or "What do you know about me?"

## Repository layout

- `code/` — the final Agent Framework/Harness finance advisor application
- `docs/` — setup, teaching notes, and the runbook for the session
- `samples/` — isolated concept demos for safe file access, approvals, and memory
- `general/` in the root repo — shared setup, mock-data, and cross-session guidance

## How to test it

Run these commands from the repository root.

```powershell
$env:FOUNDRY_PROJECT_ENDPOINT = "https://YOUR-FOUNDRY-ENDPOINT.services.ai.azure.com/api/projects/YOUR-PROJECT"
$env:FOUNDRY_MODEL = "YOUR-MODEL-OR-DEPLOYMENT"
$env:FOUNDRY_MEMORY_STORE = "YOUR-MEMORY-STORE"            # optional
$env:FOUNDRY_EMBEDDING_MODEL = "YOUR-EMBEDDING-MODEL"     # optional

.\tools\configure-user-secrets.ps1 -Session 2 -WhatIf
.\tools\configure-user-secrets.ps1 -Session 2

dotnet build .\session-02\code\MafClaw.Session02.csproj
dotnet build .\session-02\samples\01-safe-file-access\MafClaw.Sample01.csproj
dotnet build .\session-02\samples\02-approval-gate\MafClaw.Sample02.csproj
dotnet build .\session-02\samples\03-memory-store\MafClaw.Sample03.csproj

dotnet run --project .\session-02\samples\01-safe-file-access\MafClaw.Sample01.csproj
dotnet run --project .\session-02\samples\02-approval-gate\MafClaw.Sample02.csproj
dotnet run --project .\session-02\samples\03-memory-store\MafClaw.Sample03.csproj
dotnet run --project .\session-02\code\MafClaw.Session02.csproj
```

The expected behavior is:

1. The secrets script targets `session-02\code\MafClaw.Session02.csproj`.
2. The isolated samples demonstrate safe file access, approval gates, and file-backed memory.
3. The finance advisor app exposes those same behaviors as Harness agent tools.
4. The app reads and writes only inside its working folder, requests approval before sensitive actions, and preserves memory between runs.

## Session status

This folder is the public-facing Session 2 package. It keeps the pace of the live series: small isolated concept samples first, then the bigger finance-agent walkthrough.

## Privacy and safety notes

All prices and portfolio examples are mock educational data. This sample is not financial advice.

When using live model calls, prompts and tool results can be sent to the configured Azure Foundry service. Hosted web search may trigger external queries and incur charges. Do not use real personal, financial, or confidential data.

## Related materials

- [Session 1](../session-01/README.md)
- [General setup and prerequisites](../general/docs/README.md)
- [Official series page](https://aka.ms/mafclaw)
- [Series blog](https://aka.ms/mafclaw/blog)
