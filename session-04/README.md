# Session 4: Production Ready

One finance-agent definition, multiple hosts, and explicit operational
boundaries. This is a .NET 10 educational reference, not a production financial
service or compliance certification. All portfolio/trade data is synthetic.

[Download the Session 4 slides (PDF, 24 pages)](slides/mafclaw-session-04.pdf).
The light .NET-purple comic presentation starts with the recap and audience
follow-ups, then the operational lessons, and ends with the complete finance app.

The package contains real implementations, replacing the old .NET 9 placeholder:

- `code`: shared finance contracts/factory plus console, evaluation and Responses hosts.
- `samples`: seven plain-C# / MAF pairs, an Aspire observability variant, and two conditional service integrations.
- `docs`: setup, MCP tools, session persistence, Foundry Local, architecture,
  telemetry, policy, evaluations, deployment and operations.
- `tests`, `evals`, `observability`, `scripts`: executable evidence and operating assets.

## Audience follow-ups from Session 3

Start with the recap, then these three focused topics before the Session 4
observability, governance, evaluation and hosting lessons:

| Topic | Plain C# | Microsoft Agent Framework | Guide |
|---|---|---|---|
| Consume external MCP tools | [50](samples/50-mcp-tools) | [51](samples/51-mcp-tools-agent) | [MCP tools](docs/mcp-tools.md) |
| Save and restore a conversation | [60](samples/60-session-persistence) | [61](samples/61-session-persistence-agent) | [Session persistence](docs/session-persistence.md) |
| Run inference on-device | [70](samples/70-foundry-local) | [71](samples/71-foundry-local-agent) | [Foundry Local](docs/foundry-local.md) |

These are independently runnable teaching samples, not additional capabilities
of `FinanceAgentFactory`. The complete finance app composes the earlier
session features and Session 4 operational controls at the end of the lesson.
The [sample ladder](samples/README.md) contains every run command.

**For the live walkthrough:** run `.\tools\configure-user-secrets.ps1` once
from the repository root, then enter a numbered sample folder and use
`dotnet run`. No `--live` or save/resume arguments are needed. Samples 60/61
show the entire persistence flow in one run. Samples 70/71 use local models
and need no cloud settings; pre-cache them before streaming.
For [Sample 12](samples/12-observability-aspire/README.md), first start
`aspire dashboard run` in another terminal. Then `dotnet run` exports the
generic agent's traces, metrics and structured logs to the local dashboard.

## Generic teaching samples first; financial app last

Every numbered sample is independent of the final app's projects and builds
its own relevant MAF/Harness integration. The walkthroughs use tiny generic
examples: a lesson-topic tool for observability, a mock outbox for approval,
`2 + 3` for evaluation, and a greeting for hosting. MCP, persistence and local
inference also have no financial-app dependencies. The cumulative financial
application remains in `code` for the final reveal.

The published v04 slides predate these generic sample rewrites. Use the
current source and guides for the revised demos; slide regeneration is
deferred until the sample review is complete.

## Run without credentials

From this session directory:

```powershell
dotnet run --project .\code\Console\MafClaw.Session04.Console.csproj -- --fixture
# Enter: Value the snapshot.
# Enter: /exit
dotnet run --project .\code\Evals\MafClaw.Session04.Evals.csproj -- --mode fixture
.\scripts\verify-session.ps1 -Mode Offline
```

Fixture mode uses **scripted inference with actual MAF tool execution**. It is
not a live model demonstration. The evaluation corpus includes 24 deterministic
contracts; additional regressions exercise approval, memory confinement,
telemetry transport and a real local Responses request.

## Run against the configured model

From the repository root:

```powershell
.\tools\configure-user-secrets.ps1 -Session 4
.\tools\configure-user-secrets.ps1 -Session 4 -Check
dotnet run --project .\session-04\code\Console\MafClaw.Session04.Console.csproj
```

The full local profile adds files, a named local memory provider, skills,
approval-gated shell/Hyperlight, and bounded research. Use `--no-shell` or
`--no-codeact` explicitly if the corresponding local capability is unavailable.
There is no silent fallback to unrestricted Python or fake inference.

## Verification and availability

The local implementation and its deterministic checks are separate from cloud
certification. A local HTTP 200 is not enough: the Responses envelope must say
`completed`. Hosted deployment, production identity/RBAC, Purview licensing and
remote Foundry grading require their own configured, authorized verification.
No cloud deployment is implied by this package.

See the [sample ladder](samples/README.md), [setup](docs/setup.md),
[architecture](docs/architecture.md), and [runbook](docs/runbook.md).
The [audience slides](slides/README.md) contain no presenter notes.
Editable deck sources, presenter material and production evidence remain private.

## Resources

- [Session 4 event](https://aka.ms/mafclaw/4)
- [Series](https://aka.ms/mafclaw)
- [Series introduction](https://aka.ms/mafclaw/blog)
- [Public repository](https://aka.ms/mafclaw/repo)
- [Official production-ready article](https://devblogs.microsoft.com/agent-framework/agent-harness-making-your-claw-production-ready/)

Not financial advice. Do not put personal, confidential or real financial data
in the model, tools, memory, logs or demonstrations.
