// Objective: distinguish work still in flight from observed terminal outcomes.
// A. Track queueing and execution. B. Represent cancellation intent. C. Record observed completion.
namespace MafClaw.Sample45;

public enum JobState
{
    Queued,
    Running,
    Completed,
    Failed,
    CancellationRequested,
    Cancelled
}
