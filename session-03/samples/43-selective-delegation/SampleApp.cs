// Objective: run the zero-worker, news-only and portfolio-only teaching contrast.
// A. Parse options before creating a live cloud connection.
// B. Run one bounded prompt, a finite demo or an isolated-turn REPL.
// C. Clean up console cancellation and return an explicit exit status.

using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample43;

public static class SampleApp
{
    public static async Task<int> RunAsync(string[] args, TextReader input, TextWriter output)
    {
        CliOptions options;
        try
        {
            options = CliOptions.Parse(args);
        }
        catch (ArgumentException exception)
        {
            await output.WriteLineAsync(exception.Message);
            return 2;
        }
        if (options.Help)
        {
            await output.WriteLineAsync("""
                Sample 43: selective delegation using Microsoft Agent Framework BackgroundAgentsProvider.
                Educational, mock holdings, not financial advice.
                --mode live|fixture   Default: live; never silently falls back.
                --prompt <text>       One bounded turn, then exit.
                --demo                Three fixed prompts, then exit (also in live mode).
                --help                Usage without cloud configuration.
                No arguments: live REPL after configuration checks; /exit or EOF exits.
                Fixture mode: SCRIPTED inference through REAL MAF tools, no Azure configuration/network.
                Exact fixture prompts:
                """);
            foreach (string prompt in new[]
                { ScenarioPrompts.Explanation, ScenarioPrompts.News, ScenarioPrompts.Portfolio, ScenarioPrompts.MorningBrief })
            {
                await output.WriteLineAsync($"  {prompt}");
            }
            return 0;
        }
        await output.WriteLineAsync("Sample 43 — educational, mock holdings, not financial advice.");
        await output.WriteLineAsync(options.Fixture
            ? "SCRIPTED FIXTURE INFERENCE: no Azure configuration/network. Actual MAF delegation and tools."
            : "LIVE MODEL INFERENCE: Foundry selects workers from descriptions. NewsAgent searches the public web with sources; allocation and risk use mock holdings.");
        // A. Fixture execution never constructs the cloud client or reads Azure settings.
        using IChatClient? liveClient = options.Fixture ? null : FoundryConnection.CreateClient();
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancel;
        try
        {
            async Task<int> Run(string prompt)
            {
                if (options.Fixture)
                {
                    try
                    {
                        _ = ScenarioPrompts.ScriptedWorkers(prompt);
                    }
                    catch (ArgumentException exception)
                    {
                        await output.WriteLineAsync(exception.Message);
                        return 2;
                    }
                }
                await output.WriteLineAsync($"\nREQUEST: {prompt}");
                var result = await TeamRunner.RunAsync(prompt, liveClient, output,
                    cancellationToken: cancellation.Token);
                return result.ExitCode;
            }
            // B. --demo never enters a forever interactive loop, including live mode.
            if (options.Prompt is not null)
            {
                return await Run(options.Prompt);
            }
            if (options.Demo)
            {
                foreach (string prompt in ScenarioPrompts.Demo)
                {
                    int code = await Run(prompt);
                    if (code != 0)
                    {
                        return code;
                    }
                }
                return 0;
            }
            await output.WriteLineAsync("Each prompt starts an isolated bounded turn. /exit exits.");
            while (!cancellation.IsCancellationRequested)
            {
                await output.WriteAsync("> ");
                string? prompt = await input.ReadLineAsync(cancellation.Token);
                if (prompt is null || prompt.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    continue;
                }
                int code = await Run(prompt);
                if (code != 0)
                {
                    return code;
                }
            }
            return 1;
        }
        finally
        {
            // C. The runner owns provider release; the console owns its Ctrl+C handler.
            Console.CancelKeyPress -= cancel;
        }
    }
}
