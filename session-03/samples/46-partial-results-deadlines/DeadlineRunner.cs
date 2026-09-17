// Objective: implement application deadlines/retries around real workers, not an SDK wait-timeout fiction.
// A. Bound worker concurrency and known retries. B. Stop waiting even if cancellation is ignored.
// C. Retain and observe late executions, then drain before disposing shared dependencies.
using System.Diagnostics;

namespace MafClaw.Sample46;

public sealed class DeadlineRunner : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly SemaphoreSlim slots;
    private readonly CancellationTokenSource lifetime = new();
    private readonly List<Task> batches = [];
    private readonly List<Task> drains = [];
    private readonly Func<TimeSpan, CancellationToken, Task> deadline;
    private readonly Action<WorkerOutcome>? reportObserved;
    private readonly int maxAttempts;
    private Task? shutdown;
    private bool stopping;
    private int lateFaults;

    public DeadlineRunner(
        int concurrency = 2,
        int maxAttempts = 2,
        Func<TimeSpan, CancellationToken, Task>? deadline = null,
        Action<WorkerOutcome>? reportObserved = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrency, 1);
        if (maxAttempts is < 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "The teaching policy permits at most two attempts.");
        }
        slots = new SemaphoreSlim(concurrency);
        this.maxAttempts = maxAttempts;
        this.deadline = deadline ?? ((duration, cancellationToken) => Task.Delay(duration, cancellationToken));
        this.reportObserved = reportObserved;
    }

    public int LateFaultsObserved => Volatile.Read(ref lateFaults);

    public int OutstandingExecutions
    {
        get
        {
            lock (gate)
            {
                return drains.Count(task => !task.IsCompleted);
            }
        }
    }

    public Task<IReadOnlyList<WorkerOutcome>> GatherAsync(
        IEnumerable<ResearchWork> work, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        var items = work.ToArray();
        if (items.Length is < 1 or > 8 || items.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != items.Length)
        {
            throw new ArgumentException("Provide 1-8 independently named workers.", nameof(work));
        }
        lock (gate)
        {
            if (stopping)
            {
                throw new InvalidOperationException("Host is stopping.");
            }
            batches.RemoveAll(task => task.IsCompleted);
            drains.RemoveAll(task => task.IsCompleted);
            var batch = Task.Run(() => GatherCoreAsync(items, timeout, cancellationToken));
            batches.Add(batch);
            return batch;
        }
    }

    private async Task<IReadOnlyList<WorkerOutcome>> GatherCoreAsync(
        ResearchWork[] work, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var waiting = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, cancellationToken);
        // B. One application deadline includes queue time. The injected fixture signal advances
        // policy deterministically; live uses Task.Delay. Neither mechanism aborts SDK inference.
        var deadlineTask = deadline(timeout, waiting.Token);
        try
        {
            return await Task.WhenAll(work.Select(item => RunOneAsync(item, deadlineTask, waiting.Token)));
        }
        finally
        {
            await waiting.CancelAsync();
            try
            {
                await deadlineTask;
            }
            catch (OperationCanceledException) when (waiting.IsCancellationRequested)
            {
            }
        }
    }

    private async Task<WorkerOutcome> RunOneAsync(ResearchWork work, Task deadlineTask, CancellationToken waitingToken)
    {
        var elapsed = Stopwatch.StartNew();
        var attempts = 0;
        // Keep worker callbacks OFF the report-wait token. Cancellation callbacks can themselves
        // ignore cancellation; they must not hold the deadline response hostage.
        var owned = new CancellationTokenSource();
        var execution = Task.Run(async () =>
        {
            var acquired = false;
            try
            {
                await slots.WaitAsync(owned.Token);
                acquired = true;
                // A. Only RetryableResearchException can start a second attempt.
                while (true)
                {
                    owned.Token.ThrowIfCancellationRequested();
                    if (waitingToken.IsCancellationRequested)
                    {
                        return new AttemptResult(OutcomeState.CancellationRequested, null,
                            "Caller/shutdown ended waiting before another attempt started.");
                    }
                    if (deadlineTask.IsCompleted)
                    {
                        return new AttemptResult(OutcomeState.TimedOut, null,
                            "Application deadline expired before another attempt started; no result.");
                    }
                    var attempt = Interlocked.Increment(ref attempts);
                    try
                    {
                        var answer = await work.Execute(attempt, owned.Token);
                        return string.IsNullOrWhiteSpace(answer)
                            ? new AttemptResult(OutcomeState.Failed, null, "Worker returned no research; no fallback result.")
                            : new AttemptResult(OutcomeState.Completed, answer, "Worker returned successfully.");
                    }
                    catch (RetryableResearchException) when (attempt < maxAttempts)
                    {
                    }
                }
            }
            catch (OperationCanceledException) when (owned.IsCancellationRequested)
            {
                return new AttemptResult(OutcomeState.Cancelled, null, "Worker cancellation was observed.");
            }
            catch (ExpectedResearchException)
            {
                return new AttemptResult(OutcomeState.Failed, null, "Expected fictional data-source failure; not retried.");
            }
            catch (RetryableResearchException)
            {
                return new AttemptResult(OutcomeState.Failed, null, $"Known retryable failure exhausted the {maxAttempts}-attempt policy.");
            }
            catch (Exception)
            {
                return new AttemptResult(OutcomeState.Failed, null, "Research unavailable; unclassified errors are not retried.");
            }
            finally
            {
                if (acquired)
                {
                    slots.Release();
                }
            }
        });

        var cancelledWait = Task.Delay(Timeout.InfiniteTimeSpan, waitingToken);
        await Task.WhenAny(execution, deadlineTask, cancelledWait);
        WorkerOutcome outcome;
        var late = !execution.IsCompleted;
        if (!late)
        {
            var result = await execution;
            outcome = new(work.Name, result.State, Volatile.Read(ref attempts), elapsed.Elapsed, result.Result, result.Detail);
        }
        else
        {
            // Cancellation is a request, not proof that work stopped. A deadline is a report
            // outcome, not a terminal SDK task state; late success never rewrites this report.
            var state = waitingToken.IsCancellationRequested ? OutcomeState.CancellationRequested : OutcomeState.TimedOut;
            outcome = new(work.Name, state, Volatile.Read(ref attempts), elapsed.Elapsed, null,
                state == OutcomeState.TimedOut
                    ? "Application deadline ended waiting; cancellation requested. Execution may still be running."
                    : "Caller/shutdown ended waiting; cancellation requested. Execution is not yet observed.");
        }

        // C. Cleanup owns the CTS beyond report return, and observes faults even after timeout.
        var drain = DrainAsync(execution, owned, late);
        lock (gate)
        {
            drains.Add(drain);
        }
        reportObserved?.Invoke(outcome);
        return outcome;
    }

    private async Task DrainAsync(Task<AttemptResult> execution, CancellationTokenSource owned, bool late)
    {
        try
        {
            if (late)
            {
                try
                {
                    await owned.CancelAsync();
                }
                catch (Exception)
                {
                    // Callback failure is observed, not evidence of worker cancellation.
                }
            }
            var observed = await execution;
            if (late && observed.State == OutcomeState.Failed)
            {
                Interlocked.Increment(ref lateFaults);
            }
        }
        finally
        {
            owned.Dispose();
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (gate)
        {
            if (shutdown is null)
            {
                stopping = true;
                var reporting = batches.ToArray();
                shutdown = Task.Run(async () =>
                {
                    try
                    {
                        await lifetime.CancelAsync();
                    }
                    catch (Exception)
                    {
                    }
                    // Reports may finish before workers. Join BOTH, in that order.
                    try
                    {
                        await Task.WhenAll(reporting);
                    }
                    finally
                    {
                        Task[] outstanding;
                        lock (gate)
                        {
                            outstanding = drains.ToArray();
                        }
                        await Task.WhenAll(outstanding);
                        slots.Dispose();
                        lifetime.Dispose();
                    }
                });
            }
            return new ValueTask(shutdown);
        }
    }
}
