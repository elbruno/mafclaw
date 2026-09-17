// Objective: replace cloud inference with known synthetic tool requests in tests.
// Steps:
// A. Return the next explicitly supplied test command.
// B. Let the real MAF approval and function-invocation pipeline handle it.
// C. Return a model summary that the host must independently verify.

using Microsoft.Extensions.AI;

namespace MafClaw.Sample21.Tests;

internal sealed class ScriptedChatClient(IReadOnlyList<string> commands) : IChatClient
{
    public int Calls { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var index = Calls++;
        if (index < commands.Count)
        {
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent($"test-call-{index}", "run_shell",
                    new Dictionary<string, object?> { ["command"] = commands[index] })
            ])));
        }
        return Task.FromResult(new ChatResponse(
            new ChatMessage(ChatRole.Assistant, "Model claims everything is finished.")));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("These tests exercise the non-streaming sample.");

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is not null ? null :
        serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("offline-test") :
        serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }
}
