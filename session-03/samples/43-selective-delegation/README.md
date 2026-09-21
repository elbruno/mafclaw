# Sample 43 — selective delegation

**Microsoft Agent Framework (MAF), .NET 10. Educational mock holdings; not financial advice.**

The MAIN agent should not run every specialist for every question:

| Question | Required workers | Why |
|---|---|---|
| Explain diversification | **None** | No data lookup is needed |
| News summary only | **NewsAgent** | Allocation/risk would be unnecessary |
| Analyze the mock portfolio | **AllocationAgent + RiskAgent** | Composition and risk, not news |
| Full morning brief | **All three** | The user requested all three responsibilities |

In **live mode the main model chooses** from worker descriptions in
`AgentFactory.cs`. There is no live keyword-based routing in the host.
`TeamRunner.cs` registers available workers, not a preselected dispatch subset.

## Run from the public `session-03` directory

```powershell
dotnet build .\samples\43-selective-delegation\MafClaw.Sample43.csproj
dotnet run --project .\samples\43-selective-delegation\MafClaw.Sample43.csproj -- --mode fixture --demo
dotnet run --project .\samples\43-selective-delegation\MafClaw.Sample43.csproj -- --mode fixture --prompt "Analyze my mock portfolio allocation and risk; no news is needed."
dotnet run --project .\samples\43-selective-delegation\MafClaw.Sample43.csproj -- --mode live --demo
dotnet run --project .\samples\43-selective-delegation\MafClaw.Sample43.csproj -- --help
```

`--demo` runs three fixed prompts—explanation, news, portfolio—and exits in
**both** modes. `--prompt <text>` runs one bounded turn and exits. These options
are mutually exclusive. No arguments defaults to a **live REPL after
configuration checks**; `/exit`, EOF or Ctrl+C stops it. Every request starts an
isolated turn, so no previous worker selection leaks into the next request.

### Live configuration

Reuse the existing Session 3 settings or replace the placeholders privately:

```powershell
dotnet user-secrets set "Foundry:ProjectEndpoint" "<your-project-endpoint>" --project .\samples\43-selective-delegation\MafClaw.Sample43.csproj
dotnet user-secrets set "Foundry:Model" "<your-model-deployment>" --project .\samples\43-selective-delegation\MafClaw.Sample43.csproj
az login
```

The shared support uses the existing `Foundry:ProjectEndpoint` / `Foundry:Model`
keys and `FOUNDRY_PROJECT_ENDPOINT` / `FOUNDRY_MODEL` aliases. Its model default
is `gpt-5-mini`. The project uses the existing Session 3 user-secrets ID
`7a3d9e21-58f4-4c6b-9e02-1a6f4d8c5e73`. Missing/invalid configuration produces
a safe nonzero failure, **never a silent offline fallback**. Live prompts and
mock tool evidence are sent to the configured Foundry deployment.

Live `NewsAgent` uses **only `HostedWebSearchTool`** to research public news,
returning source URLs, publication dates and an as-of date. It has no mock-news
or portfolio tool. Use a Foundry deployment supporting hosted web search;
failures are explicit, with no silent fictional-news fallback. Fixture news is
always clearly labelled dated fiction and never registers hosted search.
Allocation and risk always calculate over local mock holdings; no live account,
price-feed or trade tool is available.

### Explicitly scripted offline mode

Fixture inference is intentionally scripted and prominently labelled
**SCRIPTED FIXTURE INFERENCE**. It is a deterministic protocol regression, **not
proof that a live model made a routing decision**. Only inference is replaced:
the actual MAF Harness invokes the real SDK background tools and real
read-only mock-data tools. Task identifiers, statuses and collected results
come from the provider, not a fake queue.

Exact supported fixture prompts:

```text
Explain diversification without inspecting my portfolio or looking up news.
Summarize the educational news only; do not analyze my portfolio.
Analyze my mock portfolio allocation and risk; no news is needed.
Prepare my morning brief: news, mock portfolio allocation, and risk.
```

Unlisted fixture text exits nonzero; arbitrary text is accepted in live mode.
Fixture mode does not create the Foundry client, read Azure settings or make
network calls.

## Follow the evidence, not the story

`TOOL_CALL` and `TOOL_RESULT` are actual model/function protocol events, separate
from `NARRATIVE`. In the demo, count SDK start calls: **0 → 1 → 2**. Check the
corresponding result collections, and ensure no unused specialist has tool
events. `SESSION_RELEASED` appears after each isolated turn.

The three agents have separate descriptions, sessions and tool allowlists:

* Live `NewsAgent`: only `HostedWebSearchTool`, public news with source URLs and dates.
* Fixture `NewsAgent`: only `read_mock_news`, dated fictional classroom headlines.
* `AllocationAgent`: `calculate_mock_allocation`, decimal arithmetic over mock holdings.
* `RiskAgent`: `assess_mock_risk`, deterministic concentration and stress math.

`HostedNewsTracingClient` logs actual returned hosted-search events and source
metadata separately. Returned citation URLs are retained in the worker's text
for SDK fan-in. If no hosted events or source metadata are available,
`SEARCH_EVIDENCE_UNAVAILABLE` reports that execution is unverified; text alone
never creates a fake search event.

### Completion is verified, not narrated

`ForegroundFinished` records that the main agent returned a final answer.
`Completed` / exit code 0 require more: the host reads the actual SDK task list,
rejects every status except `Completed` (including `Failed` and `Lost`), and
matches each nonempty successful result against the main agent's observed
result-collection traffic. Missing, duplicate or prematurely cleared task
evidence fails verification. Host verification reads do not replace main-agent
fan-in.

The documented exact prompts must also match their expected zero/one/two/three
role subsets. This validates outcomes, not live dispatch decisions. For other
live prompts, every actually selected worker is still checked; the host does
not classify arbitrary natural-language intent. Failed checks preserve
`NARRATIVE`, emit `UNVERIFIED_RESULTS`, return nonzero, and release sessions.
Successful checks emit `VERIFIED_RESULTS`; `TASK_STATUS` shows actual SDK states.

Mock holdings total **100,000**, allocated **65% equities / 25% bonds / 10% cash**.
Largest holding: **55%**. Stress loss: **14,250 / 14.25%**, assuming equities
−20%, bonds −5%, cash unchanged. These are illustrative computations, not
forecasts, recommendations or financial advice.

## Provider ownership and safety

The named **global SDK `Microsoft.Agents.AI.BackgroundAgentsProvider`** is
registered once via `AIContextProviders`. It owns real background task state
and child `AgentSession`s. Do not additionally set
`HarnessAgentOptions.BackgroundAgents`; that would duplicate registration.
The host retains the exact provider and releases it in `finally`.

* Each agent is capped at **16 function-invocation rounds** by the MAF Harness.
  A host token sets a **90-second turn deadline**.
* The provider's **1-second wait timeout** limits only a single wait, not the
  lifetime of work. Shutdown separately calls
  `ReleaseSessionAsync(cancelRunning: true, timeout: 5 seconds)`.
* Release is terminal and cancels/awaits cooperative children. At timeout the
  SDK may abandon non-cooperative dependencies; zero tracked tasks alone does
  not prove arbitrary external code stopped. There is no SDK per-task cancel
  API. Local tools are short deterministic reads/calculations; live news search
  executes at the hosted service under the request's cancellation budget.
* No shell, file-write/access, memory, skills, todos, mode or trade tools exist.
  Default Harness web search is disabled; only live NewsAgent explicitly receives
  `HostedWebSearchTool`. Main, allocation, risk and every fixture worker lack it.
* Worker outputs and task details are untrusted advisory evidence, never new
  instructions. Expected service/IO/runtime failures receive safe diagnostics;
  cancellation, unexpected defects and fatal exceptions propagate rather than
  being indiscriminately wrapped.
* Live routing can vary. Fixture tests validate dispatch mechanics and
  boundaries, not cloud availability or live model quality.
* The fixture validates SDK-generated schemas and consumes the 1.21.0
  provider's text result envelopes. A changed SDK contract fails visibly.

## Regression checks

```powershell
dotnet run --project .\tests\SpecialistSamples.Tests\MafClaw.SpecialistSamples.Tests.csproj
```

Tests assert exact zero/one/two/three-worker subsets, no unnecessary execution,
actual specialist tools and fan-in, concurrent child execution, deterministic
math, arbitrary live-client-injected prompts, loop/deadline boundaries and
terminal cleanup. Mode-specific worker capability probes and synthetic hosted
search/citation tests exercise source retention without network calls. No Azure
configuration is needed; these probes do not claim a live search was performed.
Completion regressions cover fabricated no-dispatch success, wrong subsets,
uncollected successful tasks and actual SDK terminal failures. Narrow exception
filter tests confirm expected errors are sanitized without swallowing unexpected,
fatal or cancellation exceptions.
