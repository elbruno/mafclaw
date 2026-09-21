# Observability and privacy

Sample 10 records actual `ActivitySource`/`Meter` events without MAF. Sample 11
uses actual MAF/tool execution with the same OpenTelemetry setup as the console.
Use `--fixture` for scripted inference and `--live` only with model configuration.

```powershell
# From session-04; requires an installed container engine.
docker compose -f .\observability\compose.yaml up -d
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://127.0.0.1:4318"
dotnet run --project .\samples\11-observability-agent\MafClaw.Sample11.csproj -- --fixture
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

The default pipeline does not capture prompts, responses, tool payloads,
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
