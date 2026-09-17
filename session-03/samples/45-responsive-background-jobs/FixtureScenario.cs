// Objective: exercise real MAF routing with deterministic scripted inference and blocked workers.
// A. Script main-agent tool calls. B. Gate two concurrent worker inferences.
// C. Release one and cooperatively cancel the other without timing sleeps.
using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample45;

public sealed class FixtureScenario(bool gated)
{
    public TaskCompletionSource NewsEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource HoldingsEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseNews { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseHoldings { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int WorkerCalls => Volatile.Read(ref workerCalls);
    private int workerCalls;

    public IChatClient CreateMainClient() => new FixtureChatClient((messages, _, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lastUser = messages.LastOrDefault(message => message.Role == ChatRole.User)?.Text ?? "";
        if (messages.Last().Role == ChatRole.Tool)
        {
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                "Scripted: start tools returned. Research may still be pending; use /jobs and /collect.")));
        }
        if (!lastUser.Contains("research", StringComparison.OrdinalIgnoreCase) &&
            !lastUser.Contains("start", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                "Scripted: I can answer this next question while research is blocked. All data is fictional; not financial advice.")));
        }
        // A. FunctionCallContent is the actual Microsoft.Extensions.AI tool protocol consumed by MAF.
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
        [
            new FunctionCallContent("start-news", "start_research", new Dictionary<string, object?>
            {
                ["worker"] = "NewsResearchAgent", ["question"] = "Summarize fictional ACME news."
            }),
            new FunctionCallContent("start-holdings", "start_research", new Dictionary<string, object?>
            {
                ["worker"] = "HoldingsResearchAgent", ["question"] = "Summarize fictional ACME holdings."
            })
        ])));
    });

    public IChatClient CreateWorkerClient() => new FixtureChatClient(async (messages, _, cancellationToken) =>
    {
        Interlocked.Increment(ref workerCalls);
        var holdings = messages.Last(message => message.Role == ChatRole.User).Text.Contains("holdings", StringComparison.OrdinalIgnoreCase);
        (holdings ? HoldingsEntered : NewsEntered).TrySetResult();
        // B. Both entered signals must complete before the demo moves on.
        if (gated)
        {
            await (holdings ? ReleaseHoldings.Task : ReleaseNews.Task).WaitAsync(cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new ChatResponse(new ChatMessage(ChatRole.Assistant,
            holdings ? "SCRIPTED fake ACME holding: 12 educational units. Not financial advice."
                : "SCRIPTED fake ACME news: imaginary training product launch. Not financial advice."));
    });
}
