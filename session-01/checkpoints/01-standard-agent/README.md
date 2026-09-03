# Checkpoint 01 — Standard Agent Framework agent

[Session 1 home](../../README.md) · Previous: none · [Next: Harness core](../02-harness-core/README.md)

> **Runnable status:** Implemented. This checkpoint requires live Foundry configuration and Azure CLI authentication.

## Purpose

Begin with the shortest possible prelude: a plain Microsoft Agent Framework `AIAgent`, based on the official [Foundry hosted-agent sample](https://github.com/Azure-Samples/microsoft-foundry-hosted-agents/blob/main/01-MAF-Agent-CS/Program.cs).

The baseline is already an agent. This checkpoint is not a raw SDK chat call and the Harness is not required to make it an agent.

For parity with checkpoint 02, it uses the same Foundry provider, Responses API,
deployment/model, instructions, prompt, and intended behavior. Its client
construction is intentionally different: `AIProjectClient.AsAIAgent(...)` is the
convenience path backed by `FoundryChatClient`.

## Finance scenario

The program uses this fixed prompt:

```text
What are three things a beginner should consider before buying a stock?
```

The scenario and configuration continue through all checkpoints. This checkpoint intentionally does not claim current prices, search the web, create todos, or plan execution.

## What owns what

- `AIProjectClient` plus the Agent Framework convenience API constructs the `AIAgent`.
- The application supplies its finance instructions and prompt.
- The application entry point owns the minimal console interaction.
- No Harness is present.

Tool calling is possible with standard Agent Framework agents; it is simply deferred in this teaching sequence so the baseline remains easy to compare.

## Run

From `session-01`:

```powershell
dotnet run --project .\checkpoints\01-standard-agent\MafClaw.Checkpoint01.csproj
```

A successful run prints `LIVE · checkpoint 01 · standard agent`, then one model
response, and exits with code 0. Model-generated prose varies and is not a stable
assertion.

## Limitations

- No Harness composition.
- No registered `get_stock_price` tool in this checkpoint.
- No hosted search.
- No `TodoProvider`.
- No local plan/execute modes or approval.
- No offline fallback unless the implementation README explicitly adds one.

All stock references are educational examples, not current quotes or financial advice.

[Session 1 home](../../README.md) · [Next: Harness core](../02-harness-core/README.md)
