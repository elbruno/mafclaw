// Objective: queue a small background task and expose its observable status.
// Steps:
// A. Store a queued ticket.
// B. Complete the ticket after the simulated delay.
// C. Return status without pretending queued work is complete.

using System.Collections.Concurrent;

internal interface IBackgroundTaskRunner
{
    Task<BackgroundTaskTicket> QueueAsync(string name, string description);
}

internal sealed class BackgroundTaskRunner(BackgroundSettings settings) : IBackgroundTaskRunner
{
    private readonly ConcurrentDictionary<string, BackgroundTaskTicket> _tickets = new();

    public Task<BackgroundTaskTicket> QueueAsync(string name, string description)
    {
        // A. Publish the ticket before work continues independently.
        var ticketId = $"bg-{Guid.NewGuid():N}"[..10];
        var queued = new BackgroundTaskTicket(ticketId, name, description, "queued");
        _tickets[ticketId] = queued;
        _ = CompleteLaterAsync(ticketId);
        return Task.FromResult(queued);
    }

    private async Task CompleteLaterAsync(string ticketId)
    {
        // B. Simulate asynchronous work, then update the same ticket.
        await Task.Delay(settings.SimulatedDurationMilliseconds);
        _tickets.AddOrUpdate(
            ticketId,
            _ => throw new InvalidOperationException($"Background ticket '{ticketId}' disappeared."),
            (_, ticket) => ticket with { Status = "completed" });
    }
}

internal sealed record BackgroundTaskTicket(
    string TicketId,
    string Name,
    string Description,
    string Status);
