// Objective: reject accidental interactive reads in bounded CLI modes.
// A. Guard synchronous input. B. Guard asynchronous input. C. Surface a clear test failure.
namespace MafClaw.LifecycleSamples.Tests;

internal sealed class NoInputReader : TextReader
{
    public override string? ReadLine() =>
        throw new InvalidOperationException("Bounded fixture entry points must not read console input.");

    public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken) =>
        ValueTask.FromException<string?>(new InvalidOperationException("Bounded fixture entry points must not read console input."));
}
