// Objective: supply deterministic offline model responses and assertions.
// A. Build synthetic function calls through the shared FixtureChatClient.
// B. Capture actual tool and predecessor messages without Azure.
// C. Fail checks with focused, non-secret diagnostic messages.

using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;

namespace MafClaw.ReviewApprovalSamples.Tests;

internal static class TestSupport
{
    private static int callId;

    public static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static ChatResponse Text(string content) => new(new ChatMessage(ChatRole.Assistant, content));

    public static FunctionCallContent Function(string name, Dictionary<string, object?>? arguments = null) =>
        new($"adversarial-{Interlocked.Increment(ref callId)}", name, arguments ?? new Dictionary<string, object?>());

    public static ChatResponse Calls(params FunctionCallContent[] functions) =>
        new(new ChatMessage(ChatRole.Assistant, functions.Cast<AIContent>().ToList()));

    public static FixtureChatClient Sequence(params ChatResponse[] responses)
    {
        var next = 0;
        return new FixtureChatClient((_, _, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = next++;
            return Task.FromResult(current < responses.Length ? responses[current] : Text("SCRIPTED model claims it is finished."));
        });
    }

    public static FixtureChatClient Constant(string response) => new((_, _, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Text(response));
    });

    public static string Messages(IReadOnlyList<ChatMessage> messages) =>
        string.Join("\n", messages.Select(message => message.Text));

    public static async Task ExpectCancellationAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            return;
        }
        throw new InvalidOperationException("Cancellation was not propagated.");
    }
}
