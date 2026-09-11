# Session 03 setup

The complete Session 03 package is offline and requires no Azure credentials.

## Prerequisites

- .NET 10 SDK
- PowerShell 7+

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

No secrets, endpoints, model deployments, or network access are needed for
these offline rehearsals.
