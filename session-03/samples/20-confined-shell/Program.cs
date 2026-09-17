// Objective: show a real command allowlist without a model or approval prompt.
// Steps:
// A. Define host policy independently of the command-line request.
// B. Reject requests that do not exactly match an allowed command.
// C. Execute the approved specification and report its result.

using System.ComponentModel;
using MafClaw.Sample20;

// A. The caller supplies a request, never the list of permitted operations.
CommandSpec[] allowedCommands = [new("dotnet", ["--version"])];
string[] request = args.Length == 0 ? ["dotnet", "--version"] : args;
Console.WriteLine("Sample 20 - command allowlist (plain C#, not a sandbox)");
Console.WriteLine($"Requested: {string.Join(" ", request)}");

// B. Match both the executable and every argument before any process starts.
var approvedCommand = allowedCommands.FirstOrDefault(command => command.Matches(request));
if (approvedCommand is null)
{
    Console.Error.WriteLine("Policy: DENIED. Only dotnet --version is allowed.");
    Console.WriteLine("Process started: no.");
    return 2;
}

Console.WriteLine("Policy: ALLOWED.");
Console.WriteLine($"Command: {approvedCommand.FileName} {string.Join(" ", approvedCommand.Arguments)}");

// C. Pass the matched specification to the launcher; do not rebuild the command.
try
{
    var result = await CommandRunner.RunAsync(
        approvedCommand, AppContext.BaseDirectory, TimeSpan.FromSeconds(5));

    Console.WriteLine($"Process started: yes (PID {result.ProcessId}).");
    Console.WriteLine($"Exit code: {result.ExitCode}");
    Console.WriteLine($"Output: {result.StandardOutput.Trim()}");
    if (!string.IsNullOrWhiteSpace(result.StandardError))
    {
        Console.Error.WriteLine($"Error output: {result.StandardError.Trim()}");
    }

    if (result.TimedOut)
    {
        Console.Error.WriteLine("Timeout: exceeded 5 seconds. The sample-owned process has exited.");
        return 124;
    }

    return result.ExitCode;
}
catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or IOException)
{
    Console.Error.WriteLine($"Execution failed: {exception.Message}");
    return 1;
}
