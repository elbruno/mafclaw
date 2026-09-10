// Session flow:
// A. Ask for y/n with a deadline for each attempt.
// B. Approve or deny immediately on valid input.
// C. Retry only when input is missing or invalid.
// D. Deny safely after the final failed attempt.

using System.Diagnostics;
using System.Text;

internal sealed class TimedApprovalPolicy
{
    private readonly int maxAttempts;
    private readonly TimeSpan timeout;
    private readonly Func<TimeSpan, CancellationToken, Task<string?>> readLineAsync;

    public TimedApprovalPolicy(
        int maxAttempts,
        TimeSpan timeout,
        Func<TimeSpan, CancellationToken, Task<string?>>? readLineAsync = null)
    {
        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Approval attempts must be greater than zero.");
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Approval timeout must be greater than zero.");
        }

        this.maxAttempts = maxAttempts;
        this.timeout = timeout;
        this.readLineAsync = readLineAsync ?? ReadConsoleLineWithTimeoutAsync;
    }

    public async Task<ApprovalDecision> RequestApprovalAsync(
        string toolName,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            Console.Write(
                $"Approve tool {toolName}({arguments})? [y/n] " +
                $"Attempt {attempt}/{maxAttempts}, {timeout.TotalSeconds:0}s timeout: ");

            string? input;
            try
            {
                input = await readLineAsync(timeout, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new ApprovalDecision(false, "Denied automatically because the approval request was cancelled.");
            }
            catch (IOException ex)
            {
                return new ApprovalDecision(false, $"Denied automatically because approval input failed: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return new ApprovalDecision(false, $"Denied automatically because approval input was unavailable: {ex.Message}");
            }

            if (input is null)
            {
                Console.WriteLine(attempt == maxAttempts
                    ? "Timed out. Approval denied after the final attempt."
                    : "Timed out. Retrying approval.");
                continue;
            }

            input = input.Trim();
            if (input.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                return new ApprovalDecision(true, $"Approved by console user on attempt {attempt}.");
            }

            if (input.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                return new ApprovalDecision(false, $"Denied by console user on attempt {attempt}.");
            }

            Console.WriteLine(attempt == maxAttempts
                ? "Invalid response. Approval denied after the final attempt."
                : "Invalid response. Enter y or n; retrying approval.");
        }

        return new ApprovalDecision(false, $"Denied automatically after {maxAttempts} unanswered or invalid attempts.");
    }

    private static async Task<string?> ReadConsoleLineWithTimeoutAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();

        if (Console.IsInputRedirected)
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            try
            {
                var redirectedInput = await Console.In.ReadLineAsync(timeoutSource.Token);
                if (redirectedInput is not null)
                {
                    return redirectedInput.Trim();
                }

                // Redirected EOF is not consent; preserve the same deadline as an interactive timeout.
                var remaining = timeout - Stopwatch.GetElapsedTime(startedAt);
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining, timeoutSource.Token);
                }

                return null;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return null;
            }
        }

        var input = new StringBuilder();

        while (Stopwatch.GetElapsedTime(startedAt) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Console.KeyAvailable)
            {
                await Task.Delay(50, cancellationToken);
                continue;
            }

            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return input.ToString().Trim();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (input.Length > 0)
                {
                    input.Length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                input.Append(key.KeyChar);
                Console.Write(key.KeyChar);
            }
        }

        Console.WriteLine();
        return null;
    }
}
