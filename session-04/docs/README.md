# Session 4 documentation

The teaching sequence starts with a recap, then the audience-requested MCP,
session-persistence and Foundry Local samples. Observability, governance,
evaluation, hosting and operations follow. Show each plain-C# primitive before
its MAF bridge. Reveal the complete finance app and shared definition at the
end; the small samples keep the individual concepts explainable.

For live code walkthroughs, read each entry point's purpose and A/B/C header
before scrolling to its matching block comments. Pause at the observable result
and name what MAF/Harness supplied at each framework boundary. See the
[sample reading guide](../samples/README.md#reading-the-code-on-stream) and
[teaching-first contribution rules](../../CONTRIBUTING.md#teaching-first-sample-code).

Every numbered sample is generic and independent of the final application.
Agent/Harness construction is visible in the sample entry point; shared
`samples\Support` helpers only handle model connection, fixtures and output.
There are no project references from the samples to `code`.

| Topic | Guide |
|---|---|
| SDK, configuration and run commands | [Setup](setup.md) |
| Factory, lifetimes and host capability differences | [Architecture](architecture.md) |
| Actual traces, metrics, correlated logs, Aspire (Sample 12), privacy and OTLP | [Observability](observability.md) |
| Authorization, approval and optional Purview | [Governance](governance.md) |
| Deterministic contracts, actual MAF evals and optional remote grading | [Evaluations](evaluations.md) |
| Container/bundled deployment and identity gates | [Deployment](deployment.md) |
| Consuming MCP tools, raw client and MAF agents | [MCP tools](mcp-tools.md) |
| Saving/restoring conversations, plain and `AgentSession` | [Session persistence](session-persistence.md) |
| On-device inference with Foundry Local, plain and MAF bridge | [Foundry Local](foundry-local.md) |
| Known failure modes | [Troubleshooting](troubleshooting.md) |
| Preflight, rollback and operating boundaries | [Runbook](runbook.md) |

All examples are mock and educational, not financial advice. The title
"Production Ready" does not certify a regulated financial service.
