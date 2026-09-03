# Standard agent to Harness: ownership comparison

The checkpoints keep one scenario—a concise personal-finance assistant—while moving responsibility between the sample, Agent Framework, the Harness, and the local console.

The starting point is already an **agent**. The Harness does not turn a raw model into the first agent in this sequence; it standardizes how an existing Agent Framework application composes capabilities around a chat client.

| Concern | Checkpoint 01: standard agent | Checkpoint 02: Harness core | Checkpoint 03: tools and search | Checkpoint 04 / final sample | Owner |
|---|---|---|---|---|---|
| Agent construction | `AIProjectClient.AsAIAgent(...)` uses the convenience path backed by `FoundryChatClient`. | The Project OpenAI Responses client is explicitly adapted to `IChatClient` and passed to `AsHarnessAgent(...)`. | Same Harness construction, now with capabilities enabled. | Same Harness construction as checkpoint 03. | Agent Framework creates the agent; application code chooses the construction path. |
| `IChatClient` | Used behind the convenience API; no explicit variable appears in the sample. | Created explicitly from the Foundry Responses client. | Passed to `AsHarnessAgent(...)`. | Passed to `AsHarnessAgent(...)`. | Application/provider integration. The two baseline checkpoints intentionally do not use the same client-construction code. |
| Harness | Not used. | Introduced with optional extras disabled to isolate core construction. | Retained as the capability-composition layer. | Retained. | `Microsoft.Agents.AI.Harness`. |
| Stock tool | Not yet added in this teaching sequence. | Disabled/not registered. | `get_stock_price` is registered in `ChatOptions.Tools`. | Same local tool and mock fixture. | Application code (`StockTools`). Tool calling is an Agent Framework capability and is **not unique to the Harness**. |
| Hosted search | Not enabled by this sample. | Explicitly disabled. | Enabled in Harness options. | Enabled in Harness options. | Harness adds the hosted tool; the configured model service must support it. |
| `TodoProvider` | Not used. | Explicitly disabled. | Explicitly disabled so tools/search remain the focus. | Retrieved from the agent and displayed by `/todos`. | The Harness configures/provides the instance by default; `TodoProvider` is a reusable Agent Framework context provider. `ClawConsole` owns presentation. |
| Local modes | Not used. | Not used. | Not used by the checkpoint UI. | `plan` and `execute` are local console states. | `ClawConsole`. The Harness `AgentModeProvider` is disabled in the final sample. |
| Structured planning and approval | Not used. | Not used. | Not used. | `PlanningResponse` constrains the planning turn; `ClawConsole` asks for explicit yes/no approval before generated-plan execution. `/mode execute` opts into direct execution. | Sample console, not the Harness mode provider and not tool approval. Mode remains sticky until changed. |
| Console | Minimal sample entry point. | Minimal sample entry point. | Demonstrates Harness-backed responses and tools. | Interactive `ClawConsole` with `/mode`, `/todos`, and `/exit`. | Application code. |
| Memory and session resume | Not implemented. | Not implemented. | Harness file memory is not used. | `DisableFileMemory = true`; no `/session-export` or `/session-import`. | Supplemental material in the official article, not a runnable checkpoint in this repository. |
| Offline fallback | Not model execution. | Not model execution. | Not Harness execution. | Separate `OfflineClaw` path with deterministic local fixtures. | Application code. It does not call a model, the Harness, Foundry, or hosted search. |

## What stays the same

- The assistant remains a personal-finance learning scenario.
- Checkpoints 01 and 02 keep the same generic beginner-stock prompt.
- Checkpoints 01 and 02 also keep the same Foundry provider, Responses API,
  deployment/model, instructions, and intended educational behavior.
- Their client construction differs intentionally: convenience
  `AIProjectClient.AsAIAgent(...)` versus an explicitly constructed Responses
  `IChatClient` passed to `AsHarnessAgent(...)`.
- Checkpoints 03 and 04 add concrete ticker examples; their stock values come from local mock data, not a market-data provider.
- The sample is educational and never executes trades or provides personalized financial advice.
- Live execution uses the configured Foundry project and model.

## What changes

1. **Checkpoint 01** establishes the baseline: a short, plain `AIAgent` prelude based on the official hosted-agent sample.
2. **Checkpoint 02** exposes the `IChatClient` boundary and wraps it with a minimal `AsHarnessAgent(...)`; optional Harness capabilities are disabled so the construction change is clear.
3. **Checkpoint 03** enables the local stock tool and service-dependent hosted search.
4. **Checkpoint 04** surfaces the `TodoProvider` instance configured/provided by
   the Harness and adds sample-owned structured planning, local modes, and approval.

The value of the Harness is consistent composition, not exclusive access to tool calling. You can add tools and providers to agents without adopting the full Harness; the Harness packages common capabilities behind one configurable construction path.

## Current final-sample differences from the official article

The official article presents a broader default Harness experience. This repository's final Session 1 sample intentionally:

- disables `AgentModeProvider`;
- disables file memory;
- uses `ClawConsole` for local plan/execute state and structured approval;
- keeps the `TodoProvider` instance configured/provided by the Harness;
- leaves hosted search enabled, subject to service support;
- provides a separate deterministic offline fallback.

Checkpoint 04 narrows the live demo further by also disabling skills, compaction,
OpenTelemetry, and tool auto-approval. Those switches keep the checkpoint focused
on `TodoProvider` plus the sample-owned planning and approval UX; do not infer that
every Harness application should disable them.

See the [official Session 1 article](https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/) for file-memory and session export/import examples.

[Back to Session 1](../README.md)
