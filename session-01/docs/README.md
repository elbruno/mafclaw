# Session 1 documentation

Session 1 teaches the difference between a standard Microsoft Agent Framework
agent and an agent built with the Harness. Checkpoints 01 and 02 retain the same
Foundry provider, Responses API, deployment/model, instructions, prompt, and
intended behavior while intentionally changing client construction.

Start with the [Session 1 landing page](../README.md), then use these guides:

- [Setup](./setup.md) — prerequisites, authentication, and local configuration.
- [Checkpoint comparison](./comparison.md) — who owns each capability before and after the Harness.
- [Offline fallback](./offline.md) — deterministic rehearsal behavior and its boundaries.
- [Troubleshooting](./troubleshooting.md) — common command, configuration, authentication, and service failures.
- [Final runnable implementation](../code/README.md) — compatibility path for the previously published Session 1 sample.

## Authoritative references

- [Official Session 1 article](https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/)
- [Baseline hosted-agent sample](https://github.com/Azure-Samples/microsoft-foundry-hosted-agents/blob/main/01-MAF-Agent-CS/Program.cs)
- [MafClaw series](https://aka.ms/mafclaw)
- [Session 1 event and recording](https://aka.ms/mafclaw/1)

## Scope boundary

The current final sample disables Harness file memory and `AgentModeProvider`.
Its `ClawConsole` owns local plan/execute state, structured planning, and
generated-plan approval. The Harness configures and provides the `TodoProvider`
instance by default; `TodoProvider` is a reusable Agent Framework context
provider. Explicit `/mode execute` opts into direct execution.

The official article also demonstrates file memory plus session export/import. Those are useful supplemental concepts, but they are **not implemented by the current Session 1 sample or represented as a runnable checkpoint here**.

All stock values in this repository are mock data for education and demonstration. They are not current market quotes and are not financial advice.
