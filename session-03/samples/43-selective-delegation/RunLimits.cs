// Objective: bound delegation in the host rather than rely only on instructions.
// A. Limit function-invocation rounds.
// B. Enforce a deadline and a separate bounded cleanup period.
// C. Reject unsupported execution budgets.

namespace MafClaw.Sample43;

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
