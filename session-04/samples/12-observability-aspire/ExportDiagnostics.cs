// Objective: keep Sample 12 from claiming success when an OTLP exporter reports failure.
// A. Subscribe only to OpenTelemetry exporter warnings/errors.
// B. Record failure without printing exception payloads or configuration.

using System.Diagnostics.Tracing;

namespace MafClaw.Samples.Aspire;

internal sealed class ExportDiagnostics : EventListener
{
    private int failed;
    public bool Failed => Volatile.Read(ref failed) != 0;

    protected override void OnEventSourceCreated(EventSource source)
    {
        // A. Model success and exporter success are independent, even when ForceFlush finishes.
        if (source.Name.StartsWith("OpenTelemetry", StringComparison.Ordinal) &&
            source.Name.Contains("Exporter", StringComparison.Ordinal))
            EnableEvents(source, EventLevel.Warning);
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // B. Keep raw exporter diagnostics off the screen; Program.cs reports the recovery action.
        if (eventData.Level is EventLevel.Warning or EventLevel.Error or EventLevel.Critical)
            Interlocked.Exchange(ref failed, 1);
    }
}
