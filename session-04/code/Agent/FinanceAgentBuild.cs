// Objective: make the agent's process/provider ownership visible to each host.
// A. Expose the built agent, tools and selected profile.
// B. Keep resource disposal in reverse construction order.
// C. Dispose owned clients after asynchronous providers.
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Session04;

public sealed class FinanceAgentBuild(
    AIAgent agent, FinanceTools tools, FinanceHostProfile profile, string workingDirectory,
    IChatClient client, IReadOnlyList<object> resources, BackgroundAgentsProvider? background,
    string memoryMode) : IAsyncDisposable
{
    private readonly List<AgentSession> sessions = [];
    public AIAgent Agent { get; } = agent;
    public FinanceTools Tools { get; } = tools;
    public FinanceHostProfile Profile { get; } = profile;
    public string WorkingDirectory { get; } = workingDirectory;
    public string MemoryMode { get; } = memoryMode;
    public async Task<AgentSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        var session = await Agent.CreateSessionAsync(cancellationToken);
        sessions.Add(session);
        return session;
    }
    public async ValueTask DisposeAsync()
    {
        if (background is not null)
            foreach (var session in sessions)
                await background.ReleaseSessionAsync(session, cancelRunning: true, timeout: TimeSpan.FromSeconds(10));
        foreach (var resource in resources.Reverse())
        {
            if (resource is IAsyncDisposable asynchronous) await asynchronous.DisposeAsync();
            else if (resource is IDisposable synchronous) synchronous.Dispose();
        }
        if (Agent is IDisposable disposableAgent) disposableAgent.Dispose();
        client.Dispose();
    }
}
