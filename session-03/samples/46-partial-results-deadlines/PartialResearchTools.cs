// Objective: expose host policy as a custom MAF function, distinct from built-in background tools.
// A. Admit independent named work. B. Gather typed outcomes. C. Keep the authoritative report outside the model.
using System.ComponentModel;

namespace MafClaw.Sample46;

public sealed class PartialResearchTools(DeadlineRunner runner, Func<IReadOnlyList<ResearchWork>> createWork, TimeSpan timeout)
{
    private readonly object gate = new();
    private Task<string>? batch;
    public IReadOnlyList<WorkerOutcome> Latest { get; private set; } = [];

    public void ResetReport()
    {
        lock (gate)
        {
            if (batch is { IsCompleted: false })
            {
                throw new InvalidOperationException("Previous foreground research is still reporting.");
            }
            batch = null;
            Latest = [];
        }
    }

    [Description("Run the three independent read-only fictional research workers with application deadlines and at most two known-transient attempts. Returns an honest partial report.")]
    public Task<string> GatherResearch(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            // One batch per foreground turn, even if a model repeats its function call.
            return batch ??= GatherCoreAsync(cancellationToken);
        }
    }

    private async Task<string> GatherCoreAsync(CancellationToken cancellationToken)
    {
        // B. An ordinary AIFunction wrapper owns this policy; no BackgroundAgentsProvider cancellation API is invented.
        Latest = await runner.GatherAsync(createWork(), timeout, cancellationToken);
        return ReportRenderer.Render(Latest);
    }
}
