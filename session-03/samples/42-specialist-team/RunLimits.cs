// Objective: put execution budgets in host code, not merely in prompts.
// A. Bound function-invocation rounds and total elapsed time.
// B. Bound shutdown independently from a cancelled turn.
// C. Reject invalid budgets before creating agent sessions.

namespace MafClaw.Sample42;

public sealed record RunLimits(int MaximumIterations = 16, TimeSpan? Deadline = null)
{
    public TimeSpan TurnDeadline => Deadline ?? TimeSpan.FromSeconds(90);
    public TimeSpan ShutdownTimeout => TimeSpan.FromSeconds(5);

    public void Validate()
    {
        if (MaximumIterations is < 1 or > 64 ||
            TurnDeadline <= TimeSpan.Zero || TurnDeadline > TimeSpan.FromMinutes(5))
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumIterations), "Invalid execution budget.");
        }
    }
}
