// Objective: keep command input independent of background worker completion.
// A. Read the next line after a foreground response. B. Inspect/cancel jobs synchronously.
// C. Await only foreground conversation turns, never unfinished research.
namespace MafClaw.Sample45;

public sealed class ResponsiveConsole(JobRegistry jobs, Func<string, CancellationToken, Task<string>> foreground)
{
    public async Task RunAsync(TextReader input, TextWriter output, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            output.Write("> ");
            var line = await input.ReadLineAsync(cancellationToken);
            if (line is null || line.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase))
            {
                output.WriteLine("Stopping: requesting cancellation and draining owned jobs.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(line))
            {
                await ExecuteAsync(line.Trim(), output, cancellationToken);
            }
        }
    }

    public async Task ExecuteAsync(string input, TextWriter output, CancellationToken cancellationToken = default)
    {
        // B. These commands never await a job and never ask a model to invent its status.
        if (input.Equals("/jobs", StringComparison.OrdinalIgnoreCase))
        {
            var snapshots = jobs.List();
            output.WriteLine(snapshots.Count == 0 ? "No jobs." : string.Join(Environment.NewLine, snapshots.Select(job => job.Describe())));
        }
        else if (input.StartsWith("/collect ", StringComparison.OrdinalIgnoreCase))
        {
            output.WriteLine(jobs.Collect(input[9..].Trim()));
        }
        else if (input.StartsWith("/cancel ", StringComparison.OrdinalIgnoreCase))
        {
            output.WriteLine(jobs.Cancel(input[8..].Trim()));
        }
        else if (input.StartsWith('/'))
        {
            output.WriteLine("Commands: /jobs, /collect <id>, /cancel <id>, /exit.");
        }
        else if (input.Length > 4000)
        {
            output.WriteLine("Error: questions are limited to 4000 characters.");
        }
        else
        {
            // C. Sequential foreground turns share one main session. Worker sessions do not.
            output.WriteLine($"Main agent (narrative, not host status): {await foreground(input, cancellationToken)}");
            output.WriteLine("HOST STATUS: " + string.Join("; ", jobs.List().Select(job => job.Describe())));
        }
    }
}
