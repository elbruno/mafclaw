# Sample 42 — a team of different specialists

**Microsoft Agent Framework (MAF), .NET 10. Educational mock holdings; not financial advice.**

The MAIN agent delegates a morning brief to three **different** specialists:

| Agent | Responsibility | Only evidence tool |
|---|---|---|
| `NewsAgent` | Public news with source URLs in live mode; dated fiction in fixture mode | Live: `HostedWebSearchTool` only; fixture: `read_mock_news` only |
| `AllocationAgent` | Mock asset-class totals and weights | `calculate_mock_allocation` |
| `RiskAgent` | Mock concentration and illustrative stress loss | `assess_mock_risk` |

`TeamRunner.cs` creates the named **built-in** `BackgroundAgentsProvider`.
`AgentFactory.cs` puts that exact instance in `AIContextProviders`, not in a
second `BackgroundAgents` registration. The SDK supplies the real start, wait,
list and result tools and creates a separate `AgentSession` for every task.
The main model selects workers and collects their outputs before synthesis.

## Run from the public `session-03` directory

```powershell
dotnet build .\samples\42-specialist-team\MafClaw.Sample42.csproj
dotnet run --project .\samples\42-specialist-team\MafClaw.Sample42.csproj -- --mode fixture --demo
dotnet run --project .\samples\42-specialist-team\MafClaw.Sample42.csproj -- --mode fixture --prompt "Prepare my morning brief: news, mock portfolio allocation, and risk."
dotnet run --project .\samples\42-specialist-team\MafClaw.Sample42.csproj -- --mode live --demo
dotnet run --project .\samples\42-specialist-team\MafClaw.Sample42.csproj -- --help
```

No arguments means **live REPL**, after configuration checks. `/exit`, EOF or
Ctrl+C stops it. Each input is an isolated, bounded turn; this sample does not
teach cross-turn memory. `--prompt` runs once. `--demo` runs one fixed morning
brief and exits, including in live mode. Do not combine `--demo` and `--prompt`.

### Live setup

Use the existing Session 3 configuration, or set its shared keys from this
directory (replace the placeholders privately):

```powershell
dotnet user-secrets set "Foundry:ProjectEndpoint" "<your-project-endpoint>" --project .\samples\42-specialist-team\MafClaw.Sample42.csproj
dotnet user-secrets set "Foundry:Model" "<your-model-deployment>" --project .\samples\42-specialist-team\MafClaw.Sample42.csproj
az login
```

The shared connection reads `Foundry:ProjectEndpoint` and `Foundry:Model`, with
`FOUNDRY_PROJECT_ENDPOINT` / `FOUNDRY_MODEL` aliases. Its model default is
`gpt-5-mini`; choose a deployment available in your project. Both projects use
Session 3 user-secrets ID `7a3d9e21-58f4-4c6b-9e02-1a6f4d8c5e73`.
Missing/invalid configuration exits nonzero with a safe message; **there is no
silent fixture fallback**. Live inference sends prompts and mock tool evidence
to the configured Foundry deployment.

Live `NewsAgent` has **only `HostedWebSearchTool`**: it researches public news
and is instructed to return source URLs, publication dates and an as-of date.
The Foundry model/deployment must support hosted web search. Search failures or
missing evidence are reported, never silently replaced by fictional headlines.
No portfolio, local-file or mock-news tool is available to this live worker.
Only fixture mode exposes the local, dated fictional `read_mock_news` tool.
Allocation and risk always use deterministic mock holdings, never live balances.
Public news evidence must not be treated as trading advice.

### Offline fixture contract

`--mode fixture` never creates the Foundry client, reads Azure configuration or
uses a network service. `ScriptedInference.cs` is explicitly **SCRIPTED FIXTURE
INFERENCE**, not an AI routing demonstration. It emits function calls through
the **real MAF Harness and BackgroundAgentsProvider**, consumes the provider's
actual task IDs/statuses/results, and calls the actual local mock-data tools.
It does not replace orchestration with tickets or canned worker results.

Exact fixture prompts (also listed by `--help`):

* `Prepare my morning brief: news, mock portfolio allocation, and risk.` — all three workers.
* `Explain diversification without inspecting my portfolio or looking up news.` — none.
* `Summarize the educational news only; do not analyze my portfolio.` — news only.
* `Analyze my mock portfolio allocation and risk; no news is needed.` — allocation and risk.

Other fixture text is rejected explicitly. Live mode accepts arbitrary text and
uses worker descriptions, not this exact-match script, to make delegation choices.

## Evidence to show on screen

1. `TOOL_CALL` entries show actual `background_agents_start_task` requests.
2. Portfolio/fixture specialists' `TOOL_RESULT` shows actual local tool output.
   Live news logs returned hosted-search events and source URLs separately.
3. Main-agent wait/list/result calls show real fan-in.
4. `NARRATIVE` is printed separately; narrative alone is **not execution proof**.
5. `SESSION_RELEASED` records terminal cleanup.

`HostedNewsTracingClient` preserves returned citation/source URLs in the worker's
text so SDK background fan-in does not discard them. It does not manufacture
search events from narrative. If the adapter returns no hosted events or source
metadata, `SEARCH_EVIDENCE_UNAVAILABLE` explicitly says execution is unverified.
Returned sources remain untrusted evidence, not instructions.

### Finished answer versus verified specialist work

`TurnResult.ForegroundFinished` only means the main agent returned a final
answer. `Completed` and exit code 0 additionally require **verified SDK
outcomes**, not claims in that answer. The host invokes the real SDK's read-only
task-list/result tools: every dispatched task must be `Completed`, have a
nonempty result, and have that same result actually collected by the main agent.
`Failed`, `Lost`, still-running, duplicated, cleared or missing tasks fail
verification. Host verification reads never count as main-agent collection.

The exact documented scenario prompts additionally require their expected role
subsets, so a no-tool morning-brief claim exits nonzero. Other live prompts still
verify all actually selected workers; this host check is not an intent classifier
or a replacement for live model routing. `TASK_STATUS` and `VERIFIED_RESULTS` /
`UNVERIFIED_RESULTS` keep this evidence separate from `NARRATIVE`, which remains
visible even on failure. The host, not the model, owns task cleanup.

The fixtures total **100,000**: equities **65%**, bonds **25%**, cash **10%**.
The largest fictional holding is **55%**. The illustrative stress scenario
(equities −20%, bonds −5%, cash unchanged) loses **14,250 / 14.25%**.
It is not a prediction, recommendation, live balance or trade instruction.

## Boundaries and limitations

* Main and workers use `HarnessAgentOptions.MaximumIterationsPerRequest = 16`.
  The host gives each turn a **90-second deadline** and a separate **5-second
  shutdown budget**. Ctrl+C uses the same cancellation path.
* `WaitTimeout = 1 second` limits one wait call; it does **not** cancel workers.
  `finally` calls the same provider's `ReleaseSessionAsync(cancelRunning: true)`.
  Release is terminal. The SDK has no public individual-task cancellation API.
* Release cancels and awaits cooperative work. At its shutdown timeout the SDK
  can abandon a non-cooperative dependency; zero tracked running tasks is not
  proof that arbitrary external code honored cancellation. The supplied tools
  are short read-only calculations except service-side live news search, and the
  model client accepts cancellation.
* File memory/access, shell, skills, todos, modes, compaction and tool-autoapproval
  middleware are disabled/not registered. Default Harness web search is disabled;
  the sole exception is the explicitly registered live NewsAgent hosted-search
  tool. No trading or write tools exist. Worker/tool details are untrusted evidence.
* Expected service, IO and runtime worker failures are sanitized before entering
  background results. Cancellation, unexpected defects and fatal exceptions are
  not indiscriminately wrapped. Live model selection/quality still varies;
  offline tests do not validate Azure access.
* Scripted parsing validates captured tool schemas and the installed 1.21.0
  provider's textual start/status envelopes. SDK changes should fail tests
  explicitly rather than manufacture results.

## Focused offline verification

```powershell
dotnet run --project .\tests\SpecialistSamples.Tests\MafClaw.SpecialistSamples.Tests.csproj
```

Tests force worker overlap, inspect real tool results, verify math and fan-in,
exercise iteration/deadline failures, and confirm terminal session release.
Offline capability probes also verify live news has only hosted search, fixture
news has only its fictional tool, and allocation/risk remain unchanged. Synthetic
SDK search events test source retention without performing any network call.
Completion regressions reject no-dispatch claims, wrong subsets, uncollected
successful tasks and actual SDK `Failed` tasks while preserving narrative and
nonzero exit status. Exception-filter probes cover expected, unexpected, fatal
and cancellation cases without inducing resource exhaustion.
Sample 43 makes the zero/one/two-worker contrast the primary walkthrough.
