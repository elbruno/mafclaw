using System.Diagnostics;

// A. Accept only the configured command.
// B. Run it in a fixed directory with a timeout.
// C. Return an inspectable result instead of raw process access.
const string command = "dotnet --version";
var allowedCommands = new[] { command };
if (!allowedCommands.Contains(command, StringComparer.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Command rejected by policy.");
}

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
Console.WriteLine("Sample 20 - confined shell");
Console.WriteLine($"Command: {command}");
Console.WriteLine($"Exit code: {process.ExitCode}");
Console.WriteLine($"Output: {(await process.StandardOutput.ReadToEndAsync()).Trim()}");
