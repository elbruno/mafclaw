# Session 2 — Speaker and demo notes

## Why this session matters

Session 1 taught the agent to talk and to use tools. Session 2 teaches the agent how to work safely with user-owned files and user-owned actions.

This is the point where the harness becomes a real control plane for a helpful, but bounded, personal assistant.

## The three teaching pillars

### 1. File access

The assistant should read from a real portfolio file instead of inventing the user's holdings.

The examples show a constrained file-access provider that points at a working folder, enabling:

- list files
- search files
- read files
- write a report
- delete or overwrite only under approved conditions

The most important teaching point is that the tools are scoped to a safe path and should not be treated as unlimited filesystem access.

### 2. Approvals

A trade is a side-effecting action. It cannot run in the same way as a read-only lookup.

The harness provides approval middleware for:

- `Approve this call`
- `Always approve this tool`
- `Always approve this tool with these arguments`
- `Deny`

This becomes the human-in-the-loop boundary that makes the assistant safer and more accountable.

### 3. Memory

Memory is the next step after file and action safety.

The session introduces two complementary models:

- File memory: store watchlists or notes that the agent intentionally curates in files under a session-scoped working folder.
- Foundry memory: durable facts about the user are captured and recalled automatically by the platform.

The difference is intentionally important:

- file memory is explicit and coarse-grained
- Foundry memory is implicit and fine-grained

## The live-session arc

The audience should see the same use case evolve over time:

1. inspect a portfolio file
2. write a report to disk
3. explain and then remember things about the user
4. place a simulated trade under approval
5. persist a watchlist in file memory
6. connect those primitives to the final Agent Framework Harness app
7. review memory across a session restart

## Agent connection

The samples are paired so every concept has a plain C# version and an agentic version:

- `10-safe-file-access` proves the file boundary in plain C#.
- `11-safe-file-access-agent` turns that boundary into a Harness tool.
- `20-approval-gates` proves the human approval boundary in plain C#.
- `21-approval-gates-agent` turns the approval boundary into a Harness tool.
- `30-memory-store` proves durable local memory in plain C#.
- `31-memory-store-agent` turns memory into Harness tools.

The final app in `code/` combines all three patterns. It uses:

- `AIProjectClient`
- `AzureCliCredential`
- `AsIChatClient(...)`
- `AsHarnessAgent(...)`
- `AIFunctionFactory.Create(...)`

The app registers the same safety primitives as agent tools. The teaching message is: the agent is still a model-driven loop, but the application owns the tools, the paths, the approvals, and the memory boundaries.

## Demo safety checklist

- use mock data only
- avoid real personal financial inputs
- never screen-share raw exceptions or terminal output that includes resource IDs or tenant markers
- do not claim that every service or region supports every memory type
- explain that hosted search and memory both involve service-level policies and cost implications

## Planned sample prompts

- `What is in my portfolio?`
- `Write me a short report on my portfolio and save it.`
- `I am a conservative investor saving for a house in two years.`
- `Buy 10 shares of MSFT.`
- `Add SPY to my watchlist.`
- `What is on my watchlist?`
- `What do you know about me?`

## Related references

- [Official article](https://devblogs.microsoft.com/agent-framework/agent-harness-working-with-your-data-safely/)
- [Session 2 overview](../README.md)
- [Session 1 overview](../../session-01/README.md)
