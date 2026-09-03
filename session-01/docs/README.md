# Session 1 Documentation

Session 1 teaches the difference between a standard Microsoft Agent Framework
agent and an agent built with the Harness, through four incremental checkpoints
that each add one concept.

Start with the [Session 1 landing page](../README.md), then use these guides:

- [Setup](./setup.md) — prerequisites, authentication, and configuration.
- [Troubleshooting](./troubleshooting.md) — common errors and how to fix them.
- [Finished sample](../code/README.md) — the completed Session 1 code (copy of Checkpoint 04).

## Checkpoints

1. [01 — Hello, Agent](../checkpoints/01-hello-agent/README.md) — minimum viable agent
2. [02 — Meet the Harness](../checkpoints/02-harness-agent/README.md) — same agent through `AsHarnessAgent`
3. [03 — Tools and Search](../checkpoints/03-tools-and-search/README.md) — custom tool + hosted web search
4. [04 — Planning and Todos](../checkpoints/04-planning-and-todos/README.md) — interactive REPL with `TodoProvider`

## Authoritative references

- [Official Session 1 article](https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/)
- [MafClaw series](https://aka.ms/mafclaw)
- [Session 1 event and recording](https://aka.ms/mafclaw/1)
- [Sample repository](http://aka.ms/mafclaw/repo)

## Key design points

- Config is inline (3 lines), no config class — `AddUserSecrets<Program>().AddEnvironmentVariables()`
- No try/catch — failures surface as raw exceptions (see [Troubleshooting](./troubleshooting.md))
- `AgentModeProvider` is enabled in Checkpoint 04 — the Harness handles plan/execute natively
- `DisableFileMemory = true` is the only Harness feature explicitly disabled
- `TodoProvider` is a reusable Agent Framework context provider configured by the Harness
- Memory and session resume are [supplemental in the official article](https://devblogs.microsoft.com/agent-framework/meet-your-agent-harness-and-claw/), not implemented here

All stock values in this repository are mock data for education and demonstration. They are not current market quotes and are not financial advice.
