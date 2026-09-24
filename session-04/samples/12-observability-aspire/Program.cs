// Objective: Sample 12 (MAF/Harness) sends a generic agent's telemetry to the Aspire dashboard.
// A. Connect OpenTelemetry traces, metrics and logs to the local dashboard.
// B. Build a small Harness agent with an ordinary lesson-topic tool.
// C. Run one question, then flush telemetry so it remains visible after the console exits.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using MafClaw.Samples;
using MafClaw.Samples.Aspire;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

try
{
    if (args.Length > 1 || (args.Length == 1 && args[0] != "--fixture"))
        throw new InvalidOperationException("Run with dotnet run. Automated check: --fixture.");
    var fixture = args.Contains("--fixture");

    // A. The standalone dashboard receives OTLP/HTTP on 4318, not its browser port 18888.
    // No AppHost, Docker, finance app or extra secret is needed. Tests override only the local port.
    var address = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4318";
    if (!Uri.TryCreate(address, UriKind.Absolute, out var endpoint) || !endpoint.IsLoopback ||
        endpoint.Scheme != Uri.UriSchemeHttp || endpoint.AbsolutePath != "/" ||
        endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0 || endpoint.UserInfo.Length != 0)
        throw new InvalidOperationException("Use a loopback HTTP OTLP base address, such as http://localhost:4318.");
    const string sourceName = "MafClaw.Sample12";
    var resource = ResourceBuilder.CreateEmpty().AddService("mafclaw-sample12", autoGenerateServiceInstanceId: false);
    using var exportErrors = new ExportDiagnostics();
    using var traces = Sdk.CreateTracerProviderBuilder().SetResourceBuilder(resource)
        .AddSource(sourceName, "Microsoft.Extensions.AI")
        .AddOtlpExporter(options => ConfigureExport(options, "traces")).Build();
    using var metrics = Sdk.CreateMeterProviderBuilder().SetResourceBuilder(resource)
        .AddMeter(sourceName, "Microsoft.Extensions.AI", "Microsoft.Agents.AI")
        .AddOtlpExporter(options => ConfigureExport(options, "metrics")).Build();
    using var logs = LoggerFactory.Create(builder => builder.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(resource);
        options.IncludeFormattedMessage = true;
        options.AddOtlpExporter(exporter => ConfigureExport(exporter, "logs"));
    }));
    var logger = logs.CreateLogger("Lesson");
    using var activitySource = new ActivitySource(sourceName);
    using var meter = new Meter(sourceName);
    var toolCounter = meter.CreateCounter<long>("lesson.tool.calls", description: "Actual lesson-tool invocations.");

    // B. Microsoft.Extensions.AI instruments model calls; MAF instruments the agent and dispatches tools.
    // Content capture stays off on both wrappers. Our log contains a count, not the prompt or tool payload.
    IChatClient model = fixture
        ? new FixtureChatClient(
            new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [new FunctionCallContent("fixture-topic", "get_lesson_topic", new Dictionary<string, object?>())])),
            FixtureChatClient.Text("The Harness orchestrates the model and tools."))
        : DemoSettings.Load().CreateChatClient();
    using var observedModel = model.AsBuilder().UseOpenTelemetry(sourceName: sourceName,
        configure: telemetry => telemetry.EnableSensitiveData = false).Build();
    var toolCalls = 0;
    var harness = observedModel.AsHarnessAgent(new HarnessAgentOptions
    {
        Name = "AspireLessonAgent",
        DisableFileMemory = true, DisableAgentSkillsProvider = true, DisableWebSearch = true,
        DisableTodoProvider = true, DisableAgentModeProvider = true, DisableToolAutoApproval = true,
        DisableOpenTelemetry = true,
        ChatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(GetLessonTopic, "get_lesson_topic")],

            // This deployment requires reasoning off for Chat Completions tool calls.
            Reasoning = new ReasoningOptions { Effort = ReasoningEffort.None },
            Instructions = "Call get_lesson_topic once, then explain its result in one sentence."
        }
    });
    var agent = harness.AsBuilder().UseOpenTelemetry(sourceName,
        telemetry => telemetry.EnableSensitiveData = false).Build();

    // C. This root span groups the SDK's agent/model/tool spans and our correlated structured log.
    // The dashboard stores telemetry in memory; the console can exit without losing this run.
    Console.WriteLine(fixture ? "FIXTURE inference; REAL Harness, tool and OTLP export." : "LIVE model; REAL OTLP export to the local dashboard.");
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
    using (var run = activitySource.StartActivity("lesson.run"))
    {
        run?.SetTag("lesson.inference", fixture ? "fixture" : "live");
        var response = await agent.RunAsync("What is this workshop lesson about?",
            await agent.CreateSessionAsync(deadline.Token), cancellationToken: deadline.Token);
        if (toolCalls != 1 || !response.Messages.SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>().Any(result => result.Exception is null))
            throw new InvalidOperationException("Expected one real lesson-tool call and its result.");
        Console.WriteLine($"ANSWER: {response.Text}");
        Console.WriteLine($"TRACE ID: {run?.TraceId}");
        logger.LogInformation("Lesson completed with {ToolCalls} tool call.", toolCalls);
    }

    // Short console demos must flush before exiting. An answer alone does not prove exporter success.
    var tracesFlushed = traces.ForceFlush(10000);
    var metricsFlushed = metrics.ForceFlush(10000);
    logs.Dispose();
    if (!tracesFlushed || !metricsFlushed || exportErrors.Failed)
    {
        Console.Error.WriteLine("ASPIRE EXPORT FAIL: start aspire dashboard run and check its OTLP/HTTP port (4318).");
        return 1;
    }
    Console.WriteLine("ASPIRE EXPORT PASS: open mafclaw-sample12 in the dashboard's Traces, Metrics and Structured logs.");
    return 0;

    void ConfigureExport(OtlpExporterOptions options, string signal)
    {
        options.Endpoint = new Uri(endpoint, $"v1/{signal}");
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.TimeoutMilliseconds = 5000;
    }

    [Description("Returns a synthetic workshop topic without network or file access.")]
    string GetLessonTopic()
    {
        toolCalls++;
        toolCounter.Add(1);
        const string topic = "MAF defines the agent; the Harness orchestrates its model and tools.";
        Console.WriteLine($"TOOL RESULT: {topic}");
        return topic;
    }
}
catch (Exception exception)
{
    Console.Error.WriteLine("Sample 12 failed. Check shared model setup and start the Aspire dashboard before retrying.");
    return DemoOutput.Report(exception);
}
