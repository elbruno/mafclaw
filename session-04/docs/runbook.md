# Operating runbook

## Preflight

1. Check .NET 10, pinned package restore and `-Session 4 -Check`.
2. Run `scripts\verify-session.ps1 -Mode Offline` and inspect every failed or unrun case.
3. Select the exact authorized live cases; do not confuse local grading with free inference.
4. Confirm the telemetry destination privately and check actual trace/metric arrival.
5. For hosted work, verify the deployment version, identity and completed response envelope.

Console turns have a two-minute limit, model calls a 45-second deadline, and
function invocation a 12-iteration cap. Shell execution has its own shorter
limit and output cap. Cancel or stop only the process/session you own.
The console releases its tracked background sessions; hosted research awaits
its workers within the request and is not durable background processing.

## On failure

For Sample 12, start `aspire dashboard run` before `dotnet run`. Its UI uses
18888, but OTLP/HTTP uses 4318. Check `mafclaw-sample12` in Traces, Metrics
and Structured logs; a model answer is not proof of export. A stale
`OTEL_EXPORTER_OTLP_ENDPOINT` environment override must match the local
HTTP collector, not the browser. See the [sample runbook](../samples/12-observability-aspire/README.md#troubleshooting-and-automated-checks).

Stop the failed demonstration, preserve a safe result record and switch to the
explicit fixture/previously reviewed evidence. Never describe a fixture as live,
an HTTP 200 as completed work, or a missing service as a successful policy test.
Do not repeatedly retry a refusal or change permissions to bypass it.

## Change and release

Run the full repository verifier and package audit after shared dependency
updates. Keep previous scenarios intact and update commands/docs/tests together.
Freeze the tested versions before rehearsal; any later change needs fresh
checks. Roll back by redeploying the approved known-good version, not by rewriting
Git history or silently changing the model.

## Publication

Runtime outputs under `.local`, deployment state and credentials are not public
assets. Reviewed code/docs flow through the planning repository's sync tool.
Only a presenter-approved, reviewed slides-only PDF is a public slide artifact.
Source PPTX, notes and rehearsal files remain private. No scripts commit or push.
