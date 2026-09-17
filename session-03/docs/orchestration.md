# Main-agent orchestration: Samples 42-47

These six extensions keep Samples 40 and 41 unchanged. Sample 40 prints a
queued ticket; it does not execute research. Sample 41 registers one research
worker type for multiple ticker tasks. The extensions introduce different
specialists, routing, dependencies, interactive jobs, failure policy, and a
human-controlled side effect.

Each numbered directory is a runnable **Microsoft Agent Framework** project
targeting .NET 10. The project reference to `samples\OrchestrationSupport`
shares only connection, tracing, fixture-client, and error-reporting plumbing.
Keep that folder when copying a sample. The orchestration remains visible in
each numbered project's named C# files.

## Scenario ladder

| Sample | Main-agent responsibility | Host-owned boundary |
|---|---|---|
| [42 - Specialist team](../samples/42-specialist-team/README.md) | Delegate to news, allocation, and risk specialists; collect their findings | Read-only mock-data tools, limited capabilities, bounded runs, provider cleanup |
| [43 - Selective delegation](../samples/43-selective-delegation/README.md) | Answer directly or select only relevant specialists | No write authority; actual dispatch evidence rather than claimed delegation |
| [44 - Research, write, review](../samples/44-research-write-review/README.md) | Pass research into a writer, then review and optionally revise | Legal phase order and at most one revision; critique is advisory |
| [45 - Responsive background jobs](../samples/45-responsive-background-jobs/README.md) | Start research and continue the conversation while it runs | Per-job sessions, bounded capacity, local job commands, explicit cancellation and shutdown |
| [46 - Partial results and deadlines](../samples/46-partial-results-deadlines/README.md) | Gather independent outcomes and explain missing findings | Execution deadlines, bounded retry, preserved successful results, observed late failures |
| [47 - Approved report](../samples/47-approved-report/README.md) | Coordinate analysis and propose a report | Human approval bound to the exact content; workers cannot write or approve |

All portfolio data and fixture news are fictional and educational. Nothing
places a trade or provides investment advice.

## Two explicit modes

**Live mode** calls the configured Foundry model through the real MAF agent
pipeline. News tools, when enabled by the selected sample, can also send
queries to hosted search and incur charges. Use only mock data. Configure the
same Session 3 settings used by Sample 41:

```powershell
# From the public repository root; authenticate privately.
az login --output none
.\tools\configure-user-secrets.ps1 -Session 3
dotnet run --project .\session-03\samples\42-specialist-team\MafClaw.Sample42.csproj -- --mode live --prompt "Prepare an educational morning briefing for the mock portfolio. Include allocations, concentration, and sourced news."
```

**Fixture mode** explicitly substitutes deterministic scripted inference or
fixture workers. It needs no Foundry credentials or network. It exercises
local orchestration and MAF integration without claiming that the scripted
decisions are intelligence, current news, or a live-model rehearsal. A live
failure never silently switches to fixtures.

From the Session 03 directory:

```powershell
dotnet run --project .\samples\42-specialist-team\MafClaw.Sample42.csproj -- --mode fixture --demo
dotnet run --project .\samples\43-selective-delegation\MafClaw.Sample43.csproj -- --mode fixture --demo
dotnet run --project .\samples\44-research-write-review\MafClaw.Sample44.csproj -- --mode fixture --demo
dotnet run --project .\samples\45-responsive-background-jobs\MafClaw.Sample45.csproj -- --mode fixture --demo
dotnet run --project .\samples\46-partial-results-deadlines\MafClaw.Sample46.csproj -- --mode fixture --demo
dotnet run --project .\samples\47-approved-report\MafClaw.Sample47.csproj -- --mode fixture --demo
```

Each project supports `--help`, `--mode live|fixture`, `--prompt "<request>"`,
and `--demo`. See its README for scenario-specific input and commands.
Do not copy a fixture demonstration's scripted approval into a live run:
live report writes require an actual human decision.
Sample 47 displays the full proposal and requires the exact console response
`APPROVE <displayed SHA-256>`. Any other input or EOF denies the write.
Its explicit fixture demo supplies labeled simulated approval first and
denial on a second run; fixture `--prompt` still reads actual console input.

## What MAF supplies, and what the application supplies

Samples 42 and 43 use a named `BackgroundAgentsProvider` in
`AIContextProviders`. It supplies the same background capability that
`HarnessAgentOptions.BackgroundAgents` automatically registers in Sample 41,
but retaining the provider makes its lifetime explicit. Each delegated task
has a separate session. When the parent session ends, the host calls
`ReleaseSessionAsync` to request cancellation and await its workers. Its
cleanup timeout is not proof that an uncooperative remote operation stopped.

The provider's `WaitTimeout` bounds **one wait**, not worker execution.
It leaves unfinished work running. It is not an execution deadline, durable
queue, or per-task cancellation API.

Samples 44-47 add visible host orchestration where the lesson requires
stronger rules: an ordered review workflow, an interactive job registry,
deadline/retry policy, or an exact-content approval gate. Their workers are
MAF agents in live mode; the host does not pretend these additional policies
come automatically from the background provider.

## Evidence before narration

- Read actual tool requests/results and host status events before the final
  assistant summary. An agent claiming it delegated is not dispatch evidence.
- Allocation and concentration numbers come from deterministic mock-data
  calculations, not arithmetic invented in prose.
- A cancellation request does not mean the worker has stopped. Likewise, a
  deadline can stop waiting without forcibly terminating an uncooperative task.
- Successful siblings remain available after a failure. Failed, timed-out,
  or still-running work must never be described as completed research.
- Reviewer feedback is another agent output, not an independent guarantee
  of correctness or permission to create a side effect.
- Sample 47's workers cannot grant approval. The host must verify the exact
  approved bytes and the actual saved report.

Job registries are process-local, not durable across restarts. These small
samples do not supply production scheduling, distributed leases, durable
completion, or a production audit store. The complete `code` advisor remains
the existing offline composition; it does not silently acquire six new
autonomous workflows.

Samples 45/46 retain ownership of late tasks and cancellation callbacks until
they finish, then dispose their clients. Their **report waits can be bounded
while shutdown is not**: code that never cooperates can delay shutdown
indefinitely. They do not kill processes or label abandoned execution as
confirmed cancellation. Sample 45 admits two executing workers and retains
eight jobs, including completed ones; collecting does not erase that history.

## Live news and mock-portfolio scope

In Samples 42/43, "my portfolio" means the bundled fictional holdings, already
available through the calculation tools in both modes. Do not supply real
account or holding information for this classroom exercise.

A hosted-search attempt or an SDK `Completed` task is not proof that current
news was retrieved. Search can return no usable sources even while the worker
returns a valid explanation of that limitation. The host's task/collection
verification checks execution metadata, not factual news correctness. Inspect
actual returned source URLs and the worker's limitations; do not invent
citations or silently replace missing live news with the fixture bulletin.

## Offline regression checks

From the Session 03 directory:

```powershell
dotnet run --project .\tests\OrchestrationSupport.Tests\MafClaw.OrchestrationSupport.Tests.csproj
dotnet run --project .\tests\SpecialistSamples.Tests\MafClaw.SpecialistSamples.Tests.csproj
dotnet run --project .\tests\ReviewApprovalSamples.Tests\MafClaw.ReviewApprovalSamples.Tests.csproj
dotnet run --project .\tests\LifecycleSamples.Tests\MafClaw.LifecycleSamples.Tests.csproj
```

These checks use scripted inference and local fixtures. They do not establish
live deployment availability, hosted-search quality, or universal model
compliance. Rehearse the chosen live prompts separately before presenting.
