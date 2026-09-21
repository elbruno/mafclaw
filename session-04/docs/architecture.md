# One definition, explicit host policies

`FinanceAgentFactory` builds one set of business instructions, mock tools and
named providers. `Contracts` contains plain-C# fixtures so the primitive samples
do not acquire a hidden MAF dependency.

The local profile uses `AsHarnessAgent`: planning, actual file tools, a named
`FileMemoryProvider`, skills, bounded shell/Hyperlight and background research.
The factory returns a `FinanceAgentBuild` that tracks local sessions, cancels
background work on release and disposes owned providers/clients.

| Surface | Local console | Hosted |
|---|---|---|
| Mock quote and snapshot tools | Enabled | Enabled |
| File access | `working\data` only | No model-facing local file tools |
| Memory | Fixed current-user file store; managed store opt-in | Disabled in the baseline identity profile |
| Shell | Explicit approval, 10-second execution limit, 4096-byte output limit | Disabled |
| Hyperlight | Explicit approval; no unsandboxed fallback | Disabled |
| Simulated trades | Actual MAF approval gate, in-memory result only | Tool not registered |
| Research | MAF background provider with tracked local session cleanup | Same lean worker, at most three validated tickers, awaited within the request |
| History | Harness-managed local history | Foundry Responses runtime owns history |

The hosted path uses `ChatClientAgent` with the **same factory options and
providers**, rather than adding Harness's per-service-call history persistence
on top of the Responses runtime. With the pinned SDK, duplicating those history
owners produced a failed Responses envelope even though HTTP returned 200.
The implementation preserves the SDK storage guard and explicitly sets the
OpenAI request's `StoredOutputEnabled=false`. Tests require `status=completed`;
they do not bypass the guard or merely grep for an answer inside a failed response.

Every main/worker model request has a 45-second deadline and a shared limit of
three concurrent calls. Function invocation is capped at 12 iterations.
Hosted research is request-scoped, not a durable job queue.

A working directory is not an OS sandbox. The shell denylist is only a
prefilter; the user must review the exact proposed command. Memory has its own
file-store root, separate from general file tools. The test suite attempts an
actual traversal through the memory tool and checks the tool result.

The baseline is an educational single-user local app and a restricted hosted
reference. Local HTTP is not an authenticated multi-tenant service. Production
identity/isolation is supplied and verified at the Foundry platform boundary.

The public request boundary also rejects client-supplied tools, instructions,
model routing, budget overrides and forged system/developer input. Clients send
plain user text or user-role input messages; the supported model alias is
`MafClawFinance`. Request bodies are limited to 256 KiB. The HTTP tests attempt
actual overrides and assert rejection before inference.
