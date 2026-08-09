# Session 02 Docs

Session 02 expands the claw with safe local context patterns.

Target framework: .NET 9.

## What this sample shows

- placeholder file access guarded by an approval gate
- lightweight in-memory memory storage for demo state
- mock quote lookup from local JSON
- placeholder configuration loaded from `appsettings.template.json`

## Run the snapshot

From `session-02\code`:

```powershell
dotnet run --project .\MafClaw.Session02.csproj
```

The sample is self-contained, uses mock data only, and is not financial advice.
