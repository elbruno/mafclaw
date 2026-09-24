# Session 4 sample ladder

## On-air workflow: just `dotnet run`

Configure all cloud-backed projects once from the repository root:

```powershell
.\tools\configure-user-secrets.ps1
az login --output none
```

Then enter any numbered sample folder and run it, without application arguments:

```powershell
cd .\session-04\samples\51-mcp-tools-agent
dotnet run

cd ..\61-session-persistence-agent
dotnet run

cd ..\70-foundry-local
dotnet run

cd ..\71-foundry-local-agent
dotnet run
```

Cloud-backed samples use the configured live model. Missing settings or failed
requests are errors, never a silent switch to scripted output. Local samples
70/71 require no Azure login or secrets; first use may download their models.
Samples 40/41 stay running as loopback HTTP servers until Ctrl+C.
Sample 12 needs `aspire dashboard run` in a separate terminal before the demo.
Optional Purview (22) requires prior service configuration; remote grading (32) asks for
confirmation before submitting a paid evaluation.

## Teach the primitive, then MAF

Every numbered project is generic and independent of the final financial
application under `code`. Agent/Harness construction stays in `Program.cs`.
`Support` contains only chat connection, scripted inference and output helpers.
Start with each file's purpose and A/B/C header, then follow its separated
comment/code blocks. Keep the complete finance app for the final reveal.

| Pair | Plain C# | Microsoft Agent Framework |
|---|---|---|
| 10 / 11 | Request/tool spans and a counter | One Harness call produces the real agent/model/tool trace |
| 12 | Dashboard variant after 11 | The generic Harness exports traces, metrics and correlated logs to Aspire |
| 20 / 21 | A local rule before a mock outbox write | `ApprovalRequiredAIFunction` pauses before the write; answer y/N |
| 30 / 31 | Grade `2 + 3` against 5 | Calculator Harness plus answer/tool-result evaluators |
| 40 / 41 | Readiness/greeting HTTP endpoints | Greeting agent hosted through `AddFoundryResponses` / `MapFoundryResponses` |
| 50 / 51 | Connect, discover and call an MCP tool directly | One Harness agent uses those same Microsoft Learn MCP tools |
| 60 / 61 | Serialize and reload a `List<Turn>` | Serialize/restore a real `AgentSession` and verify resent history |
| 70 / 71 | Foundry Local SDK `ChatSession`, no MAF | ElBruno's local `IChatClient` adapter plus Harness and a clock tool |
| 22 | Optional service | Actual `WithPurview` screening, not a local-rule substitute |
| 32 | Optional service | Actual `FoundryEvals` grading, not a deterministic-score substitute |

### Observability (10/11/12)

Sample 10 calls `get_lesson_topic` directly. Show the parent/child IDs and
`lesson.tool.calls = 1`. Sample 11 visibly builds the tool, `AsHarnessAgent`,
and both `UseOpenTelemetry` wrappers. One `RunAsync` drives model/tool/model.
The trace observer displays SDK spans, not fabricated events. It does not
export OTLP or record prompt contents. `Unset` is left as an SDK status.

Sample 12 adds the exporter lesson before the finance app. Start
`aspire dashboard run` separately, then use `dotnet run` inside
`12-observability-aspire`. Select `mafclaw-sample12` in Aspire and inspect
the trace tree, `lesson.tool.calls = 1`, and the correlated completion log.
No Docker or AppHost is needed. See [Sample 12's walkthrough](12-observability-aspire/README.md).

### Governance and evaluations (20/21/22, 30/31/32)

Sample 21 prints `BEFORE DECISION: outbox=0`, asks for approval, then verifies
the outbox is still 0 after denial or 1 after approval. No external message is
sent. Sample 31 grades both the final answer and the real `add_numbers` result;
fluent prose alone is insufficient. Samples 22/32 remain separately gated
external-service demonstrations.

### Hosting (40/41)

The plain host listens on `http://127.0.0.1:5090`; the MAF host listens on
`http://127.0.0.1:5091`. Neither is an authenticated public service.
Sample 41's inbound protocol is Responses, while its configured model client
uses Chat Completions. The host owns history and model output storage is disabled.
See [deployment](../docs/deployment.md).

### MCP (50/51)

Both use the public Microsoft Learn MCP server. Sample 51 now focuses solely
on MCP: no unrelated hosted web-search deployment or preview tool is required.
MCP tools become `AITool` instances in the Harness, which owns dispatch and
result feedback. See [MCP tools](../docs/mcp-tools.md).

### Persistence (60/61)

Both demos save two exchanges, restore fresh state from JSON and continue in
one run. No save/resume filenames or extra invocations are needed. They retain
an inspectable, uniquely named JSON file under `.local\sessions` in the sample
working directory. Sample 61 verifies the earlier user messages actually reach
the model after restore. See [session persistence](../docs/session-persistence.md).

### Foundry Local (70/71)

Sample 70 uses the current native SDK's in-process `ChatSession` and
`qwen2.5-0.5b` (about 528 MB). Sample 71 uses
[`ElBruno.MAF.FoundryLocal.Adapter`](https://github.com/elbruno/ElBruno.MAF.FoundryLocal)
0.2.1 with its compatible native SDK dependency and `qwen2.5-1.5b` (about
1.3 GB), chosen for the tool lesson. Do not override the adapter's native SDK
version independently. No REST server or port management is needed.

Sample 71 requires an actual clock-tool call and checks that the live answer
contains the returned time. A hallucinated time is a failure, not a PASS.
Pre-cache both models before streaming. See [Foundry Local](../docs/foundry-local.md).

## Automated checks, separate from the presentation

The verifier selects fixture/failure switches itself. They are not needed for
the normal demo:

```powershell
# From the repository root: no model calls or local-model downloads.
.\tools\verify-repository.ps1 -Mode Offline
```

`--fixture` remains available for tests on 11/12/21/31/41/51/61/71.
Sample 12's verifier starts a temporary loopback OTLP receiver and needs no dashboard.
Sample 51's fixture still makes a **real MCP network call**, so it is excluded
from the offline matrix. `--describe` on 22/32/50/70 returns 2 without exercising
the service. A declined no-argument Sample 32 run also returns 2.

Intentional regressions: Sample 30 `--inject-regression`, Sample 31
`--fixture --inject-regression`, and Sample 11 `--fixture --fail-model`
return 1 when the expected failure is demonstrated. Sample 11's PASS marker
then means the error-trace evidence matched, not that inference succeeded.
Plain `dotnet run` never selects these test paths.
