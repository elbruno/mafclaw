// Objective: verify partial reports, retry classification, deadline/cancellation and late ownership.
// A. Control completion with signals. B. Exercise actual MAF routing. C. Assert honest report and disposal boundaries.
using MafClaw.OrchestrationSupport;
using MafClaw.Sample46;
using Microsoft.Extensions.AI;

namespace MafClaw.LifecycleSamples.Tests;

internal static class DeadlineTests
{
    public static async Task PartialSuccessAndRetryAsync()
    {
        var finishDeadline = Check.Signal();
        var success = Check.Signal();
        var failure = Check.Signal();
        var retry = Check.Signal();
        var slowEntered = Check.Signal();
        var slowCancelled = Check.Signal();
        var failureAttempts = 0;
        await using var runner = new DeadlineRunner(
            deadline: (_, cancellationToken) => finishDeadline.Task.WaitAsync(cancellationToken),
            reportObserved: outcome =>
            {
                if (outcome.Worker == "success") success.TrySetResult();
                if (outcome.Worker == "failure") failure.TrySetResult();
                if (outcome.Worker == "retry") retry.TrySetResult();
            });
        var gather = runner.GatherAsync(
        [
            new("success", (_, _) => Task.FromResult("preserved fictional result")),
            new("failure", (_, _) =>
            {
                Interlocked.Increment(ref failureAttempts);
                throw new ExpectedResearchException();
            }),
            new("retry", (attempt, _) => attempt == 1
                ? Task.FromException<string>(new RetryableResearchException()) : Task.FromResult("recovered fictional result")),
            new("slow", async (_, cancellationToken) =>
            {
                slowEntered.TrySetResult();
                try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); }
                catch (OperationCanceledException) { slowCancelled.TrySetResult(); throw; }
                return "unreachable";
            })
        ], TimeSpan.FromSeconds(3));
        await Check.Within(Task.WhenAll(success.Task, failure.Task, retry.Task, slowEntered.Task));
        finishDeadline.TrySetResult();
        var report = await Check.Within(gather);
        await Check.Within(slowCancelled.Task);
        Check.That(report[0] is { State: OutcomeState.Completed, Result: "preserved fictional result", Attempts: 1 },
            "Sibling failure cannot erase success.");
        Check.That(report[1].State == OutcomeState.Failed && failureAttempts == 1, "Expected permanent failure is not retried.");
        Check.That(report[2] is { State: OutcomeState.Completed, Attempts: 2 }, "Known transient may retry exactly once.");
        Check.That(report[3] is { State: OutcomeState.TimedOut, Result: null }, "Deadline never fabricates a result.");
        Check.That(report.All(outcome => outcome.Elapsed >= TimeSpan.Zero), "Elapsed times must be measured.");
        var text = ReportRenderer.Render(report);
        Check.That(text.Contains("PARTIAL / INCOMPLETE: 2/4") && text.Contains("timed-out"), "Render all outcomes honestly.");
    }

    public static async Task RetryClassificationAsync()
    {
        Check.Throws<ArgumentOutOfRangeException>(() => new DeadlineRunner(maxAttempts: 3));
        await using var runner = new DeadlineRunner();
        var report = await Check.Within(runner.GatherAsync(
        [
            new("always transient", (_, _) => Task.FromException<string>(new RetryableResearchException())),
            new("permanent", (_, _) => Task.FromException<string>(new ExpectedResearchException())),
            new("unknown", (_, _) => Task.FromException<string>(new InvalidOperationException("PRIVATE endpoint"))),
            new("empty", (_, _) => Task.FromResult(""))
        ], TimeSpan.FromMinutes(1)));
        Check.That(report.All(outcome => outcome.State == OutcomeState.Failed && outcome.Result is null), "No failure fallback is success.");
        Check.That(report.Select(outcome => outcome.Attempts).SequenceEqual([2, 1, 1, 1]), "Only known transient failures can retry.");
        Check.That(!ReportRenderer.Render(report).Contains("PRIVATE"), "Raw exception payloads must not be exposed.");
    }

    public static async Task NoncooperativeDeadlineAsync()
    {
        var deadline = Check.Signal();
        var entered = Check.Signal();
        var release = Check.Signal();
        var dependencyDisposed = false;
        var dependencyUsedAfterDeadline = false;
        var runner = new DeadlineRunner(deadline: (_, cancellationToken) => deadline.Task.WaitAsync(cancellationToken));
        var gather = runner.GatherAsync(
        [
            new("ignores token", async (_, _) =>
            {
                entered.TrySetResult();
                await release.Task;
                Check.That(!dependencyDisposed, "Dependency must outlive late worker access.");
                dependencyUsedAfterDeadline = true;
                throw new InvalidOperationException("PRIVATE late error");
            })
        ], TimeSpan.FromSeconds(3));
        await Check.Within(entered.Task);
        deadline.TrySetResult();
        var report = await Check.Within(gather);
        Check.That(report[0] is { State: OutcomeState.TimedOut, Attempts: 1, Result: null }, "Waiting must end without a worker result.");
        Check.That(!release.Task.IsCompleted && runner.OutstandingExecutions == 1, "Noncooperative execution remains owned after report.");
        var shutdown = runner.DisposeAsync().AsTask();
        try
        {
            Check.That(!shutdown.IsCompleted, "Shutdown cannot dispose shared dependencies before the execution settles.");
            Check.Throws<InvalidOperationException>(() => runner.GatherAsync(
                [new("new", (_, _) => Task.FromResult("x"))], TimeSpan.FromSeconds(1)));
        }
        finally
        {
            release.TrySetResult();
            await Check.Within(shutdown);
            dependencyDisposed = true;
        }
        Check.That(dependencyUsedAfterDeadline && runner.LateFaultsObserved == 1 && runner.OutstandingExecutions == 0,
            "Late worker exceptions must be observed before dependencies are disposed.");
        Check.That(report[0].State == OutcomeState.TimedOut, "Late execution may not rewrite the earlier report.");
    }

    public static async Task CancellationAsync()
    {
        var entered = Check.Signal();
        var release = Check.Signal();
        using var caller = new CancellationTokenSource();
        await using var runner = new DeadlineRunner();
        var gather = runner.GatherAsync(
        [
            new("ignores cancellation", async (_, _) => { entered.TrySetResult(); await release.Task; return "late success"; })
        ], TimeSpan.FromMinutes(1), caller.Token);
        try
        {
            await Check.Within(entered.Task);
            await caller.CancelAsync();
            var report = await Check.Within(gather);
            Check.That(report[0] is { State: OutcomeState.CancellationRequested, Result: null },
                "Caller cancellation is neither a deadline nor terminal worker cancellation.");
            release.TrySetResult();
            await runner.DisposeAsync();
            Check.That(report[0].State == OutcomeState.CancellationRequested, "Late success cannot retroactively complete the report.");
        }
        finally
        {
            release.TrySetResult();
        }
    }

    public static async Task QueuedDeadlineAsync()
    {
        var entered = Check.Signal();
        var deadline = Check.Signal();
        var queuedCalls = 0;
        await using var runner = new DeadlineRunner(concurrency: 1, deadline: (_, cancellationToken) => deadline.Task.WaitAsync(cancellationToken));
        // Ensure the first operation owns the only permit before admitting another batch.
        var first = runner.GatherAsync(
            [new("first", async (_, cancellationToken) => { entered.TrySetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); return "unreachable"; })],
            TimeSpan.FromSeconds(3));
        await Check.Within(entered.Task);
        var second = runner.GatherAsync(
            [new("queued", (_, _) => { Interlocked.Increment(ref queuedCalls); return Task.FromResult("must not run"); })],
            TimeSpan.FromSeconds(3));
        deadline.TrySetResult();
        var report = await Check.Within(second);
        await Check.Within(first);
        Check.That(report[0].Attempts == 0 && report[0].State is OutcomeState.TimedOut or OutcomeState.Cancelled,
            "Queue-time deadline must report no executed attempts.");
        Check.That(queuedCalls == 0, "Expired queued work must never begin inference.");
    }

    public static async Task MafRoutingAsync()
    {
        var fixture = new FixtureScenario();
        using var workerClient = fixture.CreateWorkerClient();
        using var mainClient = new FixtureChatClient((messages, _, _) => Task.FromResult(
            messages.Last().Role == ChatRole.Tool
                ? new ChatResponse(new ChatMessage(ChatRole.Assistant, "All workers completed successfully!"))
                : new ChatResponse(new ChatMessage(ChatRole.Assistant,
                    [new FunctionCallContent("gather", "gather_research", new Dictionary<string, object?>())]))));
        await using var runner = new DeadlineRunner(deadline: fixture.DeadlineAsync, reportObserved: fixture.Observe);
        var transcript = new Transcript(TextWriter.Null);
        var tools = new PartialResearchTools(runner,
            () => SampleApplication.CreateResearchWork(workerClient, transcript, "Fictional education. Not financial advice.", fixture: true),
            TimeSpan.FromSeconds(3));
        var main = SampleApplication.CreateMainAgent(mainClient, transcript, tools);
        var session = await main.CreateSessionAsync();
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var narrative = await Check.Within(main.RunAsync("Gather research.", session, cancellationToken: watchdog.Token));
        Check.That(narrative.Text.Contains("All workers"), "Test must inject the intentionally false model narrative.");
        Check.That(tools.Latest.Select(outcome => outcome.State).SequenceEqual(
            [OutcomeState.Completed, OutcomeState.Failed, OutcomeState.TimedOut]), "Real MAF tool routing must preserve typed worker outcomes.");
        Check.That(ReportRenderer.Render(tools.Latest).Contains("PARTIAL / INCOMPLETE: 1/3"),
            "A model's false success claim cannot modify the authoritative host report.");
        var workerCalls = ((FixtureChatClient)workerClient).Calls;
        var repeated = await tools.GatherResearch(CancellationToken.None);
        Check.That(((FixtureChatClient)workerClient).Calls == workerCalls && repeated == ReportRenderer.Render(tools.Latest),
            "Repeated model requests within a turn must reuse the original batch/report.");
        await Check.Within(fixture.SlowCancellationObserved.Task);
    }

    public static async Task ThrowingCallbackAsync()
    {
        var entered = Check.Signal();
        var deadline = Check.Signal();
        var callback = Check.Signal();
        await using var runner = new DeadlineRunner(deadline: (_, cancellationToken) => deadline.Task.WaitAsync(cancellationToken));
        var gather = runner.GatherAsync(
        [
            new("callback fails", async (_, cancellationToken) =>
            {
                using var registration = cancellationToken.Register(() => { callback.TrySetResult(); throw new InvalidOperationException("private"); });
                entered.TrySetResult();
                await callback.Task;
                cancellationToken.ThrowIfCancellationRequested();
                return "unreachable";
            })
        ], TimeSpan.FromSeconds(3));
        await Check.Within(entered.Task);
        deadline.TrySetResult();
        var report = await Check.Within(gather);
        Check.That(report[0].State == OutcomeState.TimedOut, "A cancellation callback exception cannot erase the deadline outcome.");
        await Check.Within(callback.Task);
    }

    public static async Task BlockingCallbackAsync()
    {
        var entered = Check.Signal();
        var deadline = Check.Signal();
        var callbackEntered = Check.Signal();
        var releaseCallback = Check.Signal();
        var releaseWorker = Check.Signal();
        var runner = new DeadlineRunner(deadline: (_, cancellationToken) => deadline.Task.WaitAsync(cancellationToken));
        var gather = runner.GatherAsync(
        [
            new("callback blocks", async (_, cancellationToken) =>
            {
                using var registration = cancellationToken.Register(() =>
                {
                    callbackEntered.TrySetResult();
                    // Deliberately bad external callback: the application must not await it
                    // on the report path. A gate releases it for safe test cleanup.
                    releaseCallback.Task.GetAwaiter().GetResult();
                });
                entered.TrySetResult();
                await releaseWorker.Task;
                return "late answer";
            })
        ], TimeSpan.FromSeconds(3));
        try
        {
            await Check.Within(entered.Task);
            deadline.TrySetResult();
            var report = await Check.Within(gather);
            await Check.Within(callbackEntered.Task);
            Check.That(report[0].State == OutcomeState.TimedOut && !releaseCallback.Task.IsCompleted,
                "A worker callback that blocks cannot delay the deadline report.");
            Check.That(!runner.DisposeAsync().IsCompleted, "Shutdown must still own the blocked callback and worker.");
        }
        finally
        {
            releaseCallback.TrySetResult();
            releaseWorker.TrySetResult();
            await Check.Within(runner.DisposeAsync().AsTask());
        }
    }

    public static async Task ExpiredAdmissionAsync()
    {
        var calls = 0;
        await using var runner = new DeadlineRunner(deadline: (_, _) => Task.CompletedTask);
        var report = await Check.Within(runner.GatherAsync(
            [new("expired", (_, _) => { Interlocked.Increment(ref calls); return Task.FromResult("must not execute"); })],
            TimeSpan.FromSeconds(3)));
        await runner.DisposeAsync();
        Check.That(calls == 0 && report[0] is { State: OutcomeState.TimedOut, Attempts: 0, Result: null },
            "An expired batch cannot start work merely because a worker slot is available.");
    }
}
