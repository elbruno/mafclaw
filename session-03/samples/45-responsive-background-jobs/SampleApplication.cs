// Objective: wire live or scripted MAF agents to visible host lifecycle management.
// A. Create minimal agents and only the start tool. B. Run a serial foreground session.
// C. Prove responsiveness, then drain jobs before disposing their clients.
using MafClaw.OrchestrationSupport;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample45;

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
            ? "Sample 45 — SCRIPTED FIXTURE, real MAF routing, no Azure/network/configuration."
            : "Sample 45 — LIVE MAF model, local fictional read-only research.");
        Console.WriteLine("Educational fake news/holdings; NOT financial advice. Jobs are in memory, NOT restart-durable.");
        Console.WriteLine("Host-owned registry (not BackgroundAgentsProvider): 2 executing workers, 8 retained jobs.");
        var transcript = new Transcript(Console.Out);
        var fixture = new FixtureScenario(options.Demo);

        // A. Only raw clients are owned here. Shared decorators/agents are borrowed by jobs.
        using var mainClient = options.Fixture ? fixture.CreateMainClient() : FoundryConnection.CreateClient();
        using var workerClient = options.Fixture ? fixture.CreateWorkerClient() : FoundryConnection.CreateClient();
        await using var jobs = new JobRegistry();
        var data = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "fixtures", "research.txt"));
        var namedWorkers = new[] { "NewsResearchAgent", "HoldingsResearchAgent" }.ToDictionary(
            name => name,
            name => new MafResearchWorker(new TracingChatClient(workerClient, name, transcript).AsAIAgent(
                name: name, instructions: $"Use only this fictional local data. No financial advice. Answer briefly.\n{data}")),
            StringComparer.OrdinalIgnoreCase);
        var tools = new JobTools(jobs, namedWorkers);
        var main = CreateMainAgent(mainClient, transcript, tools);
        var session = await main.CreateSessionAsync();
        async Task<string> Foreground(string prompt, CancellationToken cancellationToken)
        {
            using var turn = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            turn.CancelAfter(TimeSpan.FromSeconds(30));
            var response = await main.RunAsync(prompt, session, cancellationToken: turn.Token);
            return response.Text;
        }
        var console = new ResponsiveConsole(jobs, Foreground);

        // B. REPL turns never overlap on the main AgentSession.
        if (options.Demo)
        {
            await console.ExecuteAsync("Start research on fictional ACME using both named research agents.", Console.Out);
            if (options.Fixture)
            {
                await Task.WhenAll(fixture.NewsEntered.Task, fixture.HoldingsEntered.Task).WaitAsync(TimeSpan.FromSeconds(10));
                Console.WriteLine("HOST PROOF: both actual worker inference calls entered; neither has been released.");
            }
            await console.ExecuteAsync("/jobs", Console.Out);
            foreach (var job in jobs.List())
            {
                await console.ExecuteAsync($"/collect {job.Id}", Console.Out);
            }
            await console.ExecuteAsync("What does diversification mean?", Console.Out);
            var holdings = jobs.List().FirstOrDefault(job => job.Worker == "HoldingsResearchAgent");
            if (holdings is not null)
            {
                await console.ExecuteAsync($"/cancel {holdings.Id}", Console.Out);
            }
            fixture.ReleaseNews.TrySetResult();
            foreach (var job in jobs.List())
            {
                await jobs.WaitForCompletionAsync(job.Id);
                await console.ExecuteAsync($"/collect {job.Id}", Console.Out);
            }
            await console.ExecuteAsync("/jobs", Console.Out);
        }
        else if (options.Prompt is not null)
        {
            await console.ExecuteAsync(options.Prompt, Console.Out);
            await console.ExecuteAsync("/jobs", Console.Out);
        }
        else
        {
            Console.WriteLine(CommandOptions.Usage);
            await console.RunAsync(Console.In, Console.Out);
        }
        // C. await using drains jobs before using disposes either underlying IChatClient.
        Console.WriteLine("Shutdown: cancel outstanding work and await observation before disposing clients.");
        await jobs.DisposeAsync();
        foreach (var job in jobs.List())
        {
            Console.WriteLine($"Shutdown observed: {job.Describe()}");
        }
        return 0;
    }

    public static AIAgent CreateMainAgent(IChatClient client, Transcript transcript, JobTools tools)
    {
        // Microsoft.Extensions.AI supplies bounded tool dispatch; AsAIAgent (Microsoft.Agents.AI)
        // supplies session history and inference. No Harness filesystem/web/memory/approval tools.
        var invocation = new FunctionInvokingChatClient(new TracingChatClient(client, "MainAgent", transcript))
        {
            MaximumIterationsPerRequest = 4,
            AllowConcurrentInvocation = false
        };
        return invocation.AsAIAgent(
            name: "MainAgent",
            instructions: """
                You are a read-only finance-education assistant. All data is fictional.
                To research, call start_research on NewsResearchAgent and/or HoldingsResearchAgent.
                Start tools return ids immediately, NOT completed research. Do not await workers.
                After dispatch answer the user briefly; say /jobs and /collect show authoritative host state.
                Never claim a job completed or was cancelled based on your own narrative.
                Other questions can be answered while jobs run. No financial advice.
                """,
            tools: [AIFunctionFactory.Create(tools.StartResearch, "start_research")]);
    }
}
