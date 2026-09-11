// Objective: teach queued background work as a ticket, not a completion claim.
// Steps:
// A. Create a ticket for independent work.
// B. Return its identifier and queued status.
// C. Keep the result honest about unfinished work.

// A. Queue independent work.
// B. Return a durable-looking ticket and status immediately.
// C. Never report queued work as completed work.
// A. Return a ticket immediately so queued work is distinguishable from completion.
var ticket = new BackgroundTicket($"bg-{Guid.NewGuid():N}"[..10], "queued", ["MSFT", "NVDA", "SPY"]);

// B/C. Print the ticket fields without inventing a completed result.
Console.WriteLine("Sample 40 - plain background queue");
Console.WriteLine($"Ticket: {ticket.Id}");
Console.WriteLine($"Symbols: {string.Join(", ", ticket.Symbols)}");
Console.WriteLine($"Status: {ticket.Status}");

internal sealed record BackgroundTicket(string Id, string Status, IReadOnlyList<string> Symbols);
