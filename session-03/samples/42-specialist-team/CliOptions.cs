// Objective: keep live and explicitly scripted fixture execution distinct.
// A. Default to live mode without silently falling back.
// B. Accept either one prompt, a bounded demo, or the REPL.
// C. Reject invalid arguments before touching cloud configuration.

namespace MafClaw.Sample42;

public sealed record CliOptions(bool Fixture, bool Demo, string? Prompt, bool Help)
{
    public static CliOptions Parse(string[] args)
    {
        bool fixture = false, demo = false, help = false;
        string? prompt = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];
            if (!seen.Add(argument))
            {
                throw new ArgumentException("Duplicate option. Use --help.");
            }
            switch (argument)
            {
                case "--mode" when index + 1 < args.Length:
                    fixture = args[++index] switch
                    {
                        "fixture" => true,
                        "live" => false,
                        _ => throw new ArgumentException("Mode must be live or fixture.")
                    };
                    break;
                case "--prompt" when index + 1 < args.Length:
                    prompt = args[++index];
                    if (string.IsNullOrWhiteSpace(prompt) || prompt.StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new ArgumentException("Supply nonempty prompt text.");
                    }
                    break;
                case "--demo":
                    demo = true;
                    break;
                case "--help":
                    help = true;
                    break;
                default:
                    throw new ArgumentException("Invalid option or missing value. Use --help.");
            }
        }
        if (demo && prompt is not null)
        {
            throw new ArgumentException("Choose --demo or --prompt, not both.");
        }
        return new CliOptions(fixture, demo, prompt, help);
    }
}
