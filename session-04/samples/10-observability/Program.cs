// Objective: observe a real local calculation using plain .NET diagnostics.
// A. Subscribe to one activity source and meter.
// B. Calculate the synthetic snapshot inside nested spans.
// C. Verify the recorded operations and measurement.
using System.Diagnostics;
using System.Diagnostics.Metrics;
using MafClaw.Session04;

var spans = 0;
long measurements = 0;
using var listener = new ActivityListener
{
    ShouldListenTo = candidate => candidate.Name == "MafClaw.Sample10",
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
    ActivityStopped = activity =>
    {
        spans++;
        Console.WriteLine($"{activity.DisplayName}: {activity.Duration.TotalMilliseconds:F2} ms; trace={activity.TraceId}");
    }
};
ActivitySource.AddActivityListener(listener);
using var meterListener = new MeterListener
{
    InstrumentPublished = (instrument, active) =>
    {
        if (instrument.Meter.Name == "MafClaw.Sample10") active.EnableMeasurementEvents(instrument);
    }
};
meterListener.SetMeasurementEventCallback<long>((_, value, _, _) => measurements += value);
meterListener.Start();
using var source = new ActivitySource("MafClaw.Sample10");
using var meter = new Meter("MafClaw.Sample10");
var counter = meter.CreateCounter<long>("portfolio.calculations");
using (source.StartActivity("request"))
using (source.StartActivity("value-portfolio"))
{
    Console.WriteLine($"Mock total: {MockPortfolio.Summarize().Total:F2}. Not financial advice.");
    counter.Add(1);
}
var passed = spans == 2 && measurements == 1;
Console.WriteLine(passed ? "OBSERVABILITY PRIMITIVE PASS" : "OBSERVABILITY PRIMITIVE FAIL");
return passed ? 0 : 1;
