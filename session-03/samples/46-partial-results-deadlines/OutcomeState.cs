// Objective: distinguish useful results, observed failures and host waiting policy.
// A. Preserve completed work. B. Separate deadlines from cancellation. C. Never equate a request with observation.
namespace MafClaw.Sample46;

public enum OutcomeState
{
    Completed,
    Failed,
    TimedOut,
    CancellationRequested,
    Cancelled
}
