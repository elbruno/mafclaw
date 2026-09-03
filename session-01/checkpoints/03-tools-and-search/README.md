# Checkpoint 03 — Tools and Search

[Session 1 home](../../README.md) · Previous: [02 — Meet the Harness](../02-harness-agent/README.md) · Next: [04 — Planning and Todos](../04-planning-and-todos/README.md)

## What this teaches

Give the agent a custom tool. Register a `get_stock_price` function with hardcoded mock data — the model decides when to call it. Hosted web search is enabled by default through the Harness (no code needed).

## What changed from Checkpoint 02

| Change | Detail |
|---|---|
| New file: `StockTools.cs` | A static class with an inline dictionary of 3 ticker prices (MSFT, NVDA, AMZN) and one `[Description]`-annotated method |
| Tools registered | `Tools = [StockTools.GetStockPrice]` added to `ChatOptions` |
| Instructions updated | Now mention `get_stock_price` for stock prices |
| Prompt changed | Asks for the current price of MSFT instead of generic advice |

## What's in the box

| File | Lines | Purpose |
|---|---|---|
| `Program.cs` | 29 | Same Harness pattern, now with a tool in `ChatOptions.Tools` |
| `StockTools.cs` | 25 | `get_stock_price` — 3 hardcoded tickers in a dictionary, no file I/O |
| `.csproj` | ~20 | Same packages as Checkpoint 02 |

## How to run

From `session-01`:

```powershell
dotnet run --project .\checkpoints\03-tools-and-search\MafClaw.Checkpoint03.csproj
```

## What to expect

The model calls `get_stock_price` and prints something like `MSFT: 512.34 USD (mock)`. The number comes from the code, not the model — that's the proof the tool was called.

If the configured model supports hosted web search, you may also see recent market context sourced from the web. No code is needed to enable this — the Harness provides it.

## Mock data

All prices are hardcoded educational fixtures:

| Symbol | Price |
|---|---|
| MSFT | 512.34 USD |
| NVDA | 184.72 USD |
| AMZN | 241.18 USD |

These are not real market quotes.

## Next step

[Checkpoint 04](../04-planning-and-todos/README.md) adds an interactive REPL with the Harness `TodoProvider` and multi-turn conversations.
