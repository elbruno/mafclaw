// Objective: exercise real MAF plumbing with explicitly scripted inference.
// A. Queue deterministic model responses.
// B. Fail when the script is exhausted instead of inventing a fallback.
// C. Provide a two-turn tool-call fixture for the portfolio calculation.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace MafClaw.Session04;

public sealed class ScriptedChatClient(params ChatResponse[] responses) : IChatClient
{
    // A. Only model responses are scripted; the MAF harness still executes requested tools.
    private readonly ConcurrentQueue<ChatResponse> responses = new(responses);
    private int calls;
    public int Calls => Volatile.Read(ref calls);
    public bool Disposed { get; private set; }
    public IReadOnlyList<AIFunction> LastFunctions { get; private set; } = [];
    public void Enqueue(ChatResponse response) => responses.Enqueue(response);
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(Disposed, this);
        Interlocked.Increment(ref calls);
        LastFunctions = options?.Tools?.OfType<AIFunction>().ToArray() ?? [];

        // Hosting's storage-policy probe observes this same projection in the real OpenAI adapter.
        options?.RawRepresentationFactory?.Invoke(this);

        // B. Exhaustion is a broken fixture, not permission to invent another reply or call a live model.
        if (!responses.TryDequeue(out var response)) throw new InvalidOperationException("Scripted inference is exhausted.");
        return Task.FromResult(response);
    }
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Adapt the same queued reply into streaming updates; this does not add a different model path.
        var response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var update in response.ToChatResponseUpdates()) yield return update;
    }
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() => Disposed = true;
    public static ChatResponse Text(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    // C. First request a real valuation tool call; then return fixed prose (or the intentional bad answer).
    // The fixture ignores tool output, so evaluations must inspect the actual tool receipt separately.
    public static ScriptedChatClient Portfolio(bool wrongAnswer = false) => new(
        new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [new FunctionCallContent("fixture-valuation", "value_portfolio", new Dictionary<string, object?>())])),
        Text(wrongAnswer ? "Snapshot total 1.00 USD. Mock educational data; not financial advice."
            : "Snapshot total 27124.95 USD; Technology 66.01%. Mock educational data; not financial advice."));
}
