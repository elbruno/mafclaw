// Objective: make Sample 11's SDK-generated spans readable without a dashboard or raw payload dump.
// A. Subscribe to the sample's agent/model source and the SDK's tool-invocation source.
// B. Print parent/child relationships, elapsed time and status only.
// C. Verify that agent, model and tool work belong to the same real trace.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace MafClaw.Session04.Samples;

public sealed class TraceConsole : IDisposable
{
    public const string SourceName = "MafClaw.Sample11";
    private readonly ConcurrentQueue<Activity> spans = new();
    private readonly ActivityListener listener;

    public TraceConsole()
    {
        // A. Listening enables instrumentation, but does not create any agent, model or tool spans itself.
        listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is SourceName or "Microsoft.Extensions.AI",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Enqueue
        };
        ActivitySource.AddActivityListener(listener);
    }

    public bool HasError => spans.Any(span => span.Status == ActivityStatusCode.Error);

    public void Print()
    {
        // B. Do not dump tags/events: they can contain content. Operation labels are allowlisted below.
        Console.WriteLine("SDK TRACE (local observation, NOT collector delivery):");
        var recorded = spans.ToArray();
        foreach (var span in recorded.OrderBy(span => span.StartTimeUtc))
        {
            var depth = 0;
            var parent = span.ParentSpanId;
            while (recorded.FirstOrDefault(candidate => candidate.SpanId == parent) is { } ancestor)
            {
                depth++;
                parent = ancestor.ParentSpanId;
            }
            Console.WriteLine($"{new string(' ', depth * 2)}{Operation(span)}: {span.Duration.TotalMilliseconds:F2} ms; " +
                $"status={span.Status}; trace={span.TraceId}; span={span.SpanId}; parent={span.ParentSpanId}");
        }
        Console.WriteLine("Unset means the SDK did not explicitly set status; it is not a fabricated success value.");
        Console.WriteLine("Token usage: not shown. A fixture is not a token/cost benchmark.");
    }

    public bool HasAgentModelToolTrace()
    {
        // C. Require actual SDK stages and parent links, not a printed diagram or a plausible answer.
        var recorded = spans.ToArray();
        var agents = recorded.Where(span => Operation(span) == "invoke_agent").ToArray();
        return agents.Length == 1 && recorded.All(span => span.TraceId == agents[0].TraceId) &&
            recorded.Count(span => Operation(span) == "chat") >= 2 &&
            recorded.Any(span => Operation(span) == "execute_tool") &&
            recorded.Where(span => span != agents[0]).All(span => recorded.Any(parent => parent.SpanId == span.ParentSpanId));
    }

    private static string Operation(Activity span) => span.GetTagItem("gen_ai.operation.name")?.ToString() switch
    {
        "invoke_agent" => "invoke_agent",
        "chat" => "chat",
        "execute_tool" => "execute_tool",
        _ => "sdk_operation"
    };

    public void Dispose() => listener.Dispose();
}
