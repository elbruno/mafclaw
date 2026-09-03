# Checkpoint 02 — Meet the Harness

[Session 1 home](../../README.md) · Previous: [01 — Hello, Agent](../01-hello-agent/README.md) · Next: [03 — Tools and Search](../03-tools-and-search/README.md)

## What this teaches

Same agent, same prompt, same response — but built through the Harness. The construction call changes from `AIProjectClient.AsAIAgent(...)` to an explicit `IChatClient` passed to `.AsHarnessAgent(...)`.

**The tiny diff IS the lesson.** The Harness standardizes how capabilities are composed around a chat client.

## What changed from Checkpoint 01

| Change | Detail |
|---|---|
| `IChatClient` is now explicit | Built from `AIProjectClient → GetProjectOpenAIClient() → GetResponsesClient() → AsIChatClient(model)` |
| Agent construction | `.AsHarnessAgent(new HarnessAgentOptions { ... })` replaces `.AsAIAgent(...)` |
| New package | `Microsoft.Agents.AI.Harness` added to `.csproj` |
| File memory disabled | `DisableFileMemory = true` — keeps the demo focused on construction |

## What's in the box

| File | Lines | Purpose |
|---|---|---|
| `Program.cs` | 28 | Explicit `IChatClient`, Harness agent construction, one prompt |
| `.csproj` | ~20 | Adds `Microsoft.Agents.AI.Harness` package |

## How to run

From `session-01`:

```powershell
dotnet run --project .\checkpoints\02-harness-agent\MafClaw.Checkpoint02.csproj
```

## What to expect

Identical model response to Checkpoint 01. The output looks the same — the difference is in how the agent was built.

## Next step

[Checkpoint 03](../03-tools-and-search/README.md) gives the agent a tool: `get_stock_price` with inline mock data.
