# Session 01 Docs

Session 01 introduces the first runnable claw snapshot.

Target framework: .NET 9.

## What this sample shows

- a minimal harness-shaped agent flow via `AsHarnessAgent()`
- a `get_stock_price` tool that reads mock quotes from local JSON
- a placeholder-safe `web_search` tool result
- a todo-list planning step before the final response

## Run the snapshot

From `session-01\code`:

```powershell
dotnet run --project .\MafClaw.Session01.csproj
```

The sample is illustrative only, uses mock data only, and is not financial advice.
