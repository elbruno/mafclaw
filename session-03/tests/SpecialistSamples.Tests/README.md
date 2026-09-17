# Specialist sample offline regressions

Dependency-free .NET 10 executable tests for Samples **42** and **43**.
All test data is educational, fictional/mock, and not financial advice.
Run from the public `session-03` directory:

```powershell
dotnet run --project .\tests\SpecialistSamples.Tests\MafClaw.SpecialistSamples.Tests.csproj
```

The process exits nonzero if any group fails. These tests use explicitly
scripted `IChatClient` inference but run the **actual MAF Harness,
BackgroundAgentsProvider, child sessions and read-only tools**:

1. Inspect the six SDK-generated function schemas and terminal release behavior.
2. Force all three Sample 42 specialists to overlap before allowing completion.
3. Verify Sample 43 exact dispatch subsets: none, news only, allocation+risk, all.
4. Inspect actual tool allowlists, returned mock arithmetic and collected outputs.
5. Inject an arbitrary live-path prompt and verify the host does not classify it.
6. Challenge the real Harness iteration limit with an endless scripted model.
7. Stall children until the host deadline, then verify cancellation was awaited.
8. Sanitize synthetic worker failures and release sessions even after failure.
9. Exercise help, finite fixture demos, one-shot prompts, REPL exit and invalid input.
10. Launch isolated live child processes with empty endpoint aliases and nonexistent
    user-secrets roots, proving safe nonzero configuration failure without reading
    the real user's credentials or reaching a model.
11. Construct every worker in live and fixture mode with a non-network probe.
    Live NewsAgent has only `HostedWebSearchTool`; fixture NewsAgent has only
    `read_mock_news`; allocation/risk retain only their deterministic mock tools.
12. Pass synthetic SDK hosted-search/citation content through live NewsAgent to
    verify separate events and source URLs surviving text-only background fan-in.
    No hosted search or network request is performed by this test.
13. Reject narrative-only success, wrong dispatch subsets, uncollected completed
    workers and actual SDK `Failed` tasks. Foreground narrative stays visible;
    completion remains false/nonzero and cleanup still runs.
14. Verify the worker error filter sanitizes expected IO failures but preserves
    unexpected, fatal and cancellation exception instances. Fatal exceptions are
    synthetic test objects; the test does not create actual memory pressure.

No assertions depend on cloud credentials or live model behavior. Fixture files
are copied from the referenced projects into distinct `Fixtures42` and
`Fixtures43` output folders to avoid collisions. The tests do not edit fixtures.
