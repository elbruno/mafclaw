# Checkpoint 03 — Stock tool and hosted search

[Session 1 home](../../README.md) · [Previous: Harness core](../02-harness-core/README.md) · [Next: planning and todos](../04-planning-and-todos/README.md)

> **Runnable status:** Implemented. This checkpoint requires live Foundry configuration, Azure CLI authentication, and a service/model that supports hosted search.

## Purpose

Keep the Harness construction from checkpoint 02, register the local `get_stock_price` function, and enable the Harness's hosted-search composition.

The Harness standardizes how capabilities are assembled. It does not have exclusive ownership of tool calling: a standard Agent Framework agent can also use tools.

## Finance scenario

The program uses this fixed prompt:

```text
Show the illustrative MSFT price using get_stock_price.
Then find recent NVDA news using hosted web search and include inline source citations.
```

What stays the same:

- finance instructions and educational tone;
- Foundry project/model configuration;
- the explicit `IChatClient` and Harness construction.

What changes:

- `get_stock_price` is registered in `ChatOptions.Tools`;
- hosted search is enabled;
- the response prints an explicit used/not-used search marker.

## What owns what

- The application constructs `IChatClient`.
- The Harness constructs and composes the `AIAgent`.
- Application code owns `StockTools` and its local mock fixture.
- The Harness adds hosted search.
- The service/model decides whether hosted search is supported and can run.

## Run

From `session-01`:

```powershell
dotnet run --project .\checkpoints\03-tools-and-search\MafClaw.Checkpoint03.csproj
```

Successful live output starts with:

```text
LIVE · checkpoint 03 · tools and search
```

The model response varies. One of these stable lines follows it:

```text
[Hosted web search was used.]
[Hosted web search was not used for this response.]
```

The used marker verifies that the response contained hosted-search tool call or
result content. It does **not** verify citation annotations. Inspect citations
separately when the model returns them. The second marker is an honest result,
not a successful search claim.

Live execution and hosted search can incur Azure/model charges. Prompts and
relevant tool/query content are sent to the configured service and, when search
runs, to the hosted-search capability. Review your organization's data-sharing,
residency, logging, and cost policies. Do not send confidential, personal, or
real financial data.

If the service returns a content-filter or safety refusal, respect it. Do not
retry with evasive wording or attempt to bypass the policy. Remove unnecessary
sensitive content, choose a clearly benign educational prompt, or stop.

## Limitations

- Stock values come from local mock fixtures, not a market-data service.
- Hosted search is service-dependent and requires live execution.
- `TodoProvider` is disabled so tools/search remain the focus.
- No sample-owned plan/execute UX or approval yet.
- No offline fallback or file memory/session resume.

All displayed stock values are mock data. This sample is educational and is not financial advice.

[Previous: Harness core](../02-harness-core/README.md) · [Next: planning and todos](../04-planning-and-todos/README.md)
