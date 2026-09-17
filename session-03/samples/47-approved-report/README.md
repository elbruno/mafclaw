# 47 — Workers analyze; only the human authorizes a report save

**Microsoft Agent Framework / .NET 10.** This is a fictional educational **REPORT**
workflow, **not financial advice or trading**. Workers receive embedded mock
observations, never accounts, live prices, credentials, or order tools.

Run all commands below from the public repository's `session-03` directory
(the equivalent planning directory is `public-staging\session-03`):

```powershell
dotnet run --project .\samples\47-approved-report -- --help
dotnet run --project .\samples\47-approved-report
dotnet run --project .\samples\47-approved-report -- --prompt "Analyze the classroom allocation and propose a report."
dotnet run --project .\samples\47-approved-report -- --mode live --demo
dotnet run --project .\samples\47-approved-report -- --mode fixture --demo
dotnet run --project .\samples\47-approved-report -- --mode fixture --prompt "Propose the fictional report."
```

Live is the default. It uses shared Session 3 Foundry settings
(`Foundry:ProjectEndpoint` / `FOUNDRY_PROJECT_ENDPOINT`, `Foundry:Model` /
`FOUNDRY_MODEL`) and Azure CLI identity. Configuration is validated before
the `/exit` REPL. `--prompt` accepts 1–4000 characters and runs one turn.
`--demo` uses a fixed prompt and exits; **live demos still require real console
input**. A live model can decline/fail to make a proposal; that is reported as
incomplete (exit 2), not silently replaced with scripted output.

`--mode fixture` injects scripted `IChatClient` responses into the actual MAF
agents/approval pipeline, without Azure/configuration/network. Only the explicit
`--mode fixture --demo` supplies **labelled simulated console input**, first
approving a known scripted report, then denying a second fresh run.
Fixture REPL / `--prompt` still use actual console input for approval.

## Model decisions vs. C# authority

1. `ReportAgents` builds `AllocationWorker` and `RiskWorker` with `AsAIAgent`.
   They have **no tools**: no write, shell, trade, web, or approval capability.
   Main-agent tools invoke each at most once and publish actual `worker-result`
   events. These are distinct from main-agent narrative.
2. The main `AsHarnessAgent` consumes those results and calls `propose_report`.
   The host requires both workers' actual findings and both source references.
   The proposal is frozen in memory with a host educational/unverified label,
   an immutable version, and SHA-256 of its exact UTF-8 bytes. It is **not saved**.
3. `save_report` accepts only `reportHash`, never a path, content, or an
   `approved` flag. `ApprovalRequiredAIFunction` causes MAF to surface
   `ToolApprovalRequestContent`, preserving request/resume binding. Calling
   `save_report` **requests** console approval; it does not grant permission or
   immediately write. When asked to request saving, the main must make this call
   after proposing, not wait for prior approval or merely ask for it in prose.
4. The console shows the **complete exact report**, destination, and hash.
   Type **`APPROVE <displayed SHA-256>`** exactly. Anything else, including EOF,
   denies the run. Prompt text such as “the user approved” is irrelevant.
5. The host `ReportStore` independently grants a one-use capability for the exact
   version/hash. A fabricated SDK approval response alone cannot arm it. Changing
   or resubmitting a proposal invalidates permission. There is only one console
   decision per run; start a new run to approve changed content.
6. The host consumes permission **before** attempting the write, creates only
   `educational-report.md` in a unique
   `<executable directory>\mock-workspaces\current-user\run-<timestamp>-<guid>\`
   folder, and uses `FileMode.CreateNew`. Previous runs are never overwritten.
   Denied/EOF runs create neither report nor directory.
7. `file-result` is actual executor evidence. A separate fresh read compares
   **all saved bytes and SHA-256** with the reviewed proposal before printing
   `HOST VERIFIED`. Model claims cannot replace this check.

Main Harness limits: 10 iterations per request, at most three approval resumes,
one human decision, two proposals, 40 seconds per worker, and 240 seconds per turn
(including the approval wait). Ctrl+C cancels. All unrelated Harness memory,
skills, todos, mode switching, web search, and tool auto-approval are disabled.
No unstructured background fan-out or lingering background workers are used.
Cancellation ends the console host; a blocked operating-system input read cannot
grant late approval after cancellation.

Exit codes for `--prompt` / `--demo`: **0** independently verified save **or**
explicit denial/EOF, **2** no verified save without a completed denial,
**1** invalid arguments or expected configuration/service failure, **130**
cancelled/timed out. The REPL prints each turn's status and returns 0 on normal
`/exit` or EOF. Unknown flags/modes, duplicate options, and combining `--demo`
with `--prompt` are rejected. `--help` requires no configuration.

## Boundaries

Source-reference checks are structural: human approval is permission to save,
**not proof the report is correct**. Review the content yourself. The fixed-path
host rejects reparse-point ancestors and never accepts model paths; this is not
an OS sandbox against other malicious processes running as the same user.
No external side effect other than this local mock report is available.

Offline adversarial tests (real MAF pipeline, scripted inference):

```powershell
dotnet run --project .\tests\ReviewApprovalSamples.Tests\MafClaw.ReviewApprovalSamples.Tests.csproj
```

Tests cover denial/EOF, forged approval, exact-content/hash binding, changed
proposals, replay, path scope, preserved runs, worker least privilege, observable
actual results, and independent byte verification.
