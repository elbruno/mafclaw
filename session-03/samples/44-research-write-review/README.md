# 44 — Research → write → review, with one revision

**Microsoft Agent Framework / .NET 10.** Live inference is the default. Research is
always the embedded, fictional classroom fixture; there is no web/news access.
All outputs are educational, **not financial advice**.

Run all commands below from the public repository's `session-03` directory
(the equivalent planning directory is `public-staging\session-03`):

```powershell
dotnet run --project .\samples\44-research-write-review -- --help
dotnet run --project .\samples\44-research-write-review
dotnet run --project .\samples\44-research-write-review -- --prompt "Explain the fictional allocation."
dotnet run --project .\samples\44-research-write-review -- --mode live --demo
dotnet run --project .\samples\44-research-write-review -- --mode fixture --demo
dotnet run --project .\samples\44-research-write-review -- --mode fixture --prompt "Explain the fictional allocation."
```

Live mode uses the shared Session 3 Foundry settings (`Foundry:ProjectEndpoint` /
`FOUNDRY_PROJECT_ENDPOINT`, `Foundry:Model` / `FOUNDRY_MODEL`) and Azure CLI identity.
Configuration is validated before the `/exit` REPL begins. `--prompt` is a single
turn (1–4000 characters); `--demo` is a fixed single showcase and exits.
Fixture mode never loads Azure configuration or calls the network. Its
`FixtureChatClient` produces **scripted** decisions through the actual MAF Harness;
it is not a simulation of real news or a claim that live agents were run.

## Follow the authority boundary

1. `WorkflowAgents` creates a main `AsHarnessAgent` and named `WriterAgent` /
   `ReviewerAgent` via `AsAIAgent`. Specialists have **no tools**.
2. The main chooses host tools. `OrderedWorkflow` serializes every tool call,
   requires research → draft → review, and passes the **actual previous output**
   into the next specialist. Tools accept no fabricated draft or feedback.
3. After the first review, the main may finish or request **one** revision. A
   revised draft must be reviewed. There can be at most two drafts and two reviews;
   repeated/out-of-order calls return `HOST REJECTED` without invoking specialists.
4. `DraftCheck` requires both fixture references, no unknown `[MOCK-...]` IDs,
   and the exact disclaimer `Educational mock report. Not financial advice.`
   Missing structure cannot be overridden by reviewer praise.
5. Actual `specialist-result`, `TOOL_CALL`/`TOOL_RESULT`, and host-check events appear separately
   from `MAIN NARRATIVE`. The fixture deliberately omits the first disclaimer,
   critiques it, revises it, and completes after the second review.

This is an **explicit host-enforced workflow**, not unstructured
`BackgroundAgents` fan-out. No background agents, shell, file writes, memory,
skills, todos, web search, or auto-approval are available. The Harness handles
tool dispatch/history; C# owns legal transitions. Limits are 12 main iterations,
40 seconds per specialist, and 180 seconds per turn, with Ctrl+C cancellation.
Specialist errors fail the workflow rather than silently retrying.
Cancellation ends the console host; a blocked operating-system input read cannot
start another model turn after cancellation.

**Limitations:** reviewer feedback is advisory. Citation/disclaimer checks are
structural, not semantic fact-checking. An unsupported assertion remains
**unverified even when both the reviewer and structural checks approve**. Live
models may fail to follow the workflow; the host reports incomplete (exit 2),
never invented completion. Nothing is saved.

Exit codes for `--prompt` / `--demo`: **0** completed, **2** incomplete,
**1** invalid arguments or expected configuration/service failure, **130**
cancelled/timed out. The REPL prints each turn's status and returns 0 on normal
`/exit` or EOF. Unknown flags/modes, duplicate options, and combining `--demo`
with `--prompt` are rejected. `--help` requires no configuration.

Offline checks for both new samples:

```powershell
dotnet run --project .\tests\ReviewApprovalSamples.Tests\MafClaw.ReviewApprovalSamples.Tests.csproj
```
