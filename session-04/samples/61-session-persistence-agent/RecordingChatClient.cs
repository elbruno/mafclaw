// Objective: prove that a restored MAF AgentSession actually carries prior turns to the model.
// A. Wrap the real chat client and remember exactly what messages it was asked to answer.
using Microsoft.Extensions.AI;

namespace MafClaw.Session04.Samples;

public sealed class RecordingChatClient(IChatClient inner) : IChatClient
{
    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        LastMessages = messages.ToArray();
        return inner.GetResponseAsync(messages, options, cancellationToken);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        LastMessages = messages.ToArray();
        return inner.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : inner.GetService(serviceType, serviceKey);

    public void Dispose() => inner.Dispose();
}
