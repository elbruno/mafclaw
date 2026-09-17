// Objective: observe actual MAF tool traffic rather than trust a model's delegation claims.
// Steps:
// A. Inspect tool results entering the next model call.
// B. Observe tool requests returned by the real or explicitly scripted client.
// C. Forward the protocol unchanged; never log instruction or user-message bodies.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace MafClaw.OrchestrationSupport;

public sealed class TracingChatClient(
    IChatClient inner, string agentName, Transcript transcript) : DelegatingChatClient(inner)
{
    private readonly ConditionalWeakTable<AIContent, object> seen = new();

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var history = messages.ToArray();
        Observe(history.SelectMany(message => message.Contents));
        var response = await base.GetResponseAsync(history, options, cancellationToken);
        Observe(response.Messages.SelectMany(message => message.Contents));
        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var history = messages.ToArray();
        Observe(history.SelectMany(message => message.Contents));
        await foreach (var update in base.GetStreamingResponseAsync(history, options, cancellationToken))
        {
            Observe(update.Contents);
            yield return update;
        }
    }

    private void Observe(IEnumerable<AIContent> contents)
    {
        foreach (var content in contents)
        {
            if (content is not (FunctionCallContent or FunctionResultContent))
            {
                continue;
            }
            // Object identity avoids hiding distinct sessions that reuse a fixture call ID.
            lock (seen)
            {
                if (seen.TryGetValue(content, out _))
                {
                    continue;
                }
                seen.Add(content, new object());
            }
            if (content is FunctionCallContent call)
            {
                transcript.Write(agentName, "TOOL_CALL", $"{call.Name} [{call.CallId}] {Describe(call.Arguments)}");
            }
            else if (content is FunctionResultContent result)
            {
                transcript.Write(agentName, "TOOL_RESULT", $"[{result.CallId}] {Describe(result.Result)}");
            }
        }
    }

    private static string Describe(object? value)
    {
        try
        {
            return value is string text ? text : JsonSerializer.Serialize(value);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return $"[transcript cannot render result of type {value?.GetType().Name}; {exception.GetType().Name}]";
        }
    }
}
