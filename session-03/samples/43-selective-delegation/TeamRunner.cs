// Objective: let the main model select workers while the host owns budgets and cleanup.
// A. Supply distinct specialists and a named built-in background provider.
// B. Bound one isolated turn without hardcoded live keyword routing.
// C. Always cancel, await and release the provider's child sessions.

using MafClaw.OrchestrationSupport;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample43;

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

        // A. Register available roles, not a host-selected dispatch subset.
        // Only the main model's real background_agents_start_task calls run workers.
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
            // B. MAF Harness bounds function rounds; the host token bounds elapsed time.
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
            // C. BackgroundAgentsProvider owns all child AgentSessions. Its release
            // cancels and awaits work; WaitTimeout alone would leave that work running.
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
