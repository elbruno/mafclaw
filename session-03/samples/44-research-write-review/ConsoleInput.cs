// Objective: let Ctrl+C stop the host even when Console.In reads synchronously.
// A. Move the console read off the orchestration thread.
// B. Bound the wait with the host cancellation token.
// C. Ignore late input after cancellation; the console host exits instead of reusing it.

namespace MafClaw.Sample44;

public static class ConsoleInput
{
    public static async Task<string?> ReadLineAsync(TextReader input, CancellationToken cancellationToken)
    {
        // Console.In's synchronized reader may block inside ReadLineAsync.
        // Cancelling the wait cannot interrupt that OS read, but it does end the
        // host turn; a late line cannot trigger another model/tool operation.
        return await Task.Run(async () => await input.ReadLineAsync(cancellationToken), cancellationToken)
            .WaitAsync(cancellationToken);
    }
}
