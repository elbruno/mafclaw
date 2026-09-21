// Objective: remove content and infrastructure addresses before exporting telemetry.
// A. Keep a small allowlist of useful operation/usage attributes.
// B. Remove baggage/descriptions and drop spans with immutable event payloads.
// C. Preserve trace identity, duration and error status.
using System.Diagnostics;
using OpenTelemetry;

namespace MafClaw.Session04;

public sealed class RedactingActivityProcessor : BaseProcessor<Activity>
{
    private int dropped;
    public int DroppedCount => Volatile.Read(ref dropped);
    public static string[] AllowedTags { get; } =
    [
        "gen_ai.operation.name", "gen_ai.provider.name", "gen_ai.system",
        "gen_ai.request.model", "gen_ai.usage.input_tokens", "gen_ai.usage.output_tokens",
        "gen_ai.token.type", "gen_ai.agent.name", "tool.name", "policy.rule", "error.type"
    ];

    public override void OnEnd(Activity activity)
    {
        foreach (var tag in activity.TagObjects.ToArray())
            if (!AllowedTags.Contains(tag.Key, StringComparer.Ordinal)) activity.SetTag(tag.Key, null);
        foreach (var baggage in activity.Baggage.ToArray()) activity.SetBaggage(baggage.Key, null);
        activity.SetStatus(activity.Status);
        if (activity.Events.Any())
        {
            // ActivityEvent data cannot be rewritten. SDK export processors skip unrecorded spans.
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            activity.IsAllDataRequested = false;
            Interlocked.Increment(ref dropped);
            FinanceTelemetry.DroppedSpans.Add(1);
            Console.Error.WriteLine("Telemetry privacy filter dropped a span containing event payloads.");
        }
    }
}
