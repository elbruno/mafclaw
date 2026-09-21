// Objective: bound every main-agent and worker model request.
// A. Limit concurrent inference calls.
// B. Link caller cancellation, host shutdown and a request deadline.
// C. Release the slot even when the provider fails.
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace MafClaw.Session04;

public sealed class BoundedChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    private readonly SemaphoreSlim slots = new(3);
    private readonly CancellationTokenSource shutdown = new();

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, shutdown.Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        await slots.WaitAsync(deadline.Token);
        try { return await base.GetResponseAsync(messages, options, deadline.Token); }
        catch (Azure.Identity.AuthenticationFailedException)
        { throw new FinanceUpstreamException("Authentication failed. Sign in privately."); }
        catch (Azure.RequestFailedException exception)
        { throw new FinanceUpstreamException("Azure inference or policy request failed", exception.Status); }
        catch (System.ClientModel.ClientResultException exception)
        { throw new FinanceUpstreamException("Model request failed", exception.Status); }
        catch (HttpRequestException exception)
        { throw new FinanceUpstreamException("Upstream network request failed", (int?)exception.StatusCode); }
        finally { slots.Release(); }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var update in response.ToChatResponseUpdates()) yield return update;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) shutdown.Cancel();
        // Do not dispose the async-only semaphore while cancelled callers unwind.
        base.Dispose(disposing);
    }
}
