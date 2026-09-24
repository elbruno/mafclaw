# Evaluation layers

## Generic examples before the final app

Sample 30 grades the candidate answer to `2 + 3` against 5.
`--inject-regression` changes the candidate to 99 and must return exit 1.
This keeps attention on the gate rather than business-specific arithmetic.

Sample 31 builds its own calculator Harness with one `add_numbers` tool.
`LocalEvaluator` combines two visible `FunctionEvaluator` checks: the final
answer must equal 5, and the conversation must contain the actual tool result 5.
`EvaluateAsync` runs the conversation and aggregates outcomes.

```powershell
dotnet run --project .\samples\31-evaluations-agent\MafClaw.Sample31.csproj -- --fixture
dotnet run --project .\samples\31-evaluations-agent\MafClaw.Sample31.csproj -- --fixture --inject-regression
# Expected exits: 0, then 1 (correct tool result but deliberately wrong final answer).
```

Sample 32 creates a generic workshop-explainer agent and directly selects
`FoundryEvals.Relevance` / `Coherence`. Its `--describe` path needs no service;
Normal `dotnet run` asks for confirmation and requires separate model/grading authorization and budget.
None of these numbered samples references the final financial application.

## Final application evaluation host

The versioned `evals\cases.json` corpus has 24 deterministic contracts. It checks
decimal totals, allocation, quotes, invalid trades, policy, actual hosted/fixture
capability registration and screened streaming. The executable regression suite
adds actual approval, memory, cancellation, export and HTTP lifecycle checks.

```powershell
# From session-04:
dotnet run --project .\code\Evals\MafClaw.Session04.Evals.csproj -- --mode fixture
dotnet run --project .\code\Evals\MafClaw.Session04.Evals.csproj -- --mode fixture --inject-regression
# The second command must exit 1.
```

`LocalEvaluator`, `FunctionEvaluator` and `EvaluateAsync` are supplied by
`Microsoft.Agents.AI`, not a separate invented evaluation package. The MAF
final-app checks require the exact snapshot values and an actual valuation tool result,
not just digits in an assistant answer.

| Mode | Inference | Grading | Cost/availability |
|---|---|---|---|
| `fixture` | Explicit script; actual MAF tool loop | Local deterministic checks | No model call |
| `live` | Configured Foundry model | Local deterministic checks | Inference charges/prerequisites apply |
| `foundry` | Configured Foundry model | Real `FoundryEvals` relevance/coherence | Separate remote grading prerequisites and charges |

Use `--mode live` only after configuring the model. Use `--mode foundry`, or
Sample 32 with plain `dotnet run`, only with evaluation authorization. Sample 32 `--describe`
does not submit anything and exits 2. The remote example uses the pinned SDK's
evaluator interpretations; choose and validate domain-specific acceptance
thresholds before using it as a production release gate.

Each run writes a distinct safe report under `.local\evaluations`, including
mode, regression-injection flag, dataset hash, versions, model label and
outcomes. Reports never include raw prompts/responses or configured endpoints.
Keep fixture results separate from live and remotely graded evidence.
