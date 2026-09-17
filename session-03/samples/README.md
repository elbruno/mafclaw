# Session 03 samples

Session 03 uses the same concept-to-agent ladder as Session 02:

- `10`, `20`, `30`, and `40` isolate each primitive in plain C#.
- `11`, `21`, `31`, and `41` show the corresponding Microsoft Agent Framework
  integration surface.
- `..\code\` composes the full finance advisor for the session.

| Sample | Topic | Plain concept | MAF bridge |
|---|---|---|---|
| 10 / 11 | Skills | Discover and load local `SKILL.md` packages with bundled resources | Load the same files with `AgentSkillsProviderBuilder` and run them through a live Harness agent |
| 20 / 21 | Shell | Accept or reject a CLI request against a fixed command policy, then execute the approved specification | Give a Harness agent real shell context and an approval-gated executor; verify the resulting files in host code |
| 30 / 31 | CodeAct | Calculate portfolio value with explicit code | Let a live Harness agent read `holdings.csv` via `file_access`, then write and run Python in a Hyperlight sandbox to compute the answer |
| 40 / 41 | Background agents | Queue and observe independent research | Hand a live Harness agent a `TickerResearchAgent` via `BackgroundAgents` so it can fan research out concurrently |

Samples `10`, `20`, `30`, and `40` are offline-safe plain-C# primitives.
Samples `11`, `21`, `31`, and `41` are live Microsoft Agent Framework +
Harness demos that call a real Azure AI Foundry project:

- **11 (Skills)** — discovers the `valuation` and `risk-scoring` file-based
  skills under its own `skills/` folder via `AgentSkillsProviderBuilder`.
- **21 (Shell)** — creates a fresh mock workspace, explicitly selects
  PowerShell, and supplies `ShellEnvironmentProvider` plus an approval-gated
  `run_shell` tool. The console shows actual tool results; the host checks
  final names and unchanged SHA-256 hashes. The working directory is not
  an OS sandbox.
- **31 (CodeAct)** — reads `working/holdings.csv` through the normal
  `file_access` tools, then writes and runs Python in a Hyperlight
  micro-VM sandbox (`HyperlightCodeActProvider`, `AlwaysRequire` approval)
  to compute totals and allocations instead of doing arithmetic in prose.
- **41 (Background agents)** — registers a lean `TickerResearchAgent`
  (plain chat-client agent with only `HostedWebSearchTool`) as a
  `BackgroundAgents` entry so the claw can research multiple tickers
  concurrently and aggregate the findings.

Configure their Foundry endpoint and model with
`.\tools\configure-user-secrets.ps1 -Session 3` before running any of them.

## Run the samples

```powershell
dotnet run --project .\samples\10-skills\MafClaw.Sample10.csproj
dotnet run --project .\samples\20-confined-shell\MafClaw.Sample20.csproj
dotnet run --project .\samples\30-codeact-calculation\MafClaw.Sample30.csproj
dotnet run --project .\samples\40-background-queue\MafClaw.Sample40.csproj

# Requires Foundry credentials configured for Session 3:
.\tools\configure-user-secrets.ps1 -Session 3
dotnet run --project .\samples\11-skills-agent\MafClaw.Sample11.csproj
dotnet run --project .\samples\21-confined-shell-agent\MafClaw.Sample21.csproj
dotnet run --project .\samples\31-codeact-agent\MafClaw.Sample31.csproj
dotnet run --project .\samples\41-background-agents\MafClaw.Sample41.csproj
```

All values are mock educational data. These samples are not financial advice.

## Sample 20 - Policy before process launch

Sample 20 is plain C# and has no human approval prompt. The host's allowlist
is independent of the request; the executable and every argument must match.
From the Session 03 directory, show both paths:

```powershell
dotnet run --project .\samples\20-confined-shell\MafClaw.Sample20.csproj -- dotnet --version
dotnet run --project .\samples\20-confined-shell\MafClaw.Sample20.csproj -- dotnet --info
$LASTEXITCODE # Expected: 2 for the second, denied request.
```

Only the first request starts a child process. The default no-argument run
still performs the version check. `Process.Start` executes the matching
specification; it does not validate the allowlist. The initial working
directory is not a filesystem sandbox. On timeout, the runner terminates
the process it owns instead of only cancelling its wait.

See [Sample 20's source walkthrough and limits](20-confined-shell/README.md).
Run its offline regression checks with:

```powershell
dotnet run --project .\tests\Sample20.Tests\MafClaw.Sample20.Tests.csproj
```

## Presenter-friendly source

For Sample 21, use the [four-act walkthrough](21-confined-shell-agent/README.md):
inspect -> propose -> approve/execute -> host verification. Use the new
workspace path printed at startup; earlier runs are preserved. Every
model-proposed shell command still requires approval. `/verify` performs an
independent local check without a model call.

Each sample's C# entry point and helper begins with an objective and A/B/C
step header. Inline comments are limited to the major teaching blocks, so the
source can be used directly during an online session without hiding the
behavior behind excessive narration.

In the Microsoft Agent Framework bridge samples (11, 21, 31, and 41), comments
also identify the concrete framework type at each integration point and the
host plumbing it replaces. Present each pair in this order: first show the
plain-C# primitive, then point to the MAF type that provides the same capability
without reimplementing agent context, tool adaptation, approvals, sandbox
bridging, or background delegation.

## Additional main-agent orchestration samples

The original 40/41 comparison remains intact. These are additional MAF
variants, not renumbered replacements:

| Sample | Scenario | Main lesson |
|---|---|---|
| [42](42-specialist-team/README.md) | News, allocation, and risk specialists | Different worker roles and actual fan-out/fan-in evidence |
| [43](43-selective-delegation/README.md) | Route only the work needed | Direct answers versus selective delegation |
| [44](44-research-write-review/README.md) | Research, write, review, optionally revise | Host-enforced dependencies and a bounded feedback loop |
| [45](45-responsive-background-jobs/README.md) | Research while the conversation continues | Responsive host input, job IDs, collection, and cancellation |
| [46](46-partial-results-deadlines/README.md) | Success alongside failure and slow work | Honest partial results, deadlines, and bounded retry |
| [47](47-approved-report/README.md) | Present a report, then request permission to save | Exact-content human approval; no worker write authority |

Use `--mode live` for real Foundry inference, or explicitly select
`--mode fixture --demo` for the repeatable offline demonstration. Never
present fixture news or scripted model choices as live research.

The [orchestration guide](../docs/orchestration.md) contains all run/test
commands. Shared [OrchestrationSupport](OrchestrationSupport/README.md)
removes repeated connection/tracing plumbing; each scenario retains its own
role definitions, policies, data, and independently runnable project.
