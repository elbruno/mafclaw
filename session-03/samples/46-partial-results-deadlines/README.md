# Sample 46 — Honest partial results and application deadlines

**Microsoft Agent Framework (MAF), .NET 10.** Independent named research workers produce typed outcomes. A failure or deadline cannot erase a sibling's successful result, and a model narrative cannot overwrite the final host report. All data is fictional and read-only. **Not financial advice.**

## Run

Run from the public repository's `session-03` directory. In the planning checkout, the equivalent working directory is `public-staging\session-03`.

```powershell
dotnet run --project .\samples\46-partial-results-deadlines\MafClaw.Sample46.csproj -- --help
dotnet run --project .\samples\46-partial-results-deadlines\MafClaw.Sample46.csproj -- --mode fixture --demo
dotnet run --project .\samples\46-partial-results-deadlines\MafClaw.Sample46.csproj -- --mode fixture --prompt "Gather fictional research and report partial results."
dotnet run --project .\samples\46-partial-results-deadlines\MafClaw.Sample46.csproj
```

No arguments means **live model, interactive REPL**. Use existing Session 03 Foundry setup; `OrchestrationSupport` owns configuration/client creation. `/exit` and EOF initiate safe shutdown. `--prompt` runs exactly one foreground turn; `--demo` runs a fixed showcase and exits. These options are mutually exclusive. Each main-agent turn has up to four tool iterations, one admitted research batch and a 4,000-character prompt limit. The cooperative foreground cancellation budget is **60 seconds in live mode**; fixture mode retains its 30-second safety budget without adding a timing wait.

`--mode fixture` is explicitly **scripted**, with no Azure/configuration/network access. Scripted clients still run through real MAF function dispatch and worker inference. It proves host mechanics, not autonomous planning by a live model.

## Deterministic showcase

| Named worker | Fixture behavior | Honest report |
|---|---|---|
| `NewsResearchAgent` | Actual MAF inference waits until the slow worker has also entered, then returns fictional news. | `completed`, 1 attempt, preserved result |
| `UnavailableResearchAgent` | An explicit local data-adapter failure occurs before inference. | `failed`, 1 attempt, no result |
| `SlowHoldingsResearchAgent` | Actual MAF inference remains blocked and observes its owned cancellation token. | `timed-out`, 1 attempt, no fabricated result |

The fixture deadline is **manually advanced**, not an elapsed wait: it fires only after success and failure reports were observed and both inference calls overlapped. Fixture mode does not use the live timing delays. No timing sleeps determine success. The actual measured elapsed times are printed and may vary.

Live mode uses an application **20-second batch deadline, including queue time**, and a **40-second cancellable local slow-source delay**. `LiveTiming` keeps these limits and the 60-second cooperative foreground budget together. This leaves teaching room for a real news response while the deliberately slow adapter misses the deadline. The unavailable-source failure and slow-source delay are explicitly injected educational conditions, not claims about Foundry reliability. In live mode the slow source can expire before its MAF inference starts. A news response is still not guaranteed: real model/transport latency can exceed the budget, and the host reports that honestly rather than falling back to scripted success.

## Where policy lives

- `SampleApplication`: minimal `AsAIAgent` workers/main from `Microsoft.Agents.AI`; `FunctionInvokingChatClient` supplies bounded tool dispatch. No unrelated Harness tools or approval machinery.
- `MafResearchWorker`: a new `AgentSession` per attempt; no overlapping runs on a conversation. MAF supplies inference/history, not the application's lifecycle policy.
- `PartialResearchTools`: ordinary `AIFunction` named `gather_research`. This is a **custom host wrapper**, not the built-in `background_agents_*` provider. Duplicate calls within a turn reuse its batch/report.
- `DeadlineRunner`: at most **2 executing workers globally**, at most 8 independently named operations per batch, and **at most 2 host attempts per operation**. Only the explicit `RetryableResearchException` classification permits retry. Expected permanent errors, arbitrary cloud errors and empty responses do not. Host attempt counts describe calls into the adapter/MAF worker, not internal HTTP transport retries.
- `WorkerOutcome`: worker name, state, attempts, elapsed report time, safe detail, and a result only on observed success.
- `ReportRenderer`: runs **after** the main narrative and prints authoritative typed outcomes. `PARTIAL / INCOMPLETE` is not upgraded because a model says everything worked.

`completed`, `failed` and `cancelled` describe observed execution outcomes. `timed-out` means the **application stopped waiting**; execution might still run. `cancellation-requested` distinguishes caller/shutdown cancellation from a deadline when execution has not yet been observed. There is no claim that an SDK wait timeout cancels work or a public per-task `BackgroundAgentsProvider` cancellation API exists.

## Late work and cleanup

Deadline return does not await the worker or its cancellation callbacks. All late execution tasks remain owned and their failures are observed. A late success/fault does **not** retroactively rewrite the earlier report. Cancellation sources and shared clients remain alive until their users finish.

On `/exit`, EOF, one-turn completion or an error, shutdown stops admission, requests cancellation, joins reporting tasks, then joins executions and cancellation callbacks before disposing clients. **The report wait is bounded, but shutdown is deliberately not a hard process-kill deadline.** A truly noncooperative worker/callback can delay shutdown indefinitely. This is the safe lifetime boundary, not abandonment disguised as cancellation. Foreground cancellation is likewise cooperative; no forcible SDK abort is claimed.

Jobs and results are **in-memory only, not restart-durable**. Nothing here authorizes a trade, file mutation or external action; no model-generated approval is used.

## Offline regression checks

```powershell
dotnet build .\samples\46-partial-results-deadlines\MafClaw.Sample46.csproj
dotnet run --project .\tests\LifecycleSamples.Tests\MafClaw.LifecycleSamples.Tests.csproj
```

Checks include success preservation, a known transient succeeding on attempt two, exhausted retry budget, permanent/unclassified failures without retries, empty-result rejection, queued timeouts, cancellation vs deadlines, a deliberately noncooperative gated worker with a late fault, dependency lifetime, callback failures and real MAF routing against a deliberately false model success narrative. The watchdog limits are test-failure guards; gates establish the ordering. No live credentials are needed.
