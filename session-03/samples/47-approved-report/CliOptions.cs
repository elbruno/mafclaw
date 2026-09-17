// Objective: expose explicit live, fixture, prompt, demo, and help modes.
// A. Default to live inference and a console REPL.
// B. Validate bounded prompts and mutually exclusive options.
// C. Never infer human approval from command-line prompt text.

namespace MafClaw.Sample47;

public sealed record CliOptions(string Mode, string? Prompt, bool Demo, bool Help)
{
    public const string Usage = """
        dotnet run --project public-staging\session-03\samples\47-approved-report -- [--mode live|fixture] [--prompt "text" | --demo] [--help]
        Default: live REPL; /exit quits. --prompt and --demo exit after the showcase.
        Saving requires console input: APPROVE <displayed SHA-256>. Denial/EOF never saves.
        ONLY --mode fixture --demo supplies clearly labelled simulated approval and denial.
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
