# Evaluation layers

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
checks require the exact snapshot values and an actual valuation tool result,
not just digits in an assistant answer.

| Mode | Inference | Grading | Cost/availability |
|---|---|---|---|
| `fixture` | Explicit script; actual MAF tool loop | Local deterministic checks | No model call |
| `live` | Configured Foundry model | Local deterministic checks | Inference charges/prerequisites apply |
| `foundry` | Configured Foundry model | Real `FoundryEvals` relevance/coherence | Separate remote grading prerequisites and charges |

Use `--mode live` only after configuring the model. Use `--mode foundry`, or
Sample 32 `--live`, only with evaluation authorization. Sample 32 `--describe`
does not submit anything and exits 2. The remote example uses the pinned SDK's
evaluator interpretations; choose and validate domain-specific acceptance
thresholds before using it as a production release gate.

Each run writes a distinct safe report under `.local\evaluations`, including
mode, regression-injection flag, dataset hash, versions, model label and
outcomes. Reports never include raw prompts/responses or configured endpoints.
Keep fixture results separate from live and remotely graded evidence.
