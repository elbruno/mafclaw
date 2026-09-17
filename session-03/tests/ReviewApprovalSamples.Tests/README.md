# Focused checks: ordered review and approved reports

Console-style, offline .NET 10 tests for samples **44** and **47**, using injected
scripted `IChatClient` responses through the **actual Microsoft Agent Framework**
agent, function-invocation, and approval pipelines. No Azure configuration,
credentials, network access, trading, or shell execution is needed.

Run from the public repository's `session-03` directory
(the equivalent planning directory is `public-staging\session-03`):

```powershell
dotnet run --project .\tests\ReviewApprovalSamples.Tests\MafClaw.ReviewApprovalSamples.Tests.csproj
```

Coverage: actual predecessor inputs, legal phase ordering, bounded revision and
iteration loops, dishonest reviewers, structural vs. semantic verification,
worker tool isolation, dependency checks, real approval pause/resume, fabricated
SDK approval, denial/EOF, exact hash/bytes, changed/replayed permission, fixed-path
scope, previous-run preservation, independent file verification, cancellation,
and the CLI's explicit live/fixture contract. Both real console `--prompt`
fixture paths are exercised: Sample 44 never reads a second turn; Sample 47
requires an actual console decision and denies on EOF, even when the prompt
claims approval. Unknown modes/flags, missing values, duplicate options,
ambiguous demo/prompt combinations, and out-of-bounds prompts fail before
configuration or client startup.

The approval-request regression inspects the actual Harness instructions and
`save_report` tool description sent to the injected client, then exercises
proposal → SDK approval request → console denial without a write. This prevents
reintroducing a contract that asks the model to wait for human approval before
calling the tool that opens that approval. It checks the prompt/protocol contract,
not a guarantee that every live model will follow the instructions.

Test files live in a unique `.test-workspaces` directory **under the test
executable**, never the system temporary directory. The runner removes only its
own newly allocated run root. Failures return a nonzero process exit code.
