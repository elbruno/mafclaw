// Objective: teach explicit in-memory host job ownership, not a BackgroundAgentsProvider API.
// A. Bound admission and execution. B. Snapshot/cancel/collect without awaiting workers.
// C. Observe every execution and drain before disposing cancellation sources.
namespace MafClaw.Sample45;

public sealed class JobRegistry(int concurrency = 2, int capacity = 8) : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly Dictionary<string, OwnedJob> jobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim workers = new(concurrency > 0 ? concurrency : throw new ArgumentOutOfRangeException(nameof(concurrency)));
    private readonly int capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
    private int nextId;
    private bool stopping;
    private Task? shutdown;

    // A. Task.Run also prevents a synchronously busy worker body from blocking console admission.
    public JobSnapshot Start(string worker, Func<CancellationToken, Task<string>> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        lock (gate)
        {
            if (stopping)
            {
                throw new InvalidOperationException("Host is stopping; no new jobs are accepted.");
            }
            if (jobs.Count >= capacity)
            {
                throw new InvalidOperationException($"Retained-job capacity ({capacity}) reached. Restart after collecting results.");
            }
            var job = new OwnedJob($"job-{++nextId:000}", worker);
            jobs.Add(job.Id, job);
            job.Execution = Task.Run(() => ExecuteAsync(job, body));
            return job.Snapshot();
        }
    }

    public IReadOnlyList<JobSnapshot> List()
    {
        lock (gate)
        {
            return jobs.Values.Select(job => job.Snapshot()).ToArray();
        }
    }

    public string Collect(string id)
    {
        lock (gate)
        {
            if (!jobs.TryGetValue(id, out var job))
            {
                return $"Error: unknown job '{id}'.";
            }
            return job.State switch
            {
                JobState.Completed => $"{job.Id} completed: {job.Result}",
                JobState.Failed => $"{job.Id} failed: research unavailable; no result.",
                JobState.Cancelled => $"{job.Id} cancelled: cancellation observed; no result.",
                _ => $"{job.Id} pending ({JobSnapshot.StateText(job.State)}); no result yet."
            };
        }
    }

    // B. Intent changes immediately; only ExecuteAsync may declare cancellation terminal.
    public string Cancel(string id)
    {
        lock (gate)
        {
            if (!jobs.TryGetValue(id, out var job))
            {
                return $"Error: unknown job '{id}'.";
            }
            if (job.Snapshot().IsTerminal)
            {
                return $"{job.Id} already {JobSnapshot.StateText(job.State)}.";
            }
            RequestCancellation(job);
            return $"{job.Id} cancellation-requested; worker completion has not yet been observed.";
        }
    }

    public Task WaitForCompletionAsync(string id)
    {
        lock (gate)
        {
            return jobs.TryGetValue(id, out var job)
                ? job.Execution
                : Task.FromException(new ArgumentException("Unknown job id.", nameof(id)));
        }
    }

    private async Task ExecuteAsync(OwnedJob job, Func<CancellationToken, Task<string>> body)
    {
        var acquired = false;
        try
        {
            await workers.WaitAsync(job.Cancellation.Token);
            acquired = true;
            job.Cancellation.Token.ThrowIfCancellationRequested();
            lock (gate)
            {
                if (!job.CancellationWasRequested)
                {
                    job.State = JobState.Running;
                }
            }
            var result = await body(job.Cancellation.Token);
            lock (gate)
            {
                // A worker that returns normally after a request completed; it was not cancelled.
                job.Result = result;
                job.State = JobState.Completed;
            }
        }
        catch (OperationCanceledException) when (job.Cancellation.IsCancellationRequested)
        {
            lock (gate)
            {
                job.State = JobState.Cancelled;
            }
        }
        catch (Exception)
        {
            lock (gate)
            {
                job.State = JobState.Failed;
            }
        }
        finally
        {
            if (acquired)
            {
                workers.Release();
            }
        }
    }

    private static void RequestCancellation(OwnedJob job)
    {
        if (job.CancellationWasRequested)
        {
            return;
        }
        job.CancellationWasRequested = true;
        job.State = JobState.CancellationRequested;
        // CancelAsync marks the token immediately but runs registered callbacks asynchronously.
        // Queued work therefore cannot slip into execution before a separately scheduled signal.
        job.CancellationSignal = ObserveCancellationAsync(job.Cancellation.CancelAsync());
    }

    private static async Task ObserveCancellationAsync(Task signal)
    {
        try
        {
            await signal;
        }
        catch (Exception)
        {
            // A throwing cancellation callback must not leave an unobserved task.
            // It is not proof that the worker has stopped.
        }
    }

    // C. No locks cross awaits. Never dispose dependencies underneath outstanding workers.
    public ValueTask DisposeAsync()
    {
        lock (gate)
        {
            if (shutdown is null)
            {
                stopping = true;
                var retained = jobs.Values.ToArray();
                foreach (var job in retained.Where(job => !job.Snapshot().IsTerminal))
                {
                    RequestCancellation(job);
                }
                shutdown = Task.Run(async () =>
                {
                    await Task.WhenAll(retained.Select(job => job.Execution));
                    await Task.WhenAll(retained.Select(job => job.CancellationSignal));
                    foreach (var job in retained)
                    {
                        job.Cancellation.Dispose();
                    }
                    workers.Dispose();
                });
            }
            return new ValueTask(shutdown);
        }
    }
}
