# Session 04: Production Ready

This session refactors the cumulative Session 1-3 snapshot into a production-ready agent with observability, governance, and deployment guidance.

Target framework: .NET 10.

## What this sample will show

- Shared agent factory consumed by console host, evaluation host, and hosted deployment.
- OpenTelemetry traces and metrics.
- Application Insights or OTLP configuration.
- Optional Purview governance integration (with licensing and tenant prerequisites).
- Local deterministic and optional Foundry evaluations.
- Hosted-agent deployment manifests, container assets, identity, and verification.
- Hosted-mode restrictions (no local filesystem or shell capabilities).

## Status

Session 04 code snapshot and documentation are under development. A complete runnable sample will be available after Session 3 publication.

For now, read the official blog post for the conceptual foundation:

https://devblogs.microsoft.com/agent-framework/agent-harness-making-your-claw-production-ready/

See `session-01/docs/README.md`, `session-02/docs/README.md`, and `session-03/docs/README.md` for completed setup and architecture reference.

## Coming soon

- Full Session 04 code snapshot with real telemetry and deployment.
- Container build and deployment guidance.
- Evaluation frameworks and safety assessments.
- Production deployment checklists.
- Advanced troubleshooting and scaling patterns.
