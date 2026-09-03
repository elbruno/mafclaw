# Checkpoint 04 — Planning and Todos

[Session 1 home](../../README.md) · Previous: [03 — Tools and Search](../03-tools-and-search/README.md) · [Final sample](../../code/README.md)

## What this teaches

Turn the one-shot agent into an interactive assistant with multi-turn conversations and a todo list. The Harness provides `TodoProvider` natively — the code just retrieves it and displays the items. Planning and mode transitions are handled by the Harness through `AgentModeProvider`, not by custom application code.

## What changed from Checkpoint 03

| Change | Detail |
|---|---|
| Interactive REPL | A `while` loop with `Console.ReadLine()` replaces the one-shot prompt |
| `TodoProvider` | Retrieved via `agent.GetService<TodoProvider>()` — tracks multi-step work items created by the model |
| Sessions | `agent.CreateSessionAsync()` enables multi-turn conversation state |
| Instructions expanded | Now mention web search, todos, and multi-step work |
| Commands | `/todos` displays tracked items; `/exit` quits |

## What's in the box

| File | Lines | Purpose |
|---|---|---|
| `Program.cs` | 57 | Interactive REPL with session, `TodoProvider`, `/todos` and `/exit` commands |
| `StockTools.cs` | 25 | Same `get_stock_price` from Checkpoint 03 |
| `.csproj` | ~20 | Same packages as Checkpoint 03 |

## How to run

From `session-01`:

```powershell
dotnet run --project .\checkpoints\04-planning-and-todos\MafClaw.Checkpoint04.csproj
```

## What to expect

```
Finance assistant ready. Commands: /todos, /exit
>
```

Try a multi-step request:

```
Plan how to review MSFT and NVDA, including prices, recent context, and risks.
```

The agent will use `get_stock_price`, may use hosted web search for recent context, and can create todo items to track the work. Use `/todos` to see tracked items:

```
  [ ] Research MSFT fundamentals
  [x] Get MSFT price
```

Use `/exit` to quit.

## Key design points

- **No `/mode` command.** The Harness handles plan/execute transitions natively through `AgentModeProvider`.
- **`DisableFileMemory = true`** is the only Harness feature explicitly disabled.
- **`TodoProvider`** is a reusable Agent Framework context provider configured by the Harness — not a custom tool.
- **No try/catch.** Failures surface as raw exceptions (see [Troubleshooting](../../docs/troubleshooting.md)).

## ⚠️ Privacy note

Because there is no error-handling wrapper, raw Azure exceptions may contain tenant IDs, account names, and resource identifiers. **Do not screen-share the terminal when errors occur** during a live session.

## Next step

The [final sample](../../code/README.md) in `code/` is a copy of this checkpoint with a distinct assembly name — it is the finished Session 1 code.

All stock values are mock educational data. This sample is not financial advice.
