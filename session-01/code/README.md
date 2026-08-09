# Session 01 code snapshot

Minimal .NET sample for Episode 1.

Target framework: .NET 9.

## Scope

- harness-shaped flow via `AsHarnessAgent()`
- `get_stock_price` tool backed by mock JSON data
- placeholder-safe `web_search` tool
- todo-list planning before the sample response

## Run

```powershell
dotnet run --project .\MafClaw.Session01.csproj
```

The sample reads `mock-market-data.json` from this folder and prints a demo response to the console.
