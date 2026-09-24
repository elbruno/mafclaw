# Foundry Local: one command per demo

Samples 70/71 run on Windows x64. From each sample directory:

```powershell
dotnet run
```

No Azure login, Foundry project, API key, `--live` flag, REST server, or manually
selected port is needed. The NuGet packages supply the native SDK assets.
Model acquisition needs internet access; inference and the clock tool run
in-process on the demo machine. Pre-cache by running both samples before
streaming. A first run can take several minutes.

## 70: raw native SDK, no MAF

The sample uses `Microsoft.AI.Foundry.Local` 2.0.1:

1. `FoundryLocalManager.CreateAsync` initializes the runtime and catalog.
2. `GetModelAsync("qwen2.5-0.5b")` selects the small model (about 528 MB).
3. Download if missing, then load it.
4. `new ChatSession(model)` and `ProcessRequestAsync` produce a real response.
5. Dispose the session and unload the model.

This uses the current session API, not the obsolete `GetChatClientAsync` path.
It checks for a nonempty local response, not factual answer quality.

## 71: ElBruno's adapter into MAF/Harness

The entry point visibly constructs
`FoundryLocalModelLifecycleService` and `FoundryLocalChatClientAdapter`
from [`ElBruno.MAF.FoundryLocal.Adapter`](https://github.com/elbruno/ElBruno.MAF.FoundryLocal)
0.2.1. This is a community adapter, not an official MAF provider.

The adapter supplies `IChatClient`, model download/load/unload and native
message mapping. `AsHarnessAgent` supplies tool dispatch and conversation
management; the ordinary `get_local_time` C# function reads the real clock.
`qwen2.5-1.5b` (about 1.3 GB) is selected for the tool demonstration.

**Compatibility matters:** keep the adapter's declared native SDK dependency
(1.2.1) rather than overriding it with SDK 2.0. The overridden combination
produced tool execution followed by an answer that ignored the actual result.
The sample's live PASS requires both a real tool call and an answer containing
the actual returned time. Do not weaken that check to accept hallucinated prose.
Sample 70 and 71 are independent processes and can use their respective SDKs.

Both use the adapter's cache app name, `ElBruno_MAF_FoundryLocal`, but select
different models. The larger model's download is separate from the small one.

## Off-air verification and troubleshooting

- Sample 70 `--describe` returns 2 without initializing the runtime.
- Sample 71 `--fixture` exercises the real Harness and clock tool without
  starting/downloading a local model. It does not claim live answer quality.
- A missing catalog entry, runtime error, download failure or inaccurate live
  tool answer is a failure; there is no automatic cloud or fixture fallback.
- Let the models finish downloading before going on air. Use synthetic inputs;
  local inference is not a guarantee of factual accuracy.
