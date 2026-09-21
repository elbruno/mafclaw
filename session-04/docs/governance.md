# Governance is enforcement, not a checklist

Keep three boundaries distinct:

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
`--live` requires enabled configuration, the appropriate tenant/license,
approved application/Graph permissions and a supported credential flow.
The factory uses the actual `Microsoft.Agents.AI.Purview` wrapper; it does not
rename a local regex as Purview.

Interactive Purview authentication is only a local path. The baseline hosted
profile refuses enabled Purview without an explicitly supplied, supported
non-interactive credential. Do not copy browser authentication into a server.
Do not claim tenant policy allow/block or audit verification from a build,
description command or fixture. An enabled but failing service is an error,
not permission to silently disable governance.
