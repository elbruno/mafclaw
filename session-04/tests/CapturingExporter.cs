// Objective: inspect the data that would leave the telemetry pipeline.
// A. Capture only the post-processor activity representation.
// B. Let tests assert that synthetic sensitive markers were removed.
using System.Diagnostics;
using System.Text.Json;
using OpenTelemetry;

namespace MafClaw.Session04.Tests;

internal sealed class CapturingExporter : BaseExporter<Activity>
{
    public List<string> Payloads { get; } = [];
    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
            Payloads.Add(JsonSerializer.Serialize(new
            {
                activity.DisplayName, activity.TraceId, activity.SpanId,
                activity.Status, activity.StatusDescription,
                tags = activity.TagObjects.ToArray(),
                events = activity.Events.Select(item => new { item.Name, tags = item.Tags.ToArray() }),
                baggage = activity.Baggage.ToArray()
            }));
        return ExportResult.Success;
    }
}
