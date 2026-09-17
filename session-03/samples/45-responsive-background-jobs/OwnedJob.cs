// Objective: keep each task and cancellation source owned by the host.
// A. Store identity. B. Store guarded state. C. Retain execution and cancellation tasks for draining.
namespace MafClaw.Sample45;

internal sealed class OwnedJob(string id, string worker)
{
    public string Id { get; } = id;
    public string Worker { get; } = worker;
    public CancellationTokenSource Cancellation { get; } = new();
    public JobState State { get; set; } = JobState.Queued;
    public string? Result { get; set; }
    public bool CancellationWasRequested { get; set; }
    public Task Execution { get; set; } = Task.CompletedTask;
    public Task CancellationSignal { get; set; } = Task.CompletedTask;

    public JobSnapshot Snapshot() => new(Id, Worker, State, Result, CancellationWasRequested);
}
