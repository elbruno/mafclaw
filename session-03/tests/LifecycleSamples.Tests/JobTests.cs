// Objective: verify host-owned responsive jobs using gates instead of scheduling sleeps.
// A. Exercise real MAF routing. B. Check lifecycle boundaries and capacity. C. Verify cleanup and session isolation.
using System.Collections.Concurrent;
using MafClaw.OrchestrationSupport;
using MafClaw.Sample45;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.LifecycleSamples.Tests;

internal static class JobTests
{
    public static async Task ResponsiveMafConsoleAsync()
    {
        var fixture = new FixtureScenario(gated: true);
        using var mainClient = fixture.CreateMainClient();
        using var workerClient = fixture.CreateWorkerClient();
        await using var jobs = new JobRegistry();
        var transcript = new Transcript(TextWriter.Null);
        var workers = new[] { "NewsResearchAgent", "HoldingsResearchAgent" }.ToDictionary(
            name => name, name => new MafResearchWorker(workerClient.AsAIAgent(name: name)));
        var main = SampleApplication.CreateMainAgent(mainClient, transcript, new JobTools(jobs, workers));
        var mainSession = await main.CreateSessionAsync();
        var console = new ResponsiveConsole(jobs, async (prompt, cancellationToken) =>
            (await main.RunAsync(prompt, mainSession, cancellationToken: cancellationToken)).Text);
        using var input = new ControlledTextReader();
        using var output = new StringWriter();
        var run = console.RunAsync(input, output);
        try
        {
            input.Send("Start research.");
            await Check.Within(Task.WhenAll(fixture.NewsEntered.Task, fixture.HoldingsEntered.Task, input.ReadNumber(2)));
            Check.That(fixture.WorkerCalls == 2, "Both real MAF worker calls must have entered.");
            input.Send("/jobs");
            await Check.Within(input.ReadNumber(3));
            input.Send("/collect job-001");
            await Check.Within(input.ReadNumber(4));
            input.Send("A second independent question.");
            await Check.Within(input.ReadNumber(5));
            Check.That(!fixture.ReleaseNews.Task.IsCompleted && !fixture.ReleaseHoldings.Task.IsCompleted,
                "REPL must read/answer next question BEFORE either worker gate is released.");
            Check.That(output.ToString().Contains("pending (running)") &&
                output.ToString().Contains("answer this next question"), "Expected pending and a foreground response.");
            input.Send("/cancel job-002");
            await Check.Within(input.ReadNumber(6));
            await Check.Within(jobs.WaitForCompletionAsync("job-002"));
            Check.That(jobs.List()[1].State == JobState.Cancelled, "Cooperative MAF cancellation must be observed.");
            fixture.ReleaseNews.TrySetResult();
            await Check.Within(jobs.WaitForCompletionAsync("job-001"));
            Check.That(jobs.Collect("job-001").Contains("SCRIPTED fake ACME news"), "Actual MAF result must be retained.");
            input.End();
            await Check.Within(run);
        }
        finally
        {
            input.End();
            fixture.ReleaseNews.TrySetResult();
            fixture.ReleaseHoldings.TrySetResult();
            await Check.Within(run);
        }
    }

    public static async Task CapacityAndCollectionAsync()
    {
        await using var jobs = new JobRegistry(concurrency: 2, capacity: 3);
        var enteredA = Check.Signal();
        var enteredB = Check.Signal();
        var release = Check.Signal();
        var queuedCalls = 0;
        var a = jobs.Start("A", async cancellationToken => { enteredA.TrySetResult(); await release.Task.WaitAsync(cancellationToken); return "A"; });
        var b = jobs.Start("B", async cancellationToken => { enteredB.TrySetResult(); await release.Task.WaitAsync(cancellationToken); return "B"; });
        try
        {
            await Check.Within(Task.WhenAll(enteredA.Task, enteredB.Task));
            var c = jobs.Start("C", _ => { Interlocked.Increment(ref queuedCalls); return Task.FromResult("C"); });
            Check.That(a.Id == "job-001" && b.Id == "job-002" && c.Id == "job-003", "Ids must be stable and monotonic.");
            Check.That(c.State == JobState.Queued && jobs.List().Count(job => job.State == JobState.Running) == 2,
                "Two active workers must leave a third queued.");
            Check.Throws<InvalidOperationException>(() => jobs.Start("D", _ => Task.FromResult("D")));
            Check.That(jobs.Collect(c.Id).Contains("pending (queued)"), "Queued collection cannot fabricate a result.");
            Check.That(jobs.Collect("missing").StartsWith("Error: unknown") && jobs.Cancel("missing").StartsWith("Error: unknown"),
                "Unknown ids must be explicit errors.");
            jobs.Cancel(c.Id);
            await Check.Within(jobs.WaitForCompletionAsync(c.Id));
            Check.That(jobs.List()[2].State == JobState.Cancelled && queuedCalls == 0, "Cancelled queued work cannot invoke its body.");
            release.TrySetResult();
            await Check.Within(Task.WhenAll(jobs.WaitForCompletionAsync(a.Id), jobs.WaitForCompletionAsync(b.Id)));
            Check.That(jobs.Collect(a.Id) == jobs.Collect(a.Id) && jobs.Collect(a.Id).EndsWith("completed: A"),
                "Repeated collection preserves the same observed result.");
            Check.Throws<InvalidOperationException>(() => jobs.Start("D", _ => Task.FromResult("D")));
        }
        finally
        {
            release.TrySetResult();
        }
    }

    public static async Task CancellationBoundaryAsync()
    {
        await using var jobs = new JobRegistry();
        var entered = Check.Signal();
        var release = Check.Signal();
        var id = jobs.Start("ignores cancellation", async _ =>
        {
            entered.TrySetResult();
            await release.Task;
            return "actually returned";
        }).Id;
        try
        {
            await Check.Within(entered.Task);
            jobs.Cancel(id);
            jobs.Cancel(id);
            Check.That(jobs.List()[0].State == JobState.CancellationRequested, "Intent is not terminal cancellation.");
            Check.That(jobs.Collect(id).Contains("pending (cancellation-requested)"), "Requested work remains pending.");
            release.TrySetResult();
            await Check.Within(jobs.WaitForCompletionAsync(id));
            Check.That(jobs.List()[0] is { State: JobState.Completed, CancellationWasRequested: true },
                "An ignored request followed by a normal result is completion, not cancellation.");
            Check.That(jobs.Cancel(id).Contains("already completed"), "Late cancel must not rewrite completion.");
        }
        finally
        {
            release.TrySetResult();
        }
        var cooperativeEntered = Check.Signal();
        var cooperative = jobs.Start("cooperative", async cancellationToken =>
        {
            cooperativeEntered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return "unreachable";
        }).Id;
        await Check.Within(cooperativeEntered.Task);
        jobs.Cancel(cooperative);
        await Check.Within(jobs.WaitForCompletionAsync(cooperative));
        Check.That(jobs.List()[1].State == JobState.Cancelled, "Only observed cancellation is terminal.");
    }

    public static async Task ShutdownAndFaultsAsync()
    {
        var jobs = new JobRegistry();
        var entered = Check.Signal();
        var release = Check.Signal();
        var id = jobs.Start("late failure", async _ =>
        {
            entered.TrySetResult();
            await release.Task;
            throw new InvalidOperationException("PRIVATE cloud endpoint must never be printed");
        }).Id;
        await Check.Within(entered.Task);
        var shutdown = jobs.DisposeAsync().AsTask();
        try
        {
            Check.That(!shutdown.IsCompleted, "Shutdown must not dispose resources while a worker still runs.");
            Check.Throws<InvalidOperationException>(() => jobs.Start("rejected", _ => Task.FromResult("x")));
        }
        finally
        {
            release.TrySetResult();
            await Check.Within(shutdown);
        }
        Check.That(jobs.List()[0].State == JobState.Failed && !jobs.Collect(id).Contains("PRIVATE"),
            "Observe late faults and render only sanitized failure detail.");
        await jobs.DisposeAsync();
    }

    public static async Task DistinctMafSessionsAsync()
    {
        var messagesSeen = new ConcurrentBag<ChatMessage[]>();
        var sessionsSeen = new ConcurrentBag<AgentSession>();
        var both = Check.Signal();
        var calls = 0;
        using var client = new FixtureChatClient(async (messages, _, cancellationToken) =>
        {
            messagesSeen.Add(messages.Where(message => message.Role == ChatRole.User).ToArray());
            if (Interlocked.Increment(ref calls) == 2)
            {
                both.TrySetResult();
            }
            await both.Task.WaitAsync(cancellationToken);
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "independent answer"));
        });
        // Observe real session object identities through the installed MAF middleware API,
        // not only message history (concurrent misuse might otherwise hide a shared session).
        var observedAgent = new AIAgentBuilder(client.AsAIAgent(name: "SameNamedWorker"))
            .Use((messages, session, options, next, cancellationToken) =>
            {
                sessionsSeen.Add(session ?? throw new InvalidOperationException("Expected an explicit worker session."));
                return next(messages, session, options, cancellationToken);
            }).Build();
        var worker = new MafResearchWorker(observedAgent);
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await Check.Within(Task.WhenAll(worker.RunAsync("first question", watchdog.Token), worker.RunAsync("second question", watchdog.Token)));
        Check.That(messagesSeen.Count == 2 && messagesSeen.All(messages => messages.Length == 1),
            "Concurrent runs of one AIAgent must each get a fresh, unshared conversation history.");
        Check.That(messagesSeen.Select(messages => messages[0].Text).Distinct().Count() == 2,
            "Each isolated session must receive its own prompt.");
        var sessions = sessionsSeen.ToArray();
        Check.That(sessions.Length == 2 && !ReferenceEquals(sessions[0], sessions[1]),
            "Actual AgentSession instances must differ even for concurrent runs of the same named agent.");
    }

    public static async Task ThrowingCancellationCallbackAsync()
    {
        await using var jobs = new JobRegistry();
        var entered = Check.Signal();
        var callback = Check.Signal();
        var id = jobs.Start("throwing callback", async cancellationToken =>
        {
            using var registration = cancellationToken.Register(() =>
            {
                callback.TrySetResult();
                throw new InvalidOperationException("private callback failure");
            });
            entered.TrySetResult();
            await callback.Task;
            cancellationToken.ThrowIfCancellationRequested();
            return "unreachable";
        }).Id;
        await Check.Within(entered.Task);
        jobs.Cancel(id);
        await Check.Within(jobs.WaitForCompletionAsync(id));
        Check.That(jobs.List()[0].State == JobState.Cancelled, "A callback error must not prevent execution observation.");
    }

    public static async Task CompletionRacesAsync()
    {
        for (var round = 0; round < 16; round++)
        {
            await using var jobs = new JobRegistry();
            var entered = Check.Signal();
            var release = Check.Signal();
            var id = jobs.Start("racing", async cancellationToken =>
            {
                entered.TrySetResult();
                await release.Task.WaitAsync(cancellationToken);
                return "real result";
            }).Id;
            await Check.Within(entered.Task);
            await Task.WhenAll(Task.Run(() => jobs.Cancel(id)), Task.Run(() => release.TrySetResult()));
            await Check.Within(jobs.WaitForCompletionAsync(id));
            var state = jobs.List()[0].State;
            Check.That(state is JobState.Completed or JobState.Cancelled, "Completion/cancel races cannot leave a task pending.");
            Check.That((state == JobState.Completed) == jobs.Collect(id).Contains("real result"), "Only actual success exposes a result.");
        }
    }
}
