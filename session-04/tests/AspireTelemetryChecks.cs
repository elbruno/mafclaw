// Objective: prove Sample 12 exports real SDK telemetry and reports collector failures.
// A. Start an ephemeral OTLP/HTTP receiver without Aspire, Docker or credentials.
// B. Run the built sample with scripted inference and inspect all three wire payloads.
// C. Reject failed delivery and remote endpoint configuration with nonzero exits.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MafClaw.Session04.Tests;

internal static class AspireTelemetryChecks
{
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        // A. The receiver uses a real socket and accepts OTLP protobuf; it does not fake exporter callbacks.
        var builder = WebApplication.CreateBuilder(["--urls", "http://127.0.0.1:0"]);
        builder.Logging.ClearProviders();
        await using var collector = builder.Build();
        var packets = new ConcurrentDictionary<string, ConcurrentQueue<byte[]>>();
        var reject = false;
        collector.MapPost("/v1/{signal}", async (HttpContext context, string signal) =>
        {
            if (context.Request.ContentType != "application/x-protobuf")
                throw new InvalidOperationException("Expected OTLP protobuf content.");
            using var body = new MemoryStream();
            await context.Request.Body.CopyToAsync(body, context.RequestAborted);
            packets.GetOrAdd(signal, _ => new()).Enqueue(body.ToArray());
            context.Response.ContentType = "application/x-protobuf";
            context.Response.StatusCode = reject ? 400 : 200;
        });
        await collector.StartAsync(cancellationToken);
        try
        {
            // B. Inspect actual serialized signals, not just the sample's PASS string.
            var success = await RunSampleAsync(collector.Urls.Single(), cancellationToken);
            Require(success.ExitCode == 0 && success.Output.Contains("ASPIRE EXPORT PASS"), "Sample 12 export failed.");
            var traceId = Regex.Match(success.Output, @"TRACE ID: ([0-9a-f]{32})").Groups[1].Value;
            Require(traceId.Length == 32, "Sample 12 did not print a trace identity.");
            foreach (var signal in new[] { "traces", "metrics", "logs" })
            {
                Require(packets.TryGetValue(signal, out var bodies) && !bodies.IsEmpty, $"Missing OTLP {signal}.");
                Require(bodies!.Any(body => Encoding.UTF8.GetString(body).Contains("mafclaw-sample12")), $"Missing resource on {signal}.");
            }
            var traces = string.Concat(packets["traces"].Select(Encoding.UTF8.GetString));
            foreach (var operation in new[] { "lesson.run", "invoke_agent", "chat", "execute_tool" })
                Require(traces.Contains(operation), $"Missing real SDK operation: {operation}.");
            var traceBytes = Convert.FromHexString(traceId);
            Require(packets["traces"].Any(body => body.AsSpan().IndexOf(traceBytes) >= 0), "Exported trace identity differs.");
            Require(packets["logs"].Any(body => body.AsSpan().IndexOf(traceBytes) >= 0), "Log is not correlated with the trace.");
            var spans = packets["traces"].SelectMany(body => Messages(body, 1))
                .SelectMany(resource => Messages(resource, 2)).SelectMany(scope => Messages(scope, 2)).ToArray();
            var rootSpan = spans.Single(span => Encoding.UTF8.GetString(Messages(span, 5).Single()) == "lesson.run");
            Require(spans.All(span => Messages(span, 1).Single().SequenceEqual(traceBytes)), "SDK spans are not in one trace.");
            Require(spans.Where(span => !ReferenceEquals(span, rootSpan)).All(span =>
                spans.Any(parent => Messages(parent, 2).Single().SequenceEqual(Messages(span, 4).Single()))),
                "SDK span parent relationships were not exported.");
            var exportedMetrics = packets["metrics"].SelectMany(body => Messages(body, 1))
                .SelectMany(resource => Messages(resource, 2)).SelectMany(scope => Messages(scope, 2));
            var counter = exportedMetrics.Where(metric => Encoding.UTF8.GetString(Messages(metric, 1).Single()) == "lesson.tool.calls").ToArray();
            var points = counter.SelectMany(metric => Messages(metric, 7)).SelectMany(sum => Messages(sum, 1)).ToArray();
            Require(points.Length > 0 && points.All(point =>
                    System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(Messages(point, 6).Single()) == 1),
                "Exported tool counter must equal one, not merely exist.");
            Require(string.Concat(packets["logs"].Select(Encoding.UTF8.GetString)).Contains("Lesson completed with 1 tool call."),
                "Missing structured completion log.");
            foreach (var body in packets.Values.SelectMany(queue => queue))
            {
                var text = Encoding.UTF8.GetString(body);
                Require(!text.Contains("What is this workshop lesson about?") &&
                    !text.Contains("MAF defines the agent;") &&
                    !text.Contains("The Harness orchestrates the model and tools."),
                    "Prompt, answer or tool content leaked into telemetry.");
            }

            // C. HTTP rejection must not become a PASS just because ForceFlush finished.
            reject = true;
            var rejected = await RunSampleAsync(collector.Urls.Single(), cancellationToken);
            Require(rejected.ExitCode != 0 && rejected.Output.Contains("ASPIRE EXPORT FAIL") &&
                !rejected.Output.Contains("ASPIRE EXPORT PASS"), "Rejected exports were reported as successful.");
            await collector.StopAsync(cancellationToken);
            var unavailable = await RunSampleAsync(collector.Urls.Single(), cancellationToken);
            Require(unavailable.ExitCode != 0 && unavailable.Output.Contains("ASPIRE EXPORT FAIL"),
                "An unavailable collector was not reported.");
            var remote = await RunSampleAsync("https://example.invalid", cancellationToken);
            Require(remote.ExitCode != 0 && !remote.Output.Contains("TOOL RESULT"), "Remote endpoint was not rejected before inference.");
        }
        finally { await collector.StopAsync(cancellationToken); }
        Console.WriteLine("SAMPLE12 OTLP TESTS PASS: traces, metrics, correlated logs, content omission and failure paths.");
    }

    private static async Task<(int ExitCode, string Output)> RunSampleAsync(string endpoint, CancellationToken cancellationToken)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "MafClaw.Session04.slnx")))
            root = root.Parent;
        if (root is null) throw new InvalidOperationException("Session source root not found.");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var sample = Path.Combine(root.FullName, "samples", "12-observability-aspire");
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = sample, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(Path.Combine(sample, "bin", configuration, "net10.0", "MafClaw.Sample12.dll"));
        start.ArgumentList.Add("--fixture");
        start.Environment["OTEL_EXPORTER_OTLP_ENDPOINT"] = endpoint;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Sample did not start.");
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
            return (process.ExitCode, await stdout + await stderr);
        }
        finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    // Read just the public OTLP protobuf fields needed by these assertions, without a second SDK.
    private static IEnumerable<byte[]> Messages(byte[] payload, int field)
    {
        using var reader = new BinaryReader(new MemoryStream(payload));
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = reader.Read7BitEncodedInt();
            if ((tag & 7) == 0) { reader.Read7BitEncodedInt64(); continue; }
            var length = (tag & 7) switch
            {
                1 => 8,
                2 => reader.Read7BitEncodedInt(),
                5 => 4,
                _ => throw new InvalidOperationException("Unexpected OTLP protobuf wire type.")
            };
            var value = reader.ReadBytes(length);
            if ((tag >> 3) == field) yield return value;
        }
    }
}
