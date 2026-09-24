// Objective: replay explicit model responses while the samples execute real MAF orchestration.
// A. Queue the model turns supplied visibly by each sample.
// B. Fail if the fixture is exhausted; never fall back to a live model.
// C. Adapt those same turns to streaming when a host requests it.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace MafClaw.Samples;

public sealed class FixtureChatClient(params ChatResponse[] responses) : IChatClient
{
    // A. Each Program.cs supplies the exact model turns; this helper contains no agent behavior.
    private readonly ConcurrentQueue<ChatResponse> responses = new(responses);

    public static ChatResponse Text(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Hosting checks the same request projection that the real Responses adapter uses.
        cancellationToken.ThrowIfCancellationRequested();
        options?.RawRepresentationFactory?.Invoke(this);

        // B. A fixture supplies inference only: MAF still dispatches any FunctionCallContent to real tools.
        if (!responses.TryDequeue(out var response)) throw new InvalidOperationException("Scripted inference is exhausted.");
        return Task.FromResult(response);
    }

    // C. Streaming is a delivery choice, not a different inference source.
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var update in response.ToChatResponseUpdates()) yield return update;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}
