# Deployment assets and explicit gates

The real local Responses host is implemented. Cloud deployment, identity/RBAC,
image execution and Application Insights ingestion require separate verified,
authorized steps; a local test is not a cloud deployment.

The production host chooses a specific managed-identity credential. Verify
the identity mechanism supported by the selected Foundry runtime and assign
only the required model/infrastructure roles. Sample 41's local live path
uses `AzureCliCredential` for development and is not the production entry point.

## Local protocol proof

Sample 40 serves an ordinary `/message` greeting and `/readiness`, without a
model. Sample 41 creates `HostedLessonAgent` in its own entry point, then
registers `AddFoundryResponses` and `MapFoundryResponses` directly. It has no
tools or final-app dependencies. Its self-test requires a completed greeting,
not a portfolio value. The host owns history, so it does not add a second
Harness history store.

From either sample directory, `dotnet run` starts the teaching server. The
flags below are for finite automated checks, not required for the on-air run.
Sample 41 accepts the Responses HTTP protocol but uses Chat Completions for
model inference; the two sides of that bridge are independent.

```powershell
# From session-04, no model credentials:
dotnet run --project .\samples\41-hosted-agent\MafClaw.Sample41.csproj -- --fixture --self-test
dotnet run --project .\code\Hosted\MafClaw.Session04.Hosted.csproj -- --fixture --urls http://127.0.0.1:8088
```

Without self-test mode, the introductory servers bind only to loopback ports
5090 (40) and 5091 (41). They do not demonstrate production authentication.

The complete `code\Hosted` server exposes the MAF Responses protocol and `/capabilities`; the SDK
provides readiness checks. Local mode is unauthenticated development mode.
Do not expose it directly to other users. The Foundry edge must authenticate
production requests and provide the platform identity/session headers.

## Reproducible container path

From `session-04`, with an available container engine:

```powershell
docker build -f .\code\Hosted\Dockerfile -t mafclaw-session04:local .
```

The build context is the **session root**, containing every referenced project,
the evaluation fixture and bundled skills. The image uses pinned .NET 10 SDK
and runtime tags, a non-root user and port 8088. It does not install Python or
enable local generated-code execution.

## Bundled .NET code path

```powershell
.\scripts\publish-host.ps1
```

This produces a fresh Linux publish folder and performs no deployment. The
current `azd ai agent init` supports `--deploy-mode code`, `--runtime dotnet_10`,
`--entry-point MafClaw.Session04.Hosted.dll`, and `--dep-resolution bundled`.
Use the complete printed publish folder as the source, not a project folder
whose references point outside the uploaded bundle.

After target/spend approval, follow the installed CLI's `init --help` using an
explicit existing project and unique agent name. `agent.manifest.yaml` is an
initialization template; `agent.yaml` describes the Responses protocol/resources.
`azd` generates the environment-bound `azure.yaml`; do not commit generated
resource IDs, environment state or credentials. Keep tool-specific user-agent
settings process-local.

Before claiming deployment: verify the immutable deployed version, authenticated
completed response, unauthenticated rejection at the platform boundary,
identity/session isolation, trace correlation, disabled local capabilities,
rollback to a known-good version and explicit cleanup. Do not change access,
create models or deploy paid resources merely to make a sample status green.
