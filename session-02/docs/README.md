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

During the live demo, show both paths:

1. Ask `What is in my portfolio?` to show the allowed read from the approved working folder.
2. Copy the `Denied prompt` printed by `11-safe-file-access-agent` to ask for a harmless decoy file outside that folder.
3. Emphasize that the correct behavior is refusal: the agent should say it cannot access that folder because it is outside the approved working folder.

### 2. Approvals

A trade is a side-effecting action. It cannot run in the same way as a read-only lookup.

The harness provides approval middleware for:

- `Approve this call`
- `Always approve this tool`
- `Always approve this tool with these arguments`
- `Deny`

This becomes the human-in-the-loop boundary that makes the assistant safer and more accountable.

During the live demo, run the same trade request twice:

1. Ask `Buy 10 shares of MSFT.` and answer `y` at the approval prompt to show the approved path.
2. Ask `Buy 10 shares of MSFT.` again and answer `n` to show the denied path.
3. Emphasize that the model's request is not the boundary; the human approval decision is the boundary.

### 3. Memory

Memory is the next step after file and action safety.

The session introduces three levels:

- File memory: store watchlists or notes that the agent intentionally curates in files under a session-scoped working folder.
- Agentic local memory: expose explicit current-user save and recall tools over an inspectable JSON file.
- Foundry memory: durable facts about the user are saved to the platform memory store and recalled by the platform.

The difference is intentionally important:

- file memory is explicit and coarse-grained
- agentic local memory is reliable and inspectable, but the application owns isolation, concurrency, retention, and scale
- Foundry memory is platform-backed and scoped per user or demo identity

During the live demo, make sample 32 the reliable agentic path:

1. Tell the agent `Remember that I am a conservative investor saving for a house in two years.`
2. Enter `/memory` and show the exact JSON file.
3. Exit, restart, and ask `What do you remember about my investor profile?`.
4. Ask `What do you remember about other users?` to show that no cross-user tool exists.
5. Compare sample 31 as the managed Foundry Memory version.

If the console prints `Foundry memory save failed (403)` and the service details mention `401 Authentication` for the embedding deployment, the store exists but the backing Azure OpenAI resource rejected memory creation. Fix the Foundry memory store embedding deployment/authentication before expecting the UI to show saved memories.

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

The samples form a ladder from plain C# to agentic and managed implementations:

- `10-safe-file-access` proves the file boundary in plain C#.
- `11-safe-file-access-agent` turns that boundary into a Harness tool.
- `20-approval-gates` proves the human approval boundary in plain C#.
- `21-approval-gates-agent` turns the approval boundary into a Harness tool.
- `30-memory-store` proves durable local memory in plain C#.
- `32-local-file-memory-agent` turns local memory into explicit fixed-scope Harness tools.
- `31-memory-store-agent` demonstrates the managed Foundry Memory alternative.

The final app in `code/` combines all three patterns. It uses:

- `AIProjectClient`
- `AzureCliCredential`
- `AsIChatClient(...)`
- `AsHarnessAgent(...)`
- `FileAccessStore` with the built-in `file_access_*` tools for portfolio reads and report writes
- `ApprovalRequiredAIFunction` to gate the simulated trade tool
- `FoundryMemoryProvider` for durable user-fact recall, when configured
- `AgentConsoleRunner` to surface `ToolApprovalRequestContent` prompts and send the approval decision back to Harness

The teaching message is: the agent is still a model-driven loop, but the application owns the tools, the paths, the approvals, and the memory boundaries.

## Speaker line references

Use these line ranges when sharing the code on screen. The short header at the top of each file gives the audience the full sequence; pause on the implementation lines below to explain the boundary being added.

| Concept | File and lines | Presentation emphasis |
|---|---|---|
| Safe path | `samples/10-safe-file-access/Program.cs:10-30, 32-51` | Create the sandbox, allow the portfolio read, and block the outside path. |
| Harness file tools | `samples/11-safe-file-access-agent/Program.cs:34-50, 53-67, 82-85` | Root `FileAccessStore` at the working folder, create a harmless outside decoy, and copy the printed denied prompt. |
| Blank input guard | `samples/11-safe-file-access-agent/AgentConsoleRunner.cs:20-33` | Ignore accidental empty Enter presses before sending prompts to Harness. |
| Direct approval | `samples/20-approval-gates/Program.cs:7-29` | A side effect waits for an explicit human decision. |
| Harness approval | `samples/21-approval-gates-agent/ApprovalGateTools.cs:13-33` and `Program.cs:40-57, 61-64` | `ApprovalRequiredAIFunction` keeps the model from executing the trade directly, then the sample prints approve and deny paths. |
| Explicit memory | `samples/30-memory-store/Program.cs:9-29, 30-55` | Save, restart, reload, and show a missing-memory denied path. |
| Agentic local memory | `samples/32-local-file-memory-agent/Program.cs:14-41, 43-62, 64-74`, `LocalMemoryTools.cs:10-27, 29-43`, and `LocalFileMemoryStore.cs:16-30, 32-67, 71-130` | Give the model only current-user save/recall tools, reveal the JSON with `/memory`, and prove restart persistence. |
| Foundry memory | `samples/31-memory-store-agent/Program.cs:23-63, 72-91, 98-104` and `FoundryMemoryDemoStore.cs:9-43` | Print the memory store/scope, save the "Remember..." prompt as a real user-profile memory, then recall only the current scope. |
| Final composition | `code/Program.cs:33-50, 54-75, 91-123, 131-143` | Combine the safe root, memory provider, approval rules, tool surface, and paired allowed/denied prompts. |
| Approval round-trip | `code/AgentConsoleRunner.cs:13-52, 54-77` | Show how the console saves memory prompts and sends the user's approval back to Harness. |

## Demo safety checklist

- use mock data only
- avoid real personal financial inputs
- never screen-share raw exceptions or terminal output that includes resource IDs or tenant markers
- do not claim that every service or region supports every memory type
- explain that hosted search and memory both involve service-level policies and cost implications

## Planned sample prompts

Allowed path:

- `What is in my portfolio?`
- `Write me a short report on my portfolio and save it.`
- `I am a conservative investor saving for a house in two years.`
- `What do you know about me?`
- `Buy 10 shares of MSFT.` then answer `y`

Denied path:

- copy the `Denied prompt` printed by sample 11
- `What do you remember about other users?`
- `Buy 10 shares of MSFT.` then answer `n`

## Related references

- [Official article](https://devblogs.microsoft.com/agent-framework/agent-harness-working-with-your-data-safely/)
- [Session 2 overview](../README.md)
- [Session 1 overview](../../session-01/README.md)
