// Objective: make the live and scripted modes explicit.
// A. Validate flags and bound the single-turn prompt.
// B. Reject ambiguous combinations.
// C. Leave configuration loading to the live host.

namespace MafClaw.Sample44;

public sealed record CliOptions(string Mode, string? Prompt, bool Demo, bool Help)
{
    public const string Usage = """
        dotnet run --project public-staging\session-03\samples\44-research-write-review -- [--mode live|fixture] [--prompt "text" | --demo] [--help]
        Default: live REPL; /exit quits. --prompt and --demo run one bounded turn.
        Fixture mode is scripted IChatClient output through real MAF, not live inference.
        """;

    public static CliOptions Parse(string[] args)
    {
        var mode = "live";
        string? prompt = null;
        var demo = false;
        var help = false;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            var flag = args[i];
            if (!seen.Add(flag))
            {
                throw new ArgumentException("Duplicate option. Use --help.");
            }
            switch (flag)
            {
                case "--help": help = true; break;
                case "--demo": demo = true; break;
                case "--mode" when i + 1 < args.Length: mode = args[++i]; break;
                case "--prompt" when i + 1 < args.Length: prompt = args[++i]; break;
                default: throw new ArgumentException("Unknown or incomplete option. Use --help.");
            }
        }
        if (mode is not ("live" or "fixture") || (demo && prompt is not null) ||
            (prompt is not null && (string.IsNullOrWhiteSpace(prompt) || prompt.Length > 4000)))
        {
            throw new ArgumentException("Use live or fixture mode, and either --demo or a 1-4000 character --prompt.");
        }
        return new CliOptions(mode, prompt, demo, help);
    }
}
