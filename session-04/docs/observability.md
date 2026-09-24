# Observability: see the Harness work

Samples 10, 11 and 12 are deliberately independent of the finance app. The
lesson is **who calls the tool, where the time goes, and how one request links
the agent, model and tool**.

## 10: instrument ordinary C#

Open `samples\10-observability\Program.cs`. A plain function returns the
workshop topic. `ActivitySource` records a request with a child tool span;
`Meter` counts one tool call. The program checks that both spans share a trace
ID and that the child's parent is the request span. No MAF, model, collector
or business calculation is involved.

## 11: let MAF/Harness orchestrate

Open `samples\11-observability-agent\Program.cs`:

1. `AIFunctionFactory.Create` wraps the ordinary tool.
2. `AsHarnessAgent` owns the model/tool loop and session history.
3. The model client's `UseOpenTelemetry` reports individual inference calls;
   the agent's `UseOpenTelemetry` reports the outer invocation. The SDK's
   function-invocation instrumentation reports the tool.
4. One `RunAsync` call produces an answer and the local trace tree.

```powershell
# From session-04: no credentials, Docker or network needed.
dotnet run --project .\samples\10-observability\MafClaw.Sample10.csproj
dotnet run --project .\samples\11-observability-agent\MafClaw.Sample11.csproj -- --fixture
dotnet run --project .\samples\11-observability-agent\MafClaw.Sample11.csproj -- --fixture --fail-model
```

The first two commands return 0. The last returns **1 intentionally**: the
fixture fails the second model call, after the real tool has run. Its
`OBSERVABILITY AGENT PASS` marker means the failure evidence matched the selected
scenario; it is not an inference-success claim. Missing trace evidence returns 2.
Conflicting modes, unknown flags, and live failure injection
are rejected with exit 2.

The successful fixture shows this shape (durations and IDs vary):

```text
invoke_agent
  chat
  execute_tool
  chat
ACTUAL TOOL CALLS: 1
```

`LessonChatClient` scripts only the model responses. `TraceConsole` observes
actual SDK spans and validates their common trace and parent links; it does not
manufacture them. It prints only allowlisted operation names, duration, status
and correlation IDs, never raw tag/event dumps. `Unset` means the SDK did not
explicitly set a status. Fixture token usage is not a cost measurement.

For authorized live inference, configure Session 4 with
`tools\configure-user-secrets.ps1`, then use plain `dotnet run`. The sample
keeps `UserSecretsId` `mafclaw-session-04` and the canonical
`Foundry:ProjectEndpoint` / `Foundry:Model` keys (with their documented environment
aliases). It does not read finance, memory or Purview settings. Live provider
failures are reported by type without dumping payloads or configuration.

## 12: send the generic Harness telemetry to Aspire

Start `aspire dashboard run` in a separate terminal and open its printed
login URL. From `samples\12-observability-aspire`, run `dotnet run`.
This uses the same configured chat model as the other MAF samples, without
an AppHost, Docker, a finance factory, or new secrets.

`Program.cs` visibly registers OpenTelemetry traces, metrics and logs with
the shared resource name `mafclaw-sample12`. Three OTLP/HTTP exporters send
to `/v1/traces`, `/v1/metrics`, and `/v1/logs` on `http://localhost:4318`.
The agent/model wrappers keep content capture disabled. The tool increments
`lesson.tool.calls`; the completion log carries the same trace ID.

In Aspire, select that service and show the actual agent/model/tool spans,
the one-call counter, and the correlated structured log. The program flushes
before exiting and reports exporter failures separately from inference success.
Do not claim delivery from console spans alone. The dashboard retains
telemetry in memory until restarted; it does not manage the console process.

See [Sample 12](../samples/12-observability-aspire/README.md) for the
screen-sharing walkthrough, local-only endpoint rules and troubleshooting.
The offline `sample12-otlp` regression checks real protobuf payloads and
failure handling using an ephemeral receiver, not a running Aspire instance.

## Advanced: export the complete finance app's telemetry

Sample 11 is an **in-process instrumentation lesson**, not an exporter demo.
It does not use `OTEL_EXPORTER_OTLP_ENDPOINT`. Local trace observation is not
proof of collector delivery. Sample 12 teaches export before the complete
app's `FinanceTelemetry` and `RedactingActivityProcessor` appear at the final reveal.

```powershell
# From session-04; requires an installed container engine.
docker compose -f .\observability\compose.yaml up -d
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://127.0.0.1:4318"
dotnet run --project .\code\Console\MafClaw.Session04.Console.csproj -- --fixture --trace
# Enter: Value the snapshot.
# Enter: /exit
docker compose -f .\observability\compose.yaml down
```

Open the local dashboard at `http://127.0.0.1:18888`. Its anonymous mode is
deliberately loopback-only; do not expose it on a network interface. The pinned
dashboard image is a provided deployment asset, not evidence that Docker or
that dashboard was exercised on every machine.

The generic OTLP base URI is expanded to separate `/v1/traces` and
`/v1/metrics` endpoints. The regression suite runs a real loopback OTLP receiver
and checks emitted trace identity, metrics and the absence of a synthetic
private marker. This is transport evidence, not a claim of cloud ingestion.

The complete finance app's default pipeline does not capture prompts, responses, tool payloads,
memory or configuration values. An allowlist keeps operation/model/usage/tool
attributes; addresses, baggage and error descriptions are removed. Because
`ActivityEvent` payloads are immutable, spans containing events are explicitly
dropped before export, with a warning and `mafclaw.telemetry.privacy_drops`
measurement. A dropped span is not a successful export. This privacy-first
tradeoff can remove detailed error spans; use bounded error counters and safe
application diagnostics, not raw exception logging, for follow-up.

Unknown token usage remains unknown. Fixture inference is not a token/cost
benchmark. `--trace` enables console export; without it or an OTLP endpoint,
the console does not create an empty exporter pipeline.

The hosted runtime owns its exporter registration. The app adds privacy
processors/views before that registration rather than a duplicate exporter.
`observability\queries.kql` contains candidate workspace-based queries; verify
the actual tables/attributes after an authorized Application Insights deployment.
