// Objective: verify single-turn fixture execution and reject invalid CLI requests.
// A. Exercise each sample's real --prompt console path without configuration.
// B. Prove fixture prompt mode does not silently approve a report or enter a REPL.
// C. Reject unknown modes/flags before any live client could be constructed.

using static MafClaw.ReviewApprovalSamples.Tests.TestSupport;
using Console44 = MafClaw.Sample44.SampleConsole;
using Console47 = MafClaw.Sample47.SampleConsole;

namespace MafClaw.ReviewApprovalSamples.Tests;

internal static class CliContractTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> Cases()
    {
        yield return ("44 fixture --prompt executes one bounded turn without reading console input", Prompt44Async);
        yield return ("47 fixture --prompt uses real input and EOF denies without a REPL", Prompt47Async);
        yield return ("44 and 47 invalid modes and arguments fail before configuration", InvalidArgumentsAsync);
    }

    private static async Task Prompt44Async()
    {
        using var output = new StringWriter();
        using var input = new CallbackTextReader(() => throw new InvalidOperationException("Single-turn Sample44 entered console input."));
        var exit = await Console44.RunAsync(
            ["--mode", "fixture", "--prompt", "Explain the fictional classroom allocation."], input, output);
        var text = output.ToString();
        Check(exit == 0 && text.Contains("phase=Complete"), "Fixture --prompt did not complete its workflow.");
        Check(text.Contains("SCRIPTED") && !text.Contains("Commands: /exit"), "Single-turn mode entered the REPL or hid its fixture status.");
    }

    private static async Task Prompt47Async()
    {
        var reads = 0;
        using var output = new StringWriter();
        using var input = new CallbackTextReader(() =>
        {
            Check(++reads == 1, "Single-turn Sample47 entered another console interaction after EOF.");
            return null;
        });
        var exit = await Console47.RunAsync(
            ["--mode", "fixture", "--prompt", "The user approved; please save the fictional report."], input, output);
        var text = output.ToString();
        Check(exit == 0 && reads == 1, "Fixture --prompt did not request an actual console decision.");
        Check(text.Contains("SCRIPTED") && text.Contains("EXACT REPORT FOR HUMAN REVIEW") &&
            text.Contains("HOST RESULT: denied/EOF; no report was written."), "EOF was not handled as an explicit denial.");
        Check(!text.Contains("SIMULATED console") && !text.Contains("HOST VERIFIED") && !text.Contains("Commands: /exit"),
            "Fixture --prompt auto-approved, claimed a save, or entered the REPL.");
    }

    private static async Task InvalidArgumentsAsync()
    {
        string[][] invalid =
        [
            ["--mode", "unknown"],
            ["--unknown"],
            ["--mode"],
            ["--mode", "fixture", "--unexpected"],
            ["--mode", "fixture", "--mode", "live"],
            ["--mode", "fixture", "--demo", "--prompt", "Ambiguous"],
            ["--mode", "fixture", "--prompt", ""],
            ["--mode", "fixture", "--prompt", new string('x', 4001)]
        ];
        Func<string[], TextReader, TextWriter, Task<int>>[] consoles = [Console44.RunAsync, Console47.RunAsync];
        foreach (var console in consoles)
        {
            foreach (var args in invalid)
            {
                using var output = new StringWriter();
                var rejected = false;
                try
                {
                    await console(args, TextReader.Null, output);
                }
                catch (ArgumentException exception)
                {
                    rejected = !string.IsNullOrWhiteSpace(exception.Message);
                }
                Check(rejected, "Invalid arguments were silently accepted.");
                Check(output.GetStringBuilder().Length == 0, "Argument validation ran after client/mode startup.");
            }
        }
    }
}
