// Objective: give each independent attempt a real MAF worker and isolated conversation.
// A. Receive a named read-only AIAgent. B. Create a new AgentSession. C. Forward owned cancellation.
using Microsoft.Agents.AI;

namespace MafClaw.Sample46;

public sealed class MafResearchWorker(AIAgent agent)
{
    public async Task<string> RunAsync(string question, CancellationToken cancellationToken)
    {
        // B. Microsoft.Agents.AI stores message history; the host must not overlap runs on it.
        var session = await agent.CreateSessionAsync(cancellationToken);
        // C. MAF supplies inference. DeadlineRunner, NOT the SDK, enforces report waiting/retries.
        var response = await agent.RunAsync(question, session, cancellationToken: cancellationToken);
        return response.Text;
    }
}
