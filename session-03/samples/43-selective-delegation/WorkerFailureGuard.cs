// Objective: stop raw model exceptions from becoming background tool evidence.
// A. Forward normal non-streaming inference to the configured client.
// B. Preserve cancellation for cooperative shutdown.
// C. Replace failures with a fixed safe diagnostic.

using System.ClientModel;
using System.Text.Json;
using Azure;
using Azure.Identity;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample43;

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
