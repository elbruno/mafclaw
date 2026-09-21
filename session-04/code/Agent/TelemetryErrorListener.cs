// Objective: surface exporter diagnostics without repeating payloads or addresses.
// A. Listen for OpenTelemetry warning/error events.
// B. Print only a fixed safe diagnostic and count the events.
using System.Diagnostics.Tracing;

namespace MafClaw.Session04;

public sealed class TelemetryErrorListener : EventListener
{
    private int errors;
    public int ErrorCount => Volatile.Read(ref errors);
    protected override void OnEventSourceCreated(EventSource source)
    {
        if (source.Name.StartsWith("OpenTelemetry", StringComparison.Ordinal))
            EnableEvents(source, EventLevel.Warning);
    }
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData.EventName == "MetricInstrumentIgnored" &&
            eventData.Payload is { Count: > 2 } payload &&
            payload[2] is string reason &&
            reason == "Instrument belongs to a Meter not subscribed by this provider. If another MeterProvider is configured to listen to this Meter, this warning can be ignored.")
            return;
        if (eventData.Level is EventLevel.Error or EventLevel.Warning or EventLevel.Critical)
        {
            Interlocked.Increment(ref errors);
            Console.Error.WriteLine($"Telemetry warning/error: {eventData.EventName} ({eventData.EventId}). Inspect configuration privately.");
        }
    }
}
