# Sample 45 — Responsive, host-owned background jobs

**Microsoft Agent Framework (MAF), .NET 10.** This is a new teaching variant; Samples 40/41 are unchanged. All news and holdings are fictional, read-only educational data. **Not financial advice.**

## Run

Run from the public repository's `session-03` directory. In the planning checkout, the equivalent working directory is `public-staging\session-03`.

```powershell
dotnet run --project .\samples\45-responsive-background-jobs\MafClaw.Sample45.csproj -- --help
dotnet run --project .\samples\45-responsive-background-jobs\MafClaw.Sample45.csproj -- --mode fixture --demo
dotnet run --project .\samples\45-responsive-background-jobs\MafClaw.Sample45.csproj -- --mode fixture --prompt "Start research on fictional ACME."
dotnet run --project .\samples\45-responsive-background-jobs\MafClaw.Sample45.csproj
```

No arguments means **live model, interactive REPL**. `--mode live` is equivalent. Live mode uses the existing Session 03 Foundry connection and user-secrets setup through `OrchestrationSupport`; this sample does not change settings. No hosted web search is installed: live inference still uses only the local fictional fixture.

`--mode fixture` loads **no Azure configuration and makes no network calls**. Main-agent decisions and worker inference are explicitly scripted `FixtureChatClient` responses, but **MAF actually executes the tool protocol and worker `RunAsync` calls**. This is not evidence of autonomous live-model planning.

`--prompt <text>` runs one foreground turn, shows host status, then cancels/drains remaining jobs. `--demo` runs a fixed showcase and exits; it cannot be combined with `--prompt`. Prompts are limited to 4,000 characters, main tool invocation to four iterations, and foreground turns carry a cooperative 30-second cancellation budget.

## What to show

The scripted demo:

1. The main MAF agent calls `start_research` twice for `NewsResearchAgent` and `HoldingsResearchAgent`.
2. Both actual worker inference calls enter separate gates. **Neither is released yet.**
3. `/jobs` shows running jobs, `/collect job-001` says pending, and the main agent answers another question.
4. `/cancel job-002` records intent. Only the worker's observed cancellation makes it `cancelled`.
5. Release the news gate, collect its real scripted result, and show both final states.

No sleep duration establishes ordering. `TaskCompletionSource` barriers establish actual overlap. The regression suite additionally proves the REPL **requests its next input line** while both workers remain blocked.

Interactive commands:

| Input | Host behavior |
|---|---|
| `Start research on fictional ACME using both named agents.` | Main may invoke the start tool; tool returns immediately with a job ID. |
| `/jobs` | Snapshot authoritative states without asking a model. |
| `/collect job-001` | Pending before completion; observed result after success; explicit failure/cancellation otherwise. Repeated collection is allowed. |
| `/cancel job-002` | Request cancellation without awaiting the worker. |
| Another question | Run the next main-agent turn while background jobs continue. |
| `/exit` or EOF | Stop admission, request cancellation, join executions and cancellation callbacks, then dispose clients. |

Unknown job IDs produce explicit errors. Main-agent turns are **serial**, not concurrent; commands are read between foreground turns, not while a foreground model request itself is stalled.

## Code map and guarantees

- `SampleApplication`: `AsAIAgent` from `Microsoft.Agents.AI` supplies inference/session handling. `FunctionInvokingChatClient` from `Microsoft.Extensions.AI` supplies bounded tool dispatch. No Harness filesystem, memory, todos, skills, web, mode or auto-approval capabilities are installed.
- `JobTools`: a **custom application tool**, not an invented `BackgroundAgentsProvider` API. Validates two allowed worker names and submits host-owned work.
- `MafResearchWorker`: creates a **fresh `AgentSession` for every job**, then passes the job-owned cancellation token to real MAF `RunAsync`. Concurrent jobs never share a conversation.
- `JobRegistry`: admits at most **8 retained jobs**, runs at most **2 workers**, retains stable `job-001` IDs and observes all task exceptions. Retention includes terminal jobs; at capacity, admission is rejected rather than silently evicting a collectable result. Restart only after collecting needed results.
- `ResponsiveConsole`: never waits for background completion to list, collect, cancel or read another question.

State transitions:

```text
queued -> running -> completed | failed
queued | running -> cancellation-requested -> cancelled (only after observation)
cancellation-requested -> completed | failed (worker ignored the request and returned/faulted)
```

Cancellation callback failure is observed but is **not** proof the worker stopped. A normal return after a cancellation request is honestly reported as completed, not cancelled. Locks protect short state changes; no lock crosses an `await`.

**Lifetime boundary:** jobs are in-memory and **not durable across process restart**. Exit drains before disposing shared clients. A worker or cancellation callback that never returns can therefore delay shutdown; this sample does not kill processes, abandon SDK work, or claim cancellation is guaranteed. The 30-second foreground budget is also cooperative, not a promise to forcibly abort a noncooperative service. For durable/restartable work, a production host needs external job storage and a separately designed execution lifetime.

## Verify offline

```powershell
dotnet build .\samples\45-responsive-background-jobs\MafClaw.Sample45.csproj
dotnet run --project .\tests\LifecycleSamples.Tests\MafClaw.LifecycleSamples.Tests.csproj
```

Tests cover actual MAF dispatch, responsive reads, session isolation, deterministic IDs, capacity/queueing, unknown IDs, pending/repeat collection, cooperative and ignored cancellation, completion races, callback errors, EOF and drain-before-disposal. No service credentials are needed. If the live connection fails, check the existing Session 03 setup privately; errors must not reveal endpoint/tenant details.
