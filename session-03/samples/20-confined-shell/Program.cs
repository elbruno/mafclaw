// Objective: show a confined shell boundary without a model call.
// Steps:
// A. Allow one known command.
// B. Run it in the application folder with a timeout.
// C. Print the inspectable exit code and output.

using System.Diagnostics;

// A. Accept only the configured command.
// B. Run it in a fixed directory with a timeout.
// C. Return an inspectable result instead of raw process access.
// A. Validate the command against the tiny teaching allowlist.
const string command = "dotnet --version";
var allowedCommands = new[] { command };
if (!allowedCommands.Contains(command, StringComparer.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Command rejected by policy.");
}

// B. Re-anchor execution to the application directory and capture output.
using var process = Process.Start(new ProcessStartInfo
{
    FileName = "dotnet",
    ArgumentList = { "--version" },
    WorkingDirectory = AppContext.BaseDirectory,
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true
}) ?? throw new InvalidOperationException("Could not start allowlisted command.");

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await process.WaitForExitAsync(timeout.Token);
// C. Print the result so the boundary is easy to inspect.
Console.WriteLine("Sample 20 - confined shell");
Console.WriteLine($"Command: {command}");
Console.WriteLine($"Exit code: {process.ExitCode}");
Console.WriteLine($"Output: {(await process.StandardOutput.ReadToEndAsync()).Trim()}");
