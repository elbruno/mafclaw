// Objective: expose the outbound history used to verify Sample 61's restored MAF session.
// A. Capture messages before delegating to either a scripted or live IChatClient.
// B. Apply the same observation to streaming calls.
// C. Forward service lookup and disposal to the wrapped client.

using Microsoft.Extensions.AI;

namespace MafClaw.Session04.Samples;

public sealed class RecordingChatClient(IChatClient inner) : IChatClient
{
    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // A. Capture the request, not the answer: this is what the persistence assertion inspects.
        LastMessages = messages.ToArray();
        return inner.GetResponseAsync(messages, options, cancellationToken);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // B. Streaming changes response delivery, not what counts as evidence of sent history.
        LastMessages = messages.ToArray();
        return inner.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    // C. Preserve the inner client's services and lifetime; the wrapper adds observation only.
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : inner.GetService(serviceType, serviceKey);

    public void Dispose() => inner.Dispose();
}
