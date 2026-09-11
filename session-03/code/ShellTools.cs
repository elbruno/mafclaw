using System.Diagnostics;

internal interface IShellTool
{
    Task<ShellResult> RunAsync(string command, CancellationToken cancellationToken = default);
}

internal sealed class SafeShellTool(ShellSettings settings) : IShellTool
{
    private static readonly HashSet<string> AllowedCommands =
        new(StringComparer.OrdinalIgnoreCase) { "dotnet --version" };

    public async Task<ShellResult> RunAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedCommands.Contains(command))
        {
            throw new InvalidOperationException(
                $"Command '{command}' is not allowed. Configure one of: {string.Join(", ", AllowedCommands)}.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--version");

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("The allowlisted shell process could not be started.");
        }

        var outputTask = ReadOutputAsync(process.StandardOutput, settings.MaxOutputCharacters, cancellationToken);
        var errorTask = ReadOutputAsync(process.StandardError, settings.MaxOutputCharacters, cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"Allowlisted command exceeded the {settings.TimeoutSeconds}-second timeout.");
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        var output = await outputTask;
        var error = await errorTask;
        var combinedOutput = string.IsNullOrWhiteSpace(error)
            ? output.Trim()
            : $"{output.Trim()}{Environment.NewLine}{error.Trim()}".Trim();

        return new ShellResult(
            command,
            process.ExitCode,
            LimitOutput(combinedOutput, settings.MaxOutputCharacters));
    }

    private static string LimitOutput(string output, int maxCharacters)
    {
        return output.Length <= maxCharacters
            ? output
            : $"{output[..maxCharacters]}... [truncated]";
    }

    private static async Task<string> ReadOutputAsync(
        StreamReader reader,
        int maxCharacters,
        CancellationToken cancellationToken)
    {
        var buffer = new char[256];
        var output = new System.Text.StringBuilder();
        while (output.Length <= maxCharacters)
        {
            var read = await reader.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            output.Append(buffer, 0, read);
        }

        return output.ToString();
    }
}

internal sealed record ShellResult(string Command, int ExitCode, string Output);
