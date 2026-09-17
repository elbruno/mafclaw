// Objective: describe one independent, read-only research operation.
// A. Assign a stable name. B. Pass the bounded attempt number. C. Pass an owned cancellation token.
namespace MafClaw.Sample46;

public sealed record ResearchWork(string Name, Func<int, CancellationToken, Task<string>> Execute);
