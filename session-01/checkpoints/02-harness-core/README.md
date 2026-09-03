# Checkpoint 02 — Explicit chat client and Harness core

[Session 1 home](../../README.md) · [Previous: standard agent](../01-standard-agent/README.md) · [Next: tools and search](../03-tools-and-search/README.md)

> **Runnable status:** Implemented. This checkpoint requires live Foundry configuration and Azure CLI authentication.

## Purpose

Keep the same Foundry provider, Responses API, deployment/model, instructions,
fixed prompt, and intended behavior as checkpoint 01, but expose the boundary
used by the official article: obtain the Project OpenAI Responses client, adapt
it to `IChatClient`, then pass it to `AsHarnessAgent(...)`.

This is behavioral/provider parity, not client-construction parity. Checkpoint 01
uses the `AIProjectClient.AsAIAgent(...)` convenience path backed by
`FoundryChatClient`; checkpoint 02 constructs the Responses `IChatClient`
explicitly.

The optional Harness capabilities are deliberately disabled in this checkpoint. That isolates the architectural change before tools and search are enabled.

## Finance scenario

```text
What are three things a beginner should consider before buying a stock?
```

Using the same prompt as checkpoint 01 makes the construction change—not a change in scenario—the focus.

## What owns what

- The application constructs the provider-specific `IChatClient`.
- `AsHarnessAgent(...)` constructs the `AIAgent`.
- The application still owns instructions and the one-shot console entry point.
- Harness extras such as hosted search, todos, modes, file memory, and tool auto-approval are explicitly disabled.

An explicit `IChatClient` provides a clean provider boundary. The Harness then provides one configurable place to compose capabilities.

## Run

From `session-01`:

```powershell
dotnet run --project .\checkpoints\02-harness-core\MafClaw.Checkpoint02.csproj
```

A successful run prints `LIVE · checkpoint 02 · harness core`, then one model
response, and exits with code 0. Model-generated prose varies and is not a stable
assertion.

## Limitations

- No sample stock tool.
- Hosted search and `TodoProvider` are disabled.
- No local planning or approval.
- No offline fallback.
- Live mode requires valid Foundry configuration and authentication.

Do not include real financial or personal information in prompts. This sample is educational and is not financial advice.

[Previous: standard agent](../01-standard-agent/README.md) · [Next: tools and search](../03-tools-and-search/README.md)
