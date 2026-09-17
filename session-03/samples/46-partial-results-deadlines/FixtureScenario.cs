// Objective: make partial results deterministic with real MAF calls and explicit local fault injection.
// A. Script main-agent dispatch. B. Gate success and slow inference until both entered.
// C. Trigger the deadline only after success/failure reports are observed.
using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample46;

public sealed class FixtureScenario
{
    private readonly TaskCompletionSource successEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource slowEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource successReported = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource failureReported = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource SlowCancellationObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task DeadlineAsync(TimeSpan _, CancellationToken cancellationToken) => TriggerDeadlineAsync(cancellationToken);

    private async Task TriggerDeadlineAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(successReported.Task, failureReported.Task, slowEntered.Task).WaitAsync(cancellationToken);
    }

    public void Observe(WorkerOutcome outcome)
    {
        if (outcome.Worker == "NewsResearchAgent")
        {
            successReported.TrySetResult();
        }
        if (outcome.Worker == "UnavailableResearchAgent")
        {
            failureReported.TrySetResult();
        }
    }

    public IChatClient CreateMainClient() => new FixtureChatClient((messages, _, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (messages.Last().Role == ChatRole.Tool)
        {
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                "SCRIPTED narrative: useful news survived a failed source and a slow source. The host report is authoritative.")));
        }
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [new FunctionCallContent("gather-fixture", "gather_research", new Dictionary<string, object?>())])));
    });

    public IChatClient CreateWorkerClient() => new FixtureChatClient(RespondWorkerAsync);

    public async Task<ChatResponse> RespondWorkerAsync(IReadOnlyList<ChatMessage> messages, ChatOptions? options, CancellationToken cancellationToken)
    {
        var slow = messages.Last(message => message.Role == ChatRole.User).Text.Contains("slow", StringComparison.OrdinalIgnoreCase);
        if (slow)
        {
            slowEntered.TrySetResult();
            await successEntered.Task.WaitAsync(cancellationToken);
            try
            {
                // B. This real MAF inference never finishes normally in the fixture.
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                SlowCancellationObserved.TrySetResult();
                throw;
            }
        }
        successEntered.TrySetResult();
        await slowEntered.Task.WaitAsync(cancellationToken);
        return new ChatResponse(new ChatMessage(ChatRole.Assistant,
            "SCRIPTED successful fictional ACME news: a training-only product launched. Not financial advice."));
    }
}
