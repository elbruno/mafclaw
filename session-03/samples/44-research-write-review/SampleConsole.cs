// Objective: expose bounded live turns and a clearly scripted showcase.
// A. Select clients only after parsing mode/help.
// B. Create a fresh ordered workflow per console turn.
// C. Distinguish model narrative from host-confirmed workflow evidence.

using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample44;

public static class SampleConsole
{
    public const string DemoPrompt = "Research the fictional classroom allocation, write a report, review it, and revise at most once.";

    public static async Task<int> RunAsync(string[] args, TextReader input, TextWriter output)
    {
        var options = CliOptions.Parse(args);
        if (options.Help)
        {
            output.WriteLine(CliOptions.Usage);
            return 0;
        }
        output.WriteLine("Sample 44 — ordered collaboration. Fictional educational data; not financial advice.");
        output.WriteLine(options.Mode == "fixture"
            ? "FIXTURE: SCRIPTED model output, actual MAF tool dispatch and host state checks. No Azure/config/network."
            : "LIVE inference over read-only MOCK sources (not current news).");

        // Configuration is validated before entering the live REPL.
        using IChatClient? live = options.Mode == "live" ? FoundryConnection.CreateClient() : null;
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, eventArgs) => { eventArgs.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var single = options.Demo ? DemoPrompt : options.Prompt;
            if (single is not null)
            {
                return await RunTurnAsync(single, live, output, stop.Token);
            }
            output.WriteLine("Commands: /exit. Each prompt starts a new workflow (maximum 12 main rounds, 180 seconds).");
            while (!stop.IsCancellationRequested)
            {
                output.Write("\n> ");
                var prompt = await ConsoleInput.ReadLineAsync(input, stop.Token);
                if (prompt is null || prompt.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    continue;
                }
                if (prompt.Length > 4000 || prompt.StartsWith('/'))
                {
                    output.WriteLine("Use a 1-4000 character prompt or /exit.");
                    continue;
                }
                await RunTurnAsync(prompt, live, output, stop.Token);
            }
            return 0;
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
        }
    }

    private static async Task<int> RunTurnAsync(
        string prompt, IChatClient? live, TextWriter output, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(180));
        using var fixtureMain = live is null ? FixtureClients.CreateMain() : null;
        using var fixtureWriter = live is null ? FixtureClients.Writer() : null;
        using var fixtureReviewer = live is null ? FixtureClients.Reviewer() : null;
        var transcript = new Transcript(output);
        using var workflow = new OrderedWorkflow(
            WorkflowAgents.Writer(live ?? fixtureWriter!, transcript),
            WorkflowAgents.Reviewer(live ?? fixtureReviewer!, transcript), transcript, prompt);
        var main = WorkflowAgents.CreateMain(live ?? fixtureMain!, transcript, workflow);
        var session = await main.CreateSessionAsync(deadline.Token);
        var result = await main.RunAsync(prompt, session, cancellationToken: deadline.Token);
        output.WriteLine($"MAIN NARRATIVE (not verification): {result.Text}");
        output.WriteLine($"HOST RESULT: phase={workflow.Phase}; drafts={workflow.DraftCount}; reviews={workflow.ReviewCount}");
        output.WriteLine(workflow.Check?.Summary ?? "HOST CHECK: no draft produced.");
        return workflow.Phase == WorkflowPhase.Complete ? 0 : 2;
    }
}
