// Objective: export real traces and metrics without storing prompts.
// A. Name the application signal sources.
// B. Choose console and/or OTLP exporters explicitly.
// C. Flush and dispose providers when the host ends.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MafClaw.Session04;

public sealed class FinanceTelemetry : IDisposable
{
    // A. Tools and policy wrappers emit to these shared sources; they do not know about exporters.
    public const string SourceName = "MafClaw.Session04";
    public static ActivitySource Activities { get; } = new(SourceName);
    public static Meter Meter { get; } = new(SourceName);
    public static Counter<long> ToolCalls { get; } = Meter.CreateCounter<long>("mafclaw.tool.calls");
    public static Counter<long> PolicyDecisions { get; } = Meter.CreateCounter<long>("mafclaw.policy.denials");
    public static Counter<long> DroppedSpans { get; } = Meter.CreateCounter<long>("mafclaw.telemetry.privacy_drops");
    private readonly TracerProvider? traces;
    private readonly MeterProvider? metrics;
    private readonly TelemetryErrorListener? diagnostics;

    public FinanceTelemetry(Uri? endpoint = null, bool console = false)
    {
        // B. No exporter is selected by default. OpenTelemetry owns collection/batching once enabled.
        if (endpoint is null && !console) return;
        diagnostics = new TelemetryErrorListener();
        var resource = ResourceBuilder.CreateEmpty().AddService("mafclaw-session-04", autoGenerateServiceInstanceId: false);

        // Filter before export: keep operation evidence, not prompts, payloads or infrastructure details.
        var tracing = Sdk.CreateTracerProviderBuilder().SetResourceBuilder(resource).AddSource(SourceName)
            .AddProcessor(new RedactingActivityProcessor());
        var metering = Sdk.CreateMeterProviderBuilder().SetResourceBuilder(resource)
            .AddMeter(SourceName, "Microsoft.Extensions.AI", "Microsoft.Agents.AI")
            .AddView("*", new MetricStreamConfiguration { TagKeys = RedactingActivityProcessor.AllowedTags });
        if (endpoint is not null)
        {
            // The configured URI is a base address; traces and metrics use distinct OTLP HTTP paths.
            if (!string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment))
                throw new FinanceConfigurationException("The OTLP base URI must not include a query or fragment.");
            var baseUri = endpoint.AbsoluteUri.TrimEnd('/');
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(baseUri + "/v1/traces");
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
            metering.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(baseUri + "/v1/metrics");
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        }
        if (console) { tracing.AddConsoleExporter(); metering.AddConsoleExporter(); }
        traces = tracing.Build();
        metrics = metering.Build();
    }

    public void Dispose()
    {
        // C. Flush before disposal so a short demo does not exit while measurements are still buffered.
        // A completed model answer and successful telemetry delivery are separate outcomes.
        var tracesFlushed = traces?.ForceFlush(5000) ?? true;
        var metricsFlushed = metrics?.ForceFlush(5000) ?? true;
        traces?.Dispose();
        metrics?.Dispose();
        diagnostics?.Dispose();
        if (!tracesFlushed || !metricsFlushed)
            Console.Error.WriteLine("Telemetry flush did not complete within its deadline.");
    }
}
