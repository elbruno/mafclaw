# Sample 12: MAF/Harness telemetry in Aspire

**Microsoft Agent Framework**, not plain C#. This generic lesson-topic agent
extends the idea in Sample 11 without changing it or using the finance app.
MAF/Harness produces the agent/model/tool spans; OpenTelemetry exports them
alongside a tool-call metric and a correlated structured log.

## Run the live demo

After shared model setup (`tools\configure-user-secrets.ps1` and `az login`),
start the standalone dashboard in a separate terminal:

```powershell
aspire dashboard run
```

Use the installed [Aspire CLI](https://aspire.dev/get-started/install-cli/)
(verified with 13.5.1). No Docker or AppHost is required. Open the login URL
printed by the CLI; leave that terminal running. Default local endpoints:
browser `http://localhost:18888`, OTLP/HTTP `http://localhost:4318`.
Keep the browser token private.

In this sample's directory:

```powershell
dotnet run
```

The sample uses the existing `mafclaw-session-04` chat settings. No new secrets
or application arguments are required. Live model inference can incur charges.

## Show the audience

Select **mafclaw-sample12** in the dashboard:

1. **Traces:** find the printed trace ID. Expand `lesson.run`, then the real
   `invoke_agent`, `chat`, `execute_tool`, and second `chat` spans. The Harness
   owns orchestration; we did not hand-write the model/tool loop.
2. **Structured logs:** open `Lesson completed with 1 tool call.` Its trace ID
   links the log back to the same run.
3. **Metrics:** choose `lesson.tool.calls`. One invocation produces a value
   of **1**. Available SDK duration/token metrics are additional signals;
   missing usage is not zero and fixture usage is not a cost benchmark.

Follow the A/B/C blocks in `Program.cs`: register the three OTLP exporters,
build/instrument the Harness, then run and flush before exiting. Providers
share `service.name`; agent and model wrappers disable sensitive content.
The custom log contains a count, not the prompt, answer or tool payload.
SDK operational metadata (such as model identifiers) can still be visible;
use synthetic requests only and do not expose the local dashboard publicly.

`ASPIRE EXPORT PASS` means the run completed, flushes completed, and the SDK
reported no exporter failure. Inspect the dashboard for actual retained data.
Standalone Aspire does not manage this process or capture its console output.
Its telemetry is in memory: restarting the dashboard clears the previous runs.

## Troubleshooting and automated checks

- **No dashboard / `ASPIRE EXPORT FAIL`:** start `aspire dashboard run` first.
  Browser port 18888 is not the OTLP port; this sample uses HTTP/protobuf on 4318.
  A successful model answer alone never earns an export PASS.
- **Changed collector port:** set `OTEL_EXPORTER_OTLP_ENDPOINT` to the matching
  loopback HTTP base URI, without `/v1/traces`. Remove an old override with
  `Remove-Item Env:OTEL_EXPORTER_OTLP_ENDPOINT` to use the default. Remote
  destinations and HTTPS/custom-auth collectors are outside this local lesson.
- **Missing model settings:** run the shared setup helper; this project shares
  Session 4's existing store. It never silently switches to a fixture.
- **Offline verification:** the repository verifier's `session-04:sample12-otlp`
  case starts an ephemeral local receiver, runs `--fixture`, and checks all
  three actual wire payloads, trace parents, counter value, content omission,
  rejected exports, an unavailable receiver, and remote-endpoint rejection.
  It does not need Aspire, Azure, Docker, or a model. `--fixture` alone still
  exports real telemetry, so it needs a running local receiver.

See [the observability guide](../../docs/observability.md) and the
[Aspire standalone dashboard documentation](https://aspire.dev/dashboard/standalone/).
