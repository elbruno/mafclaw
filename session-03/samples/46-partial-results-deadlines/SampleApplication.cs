// Objective: show a main MAF agent orchestrating honest partial research under host policy.
// A. Create minimal agents and fixture adapters. B. Execute bounded foreground turns.
// C. Print uneditable host status and drain all workers before disposing clients.
using MafClaw.OrchestrationSupport;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample46;

public static class SampleApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        var options = CommandOptions.Parse(args);
        if (options.Help)
        {
            Console.WriteLine(CommandOptions.Usage);
            return 0;
        }
        Console.WriteLine(options.Fixture
            ? "Sample 46 — SCRIPTED FIXTURE, real MAF routing, no Azure/network/configuration."
            : "Sample 46 — LIVE MAF model with explicit local educational fault injection.");
        Console.WriteLine("Fake news/holdings; NOT financial advice. Read-only, in-memory work; NOT durable across restart.");
        Console.WriteLine("Custom host wrapper, NOT BackgroundAgentsProvider: concurrency=2, maxAttempts=2.");
        Console.WriteLine(options.Fixture
            ? "Fixture timing: gate-controlled deadline; no elapsed deadline wait."
            : $"Live timing: batch deadline={LiveTiming.BatchDeadline.TotalSeconds}s including queue, injected slow delay={LiveTiming.SlowSourceDelay.TotalSeconds}s, cooperative foreground budget={LiveTiming.ForegroundBudget.TotalSeconds}s.");
        var transcript = new Transcript(Console.Out);
        using var mainClient = options.Fixture ? new FixtureScenario().CreateMainClient() : FoundryConnection.CreateClient();
        var data = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "fixtures", "research.txt"));
        FixtureScenario scenario = new();
        using var workerClient = options.Fixture
            ? new FixtureChatClient((messages, chatOptions, cancellationToken) => scenario.RespondWorkerAsync(messages, chatOptions, cancellationToken))
            : FoundryConnection.CreateClient();
        await using var runner = new DeadlineRunner(
            deadline: options.Fixture ? (duration, cancellationToken) => scenario.DeadlineAsync(duration, cancellationToken) : null,
            reportObserved: outcome =>
            {
                transcript.Write(outcome.Worker, ReportRenderer.StateText(outcome.State), $"Host observed report; attempts={outcome.Attempts}.");
                scenario.Observe(outcome);
            });
        IReadOnlyList<ResearchWork> CreateWork()
        {
            // A. Main sessions remain serial; worker sessions are separate per attempt.
            return CreateResearchWork(workerClient, transcript, data, options.Fixture);
        }
        var tools = new PartialResearchTools(runner, CreateWork,
            options.Fixture ? TimeSpan.FromSeconds(3) : LiveTiming.BatchDeadline);
        var main = CreateMainAgent(mainClient, transcript, tools);
        var session = await main.CreateSessionAsync();

        async Task TurnAsync(string prompt)
        {
            // Each turn gets new barriers; async worker calls retain their original scenario.
            scenario = new FixtureScenario();
            tools.ResetReport();
            using var turn = new CancellationTokenSource(
                options.Fixture ? TimeSpan.FromSeconds(30) : LiveTiming.ForegroundBudget);
            var response = await main.RunAsync(prompt, session, cancellationToken: turn.Token);
            Console.WriteLine($"Main agent narrative (not authoritative status): {response.Text}");
            // C. This report is computed from typed outcomes, never parsed from model text.
            Console.WriteLine(ReportRenderer.Render(tools.Latest));
            if (options.Fixture && tools.Latest.Any(outcome => outcome.State == OutcomeState.TimedOut))
            {
                await scenario.SlowCancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Console.WriteLine("HOST PROOF: two inference calls overlapped; slow fixture observed cancellation after its deadline report.");
            }
        }

        // B. One foreground turn at a time: no concurrent RunAsync on the main AgentSession.
        if (options.Demo || options.Prompt is not null)
        {
            await TurnAsync(options.Prompt ?? "Gather the fictional news, unavailable source and slow holdings research. Report partial results honestly.");
        }
        else
        {
            Console.WriteLine(CommandOptions.Usage);
            while (true)
            {
                Console.Write("> ");
                var prompt = await Console.In.ReadLineAsync();
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
                    Console.WriteLine("Enter a question of up to 4000 characters, or /exit.");
                    continue;
                }
                await TurnAsync(prompt);
            }
        }
        Console.WriteLine("Shutdown: cancellation requested; draining outstanding executions before disposing shared clients.");
        await runner.DisposeAsync();
        Console.WriteLine($"Shutdown complete: {runner.LateFaultsObserved} late worker failures observed; no late results promoted to completed.");
        return 0;
    }

    public static IReadOnlyList<ResearchWork> CreateResearchWork(IChatClient client, Transcript transcript, string data, bool fixture)
    {
        MafResearchWorker Worker(string name) => new(new TracingChatClient(client, name, transcript).AsAIAgent(
            name: name, instructions: $"Read-only fictional education, not financial advice. Use only this local fixture:\n{data}"));
        var news = Worker("NewsResearchAgent");
        var unavailable = Worker("UnavailableResearchAgent");
        var slow = Worker("SlowHoldingsResearchAgent");
        return
        [
            new("NewsResearchAgent", (_, cancellationToken) => news.RunAsync("Summarize fictional news.", cancellationToken)),
            new("UnavailableResearchAgent", async (_, cancellationToken) =>
            {
                // Explicit local adapter failure BEFORE inference, in both modes. This is not a
                // fabricated cloud outage and a model cannot silently convert it into success.
                await Task.FromException(new ExpectedResearchException());
                return await unavailable.RunAsync("Summarize unavailable fictional data.", cancellationToken);
            }),
            new("SlowHoldingsResearchAgent", async (_, cancellationToken) =>
            {
                if (!fixture)
                {
                    // Live demo only: inject a clearly labelled cancellable local adapter delay.
                    await Task.Delay(LiveTiming.SlowSourceDelay, cancellationToken);
                }
                return await slow.RunAsync("Summarize slow fictional holdings.", cancellationToken);
            })
        ];
    }

    public static AIAgent CreateMainAgent(IChatClient client, Transcript transcript, PartialResearchTools tools)
    {
        // Microsoft.Extensions.AI bounds tool invocation; Microsoft.Agents.AI supplies inference
        // and sessions. No Harness mode/skills/web/memory/todos/autoapproval tools are installed.
        var invocation = new FunctionInvokingChatClient(new TracingChatClient(client, "MainAgent", transcript))
        {
            MaximumIterationsPerRequest = 4,
            AllowConcurrentInvocation = false
        };
        return invocation.AsAIAgent(
            name: "MainAgent",
            instructions: """
                You are a read-only finance-education assistant. Call gather_research once to research
                the independent fictional news, unavailable source and slow holdings workers.
                This is a custom HOST policy wrapper, not built-in background_agents tools.
                Preserve successful results. Explicitly acknowledge failures and deadlines as incomplete.
                Tool outcomes are authoritative; never claim all research completed unless they all succeeded.
                Respond briefly; a separate host-rendered status follows your narrative. Not financial advice.
                """,
            tools: [AIFunctionFactory.Create(tools.GatherResearch, "gather_research")]);
    }
}
