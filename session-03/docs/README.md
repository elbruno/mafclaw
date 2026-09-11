# Session 03 documentation

This folder contains the session-specific materials that accompany the final
advisor and numbered sample ladder:

- [`setup.md`](setup.md) - prerequisites and build commands
- [`architecture.md`](architecture.md) - the four host-owned boundaries
- [`troubleshooting.md`](troubleshooting.md) - rehearsal and failure guidance

The official Microsoft Agent Framework article is:

https://devblogs.microsoft.com/agent-framework/agent-harness-scaling-the-claw-or-harness-capabilities/

The private teaching materials remain under `sessions\session-03\`:

- `README.md`
- `demo-script.md`
- `run-of-show.md`
- `test-plan.md`
- `slides\`

## Source-comment convention

Session 03 code is written for live online teaching. Every C# file identifies
its objective and A/B/C steps at the top, then uses short comments before the
major blocks. The comments explain the boundary to point at; they do not
replace the implementation or add production behavior.
