// Objective: make live/default, offline scripted and one-turn entry points explicit.
// A. Parse only supported flags. B. Reject invalid combinations. C. Print help without loading configuration.
namespace MafClaw.Sample46;

public sealed record CommandOptions(bool Fixture, bool Demo, bool Help, string? Prompt)
{
    public const string Usage = """
        Sample 46: read-only partial research with application deadlines (not financial advice).
        dotnet run -- [--mode live|fixture] [--prompt <text> | --demo] [--help]
        No arguments: live MAF interactive REPL. /exit or EOF cancels and drains owned work.
        --mode fixture: explicitly scripted inference; no Azure/network/configuration.
        --prompt: one foreground turn. --demo: fixed showcase then exit.
        """;

    public static CommandOptions Parse(string[] args)
    {
        var fixture = false;
        var demo = false;
        var help = false;
        string? prompt = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            if (!seen.Add(args[i]))
            {
                throw new ArgumentException("Duplicate option. Use --help.");
            }
            switch (args[i])
            {
                case "--help":
                    help = true;
                    break;
                case "--demo":
                    demo = true;
                    break;
                case "--mode" when i + 1 < args.Length:
                    fixture = args[++i] switch
                    {
                        "fixture" => true,
                        "live" => false,
                        _ => throw new ArgumentException("Mode must be live or fixture.")
                    };
                    break;
                case "--prompt" when i + 1 < args.Length:
                    prompt = args[++i];
                    if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > 4000)
                    {
                        throw new ArgumentException("Prompt must contain 1-4000 characters.");
                    }
                    break;
                default:
                    throw new ArgumentException("Unsupported or incomplete option. Use --help.");
            }
        }
        return demo && prompt is not null
            ? throw new ArgumentException("Choose --prompt or --demo, not both.")
            : new(fixture, demo, help, prompt);
    }
}
