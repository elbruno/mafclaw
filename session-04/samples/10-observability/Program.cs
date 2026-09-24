// Objective: Sample 10 (plain C#) teaches traces and metrics before introducing an agent.
// A. Listen for spans and a tool-call counter.
// B. Run one request that calls one tiny teaching tool.
// C. Show parent/child correlation and verify the observed evidence.

using System.Diagnostics;
using System.Diagnostics.Metrics;

if (args.Length != 0)
{
    Console.Error.WriteLine("Usage: dotnet run (no arguments). This sample has no model or MAF dependency.");
    return 2;
}

// A. A trace describes one request; a counter summarizes how often an operation ran.
// Subscribe before starting work so ActivitySource actually creates the spans.
var spans = new List<Activity>();
long toolCalls = 0;
using var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == "MafClaw.Sample10",
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
    ActivityStopped = spans.Add
};
ActivitySource.AddActivityListener(listener);
using var meterListener = new MeterListener
{
    InstrumentPublished = (instrument, active) =>
    {
        if (instrument.Meter.Name == "MafClaw.Sample10") active.EnableMeasurementEvents(instrument);
    }
};
meterListener.SetMeasurementEventCallback<long>((_, value, _, _) => toolCalls += value);
meterListener.Start();

// B. Ordinary C# owns this call sequence. There is no model, prompt, tool dispatcher or harness yet.
using var source = new ActivitySource("MafClaw.Sample10");
using var meter = new Meter("MafClaw.Sample10");
var counter = meter.CreateCounter<long>("lesson.tool.calls");
Console.WriteLine("PLAIN C#: request -> get_lesson_topic. No model, no MAF, no network.");
using (var request = source.StartActivity("request"))
{
    using var tool = source.StartActivity("get_lesson_topic");
    counter.Add(1);
    Console.WriteLine($"TOOL RESULT: {GetLessonTopic()}");
    tool?.SetStatus(ActivityStatusCode.Ok);
    request?.SetStatus(ActivityStatusCode.Ok);
}

// C. Spans stop inside-out; print the root first to make the parent/child relationship obvious.
var root = spans.SingleOrDefault(span => span.DisplayName == "request");
var child = spans.SingleOrDefault(span => span.DisplayName == "get_lesson_topic");
foreach (var span in spans.AsEnumerable().Reverse())
{
    var indent = span.ParentSpanId == default ? "" : "  ";
    Console.WriteLine($"{indent}{span.DisplayName}: {span.Duration.TotalMilliseconds:F2} ms; " +
        $"status={span.Status}; trace={span.TraceId}; span={span.SpanId}; parent={span.ParentSpanId}");
}
Console.WriteLine($"METRIC lesson.tool.calls = {toolCalls}");
var passed = spans.Count == 2 && toolCalls == 1 && root is not null && child is not null &&
    root.TraceId == child.TraceId && child.ParentSpanId == root.SpanId &&
    root.Status == ActivityStatusCode.Ok && child.Status == ActivityStatusCode.Ok;
Console.WriteLine(passed ? "OBSERVABILITY PRIMITIVE PASS" : "OBSERVABILITY PRIMITIVE FAIL");
Console.WriteLine("Next: Sample 11 lets MAF's Harness run the model/tool loop and emit the spans.");
return passed ? 0 : 1;

// A tiny, deterministic teaching tool keeps attention on observability, not business calculations.
static string GetLessonTopic() => "MAF defines the agent; the Harness orchestrates its model and tools.";
