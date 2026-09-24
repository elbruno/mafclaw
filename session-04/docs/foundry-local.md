# Foundry Local

Samples 70/71 teach on-device inference with **Foundry Local**: no cloud
endpoint, no Foundry project, no API key. Everything — the model, the tool
call, and the answer — stays on the machine running the sample.

## Prerequisite: the Foundry Local runtime

Both `--live` modes need the native Foundry Local runtime installed once per
machine:

```powershell
winget install --id Microsoft.FoundryLocal --silent --accept-package-agreements --accept-source-agreements
foundry --version
```

`--describe` (70) and `--fixture` (71) do **not** need the runtime installed;
they either make no calls at all or use a fully scripted `IChatClient`.

## 70: the raw on-device client, no agent

`MafClaw.Sample70` uses the official
[`Microsoft.AI.Foundry.Local`](https://www.nuget.org/packages/Microsoft.AI.Foundry.Local)
SDK directly:

1. `FoundryLocalManager.CreateAsync` starts (or attaches to) the local runtime.
2. `manager.GetCatalogAsync()` → `catalog.GetModelAsync("phi-4-mini")` resolves
   the model; `model.DownloadAsync`/`LoadAsync` fetch and load it if needed
   (first run only — about 3.6 GB, cached afterward).
3. `manager.StartWebServiceAsync()` starts the model's local
   OpenAI-compatible REST endpoint on an ephemeral loopback port.
4. The real, official `OpenAI` NuGet SDK (`OpenAIClient`/`GetChatClient`) talks
   to that endpoint exactly as it would talk to any OpenAI-compatible server.

```powershell
dotnet run --project .\samples\70-foundry-local\MafClaw.Sample70.csproj -- --describe
dotnet run --project .\samples\70-foundry-local\MafClaw.Sample70.csproj -- --live
```

`--describe` prints the planned steps and makes no network/runtime call
(exit 2). `--live` runs a real on-device chat completion (exit 0 on success).

## 71: the same on-device model, inside a MAF agent

`MafClaw.Sample71` bridges Foundry Local's OpenAI-compatible client into a
real MAF harness agent:

```csharp
var chatClient = openAiClient.GetChatClient(model.Id).AsIChatClient();
var agent = chatClient.AsHarnessAgent(new HarnessAgentOptions { ... });
```

`.AsIChatClient()` is the official `Microsoft.Extensions.AI.OpenAI` bridge
from a plain `OpenAI.Chat.ChatClient` to `Microsoft.Extensions.AI.IChatClient`
— it saves us from writing any adapter code, and it is the same extension
point every cloud-backed sample in this session uses.

```powershell
dotnet run --project .\samples\71-foundry-local-agent\MafClaw.Sample71.csproj -- --fixture
dotnet run --project .\samples\71-foundry-local-agent\MafClaw.Sample71.csproj -- --live
```

`--fixture` never talks to Foundry Local; a `ScriptedChatClient` proves the
harness calls the real local `get_local_time` tool and folds its real result
into a scripted final answer. `--live` starts Foundry Local for real and asks
`phi-4-mini` a question that requires the same tool.

### Forcing the tool call

Small on-device models can be technically "tool-calling capable" per the
catalog listing and still, left to `ChatToolMode.Auto`, narrate a tool call in
plain prose (`"[Calling the get_local_time tool...]"`) instead of emitting a
real `tool_calls` payload. Sample 71 sets

```csharp
ToolMode = ChatToolMode.RequireSpecific("get_local_time")
```

which forces the model to actually invoke the function through the
OpenAI-compatible protocol. The sample's pass/fail check looks for a real
`FunctionResultContent` in the response — not just non-empty text — so a
model that only narrates the call is correctly reported as a failure.

## Notes for presenters

- No secrets are required for either sample — Foundry Local is fully local,
  so `configure-user-secrets.ps1` has nothing to configure here.
- Each sample uses its own `Configuration.AppName`
  (`MafClaw.Sample70`/`MafClaw.Sample71`), and Foundry Local scopes its model
  cache per `AppName`. Running both samples' first `--live` downloads the
  model twice. This is expected, not a bug — it keeps each sample
  independently runnable, matching the rest of the session's sample pairs.
- Cleanup (`model.UnloadAsync`/`StopWebServiceAsync`) is best-effort: the
  native runtime can briefly report a model session as still in use while an
  HTTP keep-alive connection winds down. Both samples log that as a
  non-fatal warning rather than let it override a run that already passed.
- `phi-4-mini` (~3.6 GB, NPU-targeted, tool-calling capable) is the teaching
  model. `foundry model list` shows smaller alternatives (for example
  `qwen2.5-0.5b`, ~528 MB) if download time matters more than tool-calling
  fidelity.

All examples are mock and educational, not financial advice.
