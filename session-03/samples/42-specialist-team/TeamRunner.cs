// Objective: own one bounded main-agent turn and all its background sessions.
// A. Construct distinct specialists with limited tools and real MAF orchestration.
// B. Run with a linked host deadline; log narrative separately from tool evidence.
// C. Always release the named SDK provider, including cancellation and failure paths.

using MafClaw.OrchestrationSupport;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample42;

public static class TeamRunner
{
    public static async Task<TurnResult> RunAsync(
        string prompt, IChatClient? liveClient, TextWriter output, RunLimits? limits = null,
        CancellationToken cancellationToken = default, Func<string, IChatClient>? clientFactory = null)
    {
        limits ??= new RunLimits();
        limits.Validate();
        var transcript = new Transcript(output);
        var data = new MockData();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(limits.TurnDeadline);
        IChatClient Client(string name) => clientFactory?.Invoke(name)
            ?? liveClient ?? new ScriptedInference(name, prompt).CreateClient();

        // A. Three DIFFERENT agents have distinct descriptions and tool allowlists.
        AIAgent[] specialists = new[] { "NewsAgent", "AllocationAgent", "RiskAgent" }
            .Select(name => AgentFactory.CreateWorker(
                name, new WorkerFailureGuard(Client(name)), data, transcript, limits,
                fixture: liveClient is null)).ToArray();
        var backgroundProvider = new BackgroundAgentsProvider(specialists,
            new BackgroundAgentsProviderOptions { WaitTimeout = TimeSpan.FromSeconds(1) });
        var evidence = new CompletionEvidenceClient(Client("MainAgent"), transcript);
        var mainAgent = AgentFactory.CreateMain(evidence, backgroundProvider, transcript, limits);
        var session = await mainAgent.CreateSessionAsync(cancellationToken: deadline.Token);
        string narrative = "";
        bool completed = false;
        bool foregroundFinished = false;
        bool released = false;
        int runningAfterRelease;
        try
        {
            transcript.Write("HOST", "TURN_START", "Educational, mock holdings, not financial advice. New isolated turn.");
            // B. Harness MaximumIterationsPerRequest caps tool loops; this token bounds elapsed time.
            var response = await mainAgent.RunAsync(prompt, session, cancellationToken: deadline.Token);
            narrative = response.Text;
            var lastAssistant = response.Messages.LastOrDefault(message => message.Role == ChatRole.Assistant);
            foregroundFinished = !string.IsNullOrWhiteSpace(narrative) &&
                !(lastAssistant?.Contents.OfType<FunctionCallContent>().Any() ?? false);
            evidence.Observe(response.Messages);
            transcript.Write("MainAgent", "NARRATIVE", narrative);
            completed = foregroundFinished && await evidence.VerifyAsync(prompt, deadline.Token);
            if (!completed)
            {
                transcript.Write("HOST", "INCOMPLETE", "Foreground or specialist verification is incomplete. Narrative alone is not success.");
            }
        }
        catch (OperationCanceledException)
        {
            transcript.Write("HOST", "CANCELLED", "Host deadline or cancellation reached. Partial work is not a completed brief.");
        }
        finally
        {
            // C. WaitTimeout only limits one WAIT. ReleaseSessionAsync actually cancels
            // and awaits child sessions, saving custom task/session cleanup plumbing.
            await backgroundProvider.ReleaseSessionAsync(session, cancelRunning: true,
                timeout: limits.ShutdownTimeout, cancellationToken: CancellationToken.None);
            released = true;
            runningAfterRelease = backgroundProvider.GetIncompleteTasks(session).Count;
            transcript.Write("HOST", "SESSION_RELEASED",
                $"Provider released; running tasks={runningAfterRelease}. This session cannot be reused.");
        }
        return new TurnResult(narrative, completed, released, runningAfterRelease, transcript.Entries)
        {
            ForegroundFinished = foregroundFinished
        };
    }
}
