// Objective: prevent background task failures from embedding raw cloud errors in tool output.
// A. Forward real non-streaming inference requests unchanged.
// B. Preserve cancellation for cooperative deadline and shutdown handling.
// C. Replace model exceptions with a fixed safe worker failure message.

using System.ClientModel;
using System.Text.Json;
using Azure;
using Azure.Identity;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample42;

public sealed class WorkerFailureGuard(IChatClient inner) : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is
            RequestFailedException or ClientResultException or AuthenticationFailedException or
            HttpRequestException or IOException or UnauthorizedAccessException or TimeoutException or
            InvalidOperationException or ArgumentException or JsonException or FormatException)
        {
            throw new InvalidOperationException("Specialist model request failed. Check cloud settings privately.");
        }
    }
}
