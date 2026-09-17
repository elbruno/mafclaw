// Objective: verify policy rejection, actual execution and timeout termination.
// Steps:
// A. Exercise the public CLI and exact-match command specification.
// B. Use synthetic child processes to check output, exit codes and lifetime.
// C. Print every result and fail the run if any assertion fails.

using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MafClaw.Sample20.Tests;

internal static class Sample20Tests
{
    public static async Task<int> RunAsync()
    {
        (string Name, Func<Task> Check)[] tests =
        [
            ("default request remains runnable", () => CheckCliAsync([], allowed: true)),
            ("explicit allowed request executes", () => CheckCliAsync(["dotnet", "--version"], allowed: true)),
            ("different argument is denied", () => CheckCliAsync(["dotnet", "--info"], allowed: false)),
            ("different executable is denied", () => CheckCliAsync(["git", "--version"], allowed: false)),
            ("extra arguments are denied", () => CheckCliAsync(["dotnet", "--version", "--info"], allowed: false)),
            ("shell text is not interpreted", () => CheckCliAsync(["dotnet", "--version; echo ignored"], allowed: false)),
            ("one quoted command is not split", () => CheckCliAsync(["dotnet --version"], allowed: false)),
            ("policy rejects incomplete and case-changed requests", CheckExactMatchAsync),
            ("runner uses the supplied command and captures both streams", CheckStreamsAsync),
            ("large streams do not block process exit", CheckLargeOutputAsync),
            ("working directory is applied", CheckWorkingDirectoryAsync),
            ("timeout terminates the owned process", CheckTimeoutAsync),
            ("timeout also terminates its descendant", CheckProcessTreeAsync),
            ("start failures are not returned as success", CheckStartFailureAsync),
            ("invalid timeout is rejected before execution", CheckInvalidTimeoutAsync)
        ];

        var failures = 0;
        foreach (var test in tests)
        {
            try
            {
                await test.Check();
                Console.WriteLine($"PASS: {test.Name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL: {test.Name}{Environment.NewLine}{exception}");
            }
        }

        Console.WriteLine($"{tests.Length - failures}/{tests.Length} Sample 20 checks passed.");
        return failures == 0 ? 0 : 1;
    }

    private static async Task CheckCliAsync(string[] request, bool allowed)
    {
        var testAssembly = typeof(Sample20Tests).Assembly.Location;
        var command = new CommandSpec("dotnet",
        [
            "exec", "--runtimeconfig", Path.ChangeExtension(testAssembly, ".runtimeconfig.json"),
            typeof(CommandSpec).Assembly.Location, .. request
        ]);
        var result = await CommandRunner.RunAsync(command, AppContext.BaseDirectory, TimeSpan.FromSeconds(20));
        Check(!result.TimedOut, "The CLI did not finish.");
        Check(result.ExitCode == (allowed ? 0 : 2), $"Unexpected CLI exit: {result.ExitCode}\n{result.StandardError}");
        if (allowed)
        {
            Check(result.StandardOutput.Contains("Policy: ALLOWED."), "Missing allow decision.");
            Check(result.StandardOutput.Contains("Process started: yes"), "Missing executed-process result.");
            Check(Regex.IsMatch(result.StandardOutput, @"(?m)^Output: \d+\.\d+\.\d+"), "Missing installed SDK version.");
        }
        else
        {
            Check(result.StandardError.Contains("Policy: DENIED."), "Missing explicit rejection.");
            Check(result.StandardOutput.Contains("Process started: no."), "Denied request must return before launch.");
            Check(!result.StandardOutput.Contains("Process started: yes"), "Denied request reported execution.");
            Check(!result.StandardOutput.Contains("Output:"), "Denied request returned process output.");
        }
    }

    private static Task CheckExactMatchAsync()
    {
        var policy = new CommandSpec("dotnet", ["--version"]);
        Check(policy.Matches(["dotnet", "--version"]), "Exact request should match.");
        foreach (string[] request in new string[][] { [], ["dotnet"], ["dotnet", "--VERSION"], ["DOTNET", "--version"] })
        {
            Check(!policy.Matches(request), "Policy must compare all tokens exactly.");
        }
        return Task.CompletedTask;
    }

    private static async Task CheckStreamsAsync()
    {
        var result = await RunFixtureAsync("streams");
        Check(result.ExitCode == 23 && !result.TimedOut, "The actual child exit code was not preserved.");
        Check(result.StandardOutput.Trim() == "fixture output", "Wrong stdout or supplied arguments were ignored.");
        Check(result.StandardError.Trim() == "fixture error", "Wrong stderr.");
    }

    private static async Task CheckLargeOutputAsync()
    {
        var result = await RunFixtureAsync("large-output");
        Check(result.ExitCode == 0 && !result.TimedOut, "A full pipe blocked the child.");
        Check(result.StandardOutput == new string('o', 128 * 1024), "Stdout was lost.");
        Check(result.StandardError == new string('e', 128 * 1024), "Stderr was lost.");
    }

    private static async Task CheckWorkingDirectoryAsync()
    {
        var directory = Path.GetTempPath();
        var result = await CommandRunner.RunAsync(Fixture("directory"), directory, TimeSpan.FromSeconds(20));
        Check(result.ExitCode == 0 && !result.TimedOut, "Directory fixture failed.");
        Check(Path.TrimEndingDirectorySeparator(result.StandardOutput.Trim()) ==
              Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)), "Wrong initial directory.");
    }

    private static async Task CheckTimeoutAsync()
    {
        var elapsed = Stopwatch.StartNew();
        var result = await CommandRunner.RunAsync(Fixture("wait"), AppContext.BaseDirectory, TimeSpan.FromSeconds(3));
        Check(result.TimedOut, "Long-running child did not reach the timeout.");
        Check(elapsed.Elapsed < TimeSpan.FromSeconds(10), "Timeout did not bound the execution.");
        CheckProcessExited(result.ProcessId);
    }

    private static async Task CheckProcessTreeAsync()
    {
        var result = await CommandRunner.RunAsync(Fixture("tree"), AppContext.BaseDirectory, TimeSpan.FromSeconds(3));
        Check(result.TimedOut, "The process-tree fixture did not time out.");
        var descendant = Regex.Match(result.StandardOutput, @"Descendant PID: (\d+)");
        Check(descendant.Success, "The fixture did not start its descendant before timing out.");
        CheckProcessExited(result.ProcessId);
        CheckProcessExited(int.Parse(descendant.Groups[1].Value));
    }

    private static void CheckProcessExited(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            Check(process.HasExited, "Timeout cancelled the wait but left the child running.");
        }
        catch (ArgumentException)
        {
            // The OS has already removed the terminated process from its process table.
        }
    }

    private static async Task CheckStartFailureAsync()
    {
        try
        {
            await CommandRunner.RunAsync(
                new CommandSpec($"mafclaw-missing-{Guid.NewGuid():N}", []),
                AppContext.BaseDirectory, TimeSpan.FromSeconds(5));
            throw new InvalidOperationException("A missing executable was reported as started.");
        }
        catch (Win32Exception)
        {
            // Launch failures must remain observable to the console caller.
        }
    }

    private static async Task CheckInvalidTimeoutAsync()
    {
        try
        {
            await CommandRunner.RunAsync(Fixture("wait"), AppContext.BaseDirectory, TimeSpan.Zero);
            throw new InvalidOperationException("A zero timeout was accepted.");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Invalid limits must be rejected before a child process is created.
        }
    }

    private static CommandSpec Fixture(string mode) =>
        new("dotnet", [typeof(Sample20Tests).Assembly.Location, "--fixture", mode]);

    private static Task<CommandResult> RunFixtureAsync(string mode) =>
        CommandRunner.RunAsync(Fixture(mode), AppContext.BaseDirectory, TimeSpan.FromSeconds(20));

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
