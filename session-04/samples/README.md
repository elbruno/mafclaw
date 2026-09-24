# Session 4 sample ladder

Each numbered project is independently runnable from this session. The plain
projects reference only the BCL finance contracts, not the MAF agent library.
The operational MAF samples use the shared factory so instrumentation and
policy do not drift. Audience follow-ups 51, 61 and 71 create focused standalone
agents to teach MCP tools, session persistence and local inference; they do not
add these features to the complete finance app.
All inputs are synthetic and educational, not financial advice.

| Pair | Plain C# | Microsoft Agent Framework |
|---|---|---|
| 10 / 11 | Real `ActivitySource` and `Meter` observations | Actual agent/tool spans, privacy filtering and OpenTelemetry export |
| 20 / 21 | Policy before a local side effect | Actual `ApprovalRequiredAIFunction` proposal/decision and verified trade ledger |
| 30 / 31 | Exact decimal evaluation and an intentional failing candidate | `LocalEvaluator`, `FunctionEvaluator` and `EvaluateAsync` with actual tool evidence |
| 40 / 41 | Real ASP.NET readiness/portfolio HTTP lifecycle | Real Foundry Responses hosting; require `status=completed`, not just HTTP 200 |
| 22 | Not a plain-policy substitute | Conditional real `WithPurview` screening |
| 32 | Not a deterministic-score substitute | Conditional real `FoundryEvals` model grading |
| 50 / 51 | Raw MCP client: connect, list tools, call a tool | Two agents answer one question: `HostedWebSearchTool` vs. real Microsoft Learn MCP tools |
| 60 / 61 | Explicit transcript saved/reloaded as JSON | Real MAF `AgentSession` serialize/deserialize, proven by resent history |
| 70 / 71 | Real on-device chat completion via Foundry Local + the official OpenAI SDK | Same on-device model bridged to `IChatClient`, driving a real local tool call inside the harness |

## Commands

Run from `session-04`:

```powershell
dotnet run --project .\samples\10-observability\MafClaw.Sample10.csproj
dotnet run --project .\samples\11-observability-agent\MafClaw.Sample11.csproj -- --fixture
dotnet run --project .\samples\20-governance\MafClaw.Sample20.csproj
dotnet run --project .\samples\21-governance-agent\MafClaw.Sample21.csproj -- --fixture
dotnet run --project .\samples\30-evaluations\MafClaw.Sample30.csproj
dotnet run --project .\samples\30-evaluations\MafClaw.Sample30.csproj -- --inject-regression
dotnet run --project .\samples\31-evaluations-agent\MafClaw.Sample31.csproj -- --fixture
dotnet run --project .\samples\40-host-contract\MafClaw.Sample40.csproj -- --self-test
dotnet run --project .\samples\41-hosted-agent\MafClaw.Sample41.csproj -- --fixture --self-test
dotnet run --project .\samples\50-mcp-tools\MafClaw.Sample50.csproj -- --describe
dotnet run --project .\samples\51-mcp-tools-agent\MafClaw.Sample51.csproj -- --fixture
dotnet run --project .\samples\60-session-persistence\MafClaw.Sample60.csproj -- --self-test
dotnet run --project .\samples\61-session-persistence-agent\MafClaw.Sample61.csproj -- --fixture --self-test
dotnet run --project .\samples\70-foundry-local\MafClaw.Sample70.csproj -- --describe
dotnet run --project .\samples\70-foundry-local\MafClaw.Sample70.csproj -- --live
dotnet run --project .\samples\71-foundry-local-agent\MafClaw.Sample71.csproj -- --fixture
dotnet run --project .\samples\71-foundry-local-agent\MafClaw.Sample71.csproj -- --live
```

The deliberate Sample 30 regression returns exit 1. That is successful
detection of a bad candidate, not a broken test runner. The verifier records
the expected exit separately.

## 10 / 11: follow one operation

Sample 10 creates two real nested spans and one counter measurement around the
fixed portfolio calculation. No model or OpenTelemetry collector is required.
Sample 11 adds the actual MAF tool loop and console/optional OTLP export.
`--fixture` scripts inference while still executing the real valuation tool.
`--live` uses the configured Foundry model and can incur charges. Unknown token
usage is not reported as a measured zero.

## 20 / 21: intention is not permission

Sample 20 allows one mock action and denies a restricted marker before the
action count changes. It is an application teaching rule, not enterprise DLP.
Sample 21 displays an actual model-proposed simulated trade and checks that the
trade ledger is empty before approval. The fixture denies it deterministically;
`--live` asks the user for an explicit decision. No real trade is possible.

## 30 / 31: prove the gate fails

Sample 30 checks the exact 27124.95 total and 66.01 percent allocation. The
regression flag changes the candidate total to 1.00 so the gate must reject it.
Sample 31 grades the MAF conversation, including the actual tool result. Live
inference plus local grading is still a chargeable model path.

## 40 / 41: local protocol is not cloud deployment

The self-test modes bind an ephemeral loopback port, issue a real HTTP request,
validate the response and stop the app. Without `--self-test` they run as local
servers. Sample 41 without `--fixture` uses local Azure CLI authentication for
development; the production `code\Hosted` app uses managed identity instead.
Neither local server should be exposed as an authenticated public service.

## 22 / 32: conditional services

```powershell
dotnet run --project .\samples\22-purview-agent\MafClaw.Sample22.csproj -- --describe
dotnet run --project .\samples\32-foundry-evaluations\MafClaw.Sample32.csproj -- --describe
```

These description commands return exit 2 and do not contact a service. They
are intentionally not called successful Purview/evaluation runs. After the
documented licensing, configuration, permissions and budget gates, use `--live`.
See [governance](../docs/governance.md) and [evaluations](../docs/evaluations.md).

## 50 / 51: MCP tools, raw client then two agents

Sample 50 uses the MCP C# SDK directly against the public Microsoft Learn MCP
server (`https://learn.microsoft.com/api/mcp`): real handshake, real
`ListToolsAsync`, one real `CallToolAsync`. No model, no agent. `--describe`
is offline (exit 2); `--live` makes the real call.

Sample 51 builds two MAF agents that answer the same question:
`WebSearchAgent` uses the framework's built-in `HostedWebSearchTool`;
`MicrosoftLearnAgent` uses real Microsoft Learn MCP tools wired in as
`AITool`s. `HostedWebSearchTool` runs server-side during a live model call, so
`--fixture` shows a labeled scripted stand-in for that agent only; the MCP
agent's tool call is real even in `--fixture`. See
[MCP tools](../docs/mcp-tools.md).

## 60 / 61: session persistence, transcript then `AgentSession`

Sample 60 keeps an explicit `List<Turn>` and serializes it to JSON after every
turn; `--resume <path>` reloads it; `--self-test` proves a reloaded transcript
keeps counting after a simulated restart. No model.

Sample 61 uses the framework's own `AgentSession` persistence
(`SerializeSessionAsync` / `DeserializeSessionAsync`). `--fixture --self-test`
wraps the chat client to record the exact messages resent to the model after
restore, proving the restored session — not the scripted answer — carries the
prior turns forward. `--live --save`/`--resume` exercise the same API against
a real Foundry model. See [session persistence](../docs/session-persistence.md).

## 70 / 71: on-device inference with Foundry Local

Sample 70 starts (or attaches to) the local **Foundry Local** runtime, resolves
`phi-4-mini` from its catalog, downloads/loads it if needed, starts its
local OpenAI-compatible web service on an ephemeral loopback port, then calls
it with the real, official `OpenAI` NuGet SDK. `--describe` is offline (exit
2, no download); `--live` runs entirely on this machine — no cloud endpoint,
no Foundry project, no API key. Requires the
[Foundry Local runtime](https://aka.ms/foundry-local) to be installed
(`winget install Microsoft.FoundryLocal`); the first `--live` run downloads
the model (~3.6 GB) once.

Sample 71 bridges that same on-device model into the shared MAF harness:
`openAiClient.GetChatClient(model.Id).AsIChatClient()` (via
`Microsoft.Extensions.AI.OpenAI`) plugs Foundry Local straight into
`AsHarnessAgent`, so it drives a real local `get_local_time` tool exactly like
any cloud-backed agent elsewhere in this session. `--fixture` proves the
harness/tool wiring deterministically (no Foundry Local dependency at all);
`--live` forces the tool call (`ChatToolMode.RequireSpecific`) because small
on-device models can otherwise narrate a tool call in plain text instead of
actually issuing one. See [Foundry Local](../docs/foundry-local.md).
