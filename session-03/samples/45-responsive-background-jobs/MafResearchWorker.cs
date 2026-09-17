// Objective: run a real Microsoft.Agents.AI worker in a fresh conversation for each job.
// A. Receive the named agent. B. Create per-job AgentSession state. C. Forward the host-owned token.
using Microsoft.Agents.AI;

namespace MafClaw.Sample45;

public sealed class MafResearchWorker(AIAgent agent)
{
    public async Task<string> RunAsync(string question, CancellationToken cancellationToken)
    {
        // B. MAF owns message history; a new AgentSession prevents overlapping jobs
        // from sharing a conversation, even when they use the same named AIAgent.
        var session = await agent.CreateSessionAsync(cancellationToken);
        // C. AIAgent.RunAsync supplies the inference loop, not job scheduling/cancellation policy.
        var response = await agent.RunAsync(question, session, cancellationToken: cancellationToken);
        return string.IsNullOrWhiteSpace(response.Text)
            ? throw new InvalidOperationException("Worker returned no research.")
            : response.Text;
    }
}
