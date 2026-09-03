# Checkpoint 04 — Planning, approval, and todos

[Session 1 home](../../README.md) · [Previous: tools and search](../03-tools-and-search/README.md) · Next: [final compatibility sample](../../code/README.md)

> **Runnable status:** Implemented. This checkpoint requires live Foundry configuration and Azure CLI authentication.

## Purpose

Complete the Session 1 teaching path by surfacing the `TodoProvider` instance
that the Harness configures and provides by default, then add the local planning
experience used by the validated final sample. `TodoProvider` is a reusable Agent
Framework context provider; it is not a custom tool.

The responsibilities are intentionally split:

- the Harness configures and provides the `TodoProvider` context-provider instance;
- `ClawConsole` owns local `plan`/`execute` state;
- `PlanningResponse` defines clarification and approval shapes;
- `ClawConsole` asks for explicit approval before switching to execute;
- Harness `AgentModeProvider` and file memory are disabled.
- skills, compaction, OpenTelemetry, and tool auto-approval are also disabled to keep this checkpoint focused.

This approval is structured **console approval**, not Harness tool approval.
It gates execution of the generated plan only. Entering `/mode execute`
explicitly opts into direct execution without a generated-plan approval turn.
Mode is sticky for the current console session until another `/mode` command or
an approved plan changes it.

## Finance scenario

Start in plan mode:

```text
Plan how to review MSFT and NVDA, including prices, recent context, and risks.
```

Then:

1. answer any clarification;
2. review the proposed plan;
3. approve with `y` or `yes` only when you want execution to continue;
4. run `/todos` to inspect provider state;
5. use `/mode` to inspect the local mode.

## Run

From `session-01`:

```powershell
dotnet run --project .\checkpoints\04-planning-and-todos\MafClaw.Checkpoint04.csproj
```

Live console commands:

```text
/mode
/mode plan
/mode execute
/todos
/exit
```

Expected stable markers:

```text
LIVE · mafclaw · checkpoint 04 · planning and todos
Mode starts in plan. Commands: /mode [plan|execute], /todos, /exit
```

A planning turn then produces either:

```text
Clarification required:
```

or an approval path containing:

```text
Plan approval required:
Approve plan? (y/n):
Plan approved. Switched to execute mode.
```

`/todos` prints `No todos yet.` until provider state contains items. Model-generated plan text and live search results are variable.

Live model calls and hosted search can incur Azure charges. Prompts, tool results,
and hosted-search query content can be sent to the configured service. Review
your organization's data-sharing, residency, logging, and cost policies, and do
not use confidential, personal, or real financial data.

If a content filter or safety policy refuses a request, respect the refusal. Do
not use repeated or evasive rephrasing to bypass it. Choose a benign educational
request or stop.

## Deterministic fallback

If this checkpoint does not yet include its own offline path, use the final compatibility implementation:

```powershell
dotnet run --project .\code\MafClaw.Session01.csproj -- --mode offline --scenario plan
```

That path must print `OFFLINE FALLBACK`. It demonstrates the approval boundary only; it is not model, Harness, hosted-search, or service execution.

## Limitations

- The current final sample disables `AgentModeProvider`.
- File memory is disabled.
- Session export/import is not implemented.
- Hosted search remains service-dependent.
- Stock values remain mock data.
- Approval does not authorize or execute a real trade.

Memory and session resume are [supplemental concepts in the official article](https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/), not a runnable checkpoint 05 here.

This sample is educational and is not financial advice.

[Previous: tools and search](../03-tools-and-search/README.md) · [Final compatibility sample](../../code/README.md) · [Session 1 home](../../README.md)
