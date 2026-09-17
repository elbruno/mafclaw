// Objective: render authoritative host facts after any model narrative.
// A. Count successes honestly. B. Include every worker's state/attempts/elapsed time. C. Preserve only observed results.
namespace MafClaw.Sample46;

public static class ReportRenderer
{
    public static string Render(IReadOnlyList<WorkerOutcome> outcomes)
    {
        if (outcomes.Count == 0)
        {
            return "HOST REPORT: no research batch was dispatched; no results.";
        }
        var successCount = outcomes.Count(outcome => outcome.State == OutcomeState.Completed);
        var title = successCount == outcomes.Count ? "COMPLETE" : "PARTIAL / INCOMPLETE";
        var lines = new List<string> { $"HOST REPORT — {title}: {successCount}/{outcomes.Count} workers completed successfully." };
        foreach (var outcome in outcomes)
        {
            lines.Add($"{outcome.Worker}: {StateText(outcome.State)}; attempts={outcome.Attempts}; elapsed={outcome.Elapsed.TotalMilliseconds:F1}ms");
            lines.Add($"  {outcome.Detail}");
            if (outcome.State == OutcomeState.Completed && outcome.Result is not null)
            {
                lines.Add($"  Observed research: {outcome.Result}");
            }
        }
        lines.Add("Host facts above are authoritative. Timed-out work is NOT complete. Fictional education; NOT financial advice.");
        return string.Join(Environment.NewLine, lines);
    }

    public static string StateText(OutcomeState state) => state switch
    {
        OutcomeState.TimedOut => "timed-out",
        OutcomeState.CancellationRequested => "cancellation-requested",
        _ => state.ToString().ToLowerInvariant()
    };
}
