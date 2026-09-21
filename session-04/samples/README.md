# Session 4 sample ladder

Each numbered project is independently runnable from this session. The plain
projects reference only the BCL finance contracts, not the MAF agent library.
The MAF projects use the shared factory so instrumentation and policy do not drift.
All inputs are synthetic and educational, not financial advice.

| Pair | Plain C# | Microsoft Agent Framework |
|---|---|---|
| 10 / 11 | Real `ActivitySource` and `Meter` observations | Actual agent/tool spans, privacy filtering and OpenTelemetry export |
| 20 / 21 | Policy before a local side effect | Actual `ApprovalRequiredAIFunction` proposal/decision and verified trade ledger |
| 30 / 31 | Exact decimal evaluation and an intentional failing candidate | `LocalEvaluator`, `FunctionEvaluator` and `EvaluateAsync` with actual tool evidence |
| 40 / 41 | Real ASP.NET readiness/portfolio HTTP lifecycle | Real Foundry Responses hosting; require `status=completed`, not just HTTP 200 |
| 22 | Not a plain-policy substitute | Conditional real `WithPurview` screening |
| 32 | Not a deterministic-score substitute | Conditional real `FoundryEvals` model grading |

## Commands

Run from `session-04`:

```powershell
dotnet run --project .\samples\10-observability\MafClaw.Sample10.csproj
dotnet run --project .\samples\11-observability-agent\MafClaw.Sample11.csproj -- --fixture
dotnet run --project .\samples\20-governance\MafClaw.Sample20.csproj
dotnet run --project .\samples\21-governance-agent\MafClaw.Sample21.csproj -- --fixture
dotnet run --project .\samples\30-evaluations\MafClaw.Sample30.csproj
dotnet run --project .\samples\30-evaluations\MafClaw.Sample30.csproj -- --inject-regression
dotnet run --project .\samples\31-evaluations-agent\MafClaw.Sample31.csproj -- --fixture
dotnet run --project .\samples\40-host-contract\MafClaw.Sample40.csproj -- --self-test
dotnet run --project .\samples\41-hosted-agent\MafClaw.Sample41.csproj -- --fixture --self-test
```

The deliberate Sample 30 regression returns exit 1. That is successful
detection of a bad candidate, not a broken test runner. The verifier records
the expected exit separately.

## 10 / 11: follow one operation

Sample 10 creates two real nested spans and one counter measurement around the
fixed portfolio calculation. No model or OpenTelemetry collector is required.
Sample 11 adds the actual MAF tool loop and console/optional OTLP export.
`--fixture` scripts inference while still executing the real valuation tool.
`--live` uses the configured Foundry model and can incur charges. Unknown token
usage is not reported as a measured zero.

## 20 / 21: intention is not permission

Sample 20 allows one mock action and denies a restricted marker before the
action count changes. It is an application teaching rule, not enterprise DLP.
Sample 21 displays an actual model-proposed simulated trade and checks that the
trade ledger is empty before approval. The fixture denies it deterministically;
`--live` asks the user for an explicit decision. No real trade is possible.

## 30 / 31: prove the gate fails

Sample 30 checks the exact 27124.95 total and 66.01 percent allocation. The
regression flag changes the candidate total to 1.00 so the gate must reject it.
Sample 31 grades the MAF conversation, including the actual tool result. Live
inference plus local grading is still a chargeable model path.

## 40 / 41: local protocol is not cloud deployment

The self-test modes bind an ephemeral loopback port, issue a real HTTP request,
validate the response and stop the app. Without `--self-test` they run as local
servers. Sample 41 without `--fixture` uses local Azure CLI authentication for
development; the production `code\Hosted` app uses managed identity instead.
Neither local server should be exposed as an authenticated public service.

## 22 / 32: conditional services

```powershell
dotnet run --project .\samples\22-purview-agent\MafClaw.Sample22.csproj -- --describe
dotnet run --project .\samples\32-foundry-evaluations\MafClaw.Sample32.csproj -- --describe
```

These description commands return exit 2 and do not contact a service. They
are intentionally not called successful Purview/evaluation runs. After the
documented licensing, configuration, permissions and budget gates, use `--live`.
See [governance](../docs/governance.md) and [evaluations](../docs/evaluations.md).
