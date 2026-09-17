// Objective: exercise MAF with explicit scripted inference, without a cloud connection.
// Steps:
// A. Accept the sample's deterministic response script.
// B. Count requests and honor cancellation before running that script.
// C. Identify this as a fixture client, never a real model or hosted search.

using Microsoft.Extensions.AI;

namespace MafClaw.OrchestrationSupport;

public sealed class FixtureChatClient(
    Func<IReadOnlyList<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>> responder) : IChatClient
{
    private int calls;
    public int Calls => Volatile.Read(ref calls);

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref calls);
        return responder(messages.ToArray(), options, cancellationToken);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The explicitly scripted fixture exercises non-streaming MAF runs only.");

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is not null ? null :
        serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("mafclaw-scripted-fixture") :
        serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }
}
