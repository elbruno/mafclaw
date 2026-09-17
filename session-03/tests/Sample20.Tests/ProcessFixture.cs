// Objective: provide harmless child processes for process-lifetime tests.
// Steps:
// A. Select a synthetic output, directory or long-running fixture.
// B. Emit observable output or wait for the test runner to terminate this process.
// C. Return an explicit status for fixtures that complete.

using System.Diagnostics;

namespace MafClaw.Sample20.Tests;

internal static class ProcessFixture
{
    public static async Task<int> RunAsync(string mode)
    {
        switch (mode)
        {
            case "streams":
                Console.WriteLine("fixture output");
                Console.Error.WriteLine("fixture error");
                return 23;
            case "large-output":
                await Console.Out.WriteAsync(new string('o', 128 * 1024));
                await Console.Error.WriteAsync(new string('e', 128 * 1024));
                return 0;
            case "directory":
                Console.WriteLine(Directory.GetCurrentDirectory());
                return 0;
            case "wait":
                Console.WriteLine("fixture ready");
                await Task.Delay(Timeout.InfiniteTimeSpan);
                return 0;
            case "tree":
                using (var child = Process.Start(new ProcessStartInfo
                {
                    FileName = "dotnet",
                    ArgumentList = { typeof(ProcessFixture).Assembly.Location, "--fixture", "wait" },
                    UseShellExecute = false,
                    CreateNoWindow = true
                }) ?? throw new InvalidOperationException("Could not start the synthetic descendant."))
                {
                    Console.WriteLine($"Descendant PID: {child.Id}");
                    await child.WaitForExitAsync();
                }
                return 0;
            default:
                Console.Error.WriteLine($"Unknown test fixture: {mode}");
                return 2;
        }
    }
}
