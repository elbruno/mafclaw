# Offline lifecycle regression executable

The repository's existing executable-test convention is used: no additional test framework/package or live service is required.

Run from the public repository's `session-03` directory, or `public-staging\session-03` in the planning checkout:

```powershell
dotnet run --project .\tests\LifecycleSamples.Tests\MafClaw.LifecycleSamples.Tests.csproj
```

This builds both new samples and shared support. Exit code `0` means all cases passed; nonzero means a failed invariant. These are executable assertions, so use `dotnet run`, **not** an empty `dotnet test` discovery.

`JobTests` verifies real MAF scripted tool dispatch while both worker inferences block, subsequent console reads/questions, fresh worker conversations, bounded admission/concurrency, deterministic IDs, pending/unknown/repeated collection, cancellation boundaries, cleanup and races.

`DeadlineTests` verifies independent successes survive failure, retry classification and budget, empty-result rejection, queue-time deadline, caller cancellation, late fault observation, safe dependency lifetime and authoritative host reporting after a false model narrative. `CommandTests` protects CLI defaults and validation, asserts Sample 46's live 20-second batch / 40-second slow-source / 60-second foreground budgets, executes both complete fixture `--prompt` and `--demo` paths, asserts their tool traffic and final report states, forbids interactive input in those paths, and checks safe nonzero exits for unknown modes/arguments.

`TaskCompletionSource` gates and a controlled `TextReader` establish ordering. Real-time waits are only ten-second failure watchdogs; no sleeps are used to make assertions pass. Model inference is explicitly scripted via `FixtureChatClient`, but MAF runs actual agents/tools/sessions. All research strings are fictional educational fixtures, not financial advice.
