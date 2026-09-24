// Objective: give Sample 11 an offline model stand-in without faking Harness execution.
// A. Request the teaching tool on the first model call.
// B. Return fixed prose on the second call, or inject an explicit model failure.
// C. Adapt the same responses for streaming and expose the IChatClient service.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace MafClaw.Session04.Samples;

public sealed class LessonChatClient(bool failAfterTool) : IChatClient
{
    private int calls;

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // A. This only proposes a call; MAF must invoke the real function registered in Program.cs.
        var turn = Interlocked.Increment(ref calls);
        if (turn == 1)
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [new FunctionCallContent("lesson-tool", "get_lesson_topic", new Dictionary<string, object?>())])));

        // B. A failed second turn shows that a successful tool does not guarantee a successful agent run.
        if (failAfterTool) throw new InvalidOperationException("Intentional fixture model failure.");
        if (turn != 2) throw new InvalidOperationException("The two-turn fixture is exhausted.");
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
            "MAF defines the agent; the Harness orchestrates its model and tools. [Scripted answer]")));
    }

    // C. Streaming reuses the same fixture; neither path calls a cloud model.
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
