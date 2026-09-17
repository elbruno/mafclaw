// Objective: give live inference room to succeed while the injected slow source misses its deadline.
// A. Budget the entire batch including queue time. B. Delay the slow adapter beyond that budget.
// C. Allow foreground orchestration and narration while retaining cooperative cancellation.
namespace MafClaw.Sample46;

public static class LiveTiming
{
    public static readonly TimeSpan BatchDeadline = TimeSpan.FromSeconds(20);
    public static readonly TimeSpan SlowSourceDelay = TimeSpan.FromSeconds(40);
    public static readonly TimeSpan ForegroundBudget = TimeSpan.FromSeconds(60);
}
