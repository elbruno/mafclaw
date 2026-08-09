# Session 02 code snapshot

Minimal .NET sample for Episode 2.

Target framework: .NET 9.

## Scope

- lightweight file-access flow with an approval gate
- in-memory memory store for safe demo state
- mock stock lookup using local JSON
- placeholder settings loaded from `appsettings.template.json`

## Run

```powershell
dotnet run --project .\MafClaw.Session02.csproj
```

The sample is self-contained, uses mock data only, and demonstrates safe placeholders for approvals and memory.
