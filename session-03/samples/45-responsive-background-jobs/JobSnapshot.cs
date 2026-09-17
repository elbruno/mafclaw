// Objective: expose immutable host facts instead of model-generated job status.
// A. Identify the job. B. Snapshot its state. C. Include only an observed result.
namespace MafClaw.Sample45;

public sealed record JobSnapshot(string Id, string Worker, JobState State, string? Result, bool CancellationWasRequested)
{
    public bool IsTerminal => State is JobState.Completed or JobState.Failed or JobState.Cancelled;

    public string Describe() => $"{Id} {Worker}: {StateText(State)}";

    public static string StateText(JobState state) => state switch
    {
        JobState.CancellationRequested => "cancellation-requested",
        _ => state.ToString().ToLowerInvariant()
    };
}
