# Governance is enforcement, not a checklist

## Teach the generic controls first

Sample 20 checks a synthetic `PRIVATE-DEMO` marker before adding a note to an
in-memory outbox. It contains no MAF or final-app dependency.

Sample 21 directly creates `publish_note`, wraps it in
`ApprovalRequiredAIFunction`, and builds a Harness. One model request produces
a pending approval while the outbox remains empty. `CreateResponse` binds the
decision to that exact request, and `RunAsync` resumes the same session.
No external message is sent.

```powershell
# From session-04: both are fully local and deterministic.
dotnet run --project .\samples\21-governance-agent\MafClaw.Sample21.csproj -- --fixture
dotnet run --project .\samples\21-governance-agent\MafClaw.Sample21.csproj -- --fixture --approve
```

Both return 0. The first prints `AFTER DECISION: outbox=0`; the second prints
`AFTER DECISION: outbox=1`. Both must first show `BEFORE DECISION: outbox=0`.
Plain `dotnet run` uses the configured model and asks for explicit `y/N`.

## Final application: compose the controls

Only after the generic samples, show these complete-app boundaries:

- `FinancePolicy` is an intentionally small application teaching rule.
- `ApprovalRequiredAIFunction` and Harness bind a human decision to the proposed action.
- `WithPurview` is the optional real organizational screening integration.

The local rule rejects the synthetic restricted marker and real-trading
requests. The governed client screens input, tool arguments/results and
completed output. Streaming is buffered before release so blocked content is
not partially displayed. This deliberately trades token streaming latency for
an understandable, testable output boundary.

Approval tests inspect the actual in-memory trade ledger before and after
approve/deny. The app never places a real trade. Invalid symbols, sides and
quantities fail before modifying that ledger.

## Optional Purview

Sample 22's `--describe` mode returns exit 2 and makes **no service call**.
That is a prerequisite description, not a successful Purview test.
The normal `dotnet run` requires enabled configuration, the appropriate tenant/license,
approved application/Graph permissions and a supported credential flow.
Sample 22 constructs a generic assistant and calls
`model.AsBuilder().WithPurview(...)` directly in `Program.cs`. The actual
`Microsoft.Agents.AI.Purview` wrapper performs service integration; it does not
rename a local regex as Purview or invoke the final application's factory.

Interactive Purview authentication is only a local path. In the final app, the baseline hosted
profile refuses enabled Purview without an explicitly supplied, supported
non-interactive credential. Do not copy browser authentication into a server.
Do not claim tenant policy allow/block or audit verification from a build,
description command or fixture. An enabled but failing service is an error,
not permission to silently disable governance.
