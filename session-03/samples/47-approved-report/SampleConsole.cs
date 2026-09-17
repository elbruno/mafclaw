// Objective: run bounded report turns without silently authorizing live saves.
// A. Load Foundry only for live mode, before the REPL.
// B. Permit simulated input only in the explicitly requested fixture demo.
// C. Create unique mock run paths and distinguish denial from verified saving.

using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample47;

public static class SampleConsole
{
    public const string DemoPrompt = "Analyze the fictional classroom allocation and risk, propose an educational report, and ask to save it.";

    public static async Task<int> RunAsync(string[] args, TextReader input, TextWriter output)
    {
        var options = CliOptions.Parse(args);
        if (options.Help)
        {
            output.WriteLine(CliOptions.Usage);
            return 0;
        }
        output.WriteLine("Sample 47 — human-approved mock REPORT. Fictional data; not financial advice. No trading.");
        output.WriteLine(options.Mode == "fixture"
            ? "FIXTURE: SCRIPTED inference through actual MAF orchestration. No Azure/config/network."
            : "LIVE inference over MOCK observations. Saving ALWAYS requires your actual console response.");
        using IChatClient? live = options.Mode == "live" ? FoundryConnection.CreateClient() : null;
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, eventArgs) => { eventArgs.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            if (options is { Mode: "fixture", Demo: true })
            {
                output.WriteLine("\nEXPLICIT FIXTURE DEMO: known SIMULATED console approval (not a human or live model).");
                using var approve = new StringReader($"APPROVE {FixtureClients.ReportHash}\n");
                var approved = await RunTurnAsync(DemoPrompt, null, approve, output, stop.Token);
                output.WriteLine("\nEXPLICIT FIXTURE DEMO: known SIMULATED console denial; the earlier approved run is preserved.");
                using var deny = new StringReader("DENY\n");
                var denied = await RunTurnAsync(DemoPrompt, null, deny, output, stop.Token);
                return approved == 0 && denied == 0 ? 0 : 2;
            }
            var single = options.Demo ? DemoPrompt : options.Prompt;
            if (single is not null)
            {
                return await RunTurnAsync(single, live, input, output, stop.Token);
            }
            output.WriteLine("Commands: /exit. Each prompt starts a fresh run. Only exact console approval can save.");
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
                await RunTurnAsync(prompt, live, input, output, stop.Token);
            }
            return 0;
        }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static async Task<int> RunTurnAsync(
        string prompt, IChatClient? live, TextReader input, TextWriter output, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(240));
        using var fixtureMain = live is null ? FixtureClients.CreateMain() : null;
        using var fixtureAllocation = live is null ? FixtureClients.Worker("AllocationWorker") : null;
        using var fixtureRisk = live is null ? FixtureClients.Worker("RiskWorker") : null;
        var transcript = new Transcript(output);
        // The executable's mock working area is host-selected. Unique subfolders
        // preserve previous runs; denied runs create no directory or report.
        var workspace = new ReportWorkspace(Path.Combine(AppContext.BaseDirectory, "mock-workspaces"));
        var store = new ReportStore(workspace, transcript);
        using var workflow = new AnalysisWorkflow(
            ReportAgents.Worker(live ?? fixtureAllocation!, transcript, "AllocationWorker"),
            ReportAgents.Worker(live ?? fixtureRisk!, transcript, "RiskWorker"), store, transcript);
        var main = ReportAgents.CreateMain(live ?? fixtureMain!, transcript, workflow);
        var verified = await ApprovalConsole.RunAsync(main, store, prompt, input, output, transcript, deadline.Token);
        output.WriteLine(verified ? "HOST RESULT: approved report saved and independently verified."
            : store.Denied ? "HOST RESULT: denied/EOF; no report was written." : "HOST RESULT: no verified report save.");
        return verified || store.Denied ? 0 : 2;
    }
}
