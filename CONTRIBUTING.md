# Contributing

Thanks for your interest in contributing.

## Before you start

- Review the README for repository structure and session guidance
- Use mock data only
- Do not commit secrets or private configuration

## Pull requests

- Keep changes focused
- Update docs when behavior changes
- Ensure sample code remains runnable

## Teaching-first sample code

These samples support a live stream. Prioritize a clear presentation and an
explicit, easy-to-follow sequence over production-style abstraction or
code-quality polish. Correctness, safe defaults, honest fixture/live labels,
and runnable behavior remain required.

After shared setup, `dotnet run` must run the normal teaching flow. Do not
require `--live`, save/resume paths, or test flags for a numbered-sample demo.
Keep fixture/failure switches for automated checks and optional paid-service
approval explicit.

- Start each main sample file with its number, plain-C#/Microsoft Agent Framework
  identity, purpose, and short A/B/C steps. Helper files should state their role.
- Add matching step comments before meaningful blocks. Explain what happens,
  why the audience should notice it, and what output demonstrates it; avoid
  line-by-line narration of obvious syntax.
- Put a blank line after the file header and before a new explanatory comment
  block following completed code. Keep each comment next to the code it explains.
- Keep MAF/Harness construction visible. Name the SDK type/API at each bridge
  and explain the orchestration it saves the host from implementing.
- Teach the primitive before the framework bridge. A focused sample should not
  require a tour of the complete finance application to understand its concept.
- Keep all numbered samples generic and independent of projects under `code`.
  Do not use financial-app factories, tools, settings or contracts in them.
  Build the relevant agent/Harness directly in `Program.cs`; share only
  non-domain connection, fixture and output helpers. Present the financial
  application only after the individual topics.
