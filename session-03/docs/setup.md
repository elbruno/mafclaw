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
- **21 (Shell):** `Tidy up my trade confirmations.` The agent inspects the
  seeded `working/confirmations` folder, proposes a plan, and (with your
  approval on each command) reorganizes the files - it can never leave the
  confined folder.
- **31 (CodeAct):** `What is the total portfolio value, and what percent is
  in Technology?` The agent reads `holdings.csv` via `file_access`, then
  writes and runs Python in a Hyperlight micro-VM to compute the answer
  (approve the `execute_code` call when prompted).
- **41 (Background agents):** `Research MSFT, NVDA and SPY and summarize the
  latest news.` The agent fans the tickers out to a background research
  sub-agent, runs them concurrently, and aggregates the findings.

## Teaching-source check

Before presenting, open the relevant C# file and use its objective header to
frame the explanation. The A/B/C comments should match the visible execution
order; if a sample changes, update its comments with the code rather than
adding a separate script that can drift.
