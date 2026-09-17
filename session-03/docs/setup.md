# Session 03 setup

The complete Session 03 advisor and all plain-concept samples run offline.
Samples `11-skills-agent`, `21-confined-shell-agent`, `31-codeact-agent`, and
`41-background-agents` are live Microsoft Agent Framework + Harness examples
and require an Azure AI Foundry project and model.

## Prerequisites

- .NET 10 SDK
- PowerShell 7+
- Azure CLI and an Azure AI Foundry project with a deployed model (Samples 11, 21, 31, 41)
- A CPU with hardware virtualization enabled, for Sample 31's Hyperlight sandbox

Sample 21 explicitly launches `pwsh` on every OS. Put PowerShell 7 on the
executable path; the sample reports a startup error rather than silently
changing to another shell. Configure authentication privately before sharing
the terminal.

Verify the SDK:

```powershell
dotnet --version
```

## Build everything

From `public-staging\session-03`:

```powershell
dotnet build .\code\MafClaw.Session03.csproj
dotnet build .\samples\10-skills\MafClaw.Sample10.csproj
dotnet build .\samples\11-skills-agent\MafClaw.Sample11.csproj
dotnet build .\samples\20-confined-shell\MafClaw.Sample20.csproj
dotnet build .\samples\21-confined-shell-agent\MafClaw.Sample21.csproj
dotnet build .\samples\30-codeact-calculation\MafClaw.Sample30.csproj
dotnet build .\samples\31-codeact-agent\MafClaw.Sample31.csproj
dotnet build .\samples\40-background-queue\MafClaw.Sample40.csproj
dotnet build .\samples\41-background-agents\MafClaw.Sample41.csproj
```

## Run the offline samples

No secrets, endpoints, model deployments, or network access are needed for the
complete advisor or Samples `10`, `20`, `30`, and `40`.

### Sample 20: show acceptance and rejection

From the Session 03 directory:

```powershell
dotnet run --project .\samples\20-confined-shell\MafClaw.Sample20.csproj -- dotnet --version
dotnet run --project .\samples\20-confined-shell\MafClaw.Sample20.csproj -- dotnet --info
$LASTEXITCODE # Expected: 2, because the second request is denied.
dotnet run --project .\tests\Sample20.Tests\MafClaw.Sample20.Tests.csproj
```

The allowed run prints the SDK version and child exit code. The denied run
prints `Policy: DENIED` and `Process started: no.` before returning `2`.
Pass tokens separately after `--`; a quoted `"dotnet --version"` is one token
and is not interpreted as a command line. A no-argument run retains the
original allowed version-check behavior.

These checks do not prompt for approval or change the allowlist. The test
project uses synthetic child processes to verify actual timeout termination
and output capture, without adding them to the sample's allowed commands.

## Run the live samples

From the repository root, authenticate and configure the four live projects:

```powershell
az login --output none
.\tools\configure-user-secrets.ps1 -Session 3
dotnet run --project .\session-03\samples\11-skills-agent\MafClaw.Sample11.csproj
dotnet run --project .\session-03\samples\21-confined-shell-agent\MafClaw.Sample21.csproj
dotnet run --project .\session-03\samples\31-codeact-agent\MafClaw.Sample31.csproj
dotnet run --project .\session-03\samples\41-background-agents\MafClaw.Sample41.csproj
```

Try these prompts:

- **11 (Skills):** `Value 25 shares of MSFT using the mock data.` The agent
  advertises the file-based skill, loads its `SKILL.md` instructions and
  bundled price reference on demand, then returns a mock educational result.
- **21 (Shell):** `Tidy up my trade confirmations.` Open the fresh workspace
  printed at startup. The agent requests inspection, proposes a rename batch,
  and pauses for approval on every shell call. `TOOL RESULT` contains actual
  executor output; `HOST VERIFIED: 4/4` requires the expected filenames and
  original hashes to match. `/verify` repeats the local check without a model
  call. Earlier runs are preserved; the configured directory is not a sandbox.
- **31 (CodeAct):** `What is the total portfolio value, and what percent is
  in Technology?` The agent reads `holdings.csv` via `file_access`, then
  writes and runs Python in a Hyperlight micro-VM to compute the answer
  automatically, without a CodeAct approval prompt (`NeverRequire`). Confirm
  the startup policy banner and inspect actual tool output. Use the included
  mock data only; see [Sample 31](../samples/31-codeact-agent/README.md).
- **41 (Background agents):** `Research MSFT, NVDA and SPY and summarize the
  latest news.` The agent fans the tickers out to a background research
  sub-agent, runs them concurrently, and aggregates the findings.

## Teaching-source check

For the offline Sample 21 regression checks, run from the Session 03 directory:

```powershell
dotnet run --project .\tests\Sample21.Tests\MafClaw.Sample21.Tests.csproj
```

These require PowerShell 7 but use a fake model, so they do not need Foundry
credentials. Rehearse the interactive model separately before presenting.

Before presenting, open the relevant C# file and use its objective header to
frame the explanation. The A/B/C comments should match the visible execution
order; if a sample changes, update its comments with the code rather than
adding a separate script that can drift.

## Configure and run Samples 42-47

The six [orchestration extensions](orchestration.md) reuse Session 3's
Foundry endpoint/model and user-secrets store. The setup script includes all
six new projects; no new Azure resource, shell executor, or Hyperlight runtime
is needed for these scenarios.

Every numbered project references `samples\OrchestrationSupport`. Run
`dotnet run --project <sample.csproj> -- --mode fixture --demo` to build and
exercise an offline scenario, or use `--mode live` after private authentication.
`--help` describes the individual console commands. Do not omit the explicit
fixture selection when rehearsing without a cloud connection.

See the guide for the six exact project paths and four offline test runners.
The fixture checks exercise local boundaries and scripted MAF integration;
they are distinct from a live-model rehearsal.
