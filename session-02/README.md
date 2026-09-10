# Session 2 — Working With Your Data, Safely: Files, Approvals and Memory

This session builds on the first live-coding episode and adds the three capabilities that make a personal finance agent useful and trustworthy in the real world:

- file access over a controlled working folder
- approval gates for risky actions
- durable memory for preferences and watchlists

The official source article is:

- [Working With Your Data, Safely: Files, Approvals and Memory](https://devblogs.microsoft.com/agent-framework/agent-harness-working-with-your-data-safely/)

## What this session teaches

The agent from Session 1 can talk, browse the web, and plan. Session 2 adds the boundary that turns it from a clever conversation partner into a practical assistant for real user data.

The samples use a teaching ladder. The `10`, `20`, and `30` samples teach each safety primitive as plain C# so the behavior is easy to see. The agentic samples then show the same boundaries through Microsoft Agent Framework, Harness, local tools, and Foundry Memory. The final app in `code/` combines all three into one finance advisor.

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
   - agentic local memory uses explicit fixed-scope tools over an inspectable JSON file
   - Foundry memory remembers facts about the user across sessions
   - each approach is useful, but ownership, scalability, and infrastructure differ

## The sample story

The assistant keeps working inside the same personal-finance scenario used in Session 1:

- inspect a portfolio file
- summarize holdings and risk
- save a report to disk
- place a simulated trade only after approval
- remember personal preferences and watchlist items

## Planned sample flow

The live demo is expected to evolve in this order:

1. Ask: "What is in my portfolio?" to show the allowed file path.
2. Ask the printed `Denied prompt` from sample 11 to show that outside paths are refused.
3. Ask: "Write me a short report on my portfolio and save it."
4. Tell the assistant: "I am a conservative investor saving for a house in two years."
5. Ask: "Buy 10 shares of MSFT.", answer `y`, then ask again and answer `n`.
6. Ask: "What do you remember about my investor profile?"
7. Ask: "What do you remember about other users?" to show the denied memory path.

## Repository layout

- `code/` — the final Agent Framework/Harness finance advisor application
- `docs/` — setup, teaching notes, and the runbook for the session
- `samples/` — plain C#, agentic, and managed demos for safe file access, approvals, and memory
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
dotnet build .\session-02\samples\10-safe-file-access\MafClaw.Sample10.csproj
dotnet build .\session-02\samples\11-safe-file-access-agent\MafClaw.Sample11.csproj
dotnet build .\session-02\samples\20-approval-gates\MafClaw.Sample20.csproj
dotnet build .\session-02\samples\21-approval-gates-agent\MafClaw.Sample21.csproj
dotnet build .\session-02\samples\30-memory-store\MafClaw.Sample30.csproj
dotnet build .\session-02\samples\31-memory-store-agent\MafClaw.Sample31.csproj
dotnet build .\session-02\samples\32-local-file-memory-agent\MafClaw.Sample32.csproj

dotnet run --project .\session-02\samples\10-safe-file-access\MafClaw.Sample10.csproj
dotnet run --project .\session-02\samples\11-safe-file-access-agent\MafClaw.Sample11.csproj
dotnet run --project .\session-02\samples\20-approval-gates\MafClaw.Sample20.csproj
dotnet run --project .\session-02\samples\21-approval-gates-agent\MafClaw.Sample21.csproj
dotnet run --project .\session-02\samples\30-memory-store\MafClaw.Sample30.csproj
dotnet run --project .\session-02\samples\31-memory-store-agent\MafClaw.Sample31.csproj
dotnet run --project .\session-02\samples\32-local-file-memory-agent\MafClaw.Sample32.csproj
dotnet run --project .\session-02\code\MafClaw.Session02.csproj
```

The expected behavior is:

1. The secrets script targets `session-02\code\MafClaw.Session02.csproj`.
2. The base samples demonstrate safe file access, approval gates, and file-backed memory.
3. The agentic samples use Harness `FileAccessStore`, `ApprovalRequiredAIFunction`, explicit local memory tools, and `FoundryMemoryProvider`.
4. The finance advisor app combines those same behaviors as one complete Harness agent.
5. The app reads and writes only inside its working folder, requests approval before sensitive actions, and uses Foundry memory when configured.

## Session status

This folder is the public-facing Session 2 package. It keeps the pace of the live series: isolated concept sample, agentic bridge sample, then the complete finance-agent walkthrough.

## Privacy and safety notes

All prices and portfolio examples are mock educational data. This sample is not financial advice.

When using live model calls, prompts and tool results can be sent to the configured Azure Foundry service. Hosted web search may trigger external queries and incur charges. Do not use real personal, financial, or confidential data.

## Related materials

- [Session 1](../session-01/README.md)
- [General setup and prerequisites](../general/docs/README.md)
- [Official series page](https://aka.ms/mafclaw)
- [Series blog](https://aka.ms/mafclaw/blog)
