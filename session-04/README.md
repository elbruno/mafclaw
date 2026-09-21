# Session 4: Production Ready

One finance-agent definition, multiple hosts, and explicit operational
boundaries. This is a .NET 10 educational reference, not a production financial
service or compliance certification. All portfolio/trade data is synthetic.

The package contains real implementations, replacing the old .NET 9 placeholder:

- `code`: shared finance contracts/factory plus console, evaluation and Responses hosts.
- `samples`: four plain-C# / MAF pairs and two conditional service integrations.
- `docs`: setup, architecture, telemetry, policy, evaluations, deployment and operations.
- `tests`, `evals`, `observability`, `scripts`: executable evidence and operating assets.

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
Candidate slides and presenter material remain private until final approval.

## Resources

- [Session 4 event](https://aka.ms/mafclaw/4)
- [Series](https://aka.ms/mafclaw)
- [Series introduction](https://aka.ms/mafclaw/blog)
- [Public repository](https://aka.ms/mafclaw/repo)
- [Official production-ready article](https://devblogs.microsoft.com/agent-framework/agent-harness-making-your-claw-production-ready/)

Not financial advice. Do not put personal, confidential or real financial data
in the model, tools, memory, logs or demonstrations.
