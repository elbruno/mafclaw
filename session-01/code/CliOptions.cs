namespace MafClaw.Session01;

internal enum RuntimeMode
{
    Live,
    Offline
}

internal sealed record CliOptions(RuntimeMode Mode, string? Scenario)
{
    public static bool TryParse(string[] args, out CliOptions options, out string? error)
    {
        options = default!;
        error = null;

        string? modeValue = null;
        string? scenario = null;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--mode":
                    if (!TryReadValue(args, ref index, out modeValue))
                    {
                        error = "Missing value for --mode.";
                        return false;
                    }

                    break;
                case "--scenario":
                    if (!TryReadValue(args, ref index, out scenario))
                    {
                        error = "Missing value for --scenario.";
                        return false;
                    }

                    break;
                default:
                    error = $"Unknown argument: {argument}";
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(modeValue))
        {
            error = "--mode is required.";
            return false;
        }

        if (!TryParseMode(modeValue, out var mode))
        {
            error = "--mode must be either 'live' or 'offline'.";
            return false;
        }

        if (mode == RuntimeMode.Live && !string.IsNullOrWhiteSpace(scenario))
        {
            error = "--scenario is supported only with --mode offline.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(scenario) &&
            !string.Equals(scenario, "stock", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(scenario, "plan", StringComparison.OrdinalIgnoreCase))
        {
            error = "--scenario must be either 'stock' or 'plan'.";
            return false;
        }

        options = new CliOptions(mode, scenario);
        return true;
    }

    public static void PrintUsage(TextWriter writer)
    {
        writer.WriteLine("Usage:");
        writer.WriteLine("  dotnet run --project .\\MafClaw.Session01.csproj -- --mode live");
        writer.WriteLine("  dotnet run --project .\\MafClaw.Session01.csproj -- --mode offline [--scenario stock|plan]");
    }

    private static bool TryParseMode(string modeValue, out RuntimeMode mode)
    {
        if (string.Equals(modeValue, "live", StringComparison.OrdinalIgnoreCase))
        {
            mode = RuntimeMode.Live;
            return true;
        }

        if (string.Equals(modeValue, "offline", StringComparison.OrdinalIgnoreCase))
        {
            mode = RuntimeMode.Offline;
            return true;
        }

        mode = default;
        return false;
    }

    private static bool TryReadValue(string[] args, ref int index, out string value)
    {
        var nextIndex = index + 1;
        if (nextIndex >= args.Length)
        {
            value = string.Empty;
            return false;
        }

        value = args[nextIndex];
        index = nextIndex;
        return true;
    }
}
