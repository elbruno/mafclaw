// Objective: bound approval waiting even when Console.In reads synchronously.
// A. Move the console read off the orchestration thread.
// B. Stop waiting when the turn's cancellation/deadline fires.
// C. Never interpret a late line as authorization after the host has stopped.

namespace MafClaw.Sample47;

public static class ConsoleInput
{
    public static async Task<string?> ReadLineAsync(TextReader input, CancellationToken cancellationToken)
    {
        // Console.In's synchronized reader may block inside ReadLineAsync.
        // Cancelling the wait cannot interrupt that OS read. The CLI exits on
        // cancellation, and only the awaited result below can authorize a save.
        return await Task.Run(async () => await input.ReadLineAsync(cancellationToken), cancellationToken)
            .WaitAsync(cancellationToken);
    }
}
