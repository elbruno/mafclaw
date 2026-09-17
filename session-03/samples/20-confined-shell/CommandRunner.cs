// Objective: launch an already-approved command and own its process lifetime.
// Steps:
// A. Build process settings from the approved specification.
// B. Capture both streams while enforcing the execution timeout.
// C. Wait for termination and return an inspectable result.

using System.Diagnostics;

namespace MafClaw.Sample20;

internal static class CommandRunner
{
    public static async Task<CommandResult> RunAsync(
        CommandSpec command, string workingDirectory, TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        // A. Process.Start launches; the caller's allowlist already made the policy decision.
        var startInfo = new ProcessStartInfo
        {
            FileName = command.FileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the approved command.");

        // B. Drain both pipes concurrently so a full output buffer cannot block exit.
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(timeout);
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            timedOut = true;
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) when (process.HasExited)
            {
                // The process finished between the timeout and the termination request.
            }

            await process.WaitForExitAsync();
        }

        // C. A timeout result is returned only after the sample-owned process has exited.
        return new CommandResult(
            process.Id, process.ExitCode, await outputTask, await errorTask, timedOut);
    }
}
