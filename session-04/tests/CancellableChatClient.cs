// Objective: test request concurrency and cancellation without any network call.
// A. Count requests that acquire an inference slot.
// B. Wait only until the supplied cancellation token fires.
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace MafClaw.Session04.Tests;

internal sealed class CancellableChatClient : IChatClient
{
    private int calls;
    public int Calls => Volatile.Read(ref calls);
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref calls);
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new InvalidOperationException("The cancellable fixture must not complete normally.");
    }
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await GetResponseAsync(messages, options, cancellationToken);
        yield break;
    }
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}
