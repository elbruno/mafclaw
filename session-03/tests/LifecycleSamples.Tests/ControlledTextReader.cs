// Objective: prove the REPL requests more input while worker tasks are still blocked.
// A. Feed explicit command lines. B. Signal every attempted read. C. Represent EOF without sleeps.
using System.Threading.Channels;

namespace MafClaw.LifecycleSamples.Tests;

internal sealed class ControlledTextReader : TextReader
{
    private readonly Channel<string> input = Channel.CreateUnbounded<string>();
    private readonly TaskCompletionSource[] reads = Enumerable.Range(0, 20).Select(_ => Check.Signal()).ToArray();
    private int nextRead;

    public void Send(string line) => Check.That(input.Writer.TryWrite(line), "Input channel must accept a command.");
    public void End() => input.Writer.TryComplete();
    public Task ReadNumber(int number) => reads[number - 1].Task;

    public override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        reads[Interlocked.Increment(ref nextRead) - 1].TrySetResult();
        try
        {
            return await input.Reader.ReadAsync(cancellationToken);
        }
        catch (ChannelClosedException)
        {
            return null;
        }
    }
}
