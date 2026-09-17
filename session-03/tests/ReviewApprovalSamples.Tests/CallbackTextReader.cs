// Objective: change host state during a deterministic console-review test.
// A. Invoke a test-only callback at the input boundary.
// B. Honor cancellation.
// C. Return explicitly simulated console input, never model text.

namespace MafClaw.ReviewApprovalSamples.Tests;

internal sealed class CallbackTextReader(Func<string?> read) : TextReader
{
    public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(read());
    }
}
