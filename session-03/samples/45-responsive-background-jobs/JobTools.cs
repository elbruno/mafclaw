// Objective: expose a narrow read-only host tool to the main MAF agent.
// A. Validate named workers. B. Admit a job without awaiting research. C. Return host identity/state.
using System.ComponentModel;

namespace MafClaw.Sample45;

public sealed class JobTools(JobRegistry jobs, IReadOnlyDictionary<string, MafResearchWorker> workers)
{
    [Description("Start read-only research on NewsResearchAgent or HoldingsResearchAgent. Returns immediately with a host job id, not a research result.")]
    public string StartResearch(string worker, string question)
    {
        // A. Models cannot choose arbitrary agents, commands, paths or external tools.
        if (!workers.TryGetValue(worker, out var research))
        {
            return "Error: unknown worker. Use NewsResearchAgent or HoldingsResearchAgent.";
        }
        if (string.IsNullOrWhiteSpace(question) || question.Length > 4000)
        {
            return "Error: supply a question of 1-4000 characters.";
        }
        try
        {
            // B. The body runs real MAF inference later, with its own token and session.
            return jobs.Start(worker, cancellationToken => research.RunAsync(question, cancellationToken)).Describe();
        }
        catch (InvalidOperationException)
        {
            return "Error: host admission unavailable (stopping or retained-job capacity of 8 reached).";
        }
    }
}
