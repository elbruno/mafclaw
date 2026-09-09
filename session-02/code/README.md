# Session 2 — Finance advisor app

This folder contains the runnable Session 2 finance advisor agent. It follows the official article's teaching arc: safe file access, human approval for side effects, and durable memory.

## Architecture

The code is intentionally small and demo-friendly. The isolated samples explain each concept first; this app shows the final Microsoft Agent Framework/Harness version.

The app uses:

- `AIProjectClient` to connect to Azure AI Foundry
- `AsIChatClient(...)` to adapt the project client
- `AsHarnessAgent(...)` to create the Harness agent
- `FileAccessStore` to enable the built-in Harness `file_access_*` tools
- `ApprovalRequiredAIFunction` to gate the simulated trade tool
- `FoundryMemoryProvider` to enable platform-backed durable memory when configured

### Stage 1 — file access

- `portfolio.csv` stored in a working folder
- Harness `FileAccessStore` roots file tools at the approved working folder
- read-only file tools are auto-approved with `FileAccessProvider.ReadOnlyToolsAutoApprovalRule`
- writes still go through the Harness approval flow

### Stage 2 — approval path

- file writes use the built-in `file_access_*` write tools and require approval
- `request_simulated_trade` is wrapped with `ApprovalRequiredAIFunction`
- no custom console prompt is hidden inside the tool implementation

### Stage 3 — memory

- Foundry memory is enabled when `Foundry:MemoryStore` and `Foundry:EmbeddingModel` are configured
- when disabled, the app prints that Foundry memory is disabled and does not fall back to any local memory store
- the memory store name is a logical store name, not a URL or secret

## Module structure

```text
code/
  MafClaw.Session02.csproj
  Program.cs
  AgentFinanceTools.cs
  AgentConsoleRunner.cs
  working/
    portfolio.csv
```

`AgentConsoleRunner.cs` runs the console loop and handles `ToolApprovalRequestContent` prompts, sending the user's `[y/N]` decision back to Harness as `ToolApprovalResponseContent` so approval-required tools (like the simulated trade) can complete.

## Expected behavior

The final agent should allow the user to:

- inspect holdings from a real file
- save a Markdown report to disk
- place a simulated trade only after approval
- remember durable user preferences when Foundry memory is configured

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
