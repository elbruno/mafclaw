// A. Queue independent work.
// B. Return a durable-looking ticket and status immediately.
// C. Never report queued work as completed work.
var ticket = new BackgroundTicket($"bg-{Guid.NewGuid():N}"[..10], "queued", ["MSFT", "NVDA", "SPY"]);

Console.WriteLine("Sample 40 - plain background queue");
Console.WriteLine($"Ticket: {ticket.Id}");
Console.WriteLine($"Symbols: {string.Join(", ", ticket.Symbols)}");
Console.WriteLine($"Status: {ticket.Status}");

internal sealed record BackgroundTicket(string Id, string Status, IReadOnlyList<string> Symbols);
